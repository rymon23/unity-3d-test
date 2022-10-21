using System.Collections;
using System.Collections.Generic;
using Hybrid.Components;
using UnityEngine;
using UnityEngine.AI;
using Unity.Entities;

namespace Hybrid.Systems
{
    public class AttackStateSystem : ComponentSystem
    {
        protected override void OnUpdate()
        {
            Entities
                .WithAll<IsActor, Targeting, AnimationState>()
                .ForEach((
                    Entity entity,
                    IsActor actor,
                    ActorHealth actorHealth,
                    AnimationState animationState
                ) =>
                {
                    // Check if actor is Dead OR the Player
                    if (
                        actorHealth.deathState >= DeathState.dying ||
                        actor.gameObject.tag == "Player" ||
                        animationState.isFlying
                    )
                    {
                        return;
                    }

                    // AnimationState animationState = actor.gameObject.GetComponentInChildren<AnimationState>();
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
                    CombatStateData combatStateData =
                        actor
                            .gameObject
                            .GetComponentInChildren<CombatStateData>();

                    if (actor.gameObject.tag != "Player")
                    {
                        // Update velocity float on animaor
                        NavMeshAgent agent =
                            actor
                                .gameObject
                                .GetComponentInChildren<NavMeshAgent>();

                        if (agent != null)
                        {
                            animator
                                .SetFloat("velocity",
                                agent.velocity.magnitude *
                                combatStateData.currentVelocityZ);
                        }
                    }

                    if (animationState != null && equipSlotController != null)
                    {
                        GameObject blockingCollider =
                            parryStateController.blockingCollider;

                        Weapon weapon =
                            equipSlotController.RightHandEquipSlot().weapon;
                        bool hasGun = false;

                        if (weapon != null)
                        {
                            // Assign actor's refId to weapon
                            if (
                                weapon._ownerRefId ==
                                UtilityHelpers.GetUnsetActorEntityRefId()
                            )
                            {
                                weapon._ownerRefId = actor.refId;
                            }
                            hasGun = (weapon.weaponType == WeaponType.gun);
                            animator
                                .SetInteger("iWeaponType",
                                (int) weapon.weaponType);
                        }

                        if (hasGun)
                        {
                            animationState.isBlocking = false;
                            blockingCollider?.SetActive(false);
                            combatStateData.combatMovementBehaviorType =
                                CombatMovementBehaviorType.shooter;
                            animator.SetBool("Blocking", false);
                            animator.SetFloat("IdleType", 2f);
                            animator.SetFloat("animWeaponType", 2);
                            animator.SetInteger("animMovementType", 2);

                            // animator.SetInteger("iWeaponType", (int)weapon.weaponType);
                            // animationState.isWeaponDrawn = true; //combatStateData.combatState >= CombatState.active;
                            animator
                                .SetBool("IsWeaponOut",
                                animationState.isWeaponDrawn);
                            return;
                        }

                        animator
                            .SetBool("IsWeaponOut",
                            animationState.isWeaponDrawn);

                        // animator.SetInteger("iWeaponType", 1);
                        if (combatStateData.combatState >= CombatState.active)
                        {
                            if (
                                animationState.isWeaponDrawn == false &&
                                !animationState.isDrawingWeapon() &&
                                !animationState.isSheathingWeapon()
                            )
                            {
                                animator.SetTrigger("DrawWeapon");
                            }

                            if (
                                animationState.isWeaponDrawn == false &&
                                !animationState.isDrawingWeapon()
                            )
                            {
                                // animator.SetTrigger("DrawWeapon");
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
                            if (
                                animationState.isWeaponDrawn &&
                                !animationState.isDrawingWeapon() &&
                                !animationState.isSheathingWeapon()
                            )
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

                        //Disable weapon hit collider if not in hitframe
                        if (animationState.IsInAttackHitFame())
                        {
                            // Debug.Log("Enable weapon collider for "+ actor.name);
                            // weapon.EnableWeaponCollider();
                        }
                        else
                        {
                            Debug
                                .Log("Disable weapon collider for " +
                                actor.name);
                            weapon.DisableWeaponCollider();
                        }

                        // Handle Blocking animation state & collider
                        if (
                            // !animationState.isBlocking ||
                            !animationState.IsAbleToBlock()
                        )
                        {
                            animator.SetBool("Blocking", false);
                            if (blockingCollider != null)
                                blockingCollider?.SetActive(false);
                        }
                        else
                        {
                            // animator.SetBool("Blocking", true);
                            if (blockingCollider != null)
                                blockingCollider?.SetActive(true);

                            // animator
                            //     .SetBool("Blocking", animationState.isBlocking);

                            // if (blockingCollider != null)
                            //     blockingCollider?
                            //         .SetActive(animationState.isBlocking &&
                            //         parryStateController.canParry);
                        }
                    }
                });
        }
    }
}
