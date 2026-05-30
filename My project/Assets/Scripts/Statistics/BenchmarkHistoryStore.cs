using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Statistics
{
    /// <summary>
    /// Manages persistence of benchmark history to JSON file.
    /// </summary>
    public class BenchmarkHistoryStore
    {
        private readonly string historyFilePath;

        public BenchmarkHistoryStore(string fileName = "benchmark_history_v2.json")
        {
            string persistentPath = Application.persistentDataPath;
            historyFilePath = Path.Combine(persistentPath, fileName);
        }

        /// <summary>
        /// Loads all stored benchmark history entries.
        /// </summary>
        public List<BenchmarkHistoryEntry> LoadHistory()
        {
            if (!File.Exists(historyFilePath))
            {
                return new List<BenchmarkHistoryEntry>();
            }

            try
            {
                string json = File.ReadAllText(historyFilePath);
                BenchmarkHistoryData data = JsonUtility.FromJson<BenchmarkHistoryData>(json);
                
                if (data == null || data.entries == null)
                {
                    return new List<BenchmarkHistoryEntry>();
                }

                return new List<BenchmarkHistoryEntry>(data.entries);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to load benchmark history: {ex.Message}");
                return new List<BenchmarkHistoryEntry>();
            }
        }

        /// <summary>
        /// Saves benchmark history entries to file.
        /// </summary>
        public void SaveHistory(List<BenchmarkHistoryEntry> entries)
        {
            try
            {
                BenchmarkHistoryData data = new BenchmarkHistoryData(entries.ToArray());
                string json = JsonUtility.ToJson(data, true);
                File.WriteAllText(historyFilePath, json);
                Debug.Log($"Benchmark history saved to {historyFilePath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to save benchmark history: {ex.Message}");
            }
        }

        /// <summary>
        /// Appends new benchmark results to the history.
        /// </summary>
        public void AppendResults(
            string testId,
            IReadOnlyList<Algorytm.Dane.AlgorithmMetrics> allMetrics)
        {
            if (allMetrics == null || allMetrics.Count == 0)
            {
                return;
            }

            List<BenchmarkHistoryEntry> history = LoadHistory();

            for (int i = 0; i < allMetrics.Count; i++)
            {
                BenchmarkHistoryEntry entry = BenchmarkHistoryEntry.FromMetrics(
                    testId,
                    allMetrics[i].runIndex,
                    allMetrics[i]);
                if (entry != null)
                {
                    history.Add(entry);
                }
            }

            SaveHistory(history);
        }

        /// <summary>
        /// Clears all benchmark history.
        /// </summary>
        public void ClearHistory()
        {
            try
            {
                if (File.Exists(historyFilePath))
                {
                    File.Delete(historyFilePath);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to clear benchmark history: {ex.Message}");
            }
        }

        public string GetHistoryFilePath()
        {
            return historyFilePath;
        }
    }
}
