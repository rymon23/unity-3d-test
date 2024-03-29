using System;
using System.Collections;
using System.Collections.Generic;
using Hybrid.Components;
using UnityEngine;
using UnityEngine.AI;
using Unity.Entities;
using Unity.Mathematics;

namespace Hybrid.Systems
{
    public class CombatStateSystem : ComponentSystem
    {
        private float updateTime = 0.2f;

        private float timer;

        void Start()
        {
            timer = updateTime;
        }

        protected override void OnUpdate()
        {
            timer -= Time.DeltaTime;

            if (timer < 0)
            {
                timer = updateTime;

                Entities
                    .WithAll
                    <DetectionStateData,
                        IsActor,
                        Targeting,
                        ActorFOV,
                        ActorHealth
                    >()
                    .ForEach((
                        Entity entity,
                        IsActor actor,
                        DetectionStateData detectionStateData,
                        Targeting targeting,
                        ActorFOV myFOV,
                        CombatStateData combatStateData,
                        ActorHealth actorHealth
                    ) =>
                    {
                        // Check if actor is Dead
                        if (actorHealth.deathState >= DeathState.dying)
                        {
                            return;
                        }

                        // ActorActionController actorActionController =
                        //     actor
                        //         .gameObject
                        //         .GetComponentInChildren<ActorActionController>(
                        //         );
                        // if (actorActionController != null)
                        // {
                        //     Debug.Log("EvaluateCombatStrategy");
                        //     actorActionController.EvaluateCombatStrategy();
                        // }
                        AnimationState animationState =
                            actor
                                .gameObject
                                .GetComponentInChildren<AnimationState>();
                        EquipSlotController equipSlotController =
                            actor
                                .gameObject
                                .GetComponentInChildren<EquipSlotController>();
                        ParryStateController parryStateController =
                            actor
                                .gameObject
                                .GetComponentInChildren<ParryStateController>();
                        Animator animator =
                            actor.gameObject.GetComponentInChildren<Animator>();

                        if (targeting.currentTarget != null)
                        {
                            GameObject enemyGameObject =
                                targeting.currentTarget.gameObject;
                            AnimationState enemyAnimState =
                                enemyGameObject.GetComponent<AnimationState>();
                            CombatStateData enemyCombatStateData =
                                enemyGameObject.GetComponent<CombatStateData>();
                            NavMeshAgent myNavAgent =
                                actor.GetComponent<NavMeshAgent>();
                            AIDecisionController myDecisionController =
                                actor.GetComponent<AIDecisionController>();

                            float distance =
                                Vector3
                                    .Distance(actor.transform.position,
                                    enemyGameObject.transform.position);
                            targeting.targetDistance = distance;

                            Vector3 targetPos =
                                targeting.currentTarget.transform.position +
                                (targeting.currentTarget.transform.up * 1.2f);

                            // bool hasTargetInFOV = UtilityHelpers.IsInFOVScope(myFOV.viewPoint, targetPos, myFOV.maxAngle, myFOV.maxRadius);
                            bool hasTargetInFOV =
                                UtilityHelpers
                                    .IsTargetDetectable(myFOV.viewPoint,
                                    targetPos,
                                    myFOV.maxAngle,
                                    myFOV.maxRadius);

                            UpdateActorCombatMovement (
                                distance,
                                combatStateData,
                                myNavAgent,
                                myDecisionController,
                                animationState
                            );

                            float attackDistance =
                                Vector3
                                    .Distance(targeting.attackPos,
                                    enemyGameObject.transform.position);
                            targeting.targetAttackDistance = attackDistance;

                            ActorSpells actorSpells =
                                actor.GetComponent<ActorSpells>();
                            if (
                                actorSpells != null &&
                                actorSpells.spellsTemp != null &&
                                actorSpells.spellsTemp.Length > 0
                            )
                            {
                                if (
                                    attackDistance > 1.6f &&
                                    !animationState.isCasting &&
                                    actorHealth.magic >=
                                    actorSpells.spellsTemp[0].baseMagicCost
                                )
                                {
                                    animator.SetTrigger("tCastSpell");
                                    return;
                                }
                            }

                            // Rifle Bash
                            if (
                                combatStateData.combatMovementBehaviorType ==
                                CombatMovementBehaviorType.shooter &&
                                hasTargetInFOV &&
                                animationState.IsAbleToAttack()
                            )
                            {
                                if (attackDistance < 1.2f)
                                {
                                    animator.SetTrigger("tBash");
                                    return;
                                }
                                else
                                {
                                    // Weapon weapon = equipSlotController.handEquipSlots[0].weapon;
                                    // if (weapon != null && UtilityHelpers.HasShotLinedUp(weapon.firePoint, targetPos) && UtilityHelpers.HasLOS(weapon.firePoint, targetPos))
                                    // {
                                    //     weapon.bShouldFire = true;
                                    //     return;
                                    // }
                                }
                            }

                            if (
                                hasTargetInFOV &&
                                attackDistance <
                                combatStateData.meleeAttackRange &&
                                distance > 0.01f &&
                                !animationState.isAttacking
                            )
                            // if (hasTargetInFOV && distance < 2 && distance > 0.01f && !animationState.isAttacking)
                            {
                                if (
                                    enemyAnimState.isAttacking &&
                                    enemyAnimState.attackAnimationState <=
                                    AttackAnimationState.attackPreHit
                                )
                                {
                                    if (
                                        !animationState.isBlocking &&
                                        !animationState.isDodging &&
                                        !animationState.IsInAttackHitFame()
                                    )
                                    {
                                        // if (UnityEngine.Random.Range(0, 100) < 80)
                                        // {
                                        //     int currentBlockVariant = ((int)animator.GetFloat("fAnimBlockType"));
                                        //     int maxBlendTreeLength = 6;
                                        //     int nextBlockType = (currentBlockVariant + UnityEngine.Random.Range(1, maxBlendTreeLength)) % maxBlendTreeLength;
                                        //     animator.SetFloat("fAnimBlockType", nextBlockType);
                                        //     animator.SetTrigger("BlockHit");
                                        // }
                                        // else
                                        // {
                                        //     animator.SetTrigger("Dodge");
                                        // }
                                    }
                                }
                                else
                                {
                                    if (animationState.IsAbleToAttack())
                                    {
                                        int currentBlockVariant =
                                            (
                                            (int)
                                            animator
                                                .GetFloat("fAnimMeleeAttackType")
                                            );
                                        int maxBlendTreeLength = 3;
                                        int nextAttackType =
                                            (
                                            currentBlockVariant +
                                            UnityEngine
                                                .Random
                                                .Range(1, maxBlendTreeLength)
                                            ) %
                                            maxBlendTreeLength;

                                        animator
                                            .SetFloat("fAnimMeleeAttackType",
                                            nextAttackType);
                                        animator.SetTrigger("tAttack");

                                        int rand =
                                            UnityEngine.Random.Range(0, 100);
                                        if (rand < 78)
                                        {
                                            enemyCombatStateData =
                                                enemyGameObject
                                                    .GetComponent
                                                    <CombatStateData>();
                                            if (enemyCombatStateData != null)
                                            {
                                                enemyCombatStateData
                                                    .bShouldBlock = rand < 66;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    });
            }
        }

        private void UpdateActorCombatMovement(
            float targetDistance,
            CombatStateData combatStateData,
            NavMeshAgent myNavAgent,
            AIDecisionController myDecisionController,
            AnimationState animationState
        )
        {
            combatStateData.movementUpdateTimer -= updateTime;

            ActorNavigationData actorNavigationData =
                combatStateData.gameObject.GetComponent<ActorNavigationData>();

            float holdPositionDistance;
            bool isOutOfHoldRadius = false;
            bool shouldHoldPosition =
                combatStateData.combatNavigationState ==
                CombatNavigationState.holdPosition &&
                actorNavigationData.holdPositionCenter != null;
            if (shouldHoldPosition)
            {
                holdPositionDistance =
                    Vector3
                        .Distance(combatStateData.gameObject.transform.position,
                        actorNavigationData.holdPositionCenter.position);

                isOutOfHoldRadius =
                    holdPositionDistance >
                    actorNavigationData.holdPositionRadius;

                // if (
                //     holdPositionDistance >
                //     actorNavigationData.holdPositionRadius
                // )
                // {
                //     combatStateData.combatMovementType =
                //         CombatMovementType.fallBack;

                //     combatStateData
                //         .UpdateMovementTimer(combatStateData
                //             .combatMovementType);

                //     Debug.Log("Need to hold position!");
                //     return;
                // }
            }

            float keepDistMin;

            // float keepDistMin = combatStateData.keepDistanceMin > combatStateData.meleeAttackRange ? combatStateData.keepDistanceMin : combatStateData.meleeAttackRange;
            if (
                combatStateData.combatMovementBehaviorType ==
                CombatMovementBehaviorType.shooter
            )
            {
                keepDistMin = 8f;
            }
            else
            {
                keepDistMin = combatStateData.meleeAttackRange;
            }
            float keepDistMax =
                keepDistMin * combatStateData.distanceBufferRangeMult;

            bool navStopped =
                myNavAgent.remainingDistance <= myNavAgent.stoppingDistance &&
                !myNavAgent.pathPending;

            // Override other movements if out of the buffer range
            if (
                isOutOfHoldRadius ||
                targetDistance < keepDistMin ||
                navStopped ||
                animationState.IsStaggered()
            )
            {
                combatStateData.combatMovementType =
                    CombatMovementType.fallBack;

                if (isOutOfHoldRadius)
                {
                    combatStateData
                        .UpdateMovementTimer(combatStateData
                            .combatMovementType);
                }
            }
            else if (targetDistance > keepDistMax)
            {
                if (
                    !shouldHoldPosition ||
                    combatStateData.movementUpdateTimer < 0
                )
                {
                    combatStateData.combatMovementType =
                        CombatMovementType.pressAttack;
                    if (shouldHoldPosition)
                    {
                        combatStateData
                            .UpdateMovementTimer(combatStateData
                                .combatMovementType);
                    }
                }
            }

            // Update movment type if timmer complete
            if (combatStateData.movementUpdateTimer < 0)
            {
                combatStateData.distanceBufferRangeMult =
                    UnityEngine
                        .Random
                        .Range(combatStateData.distanceBufferRangeMultMin,
                        combatStateData.distanceBufferRangeMultMax);

                // if (myDecisionController != null)
                // {
                //     combatStateData.combatMovementType = myDecisionController.GetCombatMovementChoice();
                //     myDecisionController.lastMovmentSelected = combatStateData.combatMovementType;
                // }
                // else
                // {
                combatStateData.combatMovementType =
                    UnityEngine.Random.Range(0, 100) < 50
                        ? CombatMovementType.flankRight
                        : CombatMovementType.flankLeft;

                // }
                combatStateData
                    .UpdateMovementTimer(combatStateData.combatMovementType);
            }
        }
    }
}
