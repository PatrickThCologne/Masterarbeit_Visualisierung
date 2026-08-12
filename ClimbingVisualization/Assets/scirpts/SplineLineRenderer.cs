using UnityEngine;
using UnityEngine.Splines;
using System.Collections.Generic;
using TMPro;

[RequireComponent(typeof(SplineContainer))]
[RequireComponent(typeof(LineRenderer))]
public class SplineHandler : MonoBehaviour
{
    [System.Serializable]
    public class RouteMetaData
    {
        public string routeName;
        
        [TextArea(2, 5)]
        public string description;

        public string difficulty = "7B";

        public int exes;

        [TextArea(2, 5)]
        public string info;
    }

    [Header("Line-Settings")]
    [SerializeField] private float lineWidth = 0.01f;
    [SerializeField] private Material lineMaterialURP;

    [Header("Cross-Settings")]
    [SerializeField] private GameObject crossPrefab;
    [SerializeField] private GameObject topCrossPrefab;
    [SerializeField] private float crossScale = 1.0f;
    [SerializeField] private float topCrossScale = 1.2f;

    [Header("Sonstiges")]
    [SerializeField] private RouteMetaData routeData = new RouteMetaData();
    private float crossOffsetToCamera = 0.01f;
    private float maxOffsetDistance = 0.1f;
    private Vector3 topCrossOriginalPos;
    private GameObject topCrossInstance;
    private readonly List<GameObject> spawnedCrosses = new();

    private LineRenderer lineRenderer;
    private LineRenderer hitAreaRenderer;
    private SplineContainer splineContainer;
    private MeshCollider meshCollider;
    private Mesh bakedHitMesh;

    private void OnValidate()
    {
        if (lineRenderer != null)
        {
            lineRenderer.startWidth = lineWidth;
            lineRenderer.endWidth = lineWidth;
        }

        foreach (var cross in spawnedCrosses)
        {
            if (cross == null) continue;

            if (cross == topCrossInstance)
                cross.transform.localScale = Vector3.one * topCrossScale;
            else
                cross.transform.localScale = Vector3.one * crossScale;
        }
    }

    private void Awake()
    {
        splineContainer = GetComponent<SplineContainer>();
        lineRenderer = GetComponent<LineRenderer>();
        meshCollider = GetComponent<MeshCollider>();

        if (meshCollider == null)
            meshCollider = gameObject.AddComponent<MeshCollider>();

        SetupLineRenderer();
        UpdateLineFromSpline();
        SetupLineCollider();
        SpawnCrossesAtKnots();
    }

    private void SetupLineRenderer()
    {
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;
        lineRenderer.useWorldSpace = false;
        lineRenderer.textureMode = LineTextureMode.Stretch;

        if (lineMaterialURP != null)
            lineRenderer.material = lineMaterialURP;
    }

    private void UpdateLineFromSpline()
    {
        var spline = splineContainer.Spline;
        int pointCount = 50;

        lineRenderer.useWorldSpace = false;
        lineRenderer.positionCount = pointCount;

        for (int i = 0; i < pointCount; i++)
        {
            float t = (float)i / (pointCount - 1);
            Vector3 worldPos = splineContainer.EvaluatePosition(t);
            Vector3 localPos = transform.InverseTransformPoint(worldPos);
            lineRenderer.SetPosition(i, localPos);
        }
    }

    private void SpawnCrossesAtKnots()
    {
        var spline = splineContainer.Spline;

        if (spline.Count < 1) return;

        for (int i = 0; i < spline.Count; i++)
        {
            float t = spline.Count == 1 ? 0f : (float)i / (spline.Count - 1);
            Vector3 worldPos = splineContainer.EvaluatePosition(t);
            bool isTop = (i == spline.Count - 1);

            GameObject prefabToUse = isTop ? topCrossPrefab : crossPrefab;
            if (prefabToUse == null) continue;

            GameObject cross = Instantiate(prefabToUse, worldPos, Quaternion.identity, transform);
            cross.name = isTop ? "TopCross" : $"Cross_{i}";
            cross.transform.localScale = Vector3.one * (isTop ? topCrossScale : crossScale);

            spawnedCrosses.Add(cross);

            if (isTop)
            {
                topCrossInstance = cross;
                topCrossOriginalPos = cross.transform.position;

                TextMeshPro textMesh = cross.GetComponentInChildren<TextMeshPro>(true);
                if (textMesh != null)
                {
                    textMesh.text = routeData.difficulty;

                    Material textMat = new Material(textMesh.fontMaterial);

                    Shader overlayShader = Shader.Find("TextMeshPro/Distance Field Overlay");
                    if (overlayShader == null)
                        overlayShader = Shader.Find("TextMeshPro/Mobile/Distance Field Overlay");

                    if (overlayShader != null)
                        textMat.shader = overlayShader;

                    textMat.renderQueue = 4000;
                    textMesh.fontMaterial = textMat;
                }
            }
        }
    }

    private void SetupLineCollider()
    {
        if (hitAreaRenderer == null)
        {
            GameObject hitAreaObj = new GameObject("HitAreaRenderer");
            hitAreaObj.transform.SetParent(transform, false);
            hitAreaObj.transform.localPosition = Vector3.zero;
            hitAreaObj.transform.localRotation = Quaternion.identity;
            hitAreaObj.transform.localScale = Vector3.one;

            hitAreaRenderer = hitAreaObj.AddComponent<LineRenderer>();
        }

        hitAreaRenderer.useWorldSpace = lineRenderer.useWorldSpace;
        hitAreaRenderer.alignment = lineRenderer.alignment;
        hitAreaRenderer.positionCount = lineRenderer.positionCount;
        hitAreaRenderer.textureMode = LineTextureMode.Stretch;
        hitAreaRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        hitAreaRenderer.receiveShadows = false;
        hitAreaRenderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;

        Vector3[] positions = new Vector3[lineRenderer.positionCount];
        lineRenderer.GetPositions(positions);
        hitAreaRenderer.SetPositions(positions);

        hitAreaRenderer.startWidth = lineWidth * 6f;
        hitAreaRenderer.endWidth = lineWidth * 6f;
        hitAreaRenderer.widthMultiplier = 15f;

        Material invisibleMat = CreateInvisibleURPMaterial();
        hitAreaRenderer.material = invisibleMat;

        if (bakedHitMesh != null)
            Destroy(bakedHitMesh);

        bakedHitMesh = new Mesh();
        bakedHitMesh.name = $"{gameObject.name}_HitMesh";

        hitAreaRenderer.enabled = true;
        hitAreaRenderer.BakeMesh(bakedHitMesh, Camera.main, false);
        hitAreaRenderer.enabled = false;

        meshCollider.sharedMesh = null;
        meshCollider.sharedMesh = bakedHitMesh;
        meshCollider.convex = false;
        meshCollider.isTrigger = false;
    }

    private Material CreateInvisibleURPMaterial()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
        {
            Debug.LogError("URP/Unlit Shader nicht gefunden.");
            return null;
        }

        Material mat = new Material(shader);
        mat.name = "InvisibleHitLine_URP";

        mat.SetFloat("_Surface", 1f);
        mat.SetFloat("_Blend", 0f);
        mat.SetFloat("_ZWrite", 0f);
        mat.SetFloat("_Cull", 0f);

        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);

        mat.DisableKeyword("_ALPHATEST_ON");
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");

        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", new Color(1f, 1f, 1f, 0f));

        mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

        return mat;
    }

    private void LateUpdate()
    {
        if (topCrossInstance == null || Camera.main == null)
            return;

        Vector3 camPos = Camera.main.transform.position;
        Vector3 directionToCam = (camPos - topCrossOriginalPos).normalized;
        Vector3 offset = directionToCam * crossOffsetToCamera;

        float offsetDistance = offset.magnitude;
        if (offsetDistance > maxOffsetDistance)
            offset = offset.normalized * maxOffsetDistance;

        // topCrossInstance.transform.position = topCrossOriginalPos + offset;
    }

    private void OnDestroy()
    {
        if (bakedHitMesh != null)
            Destroy(bakedHitMesh);

        if (hitAreaRenderer != null && hitAreaRenderer.material != null)
            Destroy(hitAreaRenderer.material);
    }

     public RouteMetaData RouteData => routeData;
}