using UnityEngine;

public class GridGenerator : MonoBehaviour
{
    public GameObject blockRef;
    [SerializeField] private int worldSizeX = 15;
    [SerializeField] private int worldSizeZ = 15;
    [SerializeField] private int gridOffset = 1;

    void Start()
    {
        Generate();
    }

    private void Generate() {
        for (int x = 0; x < worldSizeX; x++)
        {
            for (int z = 0; z < worldSizeZ; z++)
            {
                Vector3 pos = new Vector3(x * gridOffset, 0, z * gridOffset);
                GameObject currentBlock = Instantiate(blockRef, pos, Quaternion.identity) as GameObject;

                blockRef.transform.SetParent(this.transform);
            }
        }
    } 

}
