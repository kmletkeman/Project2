/*
 Programmer: Kevin Letkeman
 Purpose: Manages the list of valid words and provides functionality to get the daily word 
          and validate guesses, communicates to DailyWordService through gRPC
 Date: 2026-03-31
 */

using Grpc.Core;
using GrpcWordServer.Protos;
using System.Text.Json;

namespace GrpcWordServer.Services
{
    public class WordService
    {
        // List to hold the valid words loaded from the JSON file
        private readonly List<string> _words;

        // Constructor loads words from the JSON file and adds them to the _words list
        public WordService()
        {
            // Path to the JSON file
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wordle.json");
            // Load words from the JSON file in the given path
            string json = File.ReadAllText(path);

            // Deserialize the JSON into a list of strings, converted to lowercase
            _words = JsonSerializer.Deserialize<List<string>>(json)?
                .Select(w => w.ToLower()).ToList() ?? new List<string>();

            // If words list is empty throws an exception
            if (_words.Count == 0)
                throw new Exception("wordle.json is empty or invalid.");
        }

        // Determines the daily word based on the current date, ensuring it changes daily
        public string GetDailyWord()
        {
            var today = DateTime.UtcNow.Date;

            int seed = today.Year * 10000 + today.Month * 100 + today.Day;
            var random = new Random(seed);

            return _words[random.Next(_words.Count)];
        }

        // Checks if a given word is in the list of words loaded from the JSON file
        public bool IsValidWord(string word)
        {
            if (string.IsNullOrWhiteSpace(word) || word.Length != 5)
                return false;

            return _words.Contains(word.ToLower().Trim());
        }
    }
}
