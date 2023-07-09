using UnityEngine;

namespace WFCSystem
{
    [CreateAssetMenu(fileName = "New Tile Series", menuName = "Tile Series")]
    public class TileSeriesObject : ScriptableObject
    {
        public Color color = Color.green;
    }
}