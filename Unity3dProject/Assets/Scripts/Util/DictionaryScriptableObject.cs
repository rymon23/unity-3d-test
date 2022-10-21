using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// [CreateAssetMenu(fileName = "New Dictionary Storage", menuName = "Data Objects/Dictionary Storage Object")]
public class DictionaryScriptableObject : ScriptableObject
{
    [SerializeField] private List<ScriptableItem> keys = new List<ScriptableItem>();
    [SerializeField] private List<int> values = new List<int>();
    public List<ScriptableItem> Keys { get => keys; set => keys = value; }
    public List<int> Values { get => values; set => values = value; }
    private void OnValidate()
    {
        Debug.Log("OnValidate => Keys: " + Keys.Count + " / Instance: " + this.GetInstanceID());
    }
}
