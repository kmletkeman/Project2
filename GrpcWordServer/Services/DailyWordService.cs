using Grpc.Core;
using GrpcWordServer;
using GrpcWordServer.Protos;

namespace GrpcWordServer.Services
{
    public class DailyWordService : DailyWord.DailyWordBase
    {
        private readonly WordService _wordService;
        public DailyWordService(WordService wordService)
        {
            _wordService = wordService;
        }

        public override Task<WordResponse> GetWord(Empty request, ServerCallContext context)
        {
            return Task.FromResult(new WordResponse { Word = _wordService.GetDailyWord() });
        }

        public override Task<BoolResponse> ValidateWord(WordRequest request, ServerCallContext context)
        {
            return Task.FromResult(new BoolResponse { Valid = _wordService.IsValidWord(request.Word.ToLower()) });
        }
    }
}
