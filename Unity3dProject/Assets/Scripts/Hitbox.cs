using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class Hitbox : MonoBehaviour
{
    // private float offset = 1f;
    public BoxCollider col;
    private void Awake() {
        col = GetComponent<BoxCollider>();
    }

    private void OnTriggerEnter(Collider other) {
        Debug.Log("onTriggerEnter: " + gameObject.name);
    }
}
