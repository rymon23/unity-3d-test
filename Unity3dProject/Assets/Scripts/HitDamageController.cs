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
                hitPosition);
            if (weapon == null) return;

            if (hitPosition == HitPositionType.head)
            {
                actorEventManger.DamageHealth(weapon.damage * 2);
            }
            else
            {
                actorEventManger.DamageHealth(weapon.damage);
            }
        }

        void TakeBulletHit(
            Projectile projectile,
            HitPositionType hitPosition = 0
        )
        {
            Debug
                .Log("HitDamageController => TakeBulletHit: position: " +
                hitPosition);
            if (projectile == null) return;

            if (hitPosition == HitPositionType.head)
            {
                actorEventManger.DamageHealth(projectile.damage * 2);
            }
            else
            {
                actorEventManger.DamageHealth(projectile.damage);
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
