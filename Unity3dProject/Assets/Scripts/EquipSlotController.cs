using UnityEngine;

public enum HandEquipSide
{
    rightHand = 0,
    leftHand = 1
}

public class EquipSlotController : MonoBehaviour
{
    public HandEquipSlot[] handEquipSlots = new HandEquipSlot[2];
    public HandEquipSide activeHand = 0;
}
