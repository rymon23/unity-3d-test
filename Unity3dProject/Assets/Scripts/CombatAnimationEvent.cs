using UnityEngine;

public class CombatAnimationEvent : MonoBehaviour
{
    AnimationState animationState;

    void Start()
    {
        animationState = GetComponent<AnimationState>();
    }
    public void AE_OnWeaponDrawState(DrawAnimationState state) {
        Debug.Log("Animation Event: OnWeaponDrawState - " + state);
        animationState.drawAnimationState = state;
    }
    public void AE_OnWeaponSheathState(SheathAnimationState state) {
        Debug.Log("Animation Event: OnWeaponSheathState - " + state);
        animationState.sheathAnimationState = state;
    }
    public void AE_OnMeleeAttackState(AttackAnimationState state) {
        Debug.Log("Animation Event: OnMeleeAttackState - " + state);
        animationState.attackAnimationState = state;
    }
    public void AE_OnMeleeParryState(BlockAnimationState state) {
        Debug.Log("Animation Event: OnMeleeParryState - " + state);
        animationState.blockAnimationState = state;
    }
    public void AE_OnMeleeDodgeState(DodgeAnimationState state) {
        Debug.Log("Animation Event: OnMeleeDodgeState - " + state);
        animationState.dodgeAnimationState = state;
    }
}
