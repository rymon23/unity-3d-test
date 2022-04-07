using UnityEngine;
using Hybrid.Components;

public class CombatAnimationEvent : MonoBehaviour
{
    Animator animator;
    AnimationState animationState;
    ActorEventManger actorEventManger;
    EquipSlotController equipSlotController;
    Targeting targeting; // TEMP

    void Start()
    {
        animator = GetComponent<Animator>();
        animationState = GetComponent<AnimationState>();
        actorEventManger = GetComponent<ActorEventManger>();
        equipSlotController = GetComponentInChildren<EquipSlotController>();

        // Temp
        targeting = GetComponent<Targeting>();
    }

    public void AE_OnWeaponDrawState(DrawAnimationState state)
    {
        Debug.Log("Animation Event: OnWeaponDrawState - " + state);
        animationState.drawAnimationState = state;
    }
    public void AE_OnWeaponSheathState(SheathAnimationState state)
    {
        Debug.Log("Animation Event: OnWeaponSheathState - " + state);
        animationState.sheathAnimationState = state;
    }
    public void AE_OnMeleeAttackState(AttackAnimationState state)
    {
        Debug.Log("Animation Event: OnMeleeAttackState - " + state);
        animationState.attackAnimationState = state;
        animator.SetBool("bIsAttacking", animationState.isAttacking);
        actorEventManger.MeleeAttackState(state);

        if (equipSlotController != null && animationState.IsInAttackHitFame() && !animationState.IsStaggered())
        {
            Weapon weapon = equipSlotController.handEquipSlots[0].weapon;
            weapon.EnableWeaponCollider();
            Debug.Log("Enable weapon collider");
        }
        else
        {
            Weapon weapon = equipSlotController.handEquipSlots[0].weapon;
            weapon.DisableWeaponCollider();
            Debug.Log("Disable weapon collider");
        }
    }
    public void AE_OnMeleeParryState(BlockAnimationState state)
    {
        Debug.Log("Animation Event: OnMeleeParryState - " + state);
        animationState.blockAnimationState = state;
        actorEventManger.MeleeBlockState(state);
    }

    public void AE_OnMeleeBashState(BashAnimationState state)
    {
        Debug.Log("Animation Event: OnMeleeBashState - " + state);
        animationState.bashAnimationState = state;
        animator.SetBool("bIsBashing", animationState.isBashing);
        actorEventManger.BashState(state);
    }
    public void AE_OnMeleeDodgeState(DodgeAnimationState state)
    {
        Debug.Log("Animation Event: OnMeleeDodgeState - " + state);
        animationState.dodgeAnimationState = state;
        animator.SetBool("bIsDodging", animationState.isDodging);
    }
    public void AE_OnCastRangeState(AnimState_Casting state)
    {
        Debug.Log("Animation Event: OnCastRangeState - " + state);
        animationState.castAnimationState = state;
        animator.SetBool("bIsCasting", animationState.isCasting);

        ActorSpells actorSpells = this.GetComponent<ActorSpells>();
        if (animationState.IsInCastFireFame() && actorSpells != null && actorSpells.spellsTemp != null && actorSpells.spellsTemp.Length > 0)
        {
            Transform spellCastPoint = equipSlotController.handEquipSlots[(int)HandEquipSide.leftHand].castPoint;

            actorEventManger.DamageMana(actorSpells.spellsTemp[0].baseMagicCost);

            FireSpell(actorSpells.spellsTemp[0], spellCastPoint);
            Debug.Log("Fire Spell - Event: OnCastRangeState - " + state);
        }

    }
    public void AE_OnStaggerState(AnimState_Stagger state)
    {
        Debug.Log("Animation Event: AE_OnStaggerState - " + state);
        animationState.animState_Stagger = state;
        animator.SetBool("bIsStaggering", animationState.isStaggered);
    }


    private void FireSpell(MagicSpell magicSpell, Transform spellCastPoint)
    {
        if (magicSpell != null && spellCastPoint != null)
        {
            Transform prefab = magicSpell.projectile;

            // Vector3 frontPos = spellCastPoint.position + (spellCastPoint.forward * 10);
            Vector3 frontPos = targeting.currentTarget ?
                targeting.currentTarget.transform.position + (Vector3.up * 1.3f) :
                this.gameObject.transform.position + (this.gameObject.transform.forward * 10);

            Vector3 aimDir = (frontPos - spellCastPoint.position).normalized;

            // Debug.DrawRay(spellCastPoint.TransformPoint(spellCastPoint.position), aimDir, Color.red);
            // Debug.DrawRay(spellCastPoint.position, frontPos, Color.yellow);
            // Debug.DrawRay(spellCastPoint.transform.position, frontPos, Color.blue);

            Transform currentProjectile = Instantiate(prefab, spellCastPoint.transform.position + new Vector3(0,-0.4f,0), Quaternion.LookRotation(aimDir, Vector3.up));
            Projectile projectile = currentProjectile.gameObject.GetComponent<Projectile>();
            projectile.sender = this.gameObject;

            currentProjectile.position = spellCastPoint.position;
            currentProjectile.position = spellCastPoint.transform.position;
            currentProjectile.transform.forward = aimDir;
            currentProjectile.gameObject.SetActive(true);
        }
    }
}
