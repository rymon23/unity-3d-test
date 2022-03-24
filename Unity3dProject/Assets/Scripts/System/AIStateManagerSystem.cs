using UnityEngine;
using Unity.Entities;
using Hybrid.Components;


namespace Hybrid.Systems
{
    public class AIStateManagerSystem : ComponentSystem
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
                // Debug.Log(timer);

                Entities.WithAll<ActorAIStateData, Targeting, CombatStateData, IsActor>()
                    .ForEach((Entity entity, IsActor actor, ActorAIStateData actorAIStateData, Targeting targeting, CombatStateData combatStateData) =>
                    {
                        if (targeting.refId == UtilityHelpers.GetUnsetActorEntityRefId())
                        {
                            // string refId = $"ref#{entity.Version}{entity.Index}";
                            string refId = UtilityHelpers.getActorEntityRefId(entity);
                            targeting.refId = refId;
                            actor.refId = refId;
                            Debug.Log("targeting _id: " + targeting.refId);
                        }

                        if (actorAIStateData.aiState != AIState.combat)
                        {
                            if (combatStateData.IsInCombat())
                            {
                                actorAIStateData.aiState = AIState.combat;
                            }
                        }
                        else
                        {
                            if (!combatStateData.IsInCombat())
                            {
                                actorAIStateData.aiState = AIState.idle;
                            }
                        }
                    });
            }
        }
    }

}
