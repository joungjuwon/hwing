using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace ProceduralTreeGeneratorByMysticForge
{
    public class TreeGeneratorWindow : EditorWindow
{
    private int selectedTab = 0;
    private readonly string[] tabs = { "Trunk", "Branch", "Leaf" };
    private Vector2 leafTabScrollPosition;
    private Vector2 branchTabScrollPosition;
    private Vector2 trunkTabScrollPosition;
    private Vector2 dropoutScrollPosition;
    private Vector2 dropout2ScrollPosition;
    private bool showTrunkMaterialSettings = false;

    private float trunkHeight = 4.1f;
    private float trunkRadius = 0.1f;
    private float trunkRadiusCurvature = 0.8f;
    private float trunkRadiusNoise = 0.5f;
    private int trunkSubdivision = 0;
    private float trunkCrinkliness = 0f;
    private int trunkSegments = 4;
    private float trunkBending = 0.02f;
    private bool includeStump = true;
    private float treeStumpStartPoint = 0.1f;
    private float treeStumpWidth = 2f;

    private Material trunkMaterial;
    private GameObject trunkObject;
    private Vector3 spawnPosition = new Vector3(0, 0, 0);


    private int numberOfBranches = 17;
    private float branchHeightMin = 0.19f;
    private float branchHeightMax = 0.94f;
    private float branchRadius = 0.08f;
    private float branchLength = 2.99f;
    private float branchRadiusCurvature = 0.95f;
    private float branchRadiusNoise = 0.96f;
    private int branchSubdivision = 0;
    private float branchCrinkliness = 0f;
    private int branchSegments = 4;
    private float branchBending = 0.15f;
    public float branchAngle = -68.5f;
    public bool adjustBranchLengthByHeight = true;
    public bool angleAdjustmentByHeight = false;
    private float gravity = 0.13f;

    private int numberOfBranchlets = 40;
    private float branchletHeightMin = 0.2f;
    private float branchletHeightMax = 0.94f;
    private float branchletRadius = 0.2f;
    private float branchletLength = 0.76f;
    private float branchletRadiusCurvature = 0.92f;
    private float branchletRadiusNoise = 0.29f;
    private int branchletSubdivision = 0;
    private float branchletCrinkliness = 0f;
    private int branchletSegments = 3;
    private float branchletBending = 0.14f;
    private float branchletAngle = 53.1f;
    public float branchletForwardAngle = -50.5f;
    public float gravityBranchlets = 0.15f;
    public bool adjustBranchletLengthByHeight = true;

    private GameObject branchesParent;
    private GameObject branchletsParent;

    private int numberOfLeaves = 21;
    private float leafSize = 1.37f;
    private float leafPositionMin = 0.83f;
    private float leafPositionMax = 1f;
    private float leafForwardRotation = 0f;
    private float leafRotation = 0f;
    private float leafRandomizeRotation = 0.47f;
    private Material leafMaterial;
    private bool showMaterialSettings = false;
    public bool addLeavesToBranch = false;
    private float leafBranchRandomPositioning = 0f;
    private Vector3 leafBranchPositioning = Vector3.zero;

    private Vector3 leafBranchSizeV3 = new Vector3(1f,1f,1f);
    private float leafSizeBranchRandom = 0.32f;

    private int numberOfLeavesBranchlet = 15;
    private float leafBranchletSize = 1.5f;
    private float leafBranchletPositionMin = 0.27f;
    private float leafBranchletPositionMax = 1f;
    private float leafBranchletForwardRotation = 0f;
    private float leafBranchletRotation = 14.8f;
    private float leafBranchletRandomizeRotation = 0.2f;
    public bool addLeavesToBranchlets = false;
    private Vector3 leafBranchletPositioning = Vector3.zero;
    private float leafBranchletRandomPositioning = 0f;

    private Vector3 leafBranchletSizeV3 = new Vector3(1f, 1f, 1f);
    private float leafSizeBranchletRandom = 0.59f;

    private int numberOfLeavesTrunk = 14;
    private float leafTrunkSize = 2.3f;
    private float leafTrunkPositionMin = 0.97f;
    private float leafTrunkPositionMax = 1f;
    private float leafTrunkForwardRotation = 0f;
    private float leafTrunkRotation = 0f;
    private float leafTrunkRandomizeRotation = 0.27f;
    private float leafTrunkRandomPositioning = 0f;
    private Vector3 leafTrunkPositioning = Vector3.zero;

    private Vector3 leafTrunkSizeV3 = new Vector3(1f, 1f, 1f);
    private float leafSizeTrunkRandom = 0f;

    private Trunk trunk;

    //MESH INFO

    private int vertexCount = 0;
    private int triangleCount = 0;
    private int edgeCount = 0;

    private int vertexBranchCount = 0;
    private int triangleBranchCount = 0;
    private int edgeBranchCount = 0;

    private int vertexBranchletCount = 0;
    private int triangleBranchletCount = 0;
    private int edgeBranchletCount = 0;

    private int vertexBranchLeavesCount = 0;
    private int triangleBranchLeavesCount = 0;
    private int edgeBranchLeavesCount = 0;

    private int vertexBranchletLeavesCount = 0;
    private int triangleBranchletLeavesCount = 0;
    private int edgeBranchletLeavesCount = 0;

    private int vertexTrunkLeavesCount = 0;
    private int triangleTrunkLeavesCount = 0;
    private int edgeTrunkLeavesCount = 0;

    private bool showTreePath = false;
    private bool toggleLeaves = true;
    public GameObject leafPrefab;

    private GameObject leavesTrunk;
    private GameObject leavesBranchlet;
    private GameObject leavesBranch;

    string[] presets = { "Basic Tree", "Birch Tree", "Upwards", "Weeping Willow", "Bonsai", "Pine" };
    int selectedPreset = 0;

    private bool showURP = false;
    private bool showHDRP = false;
    private bool showSRP = false;

    private Material[] urpTrunkMaterials;
    private Material[] urpLeafMaterials;

    private Material[] hdrpTrunkMaterials;
    private Material[] hdrpLeafMaterials;

    private Material[] srpTrunkMaterials;
    private Material[] srpLeafMaterials;

    private bool showLeafModels = false;  // This controls the foldout state for Leaf Models
    private GameObject[] leafModels;

    [MenuItem("Tools/Tree Generator")]
    public static void ShowWindow()
    {
        var window = GetWindow<TreeGeneratorWindow>();
        window.titleContent = new GUIContent("Tree Generator");
        window.minSize = new Vector2(400, 300);
        window.Show();
    }

    private void OnGUI()
    {
        EditorGUILayout.Space();

        int previousPreset = selectedPreset;
        selectedPreset = EditorGUILayout.Popup("Presets", selectedPreset, presets);

        if (selectedPreset != previousPreset)
        {
            // Call the function immediately when the selection changes
            switch (selectedPreset)
            {
                case 0: Preset1(); break;
                case 1: Preset2(); break;
                case 2: Preset3(); break;
                case 3: Preset4(); break;
                case 4: Preset5(); break;
                case 5: Preset6(); break;
            }
        }


        GUILayout.Space(5);

        //showTreePath = EditorGUILayout.Toggle("Show Tree Paths", showTreePath);

        //toggleLeaves = EditorGUILayout.Toggle("Show/Hide Leaves", toggleLeaves);

        GUILayout.BeginHorizontal(); // Begin horizontal layout

        showTreePath = EditorGUILayout.Toggle("Show Tree Paths", showTreePath);

        toggleLeaves = EditorGUILayout.Toggle("Show/Hide Leaves", toggleLeaves);

        GUILayout.EndHorizontal();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Select a Material", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal(); // Side-by-side layout
        dropoutScrollPosition = EditorGUILayout.BeginScrollView(dropoutScrollPosition);
        // Left Side: Material Selection
        EditorGUILayout.BeginVertical(GUILayout.Width(250));
        showURP = EditorGUILayout.Foldout(showURP, "URP Materials");
        if (showURP)
        {
            DisplayMaterialList(urpTrunkMaterials, "Trunk Materials", ref trunkMaterial);
            DisplayMaterialList(urpLeafMaterials, "Leaf Materials", ref leafMaterial);
        }

        showHDRP = EditorGUILayout.Foldout(showHDRP, "HDRP Materials");
        if (showHDRP)
        {
            DisplayMaterialList(hdrpTrunkMaterials, "Trunk Materials", ref trunkMaterial);
            DisplayMaterialList(hdrpLeafMaterials, "Leaf Materials", ref leafMaterial);
        }

        showSRP = EditorGUILayout.Foldout(showSRP, "SRP Materials");
        if (showSRP)
        {
            DisplayMaterialList(srpTrunkMaterials, "Trunk Materials", ref trunkMaterial);
            DisplayMaterialList(srpLeafMaterials, "Leaf Materials", ref leafMaterial);
        }
        EditorGUILayout.EndVertical();
        EditorGUILayout.EndScrollView();
        dropout2ScrollPosition = EditorGUILayout.BeginScrollView(dropout2ScrollPosition);
        // Right Side: Leaf Model Selection
        EditorGUILayout.BeginVertical(GUILayout.Width(200)); // Adjust width if needed
        showLeafModels = EditorGUILayout.Foldout(showLeafModels, "Select a Leaf Model");
        if (showLeafModels)
        {
            foreach (var model in leafModels)
            {
                if (model == null) continue;

                Texture previewTexture = AssetPreview.GetAssetPreview(model);

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button(previewTexture, GUILayout.Width(75), GUILayout.Height(75)))
                {
                    leafPrefab = model; // Assign selected model
                }
                EditorGUILayout.LabelField(model.name, EditorStyles.miniLabel, GUILayout.Width(100));
                EditorGUILayout.EndHorizontal();
            }
        }
        EditorGUILayout.EndVertical();
        
        EditorGUILayout.EndHorizontal(); // End Side-by-Side Layout
            EditorGUILayout.EndScrollView();
        if (leavesTrunk != null) leavesTrunk.SetActive(toggleLeaves);
        if (leavesBranchlet != null) leavesBranchlet.SetActive(toggleLeaves);
        if (leavesBranch != null) leavesBranch.SetActive(toggleLeaves);

        GUILayout.BeginHorizontal();
        spawnPosition = EditorGUILayout.Vector3Field("Tree Spawn Position", spawnPosition);

        EditorGUILayout.EndHorizontal();
        
        GUILayout.Space(5);
        selectedTab = GUILayout.Toolbar(selectedTab, tabs);

        GUILayout.Space(10);
        switch (selectedTab)
        {
            case 0:
                DrawTrunkTab();
                break;
            case 1:
                DrawBranchTab();
                break;
            case 2:
                DrawLeafTab();
                break;
        }

        /*if (GUILayout.Button("PrintValues", GUILayout.Height(40)))
        {
            PrintCurrentValues();
        }*/

        if (GUILayout.Button("Generate Random Seed", GUILayout.Height(40)))
        {
            GenerateTreeTrunk(ref vertexCount, ref triangleCount, ref edgeCount, trunkSubdivision, trunkSegments, trunkBending, trunkHeight, trunkRadiusCurvature, trunkRadius,treeStumpStartPoint,treeStumpWidth, includeStump, spawnPosition);

            GenerateTreeBranches(trunkHeight, branchLength, branchAngle, branchBending, trunkBending, trunkRadius, trunkRadiusCurvature, gravity, angleAdjustmentByHeight, adjustBranchLengthByHeight, branchSegments, branchSubdivision, ref vertexBranchCount, ref triangleBranchCount, ref edgeBranchCount);

            GenerateTreeBranchlets(adjustBranchletLengthByHeight, gravityBranchlets, branchletAngle, branchletLength, branchletForwardAngle, branchletBending, branchletSegments, branchletSubdivision, ref vertexBranchletCount, ref triangleBranchletCount, ref edgeBranchletCount);

            GenerateLeafPlanes(leafBranchPositioning, leafBranchSizeV3, leafMaterial, numberOfLeaves, ref vertexBranchLeavesCount, ref triangleBranchLeavesCount, ref edgeBranchLeavesCount, leafPrefab, leafSizeBranchRandom, leafSize, leafPositionMin, leafPositionMax, leafForwardRotation, leafRotation, leafRandomizeRotation, leafBranchRandomPositioning);
            GenerateLeafBranchletPlanes(leafBranchletPositioning, leafBranchletSizeV3, leafMaterial, numberOfLeavesBranchlet, ref vertexBranchletLeavesCount, ref triangleBranchletCount, ref edgeBranchletCount, leafPrefab, leafSizeBranchRandom, leafBranchletSize, leafBranchletPositionMin, leafBranchletPositionMax, leafBranchletForwardRotation, leafBranchletRotation, leafBranchletRandomizeRotation, leafBranchletRandomPositioning);
            GenerateLeafTrunkPlanes(leafTrunkPositioning, leafTrunkSizeV3, leafMaterial, numberOfLeavesTrunk, ref vertexTrunkLeavesCount, ref triangleTrunkLeavesCount, ref edgeTrunkLeavesCount, leafPrefab, leafSizeTrunkRandom, leafTrunkSize, leafTrunkPositionMin, leafTrunkPositionMax, leafTrunkForwardRotation, leafTrunkRotation, leafTrunkRandomizeRotation, leafTrunkRandomPositioning);
        }


        int totalVertices = vertexCount + vertexBranchCount + vertexBranchletCount + vertexBranchLeavesCount + vertexBranchletLeavesCount + vertexTrunkLeavesCount;
        int totalTriangles = triangleCount + triangleBranchCount + triangleBranchletCount + triangleBranchLeavesCount + triangleBranchletLeavesCount + triangleTrunkLeavesCount;
        int totalEdges = edgeCount + edgeBranchCount + edgeBranchletCount + edgeBranchLeavesCount + edgeBranchletLeavesCount + edgeTrunkLeavesCount;

        EditorGUILayout.LabelField("Generated Mesh Info", EditorStyles.boldLabel);

        // Begin Table Box
        GUIStyle boxStyle = new GUIStyle("box");
        EditorGUILayout.BeginVertical(boxStyle);

        // Header Row with Horizontal Line
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Mesh Part", GUILayout.Width(120));
        EditorGUILayout.LabelField("Vertices", GUILayout.Width(80));
        EditorGUILayout.LabelField("Triangles", GUILayout.Width(80));
        EditorGUILayout.LabelField("Edges", GUILayout.Width(80));
        EditorGUILayout.EndHorizontal();
        EditorGUI.DrawRect(EditorGUILayout.GetControlRect(false, 1), Color.gray);

        // Trunk Info
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Trunk", GUILayout.Width(120));
        EditorGUILayout.LabelField($"{vertexCount}", GUILayout.Width(80));
        EditorGUILayout.LabelField($"{triangleCount}", GUILayout.Width(80));
        EditorGUILayout.LabelField($"{edgeCount}", GUILayout.Width(80));
        EditorGUILayout.EndHorizontal();
        EditorGUI.DrawRect(EditorGUILayout.GetControlRect(false, 1), Color.gray);

        // Branch Info
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Branch", GUILayout.Width(120));
        EditorGUILayout.LabelField($"{vertexBranchCount}", GUILayout.Width(80));
        EditorGUILayout.LabelField($"{triangleBranchCount}", GUILayout.Width(80));
        EditorGUILayout.LabelField($"{edgeBranchCount}", GUILayout.Width(80));
        EditorGUILayout.EndHorizontal();
        EditorGUI.DrawRect(EditorGUILayout.GetControlRect(false, 1), Color.gray);

        // Branchlet Info
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Branchlet", GUILayout.Width(120));
        EditorGUILayout.LabelField($"{vertexBranchletCount}", GUILayout.Width(80));
        EditorGUILayout.LabelField($"{triangleBranchletCount}", GUILayout.Width(80));
        EditorGUILayout.LabelField($"{edgeBranchletCount}", GUILayout.Width(80));
        EditorGUILayout.EndHorizontal();
        EditorGUI.DrawRect(EditorGUILayout.GetControlRect(false, 1), Color.gray);

        // Leaves Branch Info
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Leaves Branch", GUILayout.Width(120));
        EditorGUILayout.LabelField($"{vertexBranchLeavesCount}", GUILayout.Width(80));
        EditorGUILayout.LabelField($"{triangleBranchLeavesCount}", GUILayout.Width(80));
        EditorGUILayout.LabelField($"{edgeBranchLeavesCount}", GUILayout.Width(80));
        EditorGUILayout.EndHorizontal();
        EditorGUI.DrawRect(EditorGUILayout.GetControlRect(false, 1), Color.gray);

        // Leaves Branchlet Info
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Leaves Branchlet", GUILayout.Width(120));
        EditorGUILayout.LabelField($"{vertexBranchletLeavesCount}", GUILayout.Width(80));
        EditorGUILayout.LabelField($"{triangleBranchletLeavesCount}", GUILayout.Width(80));
        EditorGUILayout.LabelField($"{edgeBranchletLeavesCount}", GUILayout.Width(80));
        EditorGUILayout.EndHorizontal();
        EditorGUI.DrawRect(EditorGUILayout.GetControlRect(false, 1), Color.gray);

        // Leaves Trunk Info
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Leaves Trunk", GUILayout.Width(120));
        EditorGUILayout.LabelField($"{vertexTrunkLeavesCount}", GUILayout.Width(80));
        EditorGUILayout.LabelField($"{triangleTrunkLeavesCount}", GUILayout.Width(80));
        EditorGUILayout.LabelField($"{edgeTrunkLeavesCount}", GUILayout.Width(80));
        EditorGUILayout.EndHorizontal();
        EditorGUI.DrawRect(EditorGUILayout.GetControlRect(false, 2), Color.gray); // Double Line

        // Total Section
        EditorGUILayout.Space();
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Total", GUILayout.Width(120));
        EditorGUILayout.LabelField("Vertices", GUILayout.Width(80));
        EditorGUILayout.LabelField("Triangles", GUILayout.Width(80));
        EditorGUILayout.LabelField("Edges", GUILayout.Width(80));
        EditorGUILayout.EndHorizontal();
        EditorGUI.DrawRect(EditorGUILayout.GetControlRect(false, 1), Color.gray);

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Total", GUILayout.Width(120));
        EditorGUILayout.LabelField($"{totalVertices}", GUILayout.Width(80));
        EditorGUILayout.LabelField($"{totalTriangles}", GUILayout.Width(80));
        EditorGUILayout.LabelField($"{totalEdges}", GUILayout.Width(80));
        EditorGUILayout.EndHorizontal();

        // End Table Box
        EditorGUILayout.EndVertical();

        GUILayout.FlexibleSpace();

        GUILayout.BeginHorizontal(); // Begin horizontal layout
        if (GUILayout.Button("Save Tree to Project Folder", GUILayout.Height(40)))
        {
            SaveTreePrefab();
        }

        /*if (GUILayout.Button("Save Tree Settings as new Preset", GUILayout.Height(40)))
        {
            
        }*/

        GUILayout.EndHorizontal();
    }

    private void DrawTrunkTab()
    {
        trunkTabScrollPosition = EditorGUILayout.BeginScrollView(trunkTabScrollPosition);
        // Bold and larger header for better visibility
        GUILayout.Label("Trunk Settings", new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 14,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.white }
        });

        // Create a box to visually group trunk settings
        EditorGUILayout.BeginVertical("box");
        GUILayout.Space(5); // Add some top padding

        // Height and Radius Settings
        GUILayout.Label("Basic Shape", EditorStyles.centeredGreyMiniLabel);
        float newHeight = EditorGUILayout.Slider("Trunk Height", trunkHeight, 0f, 10f);
        float newRadius = EditorGUILayout.Slider("Trunk Radius", trunkRadius, 0f, 3f);
        float newBending = EditorGUILayout.Slider("Trunk Bending", trunkBending, 0f, 0.15f);
        includeStump = EditorGUILayout.Toggle("Include Stump", includeStump);
        float newTreeStumpStartPoint = EditorGUILayout.Slider("Tree Stump Starting Point", treeStumpStartPoint, 0f, 0.3f);
        float newTreeStumpWidth = EditorGUILayout.Slider("Tree Stump Width", treeStumpWidth, 0f, 3.0f);

        // Curvature and Noise
        GUILayout.Label("Detail Adjustments", EditorStyles.centeredGreyMiniLabel);
        float newCurvature = EditorGUILayout.Slider("Trunk Radius Curvature", trunkRadiusCurvature, -1f, 1f);
        float newNoise = EditorGUILayout.Slider("Trunk Radius Noise", trunkRadiusNoise, 0f, 1f);
        float newCrinkliness = EditorGUILayout.Slider("Trunk Crinkliness", trunkCrinkliness, 0f, 1f);

        // Subdivision and Crinkliness
        GUILayout.Label("Segments and Subdivisions", EditorStyles.centeredGreyMiniLabel);
        int newSubdivision = EditorGUILayout.IntSlider("Trunk Subdivision", trunkSubdivision, 0, 10);
        int newSegments = EditorGUILayout.IntSlider("Trunk Segments", trunkSegments, 3, 10);

        // Material Assignment
        GUILayout.Label("Material", EditorStyles.centeredGreyMiniLabel);
        trunkMaterial = (Material)EditorGUILayout.ObjectField("Trunk Material", trunkMaterial, typeof(Material), false);

        GUILayout.Space(5); // Add some bottom padding
        EditorGUILayout.EndVertical();


        if (newHeight != trunkHeight || newRadius != trunkRadius || newCurvature != trunkRadiusCurvature ||
            newNoise != trunkRadiusNoise || newSubdivision != trunkSubdivision || newCrinkliness != trunkCrinkliness ||
            newSegments != trunkSegments || newBending != trunkBending || newTreeStumpStartPoint != treeStumpStartPoint || newTreeStumpWidth != treeStumpWidth)
        {
            trunkHeight = Mathf.Max(0.1f, newHeight);
            trunkRadius = Mathf.Max(0.1f, newRadius);
            trunkRadiusCurvature = newCurvature;
            trunkRadiusNoise = newNoise;
            trunkSubdivision = newSubdivision;
            trunkCrinkliness = newCrinkliness;
            trunkSegments = newSegments;
            trunkBending = newBending;
            treeStumpStartPoint = newTreeStumpStartPoint;
            treeStumpWidth = newTreeStumpWidth;
            GenerateTreeTrunk(ref vertexCount,ref triangleCount,ref edgeCount, trunkSubdivision, trunkSegments, trunkBending, trunkHeight, trunkRadiusCurvature, trunkRadius,treeStumpStartPoint, treeStumpWidth, includeStump, spawnPosition);

            GenerateTreeBranches(trunkHeight, branchLength, branchAngle, branchBending, trunkBending, trunkRadius, trunkRadiusCurvature, gravity, angleAdjustmentByHeight, adjustBranchLengthByHeight, branchSegments, branchSubdivision, ref vertexBranchCount, ref triangleBranchCount, ref edgeBranchCount);

            GenerateTreeBranchlets(adjustBranchletLengthByHeight, gravityBranchlets, branchletAngle, branchletLength, branchletForwardAngle, branchletBending, branchletSegments, branchletSubdivision, ref vertexBranchletCount, ref triangleBranchletCount, ref edgeBranchletCount);
            GenerateLeafPlanes(leafBranchPositioning, leafBranchSizeV3, leafMaterial, numberOfLeaves, ref vertexBranchLeavesCount, ref triangleBranchLeavesCount, ref edgeBranchLeavesCount, leafPrefab, leafSizeBranchRandom, leafSize, leafPositionMin, leafPositionMax, leafForwardRotation, leafRotation, leafRandomizeRotation, leafBranchRandomPositioning);
            GenerateLeafBranchletPlanes(leafBranchletPositioning, leafBranchletSizeV3, leafMaterial, numberOfLeavesBranchlet, ref vertexBranchletLeavesCount, ref triangleBranchletLeavesCount, ref edgeBranchletLeavesCount, leafPrefab, leafSizeBranchRandom, leafBranchletSize, leafBranchletPositionMin, leafBranchletPositionMax, leafBranchletForwardRotation, leafBranchletRotation, leafBranchletRandomizeRotation, leafBranchletRandomPositioning);
            GenerateLeafTrunkPlanes(leafTrunkPositioning, leafTrunkSizeV3, leafMaterial, numberOfLeavesTrunk, ref vertexTrunkLeavesCount, ref triangleTrunkLeavesCount, ref edgeTrunkLeavesCount, leafPrefab, leafSizeTrunkRandom, leafTrunkSize, leafTrunkPositionMin, leafTrunkPositionMax, leafTrunkForwardRotation, leafTrunkRotation, leafTrunkRandomizeRotation, leafTrunkRandomPositioning);
            //GenerateLeafTrunkPlanes(Vector3 leafTrunkPositioning, leafMaterial, numberOfTrunk, vertexTrunkLeavesCount, triangleTrunkLeavesCount, edgeTrunkLeavesCount, leafPrefab, leafTrunkSize, leafTrunkPositionMin, leafTrunkPositionMax, leafTrunkForwardRotation, leafTrunkRotation, leafTrunkRandomizeRotation, leafTrunkRandomPositioning)
        }

        if (trunkMaterial != null)
        {
            // Foldout for material properties
            showTrunkMaterialSettings = EditorGUILayout.Foldout(showTrunkMaterialSettings, "Material Properties");
            if (showTrunkMaterialSettings)
            {
                Texture mainTexture = (Texture)EditorGUILayout.ObjectField("Bark Texture", trunkMaterial.GetTexture("_BarkTexture"), typeof(Texture), false);
                trunkMaterial.SetTexture("_BarkTexture", mainTexture);

                Texture mainNormalTexture = (Texture)EditorGUILayout.ObjectField("Bark Normal Texture", trunkMaterial.GetTexture("_BarkNormalTexture"), typeof(Texture), false);
                trunkMaterial.SetTexture("_BarkNormalTexture", mainNormalTexture);

                Color mainColor = EditorGUILayout.ColorField("Bark Color", trunkMaterial.GetColor("_BarkColor"));
                trunkMaterial.SetColor("_BarkColor", mainColor);

                trunkMaterial.SetFloat("_BarkNormalStrength", EditorGUILayout.FloatField("Bark Normal Strength", trunkMaterial.GetFloat("_BarkNormalStrength")));


                Texture mainMossTexture = (Texture)EditorGUILayout.ObjectField("Moss Texture", trunkMaterial.GetTexture("_MossTexture"), typeof(Texture), false);
                trunkMaterial.SetTexture("_MossTexture", mainMossTexture);

                Texture mainNormalMossTexture = (Texture)EditorGUILayout.ObjectField("Moss Normal Texture", trunkMaterial.GetTexture("_MossNormalTexture"), typeof(Texture), false);
                trunkMaterial.SetTexture("_MossNormalTexture", mainNormalMossTexture);

                Color mainMossColor = EditorGUILayout.ColorField("Moss Color", trunkMaterial.GetColor("_MossColor"));
                trunkMaterial.SetColor("_MossColor", mainMossColor);

                trunkMaterial.SetFloat("_MossHeight", EditorGUILayout.FloatField("Moss Height", trunkMaterial.GetFloat("_MossHeight")));

                trunkMaterial.SetFloat("_MossThreshold", EditorGUILayout.FloatField("Moss Threshold", trunkMaterial.GetFloat("_MossThreshold")));

                trunkMaterial.SetFloat("_Smoothness", EditorGUILayout.Slider("Smoothness", trunkMaterial.GetFloat("_Smoothness"), 0f, 1f));

                trunkMaterial.SetFloat("_AO", EditorGUILayout.Slider("AO", trunkMaterial.GetFloat("_AO"), 0f, 1f));

                Texture shadowTexture = (Texture)EditorGUILayout.ObjectField("Shadow Texture", leafMaterial.GetTexture("_ShadowTexture2D"), typeof(Texture), false);
                leafMaterial.SetTexture("_ShadowTexture2D", shadowTexture);

                Color shadowColor = EditorGUILayout.ColorField("Shadow Color", trunkMaterial.GetColor("_ShadowColor"));
                trunkMaterial.SetColor("_ShadowColor", shadowColor);

                Color shadowPatternColor = EditorGUILayout.ColorField("Shadow Pattern Color", trunkMaterial.GetColor("_ShadowPatternColor"));
                trunkMaterial.SetColor("_ShadowPatternColor", shadowPatternColor);

                trunkMaterial.SetFloat("_ShadowStep", EditorGUILayout.Slider("Shadow Step", trunkMaterial.GetFloat("_ShadowStep"), 0f, 1f));
            }
        }
        else
        {
            EditorGUILayout.HelpBox("Please assign a material to edit.", MessageType.Info);
        }
        EditorGUILayout.EndScrollView();

    }

    private void DrawBranchTab()
    {
        branchTabScrollPosition = EditorGUILayout.BeginScrollView(branchTabScrollPosition);

        // Bold and larger header for better visibility
        GUILayout.Label("Branch Settings", new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 14,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.white }
        });

        // Begin a box to group branch settings
        EditorGUILayout.BeginVertical("box");
        GUILayout.Space(5); // Add some top padding

        // General Branch Properties
        GUILayout.Label("General Properties", EditorStyles.centeredGreyMiniLabel);
        int previousNumberOfBranches = numberOfBranches;
        numberOfBranches = EditorGUILayout.IntSlider("Number of Branches", numberOfBranches, 0, 20);
        float previousBranchHeightMin = branchHeightMin;
        float previousBranchHeightMax = branchHeightMax;
        branchHeightMin = EditorGUILayout.Slider("Branch Height Min", branchHeightMin, 0f, 1f);
        branchHeightMax = EditorGUILayout.Slider("Branch Height Max", branchHeightMax, 0f, 1f);

        // Dimensions and Angles
        GUILayout.Label("Dimensions and Angles", EditorStyles.centeredGreyMiniLabel);
        float previousBranchRadius = branchRadius;
        float previousBranchLength = branchLength;
        branchRadius = EditorGUILayout.Slider("Branch Radius", branchRadius, 0f, 5f);
        branchLength = EditorGUILayout.Slider("Branch Length", branchLength, 0f, 10f);
        float previousBranchAngle = branchAngle;
        branchAngle = EditorGUILayout.Slider("Branch Angle", branchAngle, -180f, 0f);
        float previousGravity = gravity;
        gravity = EditorGUILayout.Slider("Branch Gravity", gravity, -1f, 1f);

        // Detail Adjustments
        GUILayout.Label("Detail Adjustments", EditorStyles.centeredGreyMiniLabel);
        float newCurvature = EditorGUILayout.Slider("Branch Radius Curvature", branchRadiusCurvature, -1f, 1f);
        float newNoise = EditorGUILayout.Slider("Branch Radius Noise", branchRadiusNoise, 0f, 1f);
        float newCrinkliness = EditorGUILayout.Slider("Branch Crinkliness", branchCrinkliness, 0f, 1f);
        float newBending = EditorGUILayout.Slider("Branch Bending", branchBending, 0f, 0.15f);

        // Segments and Bending
        GUILayout.Label("Segments and Subdivisions", EditorStyles.centeredGreyMiniLabel);
        int newSegments = EditorGUILayout.IntSlider("Branch Segments", branchSegments, 3, 10);
        int newSubdivision = EditorGUILayout.IntSlider("Branch Subdivision", branchSubdivision, 0, 5);

        // Miscellaneous Settings
        GUILayout.Label("Miscellaneous", EditorStyles.centeredGreyMiniLabel);
        bool newAdjustedBranchLengthByHeight = EditorGUILayout.Toggle("Shorten by Height", adjustBranchLengthByHeight);
        bool newAngleAdjustmentByHeight = EditorGUILayout.Toggle("Increase Angle by Height", angleAdjustmentByHeight);

        GUILayout.Space(5); // Add some bottom padding
        EditorGUILayout.EndVertical();


        if (previousNumberOfBranches != numberOfBranches ||
            previousBranchHeightMin != branchHeightMin ||
            previousBranchHeightMax != branchHeightMax ||
            previousBranchRadius != branchRadius ||
            previousBranchLength != branchLength || newCurvature != branchRadiusCurvature ||
            newNoise != branchRadiusNoise || newSubdivision != branchSubdivision || newCrinkliness != branchCrinkliness ||
            newSegments != branchSegments || newBending != branchBending || previousBranchAngle != branchAngle || newAdjustedBranchLengthByHeight != adjustBranchLengthByHeight || previousGravity != gravity || newAngleAdjustmentByHeight != angleAdjustmentByHeight)

        {
            branchRadiusCurvature = newCurvature;
            branchRadiusNoise = newNoise;
            branchSubdivision = newSubdivision;
            branchCrinkliness = newCrinkliness;
            branchSegments = newSegments;
            branchBending = newBending;
            adjustBranchLengthByHeight = newAdjustedBranchLengthByHeight;
            angleAdjustmentByHeight = newAngleAdjustmentByHeight;
            GenerateTreeBranches(trunkHeight, branchLength, branchAngle, branchBending, trunkBending, trunkRadius, trunkRadiusCurvature, gravity,angleAdjustmentByHeight, adjustBranchLengthByHeight, branchSegments, branchSubdivision, ref vertexBranchCount, ref triangleBranchCount, ref edgeBranchCount);


            GenerateTreeBranchlets(adjustBranchletLengthByHeight, gravityBranchlets, branchletAngle, branchletLength, branchletForwardAngle, branchletBending, branchletSegments, branchletSubdivision, ref vertexBranchletCount, ref triangleBranchletCount, ref edgeBranchletCount);
            GenerateLeafPlanes(leafBranchPositioning, leafBranchSizeV3, leafMaterial, numberOfLeaves, ref vertexBranchLeavesCount, ref triangleBranchLeavesCount, ref edgeBranchLeavesCount, leafPrefab, leafSizeBranchRandom, leafSize, leafPositionMin, leafPositionMax, leafForwardRotation, leafRotation, leafRandomizeRotation, leafBranchRandomPositioning);
            GenerateLeafBranchletPlanes(leafBranchletPositioning, leafBranchletSizeV3, leafMaterial, numberOfLeavesBranchlet, ref vertexBranchletLeavesCount, ref triangleBranchletLeavesCount, ref edgeBranchletLeavesCount, leafPrefab, leafSizeBranchRandom, leafBranchletSize, leafBranchletPositionMin, leafBranchletPositionMax, leafBranchletForwardRotation, leafBranchletRotation, leafBranchletRandomizeRotation, leafBranchletRandomPositioning);
            GenerateLeafTrunkPlanes(leafTrunkPositioning, leafTrunkSizeV3, leafMaterial, numberOfLeavesTrunk, ref vertexTrunkLeavesCount, ref triangleTrunkLeavesCount, ref edgeTrunkLeavesCount, leafPrefab, leafSizeTrunkRandom, leafTrunkSize, leafTrunkPositionMin, leafTrunkPositionMax, leafTrunkForwardRotation, leafTrunkRotation, leafTrunkRandomizeRotation, leafTrunkRandomPositioning);

        }

        GUILayout.Space(10);
        // Bold and larger header for better visibility
        GUILayout.Label("Branchlet Settings", new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 14,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.white }
        });

        // Create a box to visually group branchlet settings
        EditorGUILayout.BeginVertical("box");
        GUILayout.Space(5); // Add some top padding

        // General Branchlet Properties
        GUILayout.Label("General Properties", EditorStyles.centeredGreyMiniLabel);
        int previousNumberOfBranchlets = numberOfBranchlets;
        numberOfBranchlets = EditorGUILayout.IntSlider("Number of Branchlets", numberOfBranchlets, 0, 40);
        float previousBranchletHeightMin = branchletHeightMin;
        float previousBranchletHeightMax = branchletHeightMax;
        branchletHeightMin = EditorGUILayout.Slider("Branchlet Distance Min", branchletHeightMin, 0, 1);
        branchletHeightMax = EditorGUILayout.Slider("Branchlet Distance Max", branchletHeightMax, 0, 1);

        // Dimensions and Angles
        GUILayout.Label("Dimensions and Angles", EditorStyles.centeredGreyMiniLabel);
        float previousBranchletRadius = branchletRadius;
        float previousBranchletLength = branchletLength;
        branchletRadius = EditorGUILayout.Slider("Branchlet Radius", branchletRadius, 0, 5);
        branchletLength = EditorGUILayout.Slider("Branchlet Length", branchletLength, 0, 5);
        float previousBranchletAngle = branchletAngle;
        float previousBranchletForwardAngle = branchletForwardAngle;
        branchletAngle = EditorGUILayout.Slider("Branchlet Angle", branchletAngle, -90f, 90f);
        branchletForwardAngle = EditorGUILayout.Slider("Branchlet Forward Angle", branchletForwardAngle, -180, 180);
        float previousGravityBranchlets = gravityBranchlets;
        gravityBranchlets = EditorGUILayout.Slider("Branchlet Gravity", gravityBranchlets, -1, 1);

        // Detail Adjustments
        GUILayout.Label("Detail Adjustments", EditorStyles.centeredGreyMiniLabel);
        float newCurvatureBranchlet = EditorGUILayout.Slider("Branchlet Radius Curvature", branchletRadiusCurvature, -1f, 1f);
        float newNoiseBranchlet = EditorGUILayout.Slider("Branchlet Radius Noise", branchletRadiusNoise, 0f, 1f);
        float newCrinklinessBranchlet = EditorGUILayout.Slider("Branchlet Crinkliness", branchletCrinkliness, 0f, 1f);
        float newBendingBranchlet = EditorGUILayout.Slider("Branchlet Bending", branchletBending, 0f, 0.15f);

        // Segments and Bending
        GUILayout.Label("Segments and Subdivisions", EditorStyles.centeredGreyMiniLabel);
        int newSegmentsBranchlet = EditorGUILayout.IntSlider("Branchlet Segments", branchletSegments, 3, 10);
        int newSubdivisionBranchlet = EditorGUILayout.IntSlider("Branchlet Subdivision", branchletSubdivision, 0, 5);

        // Miscellaneous Settings
        GUILayout.Label("Miscellaneous", EditorStyles.centeredGreyMiniLabel);
        bool newAdjustedBranchletLengthByHeight = EditorGUILayout.Toggle("Shorten by Height", adjustBranchletLengthByHeight);


        GUILayout.Space(5); // Add some bottom padding
        EditorGUILayout.EndVertical();


        if (previousNumberOfBranchlets != numberOfBranchlets ||
            previousBranchletHeightMin != branchletHeightMin ||
            previousBranchletHeightMax != branchletHeightMax ||
            previousBranchletRadius != branchletRadius ||
            previousBranchletLength != branchletLength || newCurvatureBranchlet != branchletRadiusCurvature ||
            newNoiseBranchlet != branchletRadiusNoise || newSubdivisionBranchlet != branchletSubdivision || newCrinklinessBranchlet != branchletCrinkliness ||
            newSegmentsBranchlet != branchletSegments || newBendingBranchlet != branchletBending || previousBranchletAngle != branchletAngle || previousBranchletForwardAngle != branchletForwardAngle || previousGravityBranchlets != gravityBranchlets || newAdjustedBranchletLengthByHeight != adjustBranchletLengthByHeight)

        {
            branchletRadiusCurvature = newCurvatureBranchlet;
            branchletRadiusNoise = newNoiseBranchlet;
            branchletSubdivision = newSubdivisionBranchlet;
            branchletCrinkliness = newCrinklinessBranchlet;
            branchletSegments = newSegmentsBranchlet;
            branchletBending = newBendingBranchlet;
            adjustBranchletLengthByHeight = newAdjustedBranchletLengthByHeight;
            GenerateTreeBranchlets(adjustBranchletLengthByHeight, gravityBranchlets, branchletAngle, branchletLength, branchletForwardAngle, branchletBending, branchletSegments, branchletSubdivision, ref vertexBranchletCount, ref triangleBranchletCount, ref edgeBranchletCount);

            GenerateLeafPlanes(leafBranchPositioning, leafBranchSizeV3, leafMaterial, numberOfLeaves, ref vertexBranchLeavesCount, ref triangleBranchLeavesCount, ref edgeBranchLeavesCount, leafPrefab, leafSizeBranchRandom, leafSize, leafPositionMin, leafPositionMax, leafForwardRotation, leafRotation, leafRandomizeRotation, leafBranchRandomPositioning);
            GenerateLeafBranchletPlanes(leafBranchletPositioning, leafBranchletSizeV3, leafMaterial, numberOfLeavesBranchlet, ref vertexBranchletLeavesCount, ref triangleBranchletLeavesCount, ref edgeBranchletLeavesCount, leafPrefab, leafSizeBranchRandom, leafBranchletSize, leafBranchletPositionMin, leafBranchletPositionMax, leafBranchletForwardRotation, leafBranchletRotation, leafBranchletRandomizeRotation, leafBranchletRandomPositioning);
            GenerateLeafTrunkPlanes(leafTrunkPositioning, leafTrunkSizeV3, leafMaterial, numberOfLeavesTrunk, ref vertexTrunkLeavesCount, ref triangleTrunkLeavesCount, ref edgeTrunkLeavesCount, leafPrefab, leafSizeTrunkRandom, leafTrunkSize, leafTrunkPositionMin, leafTrunkPositionMax, leafTrunkForwardRotation, leafTrunkRotation, leafTrunkRandomizeRotation, leafTrunkRandomPositioning);
        }
        EditorGUILayout.EndScrollView();
    }

    private void DrawLeafTab()
    {
        leafTabScrollPosition = EditorGUILayout.BeginScrollView(leafTabScrollPosition);

        int previousNumberOfLeaves = numberOfLeaves;
        float previousLeafPositionMin = leafPositionMin;
        float previousLeafPositionMax = leafPositionMax;
        float previousLeafSize = leafSize;
        float previousLeafRotation = leafRotation;
        float previousLeafRandomizeRotation = leafRandomizeRotation;
        float previousLeafForwardRotation = leafForwardRotation;
        Vector3 previousLeafBranchPositioning = leafBranchPositioning;
        float previousLeafBranchRandomPositioning = leafBranchRandomPositioning;

        Vector3 previousLeafBranchSizeV3 = leafBranchSizeV3;
        float previousLeafSizeBranchRandom = leafSizeBranchRandom;

        Material previousLeafMaterial = leafMaterial;

        GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 14,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.white }
        };

        // Leaf Branch Settings
        GUILayout.Label("Leaf Branch Settings", headerStyle);
        EditorGUILayout.BeginVertical("box");

        GUILayout.Label("General Settings", EditorStyles.centeredGreyMiniLabel);
        numberOfLeaves = EditorGUILayout.IntSlider("Number of Leaves", numberOfLeaves, 0, 200);
        leafSize = EditorGUILayout.Slider("Leaf Size", leafSize, 0, 5);
        leafBranchSizeV3 = EditorGUILayout.Vector3Field("Leaf Scaling Vector3", leafBranchSizeV3);

        GUILayout.Label("Variation Settings", EditorStyles.centeredGreyMiniLabel);
        leafSizeBranchRandom = EditorGUILayout.Slider("Leaf Size Variation", leafSizeBranchRandom, 0, 1);
        leafPositionMin = EditorGUILayout.Slider("Leaf Position Min", leafPositionMin, 0, 1);
        leafPositionMax = EditorGUILayout.Slider("Leaf Position Max", leafPositionMax, 0, 1);

        GUILayout.Label("Rotation Settings", EditorStyles.centeredGreyMiniLabel);
        leafForwardRotation = EditorGUILayout.Slider("Leaf Forward Rotation", leafForwardRotation, 0, 360);
        leafRotation = EditorGUILayout.Slider("Leaf Rotation", leafRotation, 0, 180);
        leafRandomizeRotation = EditorGUILayout.Slider("Randomize Leaf Rotation", leafRandomizeRotation, 0, 1);

        GUILayout.Label("Positioning Settings", EditorStyles.centeredGreyMiniLabel);
        leafBranchPositioning = EditorGUILayout.Vector3Field("Leaf Positioning XYZ", leafBranchPositioning);
        leafBranchRandomPositioning = EditorGUILayout.Slider("Randomize Leaf Spread", leafBranchRandomPositioning, 0, 1);

        EditorGUILayout.EndVertical();

        if (previousLeafMaterial != leafMaterial || previousNumberOfLeaves != numberOfLeaves ||
            previousLeafSize != leafSize || previousLeafForwardRotation != leafForwardRotation || previousLeafBranchSizeV3 != leafBranchSizeV3 || previousLeafSizeBranchRandom != leafSizeBranchRandom || previousLeafRotation != leafRotation || previousLeafRandomizeRotation != leafRandomizeRotation || previousLeafPositionMin != leafPositionMin || previousLeafPositionMax != leafPositionMax || previousLeafBranchPositioning != leafBranchPositioning || previousLeafBranchRandomPositioning != leafBranchRandomPositioning)
        {
            GenerateLeafPlanes(leafBranchPositioning, leafBranchSizeV3, leafMaterial, numberOfLeaves, ref vertexBranchLeavesCount, ref triangleBranchLeavesCount, ref edgeBranchLeavesCount, leafPrefab, leafSizeBranchRandom, leafSize, leafPositionMin, leafPositionMax, leafForwardRotation, leafRotation, leafRandomizeRotation, leafBranchRandomPositioning);
        }

        //--------------------------------------------------------------------//

        int previousNumberOfLeavesBranchlet = numberOfLeavesBranchlet;
        float previousLeafBranchletPositionMin = leafBranchletPositionMin;
        float previousLeafBranchletPositionMax = leafBranchletPositionMax;
        float previousLeafBranchletSize = leafBranchletSize;
        float previousLeafBranchletRotation = leafBranchletRotation;
        float previousLeafBranchletRandomizeRotation = leafBranchletRandomizeRotation;
        float previousLeafBranchletForwardRotation = leafBranchletForwardRotation;
        Vector3 previousLeafBranchletPositioning = leafBranchletPositioning;
        float previousLeafBranchletRandomPositioning = leafBranchletRandomPositioning;

        Vector3 previousLeafBranchletSizeV3 = leafBranchletSizeV3;
        float previousLeafSizeBranchletRandom = leafSizeBranchletRandom;

        GUILayout.Space(10);

        // Leaf Branchlet Settings
        GUILayout.Label("Leaf Branchlet Settings", headerStyle);
        EditorGUILayout.BeginVertical("box");

        GUILayout.Label("General Settings", EditorStyles.centeredGreyMiniLabel);
        numberOfLeavesBranchlet = EditorGUILayout.IntSlider("Number of Leaves", numberOfLeavesBranchlet, 0, 200);
        leafBranchletSize = EditorGUILayout.Slider("Leaf Size", leafBranchletSize, 0, 5);
        leafBranchletSizeV3 = EditorGUILayout.Vector3Field("Leaf Scaling Vector3", leafBranchletSizeV3);

        GUILayout.Label("Variation Settings", EditorStyles.centeredGreyMiniLabel);
        leafSizeBranchletRandom = EditorGUILayout.Slider("Leaf Size Variation", leafSizeBranchletRandom, 0, 1);
        leafBranchletPositionMin = EditorGUILayout.Slider("Leaf Position Min", leafBranchletPositionMin, 0, 1);
        leafBranchletPositionMax = EditorGUILayout.Slider("Leaf Position Max", leafBranchletPositionMax, 0, 1);

        GUILayout.Label("Rotation Settings", EditorStyles.centeredGreyMiniLabel);
        leafBranchletForwardRotation = EditorGUILayout.Slider("Leaf Forward Rotation", leafBranchletForwardRotation, 0, 180);
        leafBranchletRotation = EditorGUILayout.Slider("Leaf Rotation", leafBranchletRotation, 0, 180);
        leafBranchletRandomizeRotation = EditorGUILayout.Slider("Randomize Leaf Rotation", leafBranchletRandomizeRotation, 0, 1);

        GUILayout.Label("Positioning Settings", EditorStyles.centeredGreyMiniLabel);
        leafBranchletPositioning = EditorGUILayout.Vector3Field("Leaf Positioning XYZ", leafBranchletPositioning);
        leafBranchletRandomPositioning = EditorGUILayout.Slider("Randomize Leaf Spread", leafBranchletRandomPositioning, 0, 1);

        EditorGUILayout.EndVertical();

        if (previousNumberOfLeavesBranchlet != numberOfLeavesBranchlet ||
            previousLeafBranchletSize != leafBranchletSize || previousLeafBranchletForwardRotation != leafBranchletForwardRotation || previousLeafBranchletSizeV3 != leafBranchletSizeV3 || previousLeafSizeBranchletRandom != leafSizeBranchletRandom || previousLeafBranchletRotation != leafBranchletRotation || previousLeafBranchletRandomizeRotation != leafBranchletRandomizeRotation || previousLeafBranchletPositionMin != leafBranchletPositionMin || previousLeafBranchletPositionMax != leafBranchletPositionMax || previousLeafBranchletPositioning != leafBranchletPositioning || previousLeafBranchletRandomPositioning != leafBranchletRandomPositioning)
        {
            GenerateLeafBranchletPlanes(leafBranchletPositioning, leafBranchletSizeV3, leafMaterial, numberOfLeavesBranchlet, ref vertexBranchletLeavesCount, ref triangleBranchletLeavesCount, ref edgeBranchletLeavesCount, leafPrefab, leafSizeBranchletRandom, leafBranchletSize, leafBranchletPositionMin, leafBranchletPositionMax, leafBranchletForwardRotation, leafBranchletRotation, leafBranchletRandomizeRotation, leafBranchletRandomPositioning);
        }

        int previousNumberOfLeavesTrunk = numberOfLeavesTrunk;
        float previousLeafTrunkPositionMin = leafTrunkPositionMin;
        float previousLeafTrunkPositionMax = leafTrunkPositionMax;
        float previousLeafTrunkSize = leafTrunkSize;
        float previousLeafTrunkRotation = leafTrunkRotation;
        float previousLeafTrunkRandomizeRotation = leafTrunkRandomizeRotation;
        float previousLeafTrunkForwardRotation = leafTrunkForwardRotation;
        Vector3 previousLeafTrunkPositioning = leafTrunkPositioning;
        float previousLeafTrunkRandomPositioning = leafTrunkRandomPositioning;

        Vector3 previousLeafTrunkSizeV3 = leafTrunkSizeV3;
        float previousLeafSizeTrunkRandom = leafSizeTrunkRandom;

        GUILayout.Label("Leaf Trunk Settings", headerStyle);
        EditorGUILayout.BeginVertical("box");

        GUILayout.Label("General Settings", EditorStyles.centeredGreyMiniLabel);
        numberOfLeavesTrunk = EditorGUILayout.IntSlider("Number of Leaves", numberOfLeavesTrunk, 0, 200);
        leafTrunkSize = EditorGUILayout.Slider("Leaf Size", leafTrunkSize, 0, 5);
        leafTrunkSizeV3 = EditorGUILayout.Vector3Field("Leaf Scaling Vector3", leafTrunkSizeV3);

        GUILayout.Label("Variation Settings", EditorStyles.centeredGreyMiniLabel);
        leafSizeTrunkRandom = EditorGUILayout.Slider("Leaf Size Variation", leafSizeTrunkRandom, 0, 1);
        leafTrunkPositionMin = EditorGUILayout.Slider("Leaf Position Min", leafTrunkPositionMin, 0, 1);
        leafTrunkPositionMax = EditorGUILayout.Slider("Leaf Position Max", leafTrunkPositionMax, 0, 1);

        GUILayout.Label("Rotation Settings", EditorStyles.centeredGreyMiniLabel);
        leafTrunkForwardRotation = EditorGUILayout.Slider("Leaf Forward Rotation", leafTrunkForwardRotation, 0, 180);
        leafTrunkRotation = EditorGUILayout.Slider("Leaf Rotation", leafTrunkRotation, 0, 180);
        leafTrunkRandomizeRotation = EditorGUILayout.Slider("Randomize Leaf Rotation", leafTrunkRandomizeRotation, 0, 1);

        GUILayout.Label("Positioning Settings", EditorStyles.centeredGreyMiniLabel);
        leafTrunkPositioning = EditorGUILayout.Vector3Field("Leaf Positioning XYZ", leafTrunkPositioning);
        leafTrunkRandomPositioning = EditorGUILayout.Slider("Randomize Leaf Spread", leafTrunkRandomPositioning, 0, 1);

        GUILayout.Label("Material and Model", EditorStyles.centeredGreyMiniLabel);

        GUILayout.BeginHorizontal(); // Start horizontal layout

        leafMaterial = (Material)EditorGUILayout.ObjectField("Leaf Material", leafMaterial, typeof(Material), false);
        leafPrefab = (GameObject)EditorGUILayout.ObjectField("Leaf Prefab", leafPrefab, typeof(GameObject), false);

        GUILayout.EndHorizontal(); // End horizontal layout


        EditorGUILayout.EndVertical();

        if (previousNumberOfLeavesTrunk != numberOfLeavesTrunk ||
            previousLeafTrunkSize != leafTrunkSize || previousLeafTrunkForwardRotation != leafTrunkForwardRotation || previousLeafTrunkSizeV3 != leafTrunkSizeV3 || previousLeafSizeTrunkRandom != leafSizeTrunkRandom || previousLeafTrunkRotation != leafTrunkRotation || previousLeafTrunkRandomizeRotation != leafTrunkRandomizeRotation || previousLeafTrunkPositionMin != leafTrunkPositionMin || previousLeafTrunkPositionMax != leafTrunkPositionMax || previousLeafTrunkPositioning != leafTrunkPositioning || previousLeafTrunkRandomPositioning != leafTrunkRandomPositioning)
        {
            GenerateLeafTrunkPlanes(leafTrunkPositioning, leafTrunkSizeV3, leafMaterial, numberOfLeavesTrunk, ref vertexTrunkLeavesCount, ref triangleTrunkLeavesCount, ref edgeTrunkLeavesCount, leafPrefab, leafSizeTrunkRandom, leafTrunkSize, leafTrunkPositionMin, leafTrunkPositionMax, leafTrunkForwardRotation, leafTrunkRotation, leafTrunkRandomizeRotation, leafTrunkRandomPositioning);
        }

            if (leafMaterial != null)
            {
                // Foldout for material properties
                showMaterialSettings = EditorGUILayout.Foldout(showMaterialSettings, "Material Properties");
                if (showMaterialSettings)
                {
                    if (leafMaterial.HasProperty("_MainTexture"))
                    {
                        Texture mainTexture = (Texture)EditorGUILayout.ObjectField("Main Texture", leafMaterial.GetTexture("_MainTexture"), typeof(Texture), false);
                        leafMaterial.SetTexture("_MainTexture", mainTexture);
                    }

                    if (leafMaterial.HasProperty("_Alpha"))
                    {
                        leafMaterial.SetFloat("_Alpha", EditorGUILayout.Slider("Texture Alpha", leafMaterial.GetFloat("_Alpha"), 0f, 1f));
                    }

                    if (leafMaterial.HasProperty("_MainColor"))
                    {
                        Color mainColor = EditorGUILayout.ColorField("Main Color", leafMaterial.GetColor("_MainColor"));
                        leafMaterial.SetColor("_MainColor", mainColor);
                    }

                    if (leafMaterial.HasProperty("_ShadowTexture2D"))
                    {
                        Texture shadowTexture = (Texture)EditorGUILayout.ObjectField("Shadow Texture", leafMaterial.GetTexture("_ShadowTexture2D"), typeof(Texture), false);
                        leafMaterial.SetTexture("_ShadowTexture2D", shadowTexture);
                    }

                    if (leafMaterial.HasProperty("_ShadowColor"))
                    {
                        Color shadowColor = EditorGUILayout.ColorField("Shadow Color", leafMaterial.GetColor("_ShadowColor"));
                        leafMaterial.SetColor("_ShadowColor", shadowColor);
                    }

                    if (leafMaterial.HasProperty("_ShadowPatternColor"))
                    {
                        Color shadowPatternColor = EditorGUILayout.ColorField("Shadow Pattern Color", leafMaterial.GetColor("_ShadowPatternColor"));
                        leafMaterial.SetColor("_ShadowPatternColor", shadowPatternColor);
                    }

                    if (leafMaterial.HasProperty("_ShadowStep"))
                    {
                        leafMaterial.SetFloat("_ShadowStep", EditorGUILayout.Slider("Shadow Step", leafMaterial.GetFloat("_ShadowStep"), 0f, 1f));
                    }

                    if (leafMaterial.HasProperty("_Wind_Direction"))
                    {
                        Vector2 windDirection = EditorGUILayout.Vector2Field("Wind Direction UV", leafMaterial.GetVector("_Wind_Direction"));
                        leafMaterial.SetVector("_Wind_Direction", windDirection);
                    }

                    if (leafMaterial.HasProperty("_Wind_Speed_UV"))
                    {
                        leafMaterial.SetFloat("_Wind_Speed_UV", EditorGUILayout.Slider("Wind Speed UV", leafMaterial.GetFloat("_Wind_Speed_UV"), 0f, 20f));
                    }

                    if (leafMaterial.HasProperty("_Wind_Noise_UV"))
                    {
                        leafMaterial.SetFloat("_Wind_Noise_UV", EditorGUILayout.Slider("Wind Noise UV", leafMaterial.GetFloat("_Wind_Noise_UV"), 0f, 100f));
                    }

                    if (leafMaterial.HasProperty("_Bending_Direction_Vertex"))
                    {
                        Vector3 bendingDirection = EditorGUILayout.Vector3Field("Bending Direction Vertex", leafMaterial.GetVector("_Bending_Direction_Vertex"));
                        leafMaterial.SetVector("_Bending_Direction_Vertex", bendingDirection);
                    }

                    if (leafMaterial.HasProperty("_Bend_Strength_Vertex"))
                    {
                        leafMaterial.SetFloat("_Bend_Strength_Vertex", EditorGUILayout.Slider("Bend Strength Vertex", leafMaterial.GetFloat("_Bend_Strength_Vertex"), 0f, 30f));
                    }

                    if (leafMaterial.HasProperty("_Wind_Speed_Vertex"))
                    {
                        leafMaterial.SetFloat("_Wind_Speed_Vertex", EditorGUILayout.Slider("Wind Speed Vertex", leafMaterial.GetFloat("_Wind_Speed_Vertex"), 0f, 1f));
                    }

                    if (leafMaterial.HasProperty("_Noise_Wind_Vertex"))
                    {
                        leafMaterial.SetFloat("_Noise_Wind_Vertex", EditorGUILayout.Slider("Wind Noise Vertex", leafMaterial.GetFloat("_Noise_Wind_Vertex"), 0f, 1f));
                    }

                    if (leafMaterial.HasProperty("_Billboard_Size"))
                    {
                        leafMaterial.SetFloat("_Billboard_Size", EditorGUILayout.Slider("Billboard Size", leafMaterial.GetFloat("_Billboard_Size"), 0f, 10f));
                    }

                    if (leafMaterial.HasProperty("_Inflate")) // Make sure the property name is correct
                    {
                        leafMaterial.SetFloat("_Inflate", EditorGUILayout.Slider("Inflate", leafMaterial.GetFloat("_Inflate"), 0f, 5f));
                    }
                }
            }

            else
            {
            EditorGUILayout.HelpBox("Please assign a material to edit.", MessageType.Info);
        }
        EditorGUILayout.EndScrollView();
    }

    void OnEnable()
    {
        SceneView.duringSceneGui += OnSceneGUI;
        LoadMaterials();
        LoadLeafModels();
    }

    void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
    }

        private void SaveTreePrefab()
        {
            if (trunkObject == null)
            {
                Debug.LogError("No tree object to save! Please generate a tree first.");
                return;
            }

            // Open a Save File Panel to select the location for saving the prefab
            string defaultName = $"{trunkObject.name}.prefab";
            string savePath = EditorUtility.SaveFilePanelInProject("Save Tree Prefab", defaultName, "prefab", "Choose a location to save the tree prefab.");

            if (string.IsNullOrEmpty(savePath))
            {
                Debug.Log("Save operation cancelled.");
                return;
            }

            string folderPath = Path.GetDirectoryName(savePath);
            string baseName = Path.GetFileNameWithoutExtension(savePath);

            // Save the trunk mesh
            MeshFilter trunkMeshFilter = trunkObject.GetComponent<MeshFilter>();
            if (trunkMeshFilter == null || trunkMeshFilter.sharedMesh == null)
            {
                Debug.LogError("No mesh found on the generated tree trunk.");
                return;
            }

            // Collect and combine trunk, branches, and branchlets into one mesh
            List<CombineInstance> allMeshes = new List<CombineInstance>();

            // Add trunk mesh
            CombineInstance trunkCombine = new CombineInstance
            {
                mesh = trunkMeshFilter.sharedMesh,
                transform = trunkObject.transform.localToWorldMatrix
            };
            allMeshes.Add(trunkCombine);

            // Add branches & branchlets
            allMeshes.AddRange(CollectBranchMeshes());

            // Create final combined mesh
            Mesh finalTreeMesh = new Mesh();
            finalTreeMesh.CombineMeshes(allMeshes.ToArray(), true, true);
            string finalMeshPath = Path.Combine(folderPath, $"{baseName}_TreeMesh.asset");
            AssetDatabase.CreateAsset(finalTreeMesh, finalMeshPath);
            Debug.Log($"Final tree mesh saved at: {finalMeshPath}");

            // Apply the final mesh to the root tree object
            MeshFilter treeMeshFilter = trunkObject.GetComponent<MeshFilter>() ?? trunkObject.AddComponent<MeshFilter>();
            treeMeshFilter.sharedMesh = finalTreeMesh;

            // Assign trunk materials
            MeshRenderer treeRenderer = trunkObject.GetComponent<MeshRenderer>() ?? trunkObject.AddComponent<MeshRenderer>();
            treeRenderer.sharedMaterials = trunkMeshFilter.GetComponent<MeshRenderer>().sharedMaterials;

            // Clean up hierarchy: Remove `TreeBranches` and `TreeBranchlets`
            if (branchesParent != null) DestroyImmediate(branchesParent.gameObject);
            if (branchletsParent != null) DestroyImmediate(branchletsParent.gameObject);

            // Save the prefab
            PrefabUtility.SaveAsPrefabAssetAndConnect(trunkObject, savePath, InteractionMode.UserAction);
            Debug.Log($"Tree prefab saved at: {savePath}");
        }


        private CombineInstance[] CollectBranchMeshes()
    {
        List<CombineInstance> combineInstances = new List<CombineInstance>();

        if (branchesParent != null)
        {
            foreach (Transform branchTransform in branchesParent.transform)
            {
                MeshFilter branchMeshFilter = branchTransform.GetComponent<MeshFilter>();
                if (branchMeshFilter != null && branchMeshFilter.sharedMesh != null)
                {
                    CombineInstance combineInstance = new CombineInstance
                    {
                        mesh = branchMeshFilter.sharedMesh,
                        transform = branchTransform.localToWorldMatrix
                    };
                    combineInstances.Add(combineInstance);
                }
            }
        }

        if (branchletsParent != null)
        {
            foreach (Transform branchletTransform in branchletsParent.transform)
            {
                MeshFilter branchletMeshFilter = branchletTransform.GetComponent<MeshFilter>();
                if (branchletMeshFilter != null && branchletMeshFilter.sharedMesh != null)
                {
                    CombineInstance combineInstance = new CombineInstance
                    {
                        mesh = branchletMeshFilter.sharedMesh,
                        transform = branchletTransform.localToWorldMatrix
                    };
                    combineInstances.Add(combineInstance);
                }
            }
        }

        return combineInstances.ToArray();
    }

    private void LoadLeafModels()
    {
        string leafModelPath = "Assets/MysticForge ProceduralTreeGenerator/LeafModels";
        string[] modelGuids = AssetDatabase.FindAssets("t:GameObject", new[] { leafModelPath });

        leafModels = new GameObject[modelGuids.Length];

        for (int i = 0; i < modelGuids.Length; i++)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(modelGuids[i]);
            leafModels[i] = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
        }
    }

    private void DisplayLeafModelSelection()
    {
        EditorGUILayout.BeginVertical(GUILayout.Width(200)); // Adjust width if needed

        showLeafModels = EditorGUILayout.Foldout(showLeafModels, "Select a Leaf Model");
        if (showLeafModels)
        {
            foreach (var model in leafModels)
            {
                if (model == null) continue;

                Texture previewTexture = AssetPreview.GetAssetPreview(model);

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button(previewTexture, GUILayout.Width(75), GUILayout.Height(75)))
                {
                    leafPrefab = model; // Assign selected model
                }
                EditorGUILayout.LabelField(model.name, EditorStyles.miniLabel, GUILayout.Width(100)); // Use model.name directly
                EditorGUILayout.EndHorizontal();
            }
        }

        EditorGUILayout.EndVertical();
    }

    private void LoadMaterials()
    {
        urpTrunkMaterials = LoadMaterialsFromPath("Assets/MysticForge ProceduralTreeGenerator/Materials/URP/Bark");
        urpLeafMaterials = LoadMaterialsFromPath("Assets/MysticForge ProceduralTreeGenerator/Materials/URP/Leaves");

        hdrpTrunkMaterials = LoadMaterialsFromPath("Assets/MysticForge ProceduralTreeGenerator/Materials/HDRP/Bark");
        hdrpLeafMaterials = LoadMaterialsFromPath("Assets/MysticForge ProceduralTreeGenerator/Materials/HDRP/Leaves");

        srpTrunkMaterials = LoadMaterialsFromPath("Assets/MysticForge ProceduralTreeGenerator/Materials/SRP/Bark");
        srpLeafMaterials = LoadMaterialsFromPath("Assets/MysticForge ProceduralTreeGenerator/Materials/SRP/Leaves");
    }

    private Material[] LoadMaterialsFromPath(string path)
    {
        if (!Directory.Exists(path))
            return new Material[0];

        return Directory.GetFiles(path, "*.mat")
            .Select(AssetDatabase.LoadAssetAtPath<Material>)
            .Where(mat => mat != null)
            .ToArray();
    }

    private void DisplayMaterialList(Material[] materials, string label, ref Material selectedMaterial)
    {
        EditorGUILayout.LabelField(label, EditorStyles.boldLabel);

        int columns = 4; // Materials per row
        int count = 0;

        EditorGUILayout.BeginHorizontal();

        foreach (var mat in materials)
        {
            if (count % columns == 0 && count != 0)
            {
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
            }

            if (mat == null) continue;

            Texture previewTexture = AssetPreview.GetAssetPreview(mat);

            EditorGUILayout.BeginVertical(GUILayout.Width(80));

            // Create a button using the material preview as an image
            if (GUILayout.Button(previewTexture, GUILayout.Width(75), GUILayout.Height(75)))
            {
                selectedMaterial = mat; // Assign clicked material
            }

            EditorGUILayout.LabelField(mat.name, EditorStyles.miniLabel, GUILayout.Width(75));
            EditorGUILayout.EndVertical();

            count++;
        }

        EditorGUILayout.EndHorizontal();
    }



    private void GenerateTreeTrunk(ref int vertexCount,ref int triangleCount,ref int edgeCount, int trunkSubdivision, int trunkSegments, float trunkBending, float trunkHeight, float trunkRadiusCurvature, float trunkRadius, float treeStumpStartPoint, float treeStumpWidth, bool includeStump, Vector3 spawnPosition)
    {
        if (trunkObject != null)
        {
            DestroyImmediate(trunkObject);
        }

        trunkObject = new GameObject("Generated Tree (unsaved)");
        trunkObject.transform.position = spawnPosition;

            MeshFilter meshFilter = trunkObject.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = trunkObject.AddComponent<MeshRenderer>();

        if (trunkMaterial != null)
        {
            meshRenderer.sharedMaterial = trunkMaterial;
        }
        else
        {
            meshRenderer.sharedMaterial = new Material(Shader.Find("Standard"));
            meshRenderer.sharedMaterial.color = new Color(0.55f, 0.27f, 0.07f);
        }
        List<Vector3> trunkBendPositions = new List<Vector3>();

        meshFilter.mesh = CreateTrunkMesh(ref vertexCount,ref triangleCount,ref edgeCount, trunkSubdivision, trunkSegments, trunkBending, trunkHeight, trunkRadiusCurvature, trunkRadius, trunkBendPositions, includeStump);

        trunk = new Trunk(trunkBendPositions);

        SceneView.RepaintAll();
    }

    private Mesh CreateTrunkMesh(ref int vertexCount, ref int triangleCount, ref int edgeCount, int trunkSubdivision, int trunkSegments, float trunkBending, float trunkHeight, float trunkRadiusCurvature, float trunkRadius, List<Vector3> trunkBendPositions, bool includeStump)
    {
        Mesh mesh = new Mesh();

        int radialSegments = Mathf.Max(6, 3 + trunkSubdivision);
        int horizontalSegments = Mathf.Max(2, trunkSegments);
        int verticesCount = (radialSegments + 1) * (horizontalSegments + 1) + 1;
        Vector3[] vertices = new Vector3[verticesCount];
        int[] triangles = new int[radialSegments * horizontalSegments * 6 + radialSegments * 3];
        Vector2[] uvs = new Vector2[verticesCount];
        Color[] colors = new Color[verticesCount]; // Define vertex colors

        float topRadius = trunkRadius * Mathf.Clamp01(1 - trunkRadiusCurvature);
        float bottomRadius = trunkRadius * Mathf.Clamp01(1 + trunkRadiusCurvature);

        float randomBending = Random.Range(15f, 45f);

        Vector3 previousPosition = Vector3.zero;

        for (int y = 0; y <= horizontalSegments; y++)
        {
            float heightFraction = (float)y / horizontalSegments;
                float radius;

                if (includeStump && heightFraction < treeStumpStartPoint)
                {
                    float stumpFactor = 1f - (heightFraction / treeStumpStartPoint);
                    float exaggeratedStumpScale = 1f + stumpFactor * treeStumpWidth; // Adjust '2f' to control how dramatic the widening is
                    float baseRadius = Mathf.Lerp(bottomRadius, topRadius, heightFraction);
                    radius = baseRadius * exaggeratedStumpScale;
                }
                else
                {
                    radius = Mathf.Lerp(bottomRadius, topRadius, heightFraction);
                }

                float bendOffset = Mathf.Sin(heightFraction * Mathf.PI * randomBending * trunkBending) * trunkHeight * Mathf.Abs(trunkBending);

            Vector3 bendPosition = new Vector3(0, heightFraction * trunkHeight, 0) + new Vector3(bendOffset, 0, 0);

            // Store the bend position for later use
            trunkBendPositions.Add(bendPosition);

            for (int x = 0; x <= radialSegments; x++)
            {
                float angle = Mathf.PI * 2 * x / radialSegments;

                float twist = trunkCrinkliness * heightFraction * Mathf.PI * 2;
                float crinkledAngle = angle + twist;

                float xPos = Mathf.Cos(crinkledAngle);
                float zPos = Mathf.Sin(crinkledAngle);

                float noiseFactor = 1 + (Mathf.PerlinNoise(xPos * trunkRadiusNoise + y, zPos * trunkRadiusNoise + y) - 0.5f) * trunkRadiusNoise;
                xPos *= radius * noiseFactor;
                zPos *= radius * noiseFactor;

                int index = y * (radialSegments + 1) + x;
                vertices[index] = new Vector3(xPos + bendOffset, heightFraction * trunkHeight, zPos);

                float u = (float)x / radialSegments;
                float v = heightFraction * trunkHeight;
                uvs[index] = new Vector2(u, v);

                // Assign blue color to each vertex
                colors[index] = Color.blue;
            }
        }

        int topRingStartIndex = horizontalSegments * (radialSegments + 1);
        Vector3 topVertexPosition = vertices[topRingStartIndex];

        int pointedTopIndex = verticesCount - 1;
        Vector3 pointedTopPosition = topVertexPosition + new Vector3(0, 0f, 0);

        vertices[pointedTopIndex] = pointedTopPosition;
        uvs[pointedTopIndex] = new Vector2(0f, 1);
        colors[pointedTopIndex] = Color.blue; // Assign blue color to the top vertex

        int triIndex = 0;
        for (int y = 0; y < horizontalSegments; y++)
        {
            for (int x = 0; x < radialSegments; x++)
            {
                int current = y * (radialSegments + 1) + x;
                int next = current + 1;
                int above = current + radialSegments + 1;
                int aboveNext = above + 1;

                triangles[triIndex++] = current;
                triangles[triIndex++] = above;
                triangles[triIndex++] = next;

                triangles[triIndex++] = next;
                triangles[triIndex++] = above;
                triangles[triIndex++] = aboveNext;
            }
        }

        for (int x = 0; x < radialSegments; x++)
        {
            int current = topRingStartIndex + x;
            int next = current + 1;

            triangles[triIndex++] = next;
            triangles[triIndex++] = current;

            triangles[triIndex++] = pointedTopIndex;
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.colors = colors; // Assign colors to the mesh
        mesh.RecalculateNormals();

        vertexCount = mesh.vertexCount;
        triangleCount = mesh.triangles.Length / 3;
        edgeCount = CalculateEdges(mesh);

        return mesh;
    }


    private int CalculateEdges(Mesh mesh)
    {
        HashSet<(int, int)> edges = new HashSet<(int, int)>();
        int[] triangles = mesh.triangles;

        for (int i = 0; i < triangles.Length; i += 3)
        {
            int a = triangles[i];
            int b = triangles[i + 1];
            int c = triangles[i + 2];

            edges.Add((Mathf.Min(a, b), Mathf.Max(a, b)));
            edges.Add((Mathf.Min(b, c), Mathf.Max(b, c)));
            edges.Add((Mathf.Min(c, a), Mathf.Max(c, a)));
        }

        return edges.Count;
    }


    public void OnSceneGUI(SceneView sceneView)
    {
        if (trunkObject != null && !EditorGUIUtility.editingTextField) // Don't override when typing in the Inspector
        {
        Vector3 newPosition = trunkObject.transform.position;

        if (newPosition != spawnPosition)
        {
            spawnPosition = newPosition;
            Repaint(); // Update the Inspector UI when moved in Scene View
        }
        }

        if(showTreePath == true)
        {

        
        // Ensure branches is not null or empty
        //if (trunks == null || trunks.Count == 0) return;

        // Loop through each branch in the branches list
        // Assuming 'trunk' is your single Trunk object
        if (trunk.trunkBendPositions != null && trunk.trunkBendPositions.Count > 0)
        {
            for (int i = 0; i < trunk.trunkBendPositions.Count; i++)
            {
                Vector3 position = trunk.trunkBendPositions[i];
                Handles.color = Color.green;

                // Draw a sphere at each bend position
                Handles.SphereHandleCap(0, position, Quaternion.identity, 0.1f, EventType.Repaint);

                // Draw lines connecting the positions
                if (i > 0)
                {
                    Handles.DrawLine(trunk.trunkBendPositions[i - 1], position);
                }
            }
        }



        // Ensure branches is not null or empty
        if (branches == null || branches.Count == 0) return;

        // Loop through each branch in the branches list
        foreach (var branch in branches)
        {
            // Check if bendPositions for the current branch exists and has elements
            if (branch.bendPositions == null || branch.bendPositions.Count == 0) continue;

            // Draw each bend position for the current branch
            for (int i = 0; i < branch.bendPositions.Count; i++)
            {
                Vector3 position = branch.bendPositions[i];
                Handles.color = Color.red;

                // Draw a sphere at each bend position
                Handles.SphereHandleCap(0, position, Quaternion.identity, 0.1f, EventType.Repaint);

                // Draw lines connecting the positions
                if (i > 0)
                {
                    Handles.DrawLine(branch.bendPositions[i - 1], position);
                }
            }
        }

        // Ensure branches is not null or empty
        if (branchlets == null || branchlets.Count == 0) return;

        // Loop through each branch in the branches list
        foreach (var branchlet in branchlets)
        {
            // Check if bendPositions for the current branch exists and has elements
            if (branchlet.bendPositions == null || branchlet.bendPositions.Count == 0) continue;

            // Draw each bend position for the current branch
            for (int i = 0; i < branchlet.bendPositions.Count; i++)
            {
                Vector3 position = branchlet.bendPositions[i];
                Handles.color = Color.blue;

                // Draw a sphere at each bend position
                Handles.SphereHandleCap(0, position, Quaternion.identity, 0.1f, EventType.Repaint);

                // Draw lines connecting the positions
                if (i > 0)
                {
                    Handles.DrawLine(branchlet.bendPositions[i - 1], position);
                }
            }
        }

        SceneView.RepaintAll();
        }
    }


    private List<Vector3> branchPositions = new List<Vector3>();
    private void GenerateTreeBranches(float trunkHeight, float branchLength, float branchAngle, float branchBending, float trunkBending, float trunkRadius, float trunkRadiusCurvature, float gravity,bool angleAdjustmentByHeight, bool adjustBranchLengthByHeight, int branchSegments, int branchSubdivision, ref int vertexBranchCount, ref int triangleBranchCount, ref int edgeBranchCount)
    {
        vertexBranchCount = 0;
        triangleBranchCount = 0;
        edgeBranchCount = 0;
        if (branchesParent != null)
        {
            DestroyImmediate(branchesParent);
        }

        branchesParent = new GameObject("TreeBranches");
        branchesParent.transform.SetParent(trunkObject.transform);

        //Mesh trunkMesh = CreateTrunkMesh(trunkSubdivision, trunkSegments, trunkBending);

        branches.Clear();

        for (int i = 0; i < numberOfBranches; i++)
        {
            float height = Random.Range(branchHeightMin * trunkHeight, branchHeightMax * trunkHeight);

            float adjustedBranchLength = branchLength;
            if (adjustBranchLengthByHeight == true)
            {
                float normalizedHeight = height / trunkHeight;
                adjustedBranchLength = Mathf.Lerp(branchLength, branchLength / 5f, normalizedHeight);
            }

            float trunkRadiusAtHeight = Mathf.Lerp(
                trunkRadius * Mathf.Clamp01(1 + trunkRadiusCurvature),
                trunkRadius * Mathf.Clamp01(1 - trunkRadiusCurvature),
                height / trunkHeight
            );

            float adjustedBranchRadius = Mathf.Min(branchRadius, trunkRadiusAtHeight);

            float normalizedHeightAlongTrunk = height / trunkHeight;
            int index1 = Mathf.FloorToInt(normalizedHeightAlongTrunk * (trunk.trunkBendPositions.Count - 1));
            int index2 = Mathf.Min(index1 + 1, trunk.trunkBendPositions.Count - 1);

            // Interpolate between two points along the trunk bend positions
            Vector3 trunkBendPosition1 = trunk.trunkBendPositions[index1];
            Vector3 trunkBendPosition2 = trunk.trunkBendPositions[index2];
            float t = normalizedHeightAlongTrunk * (trunk.trunkBendPositions.Count - 1) - index1;

            Vector3 branchPosition = Vector3.Lerp(trunkBendPosition1, trunkBendPosition2, t) + trunkObject.transform.position;
                branchPosition.y = height + trunkObject.transform.position.y;  // Correct vertical placement

            float adjustedBranchAngle = branchAngle;
            if (angleAdjustmentByHeight)
            {
                float normalizedHeight = height / trunkHeight;
                float heightBasedAdjustment = normalizedHeight * 30f; // Maximum adjustment value
                adjustedBranchAngle = Mathf.Min(branchAngle + heightBasedAdjustment, 160f);
            }
            float segmentAngle = 360f / numberOfBranches;
            float baseRotationAngle = i * segmentAngle;
            float randomVariation = Random.Range(-segmentAngle / 4f, segmentAngle / 4f);
            float randomRotationAngle = baseRotationAngle + randomVariation;

            Quaternion randomRotation = Quaternion.Euler(adjustedBranchAngle, randomRotationAngle, 0f);
            Vector3 direction = Vector3.forward;
            direction = Quaternion.Euler(-90f, 0f, 0f) * direction;
            direction = randomRotation * direction;

            List<Vector3> bendPositions = new List<Vector3>();

            GameObject branch = new GameObject("Branch" + i);
            branch.transform.SetParent(branchesParent.transform);
            branch.transform.position = branchPosition;
            branch.transform.up = direction;
            branch.transform.rotation = randomRotation;

            MeshFilter meshFilter = branch.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = branch.AddComponent<MeshRenderer>();

            meshRenderer.sharedMaterial = trunkMaterial;
            meshFilter.mesh = CreateBranchMesh(gravity, adjustedBranchRadius, adjustedBranchLength, branchBending, direction, branchPosition, randomRotation, bendPositions, branchSegments, branchSubdivision, ref vertexBranchCount, ref triangleBranchCount, ref edgeBranchCount);

            branches.Add(new Branch(branchPosition, direction, adjustedBranchLength, adjustedBranchRadius, randomRotation, bendPositions));

            SceneView.RepaintAll();
        }
    }

    private Vector3 GetBranchPosition(float height, float trunkHeight, float trunkBending, float trunkRadius, float trunkRadiusCurvature)
    {
        float heightFraction = height / trunkHeight;

        float bendOffset = Mathf.Sin(heightFraction * Mathf.PI * 30 * trunkBending) * trunkHeight * Mathf.Abs(trunkBending);

        float radius = Mathf.Lerp(trunkRadius * Mathf.Clamp01(1 + trunkRadiusCurvature), trunkRadius * Mathf.Clamp01(1 - trunkRadiusCurvature), heightFraction);

        Vector3 surfaceOffset = new Vector3(
            Random.Range(-1f, 1f),
            0f,
            Random.Range(-1f, 1f)
        ).normalized * radius;

        Vector3 branchPosition = new Vector3(
            bendOffset,
            height,
            0f
        );

        return branchPosition;
    }

    private Mesh CreateBranchMesh(float gravity, float adjustedBranchRadius, float adjustedBranchLength, float branchBending, Vector3 direction, Vector3 branchPosition, Quaternion randomRotation, List<Vector3> bendPositions, int branchSegments, int branchSubdivision, ref int vertexBranchCount, ref int triangleBranchCount, ref int edgeBranchCount)
    {
        Mesh mesh = new Mesh();

        int radialSegments = Mathf.Max(6, 3 + branchSubdivision);
        int horizontalSegments = Mathf.Max(2, branchSegments);
        int verticesCount = (radialSegments + 1) * (horizontalSegments + 1) + 1;
        Vector3[] vertices = new Vector3[verticesCount];
        int[] triangles = new int[radialSegments * horizontalSegments * 6 + radialSegments * 3];
        Vector2[] uvs = new Vector2[verticesCount];

        float bendNoiseSeed = Random.Range(0f, 100f) + branchPosition.x + branchPosition.y + branchPosition.z;

        float topRadius = adjustedBranchRadius * Mathf.Clamp01(1 - branchRadiusCurvature);
        float bottomRadius = adjustedBranchRadius * Mathf.Clamp01(1 + branchRadiusCurvature);

        for (int y = 0; y <= horizontalSegments; y++)
        {
            float heightFraction = (float)y / horizontalSegments;
            float radius = Mathf.Lerp(bottomRadius, topRadius, heightFraction);

            float bendNoise1 = Mathf.PerlinNoise(heightFraction * 10f, bendNoiseSeed);
            float bendNoise2 = Mathf.PerlinNoise(heightFraction * 15f, bendNoiseSeed + 5f);
            float combinedNoise = Mathf.Lerp(bendNoise1, bendNoise2, 0.5f);
            float randomOffset = Random.Range(-0.02f, 0.02f);

            float bendOffset = Mathf.Sin(heightFraction * Mathf.PI * 30 * branchBending * combinedNoise) * adjustedBranchLength * Mathf.Abs(branchBending) * 0.5f + randomOffset;

            float gravityBend = Mathf.Sin(heightFraction * Mathf.PI) * gravity;

            // Create a local offset for bending along the X-axis
            Vector3 localBendOffset = new Vector3(bendOffset, 0f, gravityBend);

            //Vector3 localBendOffset = new Vector3(bendOffset, Mathf.Sin(heightFraction * Mathf.PI) * gravity, 0f);

            // Transform the local offset into world space using the branch's rotation
            Vector3 worldBendOffset = randomRotation * localBendOffset;

            // Calculate the bend position
            Vector3 bendPosition = branchPosition + direction * heightFraction * adjustedBranchLength + worldBendOffset;

            bendPositions.Add(bendPosition);

            for (int x = 0; x <= radialSegments; x++)
            {
                float angle = Mathf.PI * 2 * x / radialSegments;

                float twist = branchCrinkliness * heightFraction * Mathf.PI * 2;
                float crinkledAngle = angle + twist;

                float xPos = Mathf.Cos(crinkledAngle);
                float zPos = Mathf.Sin(crinkledAngle);

                float radiusNoise = Mathf.PerlinNoise(xPos * branchRadiusNoise + y, zPos * branchRadiusNoise + y);
                float noiseAdjustedRadius = radius * (1 + (radiusNoise - 0.5f) * branchRadiusNoise);

                xPos *= noiseAdjustedRadius;
                zPos *= noiseAdjustedRadius;

                vertices[y * (radialSegments + 1) + x] = new Vector3(xPos + bendOffset, adjustedBranchLength *heightFraction, zPos + gravityBend);

                float u = (float)x / radialSegments;
                float v = heightFraction * adjustedBranchLength;
                uvs[y * (radialSegments + 1) + x] = new Vector2(u, v);
            }
        }

        int topRingStartIndex = horizontalSegments * (radialSegments + 1);
        Vector3 topVertexPosition = vertices[topRingStartIndex];

        int pointedTipIndex = verticesCount - 1;
        Vector3 pointedTipPosition = topVertexPosition + new Vector3(0, 0f, 0);

        float topRadiusDiameter = topRadius * 2;
        pointedTipPosition.x -= topRadiusDiameter / 2;

        vertices[pointedTipIndex] = pointedTipPosition;
        uvs[pointedTipIndex] = new Vector2(0.5f, 1);

        int triIndex = 0;
        for (int y = 0; y < horizontalSegments; y++)
        {
            for (int x = 0; x < radialSegments; x++)
            {
                int current = y * (radialSegments + 1) + x;
                int next = current + 1;
                int above = current + radialSegments + 1;
                int aboveNext = above + 1;

                triangles[triIndex++] = current;
                triangles[triIndex++] = above;
                triangles[triIndex++] = next;

                triangles[triIndex++] = next;
                triangles[triIndex++] = above;
                triangles[triIndex++] = aboveNext;
            }
        }

        for (int x = 0; x < radialSegments; x++)
        {
            int current = topRingStartIndex + x;
            int next = current + 1;

            triangles[triIndex++] = next;
            triangles[triIndex++] = current;

            triangles[triIndex++] = pointedTipIndex;
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.RecalculateNormals();

        vertexBranchCount += mesh.vertexCount;
        triangleBranchCount += mesh.triangles.Length / 3;
        edgeBranchCount += CalculateEdges(mesh);

        return mesh;
    }


    private void GenerateTreeBranchlets(bool adjustBranchletLengthByHeight, float gravityBranchlets, float branchletAngle, float branchletLength, float branchletForwardAngle, float branchletBending, int branchletSegments, int branchletSubdivision, ref int vertexBranchletCount, ref int triangleBranchletCount, ref int edgeBranchletCount)
    {

        vertexBranchletCount = 0;
        triangleBranchletCount = 0;
        edgeBranchletCount = 0;

        if (branchletsParent != null)
        {
            DestroyImmediate(branchletsParent);
        }

        branchletsParent = new GameObject("TreeBranchlets");
        branchletsParent.transform.SetParent(trunkObject.transform);

        branchlets.Clear();

        int branchletPerBranch = Mathf.FloorToInt((float)numberOfBranchlets / branches.Count);
        int remainderBranchlets = numberOfBranchlets % branches.Count;

        int branchletCounter = 0;

        float minHeight = Mathf.Min(branches.ConvertAll(branch => branch.position.y).ToArray());
        float maxHeight = Mathf.Max(branches.ConvertAll(branch => branch.position.y).ToArray());


        for (int i = 0; i < branches.Count; i++)
        {
            Branch branch = branches[i];
            int branchletCount = branchletPerBranch + (i < remainderBranchlets ? 1 : 0);

            for (int j = 0; j < branchletCount; j++)
            {

                float adjustedBranchletLength = branchletLength;

                // Adjust branchlet length based on height
                if (adjustBranchletLengthByHeight)
                {
                    float normalizedHeight = Mathf.InverseLerp(minHeight, maxHeight, branch.position.y);
                    adjustedBranchletLength = Mathf.Lerp(branchletLength, branchletLength / 3f, normalizedHeight);
                }

                float branchletHeight = Random.Range(branchletHeightMin, branchletHeightMax);
                branchletHeight = Mathf.Clamp01(branchletHeight);

                Vector3 branchletPosition = Vector3.zero;

                if (branch.bendPositions.Count > 1)
                {
                    float totalLength = 0f;
                    List<float> segmentLengths = new List<float>();
                    for (int k = 0; k < branch.bendPositions.Count - 1; k++)
                    {
                        float segmentLength = Vector3.Distance(branch.bendPositions[k], branch.bendPositions[k + 1]);
                        segmentLengths.Add(segmentLength);
                        totalLength += segmentLength;
                    }

                    float targetLength = branchletHeight * totalLength;

                    float accumulatedLength = 0f;
                    for (int k = 0; k < segmentLengths.Count; k++)
                    {
                        if (accumulatedLength + segmentLengths[k] >= targetLength)
                        {
                            float segmentFactor = (targetLength - accumulatedLength) / segmentLengths[k];
                            branchletPosition = Vector3.Lerp(
                                branch.bendPositions[k],
                                branch.bendPositions[k + 1],
                                segmentFactor
                            );

                            break;
                        }
                        accumulatedLength += segmentLengths[k];
                    }
                }
                else
                {
                    branchletPosition = branch.position + branch.direction.normalized * branch.length * branchletHeight;
                }

                Vector3 forwardDirection = branch.direction.normalized;
                if (branch.bendPositions.Count > 1)
                {
                    forwardDirection = (branch.bendPositions[Mathf.Min(branch.bendPositions.Count - 1, 1)] - branch.bendPositions[0]).normalized;
                }
                Quaternion branchRotation = Quaternion.LookRotation(forwardDirection);

                float sideFactor = (j % 2 == 0) ? 1f : -1f;

                float adjustedRotationAngle = sideFactor * branchletForwardAngle;

                Quaternion fixedRotation = branchRotation;

                // Apply the rotation on Y (local space forward angle)
                fixedRotation = Quaternion.AngleAxis(adjustedRotationAngle, branchRotation * Vector3.forward) * fixedRotation;

                // Apply the branchletAngle (pitch) in local space
                fixedRotation = Quaternion.AngleAxis(branchletAngle, branchRotation * Vector3.right) * fixedRotation;


                Vector3 direction = Vector3.up;

                direction = fixedRotation * direction;

                float branchletRadius = CalculateBranchletRadius(branchletPosition, branch);

                float branchletMaxRadius = branchletRadius;

                List<Vector3> bendBranchletPositions = new List<Vector3>();
                
                GameObject branchlet = new GameObject("Branchlet" + branchletCounter++);
                branchlet.transform.SetParent(branchletsParent.transform);
                branchlet.transform.position = branchletPosition;
                branchlet.transform.up = forwardDirection;
                branchlet.transform.rotation = fixedRotation;
                MeshFilter meshFilter = branchlet.AddComponent<MeshFilter>();
                MeshRenderer meshRenderer = branchlet.AddComponent<MeshRenderer>();

                meshRenderer.sharedMaterial = trunkMaterial;
                meshFilter.mesh = CreateBranchletMesh(adjustedBranchletLength, gravityBranchlets, branchletRadius, branchletBending, branchletSegments, branchletSubdivision, ref vertexBranchletCount, ref triangleBranchletCount, ref edgeBranchletCount, direction, branchletPosition, fixedRotation, branchRotation, bendBranchletPositions);

                branchlets.Add(new BranchletsX(branchletPosition, direction, branchletLength, branchletMaxRadius, sideFactor, fixedRotation, bendBranchletPositions));

                SceneView.RepaintAll();
            }
        }
    }

    private float CalculateBranchletRadius(Vector3 branchletPosition, Branch branch)
    {
        float totalLength = 0f;
        List<float> segmentLengths = new List<float>();
        for (int j = 0; j < branch.bendPositions.Count - 1; j++)
        {
            float segmentLength = Vector3.Distance(branch.bendPositions[j], branch.bendPositions[j + 1]);
            segmentLengths.Add(segmentLength);
            totalLength += segmentLength;
        }
        float accumulatedLength = 0f;
        float branchletLengthAlongBranch = 0f;
        for (int j = 0; j < segmentLengths.Count; j++)
        {
            if (accumulatedLength + segmentLengths[j] >= Vector3.Distance(branch.bendPositions[0], branchletPosition))
            {
                branchletLengthAlongBranch = accumulatedLength + (Vector3.Distance(branch.bendPositions[0], branchletPosition) - accumulatedLength);
                break;
            }
            accumulatedLength += segmentLengths[j];
        }
        float heightFraction = branchletLengthAlongBranch / totalLength;

        float topRadius = branch.adjustedBranchRadius * Mathf.Clamp01(1 - branchRadiusCurvature);
        float bottomRadius = branch.adjustedBranchRadius * Mathf.Clamp01(1 + branchRadiusCurvature);

        return Mathf.Lerp(bottomRadius, topRadius, heightFraction);
    }

    private Mesh CreateBranchletMesh(float adjustedBranchletLength, float gravityBranchlets, float branchletMaxRadius, float branchletBending, int branchletSegments, int branchletSubdivision, ref int vertexBranchletCount, ref int triangleBranchletCount, ref int edgeBranchletCount, Vector3 direction, Vector3 branchletPosition, Quaternion fixedRotation, Quaternion branchRotation, List<Vector3> bendBranchletPositions)
    {
        Mesh mesh = new Mesh();

        int radialSegments = Mathf.Max(6, 3 + branchletSubdivision);
        int horizontalSegments = Mathf.Max(2, branchletSegments);
        int verticesCount = (radialSegments + 1) * (horizontalSegments + 1) + 1;
        Vector3[] vertices = new Vector3[verticesCount];
        int[] triangles = new int[radialSegments * horizontalSegments * 6 + radialSegments * 3];
        Vector2[] uvs = new Vector2[verticesCount];

        float topRadius = branchletMaxRadius * Mathf.Clamp01(1 - branchletRadiusCurvature);
        float bottomRadius = branchletMaxRadius * Mathf.Clamp01(1 + branchletRadiusCurvature);

        float branchletBendNoiseSeed = Random.Range(0f, 100f);

        for (int y = 0; y <= horizontalSegments; y++)
        {
            float heightFraction = (float)y / horizontalSegments;
            float radius = Mathf.Lerp(bottomRadius, topRadius, heightFraction);

            float bendNoise = Mathf.PerlinNoise(branchletBendNoiseSeed, heightFraction * 10f);
            float noiseFactor1 = Mathf.Lerp(0.8f, 1.2f, bendNoise); // Adjust variability range
            float bendOffset = Mathf.Sin(heightFraction * Mathf.PI * 30 * branchletBending * noiseFactor1)
                               * adjustedBranchletLength * Mathf.Abs(branchletBending) * 0.5f;

            float gravityBend = Mathf.Sin(heightFraction * Mathf.PI) * gravityBranchlets;

            Vector3 localBendOffset = new Vector3(bendOffset, 0f, gravityBend);

            // Transform the local offset into world space using the branch's rotation
            Vector3 worldBendOffset = fixedRotation * localBendOffset;

            // Calculate the bend position
            Vector3 bendPosition = branchletPosition + direction * heightFraction * adjustedBranchletLength + worldBendOffset;

            bendBranchletPositions.Add(bendPosition);


            for (int x = 0; x <= radialSegments; x++)
            {
                float angle = Mathf.PI * 2 * x / radialSegments;

                float twist = branchletCrinkliness * heightFraction * Mathf.PI * 2;
                float crinkledAngle = angle + twist;

                float xPos = Mathf.Cos(crinkledAngle);
                float zPos = Mathf.Sin(crinkledAngle);

                float noiseFactor = 1 + (Mathf.PerlinNoise(xPos * branchletRadiusNoise + y, zPos * branchletRadiusNoise + y) - 0.5f) * branchletRadiusNoise;
                xPos *= radius * noiseFactor;
                zPos *= radius * noiseFactor;

                vertices[y * (radialSegments + 1) + x] = new Vector3(xPos + bendOffset, heightFraction * adjustedBranchletLength, zPos + gravityBend);

                float u = (float)x / radialSegments;
                float v = heightFraction * adjustedBranchletLength;
                uvs[y * (radialSegments + 1) + x] = new Vector2(u, v);
            }
        }

        int topRingStartIndex = horizontalSegments * (radialSegments + 1);
        Vector3 topVertexPosition = vertices[topRingStartIndex];

        int pointedTipIndex = verticesCount - 1;
        Vector3 pointedTipPosition = topVertexPosition + new Vector3(0, 0f, 0);

        float topRadiusDiameter = topRadius * 2;
        pointedTipPosition.x -= topRadiusDiameter / 2;

        vertices[pointedTipIndex] = pointedTipPosition;
        uvs[pointedTipIndex] = new Vector2(0.5f, 1);

        int triIndex = 0;
        for (int y = 0; y < horizontalSegments; y++)
        {
            for (int x = 0; x < radialSegments; x++)
            {
                int current = y * (radialSegments + 1) + x;
                int next = current + 1;
                int above = current + radialSegments + 1;
                int aboveNext = above + 1;

                triangles[triIndex++] = current;
                triangles[triIndex++] = above;
                triangles[triIndex++] = next;

                triangles[triIndex++] = next;
                triangles[triIndex++] = above;
                triangles[triIndex++] = aboveNext;
            }
        }

        for (int x = 0; x < radialSegments; x++)
        {
            int current = topRingStartIndex + x;
            int next = current + 1;
            triangles[triIndex++] = next;
            triangles[triIndex++] = current;

            triangles[triIndex++] = pointedTipIndex;
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.RecalculateNormals();

        vertexBranchletCount += mesh.vertexCount;
        triangleBranchletCount += mesh.triangles.Length / 3;
        edgeBranchletCount += CalculateEdges(mesh);

        return mesh;
    }

    private List<Trunk> trunks = new List<Trunk>();
    private class Trunk
    {
        /*public Vector3 position;
        public Vector3 direction;
        public float length;
        public float adjustedBranchRadius;
        public Quaternion randomRotation;*/
        public List<Vector3> trunkBendPositions;

        public Trunk(List<Vector3> trunkBendPositions)
        {
            /*this.position = position;
            this.direction = direction;
            this.length = length;
            this.randomRotation = randomRotation;*/
            this.trunkBendPositions = trunkBendPositions;
            //this.adjustedBranchRadius = adjustedBranchRadius;
        }
    }

    private List<Branch> branches = new List<Branch>();
    private class Branch
    {
        public Vector3 position;
        public Vector3 direction;
        public float length;
        public float adjustedBranchRadius;
        public Quaternion randomRotation;
        public List<Vector3> bendPositions;

        public Branch(Vector3 position, Vector3 direction, float length, float adjustedBranchRadius, Quaternion randomRotation, List<Vector3> bendPositions)
        {
            this.position = position;
            this.direction = direction;
            this.length = length;
            this.randomRotation = randomRotation;
            this.bendPositions = bendPositions;
            this.adjustedBranchRadius = adjustedBranchRadius;
        }
    }

    public List<BranchletsX> branchlets = new List<BranchletsX>();
    public class BranchletsX
    {
        public Vector3 branchletPosition { get; set; }
        public Vector3 direction { get; set; }
        public float length { get; set; }
        public float branchletMaxRadius { get; set; }
        public float sideFactor { get; set; }
        public Quaternion fixedRotation;
        public List<Vector3> bendPositions { get; set; }


        public BranchletsX(Vector3 position, Vector3 dir, float len, float branchletMaxRadius, float sideFactor, Quaternion fixedRotation, List<Vector3> bendBranchletPositions)
        {
            this.branchletPosition = position;
            this.direction = dir;
            this.length = len;
            this.branchletMaxRadius = branchletMaxRadius;
            this.sideFactor = sideFactor;
            this.fixedRotation = fixedRotation;
            this.bendPositions = bendBranchletPositions;
        }
    }

    private void GenerateLeafPlanes(Vector3 leafBranchPositioning, Vector3 leafBranchSizeV3, Material leafMaterial, int numberOfLeaves, ref int vertexBranchLeavesCount, ref int triangleBranchLeavesCount, ref int edgeBranchLeavesCount, GameObject leafPrefab, float leafSizeBranchRandom, float leafSize, float leafPositionMin, float leafPositionMax, float leafForwardRotation, float leafRotation, float leafRandomizeRotation, float leafBranchRandomPositioning)
    {
        vertexBranchLeavesCount = 0;
        triangleBranchLeavesCount = 0;
        edgeBranchLeavesCount = 0;

        if (branchesParent == null)
        {
            Debug.LogWarning("Branches must be generated before generating leaves.");
            return;
        }

        // Ensure leafPositionMin and leafPositionMax are within valid bounds
        leafPositionMin = Mathf.Clamp01(leafPositionMin);
        leafPositionMax = Mathf.Clamp01(leafPositionMax);

        if (leafPositionMin > leafPositionMax)
        {
            Debug.LogWarning("leafPositionMin cannot be greater than leafPositionMax. Swapping the values.");
            (leafPositionMin, leafPositionMax) = (leafPositionMax, leafPositionMin);
        }

        // Remove old leaves
        Transform oldLeaves = trunkObject.transform.Find("Leaves Branch");
        if (oldLeaves != null)
        {
            DestroyImmediate(oldLeaves.gameObject);
        }

        // Create a new parent object for leaves
        GameObject leavesParent = new GameObject("Leaves Branch");
        leavesParent.transform.SetParent(trunkObject.transform);
        leavesBranch = leavesParent;

        foreach (Branch branch in branches)
        {
            for (int i = 0; i < numberOfLeaves; i++)
            {
                // Adjust the base leaf rotation with alternating pattern
                float adjustedLeafRotation = (i % 2 == 0) ? leafRotation : -leafRotation;

                // Add randomness to rotations based on leafRandomizeRotation
                float randomizedLeafForwardRotation = leafForwardRotation + Random.Range(-360f, 360f) * leafRandomizeRotation;
                float randomizedLeafRotation = adjustedLeafRotation + Random.Range(-180f, 180f) * leafRandomizeRotation;

                // Calculate a random normalized position within the specified range
                float t = Random.Range(leafPositionMin, leafPositionMax);

                // Determine the segment of the branch and the local position within that segment
                int segmentCount = branch.bendPositions.Count - 1;
                int segmentIndex = Mathf.FloorToInt(t * segmentCount);
                float segmentT = (t * segmentCount) - segmentIndex;

                // Clamp the segment index to avoid out-of-range errors
                segmentIndex = Mathf.Clamp(segmentIndex, 0, segmentCount - 1);
                /*
                // Interpolate between the current and next bend positions
                Vector3 leafPosition = Vector3.Lerp(
                    branch.bendPositions[segmentIndex],
                    branch.bendPositions[segmentIndex + 1],
                    segmentT
                );*/

                Vector3 leafPosition = Vector3.zero;

                // Handle edge case where t = 1 to place the leaf at the last bend position
                if (Mathf.Approximately(t, 1f))
                {
                    leafPosition = branch.bendPositions[branch.bendPositions.Count - 1];
                }
                else
                {
                    /*// Normal interpolation for t in range [0, 1)
                    int segmentCount = branch.bendPositions.Count - 1;
                    int segmentIndex = Mathf.FloorToInt(t * segmentCount);
                    float segmentT = (t * segmentCount) - segmentIndex;*/

                    // Interpolate between the current and next bend positions
                    leafPosition = Vector3.Lerp(
                        branch.bendPositions[segmentIndex],
                        branch.bendPositions[segmentIndex + 1],
                        segmentT
                    );
                }


                leafPosition += leafBranchPositioning;

                // Calculate the direction of the branch at the current position
                Vector3 localDirection = (branch.bendPositions[segmentIndex + 1] - branch.bendPositions[segmentIndex]).normalized;

                //leafBranchletPosition += bendOffset;
                // Offset the leaf position radially around the branch

                /*Vector3 radiusOffset = Vector3.Cross(localDirection, Vector3.up).normalized * branch.adjustedBranchRadius;
                leafPosition += radiusOffset;*/

                // Calculate the leaf's rotation to align with the branch direction
                Quaternion branchRotation = Quaternion.LookRotation(localDirection);
                Quaternion customRotation = Quaternion.Euler(randomizedLeafForwardRotation, randomizedLeafRotation, 0f);
                Quaternion finalRotation = branchRotation * customRotation;

                // Create the leaf object
                GameObject leaf = Instantiate(leafPrefab);
                leaf.name = "Leaf";
                leaf.transform.SetParent(leavesParent.transform);

                // Set the leaf's position and rotation
                leaf.transform.position = leafPosition;
                leaf.transform.rotation = finalRotation;

                float randomSizeMultiplier = 1 + (Random.Range(-leafSizeBranchRandom, leafSizeBranchRandom));
                leaf.transform.localScale = leafBranchSizeV3 * leafSize * randomSizeMultiplier;

                // Assign the leaf material
                MeshRenderer leafRenderer = leaf.GetComponent<MeshRenderer>();
                leafRenderer.sharedMaterial = leafMaterial;

                leaf.transform.position += new Vector3(
                    Random.Range(0f, leafBranchRandomPositioning),
                    Random.Range(0f, leafBranchRandomPositioning),
                    Random.Range(0f, leafBranchRandomPositioning)
                );

                MeshFilter leafMeshFilter = leaf.GetComponent<MeshFilter>();
                if (leafMeshFilter != null && leafMeshFilter.sharedMesh != null)
                {
                    Mesh leafMesh = leafMeshFilter.sharedMesh;

                    vertexBranchLeavesCount += leafMesh.vertexCount;
                    triangleBranchLeavesCount += leafMesh.triangles.Length / 3;
                    edgeBranchLeavesCount += CalculateEdges(leafMesh); // Helper method to calculate edges
                }
            }
        }
    }

    private void GenerateLeafBranchletPlanes(Vector3 leafBranchletPositioning, Vector3 leafBranchletSizeV3, Material leafMaterial, int numberOfLeavesBranchlet, ref int vertexBranchletLeavesCount, ref int triangleBranchletLeavesCount, ref int edgeBranchletLeavesCount, GameObject leafPrefab, float leafSizeBranchletRandom, float leafBranchletSize, float leafBranchletPositionMin, float leafBranchletPositionMax, float leafBranchletForwardRotation, float leafBranchletRotation, float leafBranchletRandomizeRotation, float leafBranchletRandomPositioning)
    {
        vertexBranchletLeavesCount = 0;
        triangleBranchletLeavesCount = 0;
        edgeBranchletLeavesCount = 0;

        if (branchesParent == null)
        {
            Debug.LogWarning("Branches must be generated before generating leaves.");
            return;
        }

        leafBranchletPositionMin = Mathf.Clamp01(leafBranchletPositionMin);
        leafBranchletPositionMax = Mathf.Clamp01(leafBranchletPositionMax);

        if (leafBranchletPositionMin > leafBranchletPositionMax)
        {
            Debug.LogWarning("leafPositionMin cannot be greater than leafPositionMax. Swapping the values.");
            (leafBranchletPositionMin, leafBranchletPositionMax) = (leafBranchletPositionMax, leafBranchletPositionMin);
        }

        Transform oldLeaves = trunkObject.transform.Find("Leaves Branchlet");
        if (oldLeaves != null)
        {
            DestroyImmediate(oldLeaves.gameObject);
        }

        GameObject leavesParent = new GameObject("Leaves Branchlet");
        leavesParent.transform.SetParent(trunkObject.transform);
        leavesBranchlet = leavesParent;

        foreach (BranchletsX branchlet in branchlets)
        {
            for (int i = 0; i < numberOfLeavesBranchlet; i++)
            {
                float adjustedLeafRotation = (i % 2 == 0) ? leafBranchletRotation : -leafBranchletRotation;

                float randomizedLeafForwardRotation = leafBranchletForwardRotation + Random.Range(-360f, 360f) * leafBranchletRandomizeRotation;
                float randomizedLeafRotation = adjustedLeafRotation + Random.Range(-180f, 180f) * leafBranchletRandomizeRotation;

                float t = Random.Range(leafBranchletPositionMin, leafBranchletPositionMax);

                int segmentCount = branchlet.bendPositions.Count - 1;
                int segmentIndex = Mathf.FloorToInt(t * segmentCount);
                float segmentT = (t * segmentCount) - segmentIndex;

                segmentIndex = Mathf.Clamp(segmentIndex, 0, segmentCount - 1);

                Vector3 leafBranchletPosition = Vector3.zero;

                if (Mathf.Approximately(t, 1f))
                {
                    leafBranchletPosition = branchlet.bendPositions[branchlet.bendPositions.Count - 1];
                }
                else
                {
                    leafBranchletPosition = Vector3.Lerp(
                        branchlet.bendPositions[segmentIndex],
                        branchlet.bendPositions[segmentIndex + 1],
                        segmentT
                    );
                }

                leafBranchletPosition += leafBranchletPositioning;


                Quaternion leafBranchletRotationQuaternion = branchlet.fixedRotation;

                leafBranchletRotationQuaternion = Quaternion.Euler(randomizedLeafRotation, randomizedLeafForwardRotation, 0f);

                Vector3 localDirection = (branchlet.bendPositions[segmentIndex + 1] - branchlet.bendPositions[segmentIndex]).normalized;

                Quaternion branchletRotation = Quaternion.LookRotation(localDirection);
                Quaternion customRotation = Quaternion.Euler(randomizedLeafForwardRotation, randomizedLeafRotation, 0f);
                Quaternion finalRotation = branchletRotation * customRotation;


                Vector3 leafDirection = branchlet.direction.normalized;

                leafDirection = leafBranchletRotationQuaternion * leafDirection;

                GameObject leaf = Instantiate(leafPrefab);
                leaf.name = "Leaf";
                leaf.transform.SetParent(leavesParent.transform);

                // Set the leaf's position and rotation
                leaf.transform.position = leafBranchletPosition;
                leaf.transform.up = leafDirection;
                leaf.transform.rotation = finalRotation;


                float randomSizeMultiplier = 1 + (Random.Range(-leafSizeBranchletRandom, leafSizeBranchletRandom));
                leaf.transform.localScale = leafBranchletSizeV3 * leafBranchletSize * randomSizeMultiplier;
                

                // Assign the leaf material
                MeshRenderer leafRenderer = leaf.GetComponent<MeshRenderer>();
                leafRenderer.sharedMaterial = leafMaterial;

                // Optional: Add slight random offset to the position
                leaf.transform.position += new Vector3(
                    Random.Range(0f, leafBranchletRandomPositioning),
                    Random.Range(0f, leafBranchletRandomPositioning),
                    Random.Range(0f, leafBranchletRandomPositioning)
                );

                MeshFilter leafMeshFilter = leaf.GetComponent<MeshFilter>();
                if (leafMeshFilter != null && leafMeshFilter.sharedMesh != null)
                {
                    Mesh leafMesh = leafMeshFilter.sharedMesh;

                    vertexBranchletLeavesCount += leafMesh.vertexCount;
                    triangleBranchletLeavesCount += leafMesh.triangles.Length / 3;
                    edgeBranchletLeavesCount += CalculateEdges(leafMesh); // Helper method to calculate edges
                }
            }
        }
    }

    private void GenerateLeafTrunkPlanes(Vector3 leafTrunkPositioning, Vector3 leafSizeTrunkV3, Material leafMaterial, int numberOfLeavesTrunk, ref int vertexTrunkLeavesCount, ref int triangleTrunkLeavesCount, ref int edgeTrunkLeavesCount, GameObject leafPrefab, float leafSizeTrunkRandom, float leafTrunkSize, float leafTrunkPositionMin, float leafTrunkPositionMax, float leafTrunkForwardRotation, float leafTrunkRotation, float leafTrunkRandomizeRotation, float leafTrunkRandomPositioning)
    {
        vertexTrunkLeavesCount = 0;
        triangleTrunkLeavesCount = 0;
        edgeTrunkLeavesCount = 0;

        if (branchesParent == null)
        {
            Debug.LogWarning("Trunk must be generated before generating leaves.");
            return;
        }

        leafTrunkPositionMin = Mathf.Clamp01(leafTrunkPositionMin);
        leafTrunkPositionMax = Mathf.Clamp01(leafTrunkPositionMax);

        if (leafTrunkPositionMin > leafTrunkPositionMax)
        {
            Debug.LogWarning("leafTrunkPositionMin cannot be greater than leafTrunkPositionMax. Swapping the values.");
            (leafTrunkPositionMin, leafTrunkPositionMax) = (leafTrunkPositionMax, leafTrunkPositionMin);
        }

        Transform oldLeaves = trunkObject.transform.Find("Leaves Trunk");
        if (oldLeaves != null)
        {
            DestroyImmediate(oldLeaves.gameObject);
        }

        GameObject leavesParent = new GameObject("Leaves Trunk");
        leavesParent.transform.SetParent(trunkObject.transform);
        leavesTrunk = leavesParent;

        for (int i = 0; i < numberOfLeavesTrunk; i++)
        {
            float adjustedLeafRotation = (i % 2 == 0) ? leafTrunkRotation : -leafTrunkRotation;

            float randomizedLeafForwardRotation = leafTrunkForwardRotation + Random.Range(-360f, 360f) * leafTrunkRandomizeRotation;
            float randomizedLeafRotation = adjustedLeafRotation + Random.Range(-180f, 180f) * leafTrunkRandomizeRotation;

            float t = Random.Range(leafTrunkPositionMin, leafTrunkPositionMax);

            int segmentCount = trunk.trunkBendPositions.Count - 1;
            int index1 = Mathf.FloorToInt(t * segmentCount);
            int index2 = Mathf.Clamp(index1 + 1, 0, segmentCount);

            float segmentT = (t * segmentCount) - index1;

            Vector3 trunkBendPosition1 = trunk.trunkBendPositions[index1];
            Vector3 trunkBendPosition2 = trunk.trunkBendPositions[index2];
            Vector3 leafTrunkPosition = Vector3.Lerp(trunkBendPosition1, trunkBendPosition2, segmentT) + trunkObject.transform.position;

                // Offset the leaf's position slightly
                leafTrunkPosition += leafTrunkPositioning;

            float segmentAngle = 360f / numberOfLeavesTrunk;
            float baseRotationAngle = i * segmentAngle;
            float randomVariation = Random.Range(-segmentAngle / 4f, segmentAngle / 4f);
            float randomRotationAngle = baseRotationAngle + randomVariation;

            Vector3 localDirection = (trunkBendPosition2 - trunkBendPosition1).normalized;

            Quaternion trunkRotation = Quaternion.LookRotation(localDirection);
            Quaternion customRotation = Quaternion.Euler(randomizedLeafForwardRotation, randomizedLeafRotation, randomRotationAngle);
            Quaternion finalRotation = trunkRotation * customRotation;

            GameObject leaf = Instantiate(leafPrefab);
            leaf.name = "Leaf";
            leaf.transform.SetParent(leavesParent.transform);

            // Set the leaf's position and rotation
            leaf.transform.position = leafTrunkPosition;
            leaf.transform.rotation = finalRotation;

            float randomSizeMultiplier = 1 + (Random.Range(-leafSizeTrunkRandom, leafSizeTrunkRandom));
            leaf.transform.localScale = leafTrunkSizeV3 * leafTrunkSize * randomSizeMultiplier;

            // Assign the leaf material
            MeshRenderer leafRenderer = leaf.GetComponent<MeshRenderer>();
            leafRenderer.sharedMaterial = leafMaterial;

            // Optional: Add slight random offset to the position
            leaf.transform.position += new Vector3(
                Random.Range(0f, leafTrunkRandomPositioning),
                Random.Range(0f, leafTrunkRandomPositioning),
                Random.Range(0f, leafTrunkRandomPositioning)
            );

            MeshFilter leafMeshFilter = leaf.GetComponent<MeshFilter>();
            if (leafMeshFilter != null && leafMeshFilter.sharedMesh != null)
            {
                Mesh leafMesh = leafMeshFilter.sharedMesh;

                vertexTrunkLeavesCount += leafMesh.vertexCount;
                triangleTrunkLeavesCount += leafMesh.triangles.Length / 3;
                edgeTrunkLeavesCount += CalculateEdges(leafMesh); // Helper method to calculate edges
            }
        }
    }


    #region Presets
    void Preset1()
    {
        // Trunk properties
        trunkHeight = 4.1f;
        trunkRadius = 0.1f;
        trunkRadiusCurvature = 0.793f;
        trunkRadiusNoise = 0.545f;
        trunkSubdivision = 0;
        trunkCrinkliness = 0f;
        trunkSegments = 4;
        trunkBending = 0.0207f;
        //trunkMaterial = BarkTest;
        //trunkObject = TreeTrunk;

        // Branch properties
        numberOfBranches = 17;
        branchHeightMin = 0.19f;
        branchHeightMax = 0.938f;
        branchRadius = 0.08f;
        branchLength = 2.99f;
        branchRadiusCurvature = 0.945f;
        branchRadiusNoise = 0.962f;
        branchSubdivision = 0;
        branchCrinkliness = 0f;
        branchSegments = 4;
        branchBending = 0.15f;
        branchAngle = -68.8f;
        adjustBranchLengthByHeight = true;
        gravity = 0.129f;

        // Branchlet properties
        numberOfBranchlets = 40;
        branchletHeightMin = 0.205f;
        branchletHeightMax = 0.937f;
        branchletRadius = 0.2f;
        branchletLength = 0.76f;
        branchletRadiusCurvature = 0.92f;
        branchletRadiusNoise = 0.292f;
        branchletSubdivision = 0;
        branchletCrinkliness = 0f;
        branchletSegments = 3;
        branchletBending = 0.1403f;
        branchletAngle = 53.1f;
        branchletForwardAngle = -50.5f;
        gravityBranchlets = 0.154f;

        // Leaf properties (Branches)
        numberOfLeaves = 21;
        leafSize = 1.37f;
        leafPositionMin = 0.831f;
        leafPositionMax = 1f;
        leafForwardRotation = 0f;
        leafRotation = 0f;
        leafRandomizeRotation = 0.471f;
        showMaterialSettings = true;
        addLeavesToBranch = false;
        leafBranchRandomPositioning = 0.073f;
        leafBranchPositioning = new Vector3(0f, 0f, 0f);
        leafBranchSizeV3 = new Vector3(1f, 1f, 1f);
        leafSizeBranchRandom = 0.328f;

        // Leaf properties (Branchlets)
        numberOfLeavesBranchlet = 15;
        leafBranchletSize = 1.5f;
        leafBranchletPositionMin = 0.27f;
        leafBranchletPositionMax = 1f;
        leafBranchletForwardRotation = 0f;
        leafBranchletRotation = 14.8f;
        leafBranchletRandomizeRotation = 0.273f;
        addLeavesToBranchlets = false;
        leafBranchletPositioning = new Vector3(0f, 0f, 0f);
        leafBranchletRandomPositioning = 0f;
        leafBranchletSizeV3 = new Vector3(1f, 1f, 1f);
        leafSizeBranchletRandom = 0.596f;

        // Leaf properties (Trunk)
        numberOfLeavesTrunk = 14;
        leafTrunkSize = 2.28f;
        leafTrunkPositionMin = 0.973f;
        leafTrunkPositionMax = 1f;
        leafTrunkForwardRotation = 0f;
        leafTrunkRotation = 0f;
        leafTrunkRandomizeRotation = 0.241f;
        leafTrunkRandomPositioning = 0f;
        leafTrunkPositioning = new Vector3(0f, 0f, 0f);
        leafTrunkSizeV3 = new Vector3(1f, 1f, 1f);
        leafSizeTrunkRandom = 0f;

        ReloadTrees();
    }

    void Preset2()
    {
        trunkHeight = 4.1f;
        trunkRadius = 0.1f;
        trunkRadiusCurvature = 0.793f;
        trunkRadiusNoise = 1f;
        trunkSubdivision = 0;
        trunkCrinkliness = 0.041f;
        trunkSegments = 6;
        trunkBending = 0.0522f;

        numberOfBranches = 12;
        branchHeightMin = 0.375f;
        branchHeightMax = 0.938f;
        branchRadius = 0.08f;
        branchLength = 3.44f;
        branchRadiusCurvature = 0.945f;
        branchRadiusNoise = 0.962f;
        branchSubdivision = 0;
        branchCrinkliness = 0f;
        branchSegments = 4;
        branchBending = 0.15f;
        branchAngle = -51.5f;
        adjustBranchLengthByHeight = true;
        gravity = 0.129f;

        numberOfBranchlets = 40;
        branchletHeightMin = 0.205f;
        branchletHeightMax = 0.937f;
        branchletRadius = 0.2f;
        branchletLength = 0.76f;
        branchletRadiusCurvature = 0.92f;
        branchletRadiusNoise = 0.292f;
        branchletSubdivision = 0;
        branchletCrinkliness = 0f;
        branchletSegments = 3;
        branchletBending = 0.1403f;
        branchletAngle = 53.1f;
        branchletForwardAngle = -50.5f;
        gravityBranchlets = 0.154f;

        numberOfLeaves = 21;
        leafSize = 1.37f;
        leafPositionMin = 0.831f;
        leafPositionMax = 1f;
        leafForwardRotation = 0f;
        leafRotation = 0f;
        leafRandomizeRotation = 0.471f;
        showMaterialSettings = true;
        addLeavesToBranch = false;
        leafBranchRandomPositioning = 0.073f;
        leafBranchPositioning = new Vector3(0f, 0f, 0f);

        leafBranchSizeV3 = new Vector3(1f, 1f, 1f);
        leafSizeBranchRandom = 0.328f;

        numberOfLeavesBranchlet = 15;
        leafBranchletSize = 1.5f;
        leafBranchletPositionMin = 0.27f;
        leafBranchletPositionMax = 1f;
        leafBranchletForwardRotation = 0f;
        leafBranchletRotation = 14.8f;
        leafBranchletRandomizeRotation = 0.273f;
        addLeavesToBranchlets = false;
        leafBranchletPositioning = new Vector3(0f, 0f, 0f);
        leafBranchletRandomPositioning = 0f;

        leafBranchletSizeV3 = new Vector3(1f, 1f, 1f);
        leafSizeBranchletRandom = 0.596f;

        numberOfLeavesTrunk = 14;
        leafTrunkSize = 2.28f;
        leafTrunkPositionMin = 0.973f;
        leafTrunkPositionMax = 1f;
        leafTrunkForwardRotation = 0f;
        leafTrunkRotation = 0f;
        leafTrunkRandomizeRotation = 0.241f;
        leafTrunkRandomPositioning = 0f;
        leafTrunkPositioning = new Vector3(0f, 0f, 0f);

        leafTrunkSizeV3 = new Vector3(1f, 1f, 1f);
        leafSizeTrunkRandom = 0f;

        ReloadTrees();
    }

    void Preset3()
    {
        // Trunk properties
        trunkHeight = 4.1f;
        trunkRadius = 0.1f;
        trunkRadiusCurvature = 0.695f;
        trunkRadiusNoise = 0.132f;
        trunkSubdivision = 0;
        trunkCrinkliness = 0.041f;
        trunkSegments = 3;
        trunkBending = 0.0353f;


        // Branch properties
        numberOfBranches = 14;
        branchHeightMin = 0.271f;
        branchHeightMax = 0.938f;
        branchRadius = 0.08f;
        branchLength = 1.72f;
        branchRadiusCurvature = 0.945f;
        branchRadiusNoise = 0.962f;
        branchSubdivision = 0;
        branchCrinkliness = 0f;
        branchSegments = 4;
        branchBending = 0.0581f;
        branchAngle = -35.5f;
        adjustBranchLengthByHeight = false;
        gravity = -0.099f;

        // Branchlet properties
        numberOfBranchlets = 0;
        branchletHeightMin = 0.205f;
        branchletHeightMax = 0.937f;
        branchletRadius = 0.2f;
        branchletLength = 0.76f;
        branchletRadiusCurvature = 0.92f;
        branchletRadiusNoise = 0.292f;
        branchletSubdivision = 0;
        branchletCrinkliness = 0f;
        branchletSegments = 3;
        branchletBending = 0.1403f;
        branchletAngle = 53.1f;
        branchletForwardAngle = -50.5f;
        gravityBranchlets = 0.154f;

        // Leaf properties
        numberOfLeaves = 51;
        leafSize = 1.35f;
        leafPositionMin = 0.016f;
        leafPositionMax = 1f;
        leafForwardRotation = 68.8f;
        leafRotation = 0f;
        leafRandomizeRotation = 0.041f;

        showMaterialSettings = true;
        addLeavesToBranch = false;
        leafBranchRandomPositioning = 0.073f;
        leafBranchPositioning = new Vector3(0f, 0f, 0f);

        leafBranchSizeV3 = new Vector3(1f, 1f, 1f);
        leafSizeBranchRandom = 0.928f;

        numberOfLeavesBranchlet = 0;
        leafBranchletSize = 1.5f;
        leafBranchletPositionMin = 0.27f;
        leafBranchletPositionMax = 1f;
        leafBranchletForwardRotation = 0f;
        leafBranchletRotation = 14.8f;
        leafBranchletRandomizeRotation = 0.273f;
        addLeavesToBranchlets = false;
        leafBranchletPositioning = new Vector3(0f, 0f, 0f);
        leafBranchletRandomPositioning = 0f;

        leafBranchletSizeV3 = new Vector3(1f, 1f, 1f);
        leafSizeBranchletRandom = 0.596f;

        numberOfLeavesTrunk = 0;
        leafTrunkSize = 2.28f;
        leafTrunkPositionMin = 0.973f;
        leafTrunkPositionMax = 1f;
        leafTrunkForwardRotation = 0f;
        leafTrunkRotation = 0f;
        leafTrunkRandomizeRotation = 0.241f;
        leafTrunkRandomPositioning = 0f;
        leafTrunkPositioning = new Vector3(0f, 0f, 0f);

        leafTrunkSizeV3 = new Vector3(1f, 1f, 1f);
        leafSizeTrunkRandom = 0f;

        ReloadTrees();
    }

    void Preset4()
    {
        trunkHeight = 5.11f;
        trunkRadius = 0.245f;
        trunkRadiusCurvature = 0.695f;
        trunkRadiusNoise = 0.132f;
        trunkSubdivision = 0;
        trunkCrinkliness = 0.041f;
        trunkSegments = 6;
        trunkBending = 0.11f;
        //trunkMaterial = Bark1;
        //trunkObject = TreeTrunk;

        numberOfBranches = 10;
        branchHeightMin = 0.286f;
        branchHeightMax = 0.938f;
        branchRadius = 0.2f;
        branchLength = 2.64f;
        branchRadiusCurvature = 0.76f;
        branchRadiusNoise = 0.962f;
        branchSubdivision = 0;
        branchCrinkliness = 0f;
        branchSegments = 4;
        branchBending = 0.15f;
        branchAngle = -89.1f;
        adjustBranchLengthByHeight = false;
        gravity = 0.765f;

        numberOfBranchlets = 20;
        branchletHeightMin = 0.205f;
        branchletHeightMax = 0.779f;
        branchletRadius = 1.05f;
        branchletLength = 0.71f;
        branchletRadiusCurvature = 0.44f;
        branchletRadiusNoise = 0.292f;
        branchletSubdivision = 0;
        branchletCrinkliness = 0f;
        branchletSegments = 3;
        branchletBending = 0.1403f;
        branchletAngle = -34.2f;
        branchletForwardAngle = -117f;
        gravityBranchlets = 0.195f;

        numberOfLeaves = 14;
        leafSize = 2.13f;
        leafPositionMin = 0.775f;
        leafPositionMax = 1f;
        leafForwardRotation = 137.6f;
        leafRotation = 14.4f;
        leafRandomizeRotation = 0.02f;
        //leafMaterial = Leaves7;
        showMaterialSettings = true;
        addLeavesToBranch = false;
        leafBranchRandomPositioning = 0.073f;
        leafBranchPositioning = new Vector3(0f, 0f, 0f);

        leafBranchSizeV3 = new Vector3(1.66f, 1.56f, 1f);
        leafSizeBranchRandom = 0.47f;

        numberOfLeavesBranchlet = 18;
        leafBranchletSize = 2.2f;
        leafBranchletPositionMin = 0.54f;
        leafBranchletPositionMax = 1f;
        leafBranchletForwardRotation = 133.6f;
        leafBranchletRotation = 0f;
        leafBranchletRandomizeRotation = 0.032f;
        addLeavesToBranchlets = false;
        leafBranchletPositioning = new Vector3(0f, 0f, 0f);
        leafBranchletRandomPositioning = 0f;

        leafBranchletSizeV3 = new Vector3(1.6f, 1.65f, 1f);
        leafSizeBranchletRandom = 0.639f;

        numberOfLeavesTrunk = 0;
        leafTrunkSize = 1.37f;
        leafTrunkPositionMin = 0.973f;
        leafTrunkPositionMax = 1f;
        leafTrunkForwardRotation = 0f;
        leafTrunkRotation = 0f;
        leafTrunkRandomizeRotation = 0f;
        leafTrunkRandomPositioning = 0f;
        leafTrunkPositioning = new Vector3(0f, 0f, 0f);

        leafTrunkSizeV3 = new Vector3(1f, 1f, 1f);
        leafSizeTrunkRandom = 0f;

        ReloadTrees();
    }

    void Preset5()
    {
        trunkHeight = 4.78f;
        trunkRadius = 0.25f;
        trunkRadiusCurvature = 0.918f;
        trunkRadiusNoise = 0.409f;
        trunkSubdivision = 0;
        trunkCrinkliness = 1.0f;
        trunkSegments = 10;
        trunkBending = 0.11f;

        numberOfBranches = 9;
        branchHeightMin = 0.21f;
        branchHeightMax = 0.917f;
        branchRadius = 0.3f;
        branchLength = 3.48f;
        branchRadiusCurvature = 0.98f;
        branchRadiusNoise = 0.0f;
        branchSubdivision = 0;
        branchCrinkliness = 0.685f;
        branchSegments = 9;
        branchBending = 0.15f;
        branchAngle = -74.5f;
        adjustBranchLengthByHeight = true;
        gravity = 0.325f;

        numberOfBranchlets = 15;
        branchletHeightMin = 0.3f;
        branchletHeightMax = 1.0f;
        branchletRadius = 0.13f;
        branchletLength = 2.03f;
        branchletRadiusCurvature = 0.944f;
        branchletRadiusNoise = 0.292f;
        branchletSubdivision = 0;
        branchletCrinkliness = 0.788f;
        branchletSegments = 3;
        branchletBending = 0.1403f;
        branchletAngle = 90.0f;
        branchletForwardAngle = 34.2f;
        gravityBranchlets = 0.15f;

        numberOfLeaves = 16;
        leafSize = 2.35f;
        leafPositionMin = 0.985f;
        leafPositionMax = 1.0f;
        leafForwardRotation = 4.6f;
        leafRotation = 112.8f;
        leafRandomizeRotation = 0.248f;
        showMaterialSettings = false;
        addLeavesToBranch = false;
        leafBranchRandomPositioning = 0.0f;
        leafBranchPositioning = new Vector3(0.0f, 0.0f, 0.0f);

        leafBranchSizeV3 = new Vector3(1.0f, 1.0f, 1.0f);
        leafSizeBranchRandom = 0.0f;

        numberOfLeavesBranchlet = 12;
        leafBranchletSize = 1.57f;
        leafBranchletPositionMin = 0.825f;
        leafBranchletPositionMax = 1.0f;
        leafBranchletForwardRotation = 0.0f;
        leafBranchletRotation = 8.1f;
        leafBranchletRandomizeRotation = 0.338f;
        addLeavesToBranchlets = false;
        leafBranchletPositioning = new Vector3(0.0f, 0.0f, 0.0f);
        leafBranchletRandomPositioning = 0.0f;

        leafBranchletSizeV3 = new Vector3(1.0f, 1.0f, 1.0f);
        leafSizeBranchletRandom = 0.332f;

        numberOfLeavesTrunk = 3;
        leafTrunkSize = 1.94f;
        leafTrunkPositionMin = 0.995f;
        leafTrunkPositionMax = 1.0f;
        leafTrunkForwardRotation = 11.2f;
        leafTrunkRotation = 0.0f;
        leafTrunkRandomizeRotation = 0.192f;
        leafTrunkRandomPositioning = 0.23f;
        leafTrunkPositioning = new Vector3(0.0f, 0.0f, 0.0f);

        leafTrunkSizeV3 = new Vector3(1.0f, 1.0f, 1.0f);
        leafSizeTrunkRandom = 0.0f;
        /*
        string trunkMaterialPath = "Assets/Stylized Trees/Materials/URP/Bark3.mat";
        string leafMaterialPath = "Assets/Stylized Trees/Materials/URP/Leaves10.mat";

        trunkMaterial = AssetDatabase.LoadAssetAtPath<Material>(trunkMaterialPath);
        leafMaterial = AssetDatabase.LoadAssetAtPath<Material>(leafMaterialPath);*/

        ReloadTrees();
    }

    void Preset6()
    {
        trunkHeight = 4.1f;
        trunkRadius = 0.18f;
        trunkRadiusCurvature = 1f;
        trunkRadiusNoise = 1f;
        trunkSubdivision = 0;
        trunkCrinkliness = 0.007f;
        trunkSegments = 4;
        trunkBending = 0.0011f;
        //trunkMaterial = Bark1;

        numberOfBranches = 20;
        branchHeightMin = 0.457f;
        branchHeightMax = 1f;
        branchRadius = 0.07f;
        branchLength = 1.49f;
        branchRadiusCurvature = 0.945f;
        branchRadiusNoise = 0.962f;
        branchSubdivision = 0;
        branchCrinkliness = 0f;
        branchSegments = 4;
        branchBending = 0f;
        branchAngle = -158.4f;
        adjustBranchLengthByHeight = true;
        gravity = 0f;

        numberOfBranchlets = 0;
        branchletHeightMin = 0.205f;
        branchletHeightMax = 0.937f;
        branchletRadius = 0.2f;
        branchletLength = 0.76f;
        branchletRadiusCurvature = 0.92f;
        branchletRadiusNoise = 0.292f;
        branchletSubdivision = 0;
        branchletCrinkliness = 0f;
        branchletSegments = 3;
        branchletBending = 0.1403f;
        branchletAngle = 53.1f;
        branchletForwardAngle = -50.5f;
        gravityBranchlets = 0.154f;

        numberOfLeaves = 6;
        leafSize = 2.28f;
        leafPositionMin = 0.043f;
        leafPositionMax = 1f;
        leafForwardRotation = 290.7f;
        leafRotation = 180f;
        leafRandomizeRotation = 0f;
        //leafMaterial = Leaves9;
        showMaterialSettings = true;
        addLeavesToBranch = false;
        leafBranchRandomPositioning = 0.073f;
        leafBranchPositioning = new Vector3(0f, 0f, 0f);

        leafBranchSizeV3 = new Vector3(1.54f, 1.31f, 1f);
        leafSizeBranchRandom = 0.328f;

        numberOfLeavesBranchlet = 0;
        leafBranchletSize = 1.5f;
        leafBranchletPositionMin = 0.27f;
        leafBranchletPositionMax = 1f;
        leafBranchletForwardRotation = 0f;
        leafBranchletRotation = 14.8f;
        leafBranchletRandomizeRotation = 0.273f;
        addLeavesToBranchlets = false;
        leafBranchletPositioning = new Vector3(0f, 0f, 0f);
        leafBranchletRandomPositioning = 0f;

        leafBranchletSizeV3 = new Vector3(1f, 1f, 1f);
        leafSizeBranchletRandom = 0.596f;

        numberOfLeavesTrunk = 0;
        leafTrunkSize = 2.28f;
        leafTrunkPositionMin = 0.08f;
        leafTrunkPositionMax = 1f;
        leafTrunkForwardRotation = 0f;
        leafTrunkRotation = 0f;
        leafTrunkRandomizeRotation = 0f;
        leafTrunkRandomPositioning = 0f;
        leafTrunkPositioning = new Vector3(0f, 0f, 0f);

        leafTrunkSizeV3 = new Vector3(1f, 1f, 1f);
        leafSizeTrunkRandom = 0f;

        ReloadTrees();
    }



    void ReloadTrees()
    {
        GenerateTreeTrunk(ref vertexCount, ref triangleCount, ref edgeCount, trunkSubdivision, trunkSegments, trunkBending, trunkHeight, trunkRadiusCurvature, trunkRadius,treeStumpStartPoint,treeStumpWidth, includeStump, spawnPosition);

        GenerateTreeBranches(trunkHeight, branchLength, branchAngle, branchBending, trunkBending, trunkRadius, trunkRadiusCurvature, gravity, angleAdjustmentByHeight, adjustBranchLengthByHeight, branchSegments, branchSubdivision, ref vertexBranchCount, ref triangleBranchCount, ref edgeBranchCount);


        GenerateTreeBranchlets(adjustBranchletLengthByHeight, gravityBranchlets, branchletAngle, branchletLength, branchletForwardAngle, branchletBending, branchletSegments, branchletSubdivision, ref vertexBranchletCount, ref triangleBranchletCount, ref edgeBranchletCount);

        GenerateLeafPlanes(leafBranchPositioning, leafBranchSizeV3, leafMaterial, numberOfLeaves, ref vertexBranchLeavesCount, ref triangleBranchLeavesCount, ref edgeBranchLeavesCount, leafPrefab, leafSizeBranchRandom, leafSize, leafPositionMin, leafPositionMax, leafForwardRotation, leafRotation, leafRandomizeRotation, leafBranchRandomPositioning);
        GenerateLeafBranchletPlanes(leafBranchletPositioning, leafBranchletSizeV3, leafMaterial, numberOfLeavesBranchlet, ref vertexBranchletLeavesCount, ref triangleBranchletLeavesCount, ref edgeBranchletLeavesCount, leafPrefab, leafSizeBranchRandom, leafBranchletSize, leafBranchletPositionMin, leafBranchletPositionMax, leafBranchletForwardRotation, leafBranchletRotation, leafBranchletRandomizeRotation, leafBranchletRandomPositioning);
        GenerateLeafTrunkPlanes(leafTrunkPositioning, leafTrunkSizeV3, leafMaterial, numberOfLeavesTrunk, ref vertexTrunkLeavesCount, ref triangleTrunkLeavesCount, ref edgeTrunkLeavesCount, leafPrefab, leafSizeTrunkRandom, leafTrunkSize, leafTrunkPositionMin, leafTrunkPositionMax, leafTrunkForwardRotation, leafTrunkRotation, leafTrunkRandomizeRotation, leafTrunkRandomPositioning);
    }


    #endregion

    void PrintCurrentValues()
    {
        string output = $@"
trunkHeight = {trunkHeight.ToString("0.0####", CultureInfo.InvariantCulture)}f;
trunkRadius = {trunkRadius.ToString("0.0####", CultureInfo.InvariantCulture)}f;
trunkRadiusCurvature = {trunkRadiusCurvature.ToString("0.0####", CultureInfo.InvariantCulture)}f;
trunkRadiusNoise = {trunkRadiusNoise.ToString("0.0####", CultureInfo.InvariantCulture)}f;
trunkSubdivision = {trunkSubdivision};
trunkCrinkliness = {trunkCrinkliness.ToString("0.0####", CultureInfo.InvariantCulture)}f;
trunkSegments = {trunkSegments};
trunkBending = {trunkBending.ToString("0.0####", CultureInfo.InvariantCulture)}f;
trunkMaterial = {(trunkMaterial != null ? trunkMaterial.name : "null")};
trunkObject = {(trunkObject != null ? trunkObject.name : "null")};

numberOfBranches = {numberOfBranches};
branchHeightMin = {branchHeightMin.ToString("0.0####", CultureInfo.InvariantCulture)}f;
branchHeightMax = {branchHeightMax.ToString("0.0####", CultureInfo.InvariantCulture)}f;
branchRadius = {branchRadius.ToString("0.0####", CultureInfo.InvariantCulture)}f;
branchLength = {branchLength.ToString("0.0####", CultureInfo.InvariantCulture)}f;
branchRadiusCurvature = {branchRadiusCurvature.ToString("0.0####", CultureInfo.InvariantCulture)}f;
branchRadiusNoise = {branchRadiusNoise.ToString("0.0####", CultureInfo.InvariantCulture)}f;
branchSubdivision = {branchSubdivision};
branchCrinkliness = {branchCrinkliness.ToString("0.0####", CultureInfo.InvariantCulture)}f;
branchSegments = {branchSegments};
branchBending = {branchBending.ToString("0.0####", CultureInfo.InvariantCulture)}f;
branchAngle = {branchAngle.ToString("0.0####", CultureInfo.InvariantCulture)}f;
adjustBranchLengthByHeight = {adjustBranchLengthByHeight.ToString().ToLower()};
gravity = {gravity.ToString("0.0####", CultureInfo.InvariantCulture)}f;

numberOfBranchlets = {numberOfBranchlets};
branchletHeightMin = {branchletHeightMin.ToString("0.0####", CultureInfo.InvariantCulture)}f;
branchletHeightMax = {branchletHeightMax.ToString("0.0####", CultureInfo.InvariantCulture)}f;
branchletRadius = {branchletRadius.ToString("0.0####", CultureInfo.InvariantCulture)}f;
branchletLength = {branchletLength.ToString("0.0####", CultureInfo.InvariantCulture)}f;
branchletRadiusCurvature = {branchletRadiusCurvature.ToString("0.0####", CultureInfo.InvariantCulture)}f;
branchletRadiusNoise = {branchletRadiusNoise.ToString("0.0####", CultureInfo.InvariantCulture)}f;
branchletSubdivision = {branchletSubdivision};
branchletCrinkliness = {branchletCrinkliness.ToString("0.0####", CultureInfo.InvariantCulture)}f;
branchletSegments = {branchletSegments};
branchletBending = {branchletBending.ToString("0.0####", CultureInfo.InvariantCulture)}f;
branchletAngle = {branchletAngle.ToString("0.0####", CultureInfo.InvariantCulture)}f;
branchletForwardAngle = {branchletForwardAngle.ToString("0.0####", CultureInfo.InvariantCulture)}f;
gravityBranchlets = {gravityBranchlets.ToString("0.0####", CultureInfo.InvariantCulture)}f;

branchesParent = {(branchesParent != null ? branchesParent.name : "null")};
branchletsParent = {(branchletsParent != null ? branchletsParent.name : "null")};

numberOfLeaves = {numberOfLeaves};
leafSize = {leafSize.ToString("0.0####", CultureInfo.InvariantCulture)}f;
leafPositionMin = {leafPositionMin.ToString("0.0####", CultureInfo.InvariantCulture)}f;
leafPositionMax = {leafPositionMax.ToString("0.0####", CultureInfo.InvariantCulture)}f;
leafForwardRotation = {leafForwardRotation.ToString("0.0####", CultureInfo.InvariantCulture)}f;
leafRotation = {leafRotation.ToString("0.0####", CultureInfo.InvariantCulture)}f;
leafRandomizeRotation = {leafRandomizeRotation.ToString().ToLower()};
leafMaterial = {(leafMaterial != null ? leafMaterial.name : "null")};
showMaterialSettings = {showMaterialSettings.ToString().ToLower()};
addLeavesToBranch = {addLeavesToBranch.ToString().ToLower()};
leafBranchRandomPositioning = {leafBranchRandomPositioning.ToString("0.0####", CultureInfo.InvariantCulture)}f;
leafBranchPositioning = new Vector3({leafBranchPositioning.x.ToString("0.0####", CultureInfo.InvariantCulture)}f, {leafBranchPositioning.y.ToString("0.0####", CultureInfo.InvariantCulture)}f, {leafBranchPositioning.z.ToString("0.0####", CultureInfo.InvariantCulture)}f);

leafBranchSizeV3 = new Vector3({leafBranchSizeV3.x.ToString("0.0####", CultureInfo.InvariantCulture)}f, {leafBranchSizeV3.y.ToString("0.0####", CultureInfo.InvariantCulture)}f, {leafBranchSizeV3.z.ToString("0.0####", CultureInfo.InvariantCulture)}f);
leafSizeBranchRandom = {leafSizeBranchRandom.ToString("0.0####", CultureInfo.InvariantCulture)}f;

numberOfLeavesBranchlet = {numberOfLeavesBranchlet};
leafBranchletSize = {leafBranchletSize.ToString("0.0####", CultureInfo.InvariantCulture)}f;
leafBranchletPositionMin = {leafBranchletPositionMin.ToString("0.0####", CultureInfo.InvariantCulture)}f;
leafBranchletPositionMax = {leafBranchletPositionMax.ToString("0.0####", CultureInfo.InvariantCulture)}f;
leafBranchletForwardRotation = {leafBranchletForwardRotation.ToString("0.0####", CultureInfo.InvariantCulture)}f;
leafBranchletRotation = {leafBranchletRotation.ToString("0.0####", CultureInfo.InvariantCulture)}f;
leafBranchletRandomizeRotation = {leafBranchletRandomizeRotation.ToString().ToLower()}f;
addLeavesToBranchlets = {addLeavesToBranchlets.ToString().ToLower()};
leafBranchletPositioning = new Vector3({leafBranchletPositioning.x.ToString("0.0####", CultureInfo.InvariantCulture)}f, {leafBranchletPositioning.y.ToString("0.0####", CultureInfo.InvariantCulture)}f, {leafBranchletPositioning.z.ToString("0.0####", CultureInfo.InvariantCulture)}f);
leafBranchletRandomPositioning = {leafBranchletRandomPositioning.ToString("0.0####", CultureInfo.InvariantCulture)}f;

leafBranchletSizeV3 = new Vector3({leafBranchletSizeV3.x.ToString("0.0####", CultureInfo.InvariantCulture)}f, {leafBranchletSizeV3.y.ToString("0.0####", CultureInfo.InvariantCulture)}f, {leafBranchletSizeV3.z.ToString("0.0####", CultureInfo.InvariantCulture)}f);
leafSizeBranchletRandom = {leafSizeBranchletRandom.ToString("0.0####", CultureInfo.InvariantCulture)}f;

numberOfLeavesTrunk = {numberOfLeavesTrunk};
leafTrunkSize = {leafTrunkSize.ToString("0.0####", CultureInfo.InvariantCulture)}f;
leafTrunkPositionMin = {leafTrunkPositionMin.ToString("0.0####", CultureInfo.InvariantCulture)}f;
leafTrunkPositionMax = {leafTrunkPositionMax.ToString("0.0####", CultureInfo.InvariantCulture)}f;
leafTrunkForwardRotation = {leafTrunkForwardRotation.ToString("0.0####", CultureInfo.InvariantCulture)}f;
leafTrunkRotation = {leafTrunkRotation.ToString("0.0####", CultureInfo.InvariantCulture)}f;
leafTrunkRandomizeRotation = {leafTrunkRandomizeRotation.ToString().ToLower()}f;
leafTrunkRandomPositioning = {leafTrunkRandomPositioning.ToString("0.0####", CultureInfo.InvariantCulture)}f;
leafTrunkPositioning = new Vector3({leafTrunkPositioning.x.ToString("0.0####", CultureInfo.InvariantCulture)}f, {leafTrunkPositioning.y.ToString("0.0####", CultureInfo.InvariantCulture)}f, {leafTrunkPositioning.z.ToString("0.0####", CultureInfo.InvariantCulture)}f);

leafTrunkSizeV3 = new Vector3({leafTrunkSizeV3.x.ToString("0.0####", CultureInfo.InvariantCulture)}f, {leafTrunkSizeV3.y.ToString("0.0####", CultureInfo.InvariantCulture)}f, {leafTrunkSizeV3.z.ToString("0.0####", CultureInfo.InvariantCulture)}f);
leafSizeTrunkRandom = {leafSizeTrunkRandom.ToString("0.0####", CultureInfo.InvariantCulture)}f;
";
        Debug.Log(output);
    }


}
}