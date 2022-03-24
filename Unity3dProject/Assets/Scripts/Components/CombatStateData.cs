using UnityEngine;

namespace Hybrid.Components
{
    public enum CombatState
    {
        inactive = 0,
        active = 1,
        searching = 2,
        fleeing = 3,
        alerted = 4
    }
    public enum CombatNavigationState
    {
        chase = 0,
        evade = 1,
        holdPosition = 2,
    }
    public enum CombatMovementType
    {
        pressAttack = 0,
        fallBack = 1,
        holdPosition = 2,
        flankRight = 3,
        flankLeft = 4
    }

    public class CombatStateData : MonoBehaviour
    {
        public bool _shouldAttack = false;
        [SerializeField] private CombatState _combatState = 0;
        public CombatState combatState
        {
            get => _combatState;
            set => _combatState = value;
        }

        [SerializeField] private CombatMovementType _combatMovementType = 0;
        public CombatMovementType combatMovementType
        {
            get => _combatMovementType;
            set => _combatMovementType = value;
        }

        public float fallBackTimeMax = 5f;
        public float fallBackTimeMin = 3f;
        public float advanceTimeMax = 5f;
        public float advanceTimeMin = 3f;
        public float flankTimeMax = 5f;
        public float flankTimeMin = 2f;
        public float holdPositionMin = 5f;
        public float holdPositionMax = 2f;


        public float meleeAttackRange = 1.15f; //TEMP - Move this somewere else
        public float currentVelocityZ = 0f; //TEMP - Move this somewere else
        public float currentVelocityX = 0f; //TEMP - Move this somewere else
        public float currentSpeed = 0f; //TEMP - Move this somewere else
        public bool bShouldBlock = false;


        [SerializeField] private float _movementUpdateTimer = 1f;
        public float movementUpdateTimer
        {
            get => _movementUpdateTimer;
            set => _movementUpdateTimer = value;
        }

        public bool IsInCombat() => combatState == CombatState.active || combatState == CombatState.searching;
        public bool IsAdvancing() => combatMovementType == CombatMovementType.pressAttack;
        public bool IsFallingBack() => combatMovementType == CombatMovementType.fallBack;
        public bool IsFlanking() => combatMovementType == CombatMovementType.flankLeft || combatMovementType == CombatMovementType.flankLeft;
        public bool IsFleeing() => combatState == CombatState.fleeing;
        public bool ShouldAttack() => IsInCombat();

        public void UpdateMovementTimer(CombatMovementType movementType)
        {
            switch (movementType)
            {
                case CombatMovementType.fallBack:
                    movementUpdateTimer = UnityEngine.Random.Range(fallBackTimeMin, fallBackTimeMax);
                    break;
                case CombatMovementType.pressAttack:
                    movementUpdateTimer = UnityEngine.Random.Range(advanceTimeMin, advanceTimeMax);
                    break;
                case CombatMovementType.holdPosition:
                    movementUpdateTimer = UnityEngine.Random.Range(holdPositionMin, holdPositionMax);
                    break;
                default:
                    movementUpdateTimer = UnityEngine.Random.Range(flankTimeMin, flankTimeMax);
                    break;
            }
        }

    }
}