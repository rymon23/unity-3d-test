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
    private int spawnLimit = 3;

    [SerializeField]
    private float radius = 32f;

    [SerializeField]
    private Transform teamGroup;

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

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;

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

    private SpawnPoint RandomSpawnPoint()
    {
        // int ix = prefabs.Length;
        // int x = Random.Range(0, ix);
        // Debug.Log("RandomPrefab: " + x);
        return spawnPoints[Random.Range(0, spawnPoints.Length)];
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

        // currentSpawnCount++;
        if (teamGroup == null)
        {
            teamGroup =
                Instantiate(teamGroupPrefab,
                this.transform.position,
                Quaternion.identity);
        }
        teamGroup.GetComponent<TeamGroup>().AddMember(newSpawn);

        ActorEventManger actorEventManger =
            newSpawn.GetComponent<ActorEventManger>();
        if (actorEventManger != null)
            actorEventManger.onActorDeath += OnSpawnDeath;

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

    private void Start()
    {
        territory = GetComponent<Territory>();
        if (territory != null) radius = territory.GetRadius();

        spawns = new List<GameObject>();
        ScanForSpawnPoints();
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
        if (prefabs != null && keepSpawning && currentSpawnCount < spawnLimit)
        {
            currentSpawnCount++;

            if (deaths > 0)
            {
                Invoke(nameof(Spawn), 2f);
                return;
            }
            Spawn();
        }
    }
}
