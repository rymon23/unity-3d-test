using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Unity.Entities;

static class UtilityHelpers
{


    public static string GetUnsetActorEntityRefId() => "-";

    public static string getActorEntityRefId(Entity entity)
    {
        return $"ref#{entity.Version}{entity.Index}";
    }

    public static Vector3 getRandomNavmeshPoint(float radius, Vector3 center)
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


    public static float getFOVAngle(Transform viewer, Vector3 targetPos, float maxAngle, float maxRadius)
    {
        Vector3 directionBetween = (targetPos - viewer.position).normalized;
        directionBetween.y *= 0;
        return Vector3.Angle(viewer.forward, directionBetween);
    }
    public static bool HasFOVAngle(Transform viewer, Vector3 targetPos, float maxAngle, float maxRadius)
    {
        float angle = getFOVAngle(viewer, targetPos, maxAngle, maxRadius);
        return (angle <= maxAngle);
    }

    public static bool IsInFOVScope(Transform viewer, Vector3 targetPos, float maxAngle, float maxRadius)
    {

        float angle = getFOVAngle(viewer, targetPos, maxAngle, maxRadius);

        if (angle <= maxAngle)
        {
            // int layerMask = 1 << 9;
            // Does the ray intersect any objects which are in the player layer.
            // if (Physics.Raycast(viewer.position, Vector3.forward, maxRadius, layerMask))
            // {
            //     Debug.Log("The ray hit the actor");
            // }
            // int layerMask = LayerMask.GetMask("BlockLOS", "Ground", "Actor");
            int layerMask = LayerMask.GetMask("BlockLOS");


            // This would cast rays only against colliders in layer 8.
            // But instead we want to collide against everything except layer 8. The ~ operator does this, it inverts a bitmask.
            // layerMask = ~layerMask;

            // Vector3 targetPos = target.position + (target.up * 2f);
            Vector3 tarPos = targetPos + (Vector3.up * 1.2f);
            Vector3 direction = (targetPos - viewer.position);


            RaycastHit[] raycastHits = Physics.RaycastAll(viewer.position, direction, Mathf.Infinity, layerMask);
            Debug.Log("IsInFOVScope => Did Hits : " + raycastHits.Length);
            if (raycastHits.Length > 0)
            {
                float targetDist = Vector3.Distance(viewer.position, targetPos);
                Debug.Log("IsInFOVScope => distance compare - Hit:  " + raycastHits[0].distance + " / target: " + targetDist);

                // if (raycastHits[0].distance < targetDist && LayerMask.LayerToName(raycastHits[0].transform.gameObject.layer) != "Actor")
                if (LayerMask.LayerToName(raycastHits[0].transform.gameObject.layer) != "Actor")
                {
                    Debug.Log("IsInFOVScope => First Hit : " + raycastHits[0].transform.name + " Layer: " + LayerMask.LayerToName(raycastHits[0].transform.gameObject.layer));
                    Debug.DrawRay(viewer.position, direction, Color.blue);
                    return false;
                }
            }
            // Debug.Log("IsInFOVScope => Layer: " + LayerMask.NameToLayer("BlockLOS"));

            // int layerMask = LayerMask.GetMask("BlockLOS", "Ground");
            // RaycastHit hit;
            // // Does the ray intersect any objects excluding the player layer
            // if (Physics.Raycast(viewer.position, direction, out hit, Mathf.Infinity, layerMask))
            // {
            //     Debug.DrawRay(viewer.position, direction, Color.yellow);
            //     Debug.Log("IsInFOVScope => Did Hit : " + hit.transform.name);
            //     return false;
            // }
            // else
            // {
            //     Debug.DrawRay(viewer.position, direction, Color.green);
            //     Debug.Log("IsInFOVScope => Did not Hit");
            //     if (hit.transform != null)
            //     {
            //         Debug.Log("IsInFOVScope => hit =" + hit.transform.gameObject.name);
            //     }
            //     return true;
            // }

            Debug.DrawRay(viewer.position, direction, Color.green);
            return true;
        }
        //TO DO: fix LOS
        // return (angle <= maxAngle);
        return false;
    }
    public static bool IsTargetDetectable(Transform viewer, Vector3 target, float maxAngle, float maxRadius)
    {
        return (Vector3.Distance(viewer.position, target) < 3.5f || IsInFOVScope(viewer, target, maxAngle, maxRadius));
    }

}