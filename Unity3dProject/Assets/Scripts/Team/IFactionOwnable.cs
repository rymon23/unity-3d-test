using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IFactionOwnable
{
    Faction owner { get; }

    Faction lastOwner { get; }
}
