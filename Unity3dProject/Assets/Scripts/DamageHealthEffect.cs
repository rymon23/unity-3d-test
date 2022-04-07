using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Hybrid.Components;

public class DamageHealthEffect : IMagicEffect
{
    public float damage;
    private ActorEventManger actorEventManger;

    public void OnEffectStart(GameObject target, GameObject sender)
    {
        actorEventManger = target.GetComponent<ActorEventManger>();
        ActorHealth actorHealth = target.GetComponent<ActorHealth>();
        if (actorHealth != null && actorEventManger != null)
        {
            actorEventManger.DamageHealth(damage);
        }
    }
    public void OnEffectUpdate(GameObject target, GameObject sender) { }

    public void OnEffectFinish(GameObject target, GameObject sender) { }
}
