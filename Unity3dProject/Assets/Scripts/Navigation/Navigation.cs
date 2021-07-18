using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Navigation : MonoBehaviour
{
    public Vector3 RandomNavmeshLocation(float radius, Vector3 centerPoint ) {
        Vector3 randomDirection = UnityEngine.Random.insideUnitSphere * radius;
         randomDirection += centerPoint;
         NavMeshHit hit;
         Vector3 finalPosition = Vector3.zero;
         if (NavMesh.SamplePosition(randomDirection, out hit, radius, 1)) {
             finalPosition = hit.position;            
         }
         return finalPosition;
     }
}
