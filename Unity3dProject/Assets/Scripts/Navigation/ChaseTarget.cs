using UnityEngine;
using UnityEngine.AI;
using Hybrid.Components;

public class ChaseTarget : MonoBehaviour
{
    NavMeshAgent agent;
    Targeting targeting;

    void Start()
    {
        agent = agent == null? this.GetComponentInChildren<NavMeshAgent>() : agent;
        targeting = this.GetComponent<Targeting>();
    }

    void Update()
    {
        if (targeting.currentTarget) {
            agent.SetDestination(targeting.currentTarget.position);

            // transform.LookAt(targeting.currentTarget);    
        }
    }
}
