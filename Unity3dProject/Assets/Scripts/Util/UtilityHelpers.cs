using System;
using System.Collections;
using System.Collections.Generic;
using Hybrid.Components;
using UnityEngine;
using UnityEngine.AI;
using Unity.Entities;
using System.Linq;

static class UtilityHelpers
{

    public static void LogTime(DateTime startTime, string str)
    {
        TimeSpan duration = DateTime.Now - startTime;
        if (duration.TotalSeconds < 60)
        {
            Debug.LogError($"{str} - Time: {duration.TotalSeconds} seconds");
        }
        else Debug.LogError($"{str} - Time: {duration.TotalMinutes} minutes");
    }

    public static List<Vector2> SelectRandomKeys(Dictionary<Vector2, Vector2> dictionary, int count)
    {
        // Convert the dictionary keys to a list
        List<Vector2> keys = new List<Vector2>(dictionary.Keys);

        // Create a random number generator
        System.Random random = new System.Random();

        // Shuffle the keys using Fisher-Yates algorithm
        for (int i = keys.Count - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            Vector2 temp = keys[i];
            keys[i] = keys[j];
            keys[j] = temp;
        }

        // Take the specified number of keys from the shuffled list
        List<Vector2> selectedKeys = keys.Take(count).ToList();
        return selectedKeys;
    }

    public static float RoundHeightToNearestElevation(float height, int elevationStep)
    {
        int roundedHeight = Mathf.RoundToInt(height / elevationStep) * elevationStep;
        return roundedHeight;
    }

    public static bool IsValueWithinRange(float value, Vector2 rangeMinMax) => (value < rangeMinMax.y && value > rangeMinMax.x);
    public static int Find_IndexOfFirstRangeContainingValue(float value, List<Vector2> rangeList)
    {
        int index = -1;
        for (int i = 0; i < rangeList.Count; i++)
        {
            if (IsValueWithinRange(value, rangeList[i]))
            {
                index = i;
                break;
            }
        }
        return index;
    }

    public static List<Vector2> Evaluate_NoiseRangeChunks(float rangeMin, float rangeMax, int chunks, float weightMult)
    {
        List<Vector2> chunkRanges = new List<Vector2>();

        float chunkSize = (rangeMax - rangeMin) / chunks;

        float currentMin = rangeMin;
        for (int i = 0; i < chunks; i++)
        {
            float chunkWeight = Mathf.Pow(weightMult, i);
            float chunkRangeMin = currentMin;
            float chunkRangeMax = currentMin + (chunkSize * chunkWeight);

            // Ensure the chunk range stays within the overall range
            chunkRangeMin = Mathf.Clamp(chunkRangeMin, rangeMin, rangeMax);
            chunkRangeMax = Mathf.Clamp(chunkRangeMax, rangeMin, rangeMax);

            Vector2 chunkRange = new Vector2(chunkRangeMin, chunkRangeMax);
            chunkRanges.Add(chunkRange);

            currentMin = chunkRangeMax;
        }

        return chunkRanges;
    }

    public static string GenerateUniqueID(GameObject gameObject)
    {
        // Generate a new unique identifier for the object
        Guid guid = Guid.NewGuid();
        string _uid = $"{gameObject.GetInstanceID()}-{guid}";
        return _uid;
    }

    public static string GenerateUniqueID(string partial)
    {
        // Generate a new unique identifier for the object
        Guid guid = Guid.NewGuid();
        string _uid = $"{partial}-{guid}";
        return _uid;
    }

    public static Dictionary<string, Color> CustomColorDefaults()
    {
        Dictionary<string, Color> colors = new Dictionary<string, Color>() {
                {"brown", new Color(0.4f, 0.2f, 0f) },
                {"orange",  new Color(1f, 0.5f, 0f) },
                {"purple", new Color(0.8f, 0.2f, 1f) },
            };

        return colors;
    }

    public static List<Color> GenerateUniqueRandomColors(int count)
    {
        List<Color> colors = new List<Color>();

        for (int i = 0; i < count; i++)
        {
            Color color = UnityEngine.Random.ColorHSV(0f, 1f, 0.5f, 1f, 0.5f, 1f);
            while (colors.Contains(color))
            {
                color = UnityEngine.Random.ColorHSV(0f, 1f, 0.5f, 1f, 0.5f, 1f);
            }
            colors.Add(color);
        }

        return colors;
    }
    public static float RoundToNearestStep(float value, float step)
    {
        return Mathf.Round(value / step) * step;
    }

    public static bool Vector3HasNaN(Vector3 vector)
    {
        return Mathf.Approximately(vector.x, float.NaN) ||
               Mathf.Approximately(vector.y, float.NaN) ||
               Mathf.Approximately(vector.z, float.NaN);
    }

    public static float CalculateSquareMilesFromRadius(float radius)
    {
        float area = Mathf.PI * radius * radius;
        float squareMiles = area / 2589988f; // number of square meters in a square mile
        return squareMiles;
    }

    public static float CalculateAverage(float valueA, float valueB)
    {
        float average = (valueA + valueB) / 2f;
        return average;
    }

    public static float CalculateAverage(List<float> numbers)
    {
        if (numbers == null || numbers.Count == 0)
        {
            // Return a default value or handle the empty list case
            // Here, we choose to return float.NaN to indicate an invalid average
            return float.NaN;
        }

        float sum = 0f;
        foreach (float number in numbers)
        {
            sum += number;
        }

        float average = sum / numbers.Count;
        return average;
    }

    public static float CalculateAverageOfArray(float[] elevations)
    {
        float sum = 0.0f;

        for (int i = 0; i < elevations.Length; i++)
        {
            sum += elevations[i];
        }

        float average = sum / elevations.Length;
        return average;
    }

    public static int ChooseDecision(Dictionary<int, float> decisions)
    {
        float totalWeight = decisions.Values.Sum();
        float randomValue = UnityEngine.Random.Range(0, totalWeight);
        float currentWeight = 0;

        foreach (KeyValuePair<int, float> decision in decisions)
        {
            currentWeight += decision.Value;
            if (randomValue <= currentWeight)
            {
                return decision.Key;
            }
        }

        return -1; // Return -1 if no decision is chosen (this should not happen)
    }

    public static GameObject FindGameObject(string tag, string name)
    {
        GameObject[] taggedObjects = GameObject.FindGameObjectsWithTag(tag);

        foreach (GameObject obj in taggedObjects)
        {
            if (obj.name == name)
            {
                return obj;
            }
        }

        return null;
    }
    public static List<GameObject> FindGameObjectsWithTagInChildren(Transform parent, string tag)
    {
        List<GameObject> gameObjects = new List<GameObject>();
        GameObject[] children = parent.GetComponentsInChildren<GameObject>();

        foreach (GameObject child in children)
        {
            if (child.CompareTag(tag)) gameObjects.Add(child);
        }

        return gameObjects;
    }


    public static Vector3 FaceAwayFromPoint(Vector3 point, Vector3 vector)
    {
        Vector3 facingAway = vector - point;
        facingAway = facingAway.normalized;
        return facingAway;
    }
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

    public static bool IsInFOVScope(Transform viewer, Vector3 targetPos, float maxAngle, float maxRadius)
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
