using System;
using Hybrid.Components;
using UnityEngine;
using UnityEngine.AI;
using Unity.Entities;

namespace Hybrid.Systems
{
    public class NPCFollowerSystem : ComponentSystem
    {
        private float updateTime = 0.3f;

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
                    .WithAll<ActorAIStateData, Follower, Targeting, IsActor>()
                    .ForEach((
                        Entity entity,
                        IsActor actor,
                        Follower follower,
                        Targeting targeting,
                        ActorHealth actorHealth
                    ) =>
                    {
                        // Check if actor is Dead
                        if (
                            !follower.HasTarget() ||
                            actorHealth.deathState >= DeathState.dying ||
                            actor.gameObject.tag == "Player"
                        )
                        {
                            return;
                        }

                        GameObject target = follower.GetTarget().gameObject;
                        EvaluateFollowGoal (follower, target);

                        ActorNavigationData actorNavigationData =
                            follower.GetComponent<ActorNavigationData>();
                        if (
                            actorNavigationData != null &&
                            actorNavigationData.travelPosition !=
                            target.transform
                        )
                        {
                            actorNavigationData.travelPosition =
                                target.transform;
                        }

                        //Match or exceed the speed or the followed target
                        if (target != null)
                        {
                            CombatStateData combatStateData =
                                actor
                                    .gameObject
                                    .GetComponent<CombatStateData>();

                            if (combatStateData.IsInCombat()) return;

                            NavAgentController targetNavAgentController =
                                target.GetComponent<NavAgentController>();
                            NavAgentController myNavAgentController =
                                actor.GetComponent<NavAgentController>();
                            if (
                                targetNavAgentController != null &&
                                myNavAgentController != null
                            )
                            {
                                if (
                                    follower.followBehavior ==
                                    FollowBehavior.take_point
                                )
                                {
                                    if (
                                        targetNavAgentController
                                            .groundNavigationSpeed >=
                                        ActorGroundNavigationSpeed.run
                                    )
                                    {
                                        myNavAgentController
                                            .groundNavigationSpeed =
                                            ActorGroundNavigationSpeed.sprint;
                                    }
                                    else
                                    {
                                        myNavAgentController
                                            .groundNavigationSpeed =
                                            ActorGroundNavigationSpeed.run;
                                    }
                                }
                                else
                                {
                                    ActorGroundNavigationSpeed navigationSpeed =
                                        targetNavAgentController
                                            .groundNavigationSpeed;

                                    if (
                                        navigationSpeed <
                                        targetNavAgentController
                                            .groundNavigationSpeed
                                    )
                                    {
                                        navigationSpeed =
                                            targetNavAgentController
                                                .groundNavigationSpeed;
                                    }

                                    myNavAgentController.groundNavigationSpeed =
                                        navigationSpeed;
                                }
                                // }
                            }
                        }
                    });
            }
        }

        private void EvaluateFollowGoal(
            Follower follower,
            GameObject target,
            bool bForceUpdate = false
        )
        {
            AnimationState targetAnimationState =
                target.GetComponent<AnimationState>();
            bool bIsTargetMoving = targetAnimationState.isMoving;

            float targetDistanceGoal =
                Vector3.Distance(target.transform.position, follower.GetGoal());
            float targetDistanceMe =
                Vector3
                    .Distance(target.transform.position,
                    follower.gameObject.transform.position);
            bool bShouldUpdateGoal =
                (
                bForceUpdate ||
                follower.GetGoal() == Vector3.zero ||
                targetDistanceGoal > follower.GetFollowBehaviorDistanceMax()
                );

            Vector3 newGoal = follower.GetGoal();

            follower.targetDistance = targetDistanceMe;
            float variableDistMax;

            switch (follower.followBehavior)
            {
                case FollowBehavior.relaxed:
                    variableDistMax = follower.GetFollowBehaviorDistanceMax();
                    if (
                        bShouldUpdateGoal ||
                        (
                        targetDistanceMe > variableDistMax &&
                        targetDistanceGoal > variableDistMax
                        )
                    )
                    {
                        newGoal =
                            UtilityHelpers
                                .GetRandomNavmeshPoint(variableDistMax - 0.5f,
                                target.transform.position);
                    }
                    break;
                case FollowBehavior.take_point:
                    float minAheadDist =
                        (follower.takePointDistanceMult * follower.distanceMax);
                    float maxTargetPositionRadius =
                        follower.distanceMax * 0.75f;
                    follower.minAheadDist = minAheadDist;
                    follower.maxTargetPositionRadius = maxTargetPositionRadius;
                    Vector3 targetPositionAhead =
                        UtilityHelpers
                            .GetFrontPosition(target.transform,
                            maxTargetPositionRadius);
                    float goalFrontDistance =
                        Vector3
                            .Distance(targetPositionAhead, follower.GetGoal());

                    if (
                        bShouldUpdateGoal ||
                        (
                        targetDistanceMe < minAheadDist &&
                        targetDistanceGoal < minAheadDist
                        ) ||
                        (
                        bIsTargetMoving &&
                        goalFrontDistance > minAheadDist * 1.3f
                        )
                    )
                    {
                        newGoal =
                            UtilityHelpers
                                .GetRandomNavmeshPoint(follower.distanceMax *
                                0.3f,
                                UtilityHelpers
                                    .GetFrontPosition(target.transform,
                                    follower.distanceMax * 0.7f));
                    }
                    break;
                case FollowBehavior.tail:
                    variableDistMax = follower.GetFollowBehaviorDistanceMax();
                    if (
                        bShouldUpdateGoal ||
                        (
                        targetDistanceMe > variableDistMax &&
                        targetDistanceGoal > variableDistMax
                        )
                    )
                    {
                        newGoal =
                            UtilityHelpers
                                .GetRandomNavmeshPoint(variableDistMax * 0.5f,
                                UtilityHelpers
                                    .GetBehindPosition(target.transform,
                                    variableDistMax - 0.5f));
                    }
                    break;
                case FollowBehavior.go_to:
                    break;
                default:
                    // if (bShouldUpdateGoal)
                    // {
                    newGoal = target.transform.position; // UtilityHelpers.GetRandomNavmeshPoint(follower.distanceMax, target.transform.position);

                    // }
                    break;
            }
            follower.SetGoal (newGoal);
        }
    }
}
