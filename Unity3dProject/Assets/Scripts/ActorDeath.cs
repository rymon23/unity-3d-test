using Hybrid.Components;
using RootMotion.FinalIK;
using UnityEngine;
using UnityEngine.AI;

public class ActorDeath : MonoBehaviour
{
    ActorEventManger actorEventManger;
    Rigidbody rb;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        actorEventManger = GetComponent<ActorEventManger>();
        if (actorEventManger != null) actorEventManger.onActorDeath += onDeath;
    }

    private void OnDestroy()
    {
        if (actorEventManger != null) actorEventManger.onActorDeath -= onDeath;
    }

    void onDeath(GameObject thisActor)
    {
        ActorHealth actorHealth = this.GetComponent<ActorHealth>();
        Animator animator = this.GetComponentInChildren<Animator>();
        NavMeshAgent agent = this.GetComponent<NavMeshAgent>();
        BipedIK bipedIK = this.GetComponent<BipedIK>();

        actorHealth.deathState = DeathState.dead;
        if (animator != null)
        {
            animator.SetBool("isDead", true);
            animator.SetTrigger("Death");
            animator.enabled = false;
        }

        // Disable nav
        if (agent != null) agent.enabled = false;

        if (bipedIK != null) bipedIK.enabled = false;

        if (rb != null)
        {
            rb.AddForce(new Vector3(0, -10, 0));
        }
    }
}
