using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Hybrid.Components
{
    public class HitDamageController : MonoBehaviour
    {
        ActorEventManger actorEventManger;

        private void Start()
        {
            actorEventManger = GetComponent<ActorEventManger>();
            if (actorEventManger != null)
            {
                actorEventManger.onTakeWeaponHit += TakeWeaponHit;
                actorEventManger.onTakeBulletHit += TakeBulletHit;
            }
        }

        void TakeWeaponHit(Weapon weapon, HitPositionType hitPosition = 0)
        {
            Debug
                .Log("HitDamageController => TakeWeaponHit: position: " +
                hitPosition +
                " | Damage: " +
                weapon.damage);
            if (weapon == null) return;

            if (hitPosition == HitPositionType.head)
            {
                actorEventManger.DamageHealth(weapon.damage * 2, weapon.owner);
            }
            else
            {
                actorEventManger.DamageHealth(weapon.damage, weapon.owner);
            }
        }

        void TakeBulletHit(
            Weapon weapon,
            HitPositionType hitPosition,
            Projectile projectile
        )
        {
            float totalDamage = weapon.damage;
            if (projectile != null) totalDamage += projectile.damage;

            Debug
                .Log("HitDamageController => TakeBulletHit: position: " +
                hitPosition +
                " | Damage: " +
                totalDamage);

            if (projectile == null) return;

            if (hitPosition == HitPositionType.head)
            {
                actorEventManger.DamageHealth(totalDamage * 2, weapon.owner);
            }
            else
            {
                actorEventManger.DamageHealth(totalDamage, weapon.owner);
            }
        }

        private void OnDestroy()
        {
            if (actorEventManger != null)
            {
                actorEventManger.onTakeWeaponHit -= TakeWeaponHit;
                actorEventManger.onTakeBulletHit -= TakeBulletHit;
            }
        }
    }
}
