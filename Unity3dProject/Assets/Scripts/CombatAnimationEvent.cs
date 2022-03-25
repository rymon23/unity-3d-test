using UnityEngine;

public class CombatAnimationEvent : MonoBehaviour
{
    AnimationState animationState;
    EquipSlotController equipSlotController;

    void Start()
    {
        animationState = GetComponent<AnimationState>();
        equipSlotController = GetComponentInChildren<EquipSlotController>();
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
    }
    public void AE_OnMeleeDodgeState(DodgeAnimationState state)
    {
        Debug.Log("Animation Event: OnMeleeDodgeState - " + state);
        animationState.dodgeAnimationState = state;
    }
    public void AE_OnCastRangeState(AnimState_Casting state)
    {
        Debug.Log("Animation Event: OnCastRangeState - " + state);
        animationState.castAnimationState = state;
    }
    public void AE_OnStaggerState(AnimState_Stagger state)
    {
        Debug.Log("Animation Event: AE_OnStaggerState - " + state);
        animationState.animState_Stagger = state;
    }
}
