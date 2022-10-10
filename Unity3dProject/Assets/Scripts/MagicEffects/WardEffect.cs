using Hybrid.Components;
using UnityEngine;

public class WardEffect : MonoBehaviour
{
    private ActorEventManger actorEventManger;

    // private void OnEffectStart(SpellInstanceData spellInstance)
    public void OnEffectStart(SpellInstanceData spellInstance)
    {
        actorEventManger =
            spellInstance.target.GetComponent<ActorEventManger>();
        ActorHealth actorHealth =
            spellInstance.target.GetComponent<ActorHealth>();
        // if (actorHealth != null && actorEventManger != null)
        // {
        //     actorEventManger.DamageHealth (damage);
        // }
    }

    public void OnEffectUpdate(SpellInstanceData spellInstance)
    {
    }

    public void OnEffectFinish(SpellInstanceData spellInstance)
    {
    }
}
