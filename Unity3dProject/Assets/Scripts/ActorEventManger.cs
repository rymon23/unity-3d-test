using System;
using UnityEngine;
using Hybrid.Components;

public class ActorEventManger : MonoBehaviour
{
    public event Action<GameObject> onActorDeath;
    public void ActorDeath() => onActorDeath?.Invoke(this.gameObject);

    public event Action<Weapon, HitPositionType> onTakeWeaponHit;
    public void TakeWeaponHit(Weapon weapon, HitPositionType hitPosition) => onTakeWeaponHit?.Invoke(weapon, hitPosition);

    public event Action<Projectile, HitPositionType> onTakeBulletHit;
    public void TakeBulletHit(Projectile projectile, HitPositionType hitPosition) => onTakeBulletHit?.Invoke(projectile, hitPosition);

    public event Action<Weapon> onBlockWeaponHit;
    public void BlockWeaponHit(Weapon weapon) => onBlockWeaponHit?.Invoke(weapon);
    public event Action<Projectile> onBlockProjectileHit;
    public void BlockProjectileHit(Projectile projectile) => onBlockProjectileHit?.Invoke(projectile);

    public event Action<BlockAnimationState> OnMeleeBlockState;
    public void MeleeBlockState(BlockAnimationState state) => OnMeleeBlockState?.Invoke(state);
    public event Action<AttackAnimationState> OnMeleeAttackState;
    public void MeleeAttackState(AttackAnimationState state) => OnMeleeAttackState?.Invoke(state);
    public event Action<BashAnimationState> OnBashState;
    public void BashState(BashAnimationState state) => OnBashState?.Invoke(state);


    // STAT DAMAGE
    public event Action<float> onDamageHealth;
    public event Action<float> onDamageStamina;
    public event Action<float> onDamageMana;
    public void DamageHealth(float fDamage) => onDamageHealth?.Invoke(fDamage);
    public void DamageStamina(float fDamage) => onDamageStamina?.Invoke(fDamage);
    public void DamageMana(float fDamage) => onDamageMana?.Invoke(fDamage);

    public event Action<float> OnUpdateStatBar_Health;
    public void UpdateStatBar_Health(float fPercent) => OnUpdateStatBar_Health?.Invoke(fPercent);
    public event Action<float> OnUpdateStatBar_Stamina;
    public void UpdateStatBar_Stamina(float fPercent) => OnUpdateStatBar_Stamina?.Invoke(fPercent);
    public event Action<float> OnUpdateStatBar_Magic;
    public void UpdateStatBar_Magic(float fPercent) => OnUpdateStatBar_Magic?.Invoke(fPercent);


    // Animation Triggers
    public event Action<HitPositionType> onTriggerAnim_Stagger;
    public event Action onTriggerAnim_Block;
    public event Action onTriggerAnim_Attack;
    public void TriggerAnim_Stagger(HitPositionType hitPositionType) => onTriggerAnim_Stagger?.Invoke(hitPositionType);
    public void TriggerAnim_Block() => onTriggerAnim_Block?.Invoke();
    public void TriggerAnim_Attack() => onTriggerAnim_Attack?.Invoke();


    // Combat
    public event Action<float> onCombatAlert;
    public void CombatAlert(float distance) => onCombatAlert?.Invoke(distance);
    public event Action<CombatState> onCombatStateChange;
    public void CombatStateChange(CombatState newState) => onCombatStateChange?.Invoke(newState);

    public event Action<GameObject> onCombatTargetUpdate;
    public void CombatTargetUpdate(GameObject newTarget) => onCombatTargetUpdate?.Invoke(newTarget);

    public event Action<CombatMovementType> onCombatMovmentChange;
    public void CombatMovmentChange(CombatMovementType movementType) => onCombatMovmentChange?.Invoke(movementType);


    // Detection

    public event Action<GameObject> onTargetLost;
    public void TargetLost(GameObject actorGameObject) => onTargetLost?.Invoke(actorGameObject);
    public event Action<TargetTrackingState, GameObject> onTargetTrackingStateChange;
    public void TargetTrackingStateChange(TargetTrackingState trackingState, GameObject actorGameObject) => onTargetTrackingStateChange?.Invoke(trackingState, actorGameObject);
    public event Action<Vector3> onAlertSearchLocationUpdate;
    public void AlertSearchLocationUpdate(Vector3 position) => onAlertSearchLocationUpdate?.Invoke(position);

    // Moral/Flee

    public event Action<float> onEvaluateFleeingState;
    public void EvaluateFleeingState(float currentHealthPercent) => onEvaluateFleeingState?.Invoke(currentHealthPercent);
}
