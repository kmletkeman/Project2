using Grpc.Core;
using GrpcWordServer;
using System.Text.Json;

namespace GrpcWordServer.Services
{
    public class WordProvider
    {
        private readonly List<string> _words;
        private string? _todayWord;
        private DateTime _lastDate = DateTime.MinValue;
        private readonly object _lock = new();

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
            lock (_lock)
            {
                var today = DateTime.UtcNow.Date;

                if (_todayWord == null || _lastDate != today)
                {
                    var random = new Random();
                    _todayWord = _words[random.Next(_words.Count)];
                    _lastDate = today;
                    Console.WriteLine($"[WordProvider] New daily word set for {today:yyyy-MM-dd}");
                }

                return _todayWord;
            }
        }

        public bool IsValidWord(string word)
        {
            if (string.IsNullOrWhiteSpace(word) || word.Length != 5)
                return false;

            return _words.Contains(word.ToLowerInvariant().Trim());
        }
    }
}
