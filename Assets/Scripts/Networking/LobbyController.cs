using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace NexusArena.Networking
{
    public class LobbyController : MonoBehaviour
    {
        [Header("Connection UI")]
        [SerializeField] private TMP_InputField ipAddressInput;
        [SerializeField] private TMP_InputField portInput;
        [SerializeField] private Button hostButton;
        [SerializeField] private Button joinButton;
        [SerializeField] private Button disconnectButton;
        [SerializeField] private TMP_Text statusText;

        [Header("Lobby UI")]
        [SerializeField] private GameObject lobbyPanel;
        [SerializeField] private GameObject connectionPanel;
        [SerializeField] private Transform playerListContainer;
        [SerializeField] private GameObject playerListEntryPrefab;
        [SerializeField] private Button startGameButton;
        [SerializeField] private Toggle readyToggle;
        [SerializeField] private TMP_Text playerCountText;

        [Header("Chat")]
        [SerializeField] private ChatSystem chatSystem;

        private readonly Dictionary<ulong, GameObject> playerListEntries = new();

        private void Awake()
        {
            hostButton?.onClick.AddListener(OnHostClicked);
            joinButton?.onClick.AddListener(OnJoinClicked);
            disconnectButton?.onClick.AddListener(OnDisconnectClicked);
            startGameButton?.onClick.AddListener(OnStartGameClicked);
            readyToggle?.onValueChanged.AddListener(OnReadyToggled);

            ShowConnectionPanel();
        }

        private void OnEnable()
        {
            if (NetworkGameManager.Instance != null)
            {
                NetworkGameManager.Instance.OnPlayerConnected += HandlePlayerConnected;
                NetworkGameManager.Instance.OnPlayerDisconnected += HandlePlayerDisconnected;
            }
        }

        private void OnDisable()
        {
            if (NetworkGameManager.Instance != null)
            {
                NetworkGameManager.Instance.OnPlayerConnected -= HandlePlayerConnected;
                NetworkGameManager.Instance.OnPlayerDisconnected -= HandlePlayerDisconnected;
            }
        }

        private void Update()
        {
            if (startGameButton != null && NetworkManager.Singleton != null)
            {
                bool isHost = NetworkManager.Singleton.IsHost;
                startGameButton.gameObject.SetActive(isHost);
                if (isHost)
                    startGameButton.interactable = AreAllPlayersReady();
            }
        }

        private void OnHostClicked()
        {
            if (NetworkGameManager.Instance == null) return;

            if (NetworkGameManager.Instance.StartHost())
            {
                SetStatus("Hosting...");
                ShowLobbyPanel();
            }
            else
            {
                SetStatus("Failed to start host.");
            }
        }

        private void OnJoinClicked()
        {
            if (NetworkGameManager.Instance == null) return;

            string ip = ipAddressInput != null ? ipAddressInput.text : "127.0.0.1";
            if (string.IsNullOrWhiteSpace(ip)) ip = "127.0.0.1";

            ushort port = 7777;
            if (portInput != null && ushort.TryParse(portInput.text, out ushort parsed))
                port = parsed;

            if (NetworkGameManager.Instance.StartClient(ip, port))
            {
                SetStatus($"Connecting to {ip}:{port}...");
                ShowLobbyPanel();
            }
            else
            {
                SetStatus("Failed to connect.");
            }
        }

        private void OnDisconnectClicked()
        {
            NetworkGameManager.Instance?.Disconnect();
            ClearPlayerList();
            ShowConnectionPanel();
            SetStatus("Disconnected.");
        }

        private void OnStartGameClicked()
        {
            NetworkGameManager.Instance?.StartRound();
        }

        private void OnReadyToggled(bool isReady)
        {
            if (NetworkManager.Singleton == null) return;
            ulong localId = NetworkManager.Singleton.LocalClientId;
            var localSync = GetPlayerSync(localId);
            localSync?.SetReadyRpc(isReady);
        }

        private void HandlePlayerConnected(ulong clientId)
        {
            AddPlayerListEntry(clientId);
            UpdatePlayerCount();
        }

        private void HandlePlayerDisconnected(ulong clientId)
        {
            RemovePlayerListEntry(clientId);
            UpdatePlayerCount();
        }

        private void AddPlayerListEntry(ulong clientId)
        {
            if (playerListEntries.ContainsKey(clientId)) return;

            if (playerListEntryPrefab != null && playerListContainer != null)
            {
                var entry = Instantiate(playerListEntryPrefab, playerListContainer);
                var nameText = entry.GetComponentInChildren<TMP_Text>();
                if (nameText != null)
                    nameText.text = $"Player {clientId}";
                playerListEntries[clientId] = entry;
            }
        }

        private void RemovePlayerListEntry(ulong clientId)
        {
            if (playerListEntries.TryGetValue(clientId, out var entry))
            {
                Destroy(entry);
                playerListEntries.Remove(clientId);
            }
        }

        private void ClearPlayerList()
        {
            foreach (var kvp in playerListEntries)
                Destroy(kvp.Value);
            playerListEntries.Clear();
        }

        public void RefreshPlayerList()
        {
            ClearPlayerList();
            if (NetworkGameManager.Instance == null) return;

            foreach (ulong id in NetworkGameManager.Instance.ConnectedPlayers)
                AddPlayerListEntry(id);
            UpdatePlayerCount();
        }

        public void UpdatePlayerEntryName(ulong clientId, string playerName)
        {
            if (playerListEntries.TryGetValue(clientId, out var entry))
            {
                var nameText = entry.GetComponentInChildren<TMP_Text>();
                if (nameText != null)
                    nameText.text = playerName;
            }
        }

        private void UpdatePlayerCount()
        {
            if (playerCountText != null && NetworkGameManager.Instance != null)
            {
                int count = NetworkGameManager.Instance.ConnectedPlayers.Count;
                playerCountText.text = $"{count}/{NetworkGameManager.Instance.MaxPlayers}";
            }
        }

        private bool AreAllPlayersReady()
        {
            if (NetworkManager.Singleton == null) return false;

            foreach (var kvp in NetworkManager.Singleton.ConnectedClients)
            {
                var sync = kvp.Value.PlayerObject?.GetComponent<PlayerNetworkSync>();
                if (sync != null && !sync.IsReady.Value)
                    return false;
            }
            return NetworkManager.Singleton.ConnectedClients.Count > 0;
        }

        private PlayerNetworkSync GetPlayerSync(ulong clientId)
        {
            if (NetworkManager.Singleton != null &&
                NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
                return client.PlayerObject?.GetComponent<PlayerNetworkSync>();
            return null;
        }

        private void ShowConnectionPanel()
        {
            connectionPanel?.SetActive(true);
            lobbyPanel?.SetActive(false);
        }

        private void ShowLobbyPanel()
        {
            connectionPanel?.SetActive(false);
            lobbyPanel?.SetActive(true);
        }

        private void SetStatus(string message)
        {
            if (statusText != null)
                statusText.text = message;
        }
    }
}
