using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Hybrid.Components
{
    public class ActorNavigationData : MonoBehaviour
    {
        public float wanderRadius = 25f;
        public float positionReachedDistanceMin = 5f;
        public Vector3 wanderCenterPosition = Vector3.zero;
        public Vector3 travelPosition = Vector3.zero;

        private void Start() {
            if (wanderCenterPosition == Vector3.zero) {
                wanderCenterPosition = transform.position;
            }
        }
    }
}