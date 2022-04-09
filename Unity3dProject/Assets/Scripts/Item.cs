using UnityEngine;

[CreateAssetMenu(fileName = "New Item", menuName = "Item")]
public class Item : ScriptableObject
{
    public new string name;
    public float value;
    public float weight = 0f;
    public ItemType type;
    [SerializeField] private Sprite icon;
    [SerializeField] private GameObject prefeb;
}