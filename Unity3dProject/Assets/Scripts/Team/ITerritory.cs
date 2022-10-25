using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ITerritory
{
    int totalReinforcements { get; }

    int morale { get; }

    int influence { get; }

    int value { get; }
}
