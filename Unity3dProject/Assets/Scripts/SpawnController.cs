using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;

public class SpawnController : MonoBehaviour
{
    public GameObject prefab;
    public float radius = 10f;
    public static SpawnController current;

    private Entity entity;
    private EntityManager entityManager;
    private BlobAssetStore blobAssetStore;

    private void Awake()
    {
        current = this;

        // entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        // blobAssetStore = new BlobAssetStore();
        // GameObjectConversionSettings settings = GameObjectConversionSettings.FromWorld(World.DefaultGameObjectInjectionWorld, blobAssetStore);
        // entity = GameObjectConversionUtility.ConvertGameObjectHierarchy(prefab, settings);
    }
    private void Update()
    {
        // Debug.Log("RUNNING!");
        if (Input.GetKeyDown(KeyCode.P))
        {
            Debug.Log("SPAWN!");
            // Entity newEntity = entityManager.Instantiate(entity);

            Vector3 spawnPos = UtilityHelpers.GetRandomNavmeshPoint(radius, this.transform.position);
            Instantiate(prefab, new Vector3(spawnPos.x, 3, spawnPos.z), Quaternion.identity);
        }
    }
    // private void OnDestroy()
    // {
    //     blobAssetStore.Dispose();
    // }
}
