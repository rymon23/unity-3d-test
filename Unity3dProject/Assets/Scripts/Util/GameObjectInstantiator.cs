using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using Unity.Entities;
using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Rendering;


public class GameObjectInstantiator : MonoBehaviour
{
    private List<GameObject> instantiatedObjects = new List<GameObject>();

    public void InstantiateObjectsInParallel(List<GameObject> objectPrefabs, List<Vector3> positions)
    {
        int objectCount = objectPrefabs.Count;
        CountdownEvent countdownEvent = new CountdownEvent(objectCount);

        for (int i = 0; i < objectCount; i++)
        {
            int prefabIndex = i; // Capture the index variable

            // Queue the object instantiation to the thread pool
            ThreadPool.QueueUserWorkItem(_ =>
            {
                GameObject instantiatedObject = Instantiate(objectPrefabs[prefabIndex], positions[prefabIndex], Quaternion.identity);
                lock (instantiatedObjects) // Synchronize access to the instantiatedObjects list
                {
                    instantiatedObjects.Add(instantiatedObject);
                }
                countdownEvent.Signal(); // Notify the countdown event that the object instantiation is complete
            });
        }

        // Wait until all objects are instantiated
        countdownEvent.Wait();
        countdownEvent.Dispose();

        // Do something with the instantiated objects if needed
        foreach (GameObject instantiatedObject in instantiatedObjects)
        {
            // ...
        }
    }
}
