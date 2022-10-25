using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Hybrid.Components;
using RootMotion.FinalIK;
public class ActorAIEquipController : MonoBehaviour
{
    ActorEventManger actorEventManger;
    EquipSlotController equipSlotController;
    ActorInventory actorInventory;
    Targeting targeting;
    AnimationState animationState;
    CombatStateData combatStateData;

    public WeaponType desiredWeaponType;
    public bool evaluateActiveEquippment = false;
    public Transform targeter;
    [SerializeField] Transform meleeAimTransform;
    [SerializeField] private float weaponSwitchCooldown = 5f;
    [SerializeField] private float weaponSwitchCooldownTimer = 0f;


    private void Start()
    {
        bipedIK = GetComponent<BipedIK>();
        combatStateData = GetComponent<CombatStateData>();
        animationState = GetComponent<AnimationState>();
        targeting = GetComponent<Targeting>();
        equipSlotController = GetComponent<EquipSlotController>();
        actorInventory = GetComponent<ActorInventory>();
        actorEventManger = GetComponent<ActorEventManger>();
        if (actorEventManger != null) actorEventManger.OnEvaluateActiveEquippment += EvaluateActiveEquippment;
    }

    private void OnDestroy()
    {
        if (actorEventManger != null) actorEventManger.OnEvaluateActiveEquippment -= EvaluateActiveEquippment;
    }

    private void FixedUpdate()
    {
        if (evaluateActiveEquippment)
        {
            // evaluateActiveEquippment = false;

            EvaluateActiveEquippment();
        }
    }


    public bool bHasMeleeWeapon;
    public bool bHasRangedWeaqpon;

    [SerializeField] private Weapon meleeWeapon;
    [SerializeField] private Weapon rangedWeapon;

    [SerializeField] BipedIK bipedIK;
    [SerializeField] float aimWeight = 0.7f;

    private void EvaluateActiveEquippment()
    {
        if (targeting != null)
        {
            if (!combatStateData.IsInCombat()) return;


            meleeWeapon = equipSlotController.GetWornWeaponOfType(WeaponType.sword);
            rangedWeapon = equipSlotController.GetWornWeaponOfType(WeaponType.gun);
            // Item meleeWeapon = actorInventory.GetWeaponItemOfType(WeaponType.sword);
            // Item rangedWeapon = actorInventory.GetWeaponItemOfType(WeaponType.gun);
            bHasMeleeWeapon = meleeWeapon != null;
            bHasRangedWeaqpon = rangedWeapon != null;

            Weapon currentWeapon = equipSlotController.RightHandEquipSlot().weapon;

            if (animationState.isWeaponDrawn && !animationState.isSheathingWeapon())
            {
                if (!animationState.isCasting && !animationState.IsStaggered())
                {

                    if (currentWeapon.weaponType == WeaponType.sword)
                    {
                        combatStateData.combatMovementBehaviorType = CombatMovementBehaviorType.melee;

                        if (bipedIK != null && meleeAimTransform != null)
                        {
                            if (bipedIK.solvers.aim.transform != meleeAimTransform)
                            {
                                bipedIK.solvers.aim.transform = meleeAimTransform;
                            }
                            if (bipedIK.solvers.lookAt.target != targeter || bipedIK.solvers.aim.IKPositionWeight == 0)
                            {
                                bipedIK.solvers.aim.IKPositionWeight = aimWeight;
                                bipedIK.solvers.lookAt.target = targeter;
                                bipedIK.solvers.lookAt.IKPositionWeight = aimWeight;

                            }
                        }
                    }
                    else
                    {
                        combatStateData.combatMovementBehaviorType = CombatMovementBehaviorType.shooter;

                        if (bipedIK != null && targeter != null && targeting.currentTarget != null)
                        {

                            if (bipedIK.solvers.aim.transform != currentWeapon.transform.parent.transform.parent)
                            {
                                bipedIK.solvers.aim.transform = currentWeapon.transform.parent.transform.parent;
                            }

                            if (bipedIK.solvers.aim.IKPositionWeight == 0)
                            {
                                // if (bipedIK.solvers.aim.transform != currentWeapon.transform)
                                // {
                                //     bipedIK.solvers.aim.transform = currentWeapon.transform.parent.transform.parent;
                                // }
                                bipedIK.solvers.aim.IKPositionWeight = aimWeight;
                                // bipedIK.solvers.aim.target = targeter;
                            }
                        }

                    }

                }

                if (bipedIK.solvers.aim.target != targeter)
                {
                    bipedIK.solvers.aim.target = targeter;
                }

                if (targeter != null && targeting.currentTarget != null)
                {
                    targeter.position = targeting.currentTarget.transform.position + (Vector3.up * 1.14f);
                }


                if (weaponSwitchCooldownTimer > 0)
                {
                    weaponSwitchCooldownTimer -= Time.deltaTime;
                }
                else
                {
                    if (bHasMeleeWeapon && (!bHasRangedWeaqpon || targeting.targetDistance < 4.6f) && currentWeapon.weaponType != WeaponType.sword)
                    {
                        weaponSwitchCooldownTimer = weaponSwitchCooldown;

                        desiredWeaponType = WeaponType.sword;
                        equipSlotController.UpdateDesiredWeapon(meleeWeapon);
                        return;
                    }
                    if (bHasRangedWeaqpon && (!bHasMeleeWeapon || targeting.targetDistance > 9.3f) && currentWeapon.weaponType != WeaponType.gun)
                    {
                        weaponSwitchCooldownTimer = weaponSwitchCooldown;

                        desiredWeaponType = WeaponType.gun;
                        equipSlotController.UpdateDesiredWeapon(rangedWeapon);
                        return;
                    }
                }
            }
            else
            {
                if (bipedIK.solvers.aim.IKPositionWeight > 0)
                {
                    bipedIK.solvers.aim.IKPositionWeight = 0;
                }

                if (animationState.isWeaponDrawn == false && !animationState.isDrawingWeapon() && !animationState.isSheathingWeapon())
                {
                    actorEventManger.TriggerAnim_Draw();
                }
            }

        }
    }

}
