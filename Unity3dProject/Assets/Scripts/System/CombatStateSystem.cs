using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine.AI;
using Hybrid.Components;

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

                Entities.WithAll<DetectionStateData, IsActor, Targeting, ActorFOV, ActorHealth>()
            .ForEach((Entity entity, IsActor actor, DetectionStateData detectionStateData, Targeting targeting, ActorFOV myFOV, CombatStateData combatStateData, ActorHealth actorHealth) =>
            {
                // Check if actor is Dead
                if (actorHealth.deathState >= DeathState.dying)
                {
                    return;
                }

                AnimationState animationState = actor.gameObject.GetComponentInChildren<AnimationState>();
                EquipSlotController equipSlotController = actor.gameObject.GetComponentInChildren<EquipSlotController>();
                ParryStateController parryStateController = actor.gameObject.GetComponentInChildren<ParryStateController>();
                Animator animator = actor.gameObject.GetComponentInChildren<Animator>();

                if (targeting.currentTarget != null)
                {
                    GameObject enemyGameObject = targeting.currentTarget.gameObject;
                    AnimationState enemyAnimState = enemyGameObject.GetComponent<AnimationState>();
                    CombatStateData enemyCombatStateData = enemyGameObject.GetComponent<CombatStateData>();
                    NavMeshAgent myNavAgent = actor.GetComponent<NavMeshAgent>();

                    float distance = Vector3.Distance(actor.transform.position, enemyGameObject.transform.position);
                    targeting.targetDistance = distance;

                    bool hasTargetInFOV = UtilityHelpers.IsInFOVScope(actor.transform, targeting.currentTarget.transform.position, myFOV.maxAngle, myFOV.maxRadius);

                    combatStateData.movementUpdateTimer -= updateTime;
                    if (combatStateData.movementUpdateTimer < 0 || myNavAgent.remainingDistance <= myNavAgent.stoppingDistance && !myNavAgent.pathPending)
                    {
                        int rand = UnityEngine.Random.Range(0, 100);
                        // float newTimer;
                        bool isWithinOuterRange = distance < 5.2f;
                        // bool isInInnerRange = distance < 1.5f;
                        bool isInInnerRange = distance < combatStateData.meleeAttackRange;

                        if ((isInInnerRange && rand < 50) || (isWithinOuterRange && rand < 20))
                        {
                            // newTimer = UnityEngine.Random.Range(combatStateData.fallBackTimeMin, combatStateData.fallBackTimeMax);
                            combatStateData.combatMovementType = CombatMovementType.fallBack;
                        }
                        else if (isWithinOuterRange)
                        {
                            // newTimer = UnityEngine.Random.Range(combatStateData.flankTimeMin, combatStateData.flankTimeMax);
                            combatStateData.combatMovementType = UnityEngine.Random.Range(0, 100) < 50 ? CombatMovementType.flankRight : CombatMovementType.flankLeft;
                        }
                        else
                        {
                            // newTimer = UnityEngine.Random.Range(combatStateData.advanceTimeMin, combatStateData.advanceTimeMax);
                            combatStateData.combatMovementType = CombatMovementType.pressAttack;

                        }
                        // combatStateData.movementUpdateTimer = newTimer;
                        combatStateData.UpdateMovementTimer(combatStateData.combatMovementType);

                    }

                    float attackDistance = Vector3.Distance(targeting.attackPos, enemyGameObject.transform.position);
                    targeting.targetAttackDistance = attackDistance;

                    if (hasTargetInFOV && attackDistance < combatStateData.meleeAttackRange && distance > 0.01f && !animationState.isAttacking)
                    // if (hasTargetInFOV && distance < 2 && distance > 0.01f && !animationState.isAttacking)
                    {
                        if (enemyAnimState.isAttacking && enemyAnimState.attackAnimationState <= AttackAnimationState.attackPreHit)
                        {
                            if (!animationState.isBlocking && !animationState.isDodging && !animationState.IsInAttackHitFame())
                            {
                                // if (UnityEngine.Random.Range(0, 100) < 80)
                                // {
                                //     int currentBlockVariant = ((int)animator.GetFloat("animBlockType"));
                                //     int maxBlendTreeLength = 6;
                                //     int nextBlockType = (currentBlockVariant + UnityEngine.Random.Range(1, maxBlendTreeLength)) % maxBlendTreeLength;
                                //     animator.SetFloat("animBlockType", nextBlockType);
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
                            if (!animationState.IsInBlockHitFame() && !animationState.isAttacking && !animationState.bDisableAttacking)
                            {
                                int currentBlockVariant = ((int)animator.GetFloat("fAnimMeleeAttackType"));
                                int maxBlendTreeLength = 3;
                                int nextAttackType = (currentBlockVariant + UnityEngine.Random.Range(1, maxBlendTreeLength)) % maxBlendTreeLength;
                                animator.SetFloat("fAnimMeleeAttackType", nextAttackType);
                                animator.SetTrigger("Attack");

                                int rand = UnityEngine.Random.Range(0, 100);
                                if (rand < 78)
                                {
                                    enemyCombatStateData = enemyGameObject.GetComponent<CombatStateData>();
                                    if (enemyCombatStateData != null)
                                    {
                                        enemyCombatStateData.bShouldBlock = rand < 66;
                                    }
                                }
                            }
                        }
                    }


                }

            });

            }
        }
    }

}