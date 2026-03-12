using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace NexusArena.Networking
{
    public class ChatSystem : NetworkBehaviour
    {
        [Header("UI References")]
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private Transform messageContainer;
        [SerializeField] private GameObject messagePrefab;
        [SerializeField] private TMP_InputField chatInput;
        [SerializeField] private Button sendButton;

        [Header("Settings")]
        [SerializeField] private int maxMessages = 50;
        [SerializeField] private Color systemMessageColor = new(0.7f, 0.7f, 0.7f, 1f);
        [SerializeField] private Color[] playerColors = {
            new(0.3f, 0.8f, 1f), new(1f, 0.5f, 0.3f),
            new(0.5f, 1f, 0.5f), new(1f, 0.8f, 0.3f),
            new(0.8f, 0.5f, 1f), new(1f, 0.4f, 0.6f),
            new(0.4f, 1f, 0.8f), new(1f, 1f, 0.4f)
        };

        private readonly List<ChatMessage> messageHistory = new();
        private readonly List<GameObject> messageObjects = new();

        private void Awake()
        {
            sendButton?.onClick.AddListener(SendCurrentMessage);
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
            if (chatInput != null && chatInput.isFocused &&
                (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)))
            {
                SendCurrentMessage();
            }
        }

        private void SendCurrentMessage()
        {
            if (chatInput == null || string.IsNullOrWhiteSpace(chatInput.text)) return;

            string message = chatInput.text.Trim();
            chatInput.text = string.Empty;
            chatInput.ActivateInputField();

            string senderName = GetLocalPlayerName();
            ulong senderId = NetworkManager.Singleton?.LocalClientId ?? 0;

            SendChatMessageRpc(senderId, senderName, message);
        }

        [Rpc(SendTo.Server)]
        private void SendChatMessageRpc(ulong senderId, string senderName, string message, RpcParams rpcParams = default)
        {
            ReceiveChatMessageRpc(senderId, senderName, message, Time.time);
        }

        [Rpc(SendTo.Everyone)]
        private void ReceiveChatMessageRpc(ulong senderId, string senderName, string message, float timestamp)
        {
            var chatMessage = new ChatMessage
            {
                senderId = senderId,
                senderName = senderName,
                message = message,
                timestamp = timestamp,
                isSystem = false
            };

            AddMessage(chatMessage);
        }

        public void AddSystemMessage(string message)
        {
            var chatMessage = new ChatMessage
            {
                senderId = 0,
                senderName = "System",
                message = message,
                timestamp = Time.time,
                isSystem = true
            };

            AddMessage(chatMessage);
        }

        private void AddMessage(ChatMessage chatMessage)
        {
            messageHistory.Add(chatMessage);

            while (messageHistory.Count > maxMessages)
            {
                messageHistory.RemoveAt(0);
                if (messageObjects.Count > 0)
                {
                    Destroy(messageObjects[0]);
                    messageObjects.RemoveAt(0);
                }
            }

            CreateMessageUI(chatMessage);
            ScrollToBottom();
        }

        private void CreateMessageUI(ChatMessage chatMessage)
        {
            if (messagePrefab == null || messageContainer == null) return;

            var msgObj = Instantiate(messagePrefab, messageContainer);
            var text = msgObj.GetComponentInChildren<TMP_Text>();
            if (text == null) return;

            Color color = chatMessage.isSystem
                ? systemMessageColor
                : GetPlayerColor(chatMessage.senderId);

            string colorHex = ColorUtility.ToHtmlStringRGB(color);
            string timeStr = FormatTimestamp(chatMessage.timestamp);

            text.text = chatMessage.isSystem
                ? $"<color=#{colorHex}>[{timeStr}] {chatMessage.message}</color>"
                : $"<color=#{colorHex}>[{timeStr}] {chatMessage.senderName}:</color> {chatMessage.message}";

            messageObjects.Add(msgObj);
        }

        private void ScrollToBottom()
        {
            if (scrollRect != null)
            {
                Canvas.ForceUpdateCanvases();
                scrollRect.verticalNormalizedPosition = 0f;
            }
        }

        private Color GetPlayerColor(ulong clientId)
        {
            int index = (int)(clientId % (ulong)playerColors.Length);
            return playerColors[index];
        }

        private string GetLocalPlayerName()
        {
            if (NetworkManager.Singleton == null) return "Unknown";
            ulong localId = NetworkManager.Singleton.LocalClientId;

            if (NetworkManager.Singleton.ConnectedClients.TryGetValue(localId, out var client))
            {
                var sync = client.PlayerObject?.GetComponent<PlayerNetworkSync>();
                if (sync != null)
                {
                    string name = sync.PlayerName.Value.ToString();
                    if (!string.IsNullOrEmpty(name)) return name;
                }
            }
            return $"Player {localId}";
        }

        private static string FormatTimestamp(float gameTime)
        {
            int minutes = Mathf.FloorToInt(gameTime / 60f);
            int seconds = Mathf.FloorToInt(gameTime % 60f);
            return $"{minutes:00}:{seconds:00}";
        }

        private void HandlePlayerConnected(ulong clientId) =>
            AddSystemMessage($"Player {clientId} joined.");

        private void HandlePlayerDisconnected(ulong clientId) =>
            AddSystemMessage($"Player {clientId} left.");

        public void ClearChat()
        {
            messageHistory.Clear();
            foreach (var obj in messageObjects)
                Destroy(obj);
            messageObjects.Clear();
        }
    }

    [Serializable]
    public struct ChatMessage
    {
        public ulong senderId;
        public string senderName;
        public string message;
        public float timestamp;
        public bool isSystem;
    }
}
