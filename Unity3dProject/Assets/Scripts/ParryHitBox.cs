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
    public Animator animator;
    public AnimationState animationState;
    public ActorEventManger actorEventManger;

    private void Awake()
    {
        col = GetComponent<BoxCollider>();
        actor = GetComponentInParent<IsActor>();
        actorFOV = GetComponentInParent<ActorFOV>();
        animator = GetComponentInParent<Animator>();
        animationState = GetComponentInParent<AnimationState>();
        actorEventManger = GetComponentInParent<ActorEventManger>();
    }

    void onHitBlocked(float fDamage)
    {
        if (actorEventManger != null) {
            actorEventManger.TriggerAnim_Block();
            actorEventManger.BlockHit(fDamage);
        }
    }

    private void OnTriggerEnter(Collider col)
    {
        // Debug.Log("onTriggerEnter: " + gameObject.name + "| Collider: " + col.gameObject.name);
        if (col.CompareTag("WeaponHitBox"))//&& animationState.isBlocking)
        {
            // int currentBlockVariant = ((int)animator.GetFloat("fAnimBlockType"));
            // int maxBlendTreeLength = 6;
            // int nextBlockType = (currentBlockVariant + UnityEngine.Random.Range(1, maxBlendTreeLength)) % maxBlendTreeLength;

            WeaponCollider weaponCollider = col.GetComponent<WeaponCollider>();
            Weapon weapon = weaponCollider.weapon;
            Debug.Log("WeaponHitBox found on " + weapon + " On " + gameObject.name);

            bool hasTargetInFOV = UtilityHelpers.IsInFOVScope(actor.transform, weapon.transform.position, actorFOV.maxAngle, actorFOV.maxRadius);
            if (!hasTargetInFOV)
            {
                Debug.Log("Ignore Block from ourside FOV " + weapon + " On " + gameObject.name);
                return;
            }

            if (weapon != null)
            {
                if (weapon._ownerRefId == actor.refId)
                {
                    Debug.Log("Ignore Block Hit from my own weapon " + weapon + " On " + gameObject.name);
                    return;
                }

                weapon.DisableWeaponCollider();

                // animator.SetFloat("animBlockType", nextBlockType);
                // animator.SetTrigger("BlockHit");
                onHitBlocked(10f);

                Debug.Log("Blocked Hit from " + weaponCollider.weapon + " On " + gameObject.name);
            }
            else
            {
                Debug.Log("Blocked Hit from " + col + " On " + gameObject.name);
            }
        }
    }
}
