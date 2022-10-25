using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;


[CreateAssetMenu(fileName = "New Item List", menuName = "Item List")]
public class ItemList : ScriptableItem, ISerializationCallbackReceiver
{

    [MenuItem("GameObject/Create Material")]
    static DictionaryScriptableObject GenerateDictionaryData(int objectId, string assetPath = "")
    {
        if (assetPath == "") assetPath = $"Assets/Items/ObjectData/New_ItemListData_{objectId}.asset";

        DictionaryScriptableObject newData = new DictionaryScriptableObject();
        AssetDatabase.CreateAsset(newData, assetPath);

        // Print the path of the created asset
        Debug.Log(AssetDatabase.GetAssetPath(newData));
        return newData;
    }

    public struct ItemListEntry
    {
        public ItemListEntry(Item _item, int _amount = 1)
        {
            item = _item;
            amount = _amount;
        }
        public Item item { get; }
        public int amount { get; }
    }


    public bool randomize = false;
    public int random = 100;
    [SerializeField] private DictionaryScriptableObject dictionaryData;
    [SerializeField] private List<ScriptableItem> keys = new List<ScriptableItem>();
    // [SerializeField] private List<Item> keys = new List<Item>();
    [SerializeField] private List<int> values = new List<int>();
    private Dictionary<ScriptableItem, int> items = new Dictionary<ScriptableItem, int>();
    public bool modifyValues;

    public void OnBeforeSerialize()
    {
        if (modifyValues || dictionaryData == null) return;

        Debug.Log("ItemList => OnBeforeSerialize");

        keys.Clear();
        values.Clear();
        for (int i = 0; i < Mathf.Min(dictionaryData.Keys.Count, dictionaryData.Values.Count); i++)
        {
            keys.Add(dictionaryData.Keys[i]);
            values.Add(dictionaryData.Values[i]);
        }
    }

    private void OnValidate()
    {
        ValidateItems();
    }

    private void Awake()
    {
        if (dictionaryData == null)
        {
            Debug.Log("ItemList => GenerateDictionaryData");
            dictionaryData = GenerateDictionaryData(this.GetInstanceID());
        }

        ValidateItems();
    }

    public void OnAfterDeserialize() { }
    private void OnGUI() { }

    public void DeserializeDictionary()
    {
        Debug.Log("ItemList => DeserializeDictionary");

        items = new Dictionary<ScriptableItem, int>();
        dictionaryData.Keys.Clear();
        dictionaryData.Values.Clear();
        
        for (int i = 0; i < Mathf.Min(keys.Count, values.Count); i++)
        {
            //Prevent self reference
            if (keys[i].GetInstanceID() != this.GetInstanceID())
            {
                dictionaryData.Keys.Add(keys[i]);
                dictionaryData.Values.Add(values[i]);
                items.Add(keys[i], values[i]);
            }
        }
        modifyValues = false;
    }


    public void PrintDictionary()
    {
        foreach (var item in items)
        {
            Debug.Log("PrintDictionary=> Key: " + item.Key + " Value" + item.Value);
        }
    }


    private void ValidateItems()
    {
        if (modifyValues) return;

        // if (items == null)
        // {
        //     items = new Dictionary<ScriptableItem, int>();
        // }

        for (int i = 0; i < Mathf.Min(dictionaryData.Keys.Count, dictionaryData.Values.Count); i++)
        {
            if (items.ContainsKey(dictionaryData.Keys[i]))
            {
                items[dictionaryData.Keys[i]] += dictionaryData.Values[i];
            }
            else
            {
                items.Add(dictionaryData.Keys[i], dictionaryData.Values[i]);
            }
        }
        // Debug.Log("ValidateItems!");

        PrintDictionary();
    }

    // public override ItemListEntry SampleItem(int recursiveCount = 0) => GetRandomItem(recursiveCount);
    public override Dictionary<Item, int> SampleItem(int recursiveCount = 0)
    {
        if (randomize) return GetRandomInItemList(recursiveCount);
        return ExtractItems();
    }

    public override Dictionary<Item, int> ResolveScriptableItems()
    {
        Debug.Log("ItemList => ResolveScriptableItems");
        if (randomize) return GetRandomInItemList();
        return ExtractItems();

        // return this.ExtractItems();
    }

    // public ItemListEntry GetRandomItem(int recursiveCount = 0)
    // {
    //     int ix = Random.Range(0, keys.Count);

    //     Debug.Log("GetRandomItem:  items " + keys.Count);

    //     ItemListEntry res = keys[ix].SampleItem(values[ix] + recursiveCount);

    //     Debug.Log("GetRandomItem:  res " + res.item.name);

    //     return res;
    //     // return keys[ix].SampleItem(values[ix] + recursiveCount);
    // }

    public Dictionary<Item, int> GetRandomInItemList(int recursiveCount = 0)
    {
        int ix = Random.Range(0, keys.Count);
        Debug.Log("GetRandomItem:  items " + keys.Count);

        Dictionary<Item, int> res = keys[ix].SampleItem(values[ix] + recursiveCount);

        Debug.Log("GetRandomItem:  res " + res.Count);

        return res;
    }

    public Dictionary<Item, int> ExtractItems()
    {
        Dictionary<Item, int> result = new Dictionary<Item, int>();

        // Debug.Log("ExtractItems: " + keys.Count);

        foreach (ScriptableItem sItem in items.Keys)
        {
            Dictionary<Item, int> res = sItem.ResolveScriptableItems();
            foreach (Item item in res.Keys)
            {
                if (result.ContainsKey(item))
                {
                    result[item] += res[item];
                }
                else
                {
                    result.Add(item, res[item]);
                }
            }
        }
        return result;
    }
}