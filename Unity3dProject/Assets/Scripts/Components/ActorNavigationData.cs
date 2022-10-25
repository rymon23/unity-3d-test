using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Hybrid.Components
{
    public class ActorNavigationData : MonoBehaviour
    {
        public float wanderRadius = 25f;

        public float positionReachedDistanceMin = 5f;

        public Transform wanderCenterPosition;

        public Transform travelPosition;

        public Transform holdPositionCenter;

        public float holdPositionRadius = 12f;

        public bool bShouldHoldPosition = true;

        public List<Transform> pastWaypoints = new List<Transform>();

        public Transform currentWaypoint;

        public void UpdateHoldPositionData(
            Transform newLocation,
            float newRadius = float.NaN
        )
        {
            if (holdPositionCenter != newLocation)
                holdPositionCenter = newLocation;
            if (newRadius != float.NaN) holdPositionRadius = newRadius;
        }

        private void Start()
        {
            if (wanderCenterPosition == null)
            {
                wanderCenterPosition = transform;
            }
        }
    }
}
