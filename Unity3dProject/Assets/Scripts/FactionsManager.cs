using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FactionsManager : MonoBehaviour
{
    public static FactionsManager current;
    public Faction[] factions;

    private void Awake()
    {
        if (factions != null && factions.Length > 0)
        {

            for (int i = 0; i < factions.Length; i++)
            {
                factions[i].SetupRelations();
            }
        }
    }
}
