using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using Hybrid.Components;
using UnityEngine;

// Army => Faction => SubFaction
public class Territory : MonoBehaviour, IFactionOwnable, ITerritory
{
    [SerializeField] private float health = 100f;
    public int totalStrength{get; set;}
    public int morale{get; set;}
    public int influence{get; set;}
    public int value{get; set;} 
    private Faction _owner;
    private Faction _lastOwner;
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
        if (newOwner != null && newOwner.GetInstanceID() != owner.GetInstanceID()) {
            
            lastOwner = owner;
        }
        owner = newOwner;
    }

    public void Clear()
    {
        UpdateOwner(null);
    }
}

public interface ITerritory
{
    int totalStrength{get;}
    int morale{get;}
    int influence{get;}
    int value{get;} 

}

public interface IFactionOwnable
{
    Faction owner { get; }

    Faction lastOwner { get; }
}
