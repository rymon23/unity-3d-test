using UnityEngine;

public class FactionsManager : MonoBehaviour
{
    public static FactionsManager current;

    public Faction[] factions;

    private void Awake()
    {
        SetupFactionRelations();
    }

    private void SetupFactionRelations()
    {
        Debug.Log("Faction Manager - factions found: " + factions.Length);

        if (factions != null && factions.Length > 0)
        {
            for (int i = 0; i < factions.Length; i++)
            {
                factions[i].SetupRelations();
            }
        }

        Debug.Log("Faction Manager: Faction Relations Setup");
    }
}
