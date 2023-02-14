using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProceduralBase;
using System.Linq;

namespace WFCSystem
{
    public class TileCompatibilityHelper : MonoBehaviour
    {
        [SerializeField] private HexagonCellManager cellManager;
        [SerializeField] private TileDirectory tileDirectory;
        [SerializeField] private TileContext tileContext;
        [SerializeField] private TileCompatibilityDirectory tileCompatibilityDirectory;

        [SerializeField] private HexagonCell evaluatedCell;
        [SerializeField] private HexagonTileCore evaluatedTile;
        [SerializeField] private GameObject evaluatedTileGO;
        [SerializeField] private int evaluatedTileIX = 0;

        [SerializeField] private HexagonCell currentCell;
        [SerializeField] private HexagonTileCore currentTile;
        [SerializeField] private GameObject currentTileGO;
        [SerializeField] private int currentTileIX = 0;
        [SerializeField] private int currentRotation;
        [SerializeField] private int currentRotationOffet = -1;
        [SerializeField] private HexagonTileCompatibilitySide currentCompatibilitySide;
        [SerializeField] private HexagonTileCompatibilitySide incomingRelativeCompatibilitySide;
        [SerializeField] private int currentSide;
        [SerializeField] private int currentLayer;

        [Header("Controls")]

        [Header("Evaluated")]
        [SerializeField] private bool evaluateNextTile;

        [Header("Accept")]
        [SerializeField] private bool accept;
        [Header("Decline")]
        [SerializeField] private bool decline;

        [Header("Step")]
        [SerializeField] private bool next;
        [SerializeField] private NextState nextStepLevel = NextState.Default;
        [SerializeField] private bool previous;
        [SerializeField] private NextState previousStepLevel = NextState.Default;
        public enum NextState { Default = 0, NextSide, NextLayer, NextTile }

        [Header("Reset Current Step State")]
        [SerializeField] private bool resetStepState;


        [Header("Restart")]
        [SerializeField] private bool restart;


        [SerializeField] private bool goToNextOnAcceptOrDecline;
        [SerializeField] private bool showCompatibilityVisual = true;
        [Range(0.5f, 8f)][SerializeField] private float compatibilityVisualSize = 2.5f;

        [Header("Table Settings")]
        [SerializeField] private bool reevaluateTileDirectory;
        [SerializeField] private bool reloadExistingCompatibilityTable;

        [Header("Reset Table")]
        [SerializeField] private bool resetCompatibilityTable;


        [Header("Save Data ")]
        [SerializeField] private bool saveData;
        [Header(" ")]
        private bool _rotationsComplete;
        private bool _sidesComplete;
        private bool _layersComplete;
        private Transform tileFolder;

        // by [Incomming Tile id, Existing Tile Id]: Existing Tile's side, compatible[Rotation]
        Dictionary<HexagonTileCompatibilitySide, bool[]>[,] tileDirectCompatibilityMatrix;

        Dictionary<int, HexagonTileCore> tileLookupByid;
        [SerializeField] private List<HexagonTileCore> _tilePrefabs;
        float[] rotationValues = { 0f, 60f, 120f, 180f, 240f, 300f };


        private void OnValidate()
        {
            // InitialSetup();

            // SetupTileFolder();

            if (saveData)
            {
                saveData = false;
                SaveCompatibilityData();
            }

            if (resetCompatibilityTable)
            {
                resetCompatibilityTable = false;
                InitializeCompatibilityMatrix(false);
                return;
            }

            if (reloadExistingCompatibilityTable)
            {
                reloadExistingCompatibilityTable = false;
                InitializeCompatibilityMatrix(true);
                return;
            }

            if (reevaluateTileDirectory)
            {
                reevaluateTileDirectory = false;
                EvaluateTiles();
            }

            if (restart)
            {
                restart = false;

                Restart();
                return;
            }

            if (resetStepState)
            {
                resetStepState = false;

                ResetCurrentNeighborState();
            }

            if (evaluateNextTile)
            {
                evaluateNextTile = false;

                EvaluatedNextTile();
                return;
            }

            if (accept)
            {
                accept = false;
                AcceptPairing();
                return;
            }
            else if (decline)
            {
                decline = false;
                DeclinePairing();
            }


            if (next)
            {
                next = false;
                NextStep(nextStepLevel);
            }
            else if (previous)
            {
                // previous = false;
            }




        }


        private void OnDrawGizmos()
        {
            if (!showCompatibilityVisual || tileDirectCompatibilityMatrix == null) return;
            DrawCompatibilityVisual();
        }


        private void Awake()
        {
            InitialSetup();
        }

        private void InitialSetup()
        {
            cellManager = GetComponent<HexagonCellManager>();

            EvaluateTiles();

            if (tileDirectCompatibilityMatrix == null)
            {
                InitializeCompatibilityMatrix(true);
            }

            ResetCurrentNeighborState();
        }


        private void Restart()
        {
            ResetTiles();

            InstantiateEvaluatedTile();

            ResetCurrentNeighborState();
        }

        private void EvaluateTiles()
        {
            tileLookupByid = tileDirectory.CreateMicroTileDictionary();
            _tilePrefabs = tileLookupByid.Select(x => x.Value).ToList();
        }


        private void AcceptPairing()
        {
            UpdatePairCompatibility(true);

            if (goToNextOnAcceptOrDecline) NextStep();
        }
        private void DeclinePairing()
        {
            UpdatePairCompatibility(false);

            if (goToNextOnAcceptOrDecline) NextStep();
        }


        private void InitializeCompatibilityMatrix(bool loadData)
        {
            Dictionary<HexagonTileCompatibilitySide, bool[]>[,] loadedMatrix = tileCompatibilityDirectory.GetCompatibilityMatrix();
            loadData = loadData && loadedMatrix != null;
            int loadDataLength = loadData ? loadedMatrix.GetLength(0) : 0;

            Dictionary<HexagonTileCompatibilitySide, bool[]>[,] matrix = new Dictionary<HexagonTileCompatibilitySide, bool[]>[_tilePrefabs.Count, _tilePrefabs.Count];

            foreach (var tile_A in _tilePrefabs)
            {
                int placedTileId = tile_A.GetId();

                foreach (var tile_B in _tilePrefabs)
                {
                    int incomingTileId = tile_B.GetId();

                    matrix[incomingTileId, placedTileId] = new Dictionary<HexagonTileCompatibilitySide, bool[]>();

                    for (int compSide = 0; compSide < 8; compSide++)
                    {
                        matrix[incomingTileId, placedTileId].Add((HexagonTileCompatibilitySide)compSide, new bool[6]);

                        if (loadData && incomingTileId < loadDataLength && placedTileId < loadDataLength)
                        {
                            for (int rotation = 0; rotation < 6; rotation++)
                            {
                                matrix[incomingTileId, placedTileId][(HexagonTileCompatibilitySide)compSide][rotation] = loadedMatrix[incomingTileId, placedTileId][(HexagonTileCompatibilitySide)compSide][rotation];
                            }
                        }
                    }
                }
            }
            tileDirectCompatibilityMatrix = matrix;
        }



        private void UpdatePairCompatibility(bool compatible)
        {
            HexagonTileCompatibilitySide placedRelativeSide = currentCompatibilitySide;
            int incomingTileId = currentTile.GetId();
            int placedTileId = evaluatedTile.GetId();

            if (tileDirectCompatibilityMatrix[incomingTileId, placedTileId].ContainsKey(placedRelativeSide) == false)
            {
                tileDirectCompatibilityMatrix[incomingTileId, placedTileId]
                        .Add(placedRelativeSide, new bool[6]);
            }
            tileDirectCompatibilityMatrix[incomingTileId, placedTileId][placedRelativeSide][currentRotation] = compatible;

            // Set incoming relative CompatibilitySide
            if (tileDirectCompatibilityMatrix[placedTileId, incomingTileId].ContainsKey(incomingRelativeCompatibilitySide) == false)
            {
                tileDirectCompatibilityMatrix[placedTileId, incomingTileId]
                        .Add(incomingRelativeCompatibilitySide, new bool[6]);
            }
            tileDirectCompatibilityMatrix[placedTileId, incomingTileId][incomingRelativeCompatibilitySide][0] = compatible;
        }

        private void ClearCompatibility()
        {
            InitializeCompatibilityMatrix(false);
        }

        private void SaveCompatibilityData()
        {
            if (tileCompatibilityDirectory != null)
            {
                Debug.Log("SaveCompatibilityData");

                tileCompatibilityDirectory.UpdateTable(tileDirectCompatibilityMatrix);
            }
        }

        private void LoadCompatibilityData()
        {
            if (tileCompatibilityDirectory != null)
            {
                tileDirectCompatibilityMatrix = tileCompatibilityDirectory.GetCompatibilityMatrix();
            }
        }

        public void NextStep()
        {
            NextStep(NextState.Default);
        }

        private void NextStep(NextState stepLevel = NextState.Default)
        {
            if (stepLevel > NextState.Default || _rotationsComplete)
            {
                if (stepLevel > NextState.NextSide || _sidesComplete)
                {
                    if (stepLevel > NextState.NextLayer || _layersComplete)
                    {
                        NextTile();
                    }
                    else
                    {
                        NextLayer();
                    }
                }
                else
                {
                    NextSide();
                }
            }
            else
            {
                NextRotation();
            }
        }


        private void NextTile()
        {
            currentTileIX++;
            currentTile = _tilePrefabs[currentTileIX % _tilePrefabs.Count];

            ResetCurrentNeighborState();
        }

        private void NextRotation()
        {
            currentRotation = (currentRotation + 1) % 6;
            currentRotationOffet = tileCompatibilityDirectory.GetRotationOffset(0, currentRotation);

            if (currentTileGO != null) currentTileGO.gameObject.transform.rotation = Quaternion.Euler(0f, rotationValues[currentRotation], 0f);
            _rotationsComplete = (currentRotation == 5);

            // currentRotation = (currentRotation % 6);
        }
        private void NextSide()
        {
            currentSide = (currentSide + 1) % 6;
            currentCompatibilitySide = (HexagonTileCompatibilitySide)currentSide;
            _sidesComplete = (currentSide == 5);
            currentLayer = _sidesComplete ? 1 : 0;

            UpdateCurrentCell(false);

            // currentSide++;
            // _sidesComplete = (currentSide == 6);
            // currentSide = (currentSide + 1) % 6;
        }
        private void NextLayer()
        {
            currentLayer = (currentLayer + 1) % 2;
            currentCompatibilitySide = currentLayer == 0 ? HexagonTileCompatibilitySide.Bottom : HexagonTileCompatibilitySide.Top;
            _layersComplete = (currentLayer == 1);

            UpdateCurrentCell(true);

            // currentLayer++;
            // _layersComplete = (currentLayer == 2);
            // currentLayer = (currentLayer + 1) % 2;
        }

        private void UpdateCurrentCell(bool isLayered)
        {
            currentCell = isLayered ? evaluatedCell.layeredNeighbor[currentLayer] : evaluatedCell.neighborsBySide[currentSide];

            if (isLayered)
            {
                incomingRelativeCompatibilitySide = currentLayer == 0 ? HexagonTileCompatibilitySide.Top : HexagonTileCompatibilitySide.Bottom;
            }
            else
            {
                incomingRelativeCompatibilitySide = (HexagonTileCompatibilitySide)(int)evaluatedCell.GetNeighborsRelativeSide((HexagonSide)currentSide);
            }

            ResetRotation();

            UpdateCurrentTile();
        }

        private void UpdateCurrentTile()
        {
            InstantiateCurrentTile();
        }

        private void EvaluatedNextTile()
        {
            evaluatedTileIX++;
            evaluatedTile = _tilePrefabs[evaluatedTileIX % _tilePrefabs.Count];

            ResetCurrentNeighborState();

            InstantiateEvaluatedTile();

            UpdateCurrentTile();
        }

        private void ResetLayer()
        {
            currentLayer = 0;
            currentCompatibilitySide = HexagonTileCompatibilitySide.Bottom;
            _layersComplete = false;
        }

        private void ResetSide()
        {
            currentSide = 0;
            currentCompatibilitySide = 0;
            _sidesComplete = false;
        }
        private void ResetRotation()
        {
            currentRotation = 0;
            currentRotationOffet = 0;
            _rotationsComplete = false;
        }
        private void ResetCurrentNeighborState()
        {
            ResetLayer();
            ResetSide();
            ResetRotation();

            UpdateCurrentCell(false);
        }

        private void ResetTiles()
        {
            evaluatedTileIX = 0;
            evaluatedTile = _tilePrefabs[evaluatedTileIX];

            currentTileIX = 0;
            currentTile = _tilePrefabs[currentTileIX];
        }


        void InstantiateEvaluatedTile()
        {
            if (evaluatedTileGO != null)
            {
                // DestroyImmediate(evaluatedTileGO);
                evaluatedTileGO.SetActive(false);
            }
            evaluatedTileGO = Instantiate(evaluatedTile.gameObject, evaluatedCell.transform.position, Quaternion.identity);
            HexagonTileCore tileCore = evaluatedTileGO.GetComponent<HexagonTileCore>();
            tileCore.ShowSocketLabels(false);
            tileCore.SetIgnoreSocketLabelUpdates(true);

            // evaluatedTileGO.transform.SetParent(tileFolder);
        }

        void InstantiateCurrentTile()
        {
            if (currentTileGO != null)
            {
                // DestroyImmediate(currentTileGO);
                currentTileGO.SetActive(false);
            }

            currentTileGO = Instantiate(currentTile.gameObject, currentCell.transform.position, Quaternion.identity);
            currentTileGO.gameObject.transform.rotation = Quaternion.Euler(0f, rotationValues[currentRotation], 0f);
            HexagonTileCore tileCore = currentTileGO.GetComponent<HexagonTileCore>();
            tileCore.ShowSocketLabels(false);
            tileCore.SetIgnoreSocketLabelUpdates(true);

            // currentTileGO.transform.SetParent(tileFolder);
        }

        private void DrawCompatibilityVisual()
        {
            if (currentCell == null || evaluatedCell == null || currentTileGO == null || evaluatedTileGO == null) return;

            bool isCompatibile = false;

            if (currentCell.GetGridLayer() != evaluatedCell.GetGridLayer())
            {
                // HexagonTileCompatibilitySide relativeLayer;
                // relativeLayer = (currentLayer == 0) ? HexagonTileCompatibilitySide.Top : HexagonTileCompatibilitySide.Bottom;
                int incomingTileId = currentTile.GetId();
                int placedTileId = evaluatedTile.GetId();

                // if (tileDirectCompatibilityMatrix[incomingTileId, placedTileId].ContainsKey(relativeLayer))
                // {
                isCompatibile = tileDirectCompatibilityMatrix[incomingTileId, placedTileId][currentCompatibilitySide][currentRotation];

                // Debug.Log("DrawCompatibilityVisual");
                // }
            }
            else
            {
                HexagonTileCompatibilitySide placedRelativeSide = (HexagonTileCompatibilitySide)currentSide;
                int incomingTileId = currentTile.GetId();
                int placedTileId = evaluatedTile.GetId();


                if (tileDirectCompatibilityMatrix[incomingTileId, placedTileId].ContainsKey(placedRelativeSide))
                {
                    isCompatibile = tileDirectCompatibilityMatrix[incomingTileId, placedTileId][placedRelativeSide][currentRotation];
                }
            }

            Gizmos.color = isCompatibile ? Color.green : Color.red;
            Gizmos.DrawSphere(currentCell.transform.position, compatibilityVisualSize);
        }

        public static Dictionary<HexagonTileCompatibilitySide, bool[]>[,] ResizeMatrix(Dictionary<HexagonTileCompatibilitySide, bool[]>[,] currentMatrix, int newSize)
        {
            int oldWidth = currentMatrix.GetLength(0);
            int oldHeight = currentMatrix.GetLength(1);

            Dictionary<HexagonTileCompatibilitySide, bool[]>[,] resizedMatrix = new Dictionary<HexagonTileCompatibilitySide, bool[]>[newSize, newSize];

            for (int i = 0; i < newSize; i++)
            {
                for (int j = 0; j < newSize; j++)
                {
                    if (i < oldWidth && j < oldHeight)
                    {
                        resizedMatrix[i, j] = currentMatrix[i, j];
                    }
                    else
                    {
                        resizedMatrix[i, j] = new Dictionary<HexagonTileCompatibilitySide, bool[]>();
                    }
                }
            }

            return resizedMatrix;
        }

    }
}