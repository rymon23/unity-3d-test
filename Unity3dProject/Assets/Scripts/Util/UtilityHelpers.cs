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


    public static bool IsInFOVScope(Transform viewer, Vector3 targetPos, float maxAngle, float maxRadius)
    {
        float angle = getFOVAngle(viewer, targetPos, maxAngle, maxRadius);
        //TO DO: fix LOS
        // if (angle <= maxAngle)
        // {
        //     Ray ray = new Ray(viewer.position, targetPos - viewer.position);
        //     RaycastHit hit;

        //     if (Physics.Raycast(ray, out hit, maxRadius))  return hit.transform.position == targetPos;
        // }
        return (angle <= maxAngle);
    }

    public static bool IsTargetDetectable(Transform viewer, Vector3 targetPos, float maxAngle, float maxRadius)
    {
        return (IsInFOVScope(viewer, targetPos, maxAngle, maxRadius) || Vector3.Distance(viewer.position, targetPos) < 3.5f);
    }

}