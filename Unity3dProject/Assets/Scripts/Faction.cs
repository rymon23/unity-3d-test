using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public enum FactionRelationship
{
    unset = -2,
    none = -1,
    ally = 0,
    enemy = 1,
}


[CreateAssetMenu(fileName = "New Faction", menuName = "Faction")]
public class Faction : ScriptableObject
{
    // [SerializeField] private System.Guid _factionId = System.Guid.NewGuid();
    public new string name;

    public HashSet<int> enemyRelations;
    public HashSet<int> allyRelations;

    // PUBLIC FACING FOR THE EDITOR 
    // TO DO: Find a better way
    public Faction[] allies;
    public Faction[] enemys;


    public static FactionRelationship GetFactionRelationship(Faction factionA, Faction factionB)
    {
        Debug.Log("FactionA enemies: " + factionA.enemyRelations.Count);
        Debug.Log("FactionA allies: " + factionA.allyRelations.Count);

        if (factionA.enemyRelations.Contains(factionB.GetInstanceID())) return FactionRelationship.enemy;
        if (factionA.allyRelations.Contains(factionB.GetInstanceID())) return FactionRelationship.ally;
        return FactionRelationship.none;
    }

    public static FactionRelationship GetMultiFactionRelationship(Faction[] factionsA, Faction[] factionsB)
    {
        FactionRelationship relation = FactionRelationship.none;
        for (int i = 0; i < factionsA.Length; i++)
        {
            Faction factionA = factionsA[i];
            for (int j = 0; j < factionsB.Length; j++)
            {
                Faction factionB = factionsB[j];
                Debug.Log("FactionA: " + factionA);
                Debug.Log("FactionB: " + factionB);

                relation = GetFactionRelationship(factionA, factionB);
                if (relation == FactionRelationship.enemy) break;
            }
        }
        return relation;
    }


    public void SetupRelations()
    {
        if (allyRelations == null) allyRelations = new HashSet<int>();
        if (enemyRelations == null) enemyRelations = new HashSet<int>();

        if (allies != null && allies.Length > 0)
        {
            foreach (var allyFaction in allies)
            {
                allyRelations.Add(allyFaction.GetInstanceID());
            }
        }
        if (enemys != null && enemys.Length > 0)
        {
            foreach (var enemyFaction in allies)
            {
                allyRelations.Add(enemyFaction.GetInstanceID());
            }
        }
        Debug.Log("Faction: SetupRelations!");
    }

    private void Awake()
    {
        Debug.Log("Faction: Awake");

        SetupRelations();
    }
}