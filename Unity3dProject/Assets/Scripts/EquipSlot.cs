using UnityEngine;


public enum ItemType
{
    weapon = 0,
    gear = 1,
    throwable = 2,
    consumable = 3,
    other = 4
}
public enum EquipSlotType
{
    rightHand = 0,
    leftHand = 1,
    bothHands = 2,
    eitherHand = 3
}
public enum WeaponWearSlot
{
    sword = 0,
    axe = 1,
    rifle = 2,
    dagger,
    bow,
    pistol,
    pack
}

public class EquipSlot : MonoBehaviour
{

    void Start()
    {

    }

}
