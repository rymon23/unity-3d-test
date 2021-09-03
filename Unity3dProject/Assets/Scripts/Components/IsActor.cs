using UnityEngine;

namespace Hybrid.Components
{
    [RequireComponent(
        typeof(Target)
        ,typeof(Targeting)
        ,typeof(QuadrantSearchable)
        )]

    [RequireComponent(
         typeof(ActorFOV)
        ,typeof(ActorFactions)
        ,typeof(ActorNavigationData)
        )]

    public class IsActor : MonoBehaviour {
        public enum ActorType
        {
            NPC = 0,
            Animal,
        } 
    }   
}





