using UnityEngine;

namespace NexusArena.Player
{
    public class SimpleBoxController : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 10f;

        private void Update()
        {
            float h = 0f;
            float v = 0f;

            if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A))  h = -1f;
            if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D)) h = 1f;
            if (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W))    v = 1f;
            if (Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S))  v = -1f;

            Vector3 move = new Vector3(h, 0f, v) * (moveSpeed * Time.deltaTime);
            transform.Translate(move, Space.World);
        }
    }
}
