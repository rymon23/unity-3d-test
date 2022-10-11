using System.Collections.Generic;
using UnityEngine;


public abstract class ScriptableItem : ScriptableObject
{
    public virtual Dictionary<Item, int> SampleItem(int recursiveCount = 0)
    {
        Debug.Log("ScriptableItem => SampleItem: " + this.name);
        // ItemList.ItemListEntry res = new ItemList.ItemListEntry((Item)this, 1);
        Dictionary<Item, int> res = new Dictionary<Item, int>();
        res.Add((Item)this, 1);
        return res;
    }
    public virtual Dictionary<Item, int> ResolveScriptableItems() => null;
    // {
    //     Debug.Log("ScriptableItem => ResolveScriptableItems: " + this.name);
    //     Dictionary<Item, int> res = new Dictionary<Item, int>();
    //     res.Add((Item)this, 1);
    //     return res;
    // }
    // public ItemList.ItemListEntry SampleItem(int recursiveCount = 0) => new ItemList.ItemListEntry();
}

[CreateAssetMenu(fileName = "New Item", menuName = "Item")]
public class Item : ScriptableItem
{
    public new string name;
    public float value;
    public float weight = 0f;
    public ItemType type;
    public MeshSockets.SocketId equipSocketId;
    [SerializeField] private Sprite icon;
    [SerializeField] private GameObject prefeb;
    public GameObject GetPrefab() => prefeb;
    
    // public new Dictionary<Item, int> SampleItem(int recursiveCount = 0) => new ItemList.ItemListEntry(this, 1 + recursiveCount);
    public new Dictionary<Item, int> SampleItem(int recursiveCount = 0)
    {
        Debug.Log("Item => SampleItem: " + this.name);
        Dictionary<Item, int> res = new Dictionary<Item, int>();
        res.Add(this, 1);
        return res;
    }
    public override Dictionary<Item, int> ResolveScriptableItems()
    {
        Debug.Log("Item => ResolveScriptableItems: " + this.name);
        Dictionary<Item, int> res = new Dictionary<Item, int>();
        res.Add(this, 1);
        return res;
    }
}