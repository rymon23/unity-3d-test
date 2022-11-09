using UnityEngine;

[
    CreateAssetMenu(
        fileName = "New Faction Directory",
        menuName = "Faction Directory")
]
public class FactionDirectory : ScriptableObject
{
    public new string name;

    public Faction[] factions;
}
