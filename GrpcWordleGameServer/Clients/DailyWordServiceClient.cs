using Grpc.Net.Client;
using GrpcWordServer.Protos;
using Grpc.Core;
using System.Net.NetworkInformation;

namespace GrpcWordleGameServer.Clients
{
    public static class DailyWordServiceClient
    {
        private static DailyWord.DailyWordClient? _dailyWordClient = null;

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
