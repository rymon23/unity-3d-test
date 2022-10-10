using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerritoryManager : MonoBehaviour
{
    public static TerritoryManager current;

    public static bool bDebug = true;

    [SerializeField]
    private GameObject[] territoryGameObjects;

    void Start()
    {
    }

    public Territory
    GetClosestTerritory(Transform transform, Faction faction = null)
    {
        Territory result = null;
        float closestTargetDistance = -1;
        bool checkFaction = faction != null;

        foreach (GameObject item in territoryGameObjects)
        {
            Territory territory = item.GetComponent<Territory>();
            if (!checkFaction || territory.owner == faction)
            {
                float distance =
                    Vector3
                        .Distance(transform.position, item.transform.position);
                if (
                    closestTargetDistance == -1 ||
                    closestTargetDistance > distance
                )
                {
                    result = territory;
                    closestTargetDistance = distance;
                }
            }
        }
        return result;
    }
}
