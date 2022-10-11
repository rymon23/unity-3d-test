using UnityEngine;

namespace Hybrid.Components
{

    public enum ActorType
    {
        NPC = 0,
        Animal,
    }

    [RequireComponent(
        typeof(Target),
        typeof(Targeting),
        typeof(QuadrantSearchable)
        )]

    [RequireComponent(
        typeof(ActorFOV),
        typeof(DetectionStateData),
        typeof(CombatStateData)
        )]

    [RequireComponent(
        typeof(ActorSpells),
        typeof(ActorFactions),
        typeof(ActorInventory)
        )]

    [RequireComponent(
        typeof(ActorEventManger),
        typeof(ActorHealth),
        typeof(ActorDeath)
        )]

    [RequireComponent(
         typeof(HitDamageController),
         typeof(EquipSlotController)
        )]

    [RequireComponent(
        typeof(AnimationState),
        typeof(CombatAnimationEvent),
        typeof(AnimationTriggerController)
        )]

    [RequireComponent(
        typeof(ActorAIStateData),
        typeof(AIBaseBehaviors),
        typeof(ActorNavigationData)
        )]

    [RequireComponent(
        typeof(ActorRagdoll),
        typeof(ActorSpellManager),
        typeof(ActorActionController)
        )]


    public class IsActor : MonoBehaviour
    {
        public string refId = "-";
        [SerializeField] private ActorType _actorType;
        public ActorType actorType
        { get => _actorType; }
        [SerializeField] private bool _IsPlayer = false;
        public bool IsPlayer
        { get => _IsPlayer; }
    }
}

