using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Hybrid.Components
{
    public class Targeting : MonoBehaviour {
        public Transform currentTarget = null;
        public Transform lastTarget = null;
        public Transform closestTransform = null;
        public Vector3 navPosition;
        public HashSet<Transform> enemies = new HashSet<Transform>();
        public HashSet<Transform> allies = new HashSet<Transform>();
    }
}

