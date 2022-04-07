using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Panda;

public class ActorBaseBT : MonoBehaviour
{
    NavMeshAgent agent;
    void Start()
    {
        agent = this.GetComponent<NavMeshAgent>();
    }

    // private void OnDrawGizmos()
    // {
    //     if (agent != null && agent.destination != null)
    //     {
    //         Gizmos.color = Color.yellow;
    //         Gizmos.DrawWireSphere(agent.destination, agent.stoppingDistance);
    //     }
    // }

    public Vector3 RandomNavmeshLocation(float radius, Vector3 center)
    {
        Vector3 randomDirection = UnityEngine.Random.insideUnitSphere * radius;
        randomDirection += center;
        NavMeshHit hit;
        Vector3 finalPosition = Vector3.zero;
        if (NavMesh.SamplePosition(randomDirection, out hit, radius, 1))
        {
            finalPosition = hit.position;
        }
        return finalPosition;
    }


    // [Task]
    // public void PickDestination(float x, float z)
    // {
    //     Vector3 dest = new Vector3(x, 0, z);
    //     agent.SetDestination(dest);
    //     Task.current.Succeed();
    // }

    float wanderRadius = 100.0f;

    [Task]
    public void PickRandomDestination()
    {
        Vector3 dest = RandomNavmeshLocation(wanderRadius, transform.position);
        agent.SetDestination(dest);
        Task.current.Succeed();
    }

    [Task]
    public void MoveToDestination()
    {
        if (Task.isInspected)
            Task.current.debugInfo = string.Format("t={0:0.00}", Time.time);

        if (agent.remainingDistance <= agent.stoppingDistance && !agent.pathPending)
        {
            Task.current.Succeed();
        }
    }
}
