//========= Copyright 2017, HTC Corporation. All rights reserved. ===========

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Vive.Plugin.SR
{
    public class ViveSR_DualCameraImageRenderer : MonoBehaviour
    {
        public static bool UpdateDistortedMaterial
        {
            get { return _UpdateDistortedMaterial; }
            set { if (value != _UpdateDistortedMaterial) _UpdateDistortedMaterial = value; SetCallbackEnable(_CallbackMode); }
        }
        public static bool UpdateUndistortedMaterial
        {
            get { return _UpdateUndistortedMaterial; }
            set { if (value != _UpdateUndistortedMaterial) _UpdateUndistortedMaterial = value; SetCallbackEnable(_CallbackMode); }
        }
        public static bool UpdateDepthMaterial
        {
            get { return _UpdateDepthMaterial; }
            set { if (value != _UpdateDepthMaterial) _UpdateDepthMaterial = value; SetCallbackEnable(_CallbackMode); }
        }
        public static UndistortionMethod UndistortMethod
        {
            get { return _UndistortMethod; }
            set { if (value != _UndistortMethod) SetUndistortMode(value); }
        }
        public static bool CallbackMode
        {
            get { return _CallbackMode; }
            set { if (value != _CallbackMode) SetCallbackEnable(value);}
        }        
        public static bool DepthImageOcclusion 
        { 
            get { return _DepthImageOcclusion; }
            set { if (value != _DepthImageOcclusion) SetDepthImageOcclusionEnable(value); } 
        }
        public static bool VisualizeDepthOcclusion
        {
            get { return _VisualizeDepthOcclusion; }
            set { if (value != _VisualizeDepthOcclusion) SetDepthOcclusionVisualized(value); }
        }
        public static float OcclusionNearDistance
        {
            get { return _OcclusionNearDistance; }
            set { if (value != _OcclusionNearDistance) SetDepthOcclusionNearDistance(value); }
        }
        public static float OcclusionFarDistance
        {
            get { return _OcclusionFarDistance; }
            set { if (value != _OcclusionFarDistance) SetDepthOcclusionFarDistance(value); }
        }
        private static bool _UpdateDistortedMaterial = false;
        private static bool _UpdateUndistortedMaterial = false;
        private static bool _UpdateDepthMaterial = false;
        private static bool _CallbackMode = false;
        private static bool _DepthImageOcclusion = false;
        private static bool _VisualizeDepthOcclusion = false;
        private static float _OcclusionNearDistance = 0.2f;
        private static float _OcclusionFarDistance = 2.0f;
        private static UndistortionMethod _UndistortMethod = UndistortionMethod.DEFISH_BY_SRMODULE;

        public List<Material> DistortedLeft;
        public List<Material> DistortedRight;
        public List<Material> UndistortedLeft;
        public List<Material> UndistortedRight;
        public List<Material> Depth;

        private ViveSR_Timer DistortedTimer = new ViveSR_Timer();
        private ViveSR_Timer UndistortedTimer = new ViveSR_Timer();
        private ViveSR_Timer DepthTimer = new ViveSR_Timer();
        public static float RealDistortedFPS;
        public static float RealUndistortedFPS;
        public static float RealDepthFPS;
        private int LastDistortedTextureUpdateTime = 0;
        private int LastUndistortedTextureUpdateTime = 0;
        private int LastDepthTextureUpdateTime = 0;
        private Matrix4x4[] PoseDistorted = new Matrix4x4[2];
        private Matrix4x4[] PoseUndistorted = new Matrix4x4[2];
        private Texture2D[] TextureUndistorted = new Texture2D[2];
        private bool EnablePreRender = true;

        private delegate void UnityRenderEvent(int eventID);
        private System.IntPtr PtrIssuePluginEvent;

        private void Start()
        {
            SetUndistortMode(_UndistortMethod);
            PtrIssuePluginEvent = System.Runtime.InteropServices.Marshal.GetFunctionPointerForDelegate((UnityRenderEvent)(ViveSR_Framework.UnityRenderEvent));
            Camera.onPreRender += PreRender;
        }

        private void OnDestroy()
        {
            Camera.onPreRender -= PreRender;
        }

        private void PreRender(Camera eye)
        {
            if (EnablePreRender == false)
                return;

            if (LastDistortedTextureUpdateTime != 0 || LastUndistortedTextureUpdateTime != 0)
            {
                if (ViveSR_DualCameraRig.Instance.DualCameraLeft == eye || ViveSR_DualCameraRig.Instance.DualCameraRight == eye)
                {
                    if (UpdateUndistortedMaterial == true && _UndistortMethod == UndistortionMethod.DEFISH_BY_SRMODULE)
                    {
                        ViveSR_DualCameraRig.Instance.TrackedCameraLeft.transform.localPosition = ViveSR_DualCameraImageCapture.Position(PoseUndistorted[(int)DualCameraIndex.LEFT]);
                        ViveSR_DualCameraRig.Instance.TrackedCameraLeft.transform.localRotation = ViveSR_DualCameraImageCapture.Rotation(PoseUndistorted[(int)DualCameraIndex.LEFT]);
                        ViveSR_DualCameraRig.Instance.TrackedCameraRight.transform.localPosition = ViveSR_DualCameraImageCapture.Position(PoseUndistorted[(int)DualCameraIndex.RIGHT]);
                        ViveSR_DualCameraRig.Instance.TrackedCameraRight.transform.localRotation = ViveSR_DualCameraImageCapture.Rotation(PoseUndistorted[(int)DualCameraIndex.RIGHT]);
                    }
                    else if (_UpdateDistortedMaterial)
                    {
                        ViveSR_DualCameraRig.Instance.TrackedCameraLeft.transform.localPosition = ViveSR_DualCameraImageCapture.Position(PoseDistorted[(int)DualCameraIndex.LEFT]);
                        ViveSR_DualCameraRig.Instance.TrackedCameraLeft.transform.localRotation = ViveSR_DualCameraImageCapture.Rotation(PoseDistorted[(int)DualCameraIndex.LEFT]);
                        ViveSR_DualCameraRig.Instance.TrackedCameraRight.transform.localPosition = ViveSR_DualCameraImageCapture.Position(PoseDistorted[(int)DualCameraIndex.RIGHT]);
                        ViveSR_DualCameraRig.Instance.TrackedCameraRight.transform.localRotation = ViveSR_DualCameraImageCapture.Rotation(PoseDistorted[(int)DualCameraIndex.RIGHT]);
                    }
                }
            }
        }

        private IEnumerator UpdateGPUUndistortTexture()
        {
            yield return new WaitForEndOfFrame();
            GL.IssuePluginEvent(PtrIssuePluginEvent, ViveSR_Framework.MODULE_ID_SEETHROUGH);
        }
        private void Update()
        {
            if (ViveSR_DualCameraRig.DualCameraStatus == DualCameraStatus.WORKING)
            {
                if (!CallbackMode)
                {
                    if (UpdateDistortedMaterial)
                    {
                        // native buffer ptr method 1: 
                        // get native buffer ptr & let native(cpp) do texture upload
                        //StartCoroutine(UpdateGPUFishEyeTexture());
                        ViveSR_DualCameraImageCapture.UpdateDistortedImage();
                    }
                    if (UpdateUndistortedMaterial)
                    {
                        if(ViveSR_DualCameraImageCapture.UndistortTextureIsNative) 
                            StartCoroutine(UpdateGPUUndistortTexture());
                        ViveSR_DualCameraImageCapture.UpdateUndistortedImage();
                    }
                    if (UpdateDepthMaterial) ViveSR_DualCameraImageCapture.UpdateDepthImage();


                }

                #region Distorted Image
                if (_UpdateDistortedMaterial)
                {
                    int currentCameraTimeIndex = ViveSR_DualCameraImageCapture.DistortedTimeIndex;
                    if (currentCameraTimeIndex != LastDistortedTextureUpdateTime)
                    {
                        DistortedTimer.Add(currentCameraTimeIndex - LastDistortedTextureUpdateTime);
                        RealDistortedFPS = 1000 / DistortedTimer.AverageLeast(100);
                        int frameIndex, timeIndex;
                        Texture2D textureCameraLeft, textureCameraRight;
                        ViveSR_DualCameraImageCapture.GetDistortedTexture(out textureCameraLeft, out textureCameraRight, out frameIndex, out timeIndex, out PoseDistorted[(int)DualCameraIndex.LEFT], out PoseDistorted[(int)DualCameraIndex.RIGHT]);
                        for (int i = 0; i < DistortedLeft.Count; i++)
                        {
                            if (DistortedLeft[i] != null)
                            {
                                DistortedLeft[i].mainTexture = textureCameraLeft;
                                if (ViveSR_DualCameraImageCapture.DistortTextureIsNative)
                                {
                                    DistortedLeft[i].mainTextureScale = new Vector2(1, 0.5f);
                                    DistortedLeft[i].mainTextureOffset = new Vector2(0, 0.5f);
                                }
                            }
                        }
                        for (int i = 0; i < DistortedRight.Count; i++)
                        {
                            if (DistortedRight[i] != null)
                            {
                                DistortedRight[i].mainTexture = textureCameraRight;
                                if (ViveSR_DualCameraImageCapture.DistortTextureIsNative)
                                {
                                    DistortedRight[i].mainTextureScale = new Vector2(1, 0.5f);
                                }
                            }
                        }
                        LastDistortedTextureUpdateTime = currentCameraTimeIndex;

                        //change pose update flow to camera preRender
                        if (EnablePreRender == false)
                        {
                            ViveSR_DualCameraRig.Instance.TrackedCameraLeft.transform.localPosition = ViveSR_DualCameraImageCapture.Position(PoseDistorted[(int)DualCameraIndex.LEFT]);
                            ViveSR_DualCameraRig.Instance.TrackedCameraLeft.transform.localRotation = ViveSR_DualCameraImageCapture.Rotation(PoseDistorted[(int)DualCameraIndex.LEFT]);
                            ViveSR_DualCameraRig.Instance.TrackedCameraRight.transform.localPosition = ViveSR_DualCameraImageCapture.Position(PoseDistorted[(int)DualCameraIndex.RIGHT]);
                            ViveSR_DualCameraRig.Instance.TrackedCameraRight.transform.localRotation = ViveSR_DualCameraImageCapture.Rotation(PoseDistorted[(int)DualCameraIndex.RIGHT]);
                        }
                    }
                }
                #endregion

                #region Undistorted Image
                if (_UpdateUndistortedMaterial)
                {
                    int currentUndistortedTimeIndex = ViveSR_DualCameraImageCapture.UndistortedTimeIndex;
                    if (currentUndistortedTimeIndex != LastUndistortedTextureUpdateTime)
                    {
                        UndistortedTimer.Add(currentUndistortedTimeIndex - LastUndistortedTextureUpdateTime);
                        RealUndistortedFPS = 1000 / UndistortedTimer.AverageLeast(100);
                        int FrameIndexUndistorted, TimeIndexUndistorted;
                        ViveSR_DualCameraImageCapture.GetUndistortedTexture(out TextureUndistorted[(int)DualCameraIndex.LEFT], out TextureUndistorted[(int)DualCameraIndex.RIGHT], out FrameIndexUndistorted, out TimeIndexUndistorted, out PoseUndistorted[(int)DualCameraIndex.LEFT], out PoseUndistorted[(int)DualCameraIndex.RIGHT]);
                        for (int i = 0; i < UndistortedLeft.Count; i++)
                        {
                            if (UndistortedLeft[i] != null)
                            {
                                UndistortedLeft[i].mainTexture = TextureUndistorted[(int)DualCameraIndex.LEFT];
                                // restore the tiling / offset which may be modified
                                if (ViveSR_DualCameraImageCapture.DistortTextureIsNative)
                                {
                                    UndistortedLeft[i].mainTextureScale = Vector2.one;
                                    UndistortedLeft[i].mainTextureOffset = Vector2.zero;
                                }
                            }
                        }
                        for (int i = 0; i < UndistortedRight.Count; i++)
                        {
                            if (UndistortedRight[i] != null)
                            {
                                UndistortedRight[i].mainTexture = TextureUndistorted[(int)DualCameraIndex.RIGHT];
                                // restore the tiling / offset which may be modified
                                if (ViveSR_DualCameraImageCapture.DistortTextureIsNative)
                                {
                                    UndistortedRight[i].mainTextureScale = Vector2.one;
                                }
                            }
                        }
                        LastUndistortedTextureUpdateTime = currentUndistortedTimeIndex;

                        if (_UndistortMethod == UndistortionMethod.DEFISH_BY_SRMODULE && EnablePreRender == false)
                        {
                            ViveSR_DualCameraRig.Instance.TrackedCameraLeft.transform.localPosition = ViveSR_DualCameraImageCapture.Position(PoseUndistorted[(int)DualCameraIndex.LEFT]);
                            ViveSR_DualCameraRig.Instance.TrackedCameraLeft.transform.localRotation = ViveSR_DualCameraImageCapture.Rotation(PoseUndistorted[(int)DualCameraIndex.LEFT]);
                            ViveSR_DualCameraRig.Instance.TrackedCameraRight.transform.localPosition = ViveSR_DualCameraImageCapture.Position(PoseUndistorted[(int)DualCameraIndex.RIGHT]);
                            ViveSR_DualCameraRig.Instance.TrackedCameraRight.transform.localRotation = ViveSR_DualCameraImageCapture.Rotation(PoseUndistorted[(int)DualCameraIndex.RIGHT]);
                        }
                    }
                }
                #endregion

                #region Depth Image
                if (_UpdateDepthMaterial)
                {
                    int currentDepthTimeIndex = ViveSR_DualCameraImageCapture.DepthTimeIndex;
                    if (currentDepthTimeIndex != LastDepthTextureUpdateTime)
                    {
                        DepthTimer.Add(currentDepthTimeIndex - LastDepthTextureUpdateTime);
                        RealDepthFPS = 1000 / DepthTimer.AverageLeast(100);
                        int frameIndex, timeIndex;
                        Texture2D textureDepth;
                        Matrix4x4 PoseDepth;
                        ViveSR_DualCameraImageCapture.GetDepthTexture(out textureDepth, out frameIndex, out timeIndex, out PoseDepth);
                        for (int i = 0; i < Depth.Count; i++)
                        {
                            if (Depth[i] != null) Depth[i].mainTexture = textureDepth;
                        }
                        LastDepthTextureUpdateTime = currentDepthTimeIndex;
                    }
                }
                #endregion
            }
        }

        private void OnDisable()
        {
            SetCallbackEnable(false);

            // restore the tiling / offset which may be modified
            for (int i = 0; i < DistortedLeft.Count; i++)
            {
                if (DistortedLeft[i] != null)
                {
                    DistortedLeft[i].mainTextureScale = Vector2.one;
                    DistortedLeft[i].mainTextureOffset = Vector2.zero;
                }
            }
            for (int i = 0; i < DistortedRight.Count; i++)
            {
                if (DistortedRight[i] != null)
                {
                    DistortedRight[i].mainTextureScale = Vector2.one;
                }
            }
        }

        private static void SetUndistortMode(UndistortionMethod method)
        {
            _UndistortMethod = method; 
            if (_UndistortMethod == UndistortionMethod.DEFISH_BY_SRMODULE)
            {
                UpdateDistortedMaterial = false;
                UpdateUndistortedMaterial = true;
            }
            else
            {
                UpdateDistortedMaterial = true;
                UpdateUndistortedMaterial = false;
            }
            ViveSR_DualCameraRig.Instance.TrackedCameraLeft.ImagePlane.SetUndistortMethod(UndistortMethod);
            ViveSR_DualCameraRig.Instance.TrackedCameraRight.ImagePlane.SetUndistortMethod(UndistortMethod);
            ViveSR_DualCameraRig.Instance.TrackedCameraLeft.ImagePlaneCalibration.SetUndistortMethod(UndistortMethod);
            ViveSR_DualCameraRig.Instance.TrackedCameraRight.ImagePlaneCalibration.SetUndistortMethod(UndistortMethod);
        }

        private static void SetDepthImageOcclusionEnable(bool enable)
        {
            ViveSR_DualCameraRig.Instance.TrackedCameraLeft.DepthImageOccluder.gameObject.SetActive(enable);
            ViveSR_DualCameraRig.Instance.TrackedCameraRight.DepthImageOccluder.gameObject.SetActive(enable);
            _DepthImageOcclusion = enable;
        }

        private static void SetDepthOcclusionVisualized(bool enable)
        {
            ViveSR_DualCameraRig.Instance.TrackedCameraLeft.DepthImageOccluder.sharedMaterial.SetInt("_ColorWrite", enable? 15 : 0);
            ViveSR_DualCameraRig.Instance.TrackedCameraRight.DepthImageOccluder.sharedMaterial.SetInt("_ColorWrite", enable ? 15 : 0);
            _VisualizeDepthOcclusion = enable;
        }

        private static void SetDepthOcclusionNearDistance(float value)
        {
            _OcclusionNearDistance = Mathf.Min(value, _OcclusionFarDistance);
            ViveSR_DualCameraRig.Instance.TrackedCameraLeft.DepthImageOccluder.sharedMaterial.SetFloat("_MinDepth", _OcclusionNearDistance);
            ViveSR_DualCameraRig.Instance.TrackedCameraRight.DepthImageOccluder.sharedMaterial.SetFloat("_MinDepth", _OcclusionNearDistance);
        }

        private static void SetDepthOcclusionFarDistance(float value)
        {
            _OcclusionFarDistance = Mathf.Max(value, _OcclusionNearDistance);
            ViveSR_DualCameraRig.Instance.TrackedCameraLeft.DepthImageOccluder.sharedMaterial.SetFloat("_MaxDepth", _OcclusionFarDistance);
            ViveSR_DualCameraRig.Instance.TrackedCameraRight.DepthImageOccluder.sharedMaterial.SetFloat("_MaxDepth", _OcclusionFarDistance);
        }

        

        private static void SetCallbackEnable(bool enable)
        {
            if (enable == _CallbackMode) return;
            if (enable)
            {
                if (UpdateDistortedMaterial) ViveSR_DualCameraImageCapture.RegisterDistortedCallback();
                if (UpdateUndistortedMaterial) ViveSR_DualCameraImageCapture.RegisterUndistortedCallback();
                if (UpdateDepthMaterial) ViveSR_DualCameraImageCapture.RegisterDepthCallback();
            }
            else
            {
                ViveSR_DualCameraImageCapture.UnregisterDistortedCallback();
                ViveSR_DualCameraImageCapture.UnregisterUndistortedCallback();
                ViveSR_DualCameraImageCapture.UnregisterDepthCallback();
            }
            _CallbackMode = enable;
        }
    }
}
