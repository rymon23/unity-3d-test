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
    public EntityManager entityManager;
    private BlobAssetStore blobAssetStore;

    private void Awake()
    {
        current = this;

        entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        blobAssetStore = new BlobAssetStore();
        GameObjectConversionSettings settings = GameObjectConversionSettings.FromWorld(World.DefaultGameObjectInjectionWorld, blobAssetStore);
        entity = GameObjectConversionUtility.ConvertGameObjectHierarchy(prefab, settings);
    }


    [SerializeField] private bool bSpawn = false;
    private void Update()
    {
        // Debug.Log("RUNNING!");
        if (bSpawn)
        // if (Input.GetKeyDown(KeyCode.P))
        {
            bSpawn = false;

            Entity newEntity = entityManager.Instantiate(entity);
            Debug.Log("SPAWN!");


            // Vector3 spawnPos = UtilityHelpers.GetRandomNavmeshPoint(radius, this.transform.position);
            // Instantiate(prefab, new Vector3(spawnPos.x, 3, spawnPos.z), Quaternion.identity);
        }
    }
    private void OnDestroy()
    {
        blobAssetStore.Dispose();
    }
}
