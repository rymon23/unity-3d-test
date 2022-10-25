using System;
using Hybrid.Components;
using UnityEngine;
using Unity.Entities;

namespace Hybrid.Systems
{
    public class TerritoryStateSystem : ComponentSystem
    {
        private float defaultUpdate = 1.0f;

        private float defaultUpdatetimer = 0f;

        private float evaluateNeightborsUpdate = 400f;

        private float evaluateNeightborsUpdateTmer = 0f;

        private float evalulateTeamGroupTerritoryPosUpdate = 5f;

        private float evalulateTeamGroupTerritoryPosUpdateTmer = 0f;

        private void Start()
        {
            defaultUpdatetimer = defaultUpdate;
        }

        protected override void OnUpdate()
        {
            if (evaluateNeightborsUpdateTmer > 0f)
            {
                evaluateNeightborsUpdateTmer -= Time.DeltaTime;
            }
            else
            {
                evaluateNeightborsUpdateTmer = evaluateNeightborsUpdate;
                EvaluateAllTerritoryNeightbors();
            }

            if (defaultUpdatetimer > 0f)
            {
                defaultUpdatetimer -= Time.DeltaTime;
            }
            else
            {
                defaultUpdatetimer = defaultUpdate;
                UpdateTerritories();
            }

            if (evalulateTeamGroupTerritoryPosUpdateTmer > 0f)
            {
                evalulateTeamGroupTerritoryPosUpdateTmer -= Time.DeltaTime;
            }
            else
            {
                evalulateTeamGroupTerritoryPosUpdateTmer =
                    evalulateTeamGroupTerritoryPosUpdate;
                EvaluateTeamGroupTerritoryLocation();
            }
        }

        private void UpdateTerritories()
        {
            Debug.Log("TerritoryStateSystem: UpdateTerritories");

            Entities
                .WithAll<Territory>()
                .ForEach((Entity entity, Territory territory) =>
                {
                    territory.EvaluateCurrentState();

                    TerritoryDebugStats debugStats =
                        territory
                            .gameObject
                            .GetComponentInChildren<TerritoryDebugStats>();

                    territory.RegenerateHealth();
                    territory.EvaluateUnitAllocation();
                    debugStats.UpdateStats();

                    // Entities
                    //     .WithAll<Territory>()
                    //     .ForEach((Entity entityB, Territory territoryB) =>
                    //     {
                    //         if (entity.Equals(entityB)) return;
                    //         float distance =
                    //             Vector3
                    //                 .Distance(territory
                    //                     .gameObject
                    //                     .transform
                    //                     .position,
                    //                 territoryB.gameObject.transform.position);

                    //         if (distance < Territory.maxNeighborDistance)
                    //         {
                    //             territory.AddNeighbor (territoryB);
                    //         }
                    //     });
                });
        }

        private void EvaluateAllTerritoryNeightbors()
        {
            Debug.Log("TerritoryStateSystem: EvaluateAllTerritoryNeightbors");

            Entities
                .WithAll<Territory>()
                .ForEach((Entity entity, Territory territory) =>
                {
                    Entities
                        .WithAll<Territory>()
                        .ForEach((Entity entityB, Territory territoryB) =>
                        {
                            if (entity.Equals(entityB)) return;

                            float distance =
                                Vector3
                                    .Distance(territory
                                        .gameObject
                                        .transform
                                        .position,
                                    territoryB.gameObject.transform.position);

                            // Debug
                            //     .Log("TerritoryStateSystem: neighborDistance: " +
                            //     distance);
                            if (distance < Territory.maxNeighborDistance)
                            {
                                territory.AddNeighbor (territoryB);
                            }
                        });

                    territory.EvaluateNeighbors();
                });
        }

        private void EvaluateTeamGroupTerritoryLocation()
        {
            Debug
                .Log("TerritoryStateSystem: EvaluateTeamGroupTerritoryLocation");

            Entities
                .WithAll<TeamGroup>()
                .ForEach((Entity entity, TeamGroup teamGroup) =>
                {
                    bool bFound = false;

                    Entities
                        .WithAll<Territory>()
                        .ForEach((Entity entityB, Territory territory) =>
                        {
                            if (!bFound)
                            {
                                float distance =
                                    Vector3
                                        .Distance(territory
                                            .gameObject
                                            .transform
                                            .position,
                                        teamGroup
                                            .gameObject
                                            .transform
                                            .position);

                                if (distance < territory.GetMinInvasionRadius())
                                {
                                    teamGroup.SetLocalTerritory (territory);
                                    bFound = true;
                                }
                            }
                        });

                    if (!bFound) teamGroup.SetLocalTerritory(null);
                });
        }
    }
}
