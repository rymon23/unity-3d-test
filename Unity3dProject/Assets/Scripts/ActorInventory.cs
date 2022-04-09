using System.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActorInventory : MonoBehaviour
{
    [SerializeField] private float totalWeight = 0;
    [SerializeField] private Dictionary<WeaponWearSlot, Item> equippedWeapons;
    [SerializeField] private Dictionary<ItemType, Dictionary<Item, int>> inventoryItems;

    public Item[] items_TEMP;

    public float GetTotalWeight()
    {
        return totalWeight;
    }
    public bool HasItem(Item item)
    {
        return inventoryItems[item.type].ContainsKey(item);
    }
    public int GetItemCount(Item item)
    {
        if (HasItem(item))
        {
            return inventoryItems[item.type][item];
        }
        return 0;
    }
    public void AddItem(Item item, int amount = 1)
    {
        if (HasItem(item))
        {
            inventoryItems[item.type][item]++;
        }
        else
        {
            inventoryItems[item.type].Add(item, amount);
        }
        totalWeight += (item.weight * amount);
    }
    public void RemoveItem(Item item, int amount = 1)
    {
        if (HasItem(item))
        {
            inventoryItems[item.type][item]--;
            totalWeight -= (item.weight * amount);
        }
    }
    public void ResetInventory()
    {
        if (inventoryItems != null)
        {
            inventoryItems.Clear();
        }
        inventoryItems = new Dictionary<ItemType, Dictionary<Item, int>>();
        inventoryItems.Add(ItemType.weapon, new Dictionary<Item, int>());
        inventoryItems.Add(ItemType.throwable, new Dictionary<Item, int>());
        inventoryItems.Add(ItemType.consumable, new Dictionary<Item, int>());
        inventoryItems.Add(ItemType.gear, new Dictionary<Item, int>());
        inventoryItems.Add(ItemType.other, new Dictionary<Item, int>());

        totalWeight = 0;
        Debug.Log("ActorInventory => ResetInventory");
    }

    private void AddEditorItems()
    {
        int added = 0;
        foreach (Item item in items_TEMP)
        {
            AddItem(item);
            added++;
        }
        Debug.Log("ActorInventory => Items Added: " + added);
    }

    void Start()
    {
        Debug.Log("ActorInventory => init");
        ResetInventory();
        AddEditorItems();
    }

}
