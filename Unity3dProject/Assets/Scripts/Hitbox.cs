using Hybrid.Components;
using UnityEngine;

public enum HitPositionType
{
    body = 0,
    head = 1
}

public enum HitDirectionType
{
    front = 0,
    back = 1,
    side = 2
}

[RequireComponent(typeof (BoxCollider))]
public class Hitbox : MonoBehaviour
{
    [SerializeField]
    IsActor actor;

    [SerializeField]
    GameObject myGameObject;

    [SerializeField]
    BoxCollider myCollider;

    [SerializeField]
    ActorEventManger actorEventManger;

    [SerializeField]
    HitPositionType bodyPosition = 0;

    [SerializeField]
    bool bIsPlayer = false;

    private void Awake()
    {
        actor = GetComponentInParent<IsActor>();
        actorEventManger = GetComponentInParent<ActorEventManger>();
        myGameObject = GetComponentInParent<IsActor>().gameObject;
        myCollider = GetComponent<BoxCollider>();
    }

    public void onWeaponHit(Weapon weapon)
    {
        Debug
            .Log("onWeaponHit - Weapon: " +
            weapon +
            ", hitPosition: " +
            bodyPosition);

        if (weapon._ownerRefId == actor.refId) return;

        if (actorEventManger != null)
        {
            actorEventManger.TriggerAnim_Stagger (bodyPosition);
            actorEventManger.TakeWeaponHit (weapon, bodyPosition);

            if (bIsPlayer) actorEventManger.RumbleFire();
        }
    }

    public void onBulletHit(Weapon weapon, Projectile projectile = null)
    {
        if (actorEventManger != null)
        {
            actorEventManger.TriggerAnim_Stagger (bodyPosition);
            actorEventManger
                .TakeBulletHit(weapon,
                bodyPosition,
                projectile ? projectile : weapon.GetDefaultProjectile());

            if (bIsPlayer) actorEventManger.RumbleFire();
        }
    }

    public void onSpellHit(MagicSpell magicSpell, GameObject sender)
    {
        if (actorEventManger != null)
        {
            actorEventManger.TriggerAnim_Stagger (bodyPosition);

            // actorEventManger
            //     .TakeBulletHit(weapon,
            //     bodyPosition,
            //     projectile ? projectile : weapon.GetDefaultProjectile());
            if (bIsPlayer) actorEventManger.RumbleFire();

            Transform spell = Instantiate(magicSpell.spellPrefab.transform);
            ActiveSpellController spellController =
                spell.gameObject.GetComponent<ActiveSpellController>();
            if (spellController != null)
            {
                spellController
                    .FireMagicEffects(new SpellInstanceData(sender,
                        actor.gameObject,
                        ProjectileType.spell,
                        magicSpell));
            }
        }
    }

    private void OnTriggerEnter(Collider col)
    {
        Debug
            .Log("onTriggerEnter - Object: " +
            gameObject.name +
            ", Collider: " +
            col.gameObject.name +
            ", Tag: " +
            col.tag);

        if (col.CompareTag("WeaponHitBox"))
        {
            WeaponCollider weaponCollider = col.GetComponent<WeaponCollider>();
            Debug
                .Log("Weapon HitBox: " +
                col.gameObject.name +
                ", Owner: " +
                weaponCollider.weapon._ownerRefId);

            if (weaponCollider.weapon != null)
            {
                if (weaponCollider.weapon._ownerRefId == actor.refId)
                {
                    Debug
                        .Log("Ignore my own weapon hit" +
                        weaponCollider.weapon +
                        " On " +
                        gameObject.name);
                    return;
                }

                // if (weaponCollider.weapon.attackblockedList.Contains(actor.refId))
                // {
                //     Debug.Log("Ignore Blocked Hit: " + weaponCollider.weapon + " On " + gameObject.name);
                //     return;
                // }
                Debug
                    .Log("Weapon Hit: " +
                    weaponCollider.weapon +
                    " On " +
                    gameObject.name);
                onWeaponHit(weaponCollider.weapon);

                // TODO IMPROVE THIS:
                IsActor attacker =
                    weaponCollider
                        .transform
                        .root
                        .GetComponentInChildren<IsActor>();
                if (attacker != null && attacker.IsPlayer)
                {
                    ActorEventManger attackerEventManager =
                        attacker.gameObject.GetComponent<ActorEventManger>();
                    attackerEventManager.RumbleFire();
                }
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
                projectile.FireMagicEffects(actor.gameObject);
            }
            else
            {
                onBulletHit(projectile.weapon, projectile);
            }
        }
    }
}
