using Hybrid.Components;
using RootMotion.FinalIK;
using UnityEngine;

public class CombatAnimationEvent : MonoBehaviour
{
    Animator animator;

    AnimationState animationState;

    ActorEventManger actorEventManger;

    EquipSlotController equipSlotController;

    ActorAIEquipController aIEquipController;

    CastingController castingController;

    Targeting targeting; // TEMP

    BipedIK bipedIK;

    void Start()
    {
        animator = GetComponent<Animator>();
        animationState = GetComponent<AnimationState>();
        actorEventManger = GetComponent<ActorEventManger>();
        equipSlotController = GetComponentInChildren<EquipSlotController>();
        bipedIK = GetComponent<BipedIK>();
        aIEquipController = GetComponent<ActorAIEquipController>();
        castingController = GetComponent<CastingController>();

        // Temp
        targeting = GetComponent<Targeting>();
    }


#region Weapon Draw / Sheath
    public void AE_OnWeaponDrawState(DrawAnimationState state)
    {
        Debug.Log("Animation Event: OnWeaponDrawState - " + state);
        animationState.drawAnimationState = state;

        Weapon weapon = equipSlotController.RightHandEquipSlot().weapon;
        if (state == DrawAnimationState.weaponGrab)
        {
            equipSlotController.DrawWeapon();
        }
    }

    public void AE_OnWeaponSheathState(SheathAnimationState state)
    {
        Debug.Log("Animation Event: OnWeaponSheathState - " + state);
        animationState.sheathAnimationState = state;

        Weapon weapon = equipSlotController.RightHandEquipSlot().weapon;
        if (state == SheathAnimationState.inactive)
        {
            equipSlotController.SheatheWeapon();
        }
        else if (state == SheathAnimationState.finish)
        {
            equipSlotController.UpdateActiveWeapon();
        }
    }


#endregion



#region Melee Attack / Bash
    public void AE_OnMeleeAttackState(AttackAnimationState state)
    {
        Debug.Log("Animation Event: OnMeleeAttackState - " + state);
        animationState.attackAnimationState = state;
        animator.SetBool("bIsAttacking", animationState.isAttacking);
        actorEventManger.MeleeAttackState (state);

        Weapon weapon = equipSlotController.RightHandEquipSlot().weapon;
        if (
            equipSlotController != null &&
            !animationState.IsStaggered() &&
            (
            animationState.IsInAttackHitFame() ||
            animationState.attackAnimationState ==
            AttackAnimationState.attackPreHit
            )
        )
        {
            if (weapon.useMeleeRaycasts)
            {
                weapon.raycastMeleeActive = true;
            }
            else
            {
                weapon.EnableWeaponCollider();
                Debug.Log("Enable weapon collider");
            }
        }
        else
        {
            if (weapon.useMeleeRaycasts)
            {
                weapon.raycastMeleeActive = false;
            }
            else
            {
                weapon.DisableWeaponCollider();
                Debug.Log("Disable weapon collider");
            }
        }
    }

    public void AE_OnMeleeBashState(BashAnimationState state)
    {
        Debug.Log("Animation Event: OnMeleeBashState - " + state);
        animationState.bashAnimationState = state;
        animator.SetBool("bIsBashing", animationState.isBashing);
        actorEventManger.BashState (state);
    }
#endregion



#region Melee Block / Parrying
    public void AE_OnMeleeParryState(BlockAnimationState state)
    {
        Debug.Log("Animation Event: OnMeleeParryState - " + state);
        animationState.blockAnimationState = state;
        actorEventManger.MeleeBlockState (state);
    }
#endregion



#region Melee Dodging
    public void AE_OnMeleeDodgeState(DodgeAnimationState state)
    {
        Debug.Log("Animation Event: OnMeleeDodgeState - " + state);
        animationState.dodgeAnimationState = state;
        animator.SetBool("bIsDodging", animationState.isDodging);
    }
#endregion



#region Spell Casting
    public void AE_OnCastSelfState(AnimState_Casting state)
    {
        Debug.Log("Animation Event: AE_OnCastSelfState - " + state);
        animationState.castAnimationState = state;
        animator.SetBool("bIsCasting", animationState.isCasting);
    }

    public void AE_OnCastRangeState(AnimState_Casting state)
    {
        Debug.Log("Animation Event: OnCastRangeState - " + state);
        animationState.castAnimationState = state;
        animator.SetBool("bIsCasting", animationState.isCasting);

        if (bipedIK != null)
        {
            Transform spellCastPoint =
                equipSlotController.LeftHandEquipSlot().castPoint;
            // if (state == AnimState_Casting.castStart)
            // {
            //     bipedIK.solvers.leftHand.IKPositionWeight = 0.3f;
            //     bipedIK.solvers.leftHand.IKRotationWeight = 0.5f;
            // }
            // else if (
            //     state > AnimState_Casting.castStart &&
            //     state < AnimState_Casting.castingFireFinish
            // )
            // {
            //     bipedIK.solvers.aim.IKPositionWeight = 0.8f;
            //     bipedIK.solvers.leftHand.target = aIEquipController.targeter;
            //     bipedIK.solvers.leftHand.IKPositionWeight = 0.8f;
            //     bipedIK.solvers.leftHand.IKRotationWeight = 0.5f;
            //     bipedIK.solvers.lookAt.IKPositionWeight = 1f;
            // }
            // else
            // {
            //     bipedIK.solvers.leftHand.IKPositionWeight = 0;
            //     bipedIK.solvers.leftHand.IKRotationWeight = 0;
            // }
        }
        ActorSpells actorSpells = this.GetComponent<ActorSpells>();
        if (
            animationState.IsInCastFireFame() &&
            actorSpells != null &&
            actorSpells.spellsTemp != null &&
            actorSpells.spellsTemp.Length > 0
        )
        {
            Transform spellCastPoint =
                equipSlotController.LeftHandEquipSlot().castPoint;

            actorEventManger
                .DamageMana(actorSpells.spellsTemp[0].baseMagicCost);

            castingController.FireSpellWithRaycast(spellCastPoint.position);
        }
    }
#endregion



#region Staggering / Knockdown
    public void AE_OnStaggerState(AnimState_Stagger state)
    {
        Debug.Log("Animation Event: AE_OnStaggerState - " + state);
        animationState.animState_Stagger = state;
        animator.SetBool("bIsStaggering", animationState.isStaggered);

        bipedIK.solvers.rightHand.IKPositionWeight = 0;
        bipedIK.solvers.rightHand.IKRotationWeight = 0;
        bipedIK.solvers.leftHand.IKPositionWeight = 0;
        bipedIK.solvers.leftHand.IKRotationWeight = 0;
    }

    public void AE_OnKnockDownState(AnimState_Knockdown state)
    {
        Debug.Log("Animation Event: OnKnockDownState - " + state);
        animationState.anim_knockdownstate = state;
        animator.SetBool("bIsKnockedDown", animationState.isKnockedDown);

        // actorEventManger.MeleeBlockState(state);
    }
#endregion

}
