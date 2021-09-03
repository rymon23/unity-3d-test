using UnityEngine;

public enum HandEquipSide
{
    rightHand = 0,
    leftHand = 1
}

public class EquipSlotController : MonoBehaviour
{

    public HandEquipSlot[] handEquipSlots;
    public HandEquipSide activeHand = 0;

    void Start()
    {
        if (handEquipSlots == null) {
            handEquipSlots = new HandEquipSlot[2];
        }
    }
}
