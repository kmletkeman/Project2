using Grpc.Core;
using GrpcWordServer;
using System.Text.Json;

namespace GrpcWordServer.Services
{
    public class WordProvider
    {
        private readonly List<string> _words;

        public WordProvider()
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wordle.json");
            string json = File.ReadAllText(path);

            _words = JsonSerializer.Deserialize<List<string>>(json)?
                .Select(w => w.ToLowerInvariant().Trim())
                .Where(w => w.Length == 5)
                .Distinct()
                .ToList() ?? new List<string>();

            if (_words.Count == 0)
                throw new Exception("wordle.json is empty or invalid.");
        }

        public string GetDailyWord()
        {
            var today = DateTime.UtcNow.Date;

            int seed = today.Year * 10000 + today.Month * 100 + today.Day;
            var random = new Random(seed);

            return _words[random.Next(_words.Count)];
        }

        public bool IsValidWord(string word)
        {
            if (string.IsNullOrWhiteSpace(word) || word.Length != 5)
                return false;

            return _words.Contains(word.ToLowerInvariant().Trim());
        }
    }
}
