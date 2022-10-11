using System.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActorInventory : MonoBehaviour
{
    [SerializeField] private Dictionary<ItemType, Dictionary<Item, int>> _inventoryItems;
    [SerializeField] private float totalWeight = 0;

    public Item[] items_TEMP;
    public ItemList[] itemLists;

    ActorEventManger actorEventManger;

    public float GetTotalWeight()
    {
        return totalWeight;
    }
    public bool HasItem(Item item)
    {
        return _inventoryItems[item.type].ContainsKey(item);
    }

    public int GetItemCount(Item item)
    {
        if (HasItem(item))
        {
            return _inventoryItems[item.type][item];
        }
        return 0;
    }
    public void AddItem(Item item, int amount = 1)
    {
        if (HasItem(item))
        {
            _inventoryItems[item.type][item] += amount;
        }
        else
        {
            _inventoryItems[item.type].Add(item, amount);

            if (item.type == ItemType.weapon) actorEventManger.WeaponItemAdded(item);
        }
        totalWeight += (item.weight * amount);
    }

    public void RemoveItem(Item item, int amount = 1)
    {
        if (HasItem(item))
        {
            _inventoryItems[item.type][item]--;
            totalWeight -= (item.weight * amount);
        }
    }
    public GameObject GetFirstWeapon()
    {
        GameObject result = null;

        foreach (Item item in _inventoryItems[ItemType.weapon].Keys)
        {
            result = item.GetPrefab();
            if (result != null) break;
        }

        return result;
    }

    public List<Item> GetWeaponsOfType(WeaponType type)
    {
        List<Item> result = new List<Item>();

        foreach (Item item in _inventoryItems[ItemType.weapon].Keys)
        {
            GameObject gm = item.GetPrefab();

            if (gm != null && gm.GetComponent<Weapon>().weaponType == type)
            {
                result.Add(item);
            }
        }
        return result;
    }

    public Item GetWeaponItemOfType(WeaponType type)
    {
        if (weaponsByType.ContainsKey(type))
        {
            return weaponsByType[type][0];
        }
        return null;
    }

    private Dictionary<WeaponType, List<Item>> weaponsByType = new Dictionary<WeaponType, List<Item>>();
    public Dictionary<WeaponType, List<Item>> EvaluateWeapons()
    {
        Dictionary<WeaponType, List<Item>> result = new Dictionary<WeaponType, List<Item>>();

        foreach (Item item in _inventoryItems[ItemType.weapon].Keys)
        {
            GameObject gm = item.GetPrefab();

            if (gm != null)
            {
                Weapon weapon = gm.GetComponent<Weapon>();
                if (!result.ContainsKey(weapon.weaponType)) result.Add(weapon.weaponType, new List<Item>());

                if (!result[weapon.weaponType].Contains(item))
                {
                    result[weapon.weaponType].Add(item);
                }
            }
        }
        weaponsByType = result;
        return result;
    }


    public void ResetInventory()
    {
        if (_inventoryItems != null)
        {
            _inventoryItems.Clear();
        }
        _inventoryItems = new Dictionary<ItemType, Dictionary<Item, int>>();
        _inventoryItems.Add(ItemType.weapon, new Dictionary<Item, int>());
        _inventoryItems.Add(ItemType.throwable, new Dictionary<Item, int>());
        _inventoryItems.Add(ItemType.consumable, new Dictionary<Item, int>());
        _inventoryItems.Add(ItemType.gear, new Dictionary<Item, int>());
        _inventoryItems.Add(ItemType.other, new Dictionary<Item, int>());

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
        if (itemLists != null)
        {
            foreach (ItemList itemList in itemLists)
            {
                if (itemList.randomize)
                {
                    // ItemList.ItemListEntry entry = itemList.GetRandomItem();
                    // if (entry.item != null)
                    // {
                    //     AddItem(entry.item, entry.amount);
                    //     added++;
                    // }
                    Dictionary<Item, int> listItems = itemList.GetRandomInItemList();
                    foreach (Item item in listItems.Keys)
                    {
                        AddItem(item, listItems[item]);
                        added++;
                    }
                }
                else
                {
                    Dictionary<Item, int> listItems = itemList.ExtractItems();
                    foreach (Item item in listItems.Keys)
                    {
                        AddItem(item, listItems[item]);
                        added++;
                    }
                }
            }
        }
        Debug.Log("ActorInventory => Items Added: " + added);

        EvaluateWeapons();
    }

    void Start()
    {
        actorEventManger = GetComponent<ActorEventManger>();

        Debug.Log("ActorInventory => init");
        ResetInventory();
        // AddEditorItems();
        Invoke(nameof(AddEditorItems), 1.5f);
    }

}
