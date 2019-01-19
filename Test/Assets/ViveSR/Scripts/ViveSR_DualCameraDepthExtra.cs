using System;
using System.Runtime.InteropServices;
using Vive.Plugin.SR;

namespace Vive.Plugin.SR
{
    public class ViveSR_DualCameraDepthExtra
    {
        private static DataInfo[] DataInfoDepthCollider = null;
        
        private static bool InitialDepthColliderPtrSize = false;

        private static int[] RawDepthColliderTimeIndex = new int[1];
        private static int[] RawDepthColliderVerticesNum = new int[1];
        private static int[] RawDepthColliderBytePervert = new int[1];
        private static int[] RawDepthColliderIndicesNum = new int[1];
        private static float[] RawDepthColliderVertices;
        private static int[] RawDepthColliderIndices;

        private static int LastDepthColliderTimeIndex = -1;
        public static int DepthColliderTimeIndex { get { return RawDepthColliderTimeIndex[0]; } }
        public static int ColliderVerticeNum { get { return RawDepthColliderVerticesNum[0]; } }
        public static int ColliderBytePervert { get { return RawDepthColliderBytePervert[0]; } }
        public static int ColliderIndicesNum { get { return RawDepthColliderIndicesNum[0]; } }


        public static void InitialDepthCollider(int depthImageWidth, int depthImageHeight)
        {
            InitialDepthColliderPtrSize = false;

            RawDepthColliderVertices = new float[depthImageWidth * depthImageHeight * 3];
            RawDepthColliderIndices = new int[depthImageWidth * depthImageHeight * 6];

            // To use enum DepthDataMask, It needs to assign each type.
            DataInfoDepthCollider = ViveSR_Framework.CreateDataInfo(new IntPtr[] {
            IntPtr.Zero,                                            //
            IntPtr.Zero,                                            //
            IntPtr.Zero,                                            //
            Marshal.AllocCoTaskMem(sizeof(uint)),                   // TIME_STP
            IntPtr.Zero,                                            //
            Marshal.AllocCoTaskMem(sizeof(uint)),                   // NUM_VERTICES
            Marshal.AllocCoTaskMem(sizeof(uint)),                   // BYTEPERVERT
            Marshal.AllocCoTaskMem(sizeof(float) * 640 * 480 * 3),  // VERTICES
            Marshal.AllocCoTaskMem(sizeof(uint)),                   // NUM_INDICES
            Marshal.AllocCoTaskMem(sizeof(int) * 640 * 480 * 6), });// INDICES
        }
        public static void ReleaseDepthCollider()
        {
            RawDepthColliderVertices = null;
            RawDepthColliderIndices = null;
            if (DataInfoDepthCollider != null)
            {
                foreach (DataInfo data in DataInfoDepthCollider)
                {
                    if (data.ptr != IntPtr.Zero)
                    {
                        Marshal.FreeCoTaskMem(data.ptr);
                    }
                }

                DataInfoDepthCollider = null;
            }
        }
        public static bool GetDepthColliderFrameInfo()
        {
            int result = (int)Error.FAILED;
            if (!InitialDepthColliderPtrSize)
            {
                result = ViveSR_Framework.GetMultiDataSize(ViveSR_Framework.MODULE_ID_DEPTH, DataInfoDepthCollider, DataInfoDepthCollider.Length);
                InitialDepthColliderPtrSize = (result == (int)Error.WORK);
            }

            DataInfo[] dataInfoFrame = new DataInfo[] { DataInfoDepthCollider[(int)DepthDataMask.TIME_STP] };
            result = ViveSR_Framework.GetMultiData(ViveSR_Framework.MODULE_ID_DEPTH, dataInfoFrame, dataInfoFrame.Length);
            if (result != (int)Error.WORK) return false;

            Marshal.Copy(DataInfoDepthCollider[(int)DepthDataMask.TIME_STP].ptr, RawDepthColliderTimeIndex, 0, RawDepthColliderTimeIndex.Length);
            if (LastDepthColliderTimeIndex == DepthColliderTimeIndex) return false;
            else LastDepthColliderTimeIndex = DepthColliderTimeIndex;
            return true;
        }

        public static bool GetDepthColliderData(ref int verticesNum, out float[] verticesBuff, ref int indicesNum, out int[] indicesBuff)
        {
            int result = ViveSR_Framework.GetMultiData(ViveSR_Framework.MODULE_ID_DEPTH, DataInfoDepthCollider, DataInfoDepthCollider.Length);
            if (result == (int)Error.WORK)
            {
                Marshal.Copy(DataInfoDepthCollider[(int)DepthDataMask.NUM_VERTICES].ptr, RawDepthColliderVerticesNum, 0, RawDepthColliderVerticesNum.Length);
                Marshal.Copy(DataInfoDepthCollider[(int)DepthDataMask.BYTEPERVERT].ptr, RawDepthColliderBytePervert, 0, RawDepthColliderBytePervert.Length);
                Marshal.Copy(DataInfoDepthCollider[(int)DepthDataMask.VERTICES].ptr, RawDepthColliderVertices, 0, ColliderVerticeNum * ColliderBytePervert / 3);
                Marshal.Copy(DataInfoDepthCollider[(int)DepthDataMask.NUM_INDICES].ptr, RawDepthColliderIndicesNum, 0, RawDepthColliderIndicesNum.Length);
                Marshal.Copy(DataInfoDepthCollider[(int)DepthDataMask.INDICES].ptr, RawDepthColliderIndices, 0, ColliderIndicesNum);
            }
            else
            {
                RawDepthColliderVerticesNum[0] = 0;
                RawDepthColliderBytePervert[0] = 0;
                RawDepthColliderIndicesNum[0] = 0;
            }
            verticesNum = ColliderVerticeNum;
            indicesNum = ColliderIndicesNum;
            verticesBuff = RawDepthColliderVertices;
            indicesBuff = RawDepthColliderIndices;
            return true;
        }
    }
}