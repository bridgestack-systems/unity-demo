using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using NexusArena.Core;

namespace NexusArena.Networking
{
    public class NetworkGameManager : NetworkBehaviour
    {
        public static NetworkGameManager Instance { get; private set; }

        [Header("Configuration")]
        [SerializeField] private int maxPlayers = 8;
        [SerializeField] private string serverPassword = "";

        public int MaxPlayers => maxPlayers;

        private NetworkVariable<GameState> networkGameState = new(
            GameState.MainMenu,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        private NetworkList<ulong> connectedPlayerIds;

        public event Action<ulong> OnPlayerConnected;
        public event Action<ulong> OnPlayerDisconnected;
        public event Action<GameState> OnNetworkGameStateChanged;
        public event Action<ulong, int> OnScoreUpdated;
        public event Action OnRoundStarted;
        public event Action OnRoundEnded;

        public GameState CurrentGameState => networkGameState.Value;
        public IReadOnlyList<ulong> ConnectedPlayers
        {
            get
            {
                var list = new List<ulong>();
                if (connectedPlayerIds != null)
                {
                    foreach (var id in connectedPlayerIds)
                        list.Add(id);
                }
                return list;
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            connectedPlayerIds = new NetworkList<ulong>();
        }

        private void OnEnable()
        {
            networkGameState.OnValueChanged += HandleGameStateChanged;
        }

        private void OnDisable()
        {
            networkGameState.OnValueChanged -= HandleGameStateChanged;
        }

        public bool StartHost()
        {
            ConfigureTransport();
            NetworkManager.Singleton.ConnectionApprovalCallback = ApprovalCallback;
            NetworkManager.Singleton.OnClientConnectedCallback += HandleClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += HandleClientDisconnected;
            return NetworkManager.Singleton.StartHost();
        }

        public bool StartClient(string ipAddress = "127.0.0.1", ushort port = 7777)
        {
            ConfigureTransport(ipAddress, port);
            var payload = new ConnectionPayload { password = serverPassword };
            NetworkManager.Singleton.NetworkConfig.ConnectionData =
                System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(payload));
            return NetworkManager.Singleton.StartClient();
        }

        public bool StartServer()
        {
            ConfigureTransport();
            NetworkManager.Singleton.ConnectionApprovalCallback = ApprovalCallback;
            NetworkManager.Singleton.OnClientConnectedCallback += HandleClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += HandleClientDisconnected;
            return NetworkManager.Singleton.StartServer();
        }

        public void Disconnect()
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            {
                NetworkManager.Singleton.OnClientConnectedCallback -= HandleClientConnected;
                NetworkManager.Singleton.OnClientDisconnectCallback -= HandleClientDisconnected;
                NetworkManager.Singleton.Shutdown();
            }
        }

        private void ConfigureTransport(string ip = "127.0.0.1", ushort port = 7777)
        {
            if (NetworkManager.Singleton.NetworkConfig.NetworkTransport is Unity.Netcode.Transports.UTP.UnityTransport utp)
            {
                utp.ConnectionData.Address = ip;
                utp.ConnectionData.Port = port;
            }
        }

        private void ApprovalCallback(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
        {
            int currentCount = NetworkManager.Singleton.ConnectedClientsIds.Count;
            if (currentCount >= maxPlayers)
            {
                response.Approved = false;
                response.Reason = "Server is full.";
                return;
            }

            if (!string.IsNullOrEmpty(serverPassword))
            {
                string json = System.Text.Encoding.UTF8.GetString(request.Payload);
                var payload = JsonUtility.FromJson<ConnectionPayload>(json);
                if (payload?.password != serverPassword)
                {
                    response.Approved = false;
                    response.Reason = "Invalid password.";
                    return;
                }
            }

            response.Approved = true;
            response.CreatePlayerObject = true;
        }

        private void HandleClientConnected(ulong clientId)
        {
            if (IsServer)
            {
                connectedPlayerIds.Add(clientId);
            }
            OnPlayerConnected?.Invoke(clientId);
        }

        private void HandleClientDisconnected(ulong clientId)
        {
            if (IsServer)
            {
                connectedPlayerIds.Remove(clientId);
            }
            OnPlayerDisconnected?.Invoke(clientId);
        }

        private void HandleGameStateChanged(GameState previous, GameState current)
        {
            OnNetworkGameStateChanged?.Invoke(current);
            GameManager.Instance?.SetState(current);
        }

        public void SetGameState(GameState newState)
        {
            if (!IsServer) return;
            networkGameState.Value = newState;
        }

        [Rpc(SendTo.Everyone)]
        public void BroadcastScoreUpdateRpc(ulong playerId, int newScore)
        {
            OnScoreUpdated?.Invoke(playerId, newScore);
        }

        [Rpc(SendTo.Everyone)]
        public void BroadcastRoundStartRpc()
        {
            OnRoundStarted?.Invoke();
        }

        [Rpc(SendTo.Everyone)]
        public void BroadcastRoundEndRpc()
        {
            OnRoundEnded?.Invoke();
        }

        [Rpc(SendTo.Server)]
        public void RequestScoreUpdateRpc(ulong playerId, int scoreChange, RpcParams rpcParams = default)
        {
            var syncComponent = GetPlayerSync(playerId);
            if (syncComponent != null)
            {
                int newScore = syncComponent.Score.Value + scoreChange;
                syncComponent.SetScore(newScore);
                BroadcastScoreUpdateRpc(playerId, newScore);
            }
        }

        public void StartRound()
        {
            if (!IsServer) return;
            SetGameState(GameState.Playing);
            BroadcastRoundStartRpc();
        }

        public void EndRound()
        {
            if (!IsServer) return;
            SetGameState(GameState.GameOver);
            BroadcastRoundEndRpc();
        }

        private PlayerNetworkSync GetPlayerSync(ulong clientId)
        {
            if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
                return client.PlayerObject?.GetComponent<PlayerNetworkSync>();
            return null;
        }

        public override void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
            base.OnDestroy();
        }

        [Serializable]
        private class ConnectionPayload
        {
            public string password;
        }
    }
}
