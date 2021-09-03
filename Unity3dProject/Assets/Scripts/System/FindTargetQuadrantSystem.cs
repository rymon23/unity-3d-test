using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Collections;
using Hybrid.Components;
using Unity.Mathematics;

namespace Hybrid.Systems
{  
    public class FindTargetQuadrantSystem : ComponentSystem 
    {
        [ReadOnly] public NativeMultiHashMap<int, QuadrantData> quadrantMultiHashMap;

        protected override void OnCreate () {
            quadrantMultiHashMap = QuadrantSystem.quadrantMultiHashMap;
            base.OnCreate();
        }

        protected override void OnDestroy() {
            quadrantMultiHashMap.Dispose();    
            base.OnDestroy();        
        }

        protected override void OnUpdate(){
            // Debug.Log("FindTargetQuadrantSystem: running");

            int entityCount = 0;
            int targetCount = 0;

            Entities.WithAll<IsActor, Targeting, ActorFOV, ActorFactions>()
                .ForEach((Entity entity, Transform myTransform, Targeting myTargeting, ActorFOV myFOV, ActorFactions actorFactions) => {

                entityCount += 1;

                float3 unitPosition = myTransform.position;
                Entity closestTargetEntity = Entity.Null;
                float closestTargetDistance = float.MaxValue;

                Vector3[] myQuadrantPositions = QuadrantSystem.GetNeighoborQuadrantPositions(myTransform.position);

                for (int i = 0; i < myQuadrantPositions.Length; i++)
                {
                    int hashMapKey = QuadrantSystem.GetPositionHashMapKey(myQuadrantPositions[i]);

                    QuadrantData quadrantData;
                    NativeMultiHashMapIterator<int> nativeMultiHashMapIterator;

                    if (quadrantMultiHashMap.TryGetFirstValue(hashMapKey, out quadrantData, out nativeMultiHashMapIterator)) {
                        do {

                            // Debug.Log(quadrantData.entity);

                            // Make sure its not this entity
                            if ( !quadrantData.entity.Equals(entity) && IsInFOVScope(myFOV.viewPoint, quadrantData.position, myFOV.maxAngle, myFOV.maxRadius)){
                                targetCount += 1;

                                if (closestTargetEntity == Entity.Null) {
                                    // No Target
                                    closestTargetEntity = quadrantData.entity;
                                    closestTargetDistance = math.distancesq(unitPosition, quadrantData.position);

                                } else if (math.distancesq(unitPosition, quadrantData.position) < closestTargetDistance) {
                                    // This target is closer
                                    closestTargetEntity = quadrantData.entity;
                                    closestTargetDistance = math.distancesq(unitPosition, quadrantData.position);
                                }
                            }

                        } while (quadrantMultiHashMap.TryGetNextValue(out quadrantData, ref nativeMultiHashMapIterator));
                    }
                }


                // Debug.Log(closestTargetEntity);

                if (closestTargetEntity != Entity.Null) {
                    Entities.WithAll<IsActor, Targeting, ActorFOV, ActorFactions>()
                    .ForEach((Entity targetEntity, Transform targetTransform) => {
                        if (targetEntity.Equals(closestTargetEntity)) {
                            myFOV.currentTarget = targetTransform.gameObject;

                            if (myTargeting.currentTarget != targetTransform ) {
                                myTargeting.lastTarget = myTargeting.currentTarget;
                                myTargeting.currentTarget = targetTransform;
                            }
                            Debug.Log(myTransform.gameObject.name + "'s closest target: [" + targetTransform.gameObject.name + "] | InFOV: " + IsInFOVScope(myFOV.viewPoint, targetTransform.position, myFOV.maxAngle, myFOV.maxRadius));
                        }
                    });
                }
                
                // targeting.currentTarget = closestTargetTransform;
                // targeting.navPosition = closestTargetTransform.position;
            });

            Debug.Log("FindTargetQuadrantSystem: \n Total Targeters: " + entityCount + " | Total Targets: " + targetCount);
        }

        protected static bool IsInFOVScope(Transform viewer, Vector3 targetPos, float maxAngle, float maxRadius){
            Vector3 directionBetween = (targetPos - viewer.position).normalized;
            directionBetween.y *= 0;
            float angle = Vector3.Angle(viewer.forward, directionBetween);


            //TO DO: fix LOS
            // if (angle <= maxAngle)
            // {
            //     Ray ray = new Ray(viewer.position, targetPos - viewer.position);
            //     RaycastHit hit;

            //     if (Physics.Raycast(ray, out hit, maxRadius))  return hit.transform.position == targetPos;
            // }
            return (angle <= maxAngle);
            // return false;
        }

    }

}