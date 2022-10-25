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
            Vector4 color = Vector4.zero;

            for (int i = 0; i < factions.Length; i++)
            {
                factions[i].AssigneColor(color);
                factions[i].SetupRelations();

                color +=
                    new Vector4(Random.Range(0.0f, 0.15f),
                        Random.Range(0.0f, 0.25f),
                        Random.Range(0.0f, 0.25f),
                        1f);
            }
        }

        Debug.Log("Faction Manager: Faction Relations Setup");
    }
}
