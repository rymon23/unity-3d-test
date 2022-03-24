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
    public string _ownerRefId = "-";
    public WeaponType weaponType;
    public EquipSlotType equipSlotType;
    public ItemType itemType = 0;

    public WeaponCollider weaponCollider;
    [SerializeField] private BoxCollider boxCollider;

    public float damage = 0;
    public bool canParry = true;

    public HashSet<string> attackblockedList;

    void Awake()
    {
        weaponCollider = this.gameObject.GetComponentInChildren<WeaponCollider>();
        if (weaponCollider != null)
        {
            weaponCollider.weapon = this;
            boxCollider = this.gameObject.GetComponentInChildren<BoxCollider>();
            DisableWeaponCollider();
        }
    }

    private void Start()
    {
        // attackblockedList = new HashSet<string>();
    }

    public void EnableWeaponCollider()
    {
        boxCollider.enabled = true;

        // weaponCollider.gameObject.SetActive(true);

        // weaponCollider.transform.SetParent(this.transform);
        // weaponCollider.transform.localPosition = Vector3.zero; // Or desired position
        // weaponCollider.transform.rotation = Quaternion.Euler(0, 0, 0);
    }
    public void DisableWeaponCollider()
    {
        boxCollider.enabled = false;

        // weaponCollider.gameObject.SetActive(false);
    }
}
