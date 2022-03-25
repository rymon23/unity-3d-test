using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Unity.Entities;
using Hybrid.Components;

namespace Hybrid.Systems
{
    public class AttackStateSystem : ComponentSystem
    {

        protected override void OnUpdate()
        {

            Entities.WithAll<IsActor, Targeting, AnimationState>()
                .ForEach((Entity entity, IsActor actor, ActorHealth actorHealth, AnimationState animationState) =>
                {

                    // Check if actor is Dead
                    if (actorHealth.deathState >= DeathState.dying)
                    {
                        return;
                    }

                    // AnimationState animationState = actor.gameObject.GetComponentInChildren<AnimationState>();
                    EquipSlotController equipSlotController = actor.gameObject.GetComponentInChildren<EquipSlotController>();
                    ParryStateController parryStateController = actor.gameObject.GetComponentInChildren<ParryStateController>();
                    Animator animator = actor.gameObject.GetComponentInChildren<Animator>();
                    CombatStateData combatStateData = actor.gameObject.GetComponentInChildren<CombatStateData>();

                    if (actor.gameObject.tag != "Player")
                    {
                        // Update velocity float on animaor
                        NavMeshAgent agent = actor.gameObject.GetComponentInChildren<NavMeshAgent>();
                        animator.SetFloat("velocity", agent.velocity.magnitude * combatStateData.currentVelocityZ);
                    }

                    if (animationState != null && equipSlotController != null)
                    {
                        GameObject blockingCollider = parryStateController.blockingCollider;

                        Weapon weapon = equipSlotController.handEquipSlots[0].weapon;
                        bool hasGun = false;

                        if (weapon != null)
                        {
                            // Assign actor's refId to weapon
                            if (weapon._ownerRefId == UtilityHelpers.GetUnsetActorEntityRefId())
                            {
                                weapon._ownerRefId = actor.refId;
                            }
                            hasGun = (weapon.weaponType == WeaponType.gun);

                        }

                        if (hasGun)
                        {
                            animationState.isBlocking = false;
                            animationState.isWeaponDrawn = true;
                            blockingCollider?.SetActive(false);
                            animator.SetBool("Blocking", false);
                            animator.SetBool("IsWeaponOut", true);
                            animator.SetFloat("IdleType", 2f);
                            animator.SetFloat("animWeaponType", 2);
                            animator.SetInteger("animMovementType", 2);
                            return;
                        }

                        animator.SetBool("IsWeaponOut", animationState.isWeaponDrawn);

                        if (combatStateData.combatState >= CombatState.active)
                        {
                            if (animationState.isWeaponDrawn == false && !animationState.isDrawingWeapon())
                            {
                                animator.SetTrigger("DrawWeapon");
                                animator.SetInteger("animMovementType", 1);
                            }
                            else
                            {
                                animator.SetFloat("IdleType", 1f);
                                animator.SetFloat("animWeaponType", 1);
                            }
                        }
                        else
                        {
                            if (animationState.isWeaponDrawn && !animationState.isSheathingWeapon())
                            {
                                animator.SetTrigger("SheathWeapon");
                            }
                            else
                            {
                                animator.SetFloat("IdleType", 0f);
                                animator.SetFloat("animWeaponType", 0);
                                animator.SetInteger("animMovementType", 0);
                            }
                        }

                        if (weapon == null) return;

                        if (animationState.IsInAttackHitFame())
                        {
                            // Debug.Log("Enable weapon collider for "+ actor.name);
                            // weapon.EnableWeaponCollider();
                        }
                        else
                        {
                            Debug.Log("Disable weapon collider for " + actor.name);
                            weapon.DisableWeaponCollider();

                            if (blockingCollider != null)
                            {
                                blockingCollider?.SetActive(animationState.isBlocking && parryStateController.canParry);
                                animator.SetBool("Blocking", animationState.isBlocking);
                            }
                        }
                    }
                });
        }

    }

}