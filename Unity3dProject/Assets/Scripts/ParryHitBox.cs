using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(
    typeof(BoxCollider))]
public class ParryHitBox : MonoBehaviour
{
    public BoxCollider col;
    public Animator animator;
    public AnimationState animationState;

    private void Awake() {
        col = GetComponent<BoxCollider>();
        animator = GetComponentInParent<Animator>();
        animationState = GetComponentInParent<AnimationState>();
    }

    private void OnTriggerEnter(Collider col) {
        Debug.Log("onTriggerEnter: " + gameObject.name + "| Collider: " + col.gameObject.name);
        if (col.CompareTag("WeaponHitBox") && animationState.isBlocking) {

            animator.SetTrigger("BlockHit");
            
            WeaponCollider weaponCollider = col.GetComponent<WeaponCollider>();

            if (weaponCollider?.weapon != null) {
                Debug.Log("Blocked Hit from " + weaponCollider.weapon + " On "+ gameObject.name);
            } else {
                Debug.Log("Blocked Hit from " + col + " On "+ gameObject.name);
            }
        }
    }
}
