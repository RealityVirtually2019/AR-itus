using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace Vive.Plugin.SR
{
    [RequireComponent(typeof(Material))]
    public class ViveSR_DepthWarp : MonoBehaviour
    {
        public ComputeShader _computeShader;
        public Material _renderMat;

        private int _kernel;        
        private RenderTexture _warpDepth;
        private Material _clearMat;   

        private Vector4 _depthParam = new Vector4();        // focalL, baseline, minDepth, maxDepth
        private int _width;
        private int _height;

        void Start()
        {
            if (_computeShader != null )
            {
                _clearMat = new Material(Shader.Find("Unlit/Color"));
                _clearMat.color = Color.black;

                // kernel
                _kernel = _computeShader.FindKernel("CSMain");

                // constant buffer
                _width = ViveSR_DualCameraImageCapture.DepthImageWidth;
                _height = ViveSR_DualCameraImageCapture.DepthImageHeight;
                _depthParam.x = (float)ViveSR_DualCameraImageCapture.FocalLength_L;
                _depthParam.y = (float)ViveSR_DualCameraImageCapture.Baseline;
                _depthParam.z = ViveSR_DualCameraImageRenderer.OcclusionNearDistance;
                _depthParam.w = ViveSR_DualCameraImageRenderer.OcclusionFarDistance;

                // input texture
                int frameIndex, timeIndex;
                Texture2D textureDepth;
                Matrix4x4 Pose_L;
                ViveSR_DualCameraImageCapture.GetDepthTexture(out textureDepth, out frameIndex, out timeIndex, out Pose_L);

                // result texture
                _warpDepth = new RenderTexture(_width, _height, 0, RenderTextureFormat.RFloat);
                _warpDepth.enableRandomWrite = true;
                _warpDepth.Create();

                // bind
                _computeShader.SetInt("ImageWidth", _width);
                _computeShader.SetVector("DepthParam", _depthParam);
                _computeShader.SetTexture(_kernel, "DepthInput", textureDepth);
                _computeShader.SetTexture(_kernel, "Result", _warpDepth);
            }                
        }

        void OnDestroy()
        {
            if (_warpDepth != null)
                _warpDepth.Release();
        }
        
        // Update is called once per frame
        void Update()
        {
            if (_computeShader != null && _renderMat != null)
            {
                _RunShader();
                _renderMat.mainTexture = _warpDepth;
            }     
        }

        void _RunShader()
        {
            // Clear RT        
            Graphics.Blit( null, _warpDepth, _clearMat);

            // Warp
            if (_depthParam.z != ViveSR_DualCameraImageRenderer.OcclusionNearDistance || _depthParam.w != ViveSR_DualCameraImageRenderer.OcclusionFarDistance)
            {
                _depthParam.z = ViveSR_DualCameraImageRenderer.OcclusionNearDistance;
                _depthParam.w = ViveSR_DualCameraImageRenderer.OcclusionFarDistance;
                _computeShader.SetVector("DepthParam", _depthParam);
            }            

            _computeShader.Dispatch(_kernel, _width / 8, _height / 8, 1);
        }
    }
}

