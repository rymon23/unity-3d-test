using System;
using UnityEngine;
using Hybrid.Components;

public class ActorEventManger : MonoBehaviour
{
    public event Action onActorDeath;
    public void ActorDeath() => onActorDeath?.Invoke();

    public event Action<Weapon, HitPositionType> onTakeWeaponHit;
    public void TakeWeaponHit(Weapon weapon, HitPositionType hitPosition) => onTakeWeaponHit?.Invoke(weapon, hitPosition);
    public event Action<float> onBlockHit;
    public void BlockHit(float fDamage) => onBlockHit?.Invoke(fDamage);


    // STAT DAMAGE
    public event Action<float> onDamageHealth;
    public event Action<float> onDamageStamina;
    public event Action<float> onDamageMana;
    public void DamageHealth(float fDamage) => onDamageHealth?.Invoke(fDamage);
    public void DamageStamina(float fDamage) => onDamageStamina?.Invoke(fDamage);
    public void DamageMana(float fDamage) => onDamageMana?.Invoke(fDamage);


    // Animation Triggers
    public event Action<HitPositionType> onTriggerAnim_Stagger;
    public event Action onTriggerAnim_Block;
    public event Action onTriggerAnim_Attack;
    public void TriggerAnim_Stagger(HitPositionType hitPositionType) => onTriggerAnim_Stagger?.Invoke(hitPositionType);
    public void TriggerAnim_Block() => onTriggerAnim_Block?.Invoke();
    public void TriggerAnim_Attack() => onTriggerAnim_Attack?.Invoke();


    public event Action<float> onUpdateHealthBar;
    public void UpdateHealthBar(float fPercent) => onUpdateHealthBar?.Invoke(fPercent);

    public event Action<CombatState> onCombatStateChange;
    public void CombatStateChange(CombatState newState) => onCombatStateChange?.Invoke(newState);

    public event Action<GameObject> onCombatTargetUpdate;
    public void CombatTargetUpdate(GameObject newTarget) => onCombatTargetUpdate?.Invoke(newTarget);
}
