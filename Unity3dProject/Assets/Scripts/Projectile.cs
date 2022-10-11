using UnityEngine;

namespace Hybrid.Components
{
    public struct SpellInstanceData
    {
        public SpellInstanceData(
            GameObject _caster,
            GameObject _target,
            ProjectileType _projectileType,
            MagicSpell _baseMagicSpell
        )
        {
            caster = _caster;
            target = _target;
            baseMagicSpell = _baseMagicSpell;
            projectileType = _projectileType;
        }

        public GameObject caster { get; }

        public GameObject target { get; }

        public ProjectileType projectileType { get; }

        public MagicSpell baseMagicSpell { get; }
    }

    public enum ProjectileType
    {
        spell = 0,
        bullet = 1,
        arrow = 2,
        bolt = 3,
        spear = 4
    }

    public class Projectile : MonoBehaviour
    {
        public ProjectileType projectileType;

        public GameObject sender;

        public float damage = 0f;

        public float speed = 24f;

        [SerializeField]
        public GameObject spellPrefab;

        [SerializeField]
        public MagicSpell magicSpellPrefab;

        private Spell spell;

        public Weapon weapon;

        [SerializeField]
        public MagicSpell[] spells; // { get; private set; }

        [SerializeField]
        private bool timedSelfdestruct = true;

        [SerializeField]
        private float selfDestructTimer = 6f;

        private Rigidbody rigidbody;

        private void Start()
        {
            rigidbody = GetComponent<Rigidbody>();
            rigidbody.velocity = transform.forward * speed;

            if (timedSelfdestruct)
            {
                Destroy (gameObject, selfDestructTimer);
            }
        }

        public void FireMagicEffects(GameObject target)
        {
            if (spellPrefab != null)
            {
                Debug.Log("FireMagicEffects = Spell: " + this.gameObject.name);

                Transform spell = Instantiate(spellPrefab.transform);
                ActiveSpellController spellController =
                    spell.gameObject.GetComponent<ActiveSpellController>();
                if (spellController != null)
                {
                    spellController
                        .FireMagicEffects(new SpellInstanceData(sender,
                            target,
                            projectileType,
                            magicSpellPrefab));
                }
                Destroy (gameObject, selfDestructTimer);
            }
            else
            {
                Destroy (gameObject);
            }
        }

        // private void OnTriggerEnter(Collider other)
        // {
        // }

        // public void InvokeSpells(GameObject target, GameObject sender)
        // {
        //     if (spells != null && spells.Length > 0)
        //     {
        //         Debug.Log("InvokeSpells - " + spells.Length);
        //         spells[0].InvokeEFfects(target, sender);
        //     }
        // }
    }
}
