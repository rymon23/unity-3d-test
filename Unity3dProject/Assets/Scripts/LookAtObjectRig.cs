using System;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using Hybrid.Components;

public class LookAtObjectRig : MonoBehaviour
{

    private Rig rig;
    [SerializeField] private float targetWeight = 0f;
    [SerializeField] private Targeting myTargeting;

    private void Awake()
    {
        rig = GetComponent<Rig>();
        myTargeting = GetComponentInParent<Targeting>();
    }

    private void Update()
    {
        if (myTargeting != null && myTargeting.currentTarget != null)
        {
            if (Vector3.Distance(myTargeting.currentTarget.gameObject.transform.position, myTargeting.gameObject.transform.position) < 5f)
            {
                targetWeight = 1f;
            }
            else
            {
                targetWeight = 0f;
            }
        }

        rig.weight = Mathf.Lerp(rig.weight, targetWeight, Time.deltaTime * 10f);
    }
}
