using Grpc.Core;
using Grpc.Net.Client;
using GrpcWordleGameServer.Protos;
using GrpcWordServer.Protos;
using System;
using System.Threading.Tasks;

namespace TestWordleClient;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("=== Wordle Test Client (Standalone) ===\n");

        // Show today's correct word (makes testing easy)
        var wordChannel = GrpcChannel.ForAddress("https://localhost:7179");// WordServer port
        var wordClient = new DailyWord.DailyWordClient(wordChannel);

        string correctWord = "";
        try
        {
            // Fully qualify Empty to avoid ambiguity
            var wordReply = await wordClient.GetWordAsync(new GrpcWordServer.Protos.Empty());
            correctWord = wordReply.Word.ToUpper();
            Console.WriteLine($"Today's secret word is: {correctWord}\n");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Could not get daily word: {ex.Message}\n");
        }

        // Start the actual game
        var gameChannel = GrpcChannel.ForAddress("https://localhost:7004");// GameServer port
        var gameClient = new DailyWordle.DailyWordleClient(gameChannel);

        Console.WriteLine("Starting game...\n");

        using var call = gameClient.Play();

        // Read responses from server
        var readTask = Task.Run(async () =>
        {
            while (await call.ResponseStream.MoveNext())
            {
                var response = call.ResponseStream.Current;
                Console.WriteLine($"Feedback : {response.Feedback}");
                if (!string.IsNullOrEmpty(response.Message))
                    Console.WriteLine($"Message  : {response.Message}");
                Console.WriteLine($"Game Over: {response.GameOver}\n");
            }
        });

        // User types guesses
        while (true)
        {
            Console.Write("Enter your 5-letter guess (or press Enter to finish): ");
            string? guess = Console.ReadLine()?.Trim().ToLower();

            if (string.IsNullOrEmpty(guess))
                break;

            if (guess.Length != 5)
            {
                Console.WriteLine("Guess must be exactly 5 letters.\n");
                continue;
            }

            await call.RequestStream.WriteAsync(new GuessRequest { Guess = guess });
        }

        await call.RequestStream.CompleteAsync();
        await readTask;

        // Final statistics
        Console.WriteLine("\n=== Final Statistics for Today ===");
        try
        {
            var stats = await gameClient.GetStatsAsync(new GrpcWordleGameServer.Protos.Empty());
            Console.WriteLine($"Players today     : {stats.Players}");
            Console.WriteLine($"Win percentage   : {stats.WinPercent:F1}%");
            Console.WriteLine("\nGuess Distribution:");
            foreach (var entry in stats.GuessDistribution)
                Console.WriteLine($"  {entry.Key} guesses : {entry.Value} players");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Could not fetch stats: {ex.Message}");
        }

        Console.WriteLine("\nTest finished. Press any key to exit.");
        Console.ReadKey();
    }
}