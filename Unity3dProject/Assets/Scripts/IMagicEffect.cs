using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IMagicEffect
{
    public void OnEffectStart(GameObject target, GameObject sender);
    public void OnEffectUpdate(GameObject target, GameObject sender);
    public void OnEffectFinish(GameObject target, GameObject sender);
}
