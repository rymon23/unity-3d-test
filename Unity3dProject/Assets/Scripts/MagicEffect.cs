using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum SpellCastingType
{
    hold = 0,
    fire = 1,
    constant = 2,
}

public enum SpellDeliveryType
{
    self = 0,
    contact = 1,
    aimed = 2,
    target_actor = 3,
    target_location = 4
}

public class MagicEffect : MonoBehaviour //, IMagicEffect
{
    public bool hostile = true;
    [SerializeField] private SpellCastingType _castingType = SpellCastingType.fire;
    public SpellCastingType castingType
    {
        get => _castingType;
    }
    [SerializeField] private SpellDeliveryType _deliveryType = SpellDeliveryType.aimed;
    public SpellDeliveryType deliveryType
    {
        get => _deliveryType;
    }

}