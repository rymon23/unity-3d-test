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

    public void AE_OnMeleeParryState(BlockAnimationState state)
    {
        Debug.Log("Animation Event: OnMeleeParryState - " + state);
        animationState.blockAnimationState = state;
        actorEventManger.MeleeBlockState (state);
    }

    public void AE_OnMeleeBashState(BashAnimationState state)
    {
        Debug.Log("Animation Event: OnMeleeBashState - " + state);
        animationState.bashAnimationState = state;
        animator.SetBool("bIsBashing", animationState.isBashing);
        actorEventManger.BashState (state);
    }

    public void AE_OnMeleeDodgeState(DodgeAnimationState state)
    {
        Debug.Log("Animation Event: OnMeleeDodgeState - " + state);
        animationState.dodgeAnimationState = state;
        animator.SetBool("bIsDodging", animationState.isDodging);
    }

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

            // FireSpell(actorSpells.spellsTemp[0], spellCastPoint);
            // Debug.Log("Fire Spell - Event: OnCastRangeState - " + state);
        }
    }

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

    private void FireSpell(MagicSpell magicSpell, Transform spellCastPoint)
    {
        Debug.Log("Animation Event: FireSpell - " + magicSpell.name);

        if (magicSpell != null && spellCastPoint != null)
        {
            GameObject prefab = magicSpell.projectile.gameObject;

            float spread = 1f;
            Vector3 frontPos;

            if (aIEquipController.targeter != null)
            {
                frontPos = aIEquipController.targeter.position;
            }
            else
            {
                frontPos =
                    spellCastPoint.position + (spellCastPoint.forward * 10);
            }

            // Vector3 aimDir = (frontPos - spellCastPoint.position).normalized;
            Vector3 aimDir =
                ((frontPos + (Vector3.up * 0.2f)) - spellCastPoint.position);

            // Calculate Spread
            float x = Random.Range(-spread, spread);
            float y = Random.Range(-spread, spread);

            Vector3 directionWithSpread = aimDir + new Vector3(x, y, 0);

            if (true)
                Debug
                    .DrawRay(spellCastPoint.position,
                    directionWithSpread,
                    Color.white);

            // if (!PreFireRayCheckIsBlocking(directionWithSpread))
            // {
            // Transform spell = Instantiate(ammunition, spellCastPoint.position, Quaternion.LookRotation(aimDir, Vector3.up));
            Transform currentBullet =
                Instantiate(prefab.transform,
                spellCastPoint.position,
                Quaternion.LookRotation(directionWithSpread, Vector3.up));
            Projectile projectile =
                currentBullet.gameObject.GetComponent<Projectile>();

            // projectile.damage += damage;
            // projectile.weapon = this;
            projectile.spellPrefab = magicSpell.spellPrefab;
            projectile.magicSpellPrefab = magicSpell;
            projectile.sender = this.gameObject;

            currentBullet.gameObject.transform.position =
                spellCastPoint.transform.position;
            currentBullet.transform.forward = directionWithSpread;
            currentBullet.gameObject.SetActive(true);
            // }
        }
    }
}
