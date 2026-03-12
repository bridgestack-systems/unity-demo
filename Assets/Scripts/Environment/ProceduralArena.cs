using System.Collections.Generic;
using UnityEngine;

namespace NexusArena.Environment
{
    public class ProceduralArena : MonoBehaviour
    {
        [Header("Hex Grid")]
        [SerializeField] private int hexRings = 8;
        [SerializeField] private float tileSize = 1.2f;
        [SerializeField] private float tileGap = 0.08f;
        [SerializeField] private Material arenaMaterial;

        [Header("Edge Glow")]
        [SerializeField] private bool enableEdgeGlow = true;
        [SerializeField] private Color edgeGlowColor = new(0f, 0.9f, 1f, 1f);
        [SerializeField] private float edgeEmissionIntensity = 4f;

        [Header("Height Variation")]
        [SerializeField] private bool randomHeightVariation = true;
        [SerializeField] private float maxHeightOffset = 0.15f;

        [Header("Walls")]
        [SerializeField] private float wallHeight = 3f;
        [SerializeField] private float wallThickness = 0.3f;
        [SerializeField] private Material wallMaterial;

        [Header("Spawn Points")]
        [SerializeField] private GameObject spawnPointMarkerPrefab;

        private readonly List<GameObject> _tiles = new();
        private readonly List<Vector3> _spawnPositions = new();

        private static readonly Vector2[] HexDirections =
        {
            new(Mathf.Sqrt(3f), 0f),
            new(Mathf.Sqrt(3f) / 2f, 1.5f),
            new(-Mathf.Sqrt(3f) / 2f, 1.5f),
            new(-Mathf.Sqrt(3f), 0f),
            new(-Mathf.Sqrt(3f) / 2f, -1.5f),
            new(Mathf.Sqrt(3f) / 2f, -1.5f)
        };

        private void Start()
        {
            GenerateArena();
        }

        public void GenerateArena()
        {
            ClearArena();
            GenerateHexGrid();
            GenerateWalls();
            GenerateSpawnPoints();
        }

        public void ClearArena()
        {
            foreach (var tile in _tiles)
            {
                if (tile != null) Destroy(tile);
            }
            _tiles.Clear();
            _spawnPositions.Clear();
        }

        private void GenerateHexGrid()
        {
            float spacing = (tileSize + tileGap) * 2f;

            for (int q = -hexRings; q <= hexRings; q++)
            {
                int r1 = Mathf.Max(-hexRings, -q - hexRings);
                int r2 = Mathf.Min(hexRings, -q + hexRings);

                for (int r = r1; r <= r2; r++)
                {
                    Vector3 worldPos = HexToWorld(q, r, spacing);
                    int ring = HexDistance(q, r);
                    bool isEdge = ring == hexRings;

                    if (randomHeightVariation && !isEdge)
                    {
                        worldPos.y += Random.Range(-maxHeightOffset, maxHeightOffset);
                    }

                    var tile = CreateHexTile(worldPos, isEdge);
                    tile.transform.SetParent(transform);
                    _tiles.Add(tile);
                }
            }
        }

        private Vector3 HexToWorld(int q, int r, float spacing)
        {
            float x = spacing * (Mathf.Sqrt(3f) * q + Mathf.Sqrt(3f) / 2f * r) / 2f;
            float z = spacing * (3f / 2f * r) / 2f;
            return new Vector3(x, 0f, z);
        }

        private int HexDistance(int q, int r)
        {
            return (Mathf.Abs(q) + Mathf.Abs(q + r) + Mathf.Abs(r)) / 2;
        }

        private GameObject CreateHexTile(Vector3 position, bool isEdge)
        {
            var tile = new GameObject("HexTile");
            tile.transform.position = position;

            var meshFilter = tile.AddComponent<MeshFilter>();
            meshFilter.mesh = CreateHexMesh();

            var meshRenderer = tile.AddComponent<MeshRenderer>();
            if (isEdge && enableEdgeGlow)
            {
                var edgeMat = arenaMaterial != null ? new Material(arenaMaterial) : new Material(Shader.Find("Universal Render Pipeline/Lit"));
                edgeMat.EnableKeyword("_EMISSION");
                edgeMat.SetColor("_EmissionColor", edgeGlowColor * edgeEmissionIntensity);
                edgeMat.SetColor("_BaseColor", edgeGlowColor * 0.5f);
                meshRenderer.material = edgeMat;
            }
            else
            {
                meshRenderer.material = arenaMaterial != null ? arenaMaterial : new Material(Shader.Find("Universal Render Pipeline/Lit"));
            }

            var meshCollider = tile.AddComponent<MeshCollider>();
            meshCollider.sharedMesh = meshFilter.mesh;

            return tile;
        }

        private Mesh CreateHexMesh()
        {
            var mesh = new Mesh { name = "HexTile" };
            var vertices = new Vector3[7];
            var uvs = new Vector2[7];

            vertices[0] = Vector3.zero;
            uvs[0] = new Vector2(0.5f, 0.5f);

            for (int i = 0; i < 6; i++)
            {
                float angle = 60f * i - 30f;
                float rad = angle * Mathf.Deg2Rad;
                vertices[i + 1] = new Vector3(Mathf.Cos(rad) * tileSize, 0f, Mathf.Sin(rad) * tileSize);
                uvs[i + 1] = new Vector2(
                    0.5f + Mathf.Cos(rad) * 0.5f,
                    0.5f + Mathf.Sin(rad) * 0.5f
                );
            }

            var triangles = new int[18];
            for (int i = 0; i < 6; i++)
            {
                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = i + 1;
                triangles[i * 3 + 2] = (i + 1) % 6 + 1;
            }

            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.uv = uvs;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            return mesh;
        }

        private void GenerateWalls()
        {
            float arenaRadius = hexRings * tileSize * 2f + tileSize;
            int wallSegments = 6;

            for (int i = 0; i < wallSegments; i++)
            {
                float angle = 60f * i;
                float nextAngle = 60f * (i + 1);
                float midAngle = (angle + nextAngle) * 0.5f * Mathf.Deg2Rad;
                float rad = angle * Mathf.Deg2Rad;
                float nextRad = nextAngle * Mathf.Deg2Rad;

                Vector3 start = new Vector3(Mathf.Cos(rad), 0f, Mathf.Sin(rad)) * arenaRadius;
                Vector3 end = new Vector3(Mathf.Cos(nextRad), 0f, Mathf.Sin(nextRad)) * arenaRadius;
                float segmentLength = Vector3.Distance(start, end);

                var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                wall.name = $"Wall_{i}";
                wall.transform.SetParent(transform);

                Vector3 midPoint = (start + end) / 2f;
                midPoint.y = wallHeight / 2f;
                wall.transform.position = midPoint;
                wall.transform.localScale = new Vector3(segmentLength, wallHeight, wallThickness);
                wall.transform.LookAt(new Vector3(0f, midPoint.y, 0f));

                if (wallMaterial != null)
                    wall.GetComponent<Renderer>().material = wallMaterial;
            }
        }

        private void GenerateSpawnPoints()
        {
            float spawnRadius = hexRings * tileSize * 1.2f;
            Vector3[] cardinals = { Vector3.forward, Vector3.back, Vector3.right, Vector3.left };

            foreach (var dir in cardinals)
            {
                Vector3 pos = dir * spawnRadius;
                pos.y = 0.1f;
                _spawnPositions.Add(pos);

                if (spawnPointMarkerPrefab != null)
                {
                    var marker = Instantiate(spawnPointMarkerPrefab, pos, Quaternion.identity, transform);
                    marker.name = $"SpawnPoint_{dir}";
                }
                else
                {
                    var marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    marker.name = $"SpawnPoint_{dir}";
                    marker.transform.SetParent(transform);
                    marker.transform.position = pos;
                    marker.transform.localScale = new Vector3(0.5f, 0.02f, 0.5f);
                    var col = marker.GetComponent<Collider>();
                    if (col != null) col.enabled = false;

                    var renderer = marker.GetComponent<Renderer>();
                    var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", new Color(0f, 0.9f, 1f) * 3f);
                    mat.SetColor("_BaseColor", new Color(0f, 0.9f, 1f));
                    renderer.material = mat;
                }
            }
        }

        public IReadOnlyList<Vector3> GetSpawnPositions() => _spawnPositions;
    }
}
