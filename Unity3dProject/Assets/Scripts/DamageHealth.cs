using UnityEngine;

public class DamageHealth : MonoBehaviour
{
    public float baseDamage;
    public void Damage(GameObject target, GameObject sender)
    {
        Debug.Log("Spell: Damage Health effect: target: " + target.name + " / sender: " + sender.name);
        ActorEventManger actorEventManger = target.GetComponent<ActorEventManger>();
        actorEventManger.DamageHealth(baseDamage);
        actorEventManger.TriggerAnim_Stagger(0);
    }
}
