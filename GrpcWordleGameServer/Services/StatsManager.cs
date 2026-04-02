/*
 Programmer: Kevin Letkeman
 Purpose: Manages daily game statistics (num of players, winners, and guess distribution) 
          It resets the statistics daily
 Date: 2026-03-31
 */

using GrpcWordleGameServer.Protos;
using System.Text.Json;

namespace GrpcWordleGameServer.Services
{
    public class StatsManager
    {
        // File path for storing daily statistics
        private static readonly string _filePath = 
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "daily_stats.json");
        // Mutex to ensure thread-safe access to the statistics file
        private static readonly Mutex _mutex = new(false, "WordleStatsMutex");

        // Updates the statistics after a game is completed
        // It increments the player count, winner count (if won), and updates the guess distribution
        public static void UpdateStats(bool won, int guessesUsed)
        {
            // Lock the mutex to ensure thread-safe access to the stats file
            _mutex.WaitOne();
            try
            {
                // Calls LoadStats to read the current statistics from the file
                var stats = LoadStats();

                stats.Players++;// Increment the total number of players

                // If the player won, increment the winners count and update the guess distribution
                if (won)
                {
                    stats.Winners++;
                    // If the guess count already exists in the distribution increment it
                    // Else add it with a count of 1
                    if (stats.GuessDistribution.ContainsKey(guessesUsed))
                        stats.GuessDistribution[guessesUsed]++;
                    else
                        stats.GuessDistribution[guessesUsed] = 1;
                }

                // Calls SaveStats to write the updated statistics back to the file
                SaveStats(stats);
            }
            finally
            {
                _mutex.ReleaseMutex();// Ensure the mutex is released even if an exception occurs
            }
        }

        // Retrieves the current statistics for the day
        public static StatsResponse GetCurrentStats()
        {
            // Lock the mutex to ensure thread-safe access to the stats file
            _mutex.WaitOne();
            try
            {
                // Calls LoadStats to read the current statistics from the file
                var data = LoadStats();

                // Get the total number of players
                int players = data.Players;
                // Calculate the win percentage, handles division by zero if there are no players
                double winPercent = players == 0 ? 0 : Math.Round(data.Winners * 100.0 / players);

                // Create a StatsResponse object to return the statistics
                var response = new StatsResponse
                {
                    Players = players,
                    WinPercent = winPercent
                };

                // Populate the guess distribution in the response object
                foreach ( var k in data.GuessDistribution)
                    response.GuessDistribution.Add(k.Key, k.Value);

                return response;
            }
            finally
            {
                _mutex.ReleaseMutex();// Ensure the mutex is released even if an exception occurs
            }
        }

        // Loads the statistics from the file, if the file doesn't exist or the date has changed
        // return a new stats object with today's date
        private static DailyStatsData LoadStats()
        {
            // If the file doesn't exist, return a new stats object with today's date
            if (!File.Exists(_filePath))
                return new DailyStatsData { Date = DateTime.UtcNow.Date };

            // Read the JSON data from the file
            string json = File.ReadAllText(_filePath);
            // Deserialize the JSON data into a DailyStatsData object. If it fails, assigns an empty object
            var data = JsonSerializer.Deserialize<DailyStatsData>(json) ?? new DailyStatsData();

            // Reset stats if the date has changed
            if (data.Date.Date != DateTime.UtcNow.Date)
                return new DailyStatsData { Date = DateTime.UtcNow.Date };

            return data;
        }

        // Saves the statistics to the file by serializing the DailyStatsData object to JSON format
        private static void SaveStats(DailyStatsData data)
        {
            // Serialize the DailyStatsData object to JSON with indentation
            string json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            // Write the JSON data to the file, overwriting any existing content
            File.WriteAllText(_filePath, json);
        }

        // Private class that represents the structure of the daily statistics data
        private class DailyStatsData
        {
            public DateTime Date { get; set; } = DateTime.MinValue;
            public int Players { get; set; } = 0;
            public int Winners { get; set; } = 0;
            public Dictionary<int, int> GuessDistribution { get; set; } = new();
        }
    }
}