using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Army : MonoBehaviour
{
    [SerializeField]
    private Faction faction;

    public int wins { get; private set; } = 0;

    public int losses { get; private set; } = 0;

    public float successRate { get; private set; } = 0;

    public HashSet<int> territories;
}
