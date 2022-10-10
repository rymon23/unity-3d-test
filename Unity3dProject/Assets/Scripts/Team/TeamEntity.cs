using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum TerritoryBehavior
{
    idle = 0,
    defend
}

public enum TeamEntityTerritorialState
{
    idle = 0,
    expand,
    constrict
}

public class TeamEntity : MonoBehaviour
{
    [SerializeField]
    private Faction faction;

    public HashSet<int> territories;

    public HashSet<int> armies;

    [SerializeField]
    private TeamEntity priorityTarget;

    [SerializeField]
    private TeamEntityTerritorialState
        territorialState = TeamEntityTerritorialState.expand;

    [SerializeField]
    private List<TeamEntity> allies;

    [SerializeField]
    private List<TeamEntity> enemies;

    private void EvaluateProcesses()
    {
        if (priorityTarget == null)
        {
            if (enemies?.Count > 0)
                UpdatePriorityTarget(GetRandomEnemyNeighbor());
            EvaluateEnemies();
        }
        else
        {
        }
    }

    private void EvaluateEnemies()
    {
    }

    private void Start()
    {
        allies = new List<TeamEntity>();
        enemies = new List<TeamEntity>();

        // if (actorEventManger != null)
        // {
        //     actorEventManger.onActorDeath += OnMemberDeath;
        // }
        EvaluateProcesses();
    }

    private void UpdatePriorityTarget(TeamEntity newTarget)
    {
        if (newTarget != null)
        {
            priorityTarget = newTarget;
        }
    }

    private TeamEntity GetRandomEnemyNeighbor()
    {
        return enemies[Random.Range(0, enemies.Count)];
    }
}
// objectives:
//     - defend
//         - destroyEnemies
//     - expand
//         - findNewTerritory
//         - aquireNewTerritory
//     - contract
//         - consolidateTerritory
//     - constant
//         - extract
//         - fortify
