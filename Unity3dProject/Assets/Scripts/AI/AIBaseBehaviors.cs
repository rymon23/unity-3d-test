
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Hybrid.Components;
using Panda;

public class AIBaseBehaviors : MonoBehaviour
{
    
    ActorNavigationData myNavData;
    NavMeshAgent agent;

    void Start()
    {
        myNavData = this.GetComponent<ActorNavigationData>();
        agent = this.GetComponentInChildren<NavMeshAgent>();
    }

    public Vector3 GetRandomNavmeshPosition(Vector3 centerPosition,  float radius) {
        Vector3 randomDirection = UnityEngine.Random.insideUnitSphere * radius;
         randomDirection += centerPosition;
         NavMeshHit hit;
         Vector3 finalPosition = Vector3.zero;
         if (NavMesh.SamplePosition(randomDirection, out hit, radius, 1)) {
             finalPosition = hit.position;            
         }
         return finalPosition;
     }


    [Task]
    public void Task_UpdateWanderDestination()
    {   
        Vector3 destination;

        if (myNavData) {
            destination = GetRandomNavmeshPosition(myNavData.wanderCenterPosition, myNavData.wanderRadius);
        }else {
            destination = GetRandomNavmeshPosition(transform.position, 15f);
        }
        myNavData.travelPosition = destination;
        agent.SetDestination(destination);
        Task.current.Succeed();
    }

    [Task]
    public void Task_TravelToDestination()
    {
        if( Task.isInspected )
                Task.current.debugInfo = string.Format("t={0:0.00}", Time.time);

        if(agent.remainingDistance <= agent.stoppingDistance && !agent.pathPending)
        {
            Task.current.Succeed();
        }     
    }
}
