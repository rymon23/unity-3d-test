using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Hybrid.Components;

namespace Hybrid.Systems
{      
    public class AttackStateSystem : ComponentSystem
    {

        protected override void OnUpdate() {
        
            Entities.WithAll<IsActor, Targeting>()
                .ForEach((Entity entity, IsActor actor) => {
                    AnimationState animationState = actor.gameObject.GetComponentInChildren<AnimationState>();
                    EquipSlotController equipSlotController = actor.gameObject.GetComponentInChildren<EquipSlotController>();
                    ParryStateController parryStateController = actor.gameObject.GetComponentInChildren<ParryStateController>();
                    Animator animator = actor.gameObject.GetComponentInChildren<Animator>();
                    CombatStateData combatStateData = actor.gameObject.GetComponentInChildren<CombatStateData>();
                    
                    if (animationState != null && equipSlotController != null) {
                        Weapon weapon = equipSlotController.handEquipSlots[0].weapon;   
                        GameObject blockingCollider = parryStateController.blockingCollider;

                        animator.SetBool("IsWeaponOut", animationState.isWeaponDrawn);

                        if (animationState.isWeaponDrawn == false) {
                          if (combatStateData.combatState >= CombatState.active && !animationState.isDrawingWeapon()) {
                                animator.SetTrigger("DrawWeapon");
                            }
                        }

                        if (animationState.IsInAttackHitFame()) {
                            // Debug.Log("Enable weapon collider for "+ actor.name);
                            weapon.EnableWeaponCollider();
                        } else {
                            // Debug.Log("Disable weapon collider for "+ actor.name);
                            weapon.DisableWeaponCollider();

                            if (blockingCollider != null) {
                                blockingCollider?.SetActive(animationState.isBlocking && parryStateController.canParry);
                                animator.SetBool("Blocking", animationState.isBlocking);
                            }
                        }
                    } 
                });
        }

    }

}