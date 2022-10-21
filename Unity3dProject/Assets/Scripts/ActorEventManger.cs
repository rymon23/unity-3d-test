using System;
using Hybrid.Components;
using UnityEngine;

public class ActorEventManger : MonoBehaviour
{
    public event Action<RagdollState> onActorRagdoll;

    public void ActorRagdoll(RagdollState state) =>
        onActorRagdoll?.Invoke(state);

    public event Action<GameObject> onActorDeath;

    public void ActorDeath() => onActorDeath?.Invoke(this.gameObject);

    public event Action<Weapon, HitPositionType> onTakeWeaponHit;

    public void TakeWeaponHit(Weapon weapon, HitPositionType hitPosition) =>
        onTakeWeaponHit?.Invoke(weapon, hitPosition);

    public event Action<Weapon, HitPositionType, Projectile> onTakeBulletHit;

    public void TakeBulletHit(
        Weapon weapon,
        HitPositionType hitPosition,
        Projectile projectile = null
    ) => onTakeBulletHit?.Invoke(weapon, hitPosition, projectile); // Use projectile if weapon fires multiple types of projectile

    public event Action<Weapon> onBlockWeaponHit;

    public void BlockWeaponHit(Weapon weapon) =>
        onBlockWeaponHit?.Invoke(weapon);

    public event Action<Projectile> onBlockProjectileHit;

    public void BlockProjectileHit(Projectile projectile) =>
        onBlockProjectileHit?.Invoke(projectile);

    public event Action<BlockAnimationState> OnMeleeBlockState;

    public void MeleeBlockState(BlockAnimationState state) =>
        OnMeleeBlockState?.Invoke(state);

    public event Action<AttackAnimationState> OnMeleeAttackState;

    public void MeleeAttackState(AttackAnimationState state) =>
        OnMeleeAttackState?.Invoke(state);

    public event Action<BashAnimationState> OnBashState;

    public void BashState(BashAnimationState state) =>
        OnBashState?.Invoke(state);

    // STAT DAMAGE
    public event Action<float> onDamageHealth;

    public event Action<float> onDamageStamina;

    public event Action<float> onDamageMana;

    public void DamageHealth(float fDamage) => onDamageHealth?.Invoke(fDamage);

    public void DamageStamina(float fDamage) =>
        onDamageStamina?.Invoke(fDamage);

    public void DamageMana(float fDamage) => onDamageMana?.Invoke(fDamage);

    public event Action<float> OnUpdateStatBar_Health;

    public void UpdateStatBar_Health(float fPercent) =>
        OnUpdateStatBar_Health?.Invoke(fPercent);

    public event Action<float> OnUpdateStatBar_Stamina;

    public void UpdateStatBar_Stamina(float fPercent) =>
        OnUpdateStatBar_Stamina?.Invoke(fPercent);

    public event Action<float> OnUpdateStatBar_Magic;

    public void UpdateStatBar_Magic(float fPercent) =>
        OnUpdateStatBar_Magic?.Invoke(fPercent);

    // Animation Triggers
    public event Action<HitPositionType> onTriggerAnim_Stagger;

    public event Action onTriggerAnim_Block;

    public event Action<ActorAction> onEvaluateAndAction;

    public event Action onTriggerAnim_Sheath;

    public event Action onTriggerAnim_Draw;

    public void TriggerAnim_Stagger(HitPositionType hitPositionType) =>
        onTriggerAnim_Stagger?.Invoke(hitPositionType);

    public void TriggerAnim_Block() => onTriggerAnim_Block?.Invoke();

    public void EvaluateAndAction(ActorAction actorAction) =>
        onEvaluateAndAction?.Invoke(actorAction);

    public void TriggerAnim_Sheath() => onTriggerAnim_Sheath?.Invoke();

    public void TriggerAnim_Draw() => onTriggerAnim_Draw?.Invoke();

    // Combat
    public event Action<float> onCombatAlert;

    public void CombatAlert(float distance) => onCombatAlert?.Invoke(distance);

    public event Action<CombatState> onCombatStateChange;

    public void CombatStateChange(CombatState newState) =>
        onCombatStateChange?.Invoke(newState);

    public event Action<GameObject> onCombatTargetUpdate;

    public void CombatTargetUpdate(GameObject newTarget) =>
        onCombatTargetUpdate?.Invoke(newTarget);

    public event Action<CombatMovementType> onCombatMovmentChange;

    public void CombatMovmentChange(CombatMovementType movementType) =>
        onCombatMovmentChange?.Invoke(movementType);

    public event Action onEvaluateCastingBehavior;

    public void EvaluateCastingBehavior() =>
        onEvaluateCastingBehavior?.Invoke();

    // Detection
    public event Action<GameObject> onTargetLost;

    public void TargetLost(GameObject actorGameObject) =>
        onTargetLost?.Invoke(actorGameObject);

    public event Action<TargetTrackingState, GameObject>
        onTargetTrackingStateChange;

    public void TargetTrackingStateChange(
        TargetTrackingState trackingState,
        GameObject actorGameObject
    ) => onTargetTrackingStateChange?.Invoke(trackingState, actorGameObject);

    public event Action<Vector3> onAlertSearchLocationUpdate;

    public void AlertSearchLocationUpdate(Vector3 position) =>
        onAlertSearchLocationUpdate?.Invoke(position);

    // Moral/Flee
    public event Action<float> onEvaluateFleeingState;

    public void EvaluateFleeingState(float currentHealthPercent) =>
        onEvaluateFleeingState?.Invoke(currentHealthPercent);

    // Rumble
    public event Action OnRumbleFire;

    public void RumbleFire() => OnRumbleFire?.Invoke();

    // Items
    public event Action<Item> OnWeaponItemAdded;

    public void WeaponItemAdded(Item item) => OnWeaponItemAdded?.Invoke(item);

    public event Action<Weapon> OnWeaponEquipped;

    public void WeaponEquipped(Weapon weapon) =>
        OnWeaponEquipped?.Invoke(weapon);

    public event Action<Weapon> OnWeaponUnequipped;

    public void WeaponUnequipped(Weapon weapon) =>
        OnWeaponUnequipped?.Invoke(weapon);

    public event Action OnEvaluateActiveEquippment;

    public void EvaluateActiveEquippment() =>
        OnEvaluateActiveEquippment?.Invoke();
}
