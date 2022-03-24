using UnityEngine;

namespace Hybrid.Components
{
    [RequireComponent(
        typeof(Target)
        , typeof(Targeting)
        , typeof(QuadrantSearchable)
        )]

    [RequireComponent(
         typeof(ActorFOV)
        , typeof(ActorFactions)
        , typeof(ActorNavigationData)
        )]

    [RequireComponent(
         typeof(DetectionStateData),
         typeof(ActorAIStateData),
         typeof(CombatStateData)
        )]

    [RequireComponent(
         typeof(AIBaseBehaviors),
         typeof(ActorHealth)
        )]

    public class IsActor : MonoBehaviour
    {
        public string refId = "-";
        public enum ActorType
        {
            NPC = 0,
            Animal,
        }
    }
}





