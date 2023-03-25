using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WFCSystem
{
    public class TileBatchGenerator : MonoBehaviour
    {
        [SerializeField] private GameObject[] modelPrefabs;
        [SerializeField] private HexagonTileCore _emptyTileTemplate;

        [SerializeField] private string namePrefix = "TileX4__";
        [SerializeField] private bool generate;

        private void OnValidate()
        {
            if (generate)
            {
                generate = false;

                GenerateTiles();
            }
        }

        private void ValidateSetup()
        {
            transform.position = Vector3.zero;

            if (_emptyTileTemplate == null)
            {
                Debug.LogError("No tile template filled!");
                return;
            }
            if (modelPrefabs == null || modelPrefabs.Length == 0)
            {
                Debug.LogError("No models filled!");
                return;
            }
        }



        private void GenerateTiles()
        {
            ValidateSetup();

            Debug.Log("Generating new tiles!");

            for (var i = 0; i < modelPrefabs.Length; i++)
            {
                GameObject currentModel = modelPrefabs[i];
                GenerateTile(currentModel);
            }
        }

        private void GenerateTile(GameObject modelPrefab)
        {
            GameObject tileGO = GameObject.Instantiate(_emptyTileTemplate.gameObject, transform.position, Quaternion.identity);
            GameObject modelGO = GameObject.Instantiate(modelPrefab.gameObject, transform.position, Quaternion.identity);

            HexagonTileCore tileCore = tileGO.GetComponent<HexagonTileCore>();

            // Make model a child of tile
            modelGO.transform.SetParent(tileGO.transform);
            modelGO.transform.localPosition = Vector3.zero;

            //  Assign model to tile model field 
            tileCore.SetModel(modelGO);

            tileGO.gameObject.name = namePrefix + modelPrefab.gameObject.name;
        }
    }
}