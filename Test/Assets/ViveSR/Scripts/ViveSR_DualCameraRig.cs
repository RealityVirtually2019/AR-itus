//========= Copyright 2017, HTC Corporation. All rights reserved. ===========

using UnityEngine;
using System.Collections;

namespace Vive.Plugin.SR
{
    [ExecuteInEditMode]
    public class ViveSR_DualCameraRig : ViveSR_Module
    {
        public Camera OriginalCamera;
        public Camera VirtualCamera;
        public Camera DualCameraLeft;
        public Camera DualCameraRight;
        public ViveSR_DualCameraImageRenderer DualCameraImageRenderer;
        public ViveSR_DualCameraCalibrationTool DualCameraCalibration;
        public ViveSR_TrackedCamera TrackedCameraLeft;
        public ViveSR_TrackedCamera TrackedCameraRight;
        public ViveSR_HMDCameraShifter HMDCameraShifter;

        public DualCameraDisplayMode Mode = DualCameraDisplayMode.MIX;
        public static DualCameraStatus DualCameraStatus { get; private set; }
        public static string LastError { get; private set; }

        private ViveSR_DualCameraRig() { }
        private static ViveSR_DualCameraRig Mgr = null;
        public static ViveSR_DualCameraRig Instance
        {
            get
            {
                if (Mgr == null)
                {
                    Mgr = FindObjectOfType<ViveSR_DualCameraRig>();
                }
                //if (Mgr == null)
                //{
                //    Debug.LogError("ViveSR_DualCameraManager does not be attached on GameObject");
                //}
                return Mgr;
            }
        }

        private void Awake()
        {
#if UNITY_EDITOR
            if (Application.isEditor) ViveSR_Settings.Update();
#endif
        }

        public override bool Initial()
        {
            DualCameraStatus = DualCameraStatus.IDLE;
            if (!ViveSR.EnableUnitySeeThrough)
            {
                return false;
            }
            if (ViveSR.FrameworkStatus == FrameworkStatus.WORKING)
            {
                int result = ViveSR_DualCameraImageCapture.Initial();
                if (result != (int)Error.WORK)
                {
                    DualCameraStatus = DualCameraStatus.ERROR;
                    LastError = "[ViveSR] Initial Camera error " + result;
                    Debug.LogError(LastError);
                    return false;
                }

                if (DualCameraCalibration != null)
                {
                    DualCameraCalibration.LoadDeviceParameter();
                }
                if (TrackedCameraLeft != null)
                {
                    if (TrackedCameraLeft.ImagePlane != null) TrackedCameraLeft.ImagePlane.Initial();
                    if (TrackedCameraLeft.ImagePlaneCalibration != null)
                    {
                        TrackedCameraLeft.ImagePlaneCalibration.Initial();
                        TrackedCameraLeft.ImagePlaneCalibration.gameObject.SetActive(false);
                    }
                }
                if (TrackedCameraRight != null)
                {
                    if (TrackedCameraRight.ImagePlane != null) TrackedCameraRight.ImagePlane.Initial();
                    if (TrackedCameraRight.ImagePlaneCalibration != null)
                    {
                        TrackedCameraRight.ImagePlaneCalibration.Initial();
                        TrackedCameraRight.ImagePlaneCalibration.gameObject.SetActive(false);
                    }
                }
                DualCameraStatus = DualCameraStatus.WORKING;
                SetMode(Mode);
                return true;
            }       
            return false;
        }

        public override bool Release()
        {
            DualCameraStatus = DualCameraStatus.IDLE;
            if (!ViveSR.EnableUnitySeeThrough)
            {
                return false;
            }
            ViveSR_DualCameraImageCapture.EnableDepthProcess(false);
            ViveSR_DualCameraImageCapture.Release();
            if (DualCameraCalibration != null)
                DualCameraCalibration.SaveDeviceParameter();
            return true;
        }

        /// <summary>
        /// Decide whether real/virtual camera render or not.
        /// </summary>
        /// <param name="mode">Virtual, Real and Mix</param>
        public void SetMode(DualCameraDisplayMode mode)
        {
            if (OriginalCamera == null)
            {
                if (Camera.main == VirtualCamera) VirtualCamera.tag = "Untagged";
                OriginalCamera = Camera.main;
                VirtualCamera.tag = "MainCamera";
            }
            switch (mode)
            {
                case DualCameraDisplayMode.VIRTUAL:
                    if (OriginalCamera != VirtualCamera && OriginalCamera != null) OriginalCamera.enabled = true;
                    EnableViveCamera(false);
                    break;
                case DualCameraDisplayMode.REAL:
                    if (OriginalCamera != VirtualCamera && OriginalCamera != null) OriginalCamera.enabled = false;
                    EnableViveCamera(true, DualCameraMode.REAL);
                    break;
                case DualCameraDisplayMode.MIX:
                    if (OriginalCamera != VirtualCamera && OriginalCamera != null) OriginalCamera.enabled = false;
                    EnableViveCamera(true, DualCameraMode.MIX);
                    break;
            }
        }

        private void EnableViveCamera(bool active, DualCameraMode mode = DualCameraMode.MIX)
        {
            DualCameraImageRenderer.enabled = active;
            VirtualCamera.enabled = mode == DualCameraMode.MIX ? active : false;
            DualCameraLeft.enabled = active;
            DualCameraRight.enabled = active;

            TrackedCameraLeft.gameObject.SetActive(active);
            TrackedCameraRight.gameObject.SetActive(active);
        }
    }
}