using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TeamEntity : MonoBehaviour
{
    [SerializeField]
    private Faction faction;

    public HashSet<int> territories;

    public HashSet<int> armies;
}
