using System;
using System.Collections;
using System.Collections.Generic;
using Hybrid.Components;
using UnityEngine;
using UnityEngine.AI;
using Unity.Entities;

static class UtilityHelpers
{
    public static Vector3[] GetTransformPositions(Transform[] transforms)
    {
        Vector3[] positions = new Vector3[transforms.Length];
        for (int i = 0; i < transforms.Length; i++)
        {
            positions[i] = transforms[i].position;
        }
        return positions;
    }

    public static Transform[] ConvertVector3sToTransformPositions(Vector3[] points, Transform parent)
    {
        Transform[] transforms = new Transform[points.Length];
        for (int i = 0; i < points.Length; i++)
        {
            // Instantiate a new empty GameObject
            GameObject newObject = new GameObject();
            // Access the Transform component of the new GameObject
            Transform newTransform = newObject.transform;
            newTransform.position = points[i];
            Transform t = Transform.Instantiate(newTransform, points[i], Quaternion.identity);
            t.position = points[i];
            transforms[i] = t;

            t.SetParent(parent);
        }
        return transforms;
    }

    public static string GetUnsetActorEntityRefId() => "-";

    public static string getActorEntityRefId(Entity entity)
    {
        return $"ref#{entity.Version}{entity.Index}";
    }

    public static Vector4 GenerateRandomRGB() =>
        new Vector4(UnityEngine.Random.Range(0.0f, 1.0f),
            UnityEngine.Random.Range(0.0f, 1.0f),
            UnityEngine.Random.Range(0.0f, 1.0f),
            1f);

    public static bool
    GetCloseNavMeshPoint(
        Vector3 center,
        float range,
        out Vector3 result,
        int attempts = 30
    )
    {
        for (int i = 0; i < attempts; i++)
        {
            Vector3 randomPoint =
                center + UnityEngine.Random.insideUnitSphere * range;
            NavMeshHit hit;
            if (
                NavMesh
                    .SamplePosition(randomPoint,
                    out hit,
                    1.0f,
                    NavMesh.AllAreas)
            )
            {
                result = hit.position;
                return true;
            }
        }
        result = Vector3.zero;
        return false;
    }

    public static bool
    GetCloseNavMeshPointInsideBounds(
        Bounds bounds,
        out Vector3 result,
        int attempts = 30
    )
    {
        Vector3 center = bounds.center;
        float range = Vector3.Distance(bounds.center, bounds.max);

        for (int i = 0; i < attempts; i++)
        {
            Vector3 randomPoint =
                center + UnityEngine.Random.insideUnitSphere * range;
            NavMeshHit hit;
            if (
                NavMesh
                    .SamplePosition(randomPoint,
                    out hit,
                    1.0f,
                    NavMesh.AllAreas)
            )
            {
                if (bounds.Contains(hit.position))
                {
                    result = hit.position;
                    return true;
                }
            }
        }
        result = Vector3.zero;
        return false;
    }

    public static Vector3 GetRandomNavmeshPoint(float radius, Vector3 center)
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

    public static Vector3
    GetFrontPosition(Transform target, float distanceAhead)
    {
        return target.position + (target.forward * distanceAhead);
    }

    public static Vector3
    GetBehindPosition(Transform target, float distanceBehind)
    {
        return target.position - (target.forward * distanceBehind);
    }

    public static Vector3
    GetHoldPositionBorderEdge(
        Vector3 holdPosition,
        Vector3 direction,
        float radius
    )
    {
        return holdPosition + (direction.normalized * radius);
    }

    public static float getFOVAngle(Transform viewer, Vector3 targetPos)
    {
        Vector3 directionBetween = (targetPos - viewer.position).normalized;
        directionBetween.y *= 0;
        return Vector3.Angle(viewer.forward, directionBetween);
    }

    public static bool
    HasFOVAngle(
        Transform viewer,
        Vector3 targetPos,
        float maxAngle,
        float maxRadius
    )
    {
        float angle = getFOVAngle(viewer, targetPos);
        return (angle <= maxAngle);
    }

    public static bool HasShotLinedUp(Transform viewer, Vector3 targetPos)
    {
        float angle = getFOVAngle(viewer, targetPos);

        // Vector3 fovLine1 = Quaternion.AngleAxis(angle, viewer.transform.up) * viewer.transform.forward * 10;
        // Vector3 fovLine2 = Quaternion.AngleAxis(-angle, viewer.transform.up) * viewer.transform.forward * 10;
        // Gizmos.color = Color.yellow;
        // Debug.DrawRay(viewer.transform.position, fovLine1);
        // Debug.DrawRay(viewer.transform.position, fovLine2);
        return (angle <= 12f) && HasLOS(viewer, targetPos);
    }

    public static bool HasLOS(Transform viewer, Vector3 targetPos)
    {
        int layerMask = LayerMask.GetMask("BlockLOS");

        Vector3 tarPos = targetPos + (Vector3.up * 1.2f);
        Vector3 direction = (targetPos - viewer.position);

        RaycastHit[] raycastHits =
            Physics
                .RaycastAll(viewer.position,
                direction,
                Mathf.Infinity,
                layerMask);
        if (raycastHits.Length > 0)
        {
            float targetDist = Vector3.Distance(viewer.position, targetPos);
            if (
                LayerMask
                    .LayerToName(raycastHits[0].transform.gameObject.layer) !=
                "Actor"
            )
            {
                // Debug.Log("IsInFOVScope => First Hit : " + raycastHits[0].transform.name + " Layer: " + LayerMask.LayerToName(raycastHits[0].transform.gameObject.layer));
                // Debug.DrawRay(viewer.position, direction, Color.blue);
                return false;
            }
        }

        // Debug.DrawRay(viewer.position, direction, Color.yellow);
        return true;
    }

    public static bool
    IsInFOVScope(
        Transform viewer,
        Vector3 targetPos,
        float maxAngle,
        float maxRadius
    )
    {
        float angle = getFOVAngle(viewer, targetPos);

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
            Vector3 tarPos = targetPos + (Vector3.up * 1.2f);
            Vector3 direction = (targetPos - viewer.position);

            RaycastHit[] raycastHits =
                Physics
                    .RaycastAll(viewer.position,
                    direction,
                    Mathf.Infinity,
                    layerMask);

            // Debug.Log("IsInFOVScope => Did Hits : " + raycastHits.Length);
            if (raycastHits.Length > 0)
            {
                float targetDist = Vector3.Distance(viewer.position, targetPos);

                // Debug.Log("IsInFOVScope => distance compare - Hit:  " + raycastHits[0].distance + " / target: " + targetDist);
                // if (raycastHits[0].distance < targetDist && LayerMask.LayerToName(raycastHits[0].transform.gameObject.layer) != "Actor")
                if (
                    LayerMask
                        .LayerToName(raycastHits[0]
                            .transform
                            .gameObject
                            .layer) !=
                    "Actor"
                )
                {
                    // Debug.Log("IsInFOVScope => First Hit : " + raycastHits[0].transform.name + " Layer: " + LayerMask.LayerToName(raycastHits[0].transform.gameObject.layer));
                    // Debug.DrawRay(viewer.position, direction, Color.blue);
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
            // Debug.DrawRay(viewer.position, direction, Color.green);
            return true;
        }

        //TO DO: fix LOS
        // return (angle <= maxAngle);
        return false;
    }

    public static bool
    IsTargetDetectable(
        Transform viewer,
        Vector3 target,
        float maxAngle,
        float maxRadius
    )
    {
        return (
        Vector3.Distance(viewer.position, target) < 3.5f ||
        IsInFOVScope(viewer, target, maxAngle, maxRadius)
        );
    }

    public static Vector3 FindCenterOfTransforms(List<Transform> transforms)
    {
        if (transforms.Count == 1) return transforms[0].position;

        Bounds bound = new Bounds(transforms[0].position, Vector3.zero);
        for (int i = 1; i < transforms.Count; i++)
        {
            bound.Encapsulate(transforms[i].position);
        }
        return bound.center;
    }

    public static Vector3 Between(Vector3 v1, Vector3 v2, float percentage)
    {
        return (v2 - v1) * percentage + v1;
    }


    #region Spell Methods
    public static void CastMagicSpellEffects(
        MagicSpell magicSpell,
        GameObject target,
        GameObject sender,
        ProjectileType projectileType = ProjectileType.spell
    )
    {
        Transform spell =
            GameObject.Instantiate(magicSpell.spellPrefab.transform);
        ActiveSpellController spellController =
            spell.gameObject.GetComponent<ActiveSpellController>();
        if (spellController != null)
        {
            spellController
                .FireMagicEffects(new SpellInstanceData(sender,
                    target.transform,
                    projectileType,
                    magicSpell));
        }
    }

    public static void CastMovementSpell(
        MagicSpell magicSpell,
        SpellInstanceData spellInstanceData
    )
    {
        Transform spell =
            GameObject.Instantiate(magicSpell.spellPrefab.transform);
        ActiveSpellController spellController =
            spell.gameObject.GetComponent<ActiveSpellController>();
        if (spellController != null)
        {
            spellController.FireMagicEffects(spellInstanceData);
        }
    }


    #endregion



    #region Angle Trajectory Methods

    public static float
    MagnitudeToReachXYInGravityAtAngle(Vector2 XY, float gravity, float angle)
    {
        float res = 0;
        float sin2Theta = Mathf.Sin(2 * angle * Mathf.Deg2Rad);
        float cosTheta = Mathf.Cos(angle * Mathf.Deg2Rad);
        float inner =
            (XY.x * XY.x * gravity) /
            (XY.x * sin2Theta - 2 * XY.y * cosTheta * cosTheta);
        if (inner < 0)
        {
            return float.NaN;
        }
        res = Mathf.Sqrt(inner);
        return res;
    }

    public static float
    AngleToReachXYInMagnitude(Vector2 XY, float gravity, float magnitude)
    {
        float innerSq =
            Mathf.Pow(magnitude, 4) -
            gravity *
            (gravity * XY.x * XY.x + 2 * XY.y * magnitude * magnitude);
        if (innerSq < 0)
        {
            return float.NaN;
        }
        float innerATan =
            (magnitude * magnitude + Mathf.Sqrt(innerSq)) / (gravity * XY.x);
        float res = Mathf.Atan(innerATan) * Mathf.Rad2Deg;
        return res;
    }


    #endregion
}
