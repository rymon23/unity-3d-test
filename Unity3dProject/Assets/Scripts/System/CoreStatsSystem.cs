using Hybrid.Components;
using Unity.Entities;

namespace Hybrid.Systems
{
    public class CoreStatsSystem : ComponentSystem
    {
        private float defaultUpdate = 1f;

        private float defaultUpdateTimer = 1f;

        protected override void OnUpdate()
        {
            if (defaultUpdateTimer > 0f)
            {
                defaultUpdateTimer -= Time.DeltaTime;
            }
            else
            {
                defaultUpdateTimer = defaultUpdate;
                UpdateCoreStatRegeneration();
            }
        }

        private void UpdateCoreStatRegeneration()
        {
            Entities
                .WithAll<ActorHealth>()
                .ForEach((Entity entity, ActorHealth actorHealth) =>
                {
                    if (actorHealth.isDead()) return;

                    // Handle Death
                    if (actorHealth.health <= 0)
                    {
                        ActorEventManger actorEventManger =
                            actorHealth
                                .gameObject
                                .GetComponent<ActorEventManger>();
                        if (actorEventManger != null)
                            actorEventManger
                                .ActorDeath(actorHealth.GetKiller());
                        return;
                    }

                    // Regeneration
                    if (
                        actorHealth.regenRate > 0 &&
                        actorHealth.health < actorHealth.healthMax
                    ) actorHealth.health += actorHealth.regenRate;

                    if (
                        actorHealth.staminaRegenRate > 0 &&
                        actorHealth.stamina < actorHealth.staminaMax
                    ) actorHealth.stamina += actorHealth.staminaRegenRate;

                    if (
                        actorHealth.magicRegenRate > 0 &&
                        actorHealth.magic < actorHealth.magicMax
                    ) actorHealth.magic += actorHealth.magicRegenRate;

                    //Energy Armor
                    if (
                        actorHealth.energyArmorMax > 0 &&
                        actorHealth.energyArmor < actorHealth.energyArmorMax
                    )
                    {
                        if (actorHealth.energyArmorRegenDelayTimer > 0f)
                        {
                            actorHealth.energyArmorRegenDelayTimer -= 1;
                        }
                        else
                        {
                            actorHealth.energyArmor +=
                                actorHealth.energyArmorRegenRate;
                            actorHealth.energyArmorRegenDelayTimer = 0;
                        }
                    }
                });
        }
    }
}
