using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Hybrid.Components
{
    public class Spell : MonoBehaviour
    {
        public Delegate onDamageHealth;

        // public void DamageHealth() => onDamageHealth?.Invoke();
        public UnityEvent[] unityEvent;

        [SerializeField]
        public List<IMagicEffect> array;

        public UnityAction<GameObject>[] unityAction;

        public ScriptableObject[] scriptableObjects;

        [SerializeField]
        public IMagicEffect[] imagicEffects;

        // [SerializeField] public [] objects;
        public float damage = 33f;

        // public float speed = 24f;
        public Weapon weapon;

        [SerializeField]
        private MagicEffect[] effects;

        private Vector3 shootDir;

        private Rigidbody rigidbody;

        private void Start()
        {
            rigidbody = GetComponent<Rigidbody>();
            // rigidbody.velocity = transform.forward * speed;
        }

        private void OnTriggerEnter(Collider other)
        {
            Destroy (gameObject);
        }

        public void TestMethod()
        {
        }
    }
}
