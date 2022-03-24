
using UnityEngine;
using Unity.Entities;
using Hybrid.Components;

namespace Hybrid.Systems
{
    public class NPCNavigationSystem : ComponentSystem
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
                Entities.WithAll<ActorAIStateData, Targeting, IsActor>()
                    .ForEach((Entity entity, IsActor actor, ActorHealth actorHealth, Targeting targeting) =>
                    {

                        // Check if actor is Dead
                        if (actorHealth.deathState >= DeathState.dying)
                        {
                            return;
                        }
                    });
            }
        }
    }

}