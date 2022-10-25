using System.Collections;
using System.Collections.Generic;
using Hybrid.Components;
using UnityEngine;
using Unity.Entities;

namespace Hybrid.Systems
{
    public class NPCMovementSpeedSystem : ComponentSystem
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
                    .WithAll<ActorAIStateData, NavAgentController, IsActor>()
                    .ForEach((
                        Entity entity,
                        IsActor actor,
                        ActorHealth actorHealth,
                        CombatStateData combatStateData,
                        NavAgentController navAgentController
                    ) =>
                    {
                        // Check if actor is Dead
                        if (
                            actorHealth.deathState >= DeathState.dying ||
                            navAgentController.bDebugMode
                        )
                        {
                            return;
                        }

                        if (combatStateData.IsInCombat())
                        {
                            navAgentController.groundNavigationSpeed =
                                ActorGroundNavigationSpeed.run;
                        }
                        else
                        {
                            navAgentController.groundNavigationSpeed =
                                ActorGroundNavigationSpeed.jog;
                        }
                    });
            }
        }
    }
}
