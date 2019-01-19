#if UNITY_EDITOR
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Vive.Plugin.SR
{
    public class ViveSR_Settings : EditorWindow
    {
        private static ViveSR_Settings window;
        private static List<Page> SettingPages = new List<Page>();
        private static int CountPage;
        private static int CurrentPage;

        public static void Update()
        {
            CurrentPage = CountPage = 0;
            SettingPages.Clear();

            Page[] pages = new Page[]
            {
                (Page_VRSetting)CreateInstance(typeof(Page_VRSetting)),
                (Page_LayerSetting)CreateInstance(typeof(Page_LayerSetting)),
                //(Page_QualitySetting)CreateInstance(typeof(Page_QualitySetting)),
                (Page_Finish)CreateInstance(typeof(Page_Finish)),
            };
            foreach (var page_iter in pages)
            {
                if (page_iter.IsNeedShow()) SettingPages.Add(page_iter);
            }
            CountPage = SettingPages.Count;
            if (CountPage > 1)
            {
                window = GetWindow<ViveSR_Settings>(true);
                window.minSize = new Vector2(300, 400);
            }
            EditorApplication.update -= Update;
        }

        void OnGUI()
        {
            GUI.skin.label.wordWrap = true;
            var rect = GUILayoutUtility.GetRect(position.width, 150, GUI.skin.box);
            var logo = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets\\ViveSR\\Textures\\SRWorks_logo.png");
            if (logo) GUI.DrawTexture(rect, logo, ScaleMode.ScaleToFit);

            EditorGUI.ProgressBar(new Rect(0, 157.5f, position.width, 20), (float)(CurrentPage + 1) / CountPage, "");
            GUILayout.Label("Page : " + (CurrentPage + 1) + " / " + CountPage, EditorStyles.boldLabel);
            GUILayout.Space(5);

            for (int p = CurrentPage; p < CountPage; p++)
            {
                if (SettingPages[p].IsNeedShow())
                {
                    SettingPages[p].RenderGUI();
                    break;
                }
                else
                    ++CurrentPage;
            }
            if (CurrentPage == CountPage || ViveSR_DualCameraRig.Instance == null) Close();
        }


        public abstract class Page : EditorWindow
        {
            public abstract string Name { get; }
            public abstract bool IsNeedShow();
            public abstract void RenderGUI();
        }
    }

    public class Page_VRSetting : ViveSR_Settings.Page
    {
        public override string Name { get { return "VR Setting"; } }

        const string HelpboxText_RemindEnableOpenVRSupport = "Enable OpenVR support?";
        public bool AutoEnableVR
        {
            get { return EditorPrefs.GetBool("ViveSR_AutoEnableVR", true); }
            set { EditorPrefs.SetBool("ViveSR_AutoEnableVR", value); }
        }

        public override bool IsNeedShow()
        {
            return IsNeedOpenVRSupport();
        }

        public override void RenderGUI()
        {
            EditorGUILayout.HelpBox(HelpboxText_RemindEnableOpenVRSupport, MessageType.Warning);
            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Ignore")) AutoEnableVR = false;
            if (GUILayout.Button("Accept"))
            {
                EnableOpenVRSupport();
            }
            GUILayout.EndHorizontal();
        }

        private bool IsNeedOpenVRSupport()
        {
            if (!AutoEnableVR) return false;
            if (!PlayerSettings.virtualRealitySupported) return true;

#if (UNITY_5_4 || UNITY_5_3 || UNITY_5_2 || UNITY_5_1 || UNITY_5_0)
			var devices = UnityEditorInternal.VR.VREditor.GetVREnabledDevices(BuildTargetGroup.Standalone);
#else
            var devices = UnityEditorInternal.VR.VREditor.GetVREnabledDevicesOnTargetGroup(BuildTargetGroup.Standalone);
#endif
            foreach (var device in devices)
                if (device.ToLower() == "openvr") return false;

            return true;
        }

        private void EnableOpenVRSupport()
        {
            PlayerSettings.virtualRealitySupported = true;

            if (IsNeedOpenVRSupport())
            {
#if (UNITY_5_4 || UNITY_5_3 || UNITY_5_2 || UNITY_5_1 || UNITY_5_0)
			    var devices = UnityEditorInternal.VR.VREditor.GetVREnabledDevices(BuildTargetGroup.Standalone);
#else
                var devices = UnityEditorInternal.VR.VREditor.GetVREnabledDevicesOnTargetGroup(BuildTargetGroup.Standalone);
#endif
                var newDevices = new List<string>(devices) { "OpenVR" }.ToArray();
#if (UNITY_5_4 || UNITY_5_3 || UNITY_5_2 || UNITY_5_1 || UNITY_5_0)
			    UnityEditorInternal.VR.VREditor.SetVREnabledDevices(BuildTargetGroup.Standalone, newDevices);
#else
                UnityEditorInternal.VR.VREditor.SetVREnabledDevicesOnTargetGroup(BuildTargetGroup.Standalone, newDevices);
#endif
            }
        }
    }

    public class Page_LayerSetting : ViveSR_Settings.Page
    {
        public override string Name { get { return "Layer Setting"; } }

        public const string LefteyeLayerName = "DualCamera (Left)";
        public const string RighteyeLayerName = "DualCamera (Right)";
        public const string VirtualWorldLayerName = "VirtualWorldLayer";
        private const int VRWorldLayer = 2;
        private bool IsNeedStereoSetting = false;
        private bool IsNeedPortalSetting = false;
        private int[] candidateLayerIndex = new int[3];
        private bool IsStereoInputValid = false;
        private bool IsPortalInputValid = false;

        const string Label_LayerSetting = "ViveSR needs 2 layers for rendering Lefteye and Righteye images.";
        const string HelpboxText_LayerNotValid = "Please insert valid values";
        const string BtnName_UpdateLayers = "Update layers";
        const string DialogTitle_Error = "Error";
        const string DialogTitle_ScriptNotFound = "ViveSR_DualCameraRig.Instance does not exist in current scene";
        const string Label_LayerChangeName = "If you want to change layer name, please modify the variables of LefteyeLayerName and RighteyeLayerName.";
        const string Label_PortalLayerSetting = "ViveSR needs 1 layer for rendering portal effect.";

        private string[] CandidateLayername = new string[3] { "", "", "" };
        private Rect LastRect;

        public override bool IsNeedShow()
        {
            IsNeedPortalSetting = FindObjectOfType<ViveSR_PortalMgr>() != null && !CheckPortalLayer();
            IsNeedStereoSetting = !CheckLayers(DualCameraIndex.LEFT) || !CheckLayers(DualCameraIndex.RIGHT);
            return IsNeedStereoSetting || IsNeedPortalSetting;
        }

        public override void RenderGUI()
        {
            FindCandidate();

            if (IsNeedStereoSetting) OnGUIStereo();

            // portal layer part
            if (IsNeedPortalSetting) OnGUIPortal();

            if (IsStereoInputValid || IsPortalInputValid)
                if (GUILayout.Button(BtnName_UpdateLayers))
                {
                    if (ViveSR_DualCameraRig.Instance != null)
                    {
                        if (IsStereoInputValid) SetStereoLayer();
                        if (IsPortalInputValid) SetPortalLayer();
                        //EditorUtility.DisplayDialog(BtnName_UpdateLayers, "Done!", "Ok");
                    }
                    else
                    {
                        EditorUtility.DisplayDialog(DialogTitle_Error, DialogTitle_ScriptNotFound, "Cancel");
                    }
                }
        }

        private void OnGUIStereo()
        {
            GUILayout.Label(Label_LayerSetting);
            GUILayout.BeginHorizontal();
            GUILayout.Label(LefteyeLayerName + ":  ");
            LastRect = GUILayoutUtility.GetLastRect();
            CandidateLayername[(int)DualCameraIndex.LEFT] = GUI.TextField(new Rect(LastRect.x + 130, LastRect.y, 25, 20), CandidateLayername[(int)DualCameraIndex.LEFT], 2);
            CandidateLayername[(int)DualCameraIndex.LEFT] = Regex.Replace(CandidateLayername[(int)DualCameraIndex.LEFT], @"[^0-9.]", "");
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(RighteyeLayerName + ":");
            LastRect = GUILayoutUtility.GetLastRect();
            CandidateLayername[(int)DualCameraIndex.RIGHT] = GUI.TextField(new Rect(LastRect.x + 130, LastRect.y, 25, 20), CandidateLayername[(int)DualCameraIndex.RIGHT], 2);
            CandidateLayername[(int)DualCameraIndex.RIGHT] = Regex.Replace(CandidateLayername[(int)DualCameraIndex.RIGHT], @"[^0-9.]", "");
            GUILayout.EndHorizontal();

            System.Int32.TryParse(CandidateLayername[(int)DualCameraIndex.LEFT], out candidateLayerIndex[(int)DualCameraIndex.LEFT]);
            System.Int32.TryParse(CandidateLayername[(int)DualCameraIndex.RIGHT], out candidateLayerIndex[(int)DualCameraIndex.RIGHT]);

            GUILayout.FlexibleSpace();
            if (CandidateLayername[(int)DualCameraIndex.LEFT] != CandidateLayername[(int)DualCameraIndex.RIGHT] &&
                candidateLayerIndex[(int)DualCameraIndex.LEFT] >= 8 &&
                candidateLayerIndex[(int)DualCameraIndex.LEFT] < 32 &&
                candidateLayerIndex[(int)DualCameraIndex.RIGHT] >= 8 &&
                candidateLayerIndex[(int)DualCameraIndex.RIGHT] < 32)
            {
                IsStereoInputValid = true;
            }
            else
                EditorGUILayout.HelpBox(HelpboxText_LayerNotValid, MessageType.Error);
            GUILayout.Label(Label_LayerChangeName);
        }

        private void OnGUIPortal()
        {
            GUILayout.Label(Label_PortalLayerSetting);
            GUILayout.BeginHorizontal();
            GUILayout.Label(VirtualWorldLayerName + ":");
            LastRect = GUILayoutUtility.GetLastRect();
            CandidateLayername[VRWorldLayer] = GUI.TextField(new Rect(LastRect.x + 130, LastRect.y, 25, 20), CandidateLayername[VRWorldLayer], 2);
            CandidateLayername[VRWorldLayer] = Regex.Replace(CandidateLayername[VRWorldLayer], @"[^0-9.]", "");
            GUILayout.EndHorizontal();

            System.Int32.TryParse(CandidateLayername[VRWorldLayer], out candidateLayerIndex[VRWorldLayer]);

            GUILayout.FlexibleSpace();
            if (CandidateLayername[VRWorldLayer] != "" && candidateLayerIndex[VRWorldLayer] >= 8 && candidateLayerIndex[VRWorldLayer] < 32)
            {
                IsPortalInputValid = true;
            }
        }

        private void SetStereoLayer()
        {
            ModifyLayer(System.Int32.Parse(CandidateLayername[(int)DualCameraIndex.LEFT]), LefteyeLayerName);
            ModifyLayer(System.Int32.Parse(CandidateLayername[(int)DualCameraIndex.RIGHT]), RighteyeLayerName);
            UpdateStereoCamCullingMask();
            UpdateSeeThroCamLayers(DualCameraIndex.LEFT);
            UpdateSeeThroCamLayers(DualCameraIndex.RIGHT);
        }

        private void SetPortalLayer()
        {
            ModifyLayer(System.Int32.Parse(CandidateLayername[VRWorldLayer]), VirtualWorldLayerName);
            UpdateStereoCamCullingMask();
            UpdateCollisionMatrix();
        }

        private bool FindCandidate()
        {
            int[] candidateLayer = new int[3] { LayerMask.NameToLayer(LefteyeLayerName), LayerMask.NameToLayer(RighteyeLayerName), LayerMask.NameToLayer(VirtualWorldLayerName) };
            int searchLayer = 31;
            for (int i = 0; i < candidateLayer.Length; i++)
            {
                if (candidateLayer[i] == -1)
                {
                    for (; searchLayer > 8; searchLayer--)
                    {
                        if (LayerMask.LayerToName(searchLayer) == "")
                        {
                            bool occupied = false;
                            for (int j = i; j >= 0; j--) if (candidateLayer[j] == searchLayer) occupied = true;
                            if (occupied) continue;

                            candidateLayer[i] = searchLayer;
                            break;
                        }
                    }
                }
            }

            CandidateLayername[(int)DualCameraIndex.LEFT] = candidateLayer[(int)DualCameraIndex.LEFT].ToString();
            CandidateLayername[(int)DualCameraIndex.RIGHT] = candidateLayer[(int)DualCameraIndex.RIGHT].ToString();
            CandidateLayername[VRWorldLayer] = candidateLayer[VRWorldLayer].ToString();
            return true;
        }

        private bool CheckPortalLayer()
        {
            int layer = LayerMask.NameToLayer(VirtualWorldLayerName);
            return (layer != -1);
        }

        private bool CheckLayers(DualCameraIndex cameraIndex)
        {
            if (ViveSR_DualCameraRig.Instance == null || ViveSR_DualCameraRig.Instance == null) return false;
            string layername = cameraIndex == DualCameraIndex.LEFT ? LefteyeLayerName : RighteyeLayerName;
            int mask = LayerMask.GetMask(layername);
            int layer = LayerMask.NameToLayer(layername);
            Camera cam = cameraIndex == DualCameraIndex.LEFT ?
                ViveSR_DualCameraRig.Instance.DualCameraLeft :
                ViveSR_DualCameraRig.Instance.DualCameraRight;
            ViveSR_TrackedCamera trackedCam = cameraIndex == DualCameraIndex.LEFT ?
                ViveSR_DualCameraRig.Instance.TrackedCameraLeft :
                ViveSR_DualCameraRig.Instance.TrackedCameraRight;
            ViveSR_TrackedCamera trackedCamAnother = cameraIndex == DualCameraIndex.LEFT ?
                ViveSR_DualCameraRig.Instance.TrackedCameraRight :
                ViveSR_DualCameraRig.Instance.TrackedCameraLeft;

            if (cam.cullingMask != mask) return false;
            if (trackedCam.gameObject.layer != layer) return false;
            if (trackedCam.Anchor.gameObject.layer != layer) return false;
            if (trackedCam.ImagePlane.gameObject.layer != layer) return false;
            if (trackedCamAnother.ImagePlaneCalibration.gameObject.layer != layer) return false;
            return true;
        }

        private void UpdateCollisionMatrix()
        {
            Physics.IgnoreLayerCollision(LayerMask.NameToLayer("Default"), LayerMask.NameToLayer(VirtualWorldLayerName));
        }

        private void UpdateStereoCamCullingMask()
        {
            ViveSR_DualCameraRig prefabRig = PrefabUtility.GetCorrespondingObjectFromSource(ViveSR_DualCameraRig.Instance) as ViveSR_DualCameraRig;
            prefabRig.VirtualCamera.cullingMask = -1;
            int LeftEyeLayer = LayerMask.NameToLayer(LefteyeLayerName);
            int RightEyeLayer = LayerMask.NameToLayer(RighteyeLayerName);
            int PortalWorldLayer = LayerMask.NameToLayer(VirtualWorldLayerName);
            if (LeftEyeLayer != -1) prefabRig.VirtualCamera.cullingMask &= ~(1 << LeftEyeLayer);
            if (RightEyeLayer != -1) prefabRig.VirtualCamera.cullingMask &= ~(1 << RightEyeLayer);
            if (PortalWorldLayer != -1) prefabRig.VirtualCamera.cullingMask &= ~(1 << PortalWorldLayer);
        }

        private void UpdateSeeThroCamLayers(DualCameraIndex cameraIndex)
        {
            string layername = cameraIndex == DualCameraIndex.LEFT ? LefteyeLayerName : RighteyeLayerName;
            int mask = LayerMask.GetMask(layername);
            int layer = LayerMask.NameToLayer(layername);
            ViveSR_DualCameraRig prefabRig = PrefabUtility.GetCorrespondingObjectFromSource(ViveSR_DualCameraRig.Instance) as ViveSR_DualCameraRig;

            Camera cam = cameraIndex == DualCameraIndex.LEFT ?
                prefabRig.DualCameraLeft :
                prefabRig.DualCameraRight;
            ViveSR_TrackedCamera trackedCam = cameraIndex == DualCameraIndex.LEFT ?
                prefabRig.TrackedCameraLeft :
                prefabRig.TrackedCameraRight;
            ViveSR_TrackedCamera trackedCamAnother = cameraIndex == DualCameraIndex.LEFT ?
                prefabRig.TrackedCameraRight :
                prefabRig.TrackedCameraLeft;

            cam.cullingMask = mask;
            trackedCam.gameObject.layer = layer;
            trackedCam.Anchor.gameObject.layer = layer;
            trackedCam.ImagePlane.gameObject.layer = layer;
            trackedCamAnother.ImagePlaneCalibration.gameObject.layer = layer;
        }

        private bool ModifyLayer(int index, string name)
        {
            if (index < 0 || index > 31) return false;
            if (LayerMask.NameToLayer(name) == index) return true;
            SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty layers = tagManager.FindProperty("layers");
            if (layers == null) return false;
            if (!layers.isArray) return false;
            layers.GetArrayElementAtIndex(index).stringValue = name;
            tagManager.ApplyModifiedProperties();
            return true;
        }
    }

    public class Page_Finish : ViveSR_Settings.Page
    {
        public override string Name { get { return "Finish"; } }

        private bool click = false;
        public override bool IsNeedShow()
        {
            return !click;
        }

        public override void RenderGUI()
        {
            GUILayout.Label("Let's start SRWorks!");
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Close")) click = true;
        }
    }

    public class Page_QualitySetting : ViveSR_Settings.Page
    {
        public override string Name { get { return "Quality Setting"; } }

        private const short X_FROM = 210;
        private const short X_TO = 265;
        private const short W_ITEM = 50;
        private const short H_ITEM = 20;
        public bool AutoQualitySetting
        {
            get { return EditorPrefs.GetBool("ViveSR_AutoQualitySetting", true); }
            set { EditorPrefs.SetBool("ViveSR_AutoQualitySetting", value); }
        }
        ShadowQuality recommended_Shadows = ShadowQuality.All;
        ShadowResolution recommended_ShadowResolution = ShadowResolution.High;
        int recommended_ShadowDistance = 20;
        int recommended_ShadowCascades = 2;

        bool IsOK_ShadowQuality { get { return QualitySettings.shadows == recommended_Shadows; } }
        bool IsOK_ShadowResolution { get { return (int)QualitySettings.shadowResolution >= (int)recommended_ShadowResolution; } }
        bool IsOK_ShadowDistance { get { return QualitySettings.shadowDistance <= recommended_ShadowDistance; } }
        bool IsOK_ShadowCascades { get { return QualitySettings.shadowCascades == recommended_ShadowCascades; } }

        public override bool IsNeedShow()
        {
            AutoQualitySetting = true;
            bool show = AutoQualitySetting && !(IsOK_ShadowQuality && IsOK_ShadowResolution &&
                                                IsOK_ShadowDistance && IsOK_ShadowCascades);
            return show;
        }

        public override void RenderGUI()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Item", EditorStyles.boldLabel);
            Rect rect = GUILayoutUtility.GetLastRect();
            GUI.Label(new Rect(rect.x + X_FROM, rect.y, W_ITEM, H_ITEM), "From", EditorStyles.boldLabel);
            GUI.Label(new Rect(rect.x + X_TO, rect.y, W_ITEM, H_ITEM), "To", EditorStyles.boldLabel);
            GUILayout.EndHorizontal();

            MyLabel("QualitySettings.Shadows ", QualitySettings.shadows.ToString(), recommended_Shadows.ToString(), IsOK_ShadowQuality);
            MyLabel("QualitySettings.ShadowResolution ", QualitySettings.shadowResolution.ToString(), recommended_ShadowResolution.ToString(), IsOK_ShadowResolution);
            MyLabel("QualitySettings.ShadowDistance ", QualitySettings.shadowDistance.ToString(), recommended_ShadowDistance.ToString(), IsOK_ShadowDistance);
            MyLabel("QualitySettings.ShadowCascades ", QualitySettings.shadowCascades.ToString(), recommended_ShadowCascades.ToString(), IsOK_ShadowCascades);

            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Ignore")) AutoQualitySetting = false;
            if (GUILayout.Button("Accept")) ChangeQualitySetting();
            GUILayout.EndHorizontal();
        }

        private void MyLabel(string label, string current, string target, bool isOK)
        {
            if (isOK) return;
            GUILayout.BeginHorizontal();
            GUILayout.Label(label);
            Rect rect = GUILayoutUtility.GetLastRect();
            GUI.Label(new Rect(rect.x + X_FROM, rect.y, W_ITEM, H_ITEM), current);
            GUI.Label(new Rect(rect.x + X_TO, rect.y, W_ITEM, H_ITEM), target);
            GUILayout.EndHorizontal();
        }

        private void ChangeQualitySetting()
        {
            QualitySettings.shadows = recommended_Shadows;
            if ((int)QualitySettings.shadowResolution < (int)recommended_ShadowResolution) QualitySettings.shadowResolution = recommended_ShadowResolution;
            if (QualitySettings.shadowDistance > recommended_ShadowDistance) QualitySettings.shadowDistance = recommended_ShadowDistance;
            QualitySettings.shadowCascades = recommended_ShadowCascades;
        }
    }
}
#endif