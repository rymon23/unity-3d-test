using Hybrid.Components;
using UnityEngine;
using Unity.Entities;

namespace Hybrid.Systems
{
    public class PostDeathSystem : ComponentSystem
    {
        private float updateTime = 0.33f;

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

                // Destroy Actors
                Entities
                    .WithAll<ActorAIStateData, IsActor>()
                    .ForEach((
                        Entity entity,
                        IsActor actor,
                        ActorHealth actorHealth
                    ) =>
                    {
                        // Check if actor is Dead
                        if (actorHealth.deathState >= DeathState.dying)
                        {
                            GameObject gmo = actor.transform.root.gameObject;
                            SpawnController.current.entityManager.DestroyEntity (
                                entity
                            );
                            GameObject.Destroy(gmo, 4f);
                        }
                    });

                // Destroy TeamGroups
                Entities
                    .WithAll<TeamGroup>()
                    .ForEach((Entity entity, TeamGroup teamGroup) =>
                    {
                        if (teamGroup.GetStatus() == TeamGroup.Status.dead)
                        {
                            SpawnController.current.entityManager.DestroyEntity (
                                entity
                            );
                            GameObject.Destroy(teamGroup.gameObject, 6f);
                        }
                    });
            }
        }
    }
}
