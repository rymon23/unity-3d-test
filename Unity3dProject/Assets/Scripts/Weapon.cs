using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum WeaponType
{
    sword = 0,
    gun,
    bow,
}

public class Weapon : MonoBehaviour
{
    public WeaponType weaponType;
    public EquipSlotType equipSlotType;
    public ItemType itemType = 0;

    public WeaponCollider weaponCollider;
    
    public float damage = 0;
    public bool canParry = true;

    void Start()
    {
        weaponCollider = GetComponentInChildren<WeaponCollider>();
        if (weaponCollider != null) {
            weaponCollider.weapon = this;
            // DisableWeaponCollider();
        }
    }

    public void EnableWeaponCollider() {
        if (weaponCollider != null) {
            weaponCollider.gameObject.SetActive(true);
        }
    }
    public void DisableWeaponCollider() {
        if (weaponCollider != null) {
            weaponCollider.gameObject.SetActive(false);
        }
    }
}
