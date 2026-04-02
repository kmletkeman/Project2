/*
 Programmer: Kevin Letkeman
 Purpose: Provides the daily word and validates guesses using gRPC to communicate this with clients
 Date: 2026-03-31
 */

using Grpc.Core;
using GrpcWordServer;
using GrpcWordServer.Protos;

namespace GrpcWordServer.Services
{
    public class DailyWordService : DailyWord.DailyWordBase
    {
        // WordService instance to manage the daily word and validate guesses
        private readonly WordService _wordService = new WordService();

        // GetWord method to return the daily word to clients
        public override Task<WordResponse> GetWord(Empty request, ServerCallContext context)
        {
            return Task.FromResult(new WordResponse { Word = _wordService.GetDailyWord() });
        }

        // ValidateWord method to check if a client's guess is correct
        public override Task<BoolResponse> ValidateWord(WordRequest request, ServerCallContext context)
        {
            return Task.FromResult(new BoolResponse { Valid = _wordService.IsValidWord(request.Word.ToLower()) });
        }
    }
}
