using UnityEngine;
using System.Collections.Generic;

public enum HandEquipSide
{
    rightHand = 0,
    leftHand = 1
}

public class EquipSlotController : MonoBehaviour
{
    [SerializeField] private HandEquipSlot[] handEquipSlots = new HandEquipSlot[2];

    public HandEquipSide activeHand = HandEquipSide.rightHand;
    public Weapon desiredWeapon;
    public int activeSocket = -1;

    public HandEquipSlot RightHandEquipSlot() => handEquipSlots[(int)HandEquipSide.rightHand];
    public HandEquipSlot LeftHandEquipSlot() => handEquipSlots[(int)HandEquipSide.leftHand];
    private ActorEventManger actorEventManger;
    private MeshSockets sockets;
    [SerializeField] private Dictionary<MeshSockets.SocketId, Item> wornItems = new Dictionary<MeshSockets.SocketId, Item>();
    private Dictionary<WeaponType, List<Weapon>> wornWeaponsByType = new Dictionary<WeaponType, List<Weapon>>();


    // public Dictionary<WeaponType, List<Item>> EvaluateWornWeapons()
    // {
    //     Dictionary<WeaponType, List<Item>> result = new Dictionary<WeaponType, List<Item>>();

    //     foreach (Item item in wornItems.Values)
    //     {
    //         GameObject gm = item.GetPrefab();
    //         if (gm != null)
    //         {
    //             Weapon weapon = gm.GetComponent<Weapon>();
    //             if (!result.ContainsKey(weapon.weaponType)) result.Add(weapon.weaponType, new List<Item>());

    //             if (!result[weapon.weaponType].Contains(item))
    //             {
    //                 result[weapon.weaponType].Add(item);
    //             }
    //         }
    //     }
    //     wornWeaponsByType = result;
    //     return result;
    // }
    public Weapon GetWornWeaponOfType(WeaponType type)
    {
        if (wornWeaponsByType.ContainsKey(type))
        {
            return wornWeaponsByType[type][0];
        }
        return null;
    }


    public void UnequipWeapon(HandEquipSide handEquipSide = HandEquipSide.rightHand)
    {
        GameObject currentWeaponObj = handEquipSlots[(int)handEquipSide].weapon.gameObject;
        if (currentWeaponObj != null)
        {
            Destroy(currentWeaponObj);
        }
    }

    public void EquipWeapon(GameObject weap, HandEquipSide handEquipSide = HandEquipSide.rightHand)
    {
        UnequipWeapon(handEquipSide);

        if (weap != null)
        {
            Weapon weapon = weap.GetComponent<Weapon>();
            handEquipSlots[(int)handEquipSide].weapon = weapon;
            // weapon.transform.SetParent(handEquipSlots[(int)handEquipSide].transform, false);

            if (sockets != null)
            {
                sockets.Attach(weapon.transform, MeshSockets.SocketId.Spine);
            }
        }
    }

    public void UpdateDesiredWeapon(Weapon weapon, HandEquipSide handEquipSide = HandEquipSide.rightHand)
    {
        if (weapon != null)
        {
            desiredWeapon = weapon;
            activeHand = handEquipSide;
            actorEventManger.TriggerAnim_Sheath();
        }
    }
    public void UpdateActiveWeapon()
    {
        if (desiredWeapon != null)
        {
            Debug.Log("UpdateActiveWeapon");
            handEquipSlots[(int)activeHand].weapon = desiredWeapon;
        }
    }

    private void AttachWeapon(GameObject obj, Item item, HandEquipSide handEquipSide = HandEquipSide.rightHand)
    {
        // UnequipWeapon(handEquipSide);

        if (obj != null)
        {
            Weapon weapon = obj.GetComponent<Weapon>();
            handEquipSlots[(int)handEquipSide].weapon = weapon;
            if (sockets != null)
            {
                SetWorn(item, true, weapon);
                sockets.Attach(weapon.transform, item.equipSocketId);
            }
        }
    }

    public void DrawWeapon()
    {
        Weapon weapon = handEquipSlots[(int)HandEquipSide.rightHand].weapon;
        if (sockets != null && weapon != null)
        {
            MeshSockets.SocketId newSocket = (MeshSockets.SocketId)MeshSockets.GetWeaponSocketType(weapon, HandEquipSide.rightHand);
            sockets.Attach(weapon.transform, newSocket);
            activeSocket = (int)newSocket;
            
            // if (weapon.weaponType == WeaponType.sword)
            // {
            //     sockets.Attach(weapon.transform, MeshSockets.SocketId.MeleeRight);
            //     activeSocket = (int)MeshSockets.SocketId.MeleeRight;
            // }
            // else
            // {
            //     sockets.Attach(weapon.transform, MeshSockets.SocketId.RightHand);
            //     activeSocket = (int)MeshSockets.SocketId.RightHand;
            // }
        }
    }
    public void SheatheWeapon()
    {
        Weapon weapon = handEquipSlots[(int)HandEquipSide.rightHand].weapon;
        if (sockets != null && weapon != null)
        {
            sockets.Attach(weapon.transform, weapon.item.equipSocketId);
            activeSocket = -1;
            // sockets.Attach(weapon.transform, MeshSockets.SocketId.Spine);
        }
    }

    public bool IsWornItem(Item item)
    {
        return wornItems.ContainsValue(item); // wornItems[item.equipSocketId].GetInstanceID() == item.GetInstanceID();
    }
    public bool IsWearSlotFilled(MeshSockets.SocketId slot)
    {
        return wornItems.ContainsKey(slot) && wornItems[slot] != null;
    }

    public void SetWorn(Item item, bool wear, Weapon weapon)
    {
        if (wear)
        {
            wornItems.Add(item.equipSocketId, item);

            if (!wornWeaponsByType.ContainsKey(weapon.weaponType)) wornWeaponsByType.Add(weapon.weaponType, new List<Weapon>());
            if (!wornWeaponsByType[weapon.weaponType].Contains(weapon)) wornWeaponsByType[weapon.weaponType].Add(weapon);
        }
        else
        {
            wornItems.Remove(item.equipSocketId);

            if (wornWeaponsByType.ContainsKey(weapon.weaponType) && wornWeaponsByType[weapon.weaponType].Contains(weapon))
                wornWeaponsByType[weapon.weaponType].Remove(weapon);
        }
    }


    public void OnWeaponItemAdded(Item item)
    {
        Debug.Log("EquipSlotController => OnWeaponItemAdded");

        if (item != null && !IsWearSlotFilled(item.equipSocketId))
        {
            GameObject prefab = item.GetPrefab();
            if (prefab != null)
            {
                GameObject obj = Instantiate(prefab);
                AttachWeapon(obj, item);
            }
        }
    }

    private void Start()
    {
        sockets = GetComponent<MeshSockets>();
        actorEventManger = GetComponent<ActorEventManger>();
        if (actorEventManger != null) actorEventManger.OnWeaponItemAdded += OnWeaponItemAdded;
    }
    private void OnDestroy()
    {
        if (actorEventManger != null) actorEventManger.OnWeaponItemAdded -= OnWeaponItemAdded;
    }

    [SerializeField] private bool bEquipItem = false;
    private void FixedUpdate()
    {
        if (bEquipItem)
        {
            bEquipItem = false;

            ActorInventory actorInventory = GetComponent<ActorInventory>();
            if (actorInventory)
            {
                GameObject prefab = actorInventory.GetFirstWeapon();
                GameObject weapon = Instantiate(prefab);
                EquipWeapon(weapon);
                // Invoke(nameof(DrawWeapon), 2f);
            }

        }
    }


}
