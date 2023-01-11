using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProceduralBase
{
    public static class LocationUtility
    {
        public static Vector3[] GetLocationSubzonePoints(LocationPrototype locationPrototype)
        {
            Vector3[] subzonePoints = new Vector3[locationPrototype.subzonePrototypes.Count];
            for (int i = 0; i < locationPrototype.subzonePrototypes.Count; i++)
            {
                subzonePoints[i] = locationPrototype.subzonePrototypes[i].position;
            }
            return subzonePoints;
        }
        public static List<Vector3> GetLocationSubzonePointsList(LocationPrototype locationPrototype)
        {

            List<Vector3> subzonePoints = new List<Vector3>();
            foreach (SubzonePrototype item in locationPrototype.subzonePrototypes)
            {
                subzonePoints.Add(item.position);
            }
            return subzonePoints;
        }
        public static Vector3[] GetLocationZoneConnectors(LocationPrototype locationPrototype)
        {
            Vector3[] subzoneConnectors = new Vector3[locationPrototype.subzoneConnectors.Count];
            for (int i = 0; i < subzoneConnectors.Length; i++)
            {
                subzoneConnectors[i] = locationPrototype.subzoneConnectors[i].position;

            }
            return subzoneConnectors;
        }
    }
}
