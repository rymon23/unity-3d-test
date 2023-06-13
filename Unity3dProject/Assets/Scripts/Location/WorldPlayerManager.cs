using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WFCSystem;

namespace ProceduralBase
{
    public class WorldPlayerManager : MonoBehaviour
    {
        [SerializeField] private WorldAreaManager worldAreaManager;

        private void FixedUpdate()
        {
            if (worldAreaManager != null)
            {
                worldAreaManager.Evaluate_TrackedPosition();
            }
        }
    }
}

