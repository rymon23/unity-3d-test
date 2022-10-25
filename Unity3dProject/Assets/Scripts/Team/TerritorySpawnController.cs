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
    private int spawnLimit = 4;

    public int GetSpawnLimit() => spawnLimit;

    [SerializeField]
    private int teamGroupCount = 0;

    [SerializeField]
    private Transform teamGroupPrefab;

    [SerializeField]
    private Transform[] spawnMarkers;

    [SerializeField]
    private SpawnPoint[] spawnPoints;

    public int currentSpawnCount = 0;

    public int deaths = 0;

    public bool bAssignTeamGroup = true;

    public bool bHoldPosition = true;

    private float delayStart = 1f;

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

    public int GetTeamGroups() => teamGroups.Count;

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

        // Debug
        //     .Log("Spawn Controller: " +
        //     this.name +
        //     " - TeamGroup Lost: " +
        //     teamGroup.name);
    }

    public void UpdateTeamSpawns(
        float attackUnitAllocation,
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

        // Reassign team group roles as needed
        if (teamGroupCount == maxTeamGroups)
        {
            if (
                teamGroupDefenders < maxDefenderTeams ||
                teamGroupAttackers < maxAttackerTeams
            )
            {
                int attackers = 0;
                int defenders = 0;

                foreach (TeamGroup team in teamGroups)
                {
                    if (teamGroupDefenders < maxDefenderTeams)
                    {
                        if (team.GetTeamRole() != TeamGroup.TeamRole.defend)
                        {
                            team.SetTeamRole(false);
                            teamGroupDefenders++;
                        }
                    }
                    else if (teamGroupAttackers < maxAttackerTeams)
                    {
                        if (team.GetTeamRole() != TeamGroup.TeamRole.assault)
                        {
                            team.SetTeamRole(true);
                            teamGroupAttackers++;
                        }
                    }

                    if (team.GetTeamRole() == TeamGroup.TeamRole.defend)
                        defenders++;
                    if (team.GetTeamRole() == TeamGroup.TeamRole.assault)
                        attackers++;
                }

                teamGroupAttackers = attackers;
                teamGroupDefenders = defenders;
            }
        }
        else
        {
            if (
                teamGroupCount < maxTeamGroups &&
                teamGroupDefenders < maxDefenderTeams
            )
            {
                Debug.Log("EvaluateTeams - Spawn Defender Team");
                TeamGroup newTeam =
                    SpawnTeamGroup(territory
                        .GetRandomWaypoint()
                        .gameObject
                        .transform
                        .position,
                    teamGroupSizeMax,
                    TeamGroup.TeamRole.defend);
                if (newTeam != null)
                {
                    AddTeamGroup (newTeam);
                    teamGroupDefenders++;
                    territory.totalReinforcements -= teamGroupSizeMax;
                }
            }
            if (
                teamGroupCount < maxTeamGroups &&
                teamGroupAttackers < maxAttackerTeams
            )
            {
                Debug.Log("EvaluateTeams - Spawn Attacker Team");
                TeamGroup newTeam =
                    SpawnTeamGroup(RandomSpawnPoint()
                        .gameObject
                        .transform
                        .position,
                    teamGroupSizeMax,
                    TeamGroup.TeamRole.assault);
                if (newTeam != null)
                {
                    AddTeamGroup (newTeam);
                    teamGroupAttackers++;
                    territory.totalReinforcements -= teamGroupSizeMax;
                }
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

        newTeamGroup.SetTeamRole(role == TeamGroup.TeamRole.assault);
        newTeamGroup.SetTerritoryOwner (territory);
        newTeamGroup.SetGoalPosition(territory.GetRandomAttackPoint());

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
                actorFactions.factions[0] = territory.owner;
            }

            ActorEventManger actorEventManger =
                newSpawn.GetComponent<ActorEventManger>();
            if (actorEventManger != null)
                actorEventManger.onActorDeath += OnSpawnDeath;

            TerritoryUnit territoryUnit =
                newSpawn.GetComponent<TerritoryUnit>();
            if (territoryUnit != null)
            {
                territoryUnit.SetTeamGroup (newTeamGroup);
                territoryUnit.SetSourceTerritory (territory);
                territoryUnit.EvaluateHoldPositionData(null);
            }

            newTeamGroup.AddMember (newSpawn);
            spawns.Add (newSpawn);
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
            // Debug.Log("SPAWN at SpawnPoint: " + spawnPoint.name);
        }
        else
        {
            // Debug.Log("SPAWN!");
            spawnPos =
                UtilityHelpers
                    .GetRandomNavmeshPoint(territory.GetRadius(),
                    this.transform.position);
        }

        GameObject newSpawn =
            Instantiate(RandomPrefab(),
            new Vector3(spawnPos.x, 3, spawnPos.z),
            Quaternion.identity);
        ActorFactions actorFactions = newSpawn.GetComponent<ActorFactions>();
        if (actorFactions != null)
        {
            actorFactions.factions = new Faction[1];
            actorFactions.factions[0] = territory.owner;
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
            actorNavigationData.holdPositionRadius = territory.GetRadius();
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

    private void OnSpawnDeath(GameObject spawn, GameObject killer)
    {
        currentSpawnCount--;
        deaths++;
        if (spawn == null) return;
        UnsubscribeFromSpawnEvents (spawn);
        spawns.Remove (spawn);

        if (territory != null)
        {
            string str = "TerritorySpawnController: OnSpawnDeath";

            Faction killerFaction = null;

            if (killer)
            {
                ActorFactions killerFactions =
                    killer.gameObject.GetComponent<ActorFactions>();

                str += "\nkiller: " + killer.name;

                if (killerFactions != null)
                {
                    killerFaction = killerFactions.GetFirstFaction();

                    str += "\nkillerFaction: " + killerFaction?.name;

                    territory
                        .UnitLost(killerFaction, killer.transform.position);
                    territory.InvasionStatusAlert(killer.transform.position);
                }
            }

            territory
                .UnitLost(killerFaction, spawn.gameObject.transform.position);
            Debug.Log (str);
        }
    }

    private void Awake()
    {
        territory = GetComponent<Territory>();
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
