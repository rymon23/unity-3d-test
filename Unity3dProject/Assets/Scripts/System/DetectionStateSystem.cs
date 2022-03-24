using UnityEngine;
using Unity.Entities;
using Hybrid.Components;
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

                Entities.WithAll<DetectionStateData, IsActor, Targeting, ActorFOV, CombatStateData>()
                    .ForEach((Entity entity, IsActor actor, DetectionStateData detectionStateData, Targeting targeting, ActorFOV myFOV, CombatStateData combatStateData, ActorHealth actorHealth) =>
                    {

                        // Check if actor is Dead
                        if (actorHealth?.deathState >= DeathState.dying)
                        {
                            return;
                        }

                        if (actor.gameObject.tag != "Player")
                        {
                            Transform currentTarget = targeting.currentTarget;

                            if (currentTarget != null)
                            {


                                // Debug.Log(actor.name + " distance from : " + currentTarget.name + ": " + distance);

                                // float distance = Vector3.Distance(actor.transform.position, currentTarget.position);

                                if (UtilityHelpers.IsTargetDetectable(actor.transform, currentTarget.position, myFOV.maxAngle, myFOV.maxRadius))
                                {
                                    detectionStateData.ResetTimer_TargetRegainVisibility();
                                    detectionStateData.targetTrackingState = TargetTrackingState.active;
                                    combatStateData.combatState = CombatState.active;
                                }
                                else
                                {
                                    switch (detectionStateData.targetTrackingState)
                                    {
                                        case TargetTrackingState.active:
                                            if (detectionStateData.targetRegainVisibilityTimer < 0)
                                            {
                                                detectionStateData.ResetTimer_TargetSearch();
                                                detectionStateData.targetTrackingState = TargetTrackingState.searching;
                                                combatStateData.combatState = CombatState.searching;
                                            }
                                            else
                                            {
                                                detectionStateData.UpdateTimer_TargetRegainVisibility();
                                            }
                                            break;
                                        case TargetTrackingState.searching:
                                            if (detectionStateData.targetSearchTimer < 0)
                                            {
                                                detectionStateData.targetTrackingState = TargetTrackingState.lost;
                                            }
                                            else
                                            {
                                                detectionStateData.UpdateTimer_TargetSearch();
                                            }
                                            break;

                                        case TargetTrackingState.lost:
                                            detectionStateData.targetTrackingState = TargetTrackingState.inactive;
                                            break;

                                        default:
                                            break;
                                    }
                                }
                            }
                            else
                            {
                                detectionStateData.targetTrackingState = 0;
                                combatStateData.combatState = 0;
                            }
                        }

                    });


            }
        }
    }
}