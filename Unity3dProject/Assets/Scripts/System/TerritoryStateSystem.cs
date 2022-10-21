using Hybrid.Components;
using UnityEngine;
using Unity.Entities;

namespace Hybrid.Systems
{
    public class TerritoryStateSystem : ComponentSystem
    {
        private float updateTime = 1.0f;

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
                Entities
                    .WithAll<Territory>()
                    .ForEach((Entity entity, Territory territory) =>
                    {
                        territory.EvaluateCurrentState();
                    });
            }
        }
    }
}
