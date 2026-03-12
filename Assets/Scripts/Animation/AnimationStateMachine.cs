using UnityEngine;
using UnityEngine.Events;

namespace NexusArena.Animation
{
    public class AnimationStateMachine : StateMachineBehaviour
    {
        [Header("State Events")]
        [SerializeField] private string stateName = "";
        [SerializeField] private bool logTransitions;

        [Header("Callbacks")]
        [SerializeField] private AnimationStateEvents stateEvents;

        private bool _hasEntered;

        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            _hasEntered = true;

            if (logTransitions)
                Debug.Log($"[AnimSM] {animator.gameObject.name} entered state: {stateName}");

            var receiver = animator.GetComponent<AnimationStateEventReceiver>();
            if (receiver != null)
                receiver.OnStateEntered(stateName, stateInfo);

            stateEvents?.onEnter?.Invoke();
        }

        public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            if (!_hasEntered)
                return;

            _hasEntered = false;

            if (logTransitions)
                Debug.Log($"[AnimSM] {animator.gameObject.name} exited state: {stateName}");

            var receiver = animator.GetComponent<AnimationStateEventReceiver>();
            if (receiver != null)
                receiver.OnStateExited(stateName, stateInfo);

            stateEvents?.onExit?.Invoke();
        }

        public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            stateEvents?.onUpdate?.Invoke();
        }
    }

    [System.Serializable]
    public class AnimationStateEvents
    {
        public UnityEvent onEnter;
        public UnityEvent onExit;
        public UnityEvent onUpdate;
    }

    public class AnimationStateEventReceiver : MonoBehaviour
    {
        [SerializeField] private StateEventMapping[] stateMappings = System.Array.Empty<StateEventMapping>();

        private System.Collections.Generic.Dictionary<string, StateEventMapping> _mappingLookup;

        private void Awake()
        {
            _mappingLookup = new System.Collections.Generic.Dictionary<string, StateEventMapping>(stateMappings.Length);
            foreach (var mapping in stateMappings)
            {
                if (!string.IsNullOrEmpty(mapping.stateName))
                    _mappingLookup[mapping.stateName] = mapping;
            }
        }

        public void OnStateEntered(string stateName, AnimatorStateInfo stateInfo)
        {
            if (_mappingLookup.TryGetValue(stateName, out var mapping))
                mapping.onEnter?.Invoke();
        }

        public void OnStateExited(string stateName, AnimatorStateInfo stateInfo)
        {
            if (_mappingLookup.TryGetValue(stateName, out var mapping))
                mapping.onExit?.Invoke();
        }
    }

    [System.Serializable]
    public class StateEventMapping
    {
        public string stateName;
        public UnityEvent onEnter;
        public UnityEvent onExit;
    }
}
