using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

public class MeshDemolisherTool : EditorWindow
{
    private GameObject targetObject;
    private Vector3Int divisions = new Vector3Int(2, 2, 2);
    private bool randomizeDivisions = false;
    private float randomOffset = 0.1f;
    private bool addColliders = true;
    private bool addRigidbodies = false;
    private bool applyMaterialVariation = false;
    private Material[] variationMaterials;
    private bool saveAsPrefabs = false;
    private bool previewMode = false;

    // Advanced fracture options
    private enum FracturePattern { Cubic, Voronoi, Radial, Sliced }
    private FracturePattern fracturePattern = FracturePattern.Cubic;
    private Vector3 fractureOrigin = Vector3.zero;
    private float noiseScale = 0.3f;
    private int voronoiSeed = 12345;
    private int voronoiPoints = 10;
    private float edgeVariation = 0.05f;
    private bool insideFracture = true;
    private bool createCaps = true;

    [MenuItem("Tools/Mesh Demolisher")]
    public static void ShowWindow()
    {
        GetWindow<MeshDemolisherTool>("Mesh Demolisher");
    }

    private void OnGUI()
    {
        GUILayout.Label("Mesh Demolisher Tool", EditorStyles.boldLabel);

        targetObject = (GameObject)EditorGUILayout.ObjectField("Target Object", targetObject, typeof(GameObject), true);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Fracture Settings", EditorStyles.boldLabel);
        fracturePattern = (FracturePattern)EditorGUILayout.EnumPopup("Fracture Pattern", fracturePattern);

        // Display relevant options based on fracture pattern
        switch (fracturePattern)
        {
            case FracturePattern.Cubic:
                divisions = EditorGUILayout.Vector3IntField("Divisions (X, Y, Z)", divisions);
                divisions = new Vector3Int(Mathf.Max(1, divisions.x), Mathf.Max(1, divisions.y), Mathf.Max(1, divisions.z));
                randomizeDivisions = EditorGUILayout.Toggle("Randomize Divisions", randomizeDivisions);
                if (randomizeDivisions)
                {
                    randomOffset = EditorGUILayout.Slider("Random Offset", randomOffset, 0f, 0.5f);
                    noiseScale = EditorGUILayout.Slider("Noise Scale", noiseScale, 0f, 1f);
                }
                break;

            case FracturePattern.Voronoi:
                voronoiPoints = EditorGUILayout.IntSlider("Point Count", voronoiPoints, 3, 30);
                voronoiSeed = EditorGUILayout.IntField("Random Seed", voronoiSeed);
                edgeVariation = EditorGUILayout.Slider("Edge Variation", edgeVariation, 0f, 0.2f);
                if (GUILayout.Button("Randomize Seed"))
                {
                    voronoiSeed = Random.Range(0, 99999);
                    Repaint();
                }
                break;

            case FracturePattern.Radial:
                divisions.x = EditorGUILayout.IntSlider("Radial Segments", divisions.x, 2, 16);
                divisions.y = EditorGUILayout.IntSlider("Height Segments", divisions.y, 1, 10);
                divisions.z = EditorGUILayout.IntSlider("Concentric Rings", divisions.z, 1, 10);
                fractureOrigin = EditorGUILayout.Vector3Field("Origin Point", fractureOrigin);
                break;

            case FracturePattern.Sliced:
                divisions.x = EditorGUILayout.IntSlider("Slice Count", divisions.x, 1, 20);
                randomizeDivisions = EditorGUILayout.Toggle("Random Slice Rotation", randomizeDivisions);
                break;
        }

        EditorGUILayout.Space();
        createCaps = EditorGUILayout.Toggle("Create Interior Caps", createCaps);
        insideFracture = EditorGUILayout.Toggle("Inside-Out Fracture", insideFracture);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Fragment Settings", EditorStyles.boldLabel);
        addColliders = EditorGUILayout.Toggle("Add Colliders", addColliders);
        addRigidbodies = EditorGUILayout.Toggle("Add Rigidbodies", addRigidbodies);
        applyMaterialVariation = EditorGUILayout.Toggle("Apply Material Variation", applyMaterialVariation);
        if (applyMaterialVariation)
        {
            SerializedObject serializedObject = new SerializedObject(this);
            SerializedProperty materialsProperty = serializedObject.FindProperty("variationMaterials");
            EditorGUILayout.PropertyField(materialsProperty, true);
            serializedObject.ApplyModifiedProperties();
        }

        saveAsPrefabs = EditorGUILayout.Toggle("Save as Prefabs", saveAsPrefabs);
        previewMode = EditorGUILayout.Toggle("Preview Mode", previewMode);

        if (GUILayout.Button("Demolish Mesh"))
        {
            if (ValidateInput())
            {
                DemolishMesh();
            }
        }
    }

    private bool ValidateInput()
    {
        if (targetObject == null)
        {
            EditorUtility.DisplayDialog("Error", "Please select a target GameObject.", "OK");
            return false;
        }
        MeshFilter meshFilter = targetObject.GetComponent<MeshFilter>();
        if (meshFilter == null || meshFilter.sharedMesh == null)
        {
            EditorUtility.DisplayDialog("Error", "Target GameObject must have a valid MeshFilter with a mesh.", "OK");
            return false;
        }
        MeshRenderer meshRenderer = targetObject.GetComponent<MeshRenderer>();
        if (meshRenderer == null)
        {
            EditorUtility.DisplayDialog("Error", "Target GameObject must have a MeshRenderer.", "OK");
            return false;
        }
        if (!meshFilter.sharedMesh.isReadable)
        {
            EditorUtility.DisplayDialog("Error", "The mesh is not readable. Please enable 'Read/Write' in the model import settings.", "OK");
            return false;
        }
        return true;
    }

    private void DemolishMesh()
    {
        MeshFilter meshFilter = targetObject.GetComponent<MeshFilter>();
        Mesh originalMesh = meshFilter.sharedMesh;
        Bounds originalBounds = originalMesh.bounds;
        Vector3 originalScale = targetObject.transform.localScale;

        Undo.RegisterCompleteObjectUndo(targetObject, "Demolish Mesh");

        List<GameObject> fragments = new List<GameObject>();

        // Apply different fracture patterns based on selection
        switch (fracturePattern)
        {
            case FracturePattern.Cubic:
                fragments = CubicFracture(originalMesh, originalBounds, originalScale);
                break;

            case FracturePattern.Voronoi:
                fragments = VoronoiFracture(originalMesh, originalBounds, originalScale);
                break;

            case FracturePattern.Radial:
                fragments = RadialFracture(originalMesh, originalBounds, originalScale);
                break;

            case FracturePattern.Sliced:
                fragments = SlicedFracture(originalMesh, originalBounds, originalScale);
                break;
        }

        if (fragments.Count == 0)
        {
            EditorUtility.DisplayDialog("Warning", "No fragments were generated. Try adjusting the fracture settings.", "OK");
            Debug.LogWarning("No fragments generated. Check fracture settings or mesh bounds.");
            return;
        }

        if (saveAsPrefabs)
        {
            SaveFragmentsAsPrefabs(fragments);
        }

        if (!previewMode)
        {
            Undo.DestroyObjectImmediate(targetObject);
        }
        else
        {
            Debug.Log($"Generated {fragments.Count} fragments in preview mode. Original object preserved.");
        }

        SceneView.RepaintAll();
    }

    private List<GameObject> CubicFracture(Mesh originalMesh, Bounds originalBounds, Vector3 originalScale)
    {
        List<GameObject> fragments = new List<GameObject>();
        List<float> cutPointsX = GenerateCutPoints(originalBounds.min.x, originalBounds.max.x, divisions.x, randomOffset, randomizeDivisions);
        List<float> cutPointsY = GenerateCutPoints(originalBounds.min.y, originalBounds.max.y, divisions.y, randomOffset, randomizeDivisions);
        List<float> cutPointsZ = GenerateCutPoints(originalBounds.min.z, originalBounds.max.z, divisions.z, randomOffset, randomizeDivisions);

        for (int x = 0; x < cutPointsX.Count - 1; x++)
        {
            for (int y = 0; y < cutPointsY.Count - 1; y++)
            {
                for (int z = 0; z < cutPointsZ.Count - 1; z++)
                {
                    Bounds fragmentBounds = new Bounds();
                    fragmentBounds.SetMinMax(
                        new Vector3(cutPointsX[x], cutPointsY[y], cutPointsZ[z]),
                        new Vector3(cutPointsX[x + 1], cutPointsY[y + 1], cutPointsZ[z + 1])
                    );

                    if (randomizeDivisions && noiseScale > 0)
                    {
                        fragmentBounds = ApplyPerlinNoiseToFragmentBounds(fragmentBounds, x, y, z);
                    }

                    Mesh fragmentMesh = SliceMesh(originalMesh, fragmentBounds, originalBounds);
                    if (fragmentMesh != null && fragmentMesh.vertexCount > 0)
                    {
                        GameObject fragment = CreateFragmentObject(fragmentMesh, fragmentBounds, x, y, z, originalScale);
                        fragments.Add(fragment);
                    }
                }
            }
        }

        return fragments;
    }

    private List<GameObject> VoronoiFracture(Mesh originalMesh, Bounds originalBounds, Vector3 originalScale)
    {
        List<GameObject> fragments = new List<GameObject>();
        Random.InitState(voronoiSeed);

        // Generate Voronoi cell centers
        List<Vector3> cellCenters = new List<Vector3>();
        for (int i = 0; i < voronoiPoints; i++)
        {
            Vector3 center = new Vector3(
                Random.Range(originalBounds.min.x, originalBounds.max.x),
                Random.Range(originalBounds.min.y, originalBounds.max.y),
                Random.Range(originalBounds.min.z, originalBounds.max.z)
            );
            cellCenters.Add(center);
        }

        // Create fragments for each cell
        for (int i = 0; i < cellCenters.Count; i++)
        {
            Mesh fragmentMesh = CreateVoronoiFragmentMesh(originalMesh, cellCenters, i, originalBounds);
            if (fragmentMesh != null && fragmentMesh.vertexCount > 0)
            {
                GameObject fragment = new GameObject($"{targetObject.name}_Fragment_V{i}");
                fragment.transform.SetParent(targetObject.transform.parent);
                fragment.transform.position = targetObject.transform.position; // Keep original position
                fragment.transform.rotation = targetObject.transform.rotation;
                fragment.transform.localScale = originalScale;

                MeshFilter fragmentFilter = fragment.AddComponent<MeshFilter>();
                fragmentFilter.sharedMesh = fragmentMesh;
                MeshRenderer fragmentRenderer = fragment.AddComponent<MeshRenderer>();
                fragmentRenderer.sharedMaterials = targetObject.GetComponent<MeshRenderer>().sharedMaterials;

                ApplyFragmentComponents(fragment);
                fragments.Add(fragment);
            }
        }

        return fragments;
    }

    private List<GameObject> RadialFracture(Mesh originalMesh, Bounds originalBounds, Vector3 originalScale)
    {
        List<GameObject> fragments = new List<GameObject>();
        Vector3 center = targetObject.transform.InverseTransformPoint(
            fractureOrigin != Vector3.zero ? fractureOrigin : targetObject.transform.position
        );

        float angleStep = 360f / divisions.x;
        float heightStep = (originalBounds.max.y - originalBounds.min.y) / Mathf.Max(1, divisions.y);
        float radiusStep = originalBounds.extents.magnitude / Mathf.Max(1, divisions.z);

        for (int slice = 0; slice < divisions.x; slice++)
        {
            float angle1 = slice * angleStep;
            float angle2 = (slice + 1) * angleStep;

            for (int ring = 0; ring < divisions.z; ring++)
            {
                float innerRadius = ring * radiusStep;
                float outerRadius = (ring + 1) * radiusStep;

                for (int height = 0; height < divisions.y; height++)
                {
                    float bottom = originalBounds.min.y + height * heightStep;
                    float top = originalBounds.min.y + (height + 1) * heightStep;

                    Mesh fragmentMesh = CreateRadialFragmentMesh(originalMesh, center, angle1, angle2,
                                                               innerRadius, outerRadius, bottom, top);

                    if (fragmentMesh != null && fragmentMesh.vertexCount > 0)
                    {
                        GameObject fragment = new GameObject($"{targetObject.name}_Fragment_R{slice}_{ring}_{height}");
                        fragment.transform.SetParent(targetObject.transform.parent);
                        fragment.transform.position = targetObject.transform.position; // Keep original position
                        fragment.transform.rotation = targetObject.transform.rotation;
                        fragment.transform.localScale = originalScale;

                        MeshFilter fragmentFilter = fragment.AddComponent<MeshFilter>();
                        fragmentFilter.sharedMesh = fragmentMesh;
                        MeshRenderer fragmentRenderer = fragment.AddComponent<MeshRenderer>();
                        fragmentRenderer.sharedMaterials = targetObject.GetComponent<MeshRenderer>().sharedMaterials;

                        ApplyFragmentComponents(fragment);
                        fragments.Add(fragment);
                    }
                }
            }
        }

        return fragments;
    }

    private List<GameObject> SlicedFracture(Mesh originalMesh, Bounds originalBounds, Vector3 originalScale)
    {
        List<GameObject> fragments = new List<GameObject>();
        Mesh currentMesh = originalMesh;

        for (int i = 0; i < divisions.x; i++)
        {
            Vector3 sliceNormal = randomizeDivisions ? Random.onUnitSphere : Vector3.up;
            float t = (float)i / divisions.x;
            float slicePosition = Mathf.Lerp(-originalBounds.extents.magnitude, originalBounds.extents.magnitude, t);

            Vector3 slicePoint = originalBounds.center + sliceNormal * slicePosition;
            Mesh[] slicedMeshes = SliceMeshWithPlane(currentMesh, slicePoint, sliceNormal);

            if (slicedMeshes != null && slicedMeshes.Length > 0)
            {
                for (int j = 0; j < slicedMeshes.Length; j++)
                {
                    if (slicedMeshes[j] == null || slicedMeshes[j].vertexCount == 0) continue;

                    GameObject fragment = new GameObject($"{targetObject.name}_Fragment_S{i}_{j}");
                    fragment.transform.SetParent(targetObject.transform.parent);
                    fragment.transform.position = targetObject.transform.position;
                    fragment.transform.rotation = targetObject.transform.rotation;
                    fragment.transform.localScale = originalScale;

                    MeshFilter fragmentFilter = fragment.AddComponent<MeshFilter>();
                    fragmentFilter.sharedMesh = slicedMeshes[j];
                    MeshRenderer fragmentRenderer = fragment.AddComponent<MeshRenderer>();
                    fragmentRenderer.sharedMaterials = targetObject.GetComponent<MeshRenderer>().sharedMaterials;

                    ApplyFragmentComponents(fragment);
                    fragments.Add(fragment);
                }

                if (i < divisions.x - 1)
                {
                    currentMesh = slicedMeshes[1]; // Use right side for next slice
                }
            }
        }

        return fragments;
    }

    private GameObject CreateFragmentObject(Mesh fragmentMesh, Bounds fragmentBounds, int x, int y, int z, Vector3 originalScale)
    {
        GameObject fragment = new GameObject($"{targetObject.name}_Fragment_{x}_{y}_{z}");
        Undo.RegisterCreatedObjectUndo(fragment, "Create Fragment");
        fragment.transform.SetParent(targetObject.transform.parent);
        fragment.transform.position = targetObject.transform.TransformPoint(fragmentBounds.center);
        fragment.transform.rotation = targetObject.transform.rotation;
        fragment.transform.localScale = originalScale;

        MeshFilter fragmentFilter = fragment.AddComponent<MeshFilter>();
        fragmentFilter.sharedMesh = fragmentMesh;
        MeshRenderer fragmentRenderer = fragment.AddComponent<MeshRenderer>();
        fragmentRenderer.sharedMaterials = targetObject.GetComponent<MeshRenderer>().sharedMaterials;

        ApplyFragmentComponents(fragment);
        return fragment;
    }

    private bool IsPointInBounds(Vector3 point, Vector3 boundsMin, Vector3 boundsMax)
    {
        return point.x >= boundsMin.x && point.x <= boundsMax.x &&
               point.y >= boundsMin.y && point.y <= boundsMax.y &&
               point.z >= boundsMin.z && point.z <= boundsMax.z;
    }

    private bool TriangleIntersectsBounds(Vector3 v1, Vector3 v2, Vector3 v3, Bounds bounds)
    {
        // Simple AABB check for triangle bounds
        Vector3 min = Vector3.Min(Vector3.Min(v1, v2), v3);
        Vector3 max = Vector3.Max(Vector3.Max(v1, v2), v3);

        Bounds triangleBounds = new Bounds();
        triangleBounds.SetMinMax(min, max);

        return bounds.Intersects(triangleBounds);
    }

    private bool IsBoundsInsideTriangle(Bounds bounds, Vector3 v1, Vector3 v2, Vector3 v3)
    {
        // Simple check if bounds center is inside triangle
        Vector3 center = bounds.center;
        return IsPointInsideTriangle(center, v1, v2, v3);
    }

    private bool IsPointInsideTriangle(Vector3 p, Vector3 a, Vector3 b, Vector3 c)
    {
        // Project to 2D for triangle test
        Vector3 normal = Vector3.Cross(b - a, c - a).normalized;
        Vector3 right = Vector3.Cross(normal, Vector3.up).normalized;
        Vector3 up = Vector3.Cross(right, normal);

        Vector2 p2d = new Vector2(Vector3.Dot(p - a, right), Vector3.Dot(p - a, up));
        Vector2 a2d = Vector2.zero;
        Vector2 b2d = new Vector2(Vector3.Dot(b - a, right), Vector3.Dot(b - a, up));
        Vector2 c2d = new Vector2(Vector3.Dot(c - a, right), Vector3.Dot(c - a, up));

        return IsPointInsideTriangle2D(p2d, a2d, b2d, c2d);
    }

    private bool IsPointInsideTriangle2D(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
    {
        float d1 = Sign(p, a, b);
        float d2 = Sign(p, b, c);
        float d3 = Sign(p, c, a);

        bool hasNeg = (d1 < 0) || (d2 < 0) || (d3 < 0);
        bool hasPos = (d1 > 0) || (d2 > 0) || (d3 > 0);

        return !(hasNeg && hasPos);
    }

    private float Sign(Vector2 p1, Vector2 p2, Vector2 p3)
    {
        return (p1.x - p3.x) * (p2.y - p3.y) - (p2.x - p3.x) * (p1.y - p3.y);
    }

    private void ClipTriangleAgainstBounds(
        Vector3 v1, Vector3 v2, Vector3 v3,
        Vector3 n1, Vector3 n2, Vector3 n3,
        Vector2 uv1, Vector2 uv2, Vector2 uv3,
        Bounds bounds,
        List<Vector3> clippedVertices,
        List<Vector3> clippedNormals,
        List<Vector2> clippedUVs)
    {
        // Add vertices that are inside the bounds
        if (IsPointInBounds(v1, bounds.min, bounds.max))
        {
            clippedVertices.Add(v1);
            clippedNormals.Add(n1);
            clippedUVs.Add(uv1);
        }
        if (IsPointInBounds(v2, bounds.min, bounds.max))
        {
            clippedVertices.Add(v2);
            clippedNormals.Add(n2);
            clippedUVs.Add(uv2);
        }
        if (IsPointInBounds(v3, bounds.min, bounds.max))
        {
            clippedVertices.Add(v3);
            clippedNormals.Add(n3);
            clippedUVs.Add(uv3);
        }

        // Add intersection points at bounds
        AddBoundIntersectionPoints(v1, v2, n1, n2, uv1, uv2, bounds,
                                 clippedVertices, clippedNormals, clippedUVs);
        AddBoundIntersectionPoints(v2, v3, n2, n3, uv2, uv3, bounds,
                                 clippedVertices, clippedNormals, clippedUVs);
        AddBoundIntersectionPoints(v3, v1, n3, n1, uv3, uv1, bounds,
                                 clippedVertices, clippedNormals, clippedUVs);
    }

    private void AddBoundIntersectionPoints(
        Vector3 v1, Vector3 v2, Vector3 n1, Vector3 n2, Vector2 uv1, Vector2 uv2,
        Bounds bounds, List<Vector3> vertices, List<Vector3> normals, List<Vector2> uvs)
    {
        Vector3 dir = v2 - v1;
        float length = dir.magnitude;
        if (length < 0.0001f) return;

        dir /= length;
        Ray ray = new Ray(v1, dir);
        float distance;

        // Check intersection with each plane of the bounds
        if (IntersectRayAABBPlane(ray, bounds, out distance) && distance <= length)
        {
            float t = distance / length;
            vertices.Add(Vector3.Lerp(v1, v2, t));
            normals.Add(Vector3.Lerp(n1, n2, t).normalized);
            uvs.Add(Vector2.Lerp(uv1, uv2, t));
        }
    }

    private bool IntersectRayAABBPlane(Ray ray, Bounds bounds, out float distance)
    {
        distance = float.MaxValue;
        bool hasIntersection = false;

        // Check each axis
        for (int i = 0; i < 3; i++)
        {
            if (Mathf.Abs(ray.direction[i]) < 0.0001f) continue;

            // Check min plane
            float d = (bounds.min[i] - ray.origin[i]) / ray.direction[i];
            if (d >= 0)
            {
                Vector3 p = ray.origin + ray.direction * d;
                if (IsPointInBounds(p, bounds.min, bounds.max))
                {
                    distance = Mathf.Min(distance, d);
                    hasIntersection = true;
                }
            }

            // Check max plane
            d = (bounds.max[i] - ray.origin[i]) / ray.direction[i];
            if (d >= 0)
            {
                Vector3 p = ray.origin + ray.direction * d;
                if (IsPointInBounds(p, bounds.min, bounds.max))
                {
                    distance = Mathf.Min(distance, d);
                    hasIntersection = true;
                }
            }
        }

        return hasIntersection;
    }

    private void CreateCapFaces(List<Vector3> vertices, List<Vector3> normals, List<Vector2> uvs,
                              List<int> triangles, Bounds bounds)
    {
        // Find vertices that lie on bounds planes
        List<Vector3> capVertices = new List<Vector3>();
        List<Vector3> capNormals = new List<Vector3>();
        List<Vector2> capUVs = new List<Vector2>();

        for (int i = 0; i < vertices.Count; i++)
        {
            Vector3 v = vertices[i];
            if (IsVertexOnBoundsPlane(v, bounds))
            {
                capVertices.Add(v);
                capNormals.Add(GetBoundsPlaneNormal(v, bounds));
                capUVs.Add(GenerateCapUV(v, bounds));
            }
        }

        // Triangulate cap vertices
        if (capVertices.Count >= 3)
        {
            // Simple fan triangulation from center
            Vector3 center = capVertices.Aggregate(Vector3.zero, (acc, v) => acc + v) / capVertices.Count;
            Vector3 centerNormal = capNormals[0];
            Vector2 centerUV = new Vector2(0.5f, 0.5f);

            int centerIndex = vertices.Count;
            vertices.Add(center);
            normals.Add(centerNormal);
            uvs.Add(centerUV);

            for (int i = 0; i < capVertices.Count; i++)
            {
                int next = (i + 1) % capVertices.Count;
                triangles.Add(centerIndex);
                triangles.Add(vertices.Count + i);
                triangles.Add(vertices.Count + next);
            }

            vertices.AddRange(capVertices);
            normals.AddRange(capNormals);
            uvs.AddRange(capUVs);
        }
    }

    private bool IsVertexOnBoundsPlane(Vector3 vertex, Bounds bounds)
    {
        const float epsilon = 0.0001f;
        return Mathf.Abs(vertex.x - bounds.min.x) < epsilon || Mathf.Abs(vertex.x - bounds.max.x) < epsilon ||
               Mathf.Abs(vertex.y - bounds.min.y) < epsilon || Mathf.Abs(vertex.y - bounds.max.y) < epsilon ||
               Mathf.Abs(vertex.z - bounds.min.z) < epsilon || Mathf.Abs(vertex.z - bounds.max.z) < epsilon;
    }

    private Vector3 GetBoundsPlaneNormal(Vector3 vertex, Bounds bounds)
    {
        const float epsilon = 0.0001f;
        if (Mathf.Abs(vertex.x - bounds.min.x) < epsilon) return Vector3.left;
        if (Mathf.Abs(vertex.x - bounds.max.x) < epsilon) return Vector3.right;
        if (Mathf.Abs(vertex.y - bounds.min.y) < epsilon) return Vector3.down;
        if (Mathf.Abs(vertex.y - bounds.max.y) < epsilon) return Vector3.up;
        if (Mathf.Abs(vertex.z - bounds.min.z) < epsilon) return Vector3.back;
        return Vector3.forward;
    }

    private Vector2 GenerateCapUV(Vector3 vertex, Bounds bounds)
    {
        // Project vertex onto the most appropriate plane
        Vector3 normal = GetBoundsPlaneNormal(vertex, bounds);
        Vector3 tangent = Vector3.Cross(normal, Vector3.up);
        if (tangent.sqrMagnitude < 0.0001f)
            tangent = Vector3.Cross(normal, Vector3.right);
        Vector3 bitangent = Vector3.Cross(normal, tangent);

        // Generate UV based on position in plane
        float u = Vector3.Dot(vertex - bounds.min, tangent) / bounds.size.magnitude;
        float v = Vector3.Dot(vertex - bounds.min, bitangent) / bounds.size.magnitude;
        return new Vector2(u, v);
    }

    private List<float> GenerateCutPoints(float min, float max, int divisions, float randomOffset, bool randomize)
    {
        List<float> points = new List<float>();
        float step = (max - min) / divisions;
        for (int i = 0; i <= divisions; i++)
        {
            float point = min + i * step;
            if (randomize && i > 0 && i < divisions)
            {
                point += Random.Range(-randomOffset, randomOffset) * (max - min);
            }
            points.Add(point);
        }
        return points.OrderBy(p => p).ToList();
    }

    private Bounds ApplyPerlinNoiseToFragmentBounds(Bounds bounds, int x, int y, int z)
    {
        Vector3 min = bounds.min;
        Vector3 max = bounds.max;

        // Apply Perlin noise offset to each corner
        float noiseOffsetX = (Mathf.PerlinNoise(x * 0.1f, y * 0.1f + z * 0.1f) - 0.5f) * noiseScale;
        float noiseOffsetY = (Mathf.PerlinNoise(y * 0.1f, z * 0.1f + x * 0.1f) - 0.5f) * noiseScale;
        float noiseOffsetZ = (Mathf.PerlinNoise(z * 0.1f, x * 0.1f + y * 0.1f) - 0.5f) * noiseScale;

        // Apply different noise to each corner
        min.x += noiseOffsetX;
        min.y += noiseOffsetY;
        min.z += noiseOffsetZ;

        max.x += noiseOffsetX;
        max.y += noiseOffsetY;
        max.z += noiseOffsetZ;

        Bounds noisyBounds = new Bounds();
        noisyBounds.SetMinMax(min, max);
        return noisyBounds;
    }

    private Mesh SliceMesh(Mesh originalMesh, Bounds fragmentBounds, Bounds originalBounds)
    {
        List<Vector3> vertices = new List<Vector3>();
        List<Vector3> normals = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<int> triangles = new List<int>();

        originalMesh.GetVertices(vertices);
        originalMesh.GetNormals(normals);
        originalMesh.GetUVs(0, uvs);
        triangles.AddRange(originalMesh.triangles);

        List<Vector3> newVertices = new List<Vector3>();
        List<Vector3> newNormals = new List<Vector3>();
        List<Vector2> newUVs = new List<Vector2>();
        List<int> newTriangles = new List<int>();
        Dictionary<Vector3, int> vertexMap = new Dictionary<Vector3, int>(new Vector3EqualityComparer());

        Vector3 fragmentMin = fragmentBounds.min;
        Vector3 fragmentMax = fragmentBounds.max;

        // Process triangles
        for (int i = 0; i < triangles.Count; i += 3)
        {
            if (i + 2 >= triangles.Count) continue;

            Vector3 v1 = vertices[triangles[i]];
            Vector3 v2 = vertices[triangles[i + 1]];
            Vector3 v3 = vertices[triangles[i + 2]];

            Vector3 n1 = normals[triangles[i]];
            Vector3 n2 = normals[triangles[i + 1]];
            Vector3 n3 = normals[triangles[i + 2]];

            Vector2 uv1 = uvs.Count > triangles[i] ? uvs[triangles[i]] : Vector2.zero;
            Vector2 uv2 = uvs.Count > triangles[i + 1] ? uvs[triangles[i + 1]] : Vector2.zero;
            Vector2 uv3 = uvs.Count > triangles[i + 2] ? uvs[triangles[i + 2]] : Vector2.zero;

            // Check if any vertex is within the fragment bounds
            bool v1Inside = IsPointInBounds(v1, fragmentMin, fragmentMax);
            bool v2Inside = IsPointInBounds(v2, fragmentMin, fragmentMax);
            bool v3Inside = IsPointInBounds(v3, fragmentMin, fragmentMax);

            if (v1Inside || v2Inside || v3Inside)
            {
                // If all vertices are inside or the triangle intersects the bounds
                int index1 = GetOrAddVertex(v1, n1, uv1, newVertices, newNormals, newUVs, vertexMap);
                int index2 = GetOrAddVertex(v2, n2, uv2, newVertices, newNormals, newUVs, vertexMap);
                int index3 = GetOrAddVertex(v3, n3, uv3, newVertices, newNormals, newUVs, vertexMap);

                newTriangles.Add(index1);
                newTriangles.Add(index2);
                newTriangles.Add(index3);
            }
            // Add triangles that intersect the fragment bounds (more complex case)
            else if (TriangleIntersectsBounds(v1, v2, v3, fragmentBounds) ||
                    (insideFracture && IsBoundsInsideTriangle(fragmentBounds, v1, v2, v3)))
            {
                // Add triangle vertices with clipping against bounds
                List<Vector3> clippedVertices = new List<Vector3>();
                List<Vector3> clippedNormals = new List<Vector3>();
                List<Vector2> clippedUVs = new List<Vector2>();

                // Create clipped triangle against bounds
                ClipTriangleAgainstBounds(
                    v1, v2, v3,
                    n1, n2, n3,
                    uv1, uv2, uv3,
                    fragmentBounds,
                    clippedVertices, clippedNormals, clippedUVs
                );

                // Convert clipped polygon to triangles (simple fan triangulation)
                for (int j = 0; j < clippedVertices.Count - 2; j++)
                {
                    int clipIndex1 = GetOrAddVertex(clippedVertices[0], clippedNormals[0], clippedUVs[0],
                                                 newVertices, newNormals, newUVs, vertexMap);
                    int clipIndex2 = GetOrAddVertex(clippedVertices[j + 1], clippedNormals[j + 1], clippedUVs[j + 1],
                                                 newVertices, newNormals, newUVs, vertexMap);
                    int clipIndex3 = GetOrAddVertex(clippedVertices[j + 2], clippedNormals[j + 2], clippedUVs[j + 2],
                                                 newVertices, newNormals, newUVs, vertexMap);

                    newTriangles.Add(clipIndex1);
                    newTriangles.Add(clipIndex2);
                    newTriangles.Add(clipIndex3);
                }
            }
        }

        if (newTriangles.Count == 0)
        {
            return null;
        }

        // Create caps for cut faces if needed
        if (createCaps)
        {
            CreateCapFaces(newVertices, newNormals, newUVs, newTriangles, fragmentBounds);
        }

        Mesh newMesh = new Mesh();
        newMesh.vertices = newVertices.ToArray();
        newMesh.normals = newNormals.ToArray();
        newMesh.uv = newUVs.ToArray();
        newMesh.triangles = newTriangles.ToArray();
        newMesh.RecalculateBounds();

        // Fix normals if needed
        if (newMesh.vertexCount > 0)
        {
            // Only recalculate normals for cap faces
            // newMesh.RecalculateNormals();
        }

        return newMesh;
    }

    private Mesh CreateVoronoiFragmentMesh(Mesh originalMesh, List<Vector3> cellCenters, int currentCell, Bounds originalBounds)
    {
        List<Vector3> vertices = new List<Vector3>();
        List<Vector3> normals = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<int> triangles = new List<int>();

        originalMesh.GetVertices(vertices);
        originalMesh.GetNormals(normals);
        originalMesh.GetUVs(0, uvs);
        originalMesh.GetTriangles(triangles, 0);

        List<Vector3> newVertices = new List<Vector3>();
        List<Vector3> newNormals = new List<Vector3>();
        List<Vector2> newUVs = new List<Vector2>();
        List<int> newTriangles = new List<int>();

        // Process each triangle
        for (int i = 0; i < triangles.Count; i += 3)
        {
            Vector3 v1 = vertices[triangles[i]];
            Vector3 v2 = vertices[triangles[i + 1]];
            Vector3 v3 = vertices[triangles[i + 2]];
            Vector3 centerPoint = (v1 + v2 + v3) / 3f;

            // Check if this triangle belongs to current cell
            int closestCell = GetClosestCellIndex(centerPoint, cellCenters);
            if (closestCell == currentCell)
            {
                int baseIndex = newVertices.Count;
                newVertices.Add(v1);
                newVertices.Add(v2);
                newVertices.Add(v3);

                newNormals.Add(normals[triangles[i]]);
                newNormals.Add(normals[triangles[i + 1]]);
                newNormals.Add(normals[triangles[i + 2]]);

                newUVs.Add(uvs[triangles[i]]);
                newUVs.Add(uvs[triangles[i + 1]]);
                newUVs.Add(uvs[triangles[i + 2]]);

                newTriangles.Add(baseIndex);
                newTriangles.Add(baseIndex + 1);
                newTriangles.Add(baseIndex + 2);
            }
        }

        if (newTriangles.Count == 0)
            return null;

        Mesh newMesh = new Mesh();
        newMesh.vertices = newVertices.ToArray();
        newMesh.normals = newNormals.ToArray();
        newMesh.uv = newUVs.ToArray();
        newMesh.triangles = newTriangles.ToArray();
        newMesh.RecalculateBounds();
        return newMesh;
    }

    private int GetClosestCellIndex(Vector3 point, List<Vector3> cellCenters)
    {
        int closest = 0;
        float minDist = float.MaxValue;
        for (int i = 0; i < cellCenters.Count; i++)
        {
            float dist = Vector3.SqrMagnitude(point - cellCenters[i]);
            if (dist < minDist)
            {
                minDist = dist;
                closest = i;
            }
        }
        return closest;
    }

    private Mesh CreateRadialFragmentMesh(Mesh originalMesh, Vector3 center, float angle1, float angle2,
                                        float innerRadius, float outerRadius, float bottom, float top)
    {
        List<Vector3> vertices = new List<Vector3>();
        List<Vector3> normals = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<int> triangles = new List<int>();

        originalMesh.GetVertices(vertices);
        originalMesh.GetNormals(normals);
        originalMesh.GetUVs(0, uvs);
        originalMesh.GetTriangles(triangles, 0);

        List<Vector3> newVertices = new List<Vector3>();
        List<Vector3> newNormals = new List<Vector3>();
        List<Vector2> newUVs = new List<Vector2>();
        List<int> newTriangles = new List<int>();

        // Process each triangle
        for (int i = 0; i < triangles.Count; i += 3)
        {
            Vector3 v1 = vertices[triangles[i]] - center;
            Vector3 v2 = vertices[triangles[i + 1]] - center;
            Vector3 v3 = vertices[triangles[i + 2]] - center;

            // Check if triangle is within the current radial segment
            bool inSegment = IsTriangleInRadialSegment(v1, v2, v3, angle1, angle2, innerRadius, outerRadius, bottom, top);
            if (inSegment)
            {
                int baseIndex = newVertices.Count;
                newVertices.Add(v1 + center);
                newVertices.Add(v2 + center);
                newVertices.Add(v3 + center);

                newNormals.Add(normals[triangles[i]]);
                newNormals.Add(normals[triangles[i + 1]]);
                newNormals.Add(normals[triangles[i + 2]]);

                newUVs.Add(uvs[triangles[i]]);
                newUVs.Add(uvs[triangles[i + 1]]);
                newUVs.Add(uvs[triangles[i + 2]]);

                newTriangles.Add(baseIndex);
                newTriangles.Add(baseIndex + 1);
                newTriangles.Add(baseIndex + 2);
            }
        }

        if (newTriangles.Count == 0)
            return null;

        Mesh newMesh = new Mesh();
        newMesh.vertices = newVertices.ToArray();
        newMesh.normals = newNormals.ToArray();
        newMesh.uv = newUVs.ToArray();
        newMesh.triangles = newTriangles.ToArray();
        newMesh.RecalculateBounds();
        return newMesh;
    }

    private bool IsTriangleInRadialSegment(Vector3 v1, Vector3 v2, Vector3 v3,
                                         float angle1, float angle2,
                                         float innerRadius, float outerRadius,
                                         float bottom, float top)
    {
        Vector3 center = (v1 + v2 + v3) / 3f;
        float radius = new Vector2(center.x, center.z).magnitude;
        float angle = Mathf.Atan2(center.z, center.x) * Mathf.Rad2Deg;
        if (angle < 0) angle += 360f;

        bool inAngle = (angle >= angle1 && angle <= angle2) ||
                      (angle + 360f >= angle1 && angle + 360f <= angle2);
        bool inRadius = radius >= innerRadius && radius <= outerRadius;
        bool inHeight = center.y >= bottom && center.y <= top;

        return inAngle && inRadius && inHeight;
    }

    private Mesh[] SliceMeshWithPlane(Mesh mesh, Vector3 planePoint, Vector3 planeNormal)
    {
        List<Vector3> vertices = new List<Vector3>();
        List<Vector3> normals = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<int> triangles = new List<int>();

        mesh.GetVertices(vertices);
        mesh.GetNormals(normals);
        mesh.GetUVs(0, uvs);
        mesh.GetTriangles(triangles, 0);

        List<Vector3> posVertices = new List<Vector3>();
        List<Vector3> posNormals = new List<Vector3>();
        List<Vector2> posUVs = new List<Vector2>();
        List<int> posTriangles = new List<int>();

        List<Vector3> negVertices = new List<Vector3>();
        List<Vector3> negNormals = new List<Vector3>();
        List<Vector2> negUVs = new List<Vector2>();
        List<int> negTriangles = new List<int>();

        // Process each triangle
        for (int i = 0; i < triangles.Count; i += 3)
        {
            Vector3[] triVertices = new Vector3[3] {
                vertices[triangles[i]],
                vertices[triangles[i + 1]],
                vertices[triangles[i + 2]]
            };

            float[] sides = new float[3];
            for (int j = 0; j < 3; j++)
            {
                sides[j] = Vector3.Dot(planeNormal, triVertices[j] - planePoint);
            }

            // All vertices on positive side
            if (sides.All(s => s >= 0))
            {
                AddTriangleToList(triVertices,
                    new Vector3[] { normals[triangles[i]], normals[triangles[i + 1]], normals[triangles[i + 2]] },
                    new Vector2[] { uvs[triangles[i]], uvs[triangles[i + 1]], uvs[triangles[i + 2]] },
                    posVertices, posNormals, posUVs, posTriangles);
            }
            // All vertices on negative side
            else if (sides.All(s => s <= 0))
            {
                AddTriangleToList(triVertices,
                    new Vector3[] { normals[triangles[i]], normals[triangles[i + 1]], normals[triangles[i + 2]] },
                    new Vector2[] { uvs[triangles[i]], uvs[triangles[i + 1]], uvs[triangles[i + 2]] },
                    negVertices, negNormals, negUVs, negTriangles);
            }
        }

        Mesh[] result = new Mesh[2];
        if (posTriangles.Count > 0)
        {
            result[0] = new Mesh();
            result[0].vertices = posVertices.ToArray();
            result[0].normals = posNormals.ToArray();
            result[0].uv = posUVs.ToArray();
            result[0].triangles = posTriangles.ToArray();
            result[0].RecalculateBounds();
        }
        if (negTriangles.Count > 0)
        {
            result[1] = new Mesh();
            result[1].vertices = negVertices.ToArray();
            result[1].normals = negNormals.ToArray();
            result[1].uv = negUVs.ToArray();
            result[1].triangles = negTriangles.ToArray();
            result[1].RecalculateBounds();
        }
        return result;
    }

    private void AddTriangleToList(Vector3[] vertices, Vector3[] normals, Vector2[] uvs,
                                 List<Vector3> vertList, List<Vector3> normList,
                                 List<Vector2> uvList, List<int> triList)
    {
        int baseIndex = vertList.Count;
        vertList.AddRange(vertices);
        normList.AddRange(normals);
        uvList.AddRange(uvs);
        triList.Add(baseIndex);
        triList.Add(baseIndex + 1);
        triList.Add(baseIndex + 2);
    }

    private void ApplyFragmentComponents(GameObject fragment)
    {
        if (addColliders)
        {
            MeshCollider collider = fragment.AddComponent<MeshCollider>();
            collider.convex = true;
        }

        if (addRigidbodies)
        {
            Rigidbody rb = fragment.AddComponent<Rigidbody>();
            rb.mass = 1.0f;
            rb.drag = 0.1f;
            rb.angularDrag = 0.05f;
        }

        if (applyMaterialVariation && variationMaterials != null && variationMaterials.Length > 0)
        {
            MeshRenderer renderer = fragment.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                Material randomMaterial = variationMaterials[Random.Range(0, variationMaterials.Length)];
                renderer.sharedMaterial = randomMaterial;
            }
        }
    }

    private void SaveFragmentsAsPrefabs(List<GameObject> fragments)
    {
        string folderPath = "Assets/Fragments";
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            AssetDatabase.CreateFolder("Assets", "Fragments");
        }

        foreach (GameObject fragment in fragments)
        {
            string prefabPath = $"{folderPath}/{fragment.name}.prefab";
            PrefabUtility.SaveAsPrefabAsset(fragment, prefabPath);
        }
    }

    private class Edge
    {
        public int v1, v2;
        public Edge(int v1, int v2)
        {
            this.v1 = Mathf.Min(v1, v2);
            this.v2 = Mathf.Max(v1, v2);
        }
    }

    private class EdgeEqualityComparer : IEqualityComparer<Edge>
    {
        public bool Equals(Edge e1, Edge e2)
        {
            return e1.v1 == e2.v1 && e1.v2 == e2.v2;
        }

        public int GetHashCode(Edge e)
        {
            return e.v1.GetHashCode() ^ e.v2.GetHashCode();
        }
    }

    private class Vector3EqualityComparer : IEqualityComparer<Vector3>
    {
        private const float epsilon = 0.001f;

        public bool Equals(Vector3 v1, Vector3 v2)
        {
            return Vector3.SqrMagnitude(v1 - v2) < epsilon * epsilon;
        }

        public int GetHashCode(Vector3 v)
        {
            return Mathf.RoundToInt(v.x * 100f).GetHashCode() ^
                   Mathf.RoundToInt(v.y * 100f).GetHashCode() ^
                   Mathf.RoundToInt(v.z * 100f).GetHashCode();
        }
    }

    private int GetOrAddVertex(Vector3 vertex, Vector3 normal, Vector2 uv,
        List<Vector3> vertices, List<Vector3> normals, List<Vector2> uvs,
        Dictionary<Vector3, int> vertexMap)
    {
        if (vertexMap.TryGetValue(vertex, out int index))
            return index;

        index = vertices.Count;
        vertices.Add(vertex);
        normals.Add(normal);
        uvs.Add(uv);
        vertexMap[vertex] = index;
        return index;
    }

    private void AddTriangleToMesh(
        Vector3 v1, Vector3 v2, Vector3 v3,
        Vector3 n1, Vector3 n2, Vector3 n3,
        Vector2 uv1, Vector2 uv2, Vector2 uv3,
        List<Vector3> vertices, List<Vector3> normals, List<Vector2> uvs, List<int> triangles)
    {
        int index = vertices.Count;
        vertices.Add(v1);
        vertices.Add(v2);
        vertices.Add(v3);
        normals.Add(n1);
        normals.Add(n2);
        normals.Add(n3);
        uvs.Add(uv1);
        uvs.Add(uv2);
        uvs.Add(uv3);
        triangles.Add(index);
        triangles.Add(index + 1);
        triangles.Add(index + 2);
    }
}