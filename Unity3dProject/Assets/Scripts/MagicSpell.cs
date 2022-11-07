using UnityEngine;
using UnityEngine.Events;
using Hybrid.Components;

[CreateAssetMenu(fileName = "New Magic Spell", menuName = "Magic Spell")]
public class MagicSpell : ScriptableObject
{
    public new string name;
    public UnityEvent<GameObject, GameObject>[] effects;
    public Transform projectile;
    public GameObject spellPrefab;

    [SerializeField] private SpellCastingType _castingType = SpellCastingType.fire;
    public SpellCastingType castingType
    {
        get => _castingType;
    }
    [SerializeField] private SpellDeliveryType _deliveryType = SpellDeliveryType.aimed;
    public SpellDeliveryType deliveryType
    {
        get => _deliveryType;
    }

    public float baseMagicCost = 0f ;
    public float baseStaminaCost  = 0f;

    [Header("FX")]
    [SerializeField]
    private ParticleSystem FX_OnCast;
    public ParticleSystem GetFX_OnCast() => FX_OnCast;

    [SerializeField]
    private ParticleSystem impactParticleSystem;
    public ParticleSystem GetFX_OnImpact() => impactParticleSystem;

    [SerializeField]
    private TrailRenderer bulletTrail;
    public TrailRenderer GetFX_Trail() => bulletTrail;
    public void InvokeEFfects(GameObject target, GameObject sender)
    {
        if (effects != null)
        {
            Debug.Log("InvokeEFfects - " + effects.Length);
            effects[0].Invoke(target, sender);
        }
    }
}
