using UnityEditor;
using UnityEngine;
using Vive.Plugin.SR;
using System.Collections.Generic;

[CustomEditor(typeof(ViveSR_RigidReconstructionRenderer))]
[CanEditMultipleObjects]
public class ViveSR_RigidReconstructionRendererEditor : Editor
{
    string[] displayMode = new[] { "Full Scene Point", "Field Of View", "Adaptive Mesh" };
    string[] adaptiveLable = new[] { "64cm", "32cm", "16cm", "8cm", "4cm", "2cm" };
    List<float> adaptiveLevel = new List<float>{ 64.0f, 32.0f, 16.0f, 8.0f, 4.0f, 2.0f };
    int maxSelectID, minSelectID;
    float errorThres, exportMaxSize, exportMinSize;

    bool[] SceneObjectToggle = new[] { false, false, false, false, false, false };
    string[] SceneObjectName = new[] { "Floor", "Wall", "Ceiling", "Chair", "Table", "Bed" };


    public string ReconsSceneDir = "Recons3DAsset/";
    public string SemanticObjDir = "SemanticIndoorObj";
    
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        if (!Application.isPlaying) return;

        EditorGUILayout.Separator();
        EditorGUILayout.Separator();
        GUIStyle style = new GUIStyle();
        style.fontStyle = FontStyle.Bold;
        GUILayout.Label(new GUIContent("[Runtime Command]"), style);
        EditorGUILayout.Separator();

        // start / stop
        GUILayout.Label(new GUIContent("--Start/Stop--"), style);
        if (!ViveSR_RigidReconstruction.IsScanning && !ViveSR_RigidReconstruction.IsExportingMesh)
        {
            if (GUILayout.Button("Start Reconstruction"))
            {
                ViveSR_RigidReconstruction.StartScanning();
            }
        }
        if (ViveSR_RigidReconstruction.IsScanning && !ViveSR_RigidReconstruction.IsExportingMesh)
        {
            if (GUILayout.Button("Stop Reconstruction"))
            {
                ViveSR_RigidReconstruction.StopScanning();
            }

            // live extraction mode
            EditorGUILayout.Separator();
            GUILayout.Label(new GUIContent("--Live Extraction--"), style);
            int curMode = (int)ViveSR_RigidReconstructionRenderer.LiveMeshDisplayMode;
            GUILayout.BeginHorizontal();
            GUILayout.Label("Display Mode:");
            curMode = EditorGUILayout.Popup(curMode, displayMode);
            GUILayout.EndHorizontal();

            bool enableSector = GUILayout.Toggle(ViveSR_RigidReconstructionRenderer.EnableSector, "Enable Sectioned Mesh");
            if (enableSector != ViveSR_RigidReconstructionRenderer.EnableSector) ViveSR_RigidReconstructionRenderer.EnableSector = enableSector;

            int sectorGroupNum = EditorGUILayout.IntSlider("Sectioned Mesh Limit", ViveSR_RigidReconstructionRenderer.MaxActiveGO, 50, 500);
            if (sectorGroupNum != ViveSR_RigidReconstructionRenderer.MaxActiveGO) ViveSR_RigidReconstructionRenderer.MaxActiveGO = sectorGroupNum;

            if (curMode != (int)ViveSR_RigidReconstructionRenderer.LiveMeshDisplayMode)
            {
                ViveSR_RigidReconstructionRenderer.LiveMeshDisplayMode = (ReconstructionDisplayMode)curMode;
            }
            // adaptive tunning
            if (curMode == (int)ReconstructionDisplayMode.ADAPTIVE_MESH)
            {
                EditorGUILayout.Separator();
                GUILayout.Label(new GUIContent("--Live Adaptive Mesh Tuning--"), style);
                DrawAdaptiveParamUI(ViveSR_RigidReconstruction.LiveAdaptiveMaxGridSize, ViveSR_RigidReconstruction.LiveAdaptiveMinGridSize, ViveSR_RigidReconstruction.LiveAdaptiveErrorThres);
                ViveSR_RigidReconstruction.LiveAdaptiveMaxGridSize = adaptiveLevel[maxSelectID];
                ViveSR_RigidReconstruction.LiveAdaptiveMinGridSize = adaptiveLevel[minSelectID];
                ViveSR_RigidReconstruction.LiveAdaptiveErrorThres = errorThres;
            }
        }

        // export
        EditorGUILayout.Separator();
        if (ViveSR_RigidReconstruction.IsScanning && !ViveSR_RigidReconstruction.IsExportingMesh)
        {
            GUILayout.Label(new GUIContent("--Export--"), style);
            bool exportAdaptive = ViveSR_RigidReconstruction.ExportAdaptiveMesh;
            ViveSR_RigidReconstruction.ExportAdaptiveMesh = GUILayout.Toggle(exportAdaptive, "Export Adaptive Model");

            if (ViveSR_RigidReconstruction.ExportAdaptiveMesh)
            {
                // live extraction mode
                EditorGUILayout.Separator();
                GUILayout.Label(new GUIContent("--Export Adaptive Mesh Tuning--"), style);
                DrawAdaptiveParamUI(ViveSR_RigidReconstruction.ExportAdaptiveMaxGridSize, ViveSR_RigidReconstruction.ExportAdaptiveMinGridSize, ViveSR_RigidReconstruction.ExportAdaptiveErrorThres);
                ViveSR_RigidReconstruction.ExportAdaptiveMaxGridSize = adaptiveLevel[maxSelectID];
                ViveSR_RigidReconstruction.ExportAdaptiveMinGridSize = adaptiveLevel[minSelectID];
                ViveSR_RigidReconstruction.ExportAdaptiveErrorThres = errorThres;
            }

            if (GUILayout.Button("Start Export Model"))
            {
                ViveSR_RigidReconstruction.ExportModel("Model");
            }
        }

        // Scene Understanding 
        // output surrounding objects of interest and their attributes 
        EditorGUILayout.Separator();

        GUILayout.Label(new GUIContent("--Scene Understanding--"), style);
        if (ViveSR_RigidReconstruction.IsScanning)
        {
            bool isSemanticEnabled = GUILayout.Toggle(ViveSR_SceneUnderstanding.IsEnabledSceneUnderstanding, "Enable Scene Understanding");
            if (isSemanticEnabled != ViveSR_SceneUnderstanding.IsEnabledSceneUnderstanding)
            {
                ViveSR_SceneUnderstanding.EnableSceneUnderstanding(isSemanticEnabled);
            }
        }
        if (ViveSR_SceneUnderstanding.IsEnabledSceneUnderstanding && ViveSR_RigidReconstruction.IsScanning)
        {
            bool isSemanticRefinementEnabled = GUILayout.Toggle(ViveSR_SceneUnderstanding.IsEnabledSceneUnderstandingRefinement, "Enable Scene Understanding Refinement");
            if (isSemanticRefinementEnabled != ViveSR_SceneUnderstanding.IsEnabledSceneUnderstandingRefinement)
            {
                ViveSR_SceneUnderstanding.EnableSceneUnderstandingRefinement(isSemanticRefinementEnabled);
            }
            bool isSemanticPreviewEnabled = GUILayout.Toggle(ViveSR_SceneUnderstanding.IsEnabledSceneUnderstandingView, "Enable Preview");
            if (isSemanticPreviewEnabled != ViveSR_SceneUnderstanding.IsEnabledSceneUnderstandingView)
            {
                ViveSR_SceneUnderstanding.EnableSceneUnderstandingView(isSemanticPreviewEnabled); 
            }
            int index = 0;
            foreach(bool toggle in SceneObjectToggle)
            {
                bool _toggle = GUILayout.Toggle(toggle, "View/Export "+ SceneObjectName[index]);
                if (_toggle != toggle)
                {
                    SceneObjectToggle[index] = _toggle;
                    switch (SceneObjectName[index])
                    {
                        case "Bed":
                            ViveSR_SceneUnderstanding.SetCustomSceneUnderstandingConfig((int)SceneUnderstandingObjectType.BED, 5, _toggle);
                            break;
                        case "Ceiling":
                            ViveSR_SceneUnderstanding.SetCustomSceneUnderstandingConfig((int)SceneUnderstandingObjectType.CEILING, 5, _toggle);
                            break;
                        case "Chair":
                            ViveSR_SceneUnderstanding.SetCustomSceneUnderstandingConfig((int)SceneUnderstandingObjectType.CHAIR, 5, _toggle);
                            break;
                        case "Floor":
                            ViveSR_SceneUnderstanding.SetCustomSceneUnderstandingConfig((int)SceneUnderstandingObjectType.FLOOR, 5, _toggle);
                            break;
                        case "Table":
                            ViveSR_SceneUnderstanding.SetCustomSceneUnderstandingConfig((int)SceneUnderstandingObjectType.TABLE, 5, _toggle);
                            break;
                        case "Wall":
                            ViveSR_SceneUnderstanding.SetCustomSceneUnderstandingConfig((int)SceneUnderstandingObjectType.WALL, 5, _toggle);
                            break;
                    }
                
                }
                index++;
            }
        }
        if (ViveSR_SceneUnderstanding.IsEnabledSceneUnderstanding && ViveSR_RigidReconstruction.IsScanning)
        {

            if (GUILayout.Button("Export SceneObjects (.xml)"))
            {
                ViveSR_SceneUnderstanding.ExportSceneUnderstandingInfo(SemanticObjDir);
            }
        }

        if (GUILayout.Button("Load & Show SceneObjects BoundingBox"))
        {
            ViveSR_SceneUnderstanding.ImportSceneObjects(ReconsSceneDir + SemanticObjDir);
            //ViveSR_SceneUnderstanding.GetSemanticBoundingBox(ReconsSceneDir + SemanticObjDir, (int)SceneUnderstandingObjectType.CHAIR);
            //ViveSR_SceneUnderstanding.GetSemanticBoundingBox(ReconsSceneDir + SemanticObjDir, (int)SceneUnderstandingObjectType.CEILING);
            //ViveSR_SceneUnderstanding.GetSemanticBoundingBox(ReconsSceneDir + SemanticObjDir, (int)SceneUnderstandingObjectType.FLOOR);
            //ViveSR_SceneUnderstanding.GetSemanticBoundingBox(ReconsSceneDir + SemanticObjDir, (int)SceneUnderstandingObjectType.WALL);
            //ViveSR_SceneUnderstanding.GetSemanticBoundingBox(ReconsSceneDir + SemanticObjDir, (int)SceneUnderstandingObjectType.BED);
            //ViveSR_SceneUnderstanding.GetSemanticBoundingBox(ReconsSceneDir + SemanticObjDir, (int)SceneUnderstandingObjectType.TABLE);
            ViveSR_SceneUnderstanding.ShowSemanticBoundingBoxAndIconWithType((int)SceneUnderstandingObjectType.CHAIR, true, false);
            ViveSR_SceneUnderstanding.ShowSemanticBoundingBoxAndIconWithType((int)SceneUnderstandingObjectType.CEILING, true, false);
            ViveSR_SceneUnderstanding.ShowSemanticBoundingBoxAndIconWithType((int)SceneUnderstandingObjectType.FLOOR, true, false);
            ViveSR_SceneUnderstanding.ShowSemanticBoundingBoxAndIconWithType((int)SceneUnderstandingObjectType.WALL, true, false);
            ViveSR_SceneUnderstanding.ShowSemanticBoundingBoxAndIconWithType((int)SceneUnderstandingObjectType.BED, true, false);
            ViveSR_SceneUnderstanding.ShowSemanticBoundingBoxAndIconWithType((int)SceneUnderstandingObjectType.TABLE, true, false);
            
            //ViveSR_SceneUnderstanding.ShowSemanticBoundingBoxWithId((int)SceneUnderstandingObjectType.CHAIR, 0, false, true);
        }
    }

    private void DrawAdaptiveParamUI(float maxGridSize, float minGridSize, float thres)
    {
        GUILayout.Label("Adaptive Range (Max~Min):");
        GUILayout.BeginHorizontal();
        maxSelectID = adaptiveLevel.IndexOf(maxGridSize);
        minSelectID = adaptiveLevel.IndexOf(minGridSize);
        maxSelectID = EditorGUILayout.Popup(maxSelectID, adaptiveLable);
        minSelectID = EditorGUILayout.Popup(minSelectID, adaptiveLable);
        GUILayout.EndHorizontal();

        GUILayout.Label("Divide Threshold:");
        GUILayout.BeginHorizontal();
        errorThres = GUILayout.HorizontalSlider(thres, 0.0f, 1.5f);
        GUILayout.Label("" + errorThres.ToString("0.00"));
        GUILayout.EndHorizontal();
    }
}
