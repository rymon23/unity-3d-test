using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Hybrid.Components
{
    public struct TrackedTarget
    {
        public TrackedTarget(string _refId, FactionRelationship _relationship = FactionRelationship.unset, float _targetLostTimer = 23f, float _priority = 100f)
        {
            refId = _refId;
            targetLostTimer = _targetLostTimer;
            priority = _priority;
            relationship = _relationship;
        }
        public string refId { get; }
        public float targetLostTimer { get; set; }
        public float priority { get; set; }
        public FactionRelationship relationship { get; set; }
    }

    public class Targeting : MonoBehaviour
    {
        [SerializeField]
        public string refId = "-";
        public Transform currentTarget = null;

        public string currentTargetRefId; // Temporary for debugging
        public float currentTargetLostTimer; // Temporary for debugging

        public Transform currentNavGoal = null;
        public HashSet<string> trackedTargets = null;
        public Hashtable trackedTargetStats = null;
        public Hashtable allies = null;
        public Vector3 alertedPosition = Vector3.zero;
        public Vector3 searchCenterPos = Vector3.zero;
        public Vector3 fallbackPosition = Vector3.zero;
        public Vector3 attackPos = Vector3.zero;

        public int allycount = 0;
        public int enemycount = 0;

        public int trackedTargetCount;
        // public HashSet<string> trackedGoals = null;
        public Transform closestTarget = null;
        public float targetDistance;
        public float targetAttackDistance;

        public bool hasTargetInFOV = false;
        public Transform lastTarget = null;
        public float searchRadius;
        public float targetLostTimer;
    }
}

