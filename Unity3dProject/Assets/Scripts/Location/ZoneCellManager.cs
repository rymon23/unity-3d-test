using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ZoneCellManager : MonoBehaviour
{
    [Header("Manager Settings")]
    [Range(1, 256)][SerializeField] private int maxSize;
    [Range(1, 128)][SerializeField] private int maxBuildings;
    [Range(1, 32)][SerializeField] private int minBuildingLevels;
    [Range(1, 32)][SerializeField] private int maxBuildingLevels;
    [Range(1, 12)][SerializeField] private int minEntrances;
    [Range(1, 12)][SerializeField] private int maxEntrances;

    [Header("Probability Settings")]
    [Range(0.05f, 1f)][SerializeField] private float borderWall = 1f;
    [Range(0.05f, 1f)][SerializeField] private float buidingDensity = 1f;
}
