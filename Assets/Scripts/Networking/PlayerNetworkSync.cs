using System;
using Unity.Netcode;
using UnityEngine;

namespace NexusArena.Networking
{
    public class PlayerNetworkSync : NetworkBehaviour
    {
        [Header("Sync Settings")]
        [SerializeField] private float positionLerpSpeed = 15f;
        [SerializeField] private float rotationLerpSpeed = 15f;
        [SerializeField] private float positionThreshold = 0.01f;
        [SerializeField] private float rotationThreshold = 1f;

        [Header("Components")]
        [SerializeField] private Camera playerCamera;
        [SerializeField] private Animator animator;
        [SerializeField] private MonoBehaviour[] ownerOnlyComponents;

        public NetworkVariable<FixedPlayerName> PlayerName { get; } = new(
            default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner
        );

        public NetworkVariable<int> Score { get; } = new(
            0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server
        );

        public NetworkVariable<float> Health { get; } = new(
            100f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server
        );

        public NetworkVariable<bool> IsReady { get; } = new(
            false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server
        );

        private NetworkVariable<Vector3> networkPosition = new(
            default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner
        );

        private NetworkVariable<Quaternion> networkRotation = new(
            default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner
        );

        private NetworkVariable<int> networkAnimHash = new(
            0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner
        );

        public event Action<int> OnScoreChanged;
        public event Action<float> OnHealthChanged;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            Score.OnValueChanged += (_, val) => OnScoreChanged?.Invoke(val);
            Health.OnValueChanged += (_, val) => OnHealthChanged?.Invoke(val);

            if (IsOwner)
            {
                SetupLocalPlayer();
                networkPosition.Value = transform.position;
                networkRotation.Value = transform.rotation;
            }
            else
            {
                SetupRemotePlayer();
            }
        }

        private void SetupLocalPlayer()
        {
            if (playerCamera != null)
                playerCamera.enabled = true;

            if (ownerOnlyComponents != null)
            {
                foreach (var comp in ownerOnlyComponents)
                {
                    if (comp != null) comp.enabled = true;
                }
            }

            var listener = GetComponentInChildren<AudioListener>(true);
            if (listener != null)
                listener.enabled = true;
        }

        private void SetupRemotePlayer()
        {
            if (playerCamera != null)
                playerCamera.enabled = false;

            if (ownerOnlyComponents != null)
            {
                foreach (var comp in ownerOnlyComponents)
                {
                    if (comp != null) comp.enabled = false;
                }
            }

            var listener = GetComponentInChildren<AudioListener>(true);
            if (listener != null)
                listener.enabled = false;
        }

        private void Update()
        {
            if (!IsSpawned) return;

            if (IsOwner)
            {
                UpdateOwnerState();
            }
            else
            {
                InterpolateRemoteState();
            }
        }

        private void UpdateOwnerState()
        {
            Vector3 pos = transform.position;
            if (Vector3.Distance(pos, networkPosition.Value) > positionThreshold)
                networkPosition.Value = pos;

            Quaternion rot = transform.rotation;
            if (Quaternion.Angle(rot, networkRotation.Value) > rotationThreshold)
                networkRotation.Value = rot;
        }

        private void InterpolateRemoteState()
        {
            float dt = Time.deltaTime;
            transform.position = Vector3.Lerp(transform.position, networkPosition.Value, dt * positionLerpSpeed);
            transform.rotation = Quaternion.Slerp(transform.rotation, networkRotation.Value, dt * rotationLerpSpeed);

            if (animator != null && networkAnimHash.Value != 0)
            {
                animator.CrossFadeInFixedTime(networkAnimHash.Value, 0.15f);
            }
        }

        public void PlayAnimation(int stateHash)
        {
            if (!IsOwner) return;
            networkAnimHash.Value = stateHash;
            animator?.CrossFadeInFixedTime(stateHash, 0.15f);
        }

        public void SetScore(int value)
        {
            if (!IsServer) return;
            Score.Value = value;
        }

        public void SetHealth(float value)
        {
            if (!IsServer) return;
            Health.Value = Mathf.Clamp(value, 0f, 100f);
        }

        [Rpc(SendTo.Server)]
        public void SetReadyRpc(bool ready)
        {
            IsReady.Value = ready;
        }

        [Rpc(SendTo.Server)]
        public void RequestDamageRpc(float amount, RpcParams rpcParams = default)
        {
            float newHealth = Health.Value - amount;
            SetHealth(newHealth);

            if (newHealth <= 0f)
                HandleDeathRpc();
        }

        [Rpc(SendTo.Everyone)]
        private void HandleDeathRpc()
        {
            if (IsOwner)
            {
                // Owner handles respawn request
            }
            // All clients play death effects
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
        }
    }

    public struct FixedPlayerName : INetworkSerializable, IEquatable<FixedPlayerName>
    {
        private const int MaxLength = 32;
        private byte length;
        private byte[] data;

        private void EnsureData()
        {
            data ??= new byte[MaxLength];
        }

        public override string ToString()
        {
            if (data == null || length == 0) return string.Empty;
            return System.Text.Encoding.UTF8.GetString(data, 0, length);
        }

        public static implicit operator FixedPlayerName(string s)
        {
            var result = new FixedPlayerName();
            result.EnsureData();
            if (string.IsNullOrEmpty(s)) return result;

            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(s);
            int len = Mathf.Min(bytes.Length, MaxLength);
            result.length = (byte)len;
            Array.Copy(bytes, result.data, len);
            return result;
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref length);
            EnsureData();
            for (int i = 0; i < MaxLength; i++)
                serializer.SerializeValue(ref data[i]);
        }

        public bool Equals(FixedPlayerName other) => ToString() == other.ToString();
        public override bool Equals(object obj) => obj is FixedPlayerName other && Equals(other);
        public override int GetHashCode() => ToString().GetHashCode();
    }
}
