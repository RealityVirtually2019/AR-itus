//========= Copyright 2017, HTC Corporation. All rights reserved. ===========
//#define USE_DISTORT_TEX_NATIVE_BUFFER
//#define USE_UNDISTORT_TEX_NATIVE_BUFFER

using UnityEngine;
using System.Runtime.InteropServices;
using System;

namespace Vive.Plugin.SR
{
    /// <summary>
    /// This is the wrapper for converting datas to fit unity format.
    /// </summary>
    public class ViveSR_DualCameraImageCapture
    {
        [DllImport("ViveSR_API", CallingConvention = CallingConvention.Cdecl)]
        private static extern int ViveSR_GetCameraParams(ref CameraParams parameter);

        [Obsolete("use ViveSR_Framework.CallbackBasic instead.")]
        public delegate void CallbackWithPose(IntPtr ptr1, IntPtr ptr2, int int1, int int2, IntPtr pose1);
        [Obsolete("use ViveSR_Framework.CallbackBasic instead.")]
        public delegate void CallbackWith2Pose(IntPtr ptr1, IntPtr ptr2, int int1, int int2, IntPtr pose1, IntPtr pose2);

        private static int[] RawDistortedFrameIndex = new int[1];
        private static int[] RawUndistortedFrameIndex = new int[1];
        private static int[] RawDepthFrameIndex = new int[1];

        private static int[] RawDistortedTimeIndex = new int[1];
        private static int[] RawUndistortedTimeIndex = new int[1];
        private static int[] RawDepthTimeIndex = new int[1];

        private static float[] RawDistortedPoseLeft = new float[16];
        private static float[] RawDistortedPoseRight = new float[16];
        private static float[] RawUndistortedPoseLeft = new float[16];
        private static float[] RawUndistortedPoseRight = new float[16];
        private static float[] RawDepthPose = new float[16];
        public static Matrix4x4 DistortedPoseLeft, DistortedPoseRight;
        public static Matrix4x4 UndistortedPoseLeft, UndistortedPoseRight;
        public static Matrix4x4 DepthPose;

        private static DataInfo[] DataInfoDistorted = null;
        private static DataInfo[] DataInfoUndistorted = null;
        private static DataInfo[] DataInfoDepth = null;
        private static bool InitialDistortedPtrSize = false;
        private static bool InitialUndistortedPtrSize = false;
        private static bool InitialDepthPtrSize = false;

        private static Texture2D TextureDistortedLeft;
        private static Texture2D TextureDistortedRight;
        private static Texture2D TextureUndistortedLeft;
        private static Texture2D TextureUndistortedRight;
        private static Texture2D TextureDepth;

        private static CameraParams CameraParameters = new CameraParams();
        public static double DistortedCx_L;
        public static double DistortedCy_L;
        public static double DistortedCx_R;
        public static double DistortedCy_R;
        public static double UndistortedCx_L;
        public static double UndistortedCy_L;
        public static double UndistortedCx_R;
        public static double UndistortedCy_R;
        public static double FocalLength_L;
        public static double FocalLength_R;
        public static double Baseline;
        public static int DistortedImageWidth = 0, DistortedImageHeight = 0, DistortedImageChannel = 0;
        public static int UndistortedImageWidth = 0, UndistortedImageHeight = 0, UndistortedImageChannel = 0;
        public static int DepthImageWidth = 0, DepthImageHeight = 0, DepthImageChannel = 0, DepthDataSize = 4;
        public static float[] OffsetHeadToCamera = new float[6];

        private static int LastDistortedFrameIndex = -1;
        private static int LastUndistortedFrameIndex = -1;
        private static int LastDepthFrameIndex = -1;
        public static float[] UndistortionMap_L;
        public static float[] UndistortionMap_R;

        public static int DistortedFrameIndex { get { return RawDistortedFrameIndex[0]; } }
        public static int DistortedTimeIndex { get { return RawDistortedTimeIndex[0]; } }
        public static int UndistortedFrameIndex { get { return RawUndistortedFrameIndex[0]; } }
        public static int UndistortedTimeIndex { get { return RawUndistortedTimeIndex[0]; } }
        public static int DepthFrameIndex { get { return RawDepthFrameIndex[0]; } }
        public static int DepthTimeIndex { get { return RawDepthTimeIndex[0]; } }

#if USE_DISTORT_TEX_NATIVE_BUFFER
        private static IntPtr nativeTex = IntPtr.Zero;
        private static Texture2D TextureDistortedDeviceRaw;
#endif
        public static bool DistortTextureIsNative = false;
        public static bool UndistortTextureIsNative = false;
        /// <summary>
        /// Initialize the image capturing tool.
        /// </summary>
        /// <returns></returns>
        public static int Initial()
        {
            GetParameters();
            InitialDistortedPtrSize = false;
            InitialUndistortedPtrSize = false;
            InitialDepthPtrSize = false;
            TextureDistortedLeft = new Texture2D(DistortedImageWidth, DistortedImageHeight, TextureFormat.RGBA32, false);
            TextureDistortedRight = new Texture2D(DistortedImageWidth, DistortedImageHeight, TextureFormat.RGBA32, false);
            TextureUndistortedLeft = new Texture2D(UndistortedImageWidth, UndistortedImageHeight, TextureFormat.RGBA32, false);
            TextureUndistortedRight = new Texture2D(UndistortedImageWidth, UndistortedImageHeight, TextureFormat.RGBA32, false);
            TextureDepth = new Texture2D(DepthImageWidth, DepthImageHeight, TextureFormat.RFloat, false);

#if USE_DISTORT_TEX_NATIVE_BUFFER
            DistortTextureIsNative = true;
            var deviceTexture = new Texture2D(2, 2);
            ViveSR_Framework.SetParameterNativePtr(ViveSR_Framework.MODULE_ID_SEETHROUGH, (int)SeeThroughParam.VIDEO_RES_NATIVE_PTR, deviceTexture.GetNativeTexturePtr());
            ViveSR_Framework.SetParameterNativePtr(ViveSR_Framework.MODULE_ID_SEETHROUGH, (int)SeeThroughParam.VIDEO_RES_VIEW_NATIVE_PTR, nativeTex);
#endif

#if USE_UNDISTORT_TEX_NATIVE_BUFFER
            UndistortTextureIsNative = true;
            TextureUndistortedLeft.Apply();
            TextureUndistortedRight.Apply();
            ViveSR_Framework.SetParameterNativePtr(ViveSR_Framework.MODULE_ID_SEETHROUGH, (int)SeeThroughParam.IMAGE_NATIVE_TEXTURE_PTR_L, TextureUndistortedLeft.GetNativeTexturePtr());
            ViveSR_Framework.SetParameterNativePtr(ViveSR_Framework.MODULE_ID_SEETHROUGH, (int)SeeThroughParam.IMAGE_NATIVE_TEXTURE_PTR_R, TextureUndistortedRight.GetNativeTexturePtr());    
#endif

            DataInfoDistorted = ViveSR_Framework.CreateDataInfo(new IntPtr[] {
#if USE_DISTORT_TEX_NATIVE_BUFFER
                IntPtr.Zero,                                                                                // DISTORTED_FRAME_LEFT
                IntPtr.Zero,                                                                                // DISTORTED_FRAME_RIGHT
                IntPtr.Zero,                                                                                // UNDISTORTED_FRAME_LEFT
                IntPtr.Zero,                                                                                // UNDISTORTED_FRAME_RIGHT
#else
                Marshal.AllocCoTaskMem(DistortedImageWidth * DistortedImageHeight * DistortedImageChannel), // LEFT_FRAME
                Marshal.AllocCoTaskMem(DistortedImageWidth * DistortedImageHeight * DistortedImageChannel), // RIGHT_FRAME
                IntPtr.Zero,                                                                                // UNDISTORTED_FRAME_LEFT
                IntPtr.Zero,                                                                                // UNDISTORTED_FRAME_RIGHT
#endif
                Marshal.AllocCoTaskMem(sizeof(int)),                                                        // FRAME_SEQ
                Marshal.AllocCoTaskMem(sizeof(int)),                                                        // TIME_STP
                Marshal.AllocCoTaskMem(sizeof(float) * 16),                                                 // LEFT_POSE
                Marshal.AllocCoTaskMem(sizeof(float) * 16), });                                             // RIGHT_POSE

            DataInfoUndistorted = ViveSR_Framework.CreateDataInfo(new IntPtr[] {
#if USE_UNDISTORT_TEX_NATIVE_BUFFER
                IntPtr.Zero,                                                                                // DISTORTED_FRAME_LEFT
                IntPtr.Zero,                                                                                // DISTORTED_FRAME_RIGHT
                IntPtr.Zero,                                                                                // UNDISTORTED_FRAME_LEFT
                IntPtr.Zero,                                                                                // UNDISTORTED_FRAME_RIGHT
#else
                IntPtr.Zero,                                                                                // DISTORTED_FRAME_LEFT
                IntPtr.Zero,                                                                                // DISTORTED_FRAME_RIGHT
                Marshal.AllocCoTaskMem(UndistortedImageWidth * UndistortedImageHeight * UndistortedImageChannel),// UNDISTORTED_FRAME_LEFT
                Marshal.AllocCoTaskMem(UndistortedImageWidth * UndistortedImageHeight * UndistortedImageChannel),// UNDISTORTED_FRAME_RIGHT
#endif
                Marshal.AllocCoTaskMem(sizeof(int)),                                                             // FRAME_SEQ
                Marshal.AllocCoTaskMem(sizeof(int)),                                                             // TIME_STP
                Marshal.AllocCoTaskMem(sizeof(float) * 16),                                                      // LEFT_POSE
                Marshal.AllocCoTaskMem(sizeof(float) * 16), });                                                  // RIGHT_POSE

            // To use enum DepthDataMask, It needs to assign each type.
            DataInfoDepth = ViveSR_Framework.CreateDataInfo(new IntPtr[] {
                IntPtr.Zero,                                                                                        // LEFT_FRAME
                Marshal.AllocCoTaskMem(DepthImageWidth* DepthImageHeight * DepthImageChannel * DepthDataSize),      // DEPTH_MAP
                Marshal.AllocCoTaskMem(sizeof(int)),                                                                // FRAME_SEQ
                Marshal.AllocCoTaskMem(sizeof(int)),                                                                // TIME_STP
                Marshal.AllocCoTaskMem(sizeof(float) * 16)});                                                       // POSE

            return (int)Error.WORK;
        }

        public static void Release()
        {
            Texture2D.Destroy(TextureDistortedLeft);
            Texture2D.Destroy(TextureDistortedRight);
            Texture2D.Destroy(TextureUndistortedLeft);
            Texture2D.Destroy(TextureUndistortedRight);
            Texture2D.Destroy(TextureDepth);

            TextureDistortedLeft = null;
            TextureDistortedRight = null;
            TextureUndistortedLeft = null;
            TextureUndistortedRight = null;
            TextureDepth = null;
            if (DataInfoDistorted != null)
            {
                foreach (DataInfo data in DataInfoDistorted)
                {
                    if (data.ptr != IntPtr.Zero)
                    {
                        Marshal.FreeCoTaskMem(data.ptr);
                    }
                }
                DataInfoDistorted = null;
            }
            if (DataInfoUndistorted != null)
            {
                foreach (DataInfo data in DataInfoUndistorted)
                {
                    if (data.ptr != IntPtr.Zero)
                    {
                        Marshal.FreeCoTaskMem(data.ptr);
                    }
                }
                DataInfoUndistorted = null;
            }
            if (DataInfoDepth != null)
            {
                foreach (DataInfo data in DataInfoDepth)
                {
                    if (data.ptr != IntPtr.Zero)
                    {
                        Marshal.FreeCoTaskMem(data.ptr);
                    }
                }
                DataInfoDepth = null;
            }
        }

        private static void GetParameters()
        {
            ViveSR_GetCameraParams(ref CameraParameters);
            DistortedCx_L = CameraParameters.Cx_L;
            DistortedCy_L = CameraParameters.Cy_L;
            DistortedCx_R = CameraParameters.Cx_R;
            DistortedCy_R = CameraParameters.Cy_R;
            FocalLength_L = CameraParameters.FocalLength_L;
            FocalLength_R = CameraParameters.FocalLength_R;

            ViveSR_Framework.GetParameterInt(ViveSR_Framework.MODULE_ID_SEETHROUGH, (int)SeeThroughParam.OUTPUT_DISTORTED_WIDTH, ref DistortedImageWidth);
            ViveSR_Framework.GetParameterInt(ViveSR_Framework.MODULE_ID_SEETHROUGH, (int)SeeThroughParam.OUTPUT_DISTORTED_HEIGHT, ref DistortedImageHeight);
            ViveSR_Framework.GetParameterInt(ViveSR_Framework.MODULE_ID_SEETHROUGH, (int)SeeThroughParam.OUTPUT_DISTORTED_CHANNEL, ref DistortedImageChannel);

            ViveSR_Framework.GetParameterInt(ViveSR_Framework.MODULE_ID_SEETHROUGH, (int)SeeThroughParam.OUTPUT_UNDISTORTED_WIDTH, ref UndistortedImageWidth);
            ViveSR_Framework.GetParameterInt(ViveSR_Framework.MODULE_ID_SEETHROUGH, (int)SeeThroughParam.OUTPUT_UNDISTORTED_HEIGHT, ref UndistortedImageHeight);
            ViveSR_Framework.GetParameterInt(ViveSR_Framework.MODULE_ID_SEETHROUGH, (int)SeeThroughParam.OUTPUT_UNDISTORTED_CHANNEL, ref UndistortedImageChannel);

            ViveSR_Framework.GetParameterInt(ViveSR_Framework.MODULE_ID_DEPTH, (int)DepthParam.OUTPUT_WIDTH, ref DepthImageWidth);
            ViveSR_Framework.GetParameterInt(ViveSR_Framework.MODULE_ID_DEPTH, (int)DepthParam.OUTPUT_HEIGHT, ref DepthImageHeight);
            ViveSR_Framework.GetParameterInt(ViveSR_Framework.MODULE_ID_DEPTH, (int)DepthParam.OUTPUT_CHAANEL_1, ref DepthImageChannel);
            ViveSR_Framework.GetParameterDouble(ViveSR_Framework.MODULE_ID_DEPTH, (int)DepthParam.BASELINE, ref Baseline);

            int undistortionMapSize = 0;
            ViveSR_Framework.GetParameterInt(ViveSR_Framework.MODULE_ID_SEETHROUGH, (int)SeeThroughParam.UNDISTORTION_MAP_SIZE, ref undistortionMapSize);
            UndistortionMap_L = new float[undistortionMapSize / sizeof(float)];
            UndistortionMap_R = new float[undistortionMapSize / sizeof(float)];
            ViveSR_Framework.GetParameterFloatArray(ViveSR_Framework.MODULE_ID_SEETHROUGH, (int)SeeThroughParam.UNDISTORTION_MAP_L, ref UndistortionMap_L);
            ViveSR_Framework.GetParameterFloatArray(ViveSR_Framework.MODULE_ID_SEETHROUGH, (int)SeeThroughParam.UNDISTORTION_MAP_R, ref UndistortionMap_R);

            float[] rawUndistortedCxCyArray = new float[8];
            ViveSR_Framework.GetParameterFloatArray(ViveSR_Framework.MODULE_ID_SEETHROUGH, (int)SeeThroughParam.UNDISTORTION_CENTER, ref rawUndistortedCxCyArray);
            double[] undistortedCxCyArray = new double[4];
            Buffer.BlockCopy(rawUndistortedCxCyArray, 0, undistortedCxCyArray, 0, rawUndistortedCxCyArray.Length * sizeof(float));
            UndistortedCx_L = undistortedCxCyArray[0];
            UndistortedCy_L = undistortedCxCyArray[1];
            UndistortedCx_R = undistortedCxCyArray[2];
            UndistortedCy_R = undistortedCxCyArray[3];

            ViveSR_Framework.GetParameterFloatArray(ViveSR_Framework.MODULE_ID_SEETHROUGH, (int)SeeThroughParam.OFFSET_HEAD_TO_CAMERA, ref OffsetHeadToCamera);
        }

#region Register/Unregister
        public static int RegisterDistortedCallback()
        {
            int result = ViveSR_Framework.RegisterCallback(ViveSR_Framework.MODULE_ID_SEETHROUGH, (int)SeeThroughCallback.BASIC, Marshal.GetFunctionPointerForDelegate((ViveSR_Framework.CallbackBasic)DistortedDataCallback));
            return result;
        }
        public static int RegisterUndistortedCallback()
        {
            int result = ViveSR_Framework.RegisterCallback(ViveSR_Framework.MODULE_ID_SEETHROUGH, (int)SeeThroughCallback.BASIC, Marshal.GetFunctionPointerForDelegate((ViveSR_Framework.CallbackBasic)UndistortedDataCallback));
            return result;
        }
        public static int RegisterDepthCallback()
        {
            int result = ViveSR_Framework.RegisterCallback(ViveSR_Framework.MODULE_ID_DEPTH, (int)DepthCallback.BASIC, Marshal.GetFunctionPointerForDelegate((ViveSR_Framework.CallbackBasic)DepthDataCallback));
            return result;
        }

        public static int UnregisterDistortedCallback()
        {
            int result = ViveSR_Framework.UnregisterCallback(ViveSR_Framework.MODULE_ID_SEETHROUGH, (int)SeeThroughCallback.BASIC, Marshal.GetFunctionPointerForDelegate((ViveSR_Framework.CallbackBasic)DistortedDataCallback));
            return result;
        }
        public static int UnregisterUndistortedCallback()
        {
            int result = ViveSR_Framework.UnregisterCallback(ViveSR_Framework.MODULE_ID_SEETHROUGH, (int)SeeThroughCallback.BASIC, Marshal.GetFunctionPointerForDelegate((ViveSR_Framework.CallbackBasic)UndistortedDataCallback));
            return result;
        }
        public static int UnregisterDepthCallback()
        {
            int result = ViveSR_Framework.UnregisterCallback(ViveSR_Framework.MODULE_ID_DEPTH, (int)DepthCallback.BASIC, Marshal.GetFunctionPointerForDelegate((ViveSR_Framework.CallbackBasic)DepthDataCallback));
            return result;
        }
#endregion

#region GetTexture2D
        /// <summary>
        /// Get the distorted texture, frame index, time index from current buffer.
        /// </summary>
        /// <param name="imageLeft"></param>
        /// <param name="imageRight"></param>
        /// <param name="frameIndex"></param>
        /// <param name="timeIndex"></param>
        public static void GetDistortedTexture(out Texture2D imageLeft, out Texture2D imageRight, out int frameIndex, out int timeIndex, out Matrix4x4 poseLeft, out Matrix4x4 poseRight)
        {
            // native buffer ptr method 2: get gpu texture buffer ptr directly from native(cpp)
#if USE_DISTORT_TEX_NATIVE_BUFFER
            if (TextureDistortedDeviceRaw == null)
            {
                TextureDistortedDeviceRaw = Texture2D.CreateExternalTexture(DistortedImageWidth, DistortedImageHeight * 2, TextureFormat.RGBA32, false, false, nativeTex);
            }
            else
            {
                ViveSR_Framework.GetParameterNativePtr(ViveSR_Framework.MODULE_ID_SEETHROUGH, (int)SeeThroughParam.VIDEO_RES_VIEW_NATIVE_PTR, ref nativeTex);
                TextureDistortedDeviceRaw.UpdateExternalTexture(nativeTex);
            }
            imageLeft = TextureDistortedDeviceRaw;
            imageRight = TextureDistortedDeviceRaw;
#else
            if (DataInfoDistorted[(int)SeeThroughDataMask.DISTORTED_FRAME_LEFT].ptr != IntPtr.Zero && DataInfoDistorted[(int)SeeThroughDataMask.DISTORTED_FRAME_RIGHT].ptr != IntPtr.Zero)
            {
                TextureDistortedLeft.LoadRawTextureData(DataInfoDistorted[(int)SeeThroughDataMask.DISTORTED_FRAME_LEFT].ptr, DistortedImageWidth * DistortedImageHeight * DistortedImageChannel);
                TextureDistortedRight.LoadRawTextureData(DataInfoDistorted[(int)SeeThroughDataMask.DISTORTED_FRAME_RIGHT].ptr, DistortedImageWidth * DistortedImageHeight * DistortedImageChannel);
                TextureDistortedLeft.Apply();
                TextureDistortedRight.Apply();
            }
            imageLeft = TextureDistortedLeft;
            imageRight = TextureDistortedRight;
#endif
            frameIndex = DistortedFrameIndex;
            timeIndex = DistortedTimeIndex;
            poseLeft = DistortedPoseLeft;
            poseRight = DistortedPoseRight;
        }

        /// <summary>
        /// Get the undistorted texture, frame index, time index from current buffer.
        /// </summary>
        /// <param name="imageLeft"></param>
        /// <param name="imageRight"></param>
        /// <param name="frameIndex"></param>
        /// <param name="timeIndex"></param>
        public static void GetUndistortedTexture(out Texture2D imageLeft, out Texture2D imageRight, out int frameIndex, out int timeIndex, out Matrix4x4 poseLeft, out Matrix4x4 poseRight)
        {
#if !USE_UNDISTORT_TEX_NATIVE_BUFFER
            if (DataInfoUndistorted[(int)SeeThroughDataMask.UNDISTORTED_FRAME_LEFT].ptr != IntPtr.Zero && DataInfoUndistorted[(int)SeeThroughDataMask.UNDISTORTED_FRAME_RIGHT].ptr != IntPtr.Zero)
            {
                TextureUndistortedLeft.LoadRawTextureData(DataInfoUndistorted[(int)SeeThroughDataMask.UNDISTORTED_FRAME_LEFT].ptr, UndistortedImageWidth * UndistortedImageHeight * UndistortedImageChannel);
                TextureUndistortedRight.LoadRawTextureData(DataInfoUndistorted[(int)SeeThroughDataMask.UNDISTORTED_FRAME_RIGHT].ptr, UndistortedImageWidth * UndistortedImageHeight * UndistortedImageChannel);
                TextureUndistortedLeft.Apply();
                TextureUndistortedRight.Apply();
            }            
#endif
            imageLeft = TextureUndistortedLeft;
            imageRight = TextureUndistortedRight;
            frameIndex = UndistortedFrameIndex;
            timeIndex = UndistortedTimeIndex;
            poseLeft = UndistortedPoseLeft;
            poseRight = UndistortedPoseRight;
        }

        /// <summary>
        /// Get the depth texture, frame index, time index from current buffer.
        /// </summary>
        /// <param name="imageDepth"></param>
        /// <param name="frameIndex"></param>
        /// <param name="timeIndex"></param>
        public static void GetDepthTexture(out Texture2D imageDepth, out int frameIndex, out int timeIndex, out Matrix4x4 pose)
        {
            if (DataInfoDepth[(int)DepthDataMask.DEPTH_MAP].ptr != IntPtr.Zero)
            {
                TextureDepth.LoadRawTextureData(DataInfoDepth[(int)DepthDataMask.DEPTH_MAP].ptr, DepthImageWidth * DepthImageHeight * DepthImageChannel * DepthDataSize);
                TextureDepth.Apply();
            }
            imageDepth = TextureDepth;
            frameIndex = DepthFrameIndex;
            timeIndex = DepthTimeIndex;
            pose = DepthPose;
        }
#endregion

#region Active
        /// <summary>
        /// Update the buffer of distorted texture, frame index and time index.
        /// </summary>
        public static void UpdateDistortedImage()
        {
            int result = (int)Error.FAILED;
            if (!InitialDistortedPtrSize)
            {
                result = ViveSR_Framework.GetMultiDataSize(ViveSR_Framework.MODULE_ID_SEETHROUGH, DataInfoDistorted, DataInfoDistorted.Length);
                InitialDistortedPtrSize = (result == (int)Error.WORK);
            }

            DataInfo[] dataInfoFrame = new DataInfo[] { DataInfoDistorted[(int)SeeThroughDataMask.FRAME_SEQ] };
            result = ViveSR_Framework.GetMultiData(ViveSR_Framework.MODULE_ID_SEETHROUGH, dataInfoFrame, dataInfoFrame.Length);
            if (result != (int)Error.WORK) return;

            Marshal.Copy(DataInfoDistorted[(int)SeeThroughDataMask.FRAME_SEQ].ptr, RawDistortedFrameIndex, 0, RawDistortedFrameIndex.Length);
            if (LastDistortedFrameIndex == DistortedFrameIndex) return;
            else LastDistortedFrameIndex = DistortedFrameIndex;

            result = ViveSR_Framework.GetMultiData(ViveSR_Framework.MODULE_ID_SEETHROUGH, DataInfoDistorted, DataInfoDistorted.Length);
            if (result == (int)Error.WORK) ParseDistortedPtrData();
        }

        /// <summary>
        /// Update the buffer of undistorted texture, frame index and time index.
        /// </summary>
        public static void UpdateUndistortedImage()
        {
            int result = (int)Error.FAILED;
            if (!InitialUndistortedPtrSize)
            {
                result = ViveSR_Framework.GetMultiDataSize(ViveSR_Framework.MODULE_ID_SEETHROUGH, DataInfoUndistorted, DataInfoUndistorted.Length);
                InitialUndistortedPtrSize = (result == (int)Error.WORK);
            }

            DataInfo[] dataInfoFrame = new DataInfo[] { DataInfoUndistorted[(int)SeeThroughDataMask.FRAME_SEQ] };
            result = ViveSR_Framework.GetMultiData(ViveSR_Framework.MODULE_ID_SEETHROUGH, dataInfoFrame, dataInfoFrame.Length);
            if (result != (int)Error.WORK) return;

            Marshal.Copy(DataInfoUndistorted[(int)SeeThroughDataMask.FRAME_SEQ].ptr, RawUndistortedFrameIndex, 0, RawUndistortedFrameIndex.Length);
            if (LastUndistortedFrameIndex == UndistortedFrameIndex) return;
            else LastUndistortedFrameIndex = UndistortedFrameIndex;

            result = ViveSR_Framework.GetMultiData(ViveSR_Framework.MODULE_ID_SEETHROUGH, DataInfoUndistorted, DataInfoUndistorted.Length);
            if (result == (int)Error.WORK) ParseUndistortedPtrData();
        }

        /// <summary>
        /// Update the buffer of depth texture, frame index and time index.
        /// </summary>
        public static void UpdateDepthImage()
        {
            int result = (int)Error.FAILED;
            if (!InitialDepthPtrSize)
            {
                result = ViveSR_Framework.GetMultiDataSize(ViveSR_Framework.MODULE_ID_DEPTH, DataInfoDepth, DataInfoDepth.Length);
                InitialDepthPtrSize = (result == (int)Error.WORK);
            }

            DataInfo[] dataInfoFrame = new DataInfo[] { DataInfoDepth[(int)DepthDataMask.FRAME_SEQ] };
            result = ViveSR_Framework.GetMultiData(ViveSR_Framework.MODULE_ID_DEPTH, dataInfoFrame, dataInfoFrame.Length);
            if (result != (int)Error.WORK) return;

            Marshal.Copy(DataInfoDepth[(int)DepthDataMask.FRAME_SEQ].ptr, RawDepthFrameIndex, 0, RawDepthFrameIndex.Length);
            if (LastDepthFrameIndex == DepthFrameIndex) return;
            else LastDepthFrameIndex = DepthFrameIndex;

            result = ViveSR_Framework.GetMultiData(ViveSR_Framework.MODULE_ID_DEPTH, DataInfoDepth, DataInfoDepth.Length);
            if (result == (int)Error.WORK) ParseDepthPtrData();
        }
#endregion

#region Callback
        private static void DistortedDataCallback(int key)
        {
            for (int i = 0; i < DataInfoDistorted.Length; i++) ViveSR_Framework.GetPointer(key, i, ref DataInfoDistorted[i].ptr);
            ParseDistortedPtrData();
        }
        private static void UndistortedDataCallback(int key)
        {
            for (int i = 0; i < DataInfoUndistorted.Length; i++) ViveSR_Framework.GetPointer(key, i, ref DataInfoUndistorted[i].ptr);
            ParseUndistortedPtrData();
        }
        private static void DepthDataCallback(int key)
        {
            for (int i = 0; i < DataInfoDepth.Length; i++) ViveSR_Framework.GetPointer(key, i, ref DataInfoDepth[i].ptr);
            ParseDepthPtrData();
        }
#endregion

#region Parse the pointer
        private static void ParseDistortedPtrData()
        {
            Marshal.Copy(DataInfoDistorted[(int)SeeThroughDataMask.FRAME_SEQ].ptr, RawDistortedFrameIndex, 0, RawDistortedFrameIndex.Length);
            Marshal.Copy(DataInfoDistorted[(int)SeeThroughDataMask.TIME_STP].ptr, RawDistortedTimeIndex, 0, RawDistortedTimeIndex.Length);
            Marshal.Copy(DataInfoDistorted[(int)SeeThroughDataMask.POSE_LEFT].ptr, RawDistortedPoseLeft, 0, RawDistortedPoseLeft.Length);
            Marshal.Copy(DataInfoDistorted[(int)SeeThroughDataMask.POSE_RIGHT].ptr, RawDistortedPoseRight, 0, RawDistortedPoseRight.Length);

            for (int i = 0; i < 4; i++)
            {
                DistortedPoseLeft.SetColumn(i, new Vector4(RawDistortedPoseLeft[i * 4 + 0], RawDistortedPoseLeft[i * 4 + 1],
                                                           RawDistortedPoseLeft[i * 4 + 2], RawDistortedPoseLeft[i * 4 + 3]));
                DistortedPoseRight.SetColumn(i, new Vector4(RawDistortedPoseRight[i * 4 + 0], RawDistortedPoseRight[i * 4 + 1],
                                                            RawDistortedPoseRight[i * 4 + 2], RawDistortedPoseRight[i * 4 + 3]));
            }
        }
        private static void ParseUndistortedPtrData()
        {
            Marshal.Copy(DataInfoUndistorted[(int)SeeThroughDataMask.FRAME_SEQ].ptr, RawUndistortedFrameIndex, 0, RawUndistortedFrameIndex.Length);
            Marshal.Copy(DataInfoUndistorted[(int)SeeThroughDataMask.TIME_STP].ptr, RawUndistortedTimeIndex, 0, RawUndistortedTimeIndex.Length);
            Marshal.Copy(DataInfoUndistorted[(int)SeeThroughDataMask.POSE_LEFT].ptr, RawUndistortedPoseLeft, 0, RawUndistortedPoseLeft.Length);
            Marshal.Copy(DataInfoUndistorted[(int)SeeThroughDataMask.POSE_RIGHT].ptr, RawUndistortedPoseRight, 0, RawUndistortedPoseRight.Length);

            for (int i = 0; i < 4; i++)
            {
                UndistortedPoseLeft.SetColumn(i, new Vector4(RawUndistortedPoseLeft[i * 4 + 0], RawUndistortedPoseLeft[i * 4 + 1],
                                                             RawUndistortedPoseLeft[i * 4 + 2], RawUndistortedPoseLeft[i * 4 + 3]));
                UndistortedPoseRight.SetColumn(i, new Vector4(RawUndistortedPoseRight[i * 4 + 0], RawUndistortedPoseRight[i * 4 + 1],
                                                              RawUndistortedPoseRight[i * 4 + 2], RawUndistortedPoseRight[i * 4 + 3]));
            }
        }
        private static void ParseDepthPtrData()
        {
            Marshal.Copy(DataInfoDepth[(int)DepthDataMask.FRAME_SEQ].ptr, RawDepthFrameIndex, 0, RawDepthFrameIndex.Length);
            Marshal.Copy(DataInfoDepth[(int)DepthDataMask.TIME_STP].ptr, RawDepthTimeIndex, 0, RawDepthTimeIndex.Length);
            Marshal.Copy(DataInfoDepth[(int)DepthDataMask.POSE].ptr, RawDepthPose, 0, RawDepthPose.Length);

            for (int i = 0; i < 4; i++)
            {
                DepthPose.SetColumn(i, new Vector4(RawDepthPose[i * 4 + 0], RawDepthPose[i * 4 + 1],
                                                   RawDepthPose[i * 4 + 2], RawDepthPose[i * 4 + 3]));
            }
        }
        #endregion

#region Utility
        public static Quaternion Rotation(Matrix4x4 m)
        {
            return Quaternion.LookRotation(m.GetColumn(2), m.GetColumn(1));
        }
        public static Vector3 Position(Matrix4x4 m)
        {
            return new Vector3(m.m03, m.m13, m.m23);
        }
#endregion

#region Depth LinkMethod / Commands / Parameters

        public  static bool DepthProcessing { get; private set; }
        public  static int  EnableDepthProcess(bool active)
        {
            int result = ViveSR_Framework.ChangeModuleLinkStatus(ViveSR_Framework.MODULE_ID_SEETHROUGH, ViveSR_Framework.MODULE_ID_DEPTH, active ? (int)WorkLinkMethod.ACTIVE : (int)WorkLinkMethod.NONE);
            if (result == (int)Error.WORK) DepthProcessing = active;
            return result;
        }
        private static bool _DepthRefinement = false;
        public  static bool DepthRefinement 
        { 
            get { return _DepthRefinement; }
            set { if (_DepthRefinement != value) EnableDepthRefinement(value); } 
        }
        public  static int EnableDepthRefinement(bool active)
        {
            int result = ViveSR_Framework.SetCommandBool(ViveSR_Framework.MODULE_ID_DEPTH, (int)DepthCmd.ENABLE_REFINEMENT, active);
            if (result == (int)Error.WORK) _DepthRefinement = active;
            return result;
        }
        private static bool _DepthEdgeEnhance = false;
        public static bool DepthEdgeEnhance 
        { 
            get { return _DepthEdgeEnhance; }
            set { if (_DepthEdgeEnhance != value) EnableDepthEdgeEnhance(value); } 
        }
        public  static int EnableDepthEdgeEnhance(bool active)
        {
            int result = ViveSR_Framework.SetCommandBool(ViveSR_Framework.MODULE_ID_DEPTH, (int)DepthCmd.ENABLE_EDGE_ENHANCE, active);
            if (result == (int)Error.WORK) _DepthEdgeEnhance = active;
            return result;
        }

        public static DepthCase DepthCase { get; private set; }
        public static int SetDefaultDepthCase(DepthCase depthCase)
        {
            int result = ViveSR_Framework.SetParameterInt(ViveSR_Framework.MODULE_ID_DEPTH, (int)DepthParam.DEPTH_USING_CASE, (int)depthCase);
            if (result == (int)Error.WORK) DepthCase = depthCase;
            return result;
        }
        public static int ChangeDepthCase(DepthCase depthCase)
        {
            int result = ViveSR_Framework.SetCommandInt(ViveSR_Framework.MODULE_ID_DEPTH, (int)DepthCmd.CHANGE_DEPTH_CASE, (int)depthCase);
            if (result == (int)Error.WORK) DepthCase = depthCase;
            return result;
        }

        private static float _DepthConfidenceThreshold;
        public  static float  DepthConfidenceThreshold
        {
            get
            {
                ViveSR_Framework.GetParameterFloat(ViveSR_Framework.MODULE_ID_DEPTH, (int)DepthParam.CONFIDENCE_THRESHOLD, ref _DepthConfidenceThreshold);
                return _DepthConfidenceThreshold;
            }
            set
            {
                int result = ViveSR_Framework.SetParameterFloat(ViveSR_Framework.MODULE_ID_DEPTH, (int)DepthParam.CONFIDENCE_THRESHOLD, value);
                if (result == (int)Error.WORK) _DepthConfidenceThreshold = value;
            }
        }
        private static int _DepthDenoiseGuidedFilter;
        public  static int  DepthDenoiseGuidedFilter
        {
            get
            {
                ViveSR_Framework.GetParameterInt(ViveSR_Framework.MODULE_ID_DEPTH, (int)DepthParam.DENOISE_GUIDED_FILTER, ref _DepthDenoiseGuidedFilter);
                return _DepthDenoiseGuidedFilter;
            }
            set
            {
                int result = ViveSR_Framework.SetParameterInt(ViveSR_Framework.MODULE_ID_DEPTH, (int)DepthParam.DENOISE_GUIDED_FILTER, value);
                if (result == (int)Error.WORK) _DepthDenoiseGuidedFilter = value;
            }
        }
        private static int _DepthDenoiseMedianFilter;
        public  static int  DepthDenoiseMedianFilter
        {
            get
            {
                ViveSR_Framework.GetParameterInt(ViveSR_Framework.MODULE_ID_DEPTH, (int)DepthParam.DENOISE_MEDIAN_FILTER, ref _DepthDenoiseMedianFilter);
                return _DepthDenoiseMedianFilter;
            }
            set
            {
                int result = ViveSR_Framework.SetParameterInt(ViveSR_Framework.MODULE_ID_DEPTH, (int)DepthParam.DENOISE_MEDIAN_FILTER, value);
                if (result == (int)Error.WORK) _DepthDenoiseMedianFilter = value;
            }
        }
        #endregion

#region Camera Commands / Parameters
        [StructLayout(LayoutKind.Sequential)]
        public struct CameraQualityInfo
        {
            public Int32 Status;
            public Int32 DefaultValue;
            public Int32 Min;
            public Int32 Max;
            public Int32 Step;
            public Int32 DefaultMode;
            public Int32 Value;
            public Int32 Mode;  // AUTO = 1, MANUAL = 2
        };

        public enum CameraQuality
        {
            BRIGHTNESS              = 100,
            CONTRAST                = 101,
            HUE                     = 102,
            SATURATION              = 103,
            SHARPNESS               = 104,
            GAMMA                   = 105,
            //COLOR_ENABLE          = 106,
            WHITE_BALANCE           = 107,
            BACKLIGHT_COMPENSATION  = 108,
            GAIN                    = 109,
            //PAN                   = 110,
            //TILT                  = 111,
            //ROLL                  = 112,
            //ZOOM                  = 113,
            //EXPOSURE              = 114,
            //IRIS                  = 115,
            //FOCUS                 = 116,
        }

        public static int GetCameraQualityInfo(CameraQuality item, ref CameraQualityInfo paramInfo)
        {
            return ViveSR_Framework.GetParameterStruct(ViveSR_Framework.MODULE_ID_SEETHROUGH, (int)item, ref paramInfo);
        }
        public static int SetCameraQualityInfo(CameraQuality item, CameraQualityInfo paramInfo)
        {
            return ViveSR_Framework.SetParameterStruct(ViveSR_Framework.MODULE_ID_SEETHROUGH, (int)item, paramInfo);
        }
#endregion
    }
}