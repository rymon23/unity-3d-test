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

    TerritoryUnit territoryUnit;

    TeamGroup teamGroup;

    void Awake()
    {
        agent = this.GetComponent<NavMeshAgent>();
        actorNavigationData = this.GetComponent<ActorNavigationData>();
        territoryUnit = GetComponent<TerritoryUnit>();
    }

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
            Task.current.debugInfo =
                string.Format("allowPandaBTTasks : " + allowPandaBTTasks);
            Task.current.Fail();
            return false;
        }
    }

    [Task]
    public bool HasValidAgent()
    {
        if (agent == null | !agent.enabled)
        {
            Task.current.Fail();
            return false;
        }
        Task.current.Succeed();
        return true;
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

    float wanderRadius = 100.0f;


#region Territory Tasks
    [Task]
    public void PickTerritoryNextWaypoint()
    {
        if (!CanExecuteBTTasks()) return;

        Transform newWaypoint = territoryUnit.EvaluateBorderWaypoint();
        if (newWaypoint)
        {
            actorNavigationData.currentWaypoint = newWaypoint;
        }
        else
        {
            Task.current.Fail();
            return;
        }

        Task.current.debugInfo = string.Format("Travel to next Waypoint");

        agent.SetDestination(actorNavigationData.currentWaypoint.position);
        Task.current.Succeed();
    }

    [Task]
    public void EvaluateTeamGroupGoal()
    {
        if (!CanExecuteBTTasks()) return;

        if (!HasValidAgent()) return;

        if (territoryUnit != null)
        {
            if (teamGroup != null)
            {
                if (
                    teamGroup.IsTerrioryManaged() &&
                    teamGroup.GetTeamRole() == TeamGroup.TeamRole.defend
                )
                {
                    PickTerritoryNextWaypoint();
                    Task.current.debugInfo =
                        string.Format("PickTerritoryNextWaypoint");
                    Task.current.Succeed();
                }
                else
                {
                    if (teamGroup.GetGoalPosition() != Vector3.zero)
                    {
                        Vector3 pos =
                            RandomNavmeshLocation(2f,
                            teamGroup.GetGoalPosition());
                        agent.SetDestination (pos);

                        Task.current.debugInfo =
                            string.Format("Travel to team goal");
                        Task.current.Succeed();
                    }
                    else
                    {
                        Task.current.debugInfo =
                            string.Format("No TeamGroup Goal");
                        Task.current.Fail();
                    }
                }
            }
            else
            {
                teamGroup = territoryUnit.GetTeamGroup();

                Task.current.debugInfo = string.Format("No TeamGroup");
                Task.current.Fail();
            }
        }
    }
#endregion



#region Random Destination
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
#endregion


    [Task]
    public void EvaluateCurrentTravelDestination()
    {
        if (!CanExecuteBTTasks()) return;

        if (actorNavigationData == null)
        {
            Task.current.Fail();
            return;
        }
        if (
            actorNavigationData.travelPosition == null ||
            agent == null | !agent.enabled
        )
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

    // private void OnDrawGizmos()
    // {
    //     if (agent != null && agent.destination != null)
    //     {
    //         Gizmos.color = Color.yellow;
    //         Gizmos.DrawWireSphere(agent.destination, agent.stoppingDistance);
    //     }
    // }
}
