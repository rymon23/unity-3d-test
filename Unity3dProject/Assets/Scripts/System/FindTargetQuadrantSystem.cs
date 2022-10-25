using System.Collections;
using System.Collections.Generic;
using Hybrid.Components;
using Unity.Collections;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

namespace Hybrid.Systems
{
    public class FindTargetQuadrantSystem : ComponentSystem
    {
        [ReadOnly]
        public NativeMultiHashMap<int, QuadrantData> quadrantMultiHashMap;

        protected override void OnCreate()
        {
            quadrantMultiHashMap = QuadrantSystem.quadrantMultiHashMap;
            base.OnCreate();
        }

        protected override void OnDestroy()
        {
            quadrantMultiHashMap.Dispose();
            base.OnDestroy();
        }

        private float territoryUpdateTimerMax = 1.55f;

        private float territoryUpdateTimer;

        void Start()
        {
            territoryUpdateTimer = territoryUpdateTimerMax;
        }

        protected override void OnUpdate()
        {
            // Debug.Log("FindTargetQuadrantSystem: running");
            int entityCount = 0;
            int targetCount = 0;


#region Test Territory

            territoryUpdateTimer -= Time.DeltaTime;

            if (territoryUpdateTimer < 0)
            {
                territoryUpdateTimer = territoryUpdateTimerMax;

                Entities
                    .WithAll<Territory>()
                    .ForEach((
                        Entity entity,
                        Transform myTransform,
                        Territory territory
                    ) =>
                    {
                        // Debug.Log("Territory found: ");
                        float3 unitPosition = myTransform.position;

                        HashSet<string> localEntitiesByInstanceID =
                            new HashSet<string>();
                        int localActorsFound = 0;

                        Vector3[] myQuadrantPositions =
                            QuadrantSystem
                                .GetNeighoborQuadrantPositions(myTransform
                                    .position);

                        for (int i = 0; i < myQuadrantPositions.Length; i++)
                        {
                            int hashMapKey =
                                QuadrantSystem
                                    .GetPositionHashMapKey(myQuadrantPositions[i]);

                            QuadrantData quadrantData;
                            NativeMultiHashMapIterator<int
                            > nativeMultiHashMapIterator;

                            if (
                                quadrantMultiHashMap
                                    .TryGetFirstValue(hashMapKey,
                                    out quadrantData,
                                    out nativeMultiHashMapIterator)
                            )
                            {
                                do
                                {
                                    // Make sure its not this entity
                                    if (
                                        !quadrantData.entity.Equals(entity) &&
                                        !quadrantData.isDead
                                    )
                                    {
                                        float distance =
                                            Vector3
                                                .Distance(unitPosition,
                                                quadrantData.position);

                                        if (distance < territory.GetRadius())
                                        {
                                            localActorsFound += 1;

                                            localEntitiesByInstanceID
                                                .Add(UtilityHelpers
                                                    .getActorEntityRefId(quadrantData
                                                        .entity));
                                        }
                                    }
                                }
                                while (quadrantMultiHashMap
                                        .TryGetNextValue(out quadrantData,
                                        ref nativeMultiHashMapIterator)
                                );
                            }
                        }

                        Debug
                            .Log("Territory - " +
                            myTransform.gameObject.name +
                            " - localActorsFound: " +
                            localActorsFound);

                        if (localEntitiesByInstanceID.Count > 0)
                        {
                            Dictionary<Faction, int> newLocalStrength =
                                new Dictionary<Faction, int>();

                            Entities
                                .WithAll<IsActor, Targeting, ActorFactions>()
                                .ForEach((
                                    Entity targetEntity,
                                    Transform targetTransform,
                                    Targeting targeting,
                                    ActorFactions actorFactions
                                ) =>
                                {
                                    string targetRefId =
                                        UtilityHelpers
                                            .getActorEntityRefId(targetEntity);

                                    if (
                                        localEntitiesByInstanceID
                                            .Contains(targetRefId) &&
                                        actorFactions.factions.Length > 0
                                    )
                                    {
                                        Faction firstFaction =
                                            actorFactions.factions[0];

                                        if (
                                            !newLocalStrength
                                                .ContainsKey(firstFaction)
                                        )
                                        {
                                            newLocalStrength
                                                .Add(firstFaction, 1);
                                        }
                                        else
                                        {
                                            newLocalStrength[firstFaction] += 1;
                                        }
                                    }
                                });

                            territory.UpdateLocalStrength (newLocalStrength);
                        }
                    });
            }
#endregion


            //
            Entities
                .WithAll<IsActor, Targeting, ActorFOV, ActorFactions>()
                .ForEach((
                    Entity entity,
                    Transform myTransform,
                    Targeting myTargeting,
                    ActorFOV myFOV,
                    ActorFactions actorFactions,
                    ActorHealth actorHealth
                ) =>
                {
                    entityCount += 1;

                    // Check if actor is Dead
                    if (actorHealth.deathState >= DeathState.dying)
                    {
                        return;
                    }

                    Debug.Log("Actor: " + myTransform.gameObject.name);

                    float3 unitPosition = myTransform.position;
                    Entity closestTargetEntity = Entity.Null;
                    float closestTargetDistance = float.MaxValue;

                    Vector3[] myQuadrantPositions =
                        QuadrantSystem
                            .GetNeighoborQuadrantPositions(myTransform
                                .position);

                    for (int i = 0; i < myQuadrantPositions.Length; i++)
                    {
                        int hashMapKey =
                            QuadrantSystem
                                .GetPositionHashMapKey(myQuadrantPositions[i]);

                        QuadrantData quadrantData;
                        NativeMultiHashMapIterator<int
                        > nativeMultiHashMapIterator;

                        if (
                            quadrantMultiHashMap
                                .TryGetFirstValue(hashMapKey,
                                out quadrantData,
                                out nativeMultiHashMapIterator)
                        )
                        {
                            do
                            {
                                // Debug.Log(quadrantData.entity);
                                // Make sure its not this entity
                                if (
                                    !quadrantData.entity.Equals(entity) &&
                                    !quadrantData.isDead &&
                                    UtilityHelpers
                                        .IsTargetDetectable(myFOV.viewPoint,
                                        quadrantData.position,
                                        myFOV.maxAngle,
                                        myFOV.maxRadius)
                                )
                                {
                                    targetCount += 1;

                                    if (myTargeting.trackedTargets != null)
                                    {
                                        string targetRefId =
                                            UtilityHelpers
                                                .getActorEntityRefId(quadrantData
                                                    .entity);

                                        // int targetRefId =
                                        //     quadrantData.actorInstanceID;
                                        myTargeting.trackedTargets.Add (
                                            targetRefId
                                        );
                                        if (
                                            !myTargeting
                                                .trackedTargetStats
                                                .ContainsKey(targetRefId)
                                        )
                                        {
                                            myTargeting
                                                .trackedTargetStats
                                                .Add(targetRefId,
                                                new TrackedTarget(targetRefId));
                                        }
                                    }

                                    if (closestTargetEntity == Entity.Null)
                                    {
                                        // No Target
                                        closestTargetEntity =
                                            quadrantData.entity;
                                        closestTargetDistance =
                                            math
                                                .distancesq(unitPosition,
                                                quadrantData.position);
                                    }
                                    else if (
                                        math
                                            .distancesq(unitPosition,
                                            quadrantData.position) <
                                        closestTargetDistance
                                    )
                                    {
                                        // This target is closer
                                        closestTargetEntity =
                                            quadrantData.entity;
                                        closestTargetDistance =
                                            math
                                                .distancesq(unitPosition,
                                                quadrantData.position);
                                    }
                                }
                            }
                            while (quadrantMultiHashMap
                                    .TryGetNextValue(out quadrantData,
                                    ref nativeMultiHashMapIterator)
                            );
                        }
                    }

                    // Debug.Log(closestTargetEntity);
                    if (closestTargetEntity != Entity.Null)
                    {
                        Entities
                            .WithAll
                            <IsActor, Targeting, ActorFOV, ActorFactions>()
                            .ForEach((
                                Entity targetEntity,
                                Transform targetTransform,
                                CombatStateData combatStateData,
                                Targeting targeting
                            ) =>
                            {
                                if (combatStateData.IsAlerted())
                                {
                                    Debug
                                        .Log("alerted entity: " +
                                        targetTransform.gameObject.name);
                                    if (myTargeting.trackedTargets != null)
                                    {
                                        Debug
                                            .Log("alerted entity: " +
                                            targetTransform.gameObject.name +
                                            " - trackedTargetStats: " +
                                            myTargeting
                                                .trackedTargetStats
                                                .Count);
                                        Debug
                                            .Log("alerted entity: " +
                                            targetTransform.gameObject.name +
                                            " - trackedTargets: " +
                                            myTargeting.trackedTargets.Count);
                                    }
                                }

                                if (targetEntity.Equals(closestTargetEntity))
                                {
                                    // myFOV.currentTarget = targetTransform.gameObject;
                                    //Add target to trackedTargets dictionary
                                    // if (!myTargeting.trackedTargets.ContainsKey(targetTransform.gameObject)){
                                    //     myTargeting.trackedTargets.Add(targetTransform.gameObject, new TargetDetectionData {
                                    //         targetTrackingState = 0,
                                    //         lastKnownPosition = targetTransform.position,
                                    //         targetDetectedTimer = 2f,
                                    //     });
                                    //     foreach (KeyValuePair<GameObject, TargetDetectionData> entry in myTargeting.trackedTargets) {
                                    //         TargetDetectionData v = entry.Value;
                                    //         Debug.Log(entry.Key.name + " : " + v.lastKnownPosition);
                                    //     }
                                    // }
                                    // if (myTargeting.currentTarget != targetTransform)
                                    // {
                                    //     myTargeting.lastTarget = myTargeting.currentTarget;
                                    //     myTargeting.currentTarget = targetTransform;
                                    //     myTargeting.targetDistance = closestTargetDistance;
                                    //     myTargeting.hasTargetInFOV = IsInFOVScope(myFOV.viewPoint, targetTransform.position, myFOV.maxAngle, myFOV.maxRadius);
                                    // }
                                    if (
                                        myTargeting.closestTarget !=
                                        targetTransform
                                    )
                                    {
                                        myTargeting.closestTarget =
                                            targetTransform;
                                        myTargeting.targetDistance =
                                            closestTargetDistance;
                                    }
                                    // Debug.Log(myTransform.gameObject.name + "'s closest target: [" + targetTransform.gameObject.name + "] | InFOV: " + IsInFOVScope(myFOV.viewPoint, targetTransform.position, myFOV.maxAngle, myFOV.maxRadius));
                                }
                            });
                    }

                    // targeting.currentTarget = closestTargetTransform;
                    // targeting.navPosition = closestTargetTransform.position;
                });

            Debug
                .Log("FindTargetQuadrantSystem: \n Total Targeters: " +
                entityCount +
                " | Total Targets: " +
                targetCount);
        }

        // public static bool IsInFOVScope(Transform viewer, Vector3 targetPos, float maxAngle, float maxRadius)
        // {
        //     Vector3 directionBetween = (targetPos - viewer.position).normalized;
        //     directionBetween.y *= 0;
        //     float angle = Vector3.Angle(viewer.forward, directionBetween);

        //     //TO DO: fix LOS
        //     // if (angle <= maxAngle)
        //     // {
        //     //     Ray ray = new Ray(viewer.position, targetPos - viewer.position);
        //     //     RaycastHit hit;

        //     //     if (Physics.Raycast(ray, out hit, maxRadius))  return hit.transform.position == targetPos;
        //     // }
        //     return (angle <= maxAngle);
        //     // return false;
        // }
    }
}
