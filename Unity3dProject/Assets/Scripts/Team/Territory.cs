using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using Hybrid.Components;
using UnityEngine;

public class Territory : MonoBehaviour, IFactionOwnable, ITerritory
{    
    [SerializeField] private bool isUnconquerable = false; // Cant be taken
    [SerializeField] private bool isInvincible = false; // Cant be damaged
    [SerializeField] private int radius = 30;

    [SerializeField] private List<Territory> neighbors;
    [SerializeField] private List<Territory> allies;
    [SerializeField] private List<Territory> enemies;
    public int morale { get; set; }
    public int influence { get; set; }
    public int value { get; set; }


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

        EvaluateNeighbors();
    }

    private void Update()
    {
        if (currentTarget == null)
        {
            if (enemies?.Count > 0) {
                UpdateTarget(GetRandomEnemyNeighbor());
            }
        }
        else
        {
            
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
