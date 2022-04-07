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
                        if (actorHealth.isDead())
                            return;

                        // Handle Death
                        if (actorHealth.health <= 0)
                        {
                            ActorEventManger actorEventManger = actorHealth.gameObject.GetComponent<ActorEventManger>();
                            if (actorEventManger != null) actorEventManger.ActorDeath();
                            return;
                        }

                        // Regeneration
                        if (actorHealth.regenRate > 0 && actorHealth.health < actorHealth.healthMax) actorHealth.health += actorHealth.regenRate;
                        if (actorHealth.staminaRegenRate > 0 && actorHealth.stamina < actorHealth.staminaMax) actorHealth.stamina += actorHealth.staminaRegenRate;
                        if (actorHealth.magicRegenRate > 0 && actorHealth.magic < actorHealth.magicMax) actorHealth.magic += actorHealth.magicRegenRate;
                    });
            }
        }
    }

}