//========= Copyright 2017, HTC Corporation. All rights reserved. ===========

using UnityEngine;

namespace Vive.Plugin.SR
{
    public class ViveSR_HMDCameraShifter : MonoBehaviour
    {
        [SerializeField] private Camera TargetCamera;
        public float CameraShiftZ = 0.071f;

        private void Update()
        {
            transform.localPosition = CameraShiftZ * TargetCamera.transform.forward;
        }
    }
}