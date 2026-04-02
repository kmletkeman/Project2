/*
 Programmer: Kevin Letkeman
 Purpose: Client for the DailyWord gRPC service, provides the daily word and validates guesses
 Date: 2026-03-31
 */

using Grpc.Net.Client;
using GrpcWordServer.Protos;
using Grpc.Core;
using System.Net.NetworkInformation;

namespace GrpcWordleGameServer.Clients
{
    public static class DailyWordServiceClient
    {
        private static DailyWord.DailyWordClient? _dailyWordClient = null;

        // Retrieves the daily word from the gRPC service
        // Returns an empty string if the service is unavailable
        public static string GetDailyWord()
        {
            ConnectToService();

            try
            {
                var reply = _dailyWordClient?.GetWord(new Empty());
                return reply?.Word ?? "";
            }
            catch (RpcException)
            {
                return "";
            }
        }

        // Validates a guessed word against the daily word using the gRPC service
        // Returns false if the service is unavailable or if the word is invalid
        public static bool ValidateWord(string word)
        {
            if (string.IsNullOrWhiteSpace(word))
                return false;

            ConnectToService();

            try
            {
                var reply = _dailyWordClient?.ValidateWord(new WordRequest { Word = word.ToLower() });
                return reply?.Valid ?? false;
            }
            catch (RpcException)
            {
                return false;
            }
        }

        // Establishes a connection to the gRPC service if not already connected
        private static void ConnectToService()
        {
            if (_dailyWordClient is null)
            {
                var channel = GrpcChannel.ForAddress("https://localhost:7179");
                _dailyWordClient = new DailyWord.DailyWordClient(channel);
            }
        }

    }
}
