using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Hybrid.Components;

[RequireComponent(
    typeof(BoxCollider))]
public class ParryHitBox : MonoBehaviour
{
    [SerializeField] IsActor actor;
    public BoxCollider col;
    public Animator animator;
    public AnimationState animationState;
    public ActorEventManger actorEventManger;

    private void Awake()
    {
        col = GetComponent<BoxCollider>();
        actor = GetComponentInParent<IsActor>();
        animator = GetComponentInParent<Animator>();
        animationState = GetComponentInParent<AnimationState>();
        actorEventManger = GetComponentInParent<ActorEventManger>();
    }

    void onHitBlocked(float fDamage)
    {
        if (actorEventManger != null) actorEventManger.BlockHit(fDamage);
    }

    private void OnTriggerEnter(Collider col)
    {
        Debug.Log("onTriggerEnter: " + gameObject.name + "| Collider: " + col.gameObject.name);
        if (col.CompareTag("WeaponHitBox") )//&& animationState.isBlocking)
        {
            int currentBlockVariant = ((int)animator.GetFloat("animBlockType"));
            int maxBlendTreeLength = 6;
            int nextBlockType = (currentBlockVariant + UnityEngine.Random.Range(1, maxBlendTreeLength)) % maxBlendTreeLength;

            WeaponCollider weaponCollider = col.GetComponent<WeaponCollider>();
            Weapon weapon = weaponCollider.weapon;

            if (weapon != null)
            {
                if (weapon._ownerRefId == actor.refId)
                {
                    Debug.Log("Ignore Block Hit from my own weapon " + weapon + " On " + gameObject.name);
                    return;
                }

                weapon.DisableWeaponCollider();

                animator.SetFloat("animBlockType", nextBlockType);
                animator.SetTrigger("BlockHit");
                onHitBlocked(10f);
                // weapon.attackblockedList.Add(actor.refId);

                Debug.Log("Blocked Hit from " + weaponCollider.weapon + " On " + gameObject.name);
            }
            else
            {
                Debug.Log("Blocked Hit from " + col + " On " + gameObject.name);
            }
        }
    }
}
