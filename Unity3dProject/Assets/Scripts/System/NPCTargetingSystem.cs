
using System.Linq;
using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Hybrid.Components;
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

                Entities.WithAll<DetectionStateData, IsActor, Targeting, ActorFOV, CombatStateData>()
                    .ForEach((Entity myEntity, IsActor myActor, DetectionStateData detectionStateData, Targeting myTargeting, ActorFOV myFOV, CombatStateData myCombatStateData, ActorHealth actorHealth) =>
                    {

                        // Check if actor is Dead
                        if (actorHealth.deathState >= DeathState.dying)
                        {
                            return;
                        }

                        if (myActor.gameObject.tag != "Player")
                        {

                            if (myTargeting.trackedTargets == null)
                            {
                                myTargeting.trackedTargets = new HashSet<string>();
                                myTargeting.trackedTargetStats = new Hashtable();
                            }
                            else
                            {
                                myTargeting.trackedTargetCount = myTargeting.trackedTargets.Count;
                            }

                            if (myTargeting.trackedTargetCount > 0)
                            {
                                // Debug.Log(myTargeting + "current targets: [" + myTargeting.trackedTargetCount + "]");
                                Transform priorityTarget = null;
                                float priorityLevel = 0;
                                float priorityDistance = -1;

                                string priorityTargetRefId = null;
                                float priorityTargetLostTimer = 0f;

                                ActorFactions actorFactions = myActor.gameObject.GetComponent<ActorFactions>();
                                if (actorFactions != null)
                                {
                                    Debug.Log("Factions found: " + actorFactions.factions.Length);
                                }

                                Entities.WithAll<DetectionStateData, IsActor, Targeting, ActorFOV, CombatStateData>()
                                    .ForEach((Entity targetEntity, IsActor targetActor, DetectionStateData tarDetectionStateData, Targeting tarTargeting, ActorFOV tarFOV, CombatStateData tarCombatStateData, ActorHealth tarHealth) =>
                                    {
                                        string targetRefId = UtilityHelpers.getActorEntityRefId(targetEntity);

                                        if (myTargeting.trackedTargets.Contains(targetRefId))
                                        {
                                            bool targetLost = false;

                                            if (tarHealth.deathState >= DeathState.dying)
                                            {
                                                targetLost = true;
                                                myTargeting.trackedTargets.Remove(targetRefId);
                                                myTargeting.trackedTargetStats.Remove(targetRefId);
                                                Debug.Log("Target RefId: " + targetRefId + " is dead, removing from targeting");
                                            }
                                            TrackedTarget trackedTarget;


                                            // Check Faction Relations
                                            bool sharesFaction = false;
                                            ActorFactions targetFactions = targetActor.gameObject.GetComponent<ActorFactions>();
                                            if (actorFactions != null && targetFactions != null && actorFactions.factions.Length > 0 && targetFactions.factions.Length > 0)
                                            {
                                                if (myTargeting.trackedTargetStats.ContainsKey(targetRefId))
                                                {
                                                    trackedTarget = (TrackedTarget)myTargeting.trackedTargetStats[targetRefId];
                                                    if (trackedTarget.relationship == FactionRelationship.unset)
                                                    {
                                                        trackedTarget.relationship = Faction.GetMultiFactionRelationship(actorFactions.factions, targetFactions.factions);
                                                        myTargeting.trackedTargetStats[targetRefId] = trackedTarget;

                                                        Debug.Log("Faction Relationship: " + trackedTarget.relationship);
                                                    }
                                                    sharesFaction = (trackedTarget.relationship == FactionRelationship.ally);
                                                }

                                            }
                                            if (!sharesFaction)
                                            {
                                                Debug.Log("Enemy found");
                                            }

                                            bool hasTargetLOS = UtilityHelpers.IsTargetDetectable(myFOV.viewPoint, targetActor.transform.position, myFOV.maxAngle, myFOV.maxRadius);

                                            float distance = Vector3.Distance(myActor.transform.position, targetActor.transform.position);
                                            // TrackedTarget trackedTarget;
                                            if (!sharesFaction && !targetLost && myTargeting.trackedTargetStats.ContainsKey(targetRefId))
                                            {
                                                trackedTarget = (TrackedTarget)myTargeting.trackedTargetStats[targetRefId];

                                                if (hasTargetLOS && distance < myFOV.maxRadius)
                                                {
                                                    trackedTarget.targetLostTimer = 23f;
                                                    myTargeting.trackedTargetStats[targetRefId] = trackedTarget;

                                                    priorityTargetLostTimer = trackedTarget.targetLostTimer;
                                                }
                                                else
                                                {
                                                    trackedTarget.targetLostTimer -= updateTime * 0.9f;

                                                    if (trackedTarget.targetLostTimer < 0)
                                                    {
                                                        targetLost = true;
                                                        myTargeting.trackedTargets.Remove(targetRefId);
                                                        myTargeting.trackedTargetStats.Remove(targetRefId);
                                                        Debug.Log("Target lost: " + targetRefId);
                                                    }
                                                    else
                                                    {
                                                        myTargeting.trackedTargetStats[targetRefId] = trackedTarget;

                                                        priorityTargetLostTimer = trackedTarget.targetLostTimer;
                                                        Debug.Log("Losing current target: " + targetRefId);
                                                    }
                                                }
                                            }

                                            if (!sharesFaction && !targetLost)
                                            {
                                                // int priority = -(int)Math.Floor(distance + FindTargetQuadrantSystem.getFOVAngle(myActor.transform, targetActor.transform.position, myFOV.maxAngle, myFOV.maxRadius) );


                                                // float newPriority  = distance;// * -10f;
                                                float newPriority = 4f;
                                                if (hasTargetLOS) newPriority += 9f;

                                                if (priorityDistance == -1 || priorityDistance > distance) {
                                                    priorityDistance = distance;

                                                    newPriority += 4;
                                                }


                                                // Debug.Log(myActor.gameObject.name + ": Entity found in targets! " + targetActor.name + "\n Priority: " + priority + " | Distance: " + distance);
                                                // Debug.Log("Target Priority: " + priority);

                                                if (priorityLevel <= 0 || priorityLevel > newPriority)
                                                {
                                                    priorityLevel = newPriority;
                                                    priorityTarget = targetActor.gameObject.transform;

                                                    // TEMP
                                                    priorityTargetRefId = targetRefId;
                                                    TrackedTarget priorityTrackedTarget = (TrackedTarget)myTargeting.trackedTargetStats[targetRefId];
                                                    priorityTargetLostTimer = priorityTrackedTarget.targetLostTimer;
                                                }
                                            }


                                        }

                                    });


                                if (priorityTarget != null && priorityTarget != myTargeting.currentTarget)
                                {
                                    myTargeting.currentTarget = priorityTarget;

                                    myTargeting.currentTargetRefId = priorityTargetRefId;
                                    myFOV.currentTarget = priorityTarget.gameObject;

                                    ActorEventManger actorEventManger = myActor.GetComponent<ActorEventManger>();
                                    if (actorEventManger != null) actorEventManger.CombatTargetUpdate(priorityTarget.gameObject);

                                    Debug.Log("New Priority Target! " + myTargeting.currentTarget.name);
                                }
                                else
                                {
                                    myTargeting.currentTargetLostTimer = priorityTargetLostTimer;
                                }

                            }


                            if (myTargeting.currentTarget != null)
                            {
                                ActorHealth targetHealth = myTargeting.currentTarget.gameObject.GetComponent<ActorHealth>();
                                if (targetHealth.deathState >= DeathState.dying)
                                {
                                    myTargeting.currentTarget = null;
                                    myTargeting.currentTargetRefId = null;
                                    myFOV.currentTarget = null;

                                    ActorEventManger actorEventManger = myActor.GetComponent<ActorEventManger>();
                                    if (actorEventManger != null) actorEventManger.CombatTargetUpdate(null);
                                }
                            }

                        }

                    });


            }
        }
    }

}
