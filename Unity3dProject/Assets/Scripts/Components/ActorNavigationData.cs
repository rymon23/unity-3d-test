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

        public List<Transform> pastWaypoints = new List<Transform>();

        public Transform currentWaypoint;

        private void Start()
        {
            if (wanderCenterPosition == null)
            {
                wanderCenterPosition = transform;
            }
        }

        //TEMP
        bool bShouldHoldPosition = true;

        public float holdPositionRadius = 8f;
    }
}
