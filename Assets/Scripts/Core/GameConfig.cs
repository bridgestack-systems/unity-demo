using UnityEngine;

namespace NexusArena.Core
{
    [CreateAssetMenu(fileName = "GameConfig", menuName = "NexusArena/Game Config")]
    public class GameConfig : ScriptableObject
    {
        [Header("Player")]
        public float moveSpeed = 8f;
        public float jumpForce = 12f;
        public float lookSensitivity = 2f;
        public float grabRange = 3f;

        [Header("Physics")]
        public float destructionForce = 500f;
        public float projectileSpeed = 40f;

        [Header("Network")]
        public int maxPlayers = 8;
        public int tickRate = 64;

        [Header("Game")]
        public float roundDuration = 300f;
        public float respawnDelay = 3f;
    }
}
