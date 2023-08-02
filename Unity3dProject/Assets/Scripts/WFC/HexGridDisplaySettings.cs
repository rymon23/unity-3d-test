
namespace WFCSystem
{
    [System.Serializable]
    public struct HexGridDisplaySettings
    {
        public HexGridDisplaySettings(
            CellDisplay_Type _cellDisplayType,
            GridFilter_Level _gridFilter_Level,
            GridFilter_Type _gridFilter_Type,
            HexCellSizes _gridFilter_size,
            bool _showHighlights
        )
        {
            cellDisplayType = _cellDisplayType;
            gridFilter_Level = _gridFilter_Level;
            gridFilter_Type = _gridFilter_Type;
            gridFilter_size = _gridFilter_size;
            showHighlights = _showHighlights;
        }

        public CellDisplay_Type cellDisplayType;
        public GridFilter_Level gridFilter_Level;
        public GridFilter_Type gridFilter_Type;
        public HexCellSizes gridFilter_size;
        public bool showHighlights;
    }
}