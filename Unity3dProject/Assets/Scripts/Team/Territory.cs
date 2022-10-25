using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Hybrid.Components;

public class Territory : MonoBehaviour, IFactionOwnable, ITerritory
{
    #region Static Vars
        public static float maxRadius = 50f;
        public static float maxNeighborDistance = 248f;
        private static float minInvasionRadiusMod = 10f; //  radius*minInvasionRadiusMult
        public static int maxReinforcements = 999;
        public static float healthRegenCooldown = 8f;
    #endregion

    #region Events
        public event System.Action<Territory, Faction> onTerritoryRegimeChange;

        public void TerritoryRegimeChange() => onTerritoryRegimeChange?.Invoke(this, owner);
    #endregion


    #region Reinforcements
        [SerializeField] private int _totalReinforcements = 24;
        public int totalReinforcements
        {
            get => _totalReinforcements;
            set
            {

                if (isUnconquerable)
                {
                    _totalReinforcements = Mathf.Clamp(value, 1, maxReinforcements);
                    return;
                }
                _totalReinforcements = Mathf.Clamp(value, 0, maxReinforcements);
            }
        }

        public void Damage(float fDamage, int unitLoss)
        {
            if (isInvincible) return;

            health -= System.Math.Abs(fDamage);

            // totalReinforcements -= System.Math.Abs(unitLoss);
        }
        public void UnitLost(Faction killerFaction, Vector3 killerPos)
        {
            if (isInvincible) return;

            if (killerPos != Vector3.zero) {
                float distance = Vector3.Distance(killerPos, this.gameObject.transform.position);
                if (distance < GetMinInvasionRadius()) {
                    UpdateInvaderFactionPTs(killerFaction);
                    Damage(1*invasionLossDamageMult, 0);
                    return;
                }
            }

            // string str = "Territory: UnitLost";
            // if (killerFaction != null)
            // {
            //     str += "\nkillerFaction: " + killerFaction.name;
            // }

            Damage(.25f, 0);
            // Debug.Log(str);
        }

        public bool CanSpawnReinforcements () {
            return (totalReinforcements > 1 && health > 1);
        }
        public bool HasAvailableReinforcements () {
            return totalReinforcements > 0;
        }
        public int GetAvailableReinforcements () {
            return totalReinforcements;
        }

    #endregion


    #region Borders
        [SerializeField] private int radius = 30;
        public int GetRadius() => radius;
        public float GetMinInvasionRadius () => radius + minInvasionRadiusMod;
    #endregion

    #region Hold Position Controls
        [SerializeField] private float holdPositionRadius = 30f; // Expands & Contracts 
        public float GetHoldPositionRadius() => holdPositionRadius; 
        [SerializeField] private Transform holdPositionCenter;
        public Transform GetHoldPositionCenter() => holdPositionCenter; 
        private void GenerateInnerMarkers()
        {
            if (holdPositionCenter == null) {
                bool pointFound = UtilityHelpers.GeCloseNavMeshPoint(this.gameObject.transform.position ,3f,out Vector3 result);

                if (pointFound && result != Vector3.zero) {
                    holdPositionCenter = Instantiate(waypointPrefab, result, Quaternion.identity);
                    holdPositionCenter.SetParent(this.gameObject.transform);
                    EvaluateHoldPositionRadius();
                }
            }
        }
      private void EvaluateHoldPositionRadius()
        {
            holdPositionRadius = radius*Random.Range(0.88f, 1.23f);
        }
    #endregion

    #region Waypoints
        [SerializeField] private Transform waypointPrefab;
        [SerializeField] private Transform[] borderWaypoints;
        [SerializeField] private Transform[] innerWaypoints;


        private int borderWaypointMax = 8;

        private void GeneratePatrolWaypoints()
        {
            GenerateBorderWaypoints();
            GenerateInnerWaypoints();
        }
        private void GenerateBorderWaypoints()
        {
            borderWaypoints = new Transform[borderWaypointMax];
            float pointOffset = 2f;

            float angle = 0f;
            float incAmount = (360f / borderWaypointMax);

            for (int i = 0; i < borderWaypointMax; i++)
            {
                Vector3 pos = this.transform.position + Quaternion.AngleAxis(angle, gameObject.transform.up) * (Vector3.forward * radius);
                Transform wp = Instantiate(waypointPrefab);
                wp.SetParent(this.gameObject.transform);

                bool getPoint = UtilityHelpers.GeCloseNavMeshPoint(pos, pointOffset, out Vector3 point);
                wp.transform.position = point;
                borderWaypoints[i] = wp;
                angle += incAmount;
            }
        }

        private void GenerateInnerWaypoints()
        {

        }
        public Transform GetRandomWaypoint() => borderWaypoints[Random.Range(0, borderWaypoints.Length)];
        public Transform[] GetBorderWaypoints() => borderWaypoints;

        public Waypoint.WaypointData NextBorderWaypoint(int currentIX = 0, bool counterClockwise = false)
        {
            if (borderWaypoints == null) return new Waypoint.WaypointData(-1, null);
            int nextIX;
            if (counterClockwise) {
                if (currentIX <= 0) {
                    nextIX = borderWaypoints.Length - 1;
                } else {
                    nextIX = (currentIX - 1) % (borderWaypoints.Length);
                }
            } else {
                nextIX = (currentIX + 1) % (borderWaypoints.Length);
            }
            Waypoint.WaypointData data = new Waypoint.WaypointData(nextIX, borderWaypoints[nextIX]);
            // Debug.Log("NextBorderWaypoint -  prev: "+ currentIX  +" next: "+ nextIX);
            return data;
        }
    #endregion

    #region Health 
        [SerializeField] private bool isInvincible = false; // Cant be damaged
        [SerializeField] private float invasionLossDamageMult = 3f;
        private int healthMax = 100;
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

        [SerializeField] private float healthRegenRate = 0.45f;
        [SerializeField] private float healthRegenCooldownTimer = 5f;
        public void RegenerateHealth (bool force = false) {
            if (health < healthMax && currentInvasionState == InvasionState.idle) {;
                if (!force && healthRegenCooldownTimer > 0f)
                {
                    healthRegenCooldownTimer -=  1f;
                    return;
                }

                healthRegenCooldownTimer = healthRegenCooldown;
                
                health+= healthRegenRate;
            }
        }
    #endregion


    [SerializeField] private bool isUnconquerable = false; // Cant be taken
    
    public int morale { get; set; }
    public int influence { get; set; }
    public int value { get; set; }

    // [SerializeField] private float surrenderThreashhold; 



    TerritorySpawnController spawnController;


#region Neightbors
    private int neighborsTotal = 0;
    [SerializeField] private List<Territory> neighbors;
    public void AddNeighbor(Territory newTerritory) {
        if (neighbors.Contains(newTerritory)) return;
        neighbors.Add(newTerritory);
        neighborsTotal = neighbors.Count;
    }


    [SerializeField] private List<Territory> allies;
    [SerializeField] private List<Territory> enemies;

    private Territory GetRandomEnemyNeighbor()
    {
        return enemies[Random.Range(0, enemies.Count)];
    }

    [SerializeField] private float evaluateNeighborsCooldown = 189f;
    [SerializeField] private float evaluateNeighborsTimer = 0f;


    public void EvaluateNeighbors(bool force = false)
    {
        if (neighborsTotal > 0)
        {
            if (!force && evaluateNeighborsTimer > 0f)
            {
                evaluateNeighborsTimer -= Time.fixedDeltaTime;
                return;
            }
            evaluateNeighborsTimer = evaluateNeighborsCooldown;

            for (var i = 0; i < neighbors.Count; i++)
            {
                Territory neighbor = neighbors[i];
                FactionRelationship factionRelationship = Faction.GetFactionRelationship(owner, neighbor.owner);
                // Debug.Log("Neighbor Found: " + neighbor.gameObject.name + "Relationship:  " + factionRelationship);

                if (factionRelationship == FactionRelationship.ally)
                {
                    if (!allies.Contains(neighbor)) allies.Add(neighbor);
                    if (enemies.Contains(neighbor)) enemies.Remove(neighbor);
                }
                else if (factionRelationship == FactionRelationship.enemy)
                {
                    if (!enemies.Contains(neighbor)) enemies.Add(neighbor);
                    if (allies.Contains(neighbor)) allies.Remove(neighbor);
                }
            }
        }
    }


    #endregion



    #region Invasion
    public enum InvasionState
    {
        idle = 0,
        underAttck,
        // underSeige,
        breached,
        contested,
        fallen
    }

    [SerializeField] private InvasionState currentInvasionState = InvasionState.idle;
    public InvasionState GetInvasionState() => currentInvasionState;
    [SerializeField] private float invadeStateRelaxCooldown = 45f;
    [SerializeField] private float invadeStateRelaxTimer = 60f;

    [SerializeField] private Dictionary<Faction, Vector2> invaderFactionPoints; //X = Points, Y = LastUpdated

    private int invaderDamagePTs = 1;
    // [SerializeField] private float invaderDamageClearTimer = 30f; //Time before invader data too old & removed
    // [SerializeField] private float invaderPTsDegradeCoolDown = 30f; 
    // [SerializeField] private float invaderPTsDegradeCooldownTimer = 0; 
    [SerializeField] private float invaderDamageClearTime = 30f; //Time before invader data too old & removed
    [SerializeField] private float invaderPointsLossAmount = 0.3f;
    [SerializeField] private float invaderEvaluateCooldown = 10f;
    [SerializeField] private float invaderEvaluateCooldownTimer = 0f;

    [SerializeField] private Faction invaderPriorityFaction;


    public Faction GetPriorityInvaderFaction () => invaderPriorityFaction;

    private string HName()
    {
        return "Territory: " + gameObject.name + "\n";
    }

    private void EvaluateInvaders()
    {
        if (invaderEvaluateCooldownTimer > 0f)
        {
            invaderEvaluateCooldownTimer -= Time.fixedDeltaTime;
            return;
        }
        else invaderEvaluateCooldownTimer = invaderEvaluateCooldown;

        if (invaderFactionPoints == null || invaderFactionPoints.Count == 0) return;

        float hightestPTs = -1f;

        // Debug.Log("Test 1");

        foreach (Faction faction in invaderFactionPoints.Keys)
        {
            Vector2 invaderData = invaderFactionPoints[faction];

            if (invaderData.y > 0f)
            {
                invaderData.y -= Time.deltaTime;

                if (hightestPTs == -1f || hightestPTs < invaderData.x)
                {
                    invaderPriorityFaction = faction;
                    hightestPTs = invaderData.x;
                }
                invaderData.x -= invaderPointsLossAmount;
                try
                {
                    invaderFactionPoints[faction] = invaderData;
                }
                catch (System.Exception)
                {
                    throw;
                }
            }
            else
            {
                // Clear data
                invaderFactionPoints[faction] = Vector2.zero;
            }
        }

        //
        if (hightestPTs <= -1)
        {
            // Clear all if none have points 
            invaderFactionPoints.Clear();
        }
        else
        {
            if (invaderPriorityFaction != null)
            {

                Debug.Log("invaderPriorityFaction: " + invaderPriorityFaction.name);
            }

        }
    }

    private void ResetInvaderFactionPoints() {
        invaderPriorityFaction = null;
        invaderFactionPoints.Clear();
    }
    public void UpdateInvaderFactionPTs(Faction faction, int points = 1)
    {
        if (faction == null || faction == owner) return;
        if (!invaderFactionPoints.ContainsKey(faction))
        {
            invaderFactionPoints.Add(faction, new Vector2(points, invaderDamageClearTime));
        }
        else
        {
            Vector2 data = invaderFactionPoints[faction];
            data.x += 1f;
            data.y = invaderDamageClearTime;
            invaderFactionPoints[faction] = data;
        }

        Debug.Log(this.gameObject.name + ":\nInvader Damage: " + invaderFactionPoints[faction].x + ", Faction: " + faction.name);
    }

    public void InvasionStatusAlert(Vector3 unitPosition)
    {
        if (unitPosition == Vector3.zero) return;

        float distance = Vector3.Distance(unitPosition, this.gameObject.transform.position);

        if (distance < GetMinInvasionRadius()) {
            bool isInternal = distance < radius * .9f;

            ResetInvasionRelaxTimer();

            if (isInternal && (currentInvasionState < InvasionState.breached)) {
                SetInvasionState(InvasionState.breached);
                return;
            } 

            if (currentInvasionState < InvasionState.underAttck) {
                SetInvasionState(InvasionState.underAttck);
            }
        }

        // Debug.Log(this.gameObject.name + ": Invasion Alert \ndistance: " + (int)distance + ", internal: " + isInternal);
    }

    public void SetInvasionState(InvasionState newInvasionState)
    {
        if (currentInvasionState != newInvasionState) {
            currentInvasionState = newInvasionState;
            EvaluateUnitAllocation(true);
            // GlobalEventManager.TerritoryInvasionStateChange(this, currentInvasionState);
        }
    }

    public void ResetInvasionRelaxTimer()
    {
        invadeStateRelaxTimer = invadeStateRelaxCooldown;
    }

    public bool CanBeTaken() {
        return ( (health < 1 || totalReinforcements < 1) && (spawnController.GetTeamGroups() < 1));
    }

    private void EvaluateInvasionState()
    {
        if (invaderPriorityFaction != null && currentInvasionState >= InvasionState.underAttck )
        {
            // Debug.Log(this.gameObject.name+" EvaluateInvasionState");

            if ((spawnController.GetTeamGroups() < 1) && (health < 1 || totalReinforcements < 1))
            {
                // Debug.Log(this.gameObject.name+" UpdateTerritoryOwnership");

                UpdateTerritoryOwnership(invaderPriorityFaction);
            } else {

                if (invadeStateRelaxTimer > 0f)
                {
                    invadeStateRelaxTimer -= Time.fixedDeltaTime;
                    return;
                }

                invadeStateRelaxTimer = invadeStateRelaxCooldown;
                if (currentInvasionState == InvasionState.underAttck) {
                    SetInvasionState(InvasionState.idle);
                } else {
                    SetInvasionState(currentInvasionState - 1);
                }
            }
        }

    }

    private void UpdateTerritoryOwnership(Faction newOwner)
    {
        if (newOwner == null) return;

        UpdateOwner(newOwner);

        ResetInvaderFactionPoints();

        currentTarget = null;
        health += 45;

        SetInvasionState( InvasionState.idle);
        EvaluateNeighbors(true);
        
        ResetAttackPoints();
        EvaluateAttackTargets(true);


        Debug.Log(this.gameObject.name + ": Invader Won Control: " + owner.name);
    }

        public void TeamGroupDetected(TeamGroup teamGroup) {
            Territory teamTerritory = teamGroup.GetTerritoryOwer();
            if (teamTerritory != null) {
                FactionRelationship factionRelationship = Faction.GetFactionRelationship(owner, teamTerritory.owner);
                if (factionRelationship == FactionRelationship.enemy) {
                    InvasionStatusAlert(teamGroup.transform.position);
                    UpdateInvaderFactionPTs(teamTerritory.owner, 2);
                    Debug.Log(this.gameObject.name+" Enemy TeamGroup Detected:" + teamGroup.gameObject.name);
                }
            }
            // Debug.Log(this.gameObject.name+" TeamGroupDetected:" + teamGroup.gameObject.name);
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
        if (!owner && newOwner != null)
        {
            owner = newOwner;
            return;
        }
        if (newOwner != null && owner != null && newOwner.GetInstanceID() == owner.GetInstanceID())
        {
            return;
        }

        lastOwner = owner;
        owner = newOwner;

        assignedColor = owner.color;
    }
    public void ClearOwnership()
    {
        Debug.Log("Territory - ClearOwnership of : " + owner.name);
        UpdateOwner(null);
    }
    #endregion


    private Dictionary<Faction, int> localStrength;

    public void UpdateLocalStrength(Dictionary<Faction, int> newLocalStrength)
    {
        if (newLocalStrength != null)
        {

            localStrength = newLocalStrength;

            Debug.Log("Territory - Local Strength Updated: " + localStrength.Count);
        }
    }


    private void UpdateLocalStrengths(IsActor actor, Faction faction)
    {
        if (localStrength == null)
        {
            localStrength = new Dictionary<Faction, int>();
        }
        localStrength[faction] += 1;

        Debug.Log("Local Strength - " + faction + ":  " + localStrength[faction]);
    }

    private void OnReceiveLocalActorBroadcast(IsActor actor, Faction faction)
    {
        if (!actor || !faction) return;

        UpdateLocalStrengths(actor, faction);
    }


    private void Start()
    {
        if (neighbors == null) {
            neighbors = new List<Territory>();
        }
        
        allies = new List<Territory>();
        enemies = new List<Territory>();
        invaderFactionPoints = new Dictionary<Faction, Vector2>();


        ResetAttackPoints();

        spawnController = GetComponent<TerritorySpawnController>();

        if (assignedColor == Vector4.zero) assignedColor = owner.color;
        
        GenerateInnerMarkers();
        GenerateBorderWaypoints();
    }


    [SerializeField]
    public float delayStart = 1f;

    private void FixedUpdate()
    {
        if (delayStart > 0f)
        {
            delayStart -= Time.fixedDeltaTime;
            return;
        }

        EvaluateNeighbors();

        if (currentTarget == null)
        {
            if (enemies?.Count > 0)
            {
                UpdateTarget(GetRandomEnemyNeighbor());
            }
        }
        else
        {
            EvaluateAttackTargets();
            EvaluateTeams();

            if (currentInvasionState >= InvasionState.underAttck)
            {
                EvaluateInvaders();

                EvaluateInvasionState();
            }
        }
    }

    [SerializeField] private Territory currentTarget;
    [SerializeField] private float currentTargetDistance;
    [SerializeField] private Territory lastTarget;
    private void UpdateTarget(Territory newTarget)
    {
        if (newTarget != null)
        {
            currentTarget = newTarget;
            currentTargetDistance = Vector3.Distance(gameObject.transform.position, currentTarget.transform.position);
        }
    }





    private bool HasValidOwner()
    {
        if (!owner) return false;
        if (
         health == 0 || totalReinforcements == 0
        ) return false;

        if (localStrength != null && localStrength.ContainsKey(owner))
        {
            if (localStrength[owner] > 0) return true;
        }
        return HasOwner();
    }

    public void EvaluateCurrentState()
    {
        if (!HasOwner()) return;

        // if (!HasValidOwner()) {
        //     ClearOwnership();
        // }
    }

    #region Attacking
    public Vector3[] attackPoints;
    [SerializeField] private int lastUpdatedAttackIX = -1;
    [SerializeField] private float attackUnitAllocation = .6f; // how many units to keep guard vs send to attack
    public float GetAttackUnitAllocation () => attackUnitAllocation;
    [SerializeField] private float unitAllocationUpdateCooldown = 15f;
    [SerializeField] private float unitAllocationUpdateTimer = 0;


    [SerializeField] private int _teamGroupSizeMax = 3;
    public int maxTeamGroups = 4;
    public int minTeamGroupSize = 2;
    [SerializeField] private float teamRespawnCooldown = 18f;
    [SerializeField] private float teamRespawnCooldownTimer = 0;


    public void ResetTeamGroupDeathCooldown()
    {
        teamRespawnCooldownTimer = teamRespawnCooldown;
    }

    public void EvaluateUnitAllocation (bool forced = false) {
        if (forced || unitAllocationUpdateTimer > 0f)
        {
            unitAllocationUpdateTimer -= 1;
            return;
        }
        unitAllocationUpdateTimer = unitAllocationUpdateCooldown;

        if(health < 20) {
            attackUnitAllocation = 0;
            return;
        } 

        if (healthPercent < 0.5f || currentInvasionState > InvasionState.idle) {
            attackUnitAllocation = Random.Range(0.0f, 0.34f); 
            return;
        }
        attackUnitAllocation = Random.Range(0.3f, 0.84f); 
    }

    public void EvaluateTeams()
    {
        if (CanSpawnReinforcements()) {
            if (teamRespawnCooldownTimer > 0f)
            {
                teamRespawnCooldownTimer -= Time.fixedDeltaTime;
                return;
            }

            spawnController.UpdateTeamSpawns(attackUnitAllocation, maxTeamGroups);     
        }
        // int maxAttackerTeams = Mathf.FloorToInt(maxTeamGroups *attackUnitAllocation);
        // int maxDefenderTeams = maxTeamGroups - maxAttackerTeams;

        // int spawnLimit = spawnController.GetSpawnLimit();
        // int viableTeamgroupMax = Mathf.FloorToInt(spawnLimit/ minTeamGroupSize);

        // _teamGroupSizeMax = Mathf.FloorToInt(spawnLimit / maxTeamGroups);
    }

    [SerializeField] private float updateAttackPointsCooldown = 60f;
    [SerializeField] private float updateAttackPointsTimer = 1f;


    public Vector3 GetLatestAttackPoint() => lastUpdatedAttackIX > -1 ? attackPoints[lastUpdatedAttackIX] : attackPoints[0];
    public Vector3 GetRandomAttackPoint()
    {
        Vector3 result = attackPoints[Random.Range(0, attackPoints.Length)];
        if (result == Vector3.zero) return GetLatestAttackPoint();
        return result;
    }
    private void ResetAttackPoints()
    {
        attackPoints = new Vector3[4];
        lastUpdatedAttackIX = -1;
    }
    public void EvaluateAttackTargets(bool force = false)
    {
        if (force || updateAttackPointsTimer > 0f)
        {
            updateAttackPointsTimer -= Time.fixedDeltaTime;
            return;
        }
        updateAttackPointsTimer = updateAttackPointsCooldown;

        if (currentTarget)
        {

            if (Faction.GetFactionRelationship(currentTarget.owner, owner) != FactionRelationship.enemy ) {
                currentTarget = null;
                ResetAttackPoints();
                return;
            }

            Transform[] enemyBorderWaypoints = currentTarget.GetBorderWaypoints();
            int enemyWaypoints = enemyBorderWaypoints.Length;

            if (enemyWaypoints > 0)
            {
                int nextIX;
                if (lastUpdatedAttackIX < 0)
                {
                    nextIX = 0;
                }
                else
                {
                    nextIX = (lastUpdatedAttackIX + 1) % (attackPoints.Length);
                }

                if (Random.Range(0, 100) < 30)
                {
                    attackPoints[nextIX] = UtilityHelpers.GetRandomNavmeshPoint(2f, enemyBorderWaypoints[(0 + nextIX) % enemyWaypoints].position);
                    nextIX = (nextIX + 1) % (attackPoints.Length);
                    attackPoints[nextIX] = UtilityHelpers.GetRandomNavmeshPoint(2f, enemyBorderWaypoints[(0 + nextIX) % enemyWaypoints].position);

                }
                else
                {
                    attackPoints[nextIX] = UtilityHelpers.GetRandomNavmeshPoint(2f, enemyBorderWaypoints[(0 + nextIX) % enemyWaypoints].position);
                }

                lastUpdatedAttackIX = nextIX;

                DebugShowAttackPoints();
            }
        }
    }

    #endregion




    #region Debugging
        private void DebugShowAttackPoints()
        {
            foreach (Vector3 item in attackPoints)
            {
                if (item != Vector3.zero)
                {
                    Vector3 direction = (item - this.transform.position);
                    Debug.DrawRay(this.transform.position, direction, Color.red, 15f);
                }
            }
        }
        [SerializeField] private Vector4 assignedColor = Vector4.zero;

        public void SetAsignedColor(Vector4 newColor)
        {
            assignedColor = newColor;
        }
        public Vector4 GetAssignedColor() => assignedColor;     

        private void OnDrawGizmos()
        {
            if (assignedColor != Vector4.zero)
            {
                Gizmos.color = assignedColor;
            }
            else
            {
                Gizmos.color = Color.white;
            }

            Gizmos.DrawWireSphere(this.transform.position, radius);
            Gizmos.DrawWireSphere(this.transform.position, GetMinInvasionRadius());

            Gizmos.color = Color.red;
            float pointSize = .5f;

            if (borderWaypoints != null && borderWaypoints.Length > 0)
            {
                Gizmos.DrawWireSphere(borderWaypoints[0].position, pointSize);
                Gizmos.DrawWireSphere(borderWaypoints[1].position, pointSize);
                Gizmos.DrawWireSphere(borderWaypoints[2].position, pointSize);
                Gizmos.DrawWireSphere(borderWaypoints[3].position, pointSize);
            }
            else
            {
                Gizmos.DrawWireSphere(this.transform.position + (Vector3.forward * radius), pointSize);
                Gizmos.DrawWireSphere(this.transform.position + (-Vector3.forward * radius), pointSize);
                Gizmos.DrawWireSphere(this.transform.position + (Vector3.right * radius), pointSize);
                Gizmos.DrawWireSphere(this.transform.position + (-Vector3.right * radius), pointSize);
            }

            if (holdPositionCenter != null) {
                Gizmos.color = Color.white;
                Gizmos.DrawWireSphere(holdPositionCenter.position, holdPositionRadius);
            }
    }
    #endregion
}
