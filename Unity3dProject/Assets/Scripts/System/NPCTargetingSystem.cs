using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Hybrid.Components;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

namespace Hybrid.Systems
{
    public class NPCTargetingSystem : ComponentSystem
    {
        private float updateTime = 1.55f;

        private float timer;

        void Start()
        {
            timer = updateTime;
        }

        protected override void OnUpdate()
        {
            timer -= Time.DeltaTime;

            if (timer < 0)
            {
                timer = updateTime;

                Entities
                    .WithAll
                    <DetectionStateData,
                        IsActor,
                        Targeting,
                        ActorFOV,
                        CombatStateData
                    >()
                    .ForEach((
                        Entity myEntity,
                        IsActor myActor,
                        DetectionStateData detectionStateData,
                        Targeting myTargeting,
                        ActorFOV myFOV,
                        CombatStateData myCombatStateData,
                        ActorHealth actorHealth
                    ) =>
                    {
                        // Check if actor is Dead
                        if (actorHealth.isDead())
                        {
                            return;
                        }

                        if (myActor.gameObject.tag != "Player")
                        {
                            string myRefId =
                                UtilityHelpers.getActorEntityRefId(myEntity);

                            // string myRefId = myActor.refId;
                            int allies = 0;
                            int enemies = 0;

                            if (myTargeting.trackedTargets == null)
                            {
                                myTargeting.trackedTargets =
                                    new HashSet<string>();
                                myTargeting.trackedTargetStats =
                                    new Hashtable();
                            }
                            else
                            {
                                myTargeting.trackedTargetCount =
                                    myTargeting.trackedTargets.Count;
                            }

                            Debug
                                .Log(myActor.gameObject.name +
                                " check trackedTargets: " +
                                myTargeting.trackedTargets.Count);

                            if (myTargeting.trackedTargetCount > 0)
                            {
                                // Debug.Log(myTargeting + "current targets: [" + myTargeting.trackedTargetCount + "]");
                                Transform priorityTarget = null;
                                float priorityLevel = 0;
                                float priorityDistance = -1;
                                int priorityDistanceUpdates = 0;

                                string priorityTargetRefId = null;
                                float priorityTargetLostTimer = 0f;

                                ActorFactions actorFactions =
                                    myActor
                                        .gameObject
                                        .GetComponent<ActorFactions>();
                                if (actorFactions != null)
                                {
                                    Debug
                                        .Log(myActor.gameObject.name +
                                        "- Factions: " +
                                        actorFactions.factions.Length);
                                }

                                Entities
                                    .WithAll
                                    <DetectionStateData,
                                        IsActor,
                                        Targeting,
                                        ActorFOV,
                                        CombatStateData
                                    >()
                                    .ForEach((
                                        Entity targetEntity,
                                        IsActor targetActor,
                                        DetectionStateData
                                        tarDetectionStateData,
                                        Targeting tarTargeting,
                                        ActorFOV tarFOV,
                                        CombatStateData tarCombatStateData,
                                        ActorHealth tarHealth
                                    ) =>
                                    {
                                        // string targetRefId = targetActor.refId;
                                        string targetRefId =
                                            UtilityHelpers
                                                .getActorEntityRefId(targetEntity);

                                        if (
                                            myTargeting
                                                .trackedTargets
                                                .Contains(targetRefId)
                                        )
                                        {
                                            bool targetLost = false;

                                            if (
                                                tarHealth.deathState >=
                                                DeathState.dying
                                            )
                                            {
                                                targetLost = true;
                                                myTargeting.trackedTargets.Remove (
                                                    targetRefId
                                                );
                                                myTargeting.trackedTargetStats.Remove (
                                                    targetRefId
                                                );
                                                Debug
                                                    .Log("Target RefId: " +
                                                    targetRefId +
                                                    " is dead, removing from targeting");
                                            }
                                            TrackedTarget trackedTarget;

                                            // Check Faction Relations
                                            bool isHostile = false;

                                            ActorFactions targetFactions =
                                                targetActor
                                                    .gameObject
                                                    .GetComponent
                                                    <ActorFactions>();
                                            if (
                                                actorFactions != null &&
                                                targetFactions != null &&
                                                actorFactions.factions.Length >
                                                0 &&
                                                targetFactions.factions.Length >
                                                0
                                            )
                                            {
                                                if (
                                                    myTargeting
                                                        .trackedTargetStats
                                                        .ContainsKey(targetRefId)
                                                )
                                                {
                                                    trackedTarget =
                                                        (TrackedTarget)
                                                        myTargeting
                                                            .trackedTargetStats[targetRefId];
                                                    if (
                                                        trackedTarget
                                                            .relationship ==
                                                        FactionRelationship
                                                            .unset
                                                    )
                                                    {
                                                        trackedTarget
                                                            .relationship =
                                                            Faction
                                                                .GetMultiFactionRelationship(actorFactions
                                                                    .factions,
                                                                targetFactions
                                                                    .factions);
                                                        myTargeting
                                                            .trackedTargetStats[targetRefId] =
                                                            trackedTarget;

                                                        Debug
                                                            .Log("Faction Relationship: " +
                                                            trackedTarget
                                                                .relationship +
                                                            " / myRefID: " +
                                                            myRefId);
                                                    }
                                                    if (
                                                        trackedTarget
                                                            .relationship ==
                                                        FactionRelationship.ally
                                                    )
                                                    {
                                                        allies++;
                                                    }
                                                    else if (
                                                        trackedTarget
                                                            .relationship ==
                                                        FactionRelationship
                                                            .enemy
                                                    )
                                                    {
                                                        enemies++;
                                                    }
                                                    isHostile =
                                                        trackedTarget
                                                            .relationship ==
                                                        FactionRelationship
                                                            .enemy;
                                                }
                                            }
                                            if (isHostile)
                                            {
                                                Debug.Log("Enemy found");
                                            }

                                            bool hasTargetLOS =
                                                UtilityHelpers
                                                    .IsTargetDetectable(myFOV
                                                        .viewPoint,
                                                    targetActor
                                                        .transform
                                                        .position,
                                                    myFOV.maxAngle,
                                                    myFOV.maxRadius);

                                            float distance =
                                                Vector3
                                                    .Distance(myActor
                                                        .transform
                                                        .position,
                                                    targetActor
                                                        .transform
                                                        .position);

                                            // TrackedTarget trackedTarget;
                                            if (
                                                isHostile &&
                                                !targetLost &&
                                                myTargeting
                                                    .trackedTargetStats
                                                    .ContainsKey(targetRefId)
                                            )
                                            {
                                                trackedTarget =
                                                    (TrackedTarget)
                                                    myTargeting
                                                        .trackedTargetStats[targetRefId];

                                                if (
                                                    hasTargetLOS &&
                                                    distance < myFOV.maxRadius
                                                )
                                                {
                                                    trackedTarget
                                                        .targetLostTimer = 23f;
                                                    myTargeting
                                                        .trackedTargetStats[targetRefId] =
                                                        trackedTarget;

                                                    priorityTargetLostTimer =
                                                        trackedTarget
                                                            .targetLostTimer;
                                                }
                                                else
                                                {
                                                    trackedTarget
                                                        .targetLostTimer -=
                                                        updateTime * 0.9f;

                                                    if (
                                                        trackedTarget
                                                            .targetLostTimer <
                                                        0
                                                    )
                                                    {
                                                        targetLost = true;
                                                        myTargeting.trackedTargets.Remove (
                                                            targetRefId
                                                        );
                                                        myTargeting.trackedTargetStats.Remove (
                                                            targetRefId
                                                        );
                                                        Debug
                                                            .Log("Target lost: " +
                                                            targetRefId);
                                                    }
                                                    else
                                                    {
                                                        myTargeting
                                                            .trackedTargetStats[targetRefId] =
                                                            trackedTarget;

                                                        priorityTargetLostTimer =
                                                            trackedTarget
                                                                .targetLostTimer;
                                                        Debug
                                                            .Log("Losing current target: " +
                                                            targetRefId);
                                                    }
                                                }
                                            }

                                            if (isHostile && !targetLost)
                                            {
                                                // int priority = -(int)Math.Floor(distance + FindTargetQuadrantSystem.getFOVAngle(myActor.transform, targetActor.transform.position, myFOV.maxAngle, myFOV.maxRadius) );
                                                AnimationState targetCombatState =
                                                    targetActor
                                                        .gameObject
                                                        .GetComponent
                                                        <AnimationState>();

                                                bool targetHasLOS =
                                                    UtilityHelpers
                                                        .IsTargetDetectable(tarFOV
                                                            .viewPoint,
                                                        myActor
                                                            .transform
                                                            .position,
                                                        tarFOV.maxAngle,
                                                        tarFOV.maxRadius);

                                                bool prioritize = false;
                                                bool distanceUpdated = false;
                                                float newPriority = 2f;
                                                if (
                                                    priorityDistance == -1 ||
                                                    distance < priorityDistance
                                                )
                                                {
                                                    distanceUpdated = true;
                                                    priorityDistance = distance;
                                                    priorityDistanceUpdates++;
                                                    newPriority +=
                                                        (
                                                        6 +
                                                        (
                                                        priorityDistanceUpdates *
                                                        2
                                                        )
                                                        );
                                                }

                                                if (hasTargetLOS)
                                                    newPriority += 6f;

                                                // Is targetting me
                                                if (targetHasLOS)
                                                    newPriority += 6f;

                                                if (
                                                    tarTargeting != null &&
                                                    tarTargeting
                                                        .currentTargetRefId ==
                                                    myRefId
                                                )
                                                {
                                                    newPriority += 6;

                                                    if (
                                                        distanceUpdated &&
                                                        (
                                                        hasTargetLOS ||
                                                        distance < 1.5f
                                                        )
                                                    )
                                                    {
                                                        newPriority += 12;
                                                        if (
                                                            targetCombatState
                                                                .attackAnimationState <
                                                            AttackAnimationState
                                                                .attackHitFinish
                                                        )
                                                        {
                                                            newPriority +=
                                                                6 *
                                                                (
                                                                1 +
                                                                (int)
                                                                targetCombatState
                                                                    .attackAnimationState
                                                                );
                                                        }
                                                    }
                                                }

                                                // Debug.Log(myActor.gameObject.name + ": Entity found in targets! " + targetActor.name + "\n Priority: " + priority + " | Distance: " + distance);
                                                // Debug.Log("Target Priority: " + priority);
                                                if (
                                                    prioritize ||
                                                    priorityLevel <= 0 ||
                                                    priorityLevel < newPriority
                                                )
                                                {
                                                    priorityLevel = newPriority;
                                                    priorityTarget =
                                                        targetActor
                                                            .gameObject
                                                            .transform;

                                                    // TEMP
                                                    priorityTargetRefId =
                                                        targetRefId;
                                                    TrackedTarget priorityTrackedTarget =
                                                        (TrackedTarget)
                                                        myTargeting
                                                            .trackedTargetStats[targetRefId];
                                                    priorityTargetLostTimer =
                                                        priorityTrackedTarget
                                                            .targetLostTimer;
                                                }
                                            }
                                        }
                                    });

                                if (
                                    priorityTarget != null &&
                                    priorityTarget != myTargeting.currentTarget
                                )
                                {
                                    myTargeting.currentTarget = priorityTarget;

                                    myTargeting.currentTargetRefId =
                                        priorityTargetRefId;
                                    myFOV.currentTarget =
                                        priorityTarget.gameObject;

                                    ActorEventManger actorEventManger =
                                        myActor
                                            .GetComponent<ActorEventManger>();
                                    if (actorEventManger != null)
                                        actorEventManger
                                            .CombatTargetUpdate(priorityTarget
                                                .gameObject);

                                    Debug
                                        .Log(myActor.gameObject.name +
                                        " - New Priority Target! " +
                                        myTargeting.currentTarget.name);
                                }
                                else
                                {
                                    myTargeting.currentTargetLostTimer =
                                        priorityTargetLostTimer;
                                }
                            }

                            if (myTargeting.currentTarget != null)
                            {
                                ActorHealth targetHealth =
                                    myTargeting
                                        .currentTarget
                                        .gameObject
                                        .GetComponent<ActorHealth>();
                                ActorEventManger actorEventManger =
                                    myActor.GetComponent<ActorEventManger>();

                                if (targetHealth.isDead())
                                {
                                    myTargeting.currentTarget = null;
                                    myTargeting.currentTargetRefId = null;
                                    myTargeting.allycount = allies;
                                    myTargeting.enemycount = enemies;
                                    myFOV.currentTarget = null;

                                    if (actorEventManger != null)
                                        actorEventManger
                                            .CombatTargetUpdate(null);
                                }
                                else
                                {
                                    if (!myCombatStateData.IsInCombat())
                                    {
                                        myCombatStateData.combatState =
                                            CombatState.active;

                                        if (actorEventManger)
                                            actorEventManger
                                                .CombatStateChange(myCombatStateData
                                                    .combatState);
                                    }
                                }
                            }
                            else
                            {
                                myCombatStateData.EvaluateCombatState(true);
                            }
                        }
                    });
            }
        }
    }
}
