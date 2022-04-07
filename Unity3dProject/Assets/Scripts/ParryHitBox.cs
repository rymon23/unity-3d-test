using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Hybrid.Components;

[RequireComponent(
    typeof(BoxCollider))]
public class ParryHitBox : MonoBehaviour
{
    [SerializeField] IsActor actor;
    [SerializeField] ActorFOV actorFOV;
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

    void onHitBlocked(Weapon weapon)
    {
        if (actorEventManger != null)
        {
            actorEventManger.TriggerAnim_Block();
            actorEventManger.BlockWeaponHit(weapon);
        }
    }
    public void onHitBlocked(Projectile projectile)
    {
        if (actorEventManger != null)
        {
            actorEventManger.TriggerAnim_Block();
            actorEventManger.BlockProjectileHit(projectile);
        }
    }

    // private void OnTriggerExit(Collider other)
    // {
    //     Debug.Log("OnTriggerExit: " + gameObject.name + "| Collider: " + col.gameObject.name);

    //     if (col.CompareTag("BulletHitBox"))
    //     {
    //         Projectile projectile = col.GetComponent<Projectile>();
    //         onHitBlocked(projectile);
    //         Destroy(projectile.gameObject);
    //         Debug.Log("Blocked hit from " + gameObject.name);
    //     }
    // }

    private void OnTriggerEnter(Collider col)
    {
        Debug.Log("onTriggerEnter: " + gameObject.name + "| Collider: " + col.gameObject.name);

        if (col.CompareTag("BulletHitBox"))
        {
            Projectile projectile = col.GetComponent<Projectile>();
            onHitBlocked(projectile);
            Destroy(projectile.gameObject);
            Debug.Log("Blocked hit from " + gameObject.name);

        }
        else if (col.CompareTag("WeaponHitBox"))//&& animationState.isBlocking)
        {
            WeaponCollider weaponCollider = col.GetComponent<WeaponCollider>();
            Weapon weapon = weaponCollider.weapon;
            Debug.Log("WeaponHitBox found on " + weapon + " On " + gameObject.name);

            // bool hasTargetInFOV = UtilityHelpers.IsInFOVScope(actor.transform, weapon.transform.position, actorFOV.maxAngle, actorFOV.maxRadius);
            // if (!hasTargetInFOV)
            // {
            //     Debug.Log("Ignore Block from ourside FOV " + weapon + " On " + gameObject.name);
            //     return;
            // }

            if (weapon != null)
            {
                if (weapon._ownerRefId == actor.refId)
                {
                    Debug.Log("Ignore Block Hit from my own weapon " + weapon + " On " + gameObject.name);
                    return;
                }

                weapon.DisableWeaponCollider();
                onHitBlocked(weapon);

                Debug.Log("Blocked Hit from " + weaponCollider.weapon + " On " + gameObject.name);
            }
            else
            {
                Debug.Log("Blocked Hit from " + col + " On " + gameObject.name);
            }
        }
    }
}
