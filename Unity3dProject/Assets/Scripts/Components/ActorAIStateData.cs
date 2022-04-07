using UnityEngine;


namespace Hybrid.Components {
    public enum AIState_Behavior
    {
        idle = 0,
        combat = 2,
        // alert = 3,
    }

    public class ActorAIStateData : MonoBehaviour
    {
        [SerializeField] private AIState_Behavior _aiBehaviorState = 0;
        public AIState_Behavior aiBehaviorState {
            get => _aiBehaviorState;
            set => _aiBehaviorState = value;
        }
    }
}