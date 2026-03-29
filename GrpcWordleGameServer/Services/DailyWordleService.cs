using Grpc.Core;
using GrpcWordleGameServer.Protos;
using GrpcWordleGameServer.Clients;
using GrpcWordleGameServer.Services;

namespace GrpcWordleGameServer.Services
{
    public class DailyWordleService : DailyWordle.DailyWordleBase
    {

        public override async Task Play(IAsyncStreamReader<GuessRequest> requestStream, 
                                  IServerStreamWriter<GuessResponse> responseStream, 
                                  ServerCallContext context)
        {
            string targetWord = DailyWordServiceClient.GetDailyWord().ToLower();

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

            while (await requestStream.MoveNext() && !gameWon && turnsUsed < 6)
            {
                string wordPlayed = requestStream.Current.Guess.ToLower().Trim();

                bool isValid = DailyWordServiceClient.ValidateWord(wordPlayed);

                if(!isValid)
                {
                    await responseStream.WriteAsync(new GuessResponse
                    {
                        Message = $"Invalid word: {wordPlayed}. Must be a valid 5-letter English word."
                    });
                    continue;
                }

                turnsUsed++;

                char[] results = new char[5];

                if (wordPlayed == targetWord)
                {
                    gameWon = true;
                    for (int i = 0; i < 5; i++)
                        results[i] = '*';
                }
                else
                {
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

                string feedback = new string(results);

                string message = gameWon
                ? $"Congratulations! You've guessed the word in {turnsUsed} turns!"
                : (turnsUsed >= 6 ? $"Game over. The word was: {targetWord}" : $"Feedback: {feedback}");

                await responseStream.WriteAsync(new GuessResponse
                {
                    Feedback = feedback,
                    IsCorrect = gameWon,
                    GameOver = gameWon || turnsUsed >= 6,
                    Message = message
                });
            }

            StatsManager.UpdateStats(gameWon, turnsUsed);
        }

        public override Task<StatsResponse> GetStats(Empty request, ServerCallContext context)
        {
            var stats = StatsManager.GetCurrentStats();
            return Task.FromResult(stats!);
        }

        private static int CountFrequency(string word, char letter)
        {
            int count = 0;
            foreach (char c in word)
            {
                if (c == letter) count++;
            }
            return count;
        }
    }
}
