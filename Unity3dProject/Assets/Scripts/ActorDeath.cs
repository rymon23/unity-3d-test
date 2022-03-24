using UnityEngine;
using UnityEngine.AI;
using Hybrid.Components;

public class ActorDeath : MonoBehaviour
{
    ActorEventManger actorEventManger;
    private void Start()
    {
        actorEventManger = GetComponent<ActorEventManger>();
        if (actorEventManger != null) actorEventManger.onActorDeath += onDeath;
    }

    void onDeath()
    {
        ActorHealth actorHealth = this.GetComponent<ActorHealth>();
        Animator animator = this.GetComponentInChildren<Animator>();
        NavMeshAgent agent = this.GetComponent<NavMeshAgent>();

        actorHealth.deathState = DeathState.dead;
        animator.SetBool("isDead", true);
        animator.SetTrigger("Death");
        animator.enabled = false;
        // Disable nav
        agent.enabled = false;
    }

    private void OnDestroy()
    {
        if (actorEventManger != null) actorEventManger.onActorDeath -= onDeath;
    }

}
