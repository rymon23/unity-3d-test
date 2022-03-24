using UnityEngine;


namespace Hybrid.Components {

    public enum AIState
    {
        idle = 0,
        wander = 1,
        combat = 2,
        alert = 3,
    }

    public class ActorAIStateData : MonoBehaviour
    {
        [SerializeField] private AIState _aiState = 0;
        public AIState aiState {
            get => _aiState;
            set => _aiState = value;
        }
    }
}