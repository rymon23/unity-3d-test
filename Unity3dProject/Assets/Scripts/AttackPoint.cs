using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackPoint : MonoBehaviour
{
    public Transform attackPoint;
    public bool bDebug; 
    [SerializeField] public float radius = 0.075f;

    private void Awake() {
        if (gameObject?.transform) {
            attackPoint = gameObject.transform;
        }
    }

     private void OnDrawGizmos() {
        if (!bDebug || !attackPoint) return;
        Gizmos.color = Color.magenta;
        Gizmos.DrawSphere(attackPoint.position, radius);
    }
}
