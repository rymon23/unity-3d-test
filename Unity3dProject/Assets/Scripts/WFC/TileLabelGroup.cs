using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TileLabelGroup : MonoBehaviour
{
    public GameObject[] labels;

    private void OnValidate()
    {
        Reevaluate();
    }

    private void Reevaluate()
    {
        RectTransform[] found = GetComponentsInChildren<RectTransform>();
        if (found.Length != labels.Length)
        {
            GameObject[] updated = new GameObject[found.Length];
            for (int i = 0; i < updated.Length; i++)
            {
                updated[i] = found[i].gameObject;
                updated[i].name = "label_" + i;
            }
            labels = updated;
        }
    }

    public void SetLabelsEnabled(bool enable)
    {
        if (labels != null && labels.Length > 0)
        {
            for (int i = 0; i < labels.Length; i++)
            {
                labels[i].SetActive(enable);
            }
        }
    }
}
