using UnityEngine;

namespace Hybrid.Components
{
    [RequireComponent(
        typeof(ActorFOV)
        ,typeof(Target)
        ,typeof(Targeting)
        )]

    public class IsActor : MonoBehaviour {
        public enum ActorType
        {
            NPC = 0,
            Animal,
        } 
    }   
}





