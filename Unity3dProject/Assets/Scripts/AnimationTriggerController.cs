using UnityEngine;

public class AnimationTriggerController : MonoBehaviour
{
    ActorEventManger actorEventManger;
    Animator animator;

    private void Start()
    {
        animator = GetComponent<Animator>();
        actorEventManger = GetComponent<ActorEventManger>();
        if (actorEventManger != null)
        {
            actorEventManger.onTriggerAnim_Stagger += TriggerAnim_Stagger;
            actorEventManger.onTriggerAnim_Block += TriggerAnim_Block;
            actorEventManger.onTriggerAnim_Sheath += TriggerAnim_SheathWeapoon;
            actorEventManger.onTriggerAnim_Draw += TriggerAnim_DrawWeapoon;
        }
    }

    void TriggerAnim_Stagger(HitPositionType hitPosition = 0)
    {
        int currentVariant = ((int)animator.GetFloat("fHitStaggerType"));
        int maxBlendTreeLength = 6;
        int nextVariant = (currentVariant + UnityEngine.Random.Range(1, maxBlendTreeLength)) % maxBlendTreeLength;

        animator.SetFloat("fHitPosition", (float)hitPosition);
        animator.SetTrigger("tStagger");
    }

    void TriggerAnim_Block()
    {
        int currentVariant = ((int)animator.GetFloat("fAnimBlockType"));
        int maxBlendTreeLength = 6;
        int nextVariant = (currentVariant + UnityEngine.Random.Range(1, maxBlendTreeLength)) % maxBlendTreeLength;

        animator.SetFloat("fAnimBlockType", (float)nextVariant);
        animator.SetTrigger("tBlockHit");
    }
    void TriggerAnim_SheathWeapoon()
    {
        animator.SetTrigger("SheathWeapon");
    }
    void TriggerAnim_DrawWeapoon()
    {
        animator.SetTrigger("DrawWeapon");
    }

    private void OnDestroy()
    {
        if (actorEventManger != null)
        {
            actorEventManger.onTriggerAnim_Stagger -= TriggerAnim_Stagger;
            actorEventManger.onTriggerAnim_Block -= TriggerAnim_Block;
            actorEventManger.onTriggerAnim_Sheath -= TriggerAnim_SheathWeapoon;
            actorEventManger.onTriggerAnim_Draw -= TriggerAnim_DrawWeapoon;
        }
    }
}
