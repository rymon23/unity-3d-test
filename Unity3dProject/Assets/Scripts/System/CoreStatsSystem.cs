using UnityEngine;
using UnityEngine.AI;
using Unity.Entities;
using Hybrid.Components;


namespace Hybrid.Systems
{
    public class CoreStatsSystem : ComponentSystem
    {

        private float timer = 1f;
        protected override void OnUpdate()
        {
            timer -= Time.DeltaTime;

            if (timer < 0)
            {
                timer = 1f;

                // Debug.Log(timer);

                Entities.WithAll<ActorHealth>()
                    .ForEach((Entity entity, ActorHealth actorHealth) =>
                    {
                        if (actorHealth.deathState == DeathState.alive)
                        {
                            if (actorHealth.health <= 0)
                            {
                                ActorEventManger actorEventManger = actorHealth.gameObject.GetComponent<ActorEventManger>();
                                if (actorEventManger != null) actorEventManger.ActorDeath();
                                
                                return;

                                // actorHealth.deathState = DeathState.dead;
                                // Animator animator = actorHealth.gameObject.GetComponentInChildren<Animator>();
                                // NavMeshAgent agent = actorHealth.gameObject.GetComponentInChildren<NavMeshAgent>();

                                // animator?.SetBool("isDead", true);
                                // animator?.SetTrigger("Death");
                                // animator.enabled = false;
                                // // Disable nav
                                // agent.enabled = false;
                                // return;
                            }

                            if (actorHealth.regenRate > 0 && actorHealth.health > 0 && actorHealth.health < actorHealth.healthMax)
                            {
                                actorHealth.health += actorHealth.regenRate;
                            }
                        }
                        else
                        {
                            //DEAD
                        }

                    });
            }
        }
    }

}