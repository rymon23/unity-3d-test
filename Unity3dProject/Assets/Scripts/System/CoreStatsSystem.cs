using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Hybrid.Components;


namespace Hybrid.Systems
{      
        public class CoreStatsSystem : ComponentSystem
    {

        protected override void OnUpdate() {
        
            Entities.WithAll<IsActor, ActorHealth>()
                .ForEach((Entity entity, IsActor actor, ActorHealth actorHealth) => {
                    if (actorHealth.regenRate > 0 && actorHealth.health > 0 && actorHealth.health < actorHealth.healthMax) {
                        actorHealth.health += actorHealth.regenRate;
                    }
                });
        }


        private void FixedUpdate() {
            // Entities.WithAll<IsActor, ActorHealth>()
            //     .ForEach((Entity entity, IsActor actor, ActorHealth actorHealth) => {
            //         if (actorHealth.regenRate > 0 && actorHealth.health > 0 && actorHealth.health < actorHealth.healthMax) {
            //             actorHealth.health += actorHealth.regenRate;
            //         }
            //     });
        }


    }

}