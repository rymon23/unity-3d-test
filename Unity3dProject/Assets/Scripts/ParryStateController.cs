using UnityEngine;

public class ParryStateController : MonoBehaviour
{
    AnimationState animationState;
    public GameObject blockingCollider;
    public bool canParry;

    void Start()
    {
        animationState = GetComponent<AnimationState>();
    }    
}
