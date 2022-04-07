using UnityEngine;
using Hybrid.Components;

public enum HitPositionType
{
    body = 0,
    head = 1,
}

public enum HitDirectionType
{
    front = 0,
    back = 1,
    side = 2,
}


[RequireComponent(typeof(BoxCollider))]
public class Hitbox : MonoBehaviour
{
    [SerializeField] IsActor actor;
    [SerializeField] GameObject myGameObject;
    [SerializeField] BoxCollider myCollider;
    [SerializeField] ActorEventManger actorEventManger;
    [SerializeField] HitPositionType bodyPosition = 0;

    private void Awake()
    {
        actor = GetComponentInParent<IsActor>();
        actorEventManger = GetComponentInParent<ActorEventManger>();
        myGameObject = GetComponentInParent<IsActor>().gameObject;
        myCollider = GetComponent<BoxCollider>();
    }

    void onWeaponHit(Weapon weapon, HitPositionType hitPosition)
    {
        if (actorEventManger != null)
        {
            actorEventManger.TriggerAnim_Stagger(hitPosition);
            actorEventManger.TakeWeaponHit(weapon, hitPosition);
        }
    }
    void onBulletHit(Projectile projectile, HitPositionType hitPosition)
    {
        if (actorEventManger != null)
        {
            actorEventManger.TriggerAnim_Stagger(hitPosition);
            actorEventManger.TakeBulletHit(projectile, hitPosition);
        }
    }

    private void OnTriggerEnter(Collider col)
    {
        Debug.Log("onTriggerEnter: " + gameObject.name + "| Collider: " + col.gameObject.name);


        if (col.CompareTag("WeaponHitBox"))
        {
            Debug.Log("onTriggerEnter: " + gameObject.name + "| Collider: " + col.gameObject.name);

            WeaponCollider weaponCollider = col.GetComponent<WeaponCollider>();
            if (weaponCollider.weapon != null)
            {
                if (weaponCollider.weapon._ownerRefId == actor.refId)
                {
                    Debug.Log("Ignore my own weapon hit" + weaponCollider.weapon + " On " + gameObject.name);
                    return;
                }

                // if (weaponCollider.weapon.attackblockedList.Contains(actor.refId))
                // {
                //     Debug.Log("Ignore Blocked Hit: " + weaponCollider.weapon + " On " + gameObject.name);
                //     return;
                // }

                Debug.Log("Weapon Hit: " + weaponCollider.weapon + " On " + gameObject.name);
                onWeaponHit(weaponCollider.weapon, bodyPosition);
            }
            else
            {
                Debug.Log("Non-weapon hit: " + col + " On " + gameObject.name);
            }
        }
        else if (col.CompareTag("BulletHitBox") || col.CompareTag("Projectile"))
        {
            // WeaponCollider weaponCollider = col.GetComponent<WeaponCollider>();
            Projectile projectile = col.GetComponent<Projectile>();
            if (projectile.projectileType == ProjectileType.spell)
            {
                Debug.Log("ProjectileType: Spell");
                projectile.InvokeSpells(projectile.sender, actor.gameObject);
            }
            else
            {
                onBulletHit(projectile, bodyPosition);
            }
        }

    }
}
