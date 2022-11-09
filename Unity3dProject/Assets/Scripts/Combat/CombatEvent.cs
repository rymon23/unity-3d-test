using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class CombatEvent : MonoBehaviour
{
    // public Event OnAttackHit;
    // public delegate void OnAttackHitAction();
    // public event OnAttackHitAction OnAttackHit;
    public UnityEvent OnAttackHitEvent;

    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnAttackHit(Weapon weapon) {
        Debug.Log("Hit by "+ weapon);
    }

    public void OnAttackBlocked() {

    }
}
