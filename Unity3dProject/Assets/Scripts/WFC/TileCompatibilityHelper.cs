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
        [SerializeField] private CompatibilityCheck compatibilityCheck = 0;
        public enum CompatibilityCheck { DirectTile = 0, CornerSockets, }
        [SerializeField] private HexagonSocketDirectory socketDirectory;
        [SerializeField] private HexagonCell evaluatedCell;
        [SerializeField] private HexagonTileCore evaluatedTile;
        [SerializeField] private GameObject evaluatedTileGO;
        [SerializeField] private int evaluatedTileIX = 0;

        [SerializeField] private HexagonCell currentCell;
        [SerializeField] private HexagonTileCore currentTile;
        [SerializeField] private GameObject currentTileGO;
        [SerializeField] private int currentTileIX = 0;
        [SerializeField] private int currentRotation;
        [SerializeField] private int currentLayer;
        [SerializeField] private int currentSide;
        [SerializeField] private int currentSideOffet = -1;
        [SerializeField] private HexagonTileCompatibilitySide currentTileNeighborSide;

        [Header("Relative Side Data")]
        [SerializeField] private int currentRotationOffet = -1;
        [SerializeField] private HexagonTileCompatibilitySide rotated_RelativeSideForIncomingTile;
        [SerializeField] private HexagonSide rotated_RelativeSideForEvaluatedTile;

        [Header("Controls")]

        [Header("Evaluated Tile")]

        [Header("Invert")]
        [SerializeField] private bool isInverted;
        [SerializeField] private bool invertEvaluatedTile;

        [Header("Next")]
        [SerializeField] private bool evaluateNextTile;
        [SerializeField] private bool swapEvaluatedAndCurrentTile;

        [Header("Rotate")]
        [SerializeField] private bool rotateEvaluatedTile;
        [SerializeField] private int currentEvaluatedRotation;

        [Header("Accept")]
        [SerializeField] private bool accept;
        [Header("Reject")]
        [SerializeField] private bool reject;

        [Header("Step Controls")]
        [SerializeField] private bool next;
        [SerializeField] private NextState nextStepLevel = NextState.Default;
        [SerializeField] private bool previous;
        // [SerializeField] private NextState previousStepLevel = NextState.Default;
        public enum NextState { Default = 0, NextSide, NextLayer, NextTile }

        [Header("Reset Current Step State")]
        [SerializeField] private bool resetStepState;

        [Header("Restart")]
        [SerializeField] private bool restart;


        [SerializeField] private bool goToNextOnAcceptOrReject;
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
        Dictionary<HexagonTileCompatibilitySide, HashSet<int>>[,] tileDirectCompatibilityMatrix;

        Dictionary<int, HexagonTileCore> tileLookupByid;
        [SerializeField] private List<HexagonTileCore> _tilePrefabs;
        float[] rotationValues = { 0f, 60f, 120f, 180f, 240f, 300f };

        GameObject trashFolder;


        private void OnValidate()
        {
            Transform _trashFolder = transform.Find("TrashFolder");
            if (trashFolder == null) trashFolder = new GameObject("TrashFolder");

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


            if (swapEvaluatedAndCurrentTile)
            {
                swapEvaluatedAndCurrentTile = false;
                SwapEvaluatedAndCurrentTile();
                return;
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
            else if (reject)
            {
                reject = false;
                RejectPairing();
            }


            if (next)
            {
                next = false;
                NextStep(nextStepLevel);
            }
            else if (previous)
            {
                previous = false;
                PreviousStep(nextStepLevel);
            }

            if (rotateEvaluatedTile)
            {
                rotateEvaluatedTile = false;
                RotateEvaluatedTile();
            }

            if (invertEvaluatedTile)
            {
                invertEvaluatedTile = false;

                WFCUtilities.InvertTile(evaluatedTileGO);
                isInverted = !isInverted;
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
            tileLookupByid = tileDirectory.CreateHexTileDictionary();
            _tilePrefabs = tileLookupByid.Select(x => x.Value).ToList();
        }

        #region Accept / Reject Pairing

        private void AcceptPairing()
        {
            UpdatePairCompatibility(true);

            if (goToNextOnAcceptOrReject) NextStep();
        }
        private void RejectPairing()
        {
            UpdatePairCompatibility(false);

            if (goToNextOnAcceptOrReject) NextStep();
        }
        #endregion


        private void InitializeCompatibilityMatrix(bool loadData)
        {
            Dictionary<HexagonTileCompatibilitySide, HashSet<int>>[,] loadedMatrix = tileCompatibilityDirectory.GetCompatibilityMatrix();
            loadData = loadData && loadedMatrix != null;
            int loadDataLength = loadData ? loadedMatrix.GetLength(0) : 0;

            Dictionary<HexagonTileCompatibilitySide, HashSet<int>>[,] matrix = new Dictionary<HexagonTileCompatibilitySide, HashSet<int>>[_tilePrefabs.Count, _tilePrefabs.Count];


            Debug.Log("_tilePrefabs.Count: " + _tilePrefabs.Count + ", loadDataLength: " + loadDataLength);

            foreach (var tile_A in _tilePrefabs)
            {
                int placedTileId = tile_A.GetId();

                foreach (var tile_B in _tilePrefabs)
                {
                    int incomingTileId = tile_B.GetId();

                    matrix[incomingTileId, placedTileId] = new Dictionary<HexagonTileCompatibilitySide, HashSet<int>>();

                    for (int compSide = 0; compSide < 8; compSide++)
                    {
                        matrix[incomingTileId, placedTileId].Add((HexagonTileCompatibilitySide)compSide, new HashSet<int>());

                        if (loadData && incomingTileId < loadDataLength && placedTileId < loadDataLength)
                        {
                            foreach (int item in loadedMatrix[incomingTileId, placedTileId][(HexagonTileCompatibilitySide)compSide])
                            {
                                matrix[incomingTileId, placedTileId][(HexagonTileCompatibilitySide)compSide].Add(item);
                            }

                        }
                    }
                }
            }
            tileDirectCompatibilityMatrix = matrix;
        }


        private void ClearCompatibility()
        {
            InitializeCompatibilityMatrix(false);
        }


        private void SetPairCompatibility(int incomingTileId, int existingTileId, HexagonTileCompatibilitySide existingTileRotatedSide, int rotationOffset, bool isCompatible)
        {
            if (tileDirectCompatibilityMatrix[incomingTileId, existingTileId].ContainsKey(existingTileRotatedSide) == false)
            {
                tileDirectCompatibilityMatrix[incomingTileId, existingTileId]
                        .Add(existingTileRotatedSide, new HashSet<int>());
            }
            if (isCompatible)
            {
                tileDirectCompatibilityMatrix[incomingTileId, existingTileId][existingTileRotatedSide].Add(rotationOffset);
            }
            else
            {
                tileDirectCompatibilityMatrix[incomingTileId, existingTileId][existingTileRotatedSide].Remove(rotationOffset);
            }
        }

        private static HexagonSide GetSymmetricalHexagonSide(HexagonSide side)
        {
            switch (side)
            {
                case (HexagonSide.FrontRight):
                    return HexagonSide.FrontLeft;

                case (HexagonSide.BackRight):
                    return HexagonSide.BackLeft;

                case (HexagonSide.Back):
                    return HexagonSide.Back;

                case (HexagonSide.BackLeft):
                    return HexagonSide.BackRight;

                case (HexagonSide.FrontLeft):
                    return HexagonSide.FrontRight;

                default:
                    return HexagonSide.Front;
            }
        }


        private void UpdatePairCompatibility(bool compatible)
        {
            HexagonTileCompatibilitySide placedRelativeSide = currentTileNeighborSide;
            int incomingTileId = currentTile.GetId();
            int placedTileId = evaluatedTile.GetId();

            HexagonTileCompatibilitySide existingTileRotatedSide =
                    (currentTileNeighborSide < HexagonTileCompatibilitySide.Bottom)
                        ? (HexagonTileCompatibilitySide)rotated_RelativeSideForEvaluatedTile
                            : currentTileNeighborSide;

            SetPairCompatibility(incomingTileId, placedTileId, existingTileRotatedSide, currentRotationOffet, compatible);


            HexagonTileCompatibilitySide incomingTileRotatedSide =
                    (rotated_RelativeSideForIncomingTile < HexagonTileCompatibilitySide.Bottom)
                        ? (HexagonTileCompatibilitySide)rotated_RelativeSideForEvaluatedTile
                            : rotated_RelativeSideForIncomingTile;

            int rotOffset = (currentRotationOffet * 2) % 6;
            SetPairCompatibility(placedTileId, incomingTileId, rotated_RelativeSideForIncomingTile, rotOffset, compatible);

            Debug.Log("Side Pair:  existingTileRotatedSide: " + existingTileRotatedSide + ", currentTileRotatedSide: " + rotated_RelativeSideForIncomingTile);

            // Handle Mirrored Tile Sides 
            // if (currentTileNeighborSide < HexagonTileCompatibilitySide.Bottom)
            // {
            //     MirroredSideState incomingTileMirrorState = currentTile.GetMirroredSideState();
            //     MirroredSideState existingTileMirrorState = evaluatedTile.GetMirroredSideState();

            //     if (incomingTileMirrorState != MirroredSideState.Unset && incomingTileMirrorState == existingTileMirrorState)
            //     {
            //         if (incomingTileMirrorState == MirroredSideState.SymmetricalRightAndLeft)
            //         {
            //             if (existingTileRotatedSide != HexagonTileCompatibilitySide.Front && existingTileRotatedSide != HexagonTileCompatibilitySide.Back)
            //             {

            //                 int existingSymmetricalCounterpart = (int)GetSymmetricalHexagonSide((HexagonSide)existingTileRotatedSide);
            //                 int incomingSymmetricalCounterpart = (int)GetSymmetricalHexagonSide((HexagonSide)rotated_RelativeSideForIncomingTile);

            //                 if (existingSymmetricalCounterpart == incomingSymmetricalCounterpart)
            //                 {
            //                     // matching sides
            //                     SetPairCompatibility(incomingTileId, placedTileId, (HexagonTileCompatibilitySide)existingSymmetricalCounterpart, currentRotationOffet, compatible);
            //                     SetPairCompatibility(placedTileId, incomingTileId, (HexagonTileCompatibilitySide)incomingSymmetricalCounterpart, currentRotationOffet, compatible);
            //                     Debug.Log("SymmetricalRightAndLeft:  Matching Side: " + (HexagonTileCompatibilitySide)existingSymmetricalCounterpart);
            //                 }
            //                 else
            //                 {


            //                     SetPairCompatibility(incomingTileId, placedTileId, (HexagonTileCompatibilitySide)existingSymmetricalCounterpart, rotOffset, compatible);

            //                     rotOffset = (rotOffset * 2) % 6;
            //                     SetPairCompatibility(placedTileId, incomingTileId, (HexagonTileCompatibilitySide)incomingSymmetricalCounterpart, rotOffset, compatible);

            //                     Debug.Log("SymmetricalRightAndLeft:  existingSymmetricalCounterpart: " + (HexagonTileCompatibilitySide)existingSymmetricalCounterpart + ", incomingSymmetricalCounterpart: " + (HexagonTileCompatibilitySide)incomingSymmetricalCounterpart);
            //                 }

            //             }
            //         }
            //     }
            // }

            SaveCompatibilityData();
        }



        #region Save / Load
        private void SaveCompatibilityData()
        {
            if (tileCompatibilityDirectory != null)
            {
                tileCompatibilityDirectory.UpdateTable(tileDirectCompatibilityMatrix);
                Debug.Log("Save Compatibility Data");
            }
        }

        private void LoadCompatibilityData()
        {
            if (tileCompatibilityDirectory != null) tileDirectCompatibilityMatrix = tileCompatibilityDirectory.GetCompatibilityMatrix();
        }

        #endregion



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

            UpdateRotatedSideForIncomingTile();
            UpdateRotatedSideForEvaluatedTile();
        }

        private void PreviousStep(NextState stepLevel = NextState.Default)
        {
            if (stepLevel > NextState.Default || _rotationsComplete)
            {
                if (stepLevel > NextState.NextSide || _sidesComplete)
                {
                    if (stepLevel > NextState.NextLayer || _layersComplete)
                    {
                        PreviousTile();
                    }
                    else
                    {
                        PreviousLayer();
                    }
                }
                else
                {
                    PreviousSide();
                }
            }
            else
            {
                PreviousRotation();
            }
            UpdateRotatedSideForIncomingTile();
            UpdateRotatedSideForEvaluatedTile();
        }


        #region Next
        private void NextTile()
        {
            currentTileIX++;
            currentTile = _tilePrefabs[currentTileIX % _tilePrefabs.Count];

            ResetCurrentNeighborState();
        }

        private void NextRotation()
        {
            currentRotation = (currentRotation + 1) % 6;
            UpdateRotationOffset();

            if (currentTileGO != null) currentTileGO.gameObject.transform.rotation = Quaternion.Euler(0f, rotationValues[currentRotation], 0f);
            _rotationsComplete = (currentRotation == 5);
        }
        private void NextSide()
        {
            currentSide = (currentSide + 1) % 6;
            currentTileNeighborSide = (HexagonTileCompatibilitySide)currentSide;
            UpdateSideOffset();

            _sidesComplete = (currentSide == 5);
            currentLayer = _sidesComplete ? 1 : 0;

            UpdateCurrentCell(false);
        }
        private void NextLayer()
        {
            currentLayer = (currentLayer + 1) % 2;
            currentTileNeighborSide = currentLayer == 0 ? HexagonTileCompatibilitySide.Bottom : HexagonTileCompatibilitySide.Top;
            _layersComplete = (currentLayer == 1);

            UpdateCurrentCell(true);
        }
        #endregion


        #region Previous
        private void PreviousTile()
        {
            currentTileIX--;
            currentTile = _tilePrefabs[currentTileIX % _tilePrefabs.Count];

            ResetCurrentNeighborState();
        }

        private void PreviousRotation()
        {
            currentRotation = Mathf.Abs((currentRotation - 1) % 6);
            UpdateRotationOffset();

            if (currentTileGO != null) currentTileGO.gameObject.transform.rotation = Quaternion.Euler(0f, rotationValues[currentRotation], 0f);
            _rotationsComplete = (currentRotation == 5);
        }
        private void PreviousSide()
        {
            currentSide = Mathf.Abs((currentSide - 1) % 6);
            currentTileNeighborSide = (HexagonTileCompatibilitySide)currentSide;
            UpdateSideOffset();

            _sidesComplete = (currentSide == 5);
            currentLayer = _sidesComplete ? 1 : 0;

            UpdateCurrentCell(false);
        }
        private void PreviousLayer()
        {
            currentLayer = Mathf.Abs((currentLayer - 1) % 2);
            currentTileNeighborSide = currentLayer == 0 ? HexagonTileCompatibilitySide.Bottom : HexagonTileCompatibilitySide.Top;
            _layersComplete = (currentLayer == 1);

            UpdateCurrentCell(true);
        }
        #endregion



        private void UpdateCurrentCell(bool isLayered)
        {
            currentCell = isLayered ? evaluatedCell.layeredNeighbor[currentLayer] : evaluatedCell.neighborsBySide[currentSide];

            if (isLayered)
            {
                rotated_RelativeSideForIncomingTile = currentLayer == 0 ? HexagonTileCompatibilitySide.Top : HexagonTileCompatibilitySide.Bottom;
            }
            else
            {
                UpdateRotatedSideForIncomingTile();
            }

            UpdateSideOffset();

            ResetRotation();

            UpdateCurrentTile();
        }

        private void UpdateCurrentTile()
        {
            InstantiateCurrentTile();
        }

        private void UpdateEvaluatedTile()
        {
            InstantiateEvaluatedTile();
        }

        private void EvaluatedNextTile()
        {
            evaluatedTileIX++;
            evaluatedTile = _tilePrefabs[evaluatedTileIX % _tilePrefabs.Count];

            RotateEvaluatedTile(true);

            ResetCurrentNeighborState();

            InstantiateEvaluatedTile();

            UpdateCurrentTile();
        }
        private void SwapEvaluatedAndCurrentTile()
        {
            int tempIX = evaluatedTileIX;

            evaluatedTileIX = currentTileIX;
            evaluatedTile = _tilePrefabs[evaluatedTileIX % _tilePrefabs.Count];

            currentTileIX = tempIX;
            currentTile = _tilePrefabs[currentTileIX % _tilePrefabs.Count];

            ResetCurrentNeighborState();

            InstantiateEvaluatedTile();

            UpdateCurrentTile();
        }

        private void UpdateRotationOffset()
        {
            currentRotationOffet = tileCompatibilityDirectory.GetRotationOffset(currentEvaluatedRotation, currentRotation);
            UpdateRotatedSideForEvaluatedTile();
            UpdateRotatedSideForIncomingTile();
        }

        private void UpdateSideOffset()
        {
            if (currentTileNeighborSide < HexagonTileCompatibilitySide.Bottom)
            {
                currentSideOffet = tileCompatibilityDirectory.GetSideOffset((HexagonSide)currentTileNeighborSide, (HexagonSide)rotated_RelativeSideForIncomingTile);
            }
            UpdateRotatedSideForEvaluatedTile();
            UpdateRotatedSideForIncomingTile();
        }


        private void UpdateRotatedSideForEvaluatedTile()
        {
            if (currentTileNeighborSide < HexagonTileCompatibilitySide.Bottom)
            {
                rotated_RelativeSideForEvaluatedTile = (HexagonSide)(((int)currentTileNeighborSide + currentEvaluatedRotation) % 6);
            }
        }
        private void UpdateRotatedSideForIncomingTile()
        {
            if (currentTileNeighborSide < HexagonTileCompatibilitySide.Bottom)
            {
                rotated_RelativeSideForIncomingTile = (HexagonTileCompatibilitySide)(int)((evaluatedCell.GetNeighborsRelativeSide((HexagonSide)currentSide) + currentRotation) % 6);
            }
        }

        private void RotateEvaluatedTile(bool reset = false)
        {
            if (reset)
            {
                currentEvaluatedRotation = 0;
            }
            else
            {
                currentEvaluatedRotation = (currentEvaluatedRotation + 1) % 6;
            }
            UpdateRotationOffset();

            evaluatedTileGO.gameObject.transform.rotation = Quaternion.Euler(0f, rotationValues[currentEvaluatedRotation], 0f);
        }


        #region Reset
        private void ResetLayer()
        {
            currentLayer = 0;
            currentTileNeighborSide = HexagonTileCompatibilitySide.Bottom;
            _layersComplete = false;
        }

        private void ResetSide()
        {
            currentSide = 0;
            currentSideOffet = 0;
            currentTileNeighborSide = 0;
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
            currentEvaluatedRotation = 0;
            evaluatedTile = _tilePrefabs[evaluatedTileIX];

            currentTileIX = 0;
            currentTile = _tilePrefabs[currentTileIX];
        }
        #endregion



        #region Instantiate Tiles
        void InstantiateEvaluatedTile()
        {
            if (evaluatedTileGO != null)
            {
                // DestroyImmediate(evaluatedTileGO);
                evaluatedTileGO.transform.SetParent(trashFolder.transform);
                evaluatedTileGO.SetActive(false);
            }
            evaluatedTileGO = Instantiate(evaluatedTile.gameObject, evaluatedCell.transform.position, Quaternion.identity);
            evaluatedTileGO.gameObject.transform.rotation = Quaternion.Euler(0f, rotationValues[currentEvaluatedRotation], 0f);
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
                currentTileGO.transform.SetParent(trashFolder.transform);
                currentTileGO.SetActive(false);
            }

            currentTileGO = Instantiate(currentTile.gameObject, currentCell.transform.position, Quaternion.identity);
            currentTileGO.gameObject.transform.rotation = Quaternion.Euler(0f, rotationValues[currentRotation], 0f);
            HexagonTileCore tileCore = currentTileGO.GetComponent<HexagonTileCore>();
            tileCore.ShowSocketLabels(false);
            tileCore.SetIgnoreSocketLabelUpdates(true);

            // currentTileGO.transform.SetParent(tileFolder);
        }

        #endregion

        private void DrawCompatibilityVisual()
        {
            if (currentCell == null || evaluatedCell == null || currentTileGO == null || evaluatedTileGO == null) return;

            bool isCompatibile = false;


            if (compatibilityCheck == CompatibilityCheck.DirectTile)
            {

                if (currentCell.GetGridLayer() != evaluatedCell.GetGridLayer())
                {
                    // HexagonTileCompatibilitySide relativeLayer;
                    // relativeLayer = (currentLayer == 0) ? HexagonTileCompatibilitySide.Top : HexagonTileCompatibilitySide.Bottom;
                    int incomingTileId = currentTile.GetId();
                    int placedTileId = evaluatedTile.GetId();

                    // if (tileDirectCompatibilityMatrix[incomingTileId, placedTileId].ContainsKey(relativeLayer))
                    // {
                    // isCompatibile = tileDirectCompatibilityMatrix[incomingTileId, placedTileId][currentTileNeighborSide][currentRotation];
                    HexagonTileCompatibilitySide currentTileRotatedSide = tileCompatibilityDirectory.GetRotatedTargetSide(rotated_RelativeSideForIncomingTile, currentRotation);

                    isCompatibile = tileCompatibilityDirectory.AreTilesCombatible(incomingTileId, placedTileId, currentTileNeighborSide, currentRotation, currentEvaluatedRotation);
                    // isCompatibile = tileCompatibilityDirectory.AreTilesCombatible(incomingTileId, placedTileId, currentTileRotatedSide, currentRotation, currentEvaluatedRotation);
                }
                else
                {
                    HexagonTileCompatibilitySide placedRelativeSide = (HexagonTileCompatibilitySide)currentSide;
                    int incomingTileId = currentTile.GetId();
                    int placedTileId = evaluatedTile.GetId();


                    // if (tileDirectCompatibilityMatrix[incomingTileId, placedTileId].ContainsKey(placedRelativeSide))
                    // {
                    // isCompatibile = tileDirectCompatibilityMatrix[incomingTileId, placedTileId][placedRelativeSide][currentRotation];

                    isCompatibile = tileCompatibilityDirectory.AreTilesCombatible(incomingTileId, placedTileId, placedRelativeSide, currentRotation, currentEvaluatedRotation);
                }
            }
            else
            {
                if (socketDirectory != null)
                {
                    // Debug.Log("socketDirectory - 0");

                    if (currentCell.GetGridLayer() != evaluatedCell.GetGridLayer())
                    {
                        if (currentTileNeighborSide == HexagonTileCompatibilitySide.Bottom)
                        {
                            isCompatibile = WFCUtilities.IsTileCompatibleOnLayerAndRotation(evaluatedCell, evaluatedTile, currentEvaluatedRotation, currentTile, currentRotation, socketDirectory);
                        }
                        else
                        {
                            isCompatibile = WFCUtilities.IsTileCompatibleOnLayerAndRotation(currentCell, currentTile, currentRotation, evaluatedTile, currentEvaluatedRotation, socketDirectory);
                        }

                    }
                    else
                    {
                        isCompatibile = WFCUtilities.IsTileCompatibleOnSideAndRotation(currentCell, currentTile, (int)rotated_RelativeSideForIncomingTile, evaluatedTile, currentSide, socketDirectory);
                    }
                }
            }

            Gizmos.color = isCompatibile ? Color.green : Color.red;
            Gizmos.DrawSphere(currentCell.transform.position, compatibilityVisualSize);
        }
    }
}