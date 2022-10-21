using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using Hybrid.Components;
using UnityEngine;



public struct WaypointData
{
    public WaypointData(int _index, Transform _transform )
    {
        index = _index;
        transform = _transform;
    }
    public int index { get; }
    public Transform transform { get; set; }
}



public class Territory : MonoBehaviour, IFactionOwnable, ITerritory
{    
    [SerializeField]
    private Vector4 assignedColor = Vector4.zero;
    public Vector4 GetAssignedColor () => assignedColor;

    [SerializeField] private bool isUnconquerable = false; // Cant be taken
    [SerializeField] private bool isInvincible = false; // Cant be damaged
    [SerializeField] private int radius = 30;

    [SerializeField] private List<Territory> neighbors;
    [SerializeField] private List<Territory> allies;
    [SerializeField] private List<Territory> enemies;
    public int morale { get; set; }
    public int influence { get; set; }
    public int value { get; set; }


    [SerializeField] private Transform waypointPrefab; 
    [SerializeField] private Transform[] borderWaypoints;
    [SerializeField] private Transform[] innerWaypoints;


    TerritorySpawnController spawnController;

    public int GetRadius () => radius;

    #region Health 
        public int healthMax = 100;
        [SerializeField] private float _healthPercent = 1f;
        [SerializeField] private float _health = 100f;

        public float health
        {
            get => _health;
            set
            {
                if (isUnconquerable)
                {
                    _health = Mathf.Clamp(value, 10f, (float)healthMax);
                }
                else
                {
                    _health = value;
                    _health = Mathf.Clamp(_health, 0, (float)healthMax);
                }
                _healthPercent = ((float)health / (float)healthMax + 0.001f);
            }
        }

        public float healthPercent
        {
            get => _healthPercent;
            private set
            {
                _healthPercent = value;
            }
        }
    #endregion

    #region Ownership
        [SerializeField] private Faction _owner;
        [SerializeField] private Faction _lastOwner;
        public Faction owner
        {
            get => _owner;
            private set
            {
                _owner = value;
            }
        }
        public Faction lastOwner
        {
            get => _lastOwner;
            private set
            {
                _lastOwner = value;
            }
        }

        public bool HasOwner() => owner != null;
        public Faction GetOwner() => owner;
        public void UpdateOwner(Faction newOwner)
        {
            if (!owner && newOwner != null){            
                owner = newOwner;
                return;
            }
            if (newOwner != null && owner != null && newOwner.GetInstanceID() == owner.GetInstanceID())
            {
                return;
            }
            
            lastOwner = owner;            
            owner = newOwner;
        }
        public void ClearOwnership()
        {
            Debug.Log("Territory - ClearOwnership of : "  + owner.name);
            UpdateOwner(null);
        }
    #endregion

    #region Strength
        public int strengthMax = 9999;
        [SerializeField] private int _totalStrength = 15;
        public int totalStrength
        {
            get => _totalStrength;
            private set
            {

                if (isUnconquerable)
                {
                    _totalStrength = Mathf.Clamp(value, 1, strengthMax);
                    return;
                }
                    _totalStrength = Mathf.Clamp(value, 0, strengthMax);
            }
        }
    #endregion


    private Dictionary<Faction, int> localStrength;

    public void UpdateLocalStrength(Dictionary<Faction, int> newLocalStrength) {
        if (newLocalStrength != null) {

        localStrength = newLocalStrength;
        
        Debug.Log("Territory - Local Strength Updated: "  + localStrength.Count );
        }
    }


    private void UpdateLocalStrengths(IsActor actor, Faction faction) {
        if (localStrength == null) {
            localStrength = new Dictionary<Faction, int>();
        }
        localStrength[faction] += 1;

        Debug.Log("Local Strength - "+ faction + ":  "+ localStrength[faction]);
    }
    
    private void OnRecieveLocalActorBroadcast(IsActor actor, Faction faction)
    {
        if ( !actor || !faction) return;

        UpdateLocalStrengths(actor, faction);     
    }




    public void Damage(float fDamage, int unitLoss)
    {
        if ( isInvincible) return;
        
        health -= System.Math.Abs(fDamage);

        totalStrength -= System.Math.Abs(unitLoss);        
    }

    private void Start()
    {
        allies = new List<Territory>();
        enemies = new List<Territory>();

        attackPoints = new Vector3[4];

        spawnController = GetComponent<TerritorySpawnController>();


        if (assignedColor == Vector4.zero)
        {
            assignedColor =
                new Vector4(Random.Range(0.2f, 1.0f),
                    Random.Range(0.25f, 1.0f),
                    Random.Range(0.2f, 1.0f),
                    1f);
        }

        if (spawnController != null) { 
            spawnController.SetAsignedColor(assignedColor);
        }

        GenerateBorderWaypoints();
        EvaluateNeighbors();

        // Invoke(nameof(EvaluateTeams), 2f);
    }


    [SerializeField]
    public float delayStart = 1f;

    private void FixedUpdate() {
        if (delayStart > 0f)
        {
            delayStart -= Time.fixedDeltaTime;
            return;
        }

        if (currentTarget == null)
        {
            if (enemies?.Count > 0) {
                UpdateTarget(GetRandomEnemyNeighbor());
            }
        }
        else
        {
            EvaluateAttackPoints();
            EvaluateTeams();
        }
    }

    [SerializeField] private Territory currentTarget;
    [SerializeField] private Territory lastTarget;
    private void UpdateTarget(Territory newTarget)
    {
        if (newTarget != null)
        {
            currentTarget = newTarget;
        }
    }


    private Territory GetRandomEnemyNeighbor() {
        return enemies[Random.Range(0, enemies.Count)];
    }

    private void EvaluateNeighbors()
    {
        if (neighbors?.Count > 0)
        {

            for (var i = 0; i < neighbors.Count; i++)
            {
                Territory neighbor = neighbors[i];
                FactionRelationship factionRelationship = Faction.GetFactionRelationship(owner,  neighbor.owner);

                Debug.Log("Neighbor Found: "+ neighbor.gameObject.name + "Relationship:  "+ factionRelationship);
                
                if ( factionRelationship == FactionRelationship.ally) {
                        if (!allies.Contains(neighbor)) allies.Add(neighbor);

                } else if ( factionRelationship == FactionRelationship.enemy) {
                        if (!enemies.Contains(neighbor)) enemies.Add(neighbor);
                }
            }
        }
    }


    private bool HasValidOwner()
    {
        if (!owner) return false;
        if (
         health == 0 || totalStrength == 0 
        ) return false;
        // if (
        //  health > 0 || totalStrength > 0 
        // ) return true;
        if (localStrength != null && localStrength.ContainsKey(owner)) {
            if (localStrength[owner] > 0) return true;
        }
        return HasOwner();
    }

    public void EvaluateCurrentState()
    {
        if (!HasOwner()) return;

        if (!HasValidOwner()) {
            ClearOwnership();
        }
    }

    #region Waypoints
        private int borderWaypointMax = 8;
        private void GeneratePatrolWaypoints ()  {
            GenerateBorderWaypoints();
            GenerateInnerWaypoints();
        }
        private void GenerateBorderWaypoints ()  {
            borderWaypoints = new Transform[borderWaypointMax];
            float pointOffset = 2f;

            // if (borderWaypointMax == 4) {
            //     Vector3 pointF = this.transform.position + (Vector3.forward * radius);
            //     Vector3 pointB = this.transform.position + (-Vector3.forward * radius);
            //     Vector3 pointR = this.transform.position + (Vector3.right * radius);
            //     Vector3 pointL = this.transform.position + (-Vector3.right * radius);

            //     Transform wpF = Instantiate(waypointPrefab);
            //     Transform wpB = Instantiate(waypointPrefab);
            //     Transform wpR = Instantiate(waypointPrefab);
            //     Transform wpL = Instantiate(waypointPrefab);

            //     wpF.SetParent(this.gameObject.transform);
            //     wpB.SetParent(this.gameObject.transform);
            //     wpR.SetParent(this.gameObject.transform);
            //     wpL.SetParent(this.gameObject.transform);

            //     wpF.transform.position = UtilityHelpers.GetRandomNavmeshPoint(pointOffset, pointF);
            //     wpB.transform.position = UtilityHelpers.GetRandomNavmeshPoint(pointOffset, pointB);;
            //     wpR.transform.position = UtilityHelpers.GetRandomNavmeshPoint(pointOffset, pointR);;
            //     wpL.transform.position = UtilityHelpers.GetRandomNavmeshPoint(pointOffset, pointL);;

            //     borderWaypoints[0] = wpF;
            //     borderWaypoints[1] = wpR;
            //     borderWaypoints[2] = wpB;
            //     borderWaypoints[3] = wpL;
            // } else {
                float angle = 0f;
                float incAmount = (360f / borderWaypointMax);

                for (int i = 0; i < borderWaypointMax; i++)
                {
                    Vector3 pos = this.transform.position +  Quaternion.AngleAxis(angle, gameObject.transform.up) * ( Vector3.forward * radius);
                    Transform wp = Instantiate(waypointPrefab);
                    wp.SetParent(this.gameObject.transform);

                    bool getPoint = UtilityHelpers.GeCloseNavMeshPoint(pos, pointOffset, out Vector3 point);
                    wp.transform.position = point;
                    // wp.transform.position = UtilityHelpers.GetRandomNavmeshPoint(pointOffset, pos);
                    borderWaypoints[i] = wp;
                    angle+= incAmount;
                }
            // }
        }

        private void GenerateInnerWaypoints ()  {

        }
        public  Transform GetRandomWaypoint () => borderWaypoints[Random.Range(0, borderWaypoints.Length)];
        public  Transform[] GetBorderWaypoints () => borderWaypoints;

        public  WaypointData NextBorderWaypoint (int currentIX = 0)  {
            if (borderWaypoints == null) return new WaypointData(-1, null);
            int nextIX =  (currentIX + 1) % (borderWaypoints.Length);
            WaypointData data = new WaypointData(nextIX, borderWaypoints[nextIX]);
            // Debug.Log("NextBorderWaypoint -  prev: "+ currentIX  +" next: "+ nextIX);
            return data;
        }
    #endregion



    #region Attacking
        public Vector3[] attackPoints;
        [SerializeField] private int  lastUpdatedAttackIX = -1;
        [SerializeField] private float attackUnitAllocation = .6f; // how many units to keep guard vs send to attack
        [SerializeField] private int _teamGroupSizeMax = 3; 
        public int maxTeamGroups = 4;
        public int minTeamGroupSize = 2;
        [SerializeField] private float teamRespawnCooldown = 15f;
        [SerializeField] private float teamRespawnCooldownTimmer = 0;


        public void ResetTeamGroupDeathCooldown() {
            teamRespawnCooldownTimmer = teamRespawnCooldown;
        }

        public void EvaluateTeams()  {
            // int maxAttackerTeams = Mathf.FloorToInt(maxTeamGroups *attackUnitAllocation);
            // int maxDefenderTeams = maxTeamGroups - maxAttackerTeams;

            // int spawnLimit = spawnController.GetSpawnLimit();
            // int viableTeamgroupMax = Mathf.FloorToInt(spawnLimit/ minTeamGroupSize);
            
            // _teamGroupSizeMax = Mathf.FloorToInt(spawnLimit / maxTeamGroups);

            if (teamRespawnCooldownTimmer > 0f)
            {
                teamRespawnCooldownTimmer -= Time.fixedDeltaTime;
                return;
            }

            spawnController.UpdateTeamSpawns(attackUnitAllocation);

            // if (defenderTeams.Count < maxDefenderTeams) {
            //     // int teamgroupSizeMax = Mathf.FloorToInt(spawnLimit / maxDefenderTeams);
            //     Debug.Log("EvaluateTeams - Spawn Defender Team");

            //     TeamGroup newDefenderTeam =  spawnController.SpawnTeamGroup(spawnController.RandomSpawnPoint().gameObject.transform.position, _teamGroupSizeMax, TeamGroup.TeamRole.defend);
            //     if (newDefenderTeam != null) defenderTeams.Add(newDefenderTeam);
            // }
            // if (attackerTeams.Count < maxAttackerTeams) {
            //     // int teamgroupSizeMax = Mathf.FloorToInt(spawnLimit / maxAttackerTeams);
            //     Debug.Log("EvaluateTeams - Spawn Attacker Team");

            //     TeamGroup newAttackerTeam =  spawnController.SpawnTeamGroup(spawnController.RandomSpawnPoint().gameObject.transform.position, _teamGroupSizeMax, TeamGroup.TeamRole.assault);
            //     if (newAttackerTeam != null) attackerTeams.Add(newAttackerTeam);
            // }
        }

        [SerializeField] private float updateAttackPointsCooldown = 60f; 
        [SerializeField] private float updateAttackPointsTimer = 1f; 

        private void DebugShowAttackPoints() {
            foreach (Vector3 item in attackPoints)
            {
                if (item != Vector3.zero) {
                    Vector3 direction = (item - this.transform.position);
                    Debug.DrawRay(this.transform.position, direction, Color.red, 15f);
                }
            }
        }
        public Vector3 GetLatestAttackPoint() => lastUpdatedAttackIX > -1?  attackPoints[lastUpdatedAttackIX]: attackPoints[0];
        public Vector3 GetRandomAttackPoint()  {
           Vector3 result =  attackPoints[Random.Range(0, attackPoints.Length)];
            if (result == Vector3.zero) return GetLatestAttackPoint();
            return result;
        } 
        public void EvaluateAttackPoints()  {
            if (updateAttackPointsTimer > 0f)
            {
                updateAttackPointsTimer -= Time.fixedDeltaTime;
                return;
            } else updateAttackPointsTimer =updateAttackPointsCooldown;

            if (currentTarget) {
                Transform[] enemyBorderWaypoints = currentTarget.GetBorderWaypoints();
                int enemyWaypoints = enemyBorderWaypoints.Length;

                if (enemyWaypoints > 0) { 
                    int nextIX;
                    if (lastUpdatedAttackIX < 0) {
                        nextIX = 0;
                    } else {
                        nextIX = (lastUpdatedAttackIX + 1) %(attackPoints.Length);
                    }

                    if (Random.Range(0,100) < 55) {
                        attackPoints[nextIX] = UtilityHelpers.GetRandomNavmeshPoint(2f, enemyBorderWaypoints[(0 + nextIX) % enemyWaypoints].position);
                        nextIX = (nextIX + 1) %(attackPoints.Length);
                        attackPoints[nextIX] = UtilityHelpers.GetRandomNavmeshPoint(2f, enemyBorderWaypoints[(0 + nextIX) % enemyWaypoints].position);
                        
                    } else {
                        attackPoints[nextIX] = UtilityHelpers.GetRandomNavmeshPoint(2f, enemyBorderWaypoints[(0 + nextIX) % enemyWaypoints].position);
                    }

                    lastUpdatedAttackIX = nextIX;

                    DebugShowAttackPoints();
                }
            }
        } 

    #endregion

    private void OnDrawGizmos()
    {
        float pointSize = .6f;
        Gizmos.color = Color.red;

        if ( borderWaypoints != null && borderWaypoints.Length > 0 ) {
            Gizmos.DrawWireSphere(borderWaypoints[0].position, pointSize);
            Gizmos.DrawWireSphere(borderWaypoints[1].position, pointSize);
            Gizmos.DrawWireSphere(borderWaypoints[2].position, pointSize);
            Gizmos.DrawWireSphere(borderWaypoints[3].position, pointSize);
        } else {
            Gizmos.DrawWireSphere(this.transform.position + (Vector3.forward * radius), pointSize);
            Gizmos.DrawWireSphere(this.transform.position + (-Vector3.forward * radius), pointSize);
            Gizmos.DrawWireSphere(this.transform.position + (Vector3.right * radius), pointSize);
            Gizmos.DrawWireSphere(this.transform.position + (-Vector3.right * radius), pointSize);
            
            // Gizmos.color = Color.green;
            // Gizmos.DrawWireSphere(this.transform.position +  Quaternion.AngleAxis(45f, gameObject.transform.up) * ( Vector3.forward * radius), pointSize);
            // Gizmos.DrawWireSphere(this.transform.position +  Quaternion.AngleAxis(90f, gameObject.transform.up) * ( Vector3.forward * radius), pointSize);
            // Gizmos.DrawWireSphere(this.transform.position +  Quaternion.AngleAxis(135f, gameObject.transform.up) * ( Vector3.forward * radius), pointSize);
            // Gizmos.DrawWireSphere(this.transform.position +  Quaternion.AngleAxis(180f, gameObject.transform.up) * ( Vector3.forward * radius), pointSize);
        }
    }
}

public interface ITerritory
{
    int totalStrength { get; }
    int morale { get; }
    int influence { get; }
    int value { get; }

}

public interface IFactionOwnable
{
    Faction owner { get; }

    Faction lastOwner { get; }
}
