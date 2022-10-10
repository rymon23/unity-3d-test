using System.Collections;
using System.Collections.Generic;
using Hybrid.Components;
using Panda;
using UnityEngine;
using UnityEngine.AI;

public class ActorBaseBT : MonoBehaviour
{
    public bool allowPandaBTTasks = true;

    NavMeshAgent agent;

    ActorNavigationData actorNavigationData;

    void Start()
    {
        agent = this.GetComponent<NavMeshAgent>();
        actorNavigationData = this.GetComponent<ActorNavigationData>();
    }

    // private void OnDrawGizmos()
    // {
    //     if (agent != null && agent.destination != null)
    //     {
    //         Gizmos.color = Color.yellow;
    //         Gizmos.DrawWireSphere(agent.destination, agent.stoppingDistance);
    //     }
    // }
    [Task]
    public bool CanExecuteBTTasks()
    {
        if (allowPandaBTTasks)
        {
            Task.current.Succeed();
            return true;
        }
        else
        {
            Task.current.Fail();
            return false;
        }
    }

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
        if (!CanExecuteBTTasks()) return;

        float wRadius = wanderRadius;
        if (actorNavigationData != null)
            wRadius = actorNavigationData.wanderRadius;

        Vector3 dest = RandomNavmeshLocation(wRadius, transform.position);
        agent.SetDestination (dest);
        Task.current.Succeed();
    }

    [Task]
    public void PickRandomDestinationAhead()
    {
        if (!CanExecuteBTTasks()) return;

        float wRadius = wanderRadius;
        if (actorNavigationData != null)
            wRadius = actorNavigationData.wanderRadius;

        Vector3 dest =
            UtilityHelpers
                .GetRandomNavmeshPoint(wRadius,
                UtilityHelpers.GetFrontPosition(this.transform, wRadius));

        agent.SetDestination (dest);
        Task.current.Succeed();
    }

    [Task]
    public void EvaluateCurrentTravelDestination()
    {
        if (!CanExecuteBTTasks()) return;

        if (actorNavigationData == null)
        {
            Task.current.Fail();
            return;
        }
        if (actorNavigationData.travelPosition == null)
        {
            Task.current.Fail();
            return;
        }
        if (
            agent.destination == actorNavigationData.travelPosition.position &&
            agent.remainingDistance <= agent.stoppingDistance &&
            !agent.pathPending
        )
        {
            Task.current.Succeed();
            return;
        }

        agent.SetDestination(actorNavigationData.travelPosition.position);
        Task.current.Succeed();
    }

    [Task]
    public void MoveToDestination()
    {
        if (Task.isInspected)
            Task.current.debugInfo = string.Format("t={0:0.00}", Time.time);

        if (
            agent.remainingDistance <= agent.stoppingDistance &&
            !agent.pathPending
        )
        {
            Task.current.Succeed();
        }
    }
}
