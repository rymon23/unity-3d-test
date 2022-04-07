using UnityEngine;
using Unity.Entities;
using Hybrid.Components;


namespace Hybrid.Systems
{
    public class AlertedStateSystem : ComponentSystem
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

                Entities.WithAll<IsActor, Targeting, ActorFOV, CombatStateData>()
                    .ForEach((Entity entity, IsActor actor, Targeting targeting, CombatStateData combatStateData, ActorHealth actorHealth) =>
                    {
                        // Check if actor is Dead or the Player
                        if (actorHealth.isDead() || !combatStateData.IsAlerted() || actor.gameObject.tag == "Player")
                        {
                            return;
                        }

                        if (combatStateData.alertedTimer < 0)
                        {
                            combatStateData.ResetTimer_Alerted();
                            combatStateData.combatState = CombatState.inactive;
                            ActorEventManger myEventManger = actor.gameObject.GetComponent<ActorEventManger>();
                            if (myEventManger) myEventManger.CombatStateChange(combatStateData.combatState);
                        }
                        else
                        {
                            combatStateData.UpdateTimer_Alerted();
                        }
                    });


            }
        }
    }
}