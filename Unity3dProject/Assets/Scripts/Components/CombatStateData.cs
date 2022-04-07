using System;
using System.Collections;
using System.Collections.Generic;
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
    public enum FleeState
    {
        defensive = 0,
        panic = 1,
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

    public enum CombatMovementBehaviorType
    {
        melee = 0,
        shooter = 1,
        archer = 2,
        caster = 3
    }


    public struct CombatMovementBehavior
    {
        public float fallBackTimeMax { get; set; }
        public float fallBackTimeMin { get; set; }
        public float advanceTimeMax { get; set; }
        public float advanceTimeMin { get; set; }
        public float flankTimeMax { get; set; }
        public float flankTimeMin { get; set; }
        public float holdPositionMin { get; set; }
        public float holdPositionMax { get; set; }
        public float distanceBufferRangeMultMax { get; set; }
        public float distanceBufferRangeMultMin { get; set; }
        public CombatMovementBehavior(float[] _advanceTime, float[] _fallBackTime, float[] _circleTime, float[] _holdPositionTime, float[] _distanceBufferRangeMult)
        {
            advanceTimeMin = _advanceTime[0];
            advanceTimeMax = _advanceTime[1];
            fallBackTimeMin = _fallBackTime[0];
            fallBackTimeMax = _fallBackTime[1];
            flankTimeMin = _circleTime[0];
            flankTimeMax = _circleTime[1];
            holdPositionMax = _holdPositionTime[0];
            holdPositionMin = _holdPositionTime[1];
            distanceBufferRangeMultMin = _distanceBufferRangeMult[0];
            distanceBufferRangeMultMax = _distanceBufferRangeMult[1];
        }
    }



    public class CombatStateData : MonoBehaviour
    {
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
        [SerializeField] private CombatMovementBehaviorType _combatMovementBehaviorType = 0;
        public CombatMovementBehaviorType combatMovementBehaviorType
        {
            get => _combatMovementBehaviorType;
            set => _combatMovementBehaviorType = value;
        }
        public bool _shouldAttack = false;

        public Hashtable movementBehaviors;

        public float fallBackTimeMax = 2f;
        public float fallBackTimeMin = 0.8f;
        public float advanceTimeMax = 2f;
        public float advanceTimeMin = 0.8f;
        public float flankTimeMax = 2f;
        public float flankTimeMin = 0.8f;
        public float holdPositionMin = 3f;
        public float holdPositionMax = 0.8f;



        public float keepDistanceMin = 1f;
        public float distanceBufferRangeMult = 1.9f;
        public float distanceBufferRangeMultMin = 1.3f;
        public float distanceBufferRangeMultMax = 4.8f;

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

        public float alertTime = 26f;
        [SerializeField] private float _alertedTimer = 26f;
        public float alertedTimer
        {
            get => _alertedTimer;
            set => _alertedTimer = value;
        }
        public void UpdateTimer_Alerted() => alertedTimer -= 1f;
        public void ResetTimer_Alerted() => alertedTimer = alertTime;

        public void SetAlerted()
        {
            ResetTimer_Alerted();
            combatState = CombatState.alerted;
        }


        public bool IsInCombat() => combatState == CombatState.active || combatState == CombatState.searching;
        public bool IsAdvancing() => combatMovementType == CombatMovementType.pressAttack;
        public bool IsFallingBack() => combatMovementType == CombatMovementType.fallBack;
        public bool IsFlanking() => combatMovementType == CombatMovementType.flankLeft || combatMovementType == CombatMovementType.flankLeft;
        public bool IsFleeing() => combatState == CombatState.fleeing;
        public bool IsAlerted() => combatState == CombatState.alerted;
        public bool CanBeAlerted() => IsAlerted() || combatState == CombatState.inactive;
        public bool ShouldAttack() => IsInCombat();

        public void UpdateMovementTimer(CombatMovementType movementType, float timeerOverride = -1f)
        {
            switch (movementType)
            {
                case CombatMovementType.fallBack:
                    movementUpdateTimer = UnityEngine.Random.Range(fallBackTimeMin, timeerOverride == -1f ? fallBackTimeMax : timeerOverride);
                    break;
                case CombatMovementType.pressAttack:
                    movementUpdateTimer = UnityEngine.Random.Range(advanceTimeMin, timeerOverride == -1f ? advanceTimeMax : timeerOverride);
                    break;
                case CombatMovementType.holdPosition:
                    movementUpdateTimer = UnityEngine.Random.Range(holdPositionMin, timeerOverride == -1f ? holdPositionMax : timeerOverride);
                    break;
                default:
                    movementUpdateTimer = UnityEngine.Random.Range(flankTimeMin, timeerOverride == -1f ? flankTimeMax : timeerOverride);
                    break;
            }
        }


        public void UpdateCombatMovmentBehavior(CombatMovementBehaviorType newCombatMovementBehaviorType, CombatMovementBehavior movementBehavior)
        {
            combatMovementBehaviorType = newCombatMovementBehaviorType;
            advanceTimeMin = movementBehavior.advanceTimeMin;
            advanceTimeMax = movementBehavior.advanceTimeMax;
            fallBackTimeMin = movementBehavior.fallBackTimeMin;
            fallBackTimeMax = movementBehavior.fallBackTimeMax;
            flankTimeMin = movementBehavior.flankTimeMin;
            flankTimeMax = movementBehavior.flankTimeMax;
            holdPositionMin = movementBehavior.holdPositionMin;
            holdPositionMax = movementBehavior.holdPositionMax;
            distanceBufferRangeMultMin = movementBehavior.distanceBufferRangeMultMin;
            distanceBufferRangeMultMax = movementBehavior.distanceBufferRangeMultMax;
        }


        public void EvaluateCombatState(bool targetsLost)
        {
            if (targetsLost && combatState != CombatState.fleeing)
            {
                if (IsInCombat()) SetAlerted();
            }
        }

        private void Awake()
        {

            if (movementBehaviors == null)
            {
                movementBehaviors = new Hashtable();

                CombatMovementBehavior meleeMovment = new CombatMovementBehavior(
                    new float[2] { 0.8f, 2f }, // advance
                    new float[2] { 0.8f, 2f }, // fallback
                    new float[2] { 0.8f, 2f }, // circle
                    new float[2] { 0.8f, 2f }, // holdPosition
                    new float[2] { 1.8f, 4.6f }  // distanceBufferRangeMult
                        );

                movementBehaviors.Add(CombatMovementBehaviorType.melee, meleeMovment);

                UpdateCombatMovmentBehavior(CombatMovementBehaviorType.melee, (CombatMovementBehavior)movementBehaviors[CombatMovementBehaviorType.melee]);

                // var values = Enum.GetValues(typeof(CombatMovementBehaviorType));
                // foreach (var item in values)
                // {
                //     movementBehaviors.Add(item, meleeMovment);
                // }
            }

        }

        ActorEventManger actorEventManger;
        Targeting targeting;
        private void Start()
        {
            actorEventManger = GetComponent<ActorEventManger>();
            targeting = GetComponent<Targeting>();
            GlobalEventManager.onLocationalActionBroadcast += onLocationalActionBroadcast;
        }
        private void OnDestroy()
        {
            GlobalEventManager.onLocationalActionBroadcast -= onLocationalActionBroadcast;
        }

        private void onLocationalActionBroadcast(Vector3 position, float radius)
        {
            float distance = Vector3.Distance(this.gameObject.transform.position, position);
            if (distance < radius)
            {
                if (CanBeAlerted())
                {
                    Debug.Log("onLocationalActionBroadcast => Alerted!");

                    SetAlerted();
                    targeting.alertedPosition = position;
                    actorEventManger.AlertSearchLocationUpdate(position);
                }

            }
        }

    }
}