using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(BoxCollider))]
public class Hitbox : MonoBehaviour
{
    [SerializeField] private UnityEvent<Weapon> hitEvent;
    public BoxCollider col;

    private void Awake() {
        col = GetComponent<BoxCollider>();

        // if (hitEvent == null)
        //     hitEvent = new UnityEvent<Weapon>();
        //     hitEvent.AddListener();

        // combatEvent = GetComponent<CombatEvent>();
    }

    // private void OnAttackHit(Weapon weapon) {
    //     Debug.Log("")
    // }

    private void OnTriggerEnter(Collider col) {
        Debug.Log("onTriggerEnter: " + gameObject.name + "| Collider: " + col.gameObject.name);
        if (col.CompareTag("WeaponHitBox")) {
            WeaponCollider weaponCollider = col.GetComponent<WeaponCollider>();
            if (weaponCollider?.weapon != null) {
                Debug.Log("Weapon Hit: " + weaponCollider.weapon + " On "+ gameObject.name);
                // hitEvent?.Invoke(weaponCollider.weapon);
            } else {
                Debug.Log("Weapon Hit: " + col + " On "+ gameObject.name);
            }
        }
    }
}
