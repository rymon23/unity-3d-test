using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Hybrid.Components;

public class ChaseTarget : MonoBehaviour
{
    // public GameObject target;
    NavMeshAgent agent;
    Targeting targeting;

    void Start()
    {
        agent = this.GetComponentInChildren<NavMeshAgent>();
        targeting = this.GetComponent<Targeting>();
    }

    void Update()
    {
        if (targeting.currentTarget) {
            agent.SetDestination(targeting.currentTarget.position);
        }
    }
}
