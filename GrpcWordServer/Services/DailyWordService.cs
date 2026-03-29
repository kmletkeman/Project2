using Grpc.Core;
using GrpcWordServer;
using GrpcWordServer.Protos;

namespace GrpcWordServer.Services
{
    public class DailyWordService : DailyWord.DailyWordBase
    {
        private readonly WordProvider _wordProvider;
        public DailyWordService(WordProvider wordProvider)
        {
            _wordProvider = wordProvider;
        }

        public override Task<WordResponse> GetWord(Empty request, ServerCallContext context)
        {
            string word = _wordProvider.GetDailyWord();
            return Task.FromResult(new WordResponse { Word = word });
        }

        public override Task<BoolResponse> ValidateWord(WordRequest request, ServerCallContext context)
        {
            bool isValid = _wordProvider.IsValidWord(request.Word);
            return Task.FromResult(new BoolResponse { Valid = isValid });
        }
    }
}
