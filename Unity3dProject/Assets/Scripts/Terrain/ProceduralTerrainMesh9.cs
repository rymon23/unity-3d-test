using System.Collections.Generic;
using System.Collections;
using System;
using UnityEngine;
using UnityEditor;
using System.Linq;
using ProceduralBase;

[ExecuteInEditMode]
public class ProceduralTerrainMesh9 : MonoBehaviour
{
    [SerializeField] private bool enableEditMode = true;
    [SerializeField] private MeshFilter meshFilter;
    [SerializeField] private MeshRenderer meshRenderer;
    [SerializeField] private FastNoiseUnity[] fastNoiseUnity;
    private Transform instantiatedParent;

    public enum NoiseType { Perlin, Simplex, Value }

    public NoiseType noiseType;

    public enum VertexType { Unset = 0, Road = 1, Block = 2, Generic = 3 }

    public struct Vertex
    {
        public Vector3 position;
        public VertexType vertexType;
    }

    [System.Serializable]
    public struct TerrainMaterialType
    {
        public Material material;
        public string name;
        public float minHeight;
        public float maxHeight;
    }
    [SerializeField] private TerrainMaterialType[] materialTypes;

    [Range(8, 256)] public int terrainSize = 64;
    [Range(-32, 72)] public float terrainHeight = 24f;
    [Range(-2f, 0.75f)][SerializeField] private float persistence = 0.45f;
    [Range(-1f, 2.6f)][SerializeField] private float lacunarity = 2f;
    [Range(1f, 128f)][SerializeField] private int octaves = 6;
    public float noiseScale = 32f;

    [SerializeField] private bool enableTerrainFlattenCurve;
    [SerializeField] private AnimationCurve terrainFlattenCurve;

    [Header("Layer 2 Settings")]
    [Range(-1f, 1f)][SerializeField] private float terrainHeight_L2 = 0.3f;
    [Range(-2f, 1f)][SerializeField] private float persistence_L2 = 0.8f;
    [Range(0.001f, 1f)][SerializeField] private float layer1HeightImpactMult = 0.01f;
    [SerializeField] private float noiseBlendFactor = 0.5f;
    [SerializeField] private float noiseBlendPersistance = 1f;


    [Header("Location Marker Settings")]
    [SerializeField] private bool showLocationPoints;
    [Range(0, 24)][SerializeField] private int locationCount = 2;
    [Range(0, 12)][SerializeField] private float minLocationDistance = 2f;
    [SerializeField] private float minVerticePointLevelRadius = 3f;
    [SerializeField] private float maxVerticePointLevelRadius = 9f;
    [SerializeField] private float generatePointBorderXYOffeset = 2f;
    [SerializeField] private float minlocationHeightOffsetMult = 0.25f;
    [SerializeField] private float maxlocationHeightOffsetMult = 0.88f;
    [SerializeField] private float distPercentMult = 0.88f;
    [SerializeField] private Color locationPointColor = Color.red;
    [SerializeField] private Vector3[] locationPoints;
    [SerializeField] private Vector3[] locationPointZones;
    [SerializeField] private Vector3 locationPointZoneCenter;
    [SerializeField] private float locationPointZoneCenterRadius = 9f;
    [SerializeField] private float minLocationZoneCenterYOffset = 1f;
    [SerializeField] private float maxLocationZoneCenterYOffset = 1f;



    [Header("Location Vertice Plot Assignment Settings")]
    [Range(0.001f, 1f)][SerializeField] private float binaryNoiseAssignmentDivider = 0.5f;
    [SerializeField] private bool reformatBaseNoiseValue = false;
    [SerializeField] private bool invertVertexTypeAssignmentOrder = false;


    [Header("Cluster Marker Settings")]
    [SerializeField] private bool enableLocationBlockPlotPoints;
    [SerializeField] private bool enableRoadPlotPoints;
    [SerializeField] private bool resetPlotPoints;
    [Range(0.1f, 48f)][SerializeField] private float clusterPointDistanceMax = 7f;
    [SerializeField] private float clusterSizeCheckDist = 0.6f;
    [SerializeField] private int clusterCount = 0;


    #region Saved State
    float _terrainSize;
    float _terrainHeight;
    float _minLocationDistance;
    float _generatePointYRangeMin;
    float _minlocationHeightOffsetMult;
    float _maxlocationHeightOffsetMult;
    float _generatePointYRangeMax;
    float _generatePointBorderXYOffeset;
    float _clusterPointDistanceMax;
    float _minLocationZoneCenterYOffset;
    float _maxLocationZoneCenterYOffset;

    // FastNoiseState[] fastNoiseStates;
    // struct FastNoiseState
    // {
    //     public int seed;
    //     public float frequency;

    // }
    #endregion

    Mesh mesh;
    Vertex[] vertices;
    List<Vector3> locationLandPlotVertices = new List<Vector3>();
    List<PointCluster> locationLandPlotClusters = new List<PointCluster>();
    List<Vector3> locationRoadVertices = new List<Vector3>();

    private void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();
        fastNoiseUnity = GetComponentsInChildren<FastNoiseUnity>();
    }

    void Start()
    {
        if (!enableEditMode) return;

        Update();
    }

    private float updateTime = 1f;

    private float timer;
    private void Update()
    {
        if (!enableEditMode) return;

        if (!_editorUpdate && timer > 0f)
        {
            timer -= Time.fixedDeltaTime;
            return;
        }

        timer = updateTime;

        if (!debug_editorUpdateTerrainOnce || _editorUpdate)
        {
            _editorUpdate = false;

            if (!meshFilter) meshFilter = GetComponent<MeshFilter>();
            if (!meshRenderer) meshRenderer = GetComponent<MeshRenderer>();
            if (!mesh)
            {
                mesh = new Mesh();
                mesh.name = "Procedural Terrain";
            }

            mesh.Clear();

            // Generate the mesh data
            vertices = GenerateVertices(locationPoints);
            Vector3[] vts = GetVertexPositions(vertices);
            mesh.vertices = vts;
            mesh.triangles = ProceduralTerrainUtility.GenerateTerrainTriangles(terrainSize);
            mesh.uv = ProceduralTerrainUtility.GenerateTerrainUVs(terrainSize, vertices.Length);
            // Recalculate the normals to ensure proper lighting
            mesh.RecalculateNormals();
            // Apply the mesh data to the MeshFilter component
            meshFilter.mesh = mesh;
        }
    }


    void OnValidate()
    {
        if (!enableEditMode) return;

        _editorUpdate = true;

        if (fastNoiseUnity == null || fastNoiseUnity.Length < 2)
        {
            fastNoiseUnity = GetComponentsInChildren<FastNoiseUnity>();
            // fastNoiseStates = new FastNoiseState[fastNoiseUnity.Length];
            // for (int i = 0; i < fastNoiseStates.Length; i++)
            // {
            //     fastNoiseStates[i] = new FastNoiseState();
            // }
        }

        if (locationPoints == null || locationCount != locationPoints.Length ||
            _minLocationDistance != minLocationDistance ||
            _generatePointBorderXYOffeset != generatePointBorderXYOffeset ||
            _minlocationHeightOffsetMult != minlocationHeightOffsetMult ||
            _maxlocationHeightOffsetMult != maxlocationHeightOffsetMult
            )
        {
            _minLocationDistance = minLocationDistance;
            _minlocationHeightOffsetMult = minlocationHeightOffsetMult;
            _maxlocationHeightOffsetMult = maxlocationHeightOffsetMult;

            if (generatePointBorderXYOffeset > terrainSize * 0.6f)
            {
                generatePointBorderXYOffeset = terrainSize * 0.6f;
            }
            if (generatePointBorderXYOffeset < 0)
            {
                generatePointBorderXYOffeset = 0;
            }
            _generatePointBorderXYOffeset = generatePointBorderXYOffeset;

            if (locationCount != locationPoints.Length)
            {
                showLocationPoints = true;
            }

            GenerateLocationPoints();
            GetLocationZonesCenter();
        }


        if (locationCount > 0 &&
            (_minLocationZoneCenterYOffset != minLocationZoneCenterYOffset ||
            _maxLocationZoneCenterYOffset != maxLocationZoneCenterYOffset)
        )
        {
            GetLocationZonesCenter();
        }

        if ((enableLocationBlockPlotPoints || enableRoadPlotPoints) && (resetPlotPoints || _terrainHeight != terrainHeight || _terrainSize != terrainSize || _clusterPointDistanceMax != clusterPointDistanceMax))
        {
            _terrainHeight = terrainHeight;
            _terrainSize = terrainSize;
            _clusterPointDistanceMax = clusterPointDistanceMax;
            locationLandPlotClusters = new List<PointCluster>();

            UpdateCityLandPlotPoints();

            resetPlotPoints = false;

            DestroyAllTiles(true);
        }

        if (generateBlockTiles && !resetPlotPoints)
        {
            generateBlockTiles = false;
            InstantiateTiles();
        }

        if (bSaveMesh)
        {
            bSaveMesh = false;
            SaveMeshAsset(mesh, "New Terrain Mesh");
        }
    }

    int ConvertNoiseToBinary(float noiseValue)
    {
        float finalValue = reformatBaseNoiseValue ? (Mathf.Abs(noiseValue) / 100f) : noiseValue;
        // Debug.Log("noiseValue: " + noiseValue + ", finalValue: " + finalValue);

        // If the noise value is greater than or equal to 0.5, return 1
        if (finalValue >= binaryNoiseAssignmentDivider)
        {
            return invertVertexTypeAssignmentOrder ? 1 : 2;
        }
        // Otherwise, return 0
        else
        {
            return invertVertexTypeAssignmentOrder ? 2 : 1;
        }
    }

    float GetNoiseHeightValue(float x, float z, NoiseType noiseType, FastNoise fastNoise, float persistence)
    {
        // Calculate the height of the current point
        float noiseHeight = 0;
        float frequency = 1;
        float amplitude = 1;
        float sampleX = x / noiseScale * frequency;

        for (int i = 0; i < octaves; i++)
        {
            float sampleY = z / noiseScale * frequency;

            float noiseValue = 0;
            if (noiseType == NoiseType.Perlin)
            {
                noiseValue = Mathf.PerlinNoise(sampleX, sampleY);
            }

            else if (noiseType == NoiseType.Simplex)
            {
                noiseValue = (float)fastNoise.GetNoise(x, z);
            }

            noiseHeight += noiseValue * amplitude;
            amplitude *= persistence;
            frequency *= lacunarity;
        }
        return noiseHeight;
    }
    float GetNoiseHeightValue(float x, float z, NoiseType noiseType, FastNoiseUnity[] fastNoiseUnity, float persistence)
    {
        // Calculate the height of the current point
        float noiseHeight = 0;
        float frequency = 1;
        float amplitude = 1;
        float sampleX = x / noiseScale * frequency;

        for (int i = 0; i < octaves; i++)
        {
            float sampleY = z / noiseScale * frequency;

            float noiseValue = 0;
            if (noiseType == NoiseType.Perlin)
            {
                noiseValue = Mathf.PerlinNoise(sampleX, sampleY);
            }

            else if (noiseType == NoiseType.Simplex)
            {
                if (fastNoiseUnity.Length > 1)
                {
                    noiseValue = ProceduralTerrainUtility.Blend((float)fastNoiseUnity[1].fastNoise.GetNoise(x, z), (float)fastNoiseUnity[2].fastNoise.GetNoise(x, z), noiseBlendFactor);
                }
                else
                {
                    noiseValue = (float)fastNoiseUnity[0].fastNoise.GetNoise(x, z);
                }
            }

            noiseHeight += noiseValue * amplitude;
            amplitude *= persistence;
            frequency *= lacunarity;
        }
        return noiseHeight;
    }

    Vertex[] GenerateVertices(Vector3[] points)
    {
        // Create an array to store the vertex data
        Vertex[] vertices = new Vertex[terrainSize * terrainSize];

        // Iterate through the vertex data and set the height and color of each point
        for (int x = 0; x < terrainSize; x++)
        {
            int lastIX;
            int currentIX = terrainSize;

            for (int y = 0; y < terrainSize; y++)
            {
                lastIX = currentIX;
                currentIX = x + y * terrainSize;

                float noiseHeight = GetNoiseHeightValue(x, y, noiseType, fastNoiseUnity[0].fastNoise, persistence);
                float basePosY = noiseHeight * terrainHeight;

                // Set the height and color of the current point
                vertices[x + y * terrainSize] = new Vertex
                {
                    position = new Vector3(x, basePosY, y),
                };


                if (points != null && points.Length > 0)
                {
                    List<Vector3> nearbyPoints = new List<Vector3>();

                    // Find the nearest point to the current vertex
                    Vector3 nearestPoint = points[0];
                    float nearestDist = float.MaxValue;
                    for (int i = 0; i < points.Length; i++)
                    {
                        float dist = Vector2.Distance(new Vector2(x, y), new Vector2(points[i].x, points[i].z));
                        if (dist < nearestDist)
                        {
                            nearestDist = dist;
                            nearestPoint = points[i];
                        }
                        if (dist < maxVerticePointLevelRadius)
                        {
                            nearbyPoints.Add(points[i]);
                        }
                    }
                    if (nearestDist < minVerticePointLevelRadius)
                    {
                        float distPercent = 1f - (nearestDist / minVerticePointLevelRadius);
                        float layer1HeightImpact = (basePosY) * layer1HeightImpactMult;

                        if (distPercent > distPercentMult) distPercent = distPercentMult;
                        if (distPercent < 0.01f) distPercent = 0.01f;
                        // float distPercent = (nearestDist / minVerticePointLevelRadius);
                        // float distPercent = minVerticePointLevelRadius / nearestDist;

                        float noiseHeightBlend = GetNoiseHeightValue(x, y, NoiseType.Simplex, fastNoiseUnity, noiseBlendPersistance);
                        float noiseHeight_L2 = GetNoiseHeightValue(x, y, NoiseType.Simplex, fastNoiseUnity[1].fastNoise, persistence_L2);
                        int vertexType = (int)ConvertNoiseToBinary(noiseHeight_L2);
                        vertices[x + y * terrainSize].vertexType = (VertexType)vertexType;

                        float modifiedY = nearestPoint.y + layer1HeightImpact + (terrainHeight_L2 * noiseHeight_L2);
                        float diffY = Mathf.Abs(basePosY - (modifiedY));


                        float lastY = vertices[lastIX].position.y;
                        // Debug.Log("distPercent: " + distPercent + ", nearestDist: " + nearestDist + ", diffY: " + diffY);
                        // Debug.Log("currentIX: " + currentIX + ", lastIX: " + lastIX + ", lastY: " + lastY + ", basePosY: " + basePosY);


                        // vertices[x + y * terrainSize].position.y = nearestPoint.y + vertexType * 0.5f;
                        // float layer1HeightImpact = (noiseHeight * terrainHeight) * distPercent; //0.5f;

                        // float modifiedY = (nearestPoint.y + layer1HeightImpact) + terrainHeight_L2 * noiseHeight_L2;
                        // float modifiedY = (nearestPoint.y * distPercent) + layer1HeightImpact + (terrainHeight_L2 * noiseHeight_L2);
                        // vertices[x + y * terrainSize].position.y = (nearestPoint.y + layer1HeightImpact) + terrainHeight_L2 * noiseHeight_L2;
                        // vertices[x + y * terrainSize].position.y += terrainHeight_L2 * noiseHeight_L2;
                        // if (vertices[x + y * terrainSize].position.y < nearestPoint.y)
                        // {
                        //     vertices[x + y * terrainSize].position.y += (diffY * distPercent);
                        // }
                        // else
                        // {

                        //     vertices[x + y * terrainSize].position.y -= (diffY * distPercent);
                        // }
                        // float slopeAngle = maxSlopeAngle * (nearestDist / minVerticePointLevelRadius);
                        // float slope = Mathf.Lerp(basePosY, modifiedY, distPercent);

                        // vertices[x + y * terrainSize].position.y += (diffY * distPercent);

                        if (nearestDist < minVerticePointLevelRadius * 1f)
                        {
                            // vertices[x + y * terrainSize].position.y = modifiedY;
                            float slope = Mathf.Lerp(lastY, modifiedY, distPercent);
                            vertices[x + y * terrainSize].position.y = slope;
                        }
                        else
                        {
                            vertices[x + y * terrainSize].position.y = nearestPoint.y;
                        }

                        List<Vector3> betweenPoints = ProceduralTerrainUtility.GetPointsBetweenPosition(vertices[x + y * terrainSize].position, nearbyPoints);
                        if (betweenPoints.Count > 0)
                        {

                            float dist = Vector2.Distance(new Vector2(x, y), new Vector2(locationPointZoneCenter.x, locationPointZoneCenter.z));
                            if (dist < locationPointZoneCenterRadius)
                            {
                                vertices[x + y * terrainSize].position.y = locationPointZoneCenter.y;
                            }

                            // float slope = Mathf.Lerp(lastY, locationPointZoneCenter.y, distPercent);
                            // vertices[x + y * terrainSize].position.y = slope;

                            // float avgSlopeY = CalculateAverageSlope(vertices[x + y * terrainSize].position, betweenPoints);
                            // vertices[x + y * terrainSize].position.y = avgSlopeY;
                        }

                    }

                    // // If the distance to the nearest point is within the minVerticePointLevelRadius,
                    // // gradually smooth the height of the vertex to match the elevation of the nearest point
                    else //if (nearestDist <= maxVerticePointLevelRadius)
                    {
                        float distanceFromRadius = Mathf.Abs(nearestDist - minVerticePointLevelRadius * 0.9f);
                        float distPercent = 1f - (distanceFromRadius / maxVerticePointLevelRadius);

                        if (distPercent > 0.01f)
                        {

                            float slope = Mathf.Lerp(basePosY, nearestPoint.y, distPercent);
                            vertices[x + y * terrainSize].position.y = slope;
                        }


                    }

                    float dist2 = Vector2.Distance(new Vector2(x, y), new Vector2(locationPointZoneCenter.x, locationPointZoneCenter.z));
                    if (dist2 < locationPointZoneCenterRadius)
                    {
                        vertices[x + y * terrainSize].position.y = locationPointZoneCenter.y;
                    }

                }
                else
                {
                    if (enableTerrainFlattenCurve)
                    {
                        // Use the flatten curve to control the height of the terrain
                        vertices[x + y * terrainSize].position.y = terrainFlattenCurve.Evaluate(noiseHeight) * terrainHeight;
                    }
                }
            }
        }
        return vertices;
    }

    public void GetLocationZonesCenter()
    {
        locationPointZoneCenter = ProceduralTerrainUtility.GetCenterPosition(locationPoints);
        float yPos = UnityEngine.Random.Range(locationPointZoneCenter.y - minLocationZoneCenterYOffset, locationPointZoneCenter.y + maxLocationZoneCenterYOffset);
        locationPointZoneCenter.y = yPos;
    }

    Vector3[] GetVertexPositions(Vertex[] vertices)
    {
        // Create an array to store the vertex positions
        Vector3[] vertexPositions = new Vector3[vertices.Length];

        // Iterate through the vertices and store the position of each vertex
        for (int i = 0; i < vertices.Length; i++)
        {
            vertexPositions[i] = vertices[i].position;
        }

        return vertexPositions;
    }

    void GenerateLocationPoints()
    {
        locationPoints = new Vector3[locationCount];
        for (int i = 0; i < locationCount; i++)
        {
            // Generate a random position within the bounds of the terrain
            float minLocationHeight = terrainHeight * minlocationHeightOffsetMult;
            float maxLocationHeight = terrainHeight * maxlocationHeightOffsetMult;
            locationPoints[i] = ProceduralTerrainUtility.GeneratePointWithinBounds(generatePointBorderXYOffeset, terrainSize - generatePointBorderXYOffeset, minLocationHeight, maxLocationHeight);

            // Use the transform scale to check distance
            Vector3 scale = transform.lossyScale;

            // Ensure that the point is a minimum distance away from all other points
            bool tooClose = false;
            do
            {
                tooClose = false;
                foreach (Vector3 point in locationPoints)
                {
                    if (point != locationPoints[i] && Vector3.Distance(point, locationPoints[i]) < minLocationDistance * scale.x)
                    {
                        tooClose = true;

                        locationPoints[i] = ProceduralTerrainUtility.GeneratePointWithinBounds(generatePointBorderXYOffeset, terrainSize - generatePointBorderXYOffeset, minLocationHeight, maxLocationHeight);
                        break;
                    }
                }
            } while (tooClose);
        }
    }

    void GenerateLocationZonePoints()
    {
        locationPoints = new Vector3[locationCount];
        for (int i = 0; i < locationCount; i++)
        {
            // Generate a random position within the bounds of the terrain
            // float xPos = UnityEngine.Random.Range(generatePointBorderXYOffeset, terrainSize - generatePointBorderXYOffeset);
            // float zPos = UnityEngine.Random.Range(generatePointBorderXYOffeset, terrainSize - generatePointBorderXYOffeset);

            float minLocationHeight = terrainHeight * minlocationHeightOffsetMult;
            float maxLocationHeight = terrainHeight * maxlocationHeightOffsetMult;
            locationPoints[i] = ProceduralTerrainUtility.GeneratePointWithinBounds(generatePointBorderXYOffeset, terrainSize - generatePointBorderXYOffeset, minLocationHeight, maxLocationHeight);


            // Use the transform scale to check distance
            Vector3 scale = transform.lossyScale;

            // Ensure that the point is a minimum distance away from all other points
            bool tooClose = false;
            do
            {
                tooClose = false;
                foreach (Vector3 point in locationPoints)
                {
                    if (point != locationPoints[i] && Vector3.Distance(point, locationPoints[i]) < minLocationDistance * scale.x)
                    {
                        tooClose = true;
                        locationPoints[i] = ProceduralTerrainUtility.GeneratePointWithinBounds(generatePointBorderXYOffeset, terrainSize - generatePointBorderXYOffeset, minLocationHeight, maxLocationHeight);
                        break;
                    }
                }
            } while (tooClose);
        }
    }


    [Header("Debug Settings")]
    [SerializeField] private bool debug_showPoints = true;
    [SerializeField] private bool debug_minLocationDistance;
    [SerializeField] private bool debug_locationPointLevelRadius;
    [SerializeField] private bool debug_editorUpdateTerrainOnce;
    private bool _editorUpdate;
    void OnDrawGizmos()
    {
        if (!debug_showPoints && !debug_locationPointLevelRadius && !debug_minLocationDistance) return;

        if (showLocationPoints && locationPoints != null && locationPoints.Length > 0)
        {
            Vector3 scale = transform.lossyScale;
            // Draw a sphere at each point's position
            foreach (Vector3 point in locationPoints)
            {
                Vector3 pointWorldPos = transform.TransformPoint(point);
                Gizmos.color = locationPointColor;

                if (debug_showPoints)
                {
                    Gizmos.DrawSphere(pointWorldPos, 6f);
                }
                if (debug_minLocationDistance)
                {
                    Gizmos.color = Color.magenta;
                    Gizmos.DrawWireSphere(pointWorldPos, minLocationDistance * scale.x);
                }
                if (debug_locationPointLevelRadius)
                {
                    Gizmos.color = Color.white;
                    Gizmos.DrawWireSphere(pointWorldPos, minVerticePointLevelRadius * scale.x);
                    Gizmos.DrawWireSphere(pointWorldPos, maxVerticePointLevelRadius * scale.x);
                }
            }

            Gizmos.color = Color.black;
            Vector3 worldPos = transform.TransformPoint(locationPointZoneCenter);
            Gizmos.DrawWireSphere(worldPos, locationPointZoneCenterRadius * scale.x);
        }

        // Draw the cluster points
        if (enableLocationBlockPlotPoints && !resetPlotPoints)
        {
            // Debug.Log("cityLandPointClusters[0]: " + cityLandPointClusters[0].GetPoints().Count);

            if (locationLandPlotClusters.Count > 0)
            {
                foreach (PointCluster cluster in locationLandPlotClusters)
                {

                    Gizmos.color = cluster.color;

                    List<Vector3> pts = cluster.GetPoints();
                    foreach (Vector3 point in pts)
                    {
                        Gizmos.DrawSphere(point, 0.25f);
                    }

                    List<Vector3> baseGridPoints = cluster.gridPoints;
                    if (baseGridPoints.Count > 0)
                    {
                        foreach (Vector3 point in baseGridPoints)
                        {
                            // Vector3 pointWorldPos = transform.TransformPoint(point);
                            // Gizmos.DrawWireSphere(point, clusterSizeCheckDist);
                            Gizmos.DrawSphere(point, clusterSizeCheckDist);
                        }
                    }

                    List<PointCluster.GridPointPrototype> gridPoints = cluster.gridPointPrototypes;
                    if (gridPoints.Count > 0)
                    {
                        foreach (PointCluster.GridPointPrototype point in gridPoints)
                        {
                            // Vector3 pointWorldPos = transform.TransformPoint(point);
                            // Gizmos.DrawWireSphere(point, clusterSizeCheckDist);
                            Gizmos.DrawSphere(point.position, point.radius);

                            // if (point.radius > clusterSizeCheckDist)
                            // {
                            //     Gizmos.color = Color.black;
                            //     Gizmos.DrawWireSphere(point.position, point.radius);
                            // }
                        }
                    }
                }


            }
            else if (locationLandPlotVertices.Count > 0)
            {
                foreach (Vector3 point in locationLandPlotVertices)
                {
                    Gizmos.DrawSphere(point, 0.33f);
                }
            }
        }

        if (enableRoadPlotPoints && !resetPlotPoints && locationRoadVertices.Count > 0)
        {
            foreach (Vector3 point in locationRoadVertices)
            {
                Gizmos.color = Color.black;
                Gizmos.DrawSphere(point, 0.33f);
            }
        }
    }




    #region Save Mesh

    [SerializeField] private bool bSaveMesh = false;
    [SerializeField] private Mesh lastSavedMesh;
    void SaveMeshAsset(Mesh mesh, string assetName)
    {
        // Create a new mesh asset
        lastSavedMesh = Instantiate(mesh) as Mesh;
        lastSavedMesh.name = assetName;

        // Save the mesh asset to the project
        AssetDatabase.CreateAsset(lastSavedMesh, "Assets/Terrain/" + assetName + ".asset");
        AssetDatabase.SaveAssets();
    }
    #endregion


    #region Layer 2 Methods

    void CalculatePlacementPoints()
    {
        List<Vector3> landPlotVertices = new List<Vector3>();
        List<Vector3> roadVertices = new List<Vector3>();

        if (vertices != null && vertices.Length > 0)
        {
            // // Create a list to store the placement points
            for (int i = 0; i < vertices.Length; i++)
            {
                if (vertices[i].vertexType == VertexType.Block)
                {
                    Vector3 worldPosition = transform.TransformPoint(vertices[i].position);
                    landPlotVertices.Add(worldPosition);
                }
                else if (vertices[i].vertexType == VertexType.Road)
                {
                    Vector3 worldPosition = transform.TransformPoint(vertices[i].position);
                    roadVertices.Add(worldPosition);
                }
            }
        }

        locationLandPlotVertices = landPlotVertices;
        locationRoadVertices = roadVertices;
    }

    private void RemoveOverlappingPoints(List<PointCluster> clusters)
    {
        // Iterate through all clusters
        for (int i = 0; i < clusters.Count; i++)
        {
            PointCluster currentCluster = clusters[i];
            List<Vector3> currentPoints = currentCluster.GetPoints();

            // Iterate through all other clusters
            for (int j = 0; j < clusters.Count; j++)
            {
                if (i == j) continue; // Skip the current cluster

                PointCluster otherCluster = clusters[j];
                List<Vector3> otherPoints = otherCluster.GetPoints();

                // Remove any points in the current cluster that are also in the other cluster
                currentPoints.RemoveAll(otherPoints.Contains);

                // Update the current cluster with the remaining points
                currentCluster.SetPoints(currentPoints);
            }
        }
    }

    private void UpdateOverlappedVertexType(List<PointCluster> clusters)
    {
        for (int i = 0; i < vertices.Length; i++)
        {
            if (vertices[i].vertexType == VertexType.Road)
            {
                foreach (PointCluster cluster in clusters)
                {
                    if (cluster.bounds.Contains(vertices[i].position))
                    {
                        vertices[i].vertexType = VertexType.Block;
                        cluster.AddPoint(vertices[i].position);
                        break;
                    }
                }
            }
        }
    }

    private List<PointCluster> ConsolidatePointsIntoClusters(List<Vector3> points)
    {
        List<PointCluster> clusters = new List<PointCluster>();
        Vector3 scale = transform.lossyScale;

        points = points.OrderBy(pos => pos.x).ThenBy(pos => pos.z).ToList();

        // Iterate through all of the points
        for (int i = 0; i < points.Count; i++)
        {
            // Check if the current point is within an existing cluster
            bool foundCluster = false;
            foreach (PointCluster cluster in clusters)
            {
                // Calculate the distance between the current point and the center of the cluster
                float distance = Vector2.Distance(new Vector2(cluster.center.x, cluster.center.z), new Vector2(points[i].x, points[i].z));
                if (distance < clusterPointDistanceMax)
                {
                    // Add the point to the cluster and update the center
                    cluster.AddPoint(points[i]);
                    foundCluster = true;
                    break;
                }

                List<Vector3> clusterPoints = cluster.GetPoints();
                foreach (Vector3 clusterPoint in clusterPoints)
                {
                    distance = Vector2.Distance(new Vector2(clusterPoint.x, clusterPoint.z), new Vector2(points[i].x, points[i].z));
                    if (distance < clusterPointDistanceMax)
                    {
                        // Add the point to the cluster and update the center
                        cluster.AddPoint(points[i]);
                        foundCluster = true;
                        break;
                    }
                }
            }

            // If the point was not within an existing cluster, create a new cluster for it
            if (!foundCluster)
            {
                PointCluster newCluster = new PointCluster(points[i]);
                clusters.Add(newCluster);
            }
        }

        RemoveOverlappingPoints(clusters);
        // UpdateOverlappedVertexType(clusters);

        foreach (PointCluster cluster in clusters)
        {
            cluster.UpdateMaximumClusterSize(clusterSizeCheckDist);

            List<Vector3> clusterPoints = cluster.GetPoints();

            Vector2 clusterSize = cluster.maxRectangleSize;
            // Vector2 clusterSize = GetMaximumClusterSize(clusterPoints, clusterSizeCheckDist);
            // Debug.Log("clusterSize: " + clusterSize + ", points: " + clusterPoints.Count);
        }

        return clusters;
    }

    private void UpdateCityLandPlotPoints()
    {
        CalculatePlacementPoints();
        locationLandPlotClusters = ConsolidatePointsIntoClusters(locationLandPlotVertices);

        // Add unique color for each cluster
        if (locationLandPlotClusters.Count > 0)
        {
            clusterCount = locationLandPlotClusters.Count;

            HashSet<Color> generatedColors = new HashSet<Color>();

            foreach (PointCluster cluster in locationLandPlotClusters)
            {
                Color randomColor;

                // Generate a random color until a unique color is found
                do
                {
                    // Generate a random hue value between 0 and 1
                    float hue = UnityEngine.Random.value;

                    // Generate a random saturation value between 0.5 and 1
                    float saturation = UnityEngine.Random.Range(0.5f, 1f);

                    // Generate a random value value between 0.5 and 1
                    float value = UnityEngine.Random.Range(0.5f, 1f);

                    // Convert the HSV values to RGB
                    randomColor = Color.HSVToRGB(hue, saturation, value);
                } while (generatedColors.Contains(randomColor));

                // Add the unique color to the hash set
                generatedColors.Add(randomColor);
                cluster.color = randomColor;
            }
        }

    }

    [Header("Procedural Object Placement")]
    [SerializeField] private bool generateBlockTiles;
    [SerializeField] private float InstantiateOffsetY = 1f;
    [SerializeField] private GameObject proceduralTilePrefab_sm;
    [SerializeField] private GameObject proceduralTilePrefab_md;
    [SerializeField] private List<GameObject> allTiles = new List<GameObject>();

    void DestroyAllTiles(bool immediate = false)
    {
        // Destroy any existing objects in the array
        foreach (GameObject obj in allTiles)
        {
            if (immediate)
            {
                DestroyImmediate(obj);
            }
            else
            {
                Destroy(obj);
            }
        }
        // Clear the array
        allTiles.Clear();
    }

    void InstantiateTiles()
    {
        DestroyAllTiles();

        if (instantiatedParent == null)
        {
            instantiatedParent = Instantiate(new GameObject("LocationObjects"), gameObject.transform).transform;
            // instantiatedParent.transform.SetParent(gameObject.transform);
        }

        foreach (PointCluster cluster in locationLandPlotClusters)
        {
            List<PointCluster.GridPointPrototype> gridPoints = cluster.gridPointPrototypes;
            if (gridPoints.Count > 0)
            {
                foreach (PointCluster.GridPointPrototype point in gridPoints)
                {
                    GameObject prefab;
                    if (point.radius <= 6f)
                    {
                        prefab = proceduralTilePrefab_sm;
                    }
                    else
                    {
                        prefab = proceduralTilePrefab_md;
                    }
                    Vector3 pos = transform.TransformPoint(point.position);
                    Vector3 scaleXZ = transform.lossyScale;
                    Vector3 scaleY = prefab.transform.lossyScale;
                    // GameObject newObject = Instantiate(prefab, new Vector3(pos.x * scaleXZ.x, pos.y * scaleY.y, pos.z * scaleXZ.z), Quaternion.identity);
                    GameObject newObject = Instantiate(prefab, new Vector3(point.position.x, point.position.y + (scaleY.y * 0.5f) - InstantiateOffsetY, point.position.z), Quaternion.identity);

                    allTiles.Add(newObject);
                    newObject.transform.SetParent(instantiatedParent.transform);
                }
            }

        }
    }
    #endregion

    Dictionary<TerrainMaterialType, List<Vector3>> GroupVerticesByMaterial(TerrainMaterialType[] materialTypes, Vector3[] vertices)
    {
        // Create a dictionary to store the vertices
        Dictionary<TerrainMaterialType, List<Vector3>> groupedVertices = new Dictionary<TerrainMaterialType, List<Vector3>>();

        // Loop through the vertices and group them by material type
        for (int i = 0; i < vertices.Length; i++)
        {
            // Calculate the elevation of the vertex
            float elevation = vertices[i].y;

            // Determine which material type the vertex belongs to
            TerrainMaterialType materialType = new TerrainMaterialType();
            for (int j = 0; j < materialTypes.Length; j++)
            {
                if (elevation >= materialTypes[j].minHeight && elevation <= materialTypes[j].maxHeight)
                {
                    materialType = materialTypes[j];
                    break;
                }
            }

            // Add the vertex to the appropriate group
            if (typeof(TerrainMaterialType).IsValueType)
            {
                if (!groupedVertices.ContainsKey(materialType))
                {
                    groupedVertices[materialType] = new List<Vector3>();
                }
                groupedVertices[materialType].Add(vertices[i]);
            }
        }

        return groupedVertices;
    }


    void AssignMaterialsToSubMeshes(Dictionary<TerrainMaterialType, List<Vector3>> groupedVertices)
    {
        // Set the vertices for the mesh
        // List<Vector3> vertices = new List<Vector3>();
        // foreach (var kvp in groupedVertices)
        // {
        //     vertices.AddRange(kvp.Value);
        // }
        // mesh.vertices = vertices.ToArray();

        // // Calculate the triangles for the mesh
        // List<int> triangles = new List<int>();
        // for (int i = 0; i < vertices.Count - 2; i++)
        // {
        //     triangles.Add(i);
        // }
        // mesh.triangles = triangles.ToArray();

        // Divide the mesh into sub-meshes and assign the materials
        int subMeshIndex = 0;
        foreach (var kvp in groupedVertices)
        {
            // Get the vertices for the sub-mesh
            List<Vector3> subMeshVertices = kvp.Value;

            // Calculate the triangles for the sub-mesh
            // List<int> subMeshTriangles = new List<int>();
            // for (int i = 0; i < subMeshVertices.Count - 2; i++)
            // {
            //     subMeshTriangles.Add(i);
            // }

            int[] subMeshTriangles = ProceduralTerrainUtility.CreateTriangles(subMeshVertices);

            Debug.Log("subMeshTriangles: " + subMeshTriangles.Length + ", subMeshVertices: " + subMeshVertices.Count);

            // Assign the triangles to the sub-mesh
            mesh.SetTriangles(subMeshTriangles, subMeshIndex);

            // Assign the material to the sub-mesh
            GetComponent<MeshRenderer>().materials[subMeshIndex] = kvp.Key.material;

            subMeshIndex++;
        }
    }

}

