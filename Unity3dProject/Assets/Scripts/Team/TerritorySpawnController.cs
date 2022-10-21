using System.Collections.Generic;
using Hybrid.Components;
using UnityEngine;

public class TerritorySpawnController : MonoBehaviour
{
    [SerializeField]
    private Territory territory;

    [SerializeField]
    private bool keepSpawning = true;

    [SerializeField]
    private bool bForceAssignFaction = false;

    [SerializeField]
    private int spawnLimit = 4;

    public int GetSpawnLimit() => spawnLimit;

    [SerializeField]
    private float radius = 32f;

    [SerializeField]
    private int teamGroupCount = 0;

    [SerializeField]
    private Transform teamGroupPrefab;

    [SerializeField]
    private Transform[] spawnMarkers;

    public Faction[] factions;

    [SerializeField]
    private SpawnPoint[] spawnPoints;

    public int currentSpawnCount = 0;

    public int deaths = 0;

    public bool bAssignTeamGroup = true;

    public bool bHoldPosition = true;

    [SerializeField]
    public float delayStart = 1f;

    [SerializeField]
    private Vector4 assignedColor = Vector4.zero;

    public void SetAsignedColor(Vector4 newColor)
    {
        assignedColor = newColor;
    }

    private void OnDrawGizmos()
    {
        if (territory != null) radius = territory.GetRadius();
        if (assignedColor != Vector4.zero)
        {
            Gizmos.color = assignedColor;
        }
        Gizmos.DrawWireSphere(this.transform.position, radius);
    }

    public GameObject[] prefabs;

    public List<GameObject> spawns;

    private void ScanForSpawnPoints()
    {
        SpawnPoint[] found =
            this.transform.parent.GetComponentsInChildren<SpawnPoint>();
        spawnPoints = new SpawnPoint[found.Length];
        if (found != null && found.Length > 0)
        {
            for (int i = 0; i < found.Length; i++)
            {
                spawnPoints[i] = found[i];
            }
        }
    }

    private GameObject RandomPrefab()
    {
        return prefabs[Random.Range(0, prefabs.Length)];
    }

    public SpawnPoint RandomSpawnPoint()
    {
        // int ix = prefabs.Length;
        // int x = Random.Range(0, ix);
        // Debug.Log("RandomPrefab: " + x);
        return spawnPoints[Random.Range(0, spawnPoints.Length)];
    }


#region TeamGroup Management

    [SerializeField]
    private List<TeamGroup> teamGroups;

    [SerializeField]
    private int teamGroupAttackers = 0;

    [SerializeField]
    private int teamGroupDefenders = 0;

    [SerializeField]
    private int teamGroupDeaths = 0;

    public int GetTeamGroupDeaths() => teamGroupDeaths;

    private void UpdateTeamGroupCount()
    {
        teamGroupCount = teamGroups.Count;
    }

    // private void EvaluateTeamGroups()
    // {
    //     if (teamGroups == null) teamGroups = new List<TeamGroup>();
    //     UpdateTeamGroupCount();
    //     if (territory == null) return;
    //     if (teamGroupCount < territory.maxTeamGroups)
    //     {
    //         TeamGroup newTeam =
    //             SpawnTeamGroup(territory.GetRandomWaypoint().position, 3);
    //         if (newTeam != null)
    //         {
    //             AddTeamGroup (newTeam);
    //         }
    //     }
    // }
    public void AddTeamGroup(TeamGroup teamGroup)
    {
        if (!teamGroups.Contains(teamGroup))
        {
            teamGroups.Add (teamGroup);
            teamGroup.onTeamGroupDeath += OnTeamGroupDeath;

            UpdateTeamGroupCount();
        }
    }

    public void RemoveTeamGroup(TeamGroup teamGroup)
    {
        if (teamGroups.Contains(teamGroup))
        {
            teamGroups.Remove (teamGroup);

            UpdateTeamGroupCount();
        }
    }

    private void OnTeamGroupDeath(TeamGroup teamGroup)
    {
        teamGroup.onTeamGroupDeath -= OnTeamGroupDeath;
        RemoveTeamGroup (teamGroup);

        if (teamGroup.GetTeamRole() == TeamGroup.TeamRole.defend)
        {
            teamGroupDefenders--;
        }
        else
            teamGroupAttackers--;

        teamGroupDeaths++;

        territory.ResetTeamGroupDeathCooldown();

        Debug
            .Log("Spawn Controller: " +
            this.name +
            " - TeamGroup Lost: " +
            teamGroup.name);
    }

    public void UpdateTeamSpawns(
        float attackUnitAllocation = .5f,
        int maxTeamGroups = 4,
        int minTeamGroupSize = 2
    )
    {
        int maxAttackerTeams =
            Mathf.FloorToInt(maxTeamGroups * attackUnitAllocation);
        int maxDefenderTeams = maxTeamGroups - maxAttackerTeams;
        int viableTeamgroupMax =
            Mathf.FloorToInt(spawnLimit / minTeamGroupSize);

        int teamGroupSizeMax = Mathf.FloorToInt(spawnLimit / maxTeamGroups);

        if (
            teamGroupCount < maxTeamGroups &&
            teamGroupDefenders < maxDefenderTeams
        )
        {
            Debug.Log("EvaluateTeams - Spawn Defender Team");
            TeamGroup newTeam =
                SpawnTeamGroup(RandomSpawnPoint().gameObject.transform.position,
                teamGroupSizeMax,
                TeamGroup.TeamRole.defend);
            if (newTeam != null)
            {
                AddTeamGroup (newTeam);
                teamGroupDefenders++;
            }
        }
        if (
            teamGroupCount < maxTeamGroups &&
            teamGroupAttackers < maxAttackerTeams
        )
        {
            Debug.Log("EvaluateTeams - Spawn Attacker Team");
            TeamGroup newTeam =
                SpawnTeamGroup(RandomSpawnPoint().gameObject.transform.position,
                teamGroupSizeMax,
                TeamGroup.TeamRole.assault);
            if (newTeam != null)
            {
                AddTeamGroup (newTeam);
                teamGroupAttackers++;
            }
        }
    }
#endregion



#region Spawning

    private float teamRespawnCooldown = 12f;

    private float teamRespawnCooldownTimmer = 0f;

    public TeamGroup
    SpawnTeamGroup(
        Vector3 spawnPos,
        int members,
        TeamGroup.TeamRole role = TeamGroup.TeamRole.assault
    )
    {
        if (spawnPos == Vector3.zero) return null;

        Transform newTeamGroupTransform =
            Instantiate(teamGroupPrefab,
            this.transform.position,
            Quaternion.identity);

        TeamGroup newTeamGroup =
            newTeamGroupTransform.GetComponent<TeamGroup>();

        for (int i = 0; i < members; i++)
        {
            GameObject newSpawn =
                Instantiate(RandomPrefab(),
                new Vector3(spawnPos.x, 3, spawnPos.z),
                Quaternion.identity);

            ActorFactions actorFactions =
                newSpawn.GetComponent<ActorFactions>();
            if (actorFactions != null)
            {
                actorFactions.factions = new Faction[1];
                actorFactions.factions[0] = factions[0];
            }

            ActorEventManger actorEventManger =
                newSpawn.GetComponent<ActorEventManger>();
            if (actorEventManger != null)
                actorEventManger.onActorDeath += OnSpawnDeath;

            TerritoryUnit territoryUnit =
                newSpawn.GetComponent<TerritoryUnit>();
            if (territoryUnit != null)
            {
                territoryUnit.SetSourceTerritory (territory);
            }

            spawns.Add (newSpawn);
            newTeamGroup.AddMember (newSpawn);

            // TEMP
            newTeamGroup.SetTeamRole(role == TeamGroup.TeamRole.assault);
            newTeamGroup.SetTerritoryOwner (territory);
            newTeamGroup.SetGoalPosition(territory.GetRandomAttackPoint());
        }
        return newTeamGroup;
    }

    private void Spawn()
    {
        Vector3 spawnPos;
        if (spawnPoints?.Length > 0)
        {
            Transform spawnPoint = RandomSpawnPoint().GetComponent<Transform>();
            spawnPos =
                UtilityHelpers.GetRandomNavmeshPoint(1.5f, spawnPoint.position);
            Debug.Log("SPAWN at SpawnPoint: " + spawnPoint.name);
        }
        else
        {
            Debug.Log("SPAWN!");
            spawnPos =
                UtilityHelpers
                    .GetRandomNavmeshPoint(radius, this.transform.position);
        }

        GameObject newSpawn =
            Instantiate(RandomPrefab(),
            new Vector3(spawnPos.x, 3, spawnPos.z),
            Quaternion.identity);
        ActorFactions actorFactions = newSpawn.GetComponent<ActorFactions>();
        if (actorFactions != null)
        {
            actorFactions.factions = new Faction[1];
            actorFactions.factions[0] = factions[0];
        }

        spawns.Add (newSpawn);

        // if (teamGroup == null)
        // {
        //     teamGroup =
        //         Instantiate(teamGroupPrefab,
        //         this.transform.position,
        //         Quaternion.identity);
        // }
        // teamGroup.GetComponent<TeamGroup>().AddMember(newSpawn);
        ActorEventManger actorEventManger =
            newSpawn.GetComponent<ActorEventManger>();
        if (actorEventManger != null)
            actorEventManger.onActorDeath += OnSpawnDeath;

        TerritoryUnit territoryUnit = newSpawn.GetComponent<TerritoryUnit>();
        if (territoryUnit != null) territoryUnit.SetSourceTerritory(territory);

        if (bHoldPosition)
        {
            CombatStateData combatStateData =
                newSpawn.GetComponent<CombatStateData>();
            ActorNavigationData actorNavigationData =
                newSpawn.GetComponent<ActorNavigationData>();
            actorNavigationData.travelPosition = this.transform;
            actorNavigationData.holdPositionRadius = radius;
            combatStateData.combatNavigationState =
                CombatNavigationState.holdPosition;
        }
    }
#endregion


    private void UnsubscribeFromSpawnEvents(GameObject spawn)
    {
        if (spawn != null)
        {
            ActorEventManger actorEventManger =
                spawn.GetComponent<ActorEventManger>();
            if (actorEventManger != null)
                actorEventManger.onActorDeath -= OnSpawnDeath;
        }
    }

    private void OnSpawnDeath(GameObject spawn)
    {
        currentSpawnCount--;
        deaths++;
        if (spawn == null) return;
        UnsubscribeFromSpawnEvents (spawn);
        spawns.Remove (spawn);

        if (territory != null)
        {
            Debug.Log("Territory Spawn Controller: OnSpawnDeath");
            territory.Damage(0.1f, 1);
        }
    }

    private void Awake()
    {
        territory = GetComponent<Territory>();
        if (territory != null) radius = territory.GetRadius();
    }

    private void Start()
    {
        spawns = new List<GameObject>();
        teamGroups = new List<TeamGroup>();

        ScanForSpawnPoints();

        // Invoke(nameof(EvaluateTeamGroups), 2f);
    }

    private void OnDestroy()
    {
        foreach (GameObject spawn in spawns)
        {
            if (spawn != null) UnsubscribeFromSpawnEvents(spawn);
        }
    }

    private void FixedUpdate()
    {
        if (delayStart > 0f)
        {
            delayStart -= Time.fixedDeltaTime;
            return;
        }

        // if (prefabs != null && keepSpawning && currentSpawnCount < spawnLimit)
        // {
        //     currentSpawnCount++;

        //     if (deaths > 0)
        //     {
        //         Invoke(nameof(Spawn), 2f);
        //         return;
        //     }
        //     Spawn();
        // }
    }
}
