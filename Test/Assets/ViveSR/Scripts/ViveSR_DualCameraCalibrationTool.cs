using Microsoft.Win32;
using System;
using UnityEngine;

namespace Vive.Plugin.SR
{
    public class ViveSR_DualCameraCalibrationTool : MonoBehaviour
    {
        public static bool IsCalibrating;
        public static CalibrationType CurrentCalibrationType;

        private Vector3 RelativeAngle;
        private Vector3 AbsoluteAngle;

        private string rootPaht = "HKEY_CURRENT_USER\\Vive SRWorks";
        private string keyNameRelativeAngle = "RelativeAngle";
        private string keyNameAbsoluteAngle = "AbsoluteAngle";

        public void SetCalibrationMode(bool active, CalibrationType calibrationType = CalibrationType.ABSOLUTE)
        {
            if (ViveSR_DualCameraRig.Instance.TrackedCameraLeft == null || ViveSR_DualCameraRig.Instance.TrackedCameraRight == null) return;
            CurrentCalibrationType = calibrationType;
            IsCalibrating = active;
            if (IsCalibrating)
            {
                ViveSR_DualCameraRig.Instance.TrackedCameraRight.ImagePlaneCalibration.gameObject.SetActive(calibrationType == CalibrationType.RELATIVE);
            }
            else
            {
                ViveSR_DualCameraRig.Instance.TrackedCameraRight.ImagePlaneCalibration.gameObject.SetActive(false);
            }
        }

        public void Calibration(CalibrationAxis axis, float angle)
        {
            if (ViveSR_DualCameraRig.Instance.TrackedCameraLeft == null || ViveSR_DualCameraRig.Instance.TrackedCameraRight == null) return;
            Vector3 vectorAxis = Vector3.zero;
            switch (axis)
            {
                case CalibrationAxis.X:
                    vectorAxis = Vector3.right;
                    break;
                case CalibrationAxis.Y:
                    vectorAxis = Vector3.up;
                    break;
                case CalibrationAxis.Z:
                    vectorAxis = Vector3.forward;
                    break;
            }
            if (CurrentCalibrationType == CalibrationType.RELATIVE)
            {
                ViveSR_DualCameraRig.Instance.TrackedCameraLeft.Anchor.transform.localEulerAngles += vectorAxis * angle;
                RelativeAngle += vectorAxis * angle;
            }
            if (CurrentCalibrationType == CalibrationType.ABSOLUTE)
            {
                ViveSR_DualCameraRig.Instance.TrackedCameraLeft.Anchor.transform.localEulerAngles += vectorAxis * angle;
                ViveSR_DualCameraRig.Instance.TrackedCameraRight.Anchor.transform.localEulerAngles += vectorAxis * angle;
                AbsoluteAngle += vectorAxis * angle;
            }
        }

        public void ResetCalibration()
        {
            CurrentCalibrationType = CalibrationType.RELATIVE;
            Calibration(CalibrationAxis.X, -RelativeAngle.x);
            Calibration(CalibrationAxis.Y, -RelativeAngle.y);
            Calibration(CalibrationAxis.Z, -RelativeAngle.z);

            CurrentCalibrationType = CalibrationType.ABSOLUTE;
            Calibration(CalibrationAxis.X, -AbsoluteAngle.x);
            Calibration(CalibrationAxis.Y, -AbsoluteAngle.y);
            Calibration(CalibrationAxis.Z, -AbsoluteAngle.z);
        }

        /// <summary>
        /// Load the custom calibration parameters from  DualCameraParameters.xml.
        /// </summary>
        public void LoadDeviceParameter()
        {
            foreach (DualCameraIndex camIndex in Enum.GetValues(typeof(DualCameraIndex)))
            {
                ViveSR_TrackedCamera trackedCamera = camIndex == DualCameraIndex.LEFT ? ViveSR_DualCameraRig.Instance.TrackedCameraLeft :
                                                                                        ViveSR_DualCameraRig.Instance.TrackedCameraRight;
                if (trackedCamera != null)
                {
                    int camValue = camIndex == DualCameraIndex.LEFT ? 0 : 1;
                    trackedCamera.Anchor.transform.localPosition = new Vector3(0,
                        -ViveSR_DualCameraImageCapture.OffsetHeadToCamera[camValue * 3 + 1],
                        -ViveSR_DualCameraImageCapture.OffsetHeadToCamera[camValue * 3 + 2]);

                    for (int planeIndex = 0; planeIndex < 2; planeIndex++)
                    {
                        ViveSR_DualCameraImagePlane imagePlane = planeIndex == 0 ? trackedCamera.ImagePlane :
                                                                                   trackedCamera.ImagePlaneCalibration;
                        if (imagePlane != null)
                        {
                            imagePlane.DistortedImageWidth = ViveSR_DualCameraImageCapture.DistortedImageWidth;
                            imagePlane.DistortedImageHeight = ViveSR_DualCameraImageCapture.DistortedImageHeight;
                            imagePlane.UndistortedImageWidth = ViveSR_DualCameraImageCapture.UndistortedImageWidth;
                            imagePlane.UndistortedImageHeight = ViveSR_DualCameraImageCapture.UndistortedImageHeight;
                            if (camIndex == DualCameraIndex.LEFT)
                            {
                                imagePlane.DistortedCx = ViveSR_DualCameraImageCapture.DistortedCx_L;
                                imagePlane.DistortedCy = ViveSR_DualCameraImageCapture.DistortedCy_L;
                                imagePlane.UndistortedCx = ViveSR_DualCameraImageCapture.UndistortedCx_L;
                                imagePlane.UndistortedCy = ViveSR_DualCameraImageCapture.UndistortedCy_L;
                                imagePlane.FocalLength = ViveSR_DualCameraImageCapture.FocalLength_L;
                                imagePlane.UndistortionMap = ViveSR_DualCameraImageCapture.UndistortionMap_L;
                            }
                            else if (camIndex == DualCameraIndex.RIGHT)
                            {
                                imagePlane.DistortedCx = ViveSR_DualCameraImageCapture.DistortedCx_R;
                                imagePlane.DistortedCy = ViveSR_DualCameraImageCapture.DistortedCy_R;
                                imagePlane.UndistortedCx = ViveSR_DualCameraImageCapture.UndistortedCx_R;
                                imagePlane.UndistortedCy = ViveSR_DualCameraImageCapture.UndistortedCy_R;
                                imagePlane.FocalLength = ViveSR_DualCameraImageCapture.FocalLength_R;
                                imagePlane.UndistortionMap = ViveSR_DualCameraImageCapture.UndistortionMap_R;
                            }
                        }
                    }
                }
            }
            ViveSR_DualCameraRig.Instance.HMDCameraShifter.CameraShiftZ = ViveSR_DualCameraImageCapture.OffsetHeadToCamera[2];

            Vector3 relativeAngle = new Vector3(float.Parse((string)Registry.GetValue(rootPaht, keyNameRelativeAngle + "_x", 0)),
                                                float.Parse((string)Registry.GetValue(rootPaht, keyNameRelativeAngle + "_y", 0)),
                                                float.Parse((string)Registry.GetValue(rootPaht, keyNameRelativeAngle + "_z", 0)));
            Vector3 absoluteAngle = new Vector3(float.Parse((string)Registry.GetValue(rootPaht, keyNameAbsoluteAngle + "_x", 0)),
                                                float.Parse((string)Registry.GetValue(rootPaht, keyNameAbsoluteAngle + "_y", 0)),
                                                float.Parse((string)Registry.GetValue(rootPaht, keyNameAbsoluteAngle + "_z", 0)));

            CurrentCalibrationType = CalibrationType.RELATIVE;
            Calibration(CalibrationAxis.X, relativeAngle.x);
            Calibration(CalibrationAxis.Y, relativeAngle.y);
            Calibration(CalibrationAxis.Z, relativeAngle.z);

            CurrentCalibrationType = CalibrationType.ABSOLUTE;
            Calibration(CalibrationAxis.X, absoluteAngle.x);
            Calibration(CalibrationAxis.Y, absoluteAngle.y);
            Calibration(CalibrationAxis.Z, absoluteAngle.z);
        }

        /// <summary>
        /// Save the custom calibration parameters. 
        /// </summary>
        public void SaveDeviceParameter()
        {
            Registry.SetValue(rootPaht, keyNameRelativeAngle + "_x", RelativeAngle.x.ToString());
            Registry.SetValue(rootPaht, keyNameRelativeAngle + "_y", RelativeAngle.y.ToString());
            Registry.SetValue(rootPaht, keyNameRelativeAngle + "_z", RelativeAngle.z.ToString());

            Registry.SetValue(rootPaht, keyNameAbsoluteAngle + "_x", AbsoluteAngle.x.ToString());
            Registry.SetValue(rootPaht, keyNameAbsoluteAngle + "_y", AbsoluteAngle.y.ToString());
            Registry.SetValue(rootPaht, keyNameAbsoluteAngle + "_z", AbsoluteAngle.z.ToString());
        }
    }
}