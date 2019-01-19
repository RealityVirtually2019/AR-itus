using UnityEngine;
using UnityEditor;
using Vive.Plugin.SR;

[CustomEditor(typeof(ViveSR_DualCameraImageRenderer))]
[CanEditMultipleObjects]
public class ViveSR_DualCameraImageRendererEditor : Editor
{
    float NearOcclusionDistThres;
    float FarOcclusionDistThres;
    float NearColliderDistThres;
    float FarColliderDistThres;
    string[] undistortMethod = new[] { "Defish By Mesh", "Defish By SRModule" };
    string[] depthCauseMode = new[] { "DEFAULT", "CLOSE_RANGE"};
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        if (!Application.isPlaying) return;

        GUIStyle style = new GUIStyle();
        style.fontStyle = FontStyle.Bold;

        GUILayout.BeginHorizontal();
        GUILayout.Label("Undistort Method:");
        int curMode = (int)ViveSR_DualCameraImageRenderer.UndistortMethod;
        curMode = EditorGUILayout.Popup(curMode, undistortMethod);
        GUILayout.EndHorizontal();
        if (curMode != (int)ViveSR_DualCameraImageRenderer.UndistortMethod)
        {
            ViveSR_DualCameraImageRenderer.UndistortMethod = (UndistortionMethod)curMode;
        }

        ViveSR_DualCameraImageRenderer.UpdateDistortedMaterial = GUILayout.Toggle(ViveSR_DualCameraImageRenderer.UpdateDistortedMaterial, "Update Camera Material");
        ViveSR_DualCameraImageRenderer.UpdateUndistortedMaterial = GUILayout.Toggle(ViveSR_DualCameraImageRenderer.UpdateUndistortedMaterial, "Update Undistorted Material");
        ViveSR_DualCameraImageRenderer.UpdateDepthMaterial = GUILayout.Toggle(ViveSR_DualCameraImageRenderer.UpdateDepthMaterial, "Update Depth Material");
        ViveSR_DualCameraImageRenderer.CallbackMode = GUILayout.Toggle(ViveSR_DualCameraImageRenderer.CallbackMode, "Callback Mode");

        GUILayout.Label(new GUIContent("[Depth Setting]"), style);
        string btnStrEnableDepthProcess = ViveSR_DualCameraImageCapture.DepthProcessing ? "Disable Depth Processing" : "Enable Depth Processing";
        if (GUILayout.Button(btnStrEnableDepthProcess))
        {
            ViveSR_DualCameraImageCapture.EnableDepthProcess(!ViveSR_DualCameraImageCapture.DepthProcessing);
        }

        if (ViveSR_DualCameraImageCapture.DepthProcessing)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Depth Case     :");
            int curDepthCase = (int)ViveSR_DualCameraImageCapture.DepthCase;
            curDepthCase = EditorGUILayout.Popup(curDepthCase, depthCauseMode);
            GUILayout.EndHorizontal();
            if (curDepthCase != (int)ViveSR_DualCameraImageCapture.DepthCase)
            {
                ViveSR_DualCameraImageCapture.ChangeDepthCase((DepthCase)curDepthCase);
            }
            EditorGUILayout.Separator();
            ViveSR_DualCameraImageCapture.DepthRefinement = GUILayout.Toggle(ViveSR_DualCameraImageCapture.DepthRefinement, "Enable Depth Refinement");
            ViveSR_DualCameraImageCapture.DepthEdgeEnhance = GUILayout.Toggle(ViveSR_DualCameraImageCapture.DepthEdgeEnhance, "Enable Depth Edge Enhance");

            EditorGUILayout.Separator();
            ViveSR_DualCameraImageRenderer.DepthImageOcclusion = GUILayout.Toggle(ViveSR_DualCameraImageRenderer.DepthImageOcclusion, "Enable Depth Image Occlusion");
            ViveSR_DualCameraImageRenderer.VisualizeDepthOcclusion = GUILayout.Toggle(ViveSR_DualCameraImageRenderer.VisualizeDepthOcclusion, "Visualize Depth Occlusion");
            {
                GUILayout.Label("Near Occlusion Distance:");
                GUILayout.BeginHorizontal();
                NearOcclusionDistThres = ViveSR_DualCameraImageRenderer.OcclusionNearDistance = GUILayout.HorizontalSlider(ViveSR_DualCameraImageRenderer.OcclusionNearDistance, 0.05f, 10.0f);
                GUILayout.Label("" + NearOcclusionDistThres.ToString("0.00") + "m");
                GUILayout.EndHorizontal();

                GUILayout.Label("Far Occlusion Distance:");
                GUILayout.BeginHorizontal();
                FarOcclusionDistThres = ViveSR_DualCameraImageRenderer.OcclusionFarDistance = GUILayout.HorizontalSlider(ViveSR_DualCameraImageRenderer.OcclusionFarDistance, 0.05f, 10.0f);
                GUILayout.Label("" + FarOcclusionDistThres.ToString("0.00") + "m");
                GUILayout.EndHorizontal();
            }

            EditorGUILayout.Separator();
            ViveSR_DualCameraDepthCollider.UpdateDepthCollider = GUILayout.Toggle(ViveSR_DualCameraDepthCollider.UpdateDepthCollider, "Run Depth Mesh Collider");
            if (ViveSR_DualCameraDepthCollider.UpdateDepthCollider)
            {
                ViveSR_DualCameraDepthCollider.ColliderMeshVisibility = GUILayout.Toggle(ViveSR_DualCameraDepthCollider.ColliderMeshVisibility, "Show Depth Mesh Collider");

                ViveSR_DualCameraDepthCollider.UpdateDepthColliderHoleFilling = GUILayout.Toggle(ViveSR_DualCameraDepthCollider.UpdateDepthColliderHoleFilling, "Enable Depth Mesh Hole Filling");

                ViveSR_DualCameraDepthCollider.UpdateDepthColliderRange = GUILayout.Toggle(ViveSR_DualCameraDepthCollider.UpdateDepthColliderRange, "Adjust Depth Distance");


                if (ViveSR_DualCameraDepthCollider.UpdateDepthColliderRange)
                {
                    GUILayout.Label("Mesh Collider Distance - Near:");
                    GUILayout.BeginHorizontal();
                    NearColliderDistThres = ViveSR_DualCameraDepthCollider.UpdateColliderNearDistance = GUILayout.HorizontalSlider(ViveSR_DualCameraDepthCollider.UpdateColliderNearDistance, 0.0f, 10.0f);
                    GUILayout.Label("" + NearColliderDistThres.ToString("0.00") + "m");
                    GUILayout.EndHorizontal();

                    GUILayout.Label("Mesh Collider Distance - Far:");
                    GUILayout.BeginHorizontal();
                    FarColliderDistThres = ViveSR_DualCameraDepthCollider.UpdateColliderFarDistance = GUILayout.HorizontalSlider(ViveSR_DualCameraDepthCollider.UpdateColliderFarDistance, 0.0f, 10.0f);
                    GUILayout.Label("" + FarColliderDistThres.ToString("0.00") + "m");
                    GUILayout.EndHorizontal();
                }
            }
        }

       
    }
}