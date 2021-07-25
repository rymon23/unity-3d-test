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
    public QuadrantEntity quadrantEntity;
}

public class QuadrantSystem : ComponentSystem
{
    private GameObject playerGameObject; 
    private static NativeMultiHashMap<int, QuadrantData> quadrantMultiHashMap;
    private const int quadrantYMultiplier = 1000;
    private const int quadrantCellSize = 20;

    protected static int GetPositionHashMapKey(float3 position) {
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
    // private static int GetPositionHashMapKey(float3 position) {
    //     return (int) (math.floor(position.x / quadrantCellSize) + (quadrantYMultiplier * math.floor(position.y / quadrantCellSize)));
    // }
    // private static void DebugDrawQuadrant(float3 position) {
    //     Vector3 lowerLeft = new Vector3(math.floor(position.x / quadrantCellSize) * quadrantCellSize, math.floor(position.y / quadrantCellSize) * quadrantCellSize);
    //     Debug.DrawLine(lowerLeft, lowerLeft + new Vector3(+1, +0) * quadrantCellSize);
    //     Debug.DrawLine(lowerLeft, lowerLeft + new Vector3(+0, +1) * quadrantCellSize);
    //     Debug.DrawLine(lowerLeft + new Vector3(+1, +0) * quadrantCellSize, lowerLeft + new Vector3(+1, +1) * quadrantCellSize);
    //     Debug.DrawLine(lowerLeft + new Vector3(+0, +1) * quadrantCellSize, lowerLeft + new Vector3(+1, +1) * quadrantCellSize);
    //     Debug.Log(GetPositionHashMapKey(position) + " " + position);
    // }

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
    
    // [BurstCompile]
    // private struct SetQuadrantDataHashMapJob : IJobForEachWithEntity<Translation, QuadrantEntity> {
    //     public NativeMultiHashMap<int, QuadrantData>.ParallelWriter quadrantMultiHashMap;
    //     public void Execute(Entity entity, int index, ref Translation trans, ref QuadrantEntity quadrantEntity) {
    //         int hashMapKey = GetPositionHashMapKey(trans.Value);
    //         quadrantMultiHashMap.Add(hashMapKey, new QuadrantData {
    //             entity = entity,
    //             position = trans.Value,
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
        EntityQuery entityQuery = GetEntityQuery(typeof(IsActor));
        // NativeMultiHashMap<int, QuadrantData> quadrantMultiHashMap = new NativeMultiHashMap<int, QuadrantData>(entityQuery.CalculateEntityCount(), Allocator.TempJob);

        // Set position on the IsActorComponentData struct for use on IJob
        // Entities.WithAll<IsActor, Translation, Transform>().ForEach((IsActor actor, Transform tran) => {
        //     Translation actorComponentData = tran.gameObject.GetComponent<Translation>();
        //     actorComponentData.Value = tran.position;
        //     Debug.Log(tran.gameObject.name + ": " + actorComponentData);
        // });

        Debug.Log("Running...");


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
                // quadrantEntity = quadrantEntity
            });
            // Debug.Log(actor.gameObject.name);
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
            DebugDrawQuadrant(playerGameObject.transform.position);
            Debug.Log("Actors in " + playerGameObject.name + "'s current quadrant: \n" + GetEnityCountInHashMap(quadrantMultiHashMap, GetPositionHashMapKey(playerGameObject.transform.position)));

            // quadrantMultiHashMap.Dispose();
        }
    }

}
