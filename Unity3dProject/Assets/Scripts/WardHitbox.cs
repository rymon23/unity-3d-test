using Hybrid.Components;
using UnityEngine;

[RequireComponent(typeof (BoxCollider))]
public class WardHitbox : MonoBehaviour
{
    [SerializeField]
    IsActor actor;

    [SerializeField]
    ActorFOV actorFOV;

    public BoxCollider col;

    public AnimationState animationState;

    public ActorEventManger actorEventManger;

    private void Awake()
    {
        col = GetComponent<BoxCollider>();
        actor = GetComponentInParent<IsActor>();
        actorFOV = GetComponentInParent<ActorFOV>();
        animationState = GetComponentInParent<AnimationState>();
        actorEventManger = GetComponentInParent<ActorEventManger>();
    }

    void onHitWarded(Weapon weapon)
    {
        if (actorEventManger != null)
        {
            actorEventManger.TriggerAnim_Block();
            actorEventManger.BlockWeaponHit (weapon);
        }
    }

    public void onHitWarded(Projectile projectile)
    {
        if (actorEventManger != null)
        {
            actorEventManger.TriggerAnim_Block();
            actorEventManger.BlockProjectileHit (projectile);
        }
    }

    private void OnTriggerEnter(Collider col)
    {
        Debug
            .Log("WardHitbox => onTriggerEnter: " +
            gameObject.name +
            "| Collider: " +
            col.gameObject.name);

        if (col.CompareTag("BulletHitBox") || col.CompareTag("Projectile"))
        {
            Projectile projectile = col.GetComponent<Projectile>();
            if (projectile.sender == actor.gameObject)
            {
                Debug.Log("Ward allow pass from " + gameObject.name);
            }
            else
            {
                Destroy(projectile.gameObject);
                Debug.Log("Warded hit from " + gameObject.name);
            }
        }
        else if (
            col.CompareTag("WeaponHitBox") //&& animationState.isBlocking)
        )
        {
            WeaponCollider weaponCollider = col.GetComponent<WeaponCollider>();
            Weapon weapon = weaponCollider.weapon;

            if (weapon != null)
            {
                if (weapon._ownerRefId == actor.refId)
                {
                    Debug
                        .Log("Ignore Block Hit from my own weapon " +
                        weapon +
                        " On " +
                        gameObject.name);
                    return;
                }

                weapon.DisableWeaponCollider();

                // onHitWarded(weapon);
                Debug
                    .Log("Warded Hit from " +
                    weaponCollider.weapon +
                    " On " +
                    gameObject.name);
            }
        }
    }
}
