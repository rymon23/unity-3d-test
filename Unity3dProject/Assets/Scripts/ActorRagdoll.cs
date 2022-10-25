using Hybrid.Components;
using RootMotion.FinalIK;
using UnityEngine;
using UnityEngine.AI;

namespace Hybrid.Components
{
    public enum RagdollState
    {
        knockdown = 0,
        unconscious = 1,
        dead = 2
    }

    public class ActorRagdoll : MonoBehaviour
    {
        ActorEventManger actorEventManger;

        Rigidbody rb;

        RagdollState currentRagdollState;

        private void Start()
        {
            rb = GetComponent<Rigidbody>();
            actorEventManger = GetComponent<ActorEventManger>();

            if (actorEventManger != null)
                actorEventManger.onActorRagdoll += onRagdoll;
        }

        private void OnDestroy()
        {
            if (actorEventManger != null)
                actorEventManger.onActorRagdoll -= onRagdoll;
        }

        void onRagdoll(RagdollState state)
        {
            ActorHealth actorHealth = this.GetComponent<ActorHealth>();
            Animator animator = this.GetComponentInChildren<Animator>();
            NavMeshAgent agent = this.GetComponent<NavMeshAgent>();
            BipedIK bipedIK = this.GetComponent<BipedIK>();

            if (animator != null)
            {
                // animator.SetTrigger("Death");
                animator.SetBool("bIsInRagdoll", true);
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
}
