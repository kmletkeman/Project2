using GrpcWordleGameServer.Protos;
using System.Text.Json;

namespace GrpcWordleGameServer.Services
{
    public class StatsManager
    {
        private static readonly string _filePath = 
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "daily_stats.json");
        private static readonly Mutex _mutex = new(false, "WordleStatsMutex");

        public static void UpdateStats(bool won, int guessesUsed)
        {
            _mutex.WaitOne();
            try
            {
                var stats = LoadStats();

                stats.Players++;
                if(won)
                {
                    stats.Winners++;
                    if (stats.GuessDistribution.ContainsKey(guessesUsed))
                        stats.GuessDistribution[guessesUsed]++;
                    else
                        stats.GuessDistribution[guessesUsed] = 1;
                }

                SaveStats(stats);
            }
            finally
            {
                _mutex.ReleaseMutex();
            }
        }

        public static StatsResponse GetCurrentStats()
        {
            _mutex.WaitOne();
            try
            {
                var data = LoadStats();

                int players = data.Players;
                double winPercent = players == 0 ? 0 : Math.Round(data.Winners * 100.0 / players);

                var response = new StatsResponse
                {
                    Players = players,
                    WinPercent = winPercent
                };

                foreach ( var kvp in data.GuessDistribution)
                    response.GuessDistribution.Add(kvp.Key, kvp.Value);

                return response;
            }
            finally
            {
                _mutex.ReleaseMutex();
            }
        }

        private static DailyStatsData LoadStats()
        {
            // If the file doesn't exist, return a new stats object with today's date
            if (!File.Exists(_filePath))
                return new DailyStatsData { Date = DateTime.UtcNow.Date };

            string json = File.ReadAllText(_filePath);
            var data = JsonSerializer.Deserialize<DailyStatsData>(json) ?? new DailyStatsData();

            // Reset stats if the date has changed
            if (data.Date.Date != DateTime.UtcNow.Date)
                return new DailyStatsData { Date = DateTime.UtcNow.Date };

            return data;
        }

        private static void SaveStats(DailyStatsData data)
        {
            string json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_filePath, json);
        }

        private class DailyStatsData
        {
            public DateTime Date { get; set; } = DateTime.MinValue;
            public int Players { get; set; } = 0;
            public int Winners { get; set; } = 0;
            public Dictionary<int, int> GuessDistribution { get; set; } = new();
        }
    }
}
