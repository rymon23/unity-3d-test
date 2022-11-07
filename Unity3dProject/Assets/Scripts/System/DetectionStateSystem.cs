using Hybrid.Components;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

namespace Hybrid.Systems
{
    public class DetectionStateSystem : ComponentSystem
    {
        private float updateTime = 0.25f;

        private float timer;

        private void Start()
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
                        Entity entity,
                        IsActor actor,
                        DetectionStateData detectionStateData,
                        Targeting targeting,
                        ActorFOV myFOV,
                        CombatStateData combatStateData,
                        ActorHealth actorHealth
                    ) =>
                    {
                        // Check if actor is Dead
                        if (
                            actorHealth.isDead() ||
                            actor.gameObject.tag == "Player"
                        )
                        {
                            return;
                        }

                        Transform currentTarget = targeting.currentTarget;
                        if (currentTarget == null)
                        {
                            detectionStateData.targetTrackingState = 0;
                            return;
                        }

                        ActorEventManger myEventManger =
                            actor.gameObject.GetComponent<ActorEventManger>();

                        // Debug.Log(actor.name + " distance from : " + currentTarget.name + ": " + distance);
                        // float distance = Vector3.Distance(actor.transform.position, currentTarget.position);
                        if (
                            UtilityHelpers
                                .IsTargetDetectable(myFOV.viewPoint,
                                currentTarget.transform.position,
                                myFOV.maxAngle,
                                myFOV.maxRadius)
                        )
                        {
                            detectionStateData
                                .ResetTimer_TargetRegainVisibility();

                            if (myEventManger != null)
                            {
                                if (
                                    detectionStateData.targetTrackingState !=
                                    TargetTrackingState.active
                                )
                                {
                                    myEventManger
                                        .TargetTrackingStateChange(TargetTrackingState
                                            .active,
                                        currentTarget.gameObject);
                                }
                            }

                            // if (combatStateData.combatState == CombatState.alerted)
                            // {
                            //     Debug.Log(actor.name + " was alerted and detected target!");
                            // }
                            detectionStateData.targetTrackingState =
                                TargetTrackingState.active;
                            // combatStateData.combatState = CombatState.active;
                        }
                        else
                        {
                            switch (detectionStateData.targetTrackingState)
                            {
                                case TargetTrackingState.active:
                                    if (
                                        detectionStateData
                                            .targetRegainVisibilityTimer <
                                        0
                                    )
                                    {
                                        detectionStateData
                                            .ResetTimer_TargetSearch();
                                        detectionStateData.targetTrackingState =
                                            TargetTrackingState.searching;
                                        combatStateData.combatState =
                                            CombatState.searching;

                                        if (myEventManger)
                                            myEventManger
                                                .CombatStateChange(combatStateData
                                                    .combatState);

                                        targeting.searchCenterPos =
                                            currentTarget.transform.position;
                                    }
                                    else
                                    {
                                        detectionStateData
                                            .UpdateTimer_TargetRegainVisibility(
                                            );
                                    }
                                    break;
                                case TargetTrackingState.searching:
                                    if (detectionStateData.targetSearchTimer < 0
                                    )
                                    {
                                        detectionStateData.targetTrackingState =
                                            TargetTrackingState.lost;
                                    }
                                    else
                                    {
                                        detectionStateData
                                            .UpdateTimer_TargetSearch();
                                    }
                                    break;
                                default:
                                    detectionStateData.targetTrackingState =
                                        TargetTrackingState.inactive;
                                    break;
                            }
                        }
                    });
            }
        }
    }
}
