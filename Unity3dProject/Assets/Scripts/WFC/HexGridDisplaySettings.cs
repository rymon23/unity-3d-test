
namespace WFCSystem
{
    [System.Serializable]
    public class HexGridDisplaySettings
    {
        public HexGridDisplaySettings(
            CellDisplay_Type _cellDisplayType,
            GridFilter_Level _gridFilter_Level,
            GridFilter_Type _gridFilter_Type,
            HexCellSizes _gridFilter_size,
            bool _showHighlights = true
        )
        {
            cellDisplayType = _cellDisplayType;
            gridFilter_Level = _gridFilter_Level;
            gridFilter_Type = _gridFilter_Type;
            gridFilter_size = _gridFilter_size;
            showHighlights = _showHighlights;
        }

        public CellDisplay_Type cellDisplayType = CellDisplay_Type.DrawLines;
        public GridFilter_Level gridFilter_Level = GridFilter_Level.All;
        public GridFilter_Type gridFilter_Type = GridFilter_Type.All;
        public HexCellSizes gridFilter_size = HexCellSizes.Default;
        public bool showHighlights = true;
    }
}