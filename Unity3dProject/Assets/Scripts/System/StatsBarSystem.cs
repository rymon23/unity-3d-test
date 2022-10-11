using UnityEngine;
using Unity.Entities;
using Hybrid.Components;

namespace Hybrid.Systems
{
    public class StatsBarSystem : ComponentSystem
    {
        private float updateTime = 0.2f;
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

                // Debug.Log("StatsBarSystem");

                Entities.WithAll<IsActor, ActorHealth, CombatStateData>()
                    .ForEach((Entity entity, IsActor actor, ActorHealth actorHealth, CombatStateData combatStateData) =>
                    {

                        if (actor.gameObject.tag == "Player")
                        {
                            if (actorHealth.healthBar != null)
                            {
                                if (actorHealth.healthPercent < 0.98)
                                {
                                    actorHealth.healthBar.gameObject.SetActive(true);
                                    actorHealth.healthBar.UpdateBar(actorHealth.healthPercent);
                                }
                                else
                                {
                                    actorHealth.healthBar.gameObject.SetActive(false);
                                }
                            }
                            if (actorHealth.staminaBar != null)
                            {
                                if (actorHealth.staminaPercent < 0.98)
                                {
                                    actorHealth.staminaBar.gameObject.SetActive(true);
                                    actorHealth.staminaBar.UpdateBar(actorHealth.staminaPercent);
                                }
                                else
                                {
                                    actorHealth.staminaBar.gameObject.SetActive(false);
                                }
                            }
                            if (actorHealth.magicBar != null)
                            {
                                if (actorHealth.magicPercent < 0.98)
                                {
                                    actorHealth.magicBar.gameObject.SetActive(true);
                                    actorHealth.magicBar.UpdateBar(actorHealth.magicPercent);
                                }
                                else
                                {
                                    actorHealth.magicBar.gameObject.SetActive(false);
                                }
                            }
                            return;
                        }

                        if (actorHealth.healthBar == null) return;

                        if (actorHealth.deathState < DeathState.dying)
                        {
                            // Debug.Log("healthPerc: " + healthPerc + " IsInCombat: " + combatStateData.IsInCombat());

                            if (combatStateData.IsInCombat() || actorHealth.healthPercent < 0.98)
                            {
                                actorHealth.healthBar.gameObject.SetActive(true);
                                actorHealth.healthBar.transform.LookAt(Camera.main.transform);
                                actorHealth.healthBar.UpdateBar(actorHealth.healthPercent);
                            }
                            else
                            {
                                actorHealth.healthBar.gameObject.SetActive(false);
                            }
                        }
                        else
                        {
                            actorHealth.healthBar.gameObject.SetActive(false);
                        }

                    });
            }
        }
    }

}
