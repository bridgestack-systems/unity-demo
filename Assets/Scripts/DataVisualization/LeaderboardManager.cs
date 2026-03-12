using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;

namespace NexusArena.DataVisualization
{
    public class LeaderboardManager : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private Transform rowContainer;
        [SerializeField] private GameObject rowPrefab;

        [Header("Settings")]
        [SerializeField] private float updateInterval = 2f;
        [SerializeField] private Color localPlayerHighlight = new(0.2f, 0.4f, 0.8f, 0.3f);
        [SerializeField] private Color defaultRowColor = new(0.15f, 0.15f, 0.18f, 0.5f);
        [SerializeField] private float scoreAnimationSpeed = 5f;

        [Header("Offline Mode")]
        [SerializeField] private bool offlineMode;

        private readonly List<LeaderboardEntry> entries = new();
        private readonly List<LeaderboardRow> activeRows = new();
        private readonly Dictionary<string, float> displayedScores = new();
        private float lastUpdateTime;

        private void OnEnable()
        {
            if (NexusArena.Networking.NetworkGameManager.Instance != null)
            {
                NexusArena.Networking.NetworkGameManager.Instance.OnScoreUpdated += HandleScoreUpdated;
                NexusArena.Networking.NetworkGameManager.Instance.OnPlayerConnected += _ => RefreshFromNetwork();
                NexusArena.Networking.NetworkGameManager.Instance.OnPlayerDisconnected += _ => RefreshFromNetwork();
            }
        }

        private void OnDisable()
        {
            if (NexusArena.Networking.NetworkGameManager.Instance != null)
            {
                NexusArena.Networking.NetworkGameManager.Instance.OnScoreUpdated -= HandleScoreUpdated;
            }
        }

        private void Update()
        {
            if (Time.time - lastUpdateTime >= updateInterval)
            {
                lastUpdateTime = Time.time;
                if (!offlineMode && NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
                    RefreshFromNetwork();
            }

            AnimateScores();
        }

        public void AddOrUpdateEntry(string playerName, int score, int kills = 0, int deaths = 0)
        {
            var existing = entries.Find(e => e.playerName == playerName);
            if (existing != null)
            {
                existing.score = score;
                existing.kills = kills;
                existing.deaths = deaths;
            }
            else
            {
                entries.Add(new LeaderboardEntry
                {
                    playerName = playerName,
                    score = score,
                    kills = kills,
                    deaths = deaths
                });
            }

            SortAndRebuild();
        }

        public void RemoveEntry(string playerName)
        {
            entries.RemoveAll(e => e.playerName == playerName);
            displayedScores.Remove(playerName);
            SortAndRebuild();
        }

        public void ClearLeaderboard()
        {
            entries.Clear();
            displayedScores.Clear();
            ClearRows();
        }

        private void RefreshFromNetwork()
        {
            if (NetworkManager.Singleton == null) return;

            entries.Clear();
            foreach (var kvp in NetworkManager.Singleton.ConnectedClients)
            {
                var sync = kvp.Value.PlayerObject?.GetComponent<NexusArena.Networking.PlayerNetworkSync>();
                if (sync == null) continue;

                string name = sync.PlayerName.Value.ToString();
                if (string.IsNullOrEmpty(name)) name = $"Player {kvp.Key}";

                entries.Add(new LeaderboardEntry
                {
                    playerName = name,
                    score = sync.Score.Value,
                    kills = 0,
                    deaths = 0
                });
            }

            SortAndRebuild();
        }

        private void SortAndRebuild()
        {
            entries.Sort((a, b) => b.score.CompareTo(a.score));
            for (int i = 0; i < entries.Count; i++)
                entries[i].rank = i + 1;

            RebuildUI();
        }

        private void RebuildUI()
        {
            ClearRows();
            if (rowContainer == null) return;

            string localPlayerName = GetLocalPlayerName();

            foreach (var entry in entries)
            {
                var row = CreateRow(entry, entry.playerName == localPlayerName);
                activeRows.Add(row);

                if (!displayedScores.ContainsKey(entry.playerName))
                    displayedScores[entry.playerName] = entry.score;
            }
        }

        private LeaderboardRow CreateRow(LeaderboardEntry entry, bool isLocalPlayer)
        {
            var row = new LeaderboardRow();

            if (rowPrefab != null && rowContainer != null)
            {
                row.gameObject = Instantiate(rowPrefab, rowContainer);
                var texts = row.gameObject.GetComponentsInChildren<TMP_Text>();

                // Expected row layout: Rank, Name, Score, K/D
                if (texts.Length >= 1) { row.rankText = texts[0]; row.rankText.text = $"#{entry.rank}"; }
                if (texts.Length >= 2) { row.nameText = texts[1]; row.nameText.text = entry.playerName; }
                if (texts.Length >= 3) { row.scoreText = texts[2]; row.scoreText.text = entry.score.ToString(); }
                if (texts.Length >= 4) { row.kdText = texts[3]; row.kdText.text = $"{entry.kills}/{entry.deaths}"; }

                var bg = row.gameObject.GetComponent<Image>();
                if (bg != null)
                    bg.color = isLocalPlayer ? localPlayerHighlight : defaultRowColor;
            }

            row.entry = entry;
            return row;
        }

        private void AnimateScores()
        {
            foreach (var row in activeRows)
            {
                if (row.scoreText == null || row.entry == null) continue;

                string name = row.entry.playerName;
                displayedScores.TryGetValue(name, out float displayed);
                float target = row.entry.score;

                if (Mathf.Abs(displayed - target) > 0.5f)
                {
                    displayed = Mathf.Lerp(displayed, target, Time.deltaTime * scoreAnimationSpeed);
                    displayedScores[name] = displayed;
                    row.scoreText.text = Mathf.RoundToInt(displayed).ToString();
                }
                else if (Mathf.Abs(displayed - target) > 0.01f)
                {
                    displayedScores[name] = target;
                    row.scoreText.text = ((int)target).ToString();
                }
            }
        }

        private void ClearRows()
        {
            foreach (var row in activeRows)
            {
                if (row.gameObject != null)
                    Destroy(row.gameObject);
            }
            activeRows.Clear();
        }

        private void HandleScoreUpdated(ulong playerId, int newScore)
        {
            if (!offlineMode)
                RefreshFromNetwork();
        }

        private string GetLocalPlayerName()
        {
            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
                return "LocalPlayer";

            ulong localId = NetworkManager.Singleton.LocalClientId;
            if (NetworkManager.Singleton.ConnectedClients.TryGetValue(localId, out var client))
            {
                var sync = client.PlayerObject?.GetComponent<NexusArena.Networking.PlayerNetworkSync>();
                if (sync != null)
                {
                    string name = sync.PlayerName.Value.ToString();
                    if (!string.IsNullOrEmpty(name)) return name;
                }
            }
            return $"Player {localId}";
        }
    }

    [Serializable]
    public class LeaderboardEntry
    {
        public int rank;
        public string playerName;
        public int score;
        public int kills;
        public int deaths;

        public float KDRatio => deaths > 0 ? (float)kills / deaths : kills;
    }

    internal class LeaderboardRow
    {
        public GameObject gameObject;
        public TMP_Text rankText;
        public TMP_Text nameText;
        public TMP_Text scoreText;
        public TMP_Text kdText;
        public LeaderboardEntry entry;
    }
}
