using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Hybrid.Components
{
    public enum FollowState
    {
        idle = 0,
        catch_up = 1,
        fallback = 2
    }

    public enum FollowBehavior
    {
        relaxed = 0,
        keep_close = 1,
        take_point = 2,
        tail = 3,
        go_to = 4
    }

    public class Follower : MonoBehaviour
    {
        [SerializeField]
        private Transform target;

        [SerializeField]
        private Vector3 goal = Vector3.zero;

        [SerializeField]
        public float distanceMin = 3f;

        [SerializeField]
        public float distanceMax = 19f;

        [SerializeField]
        public float takePointDistanceMult = 0.5f;

        [SerializeField]
        public float tailDistanceMult = 0.7f;

        [SerializeField]
        public float relaxDistanceMult = 0.6f;

        public FollowBehavior followBehavior = FollowBehavior.relaxed;

        //TEMP
        public float targetDistance;

        public float minAheadDist;

        public float maxTargetPositionRadius;

        public FollowState followState = FollowState.idle;

        public Transform GetTarget() => target;

        public Vector3 GetGoal() => goal;

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }

        public void SetGoal(Vector3 newGoal)
        {
            goal = newGoal;
        }

        public bool HasTarget() => target != null;

        public float GetFollowBehaviorDistanceMax()
        {
            switch (followBehavior)
            {
                case FollowBehavior.relaxed:
                    return distanceMax * relaxDistanceMult;
                case FollowBehavior.take_point:
                    return distanceMax * takePointDistanceMult;
                case FollowBehavior.tail:
                    return distanceMax * tailDistanceMult;
                default:
                    return distanceMin * 1.1f;
            }
        }

        public void EvaluateFollowBehavior(bool bRandom = true)
        {
            followBehavior = GetRandomFollowBehavior();
            Debug
                .Log("EvaluateFollowBehavior: " +
                this.name +
                " - " +
                followBehavior);
        }

        public FollowBehavior GetRandomFollowBehavior()
        {
            return (FollowBehavior)
            UnityEngine
                .Random
                .Range(0, Enum.GetNames(typeof (FollowBehavior)).Length - 1);
        }

        public bool debug_gizmo = true;

        private void OnDrawGizmos()
        {
            if (!debug_gizmo) return;

            if (target != null)
            {
                Gizmos.color = Color.green;
                Gizmos
                    .DrawWireSphere(target.position,
                    GetFollowBehaviorDistanceMax());
            }

            if (goal != Vector3.zero)
            {
                Gizmos.color = Color.white;
                Gizmos.DrawWireSphere(goal, GetFollowBehaviorDistanceMax());
            }
        }
    }
}
