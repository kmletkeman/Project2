/*
 Programmer: Kevin Letkeman
 Purpose: Service implementation for the DailyWordle gRPC service, 
          which handles the game logic for a daily Wordle game
          It processes player guesses, provides feedback, and tracks game statistics
 Date: 2026-03-31
 */

using Grpc.Core;
using GrpcWordleGameServer.Protos;
using GrpcWordleGameServer.Clients;
using GrpcWordleGameServer.Services;

namespace GrpcWordleGameServer.Services
{
    public class DailyWordleService : DailyWordle.DailyWordleBase
    {

        // The main game loop that processes incoming guesses and sends feedback until the game is won or lost
        public override async Task Play(IAsyncStreamReader<GuessRequest> requestStream, 
                                  IServerStreamWriter<GuessResponse> responseStream, 
                                  ServerCallContext context)
        {
            // Retrieve the daily word from the DailyWordServiceClient
            string targetWord = DailyWordServiceClient.GetDailyWord().ToLower();

            // If we couldn't retrieve the daily word, send an error message and end the game
            if (string.IsNullOrEmpty(targetWord))
            {
                await responseStream.WriteAsync(new GuessResponse 
                { 
                    Message = "Error: Could not retrieve the daily word."
                });
                return;
            }

            int turnsUsed = 0;
            bool gameWon = false;

            // Process each guess from the client until the game is won or the player runs out of turns
            while (await requestStream.MoveNext() && !gameWon && turnsUsed < 6)
            {
                // Normalize the guessed word by converting it to lowercase and trimming whitespace
                string wordPlayed = requestStream.Current.Guess.ToLower().Trim();

                // Validate the guessed word using the DailyWordServiceClient
                // Checks if the word is a valid 5 letter English word
                bool isValid = DailyWordServiceClient.ValidateWord(wordPlayed);

                // If the guessed word is invalid, sends error message and skips the loop iteration
                if (!isValid)
                {
                    await responseStream.WriteAsync(new GuessResponse
                    {
                        Message = $"Invalid word: {wordPlayed}. Must be a valid 5-letter English word."
                    });
                    continue;
                }

                turnsUsed++;

                // Create an array to hold the feedback for each letter in the guessed word
                char[] results = new char[5];

                // If the guessed word matches the target word
                // mark all letters as correct and end the game
                if (wordPlayed == targetWord)
                {
                    gameWon = true;
                    for (int i = 0; i < 5; i++)
                        results[i] = '*';
                }
                // If they don't match provide feedback on which letters are in the correct position,
                // which are in the wrong position, and which are not in the target word at all
                else
                {
                    // Dictionary to track how many times each letter has been matched in the correct position
                    var matches = new Dictionary<char, int>
                    {
                        ['a'] = 0, ['b'] = 0, ['c'] = 0, ['d'] = 0, ['e'] = 0, ['f'] = 0, ['g'] = 0,
                        ['h'] = 0, ['i'] = 0, ['j'] = 0, ['k'] = 0, ['l'] = 0, ['m'] = 0, ['n'] = 0,
                        ['o'] = 0, ['p'] = 0, ['q'] = 0, ['r'] = 0, ['s'] = 0, ['t'] = 0, ['u'] = 0,
                        ['v'] = 0, ['w'] = 0, ['x'] = 0, ['y'] = 0, ['z'] = 0
                    };

                    //Search for letters that are in the correct position first
                    for (int i = 0; i < 5; i++)
                    {
                        char letter = wordPlayed[i];
                        // If the letter is in the correct position, mark it with '*' and
                        // increment the match count for that letter in the dictionary
                        if (letter == targetWord[i])
                        {
                            results[i] = '*';
                            matches[letter]++;
                        }
                    }

                    // Search for letters that are in the word but in the wrong position
                    for (int i = 0; i < 5; i++)
                    {
                        char letter = wordPlayed[i];

                        if (results[i] == '*')
                            continue; // Skip letters already marked as correct

                        if (CountFrequency(targetWord, letter) == 0)
                            results[i] = 'x'; // Letter not in target word

                        else if (letter != targetWord[i] && 
                                matches[letter] < CountFrequency(targetWord, letter))
                        {
                            results[i] = '?'; // Letter in word but wrong position
                            matches[letter]++;
                        }
                        else
                            results[i] = 'x'; // Letter not in target word or already accounted for
                    }
                }

                // Convert the results array to a string to send back as feedback to the client
                string feedback = new string(results);

                string message = gameWon
                ? $"Congratulations! You've guessed the word in {turnsUsed} turns!"
                : (turnsUsed >= 6 ? $"Game over. The word was: {targetWord}" : $"Feedback: {feedback}");

                // Send the feedback, game status, and message back to the client
                await responseStream.WriteAsync(new GuessResponse
                {
                    Feedback = feedback,
                    IsCorrect = gameWon,
                    GameOver = gameWon || turnsUsed >= 6,
                    Message = message
                });
            }

            // Update the game statistics using the StatsManager after the game has ended
            StatsManager.UpdateStats(gameWon, turnsUsed);
        }

        // Endpoint to retrieve the current game statistics
        public override Task<StatsResponse> GetStats(Empty request, ServerCallContext context)
        {
            var stats = StatsManager.GetCurrentStats();
            return Task.FromResult(stats);
        }

        // Helper method to count the frequency of a specific letter in a given word
        private static int CountFrequency(string word, char letter)
        {
            int count = 0;
            foreach (char c in word)
                if (c == letter) count++;

            return count;
        }
    }
}
