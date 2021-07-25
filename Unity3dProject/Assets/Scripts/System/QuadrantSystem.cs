using System.ComponentModel;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Transforms;
using Unity.Burst;
using Unity.Jobs;
using Hybrid.Components;

public struct QuadrantEntity : IComponentData {
    public TypeEnum typeEnum;
    public enum TypeEnum {
        Unit,
        Target
    }
}

public struct QuadrantData {
    public Entity entity;
    public float3 position;
    // public Transform transform;
    public QuadrantEntity quadrantEntity;
}

public class QuadrantSystem : ComponentSystem
{
    private GameObject playerGameObject; 
    public static NativeMultiHashMap<int, QuadrantData> quadrantMultiHashMap;
    private const int quadrantYMultiplier = 1000;
    private const int quadrantCellSize = 20;

    public static int GetPositionHashMapKey(float3 position) {
        return (int) (math.floor(position.x / quadrantCellSize) + (quadrantYMultiplier * math.floor(position.z / quadrantCellSize)));
    }

    private static void DebugDrawQuadrant(float3 position) {
        Vector3 lowerLeft = new Vector3(math.floor(position.x / quadrantCellSize) * quadrantCellSize, 0,math.floor(position.z / quadrantCellSize) * quadrantCellSize);
        Debug.DrawLine(lowerLeft, lowerLeft + new Vector3(+1, 0, +0) * quadrantCellSize);
        Debug.DrawLine(lowerLeft, lowerLeft + new Vector3(+0, 0, +1) * quadrantCellSize);
        Debug.DrawLine(lowerLeft + new Vector3(+1, 0, +0) * quadrantCellSize, lowerLeft + new Vector3(+1, 0, +1) * quadrantCellSize);
        Debug.DrawLine(lowerLeft + new Vector3(+0, 0, +1) * quadrantCellSize, lowerLeft + new Vector3(+1, 0, +1) * quadrantCellSize);
        // Debug.Log(GetPositionHashMapKey(position) + " " + position);
    }

    private static int GetEnityCountInHashMap(NativeMultiHashMap<int, QuadrantData> quadrantMultiHashMap, int hashMapKey) {
        QuadrantData quadrantData;
        NativeMultiHashMapIterator<int> nativeMultiHashMapIterator;
        int count = 0;
        if (quadrantMultiHashMap.TryGetFirstValue(hashMapKey, out quadrantData, out nativeMultiHashMapIterator)) {
            do {
                count++;
            } while (quadrantMultiHashMap.TryGetNextValue(out quadrantData, ref nativeMultiHashMapIterator));
        }
        return count;
    }

    private static Vector3[] GetNeighoborQuadrantPositions(Vector3 currentPosition) {
        Vector3[] neighborPositions = new Vector3[8];

        neighborPositions[0] = currentPosition + Vector3.forward * quadrantCellSize;
        neighborPositions[1] = currentPosition + Vector3.back * quadrantCellSize;
        neighborPositions[2] = currentPosition + Vector3.left * quadrantCellSize;
        neighborPositions[3] = currentPosition + Vector3.right * quadrantCellSize;

        neighborPositions[4] = currentPosition + (Vector3.right + Vector3.forward) * quadrantCellSize;
        neighborPositions[5] = currentPosition + (Vector3.left + Vector3.forward) * quadrantCellSize;
        neighborPositions[6] = currentPosition + (Vector3.right + Vector3.back) * quadrantCellSize;
        neighborPositions[7] = currentPosition + (Vector3.left + Vector3.back) * quadrantCellSize;
        return neighborPositions;
    }
    
    // [BurstCompile]
    // private struct SetQuadrantDataHashMapJob : IJobForEachWithEntity<Translation, QuadrantEntity> {
    //     public NativeMultiHashMap<int, QuadrantData>.ParallelWriter quadrantMultiHashMap;
    //     public void Execute(Entity entity, int index, ref Translation trans, ref QuadrantEntity quadrantEntity) {
    //         int hashMapKey = GetPositionHashMapKey(trans.Value);
    //         quadrantMultiHashMap.Add(hashMapKey, new QuadrantData {
    //             entity = entity,
    //             position = trans.Value,
                    // transform = transform,
    //             quadrantEntity = quadrantEntity
    //         });
    //     }
    // }
    
    protected override void OnCreate () {
        quadrantMultiHashMap = new NativeMultiHashMap<int, QuadrantData>(0, Allocator.Persistent);
        base.OnCreate();
    }

    protected override void OnDestroy() {
        quadrantMultiHashMap.Dispose();    
        base.OnDestroy();        
    }

    protected override void OnUpdate() {
        EntityQuery entityQuery = GetEntityQuery(typeof(QuadrantSearchable));
        // Debug.Log("Running...");

        quadrantMultiHashMap.Clear();

        //Expand to fit 
        if (entityQuery.CalculateEntityCount() > quadrantMultiHashMap.Capacity) {
            quadrantMultiHashMap.Capacity = entityQuery.CalculateEntityCount();
        }

        Entities.ForEach((Entity entity, IsActor actor) => {
            int hashMapKey = GetPositionHashMapKey(actor.gameObject.transform.position);
            quadrantMultiHashMap.Add(hashMapKey, new QuadrantData{
                entity = entity,
                position = actor.gameObject.transform.position,
                // transform = actor.gameObject.transform
                // quadrantEntity = quadrantEntity
            });
        });

        // TODO: Make this work with the JobHandler instead of the above
        // SetQuadrantDataHashMapJob setQuadrantDataHashMapJob = new SetQuadrantDataHashMapJob {
        //     quadrantMultiHashMap = quadrantMultiHashMap.AsParallelWriter(),
        // };
        // JobHandle jobHandle = JobForEachExtensions.Schedule(setQuadrantDataHashMapJob, entityQuery);
        // jobHandle.Complete();

        if (!playerGameObject) {
            Entities.WithAll<Target, Transform>().ForEach((Transform tran) => {
                if (tran.gameObject.tag == "Player") {
                    playerGameObject = tran.gameObject;
                    Debug.Log("Player GameObject found: " + tran.gameObject.name);
                }
            });            
        }

        if (playerGameObject) {
            Vector3 playerPos = playerGameObject.transform.position;
            DebugDrawQuadrant(playerPos);

            Vector3[] neighborPositions = GetNeighoborQuadrantPositions(playerPos);
            
            for (int i = 0; i < neighborPositions.Length; i++)
            {
                DebugDrawQuadrant(neighborPositions[i]);
            }

            // int quadrantNeighborCellSize = quadrantCellSize; //math.abs(quadrantCellSize / 2);

            // Vector3 posForward = playerPos + Vector3.forward * quadrantNeighborCellSize;
            // Vector3 posBack = playerPos + Vector3.back * quadrantNeighborCellSize;
            // Vector3 posLeft = playerPos + Vector3.left * quadrantNeighborCellSize;
            // Vector3 posRight = playerPos + Vector3.right * quadrantNeighborCellSize;

            // Vector3 posForwardRight = playerPos + (Vector3.right + Vector3.forward) * quadrantNeighborCellSize;
            // Vector3 posForwardLeft = playerPos + (Vector3.left + Vector3.forward) * quadrantNeighborCellSize;
            // Vector3 posBackRight = playerPos + (Vector3.right + Vector3.back) * quadrantNeighborCellSize;
            // Vector3 posBackLeft = playerPos + (Vector3.left + Vector3.back) * quadrantNeighborCellSize;

            // DebugDrawQuadrant(posForwardRight);
            // DebugDrawQuadrant(posForwardLeft);
            // DebugDrawQuadrant(posBackLeft);
            // DebugDrawQuadrant(posBackRight);
            
            // DebugDrawQuadrant(posForward);
            // DebugDrawQuadrant(posBack);
            // DebugDrawQuadrant(posLeft);
            // DebugDrawQuadrant(posRight);

            Debug.Log("\nActors in " + playerGameObject.name + "'s current quadrant: " + GetEnityCountInHashMap(quadrantMultiHashMap, GetPositionHashMapKey(playerGameObject.transform.position)));
        }
    }


}
