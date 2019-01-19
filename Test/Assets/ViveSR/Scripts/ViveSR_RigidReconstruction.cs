using UnityEngine;
using System.Runtime.InteropServices;
using System;

namespace Vive.Plugin.SR
{
    //public static class ViveSR_RigidReconstructionConfig
    //{
    //    public static ReconstructionDataSource ReconstructionDataSource = ReconstructionDataSource.DATASET;
    //    public static uint NumDatasetFrame = 0;
    //    public static string DatasetPath = "";
    //    public static ReconstructionQuality Quality = ReconstructionQuality.MID;
    //    public static bool ExportCollider = false;
    //}
    public class ViveSR_RigidReconstruction
    {
        [Obsolete("use ViveSR_Framework.CallbackBasic instead.")]
        public delegate void Callback(int numFrame, IntPtr poseMtx, IntPtr vertData, int numVert, int vertStide, IntPtr idxData, int numIdx);
        public delegate void ExportProgressCallback(int stage, int percentage);

        private static DataInfo[] DataInfoPointCloud = null;
        private static bool InitialPointCloudPtrSize = false;

        private static int[] RawPointCloudFrameIndex = new int[1];
        private static int[] RawPointCloudVerticeNum = new int[1];
        private static int[] RawPointCloudIndicesNum = new int[1];
        private static int[] RawPointCloudBytePerVetex = new int[1];
        private static int[] RawPointCloudSectorNum = new int[1];
        
        private static float[] OutVertex;
        private static int[] OutIndex;
        private static float[] TrackedPose;
        private static int ExportStage;
        private static int ExportPercentage;
        private static int FrameSeq { get { return RawPointCloudFrameIndex[0]; } }
        private static int VertNum { get { return RawPointCloudVerticeNum[0]; } }
        private static int IdxNum { get { return RawPointCloudIndicesNum[0]; } }
        private static int VertStrideInByte { get { return RawPointCloudBytePerVetex[0]; } }
        private static int SectorNum { get { return RawPointCloudSectorNum[0]; } }
        private static int[] SectorIDList;
        private static int[] SectorVertNum;
        private static int[] SectorMeshIdNum;
        private static bool UsingCallback = false;

        public static bool ExportAdaptiveMesh { get; set; }
        public static float ExportAdaptiveMaxGridSize { get; set; }
        public static float ExportAdaptiveMinGridSize { get; set; }
        public static float ExportAdaptiveErrorThres { get; set; }
        public static float LiveAdaptiveMaxGridSize { get; set; }
        public static float LiveAdaptiveMinGridSize { get; set; }
        public static float LiveAdaptiveErrorThres { get; set; }
        public static bool IsScanning { get; private set; }
        public static bool IsExportingMesh { get; private set; }

        public static bool InitRigidReconstructionParamFromFile(string configFile)
        {
            return ViveSR_Framework.SetParameterString(ViveSR_Framework.MODULE_ID_RIGID_RECONSTRUCTION, (int)ReconstructionParam.CONFIG_FILEPATH, configFile) == (int)Error.WORK;
        }

        //public static void InitRigidReconstructionParam()
        //{
        //    this function is not called in current version, keep this API on, we can allow user to adjust some default setting
        //    ViveSR_Framework.SetParameterInt(ViveSR_Framework.MODULE_ID_RIGID_RECONSTRUCTION, (int)ReconstructionParam.CONFIG_DATA_SOURCE, (int)ViveSR_RigidReconstructionConfig.ReconstructionDataSource);
        //    ViveSR_Framework.SetParameterInt(ViveSR_Framework.MODULE_ID_RIGID_RECONSTRUCTION, (int)ReconstructionParam.CONFIG_DATASET_FRAME_NUM, (int)ViveSR_RigidReconstructionConfig.NumDatasetFrame);
        //    ViveSR_Framework.SetParameterString(ViveSR_Framework.MODULE_ID_RIGID_RECONSTRUCTION, (int)ReconstructionParam.CONFIG_DATASET_PATH, ViveSR_RigidReconstructionConfig.DatasetPath);
        //    ViveSR_Framework.SetParameterBool(ViveSR_Framework.MODULE_ID_RIGID_RECONSTRUCTION, (int)ReconstructionParam.CONFIG_EXPORT_COLLIDER, ViveSR_RigidReconstructionConfig.ExportCollider);
        //    ViveSR_Framework.SetParameterBool(ViveSR_Framework.MODULE_ID_RIGID_RECONSTRUCTION, (int)ReconstructionParam.CONFIG_EXPORT_TEXTURE, true);
        //    ViveSR_Framework.SetParameterInt(ViveSR_Framework.MODULE_ID_RIGID_RECONSTRUCTION, (int)ReconstructionParam.CONFIG_QUALITY, (int)ViveSR_RigidReconstructionConfig.Quality);
        //}

        public static int GetRigidReconstructionIntParameter(int type)
        {
            int ret = -1;

            if (ViveSR_Framework.GetParameterInt(ViveSR_Framework.MODULE_ID_RIGID_RECONSTRUCTION, type, ref ret) != (int)Error.WORK)
                Debug.Log("[ViveSR] [RigidReconstruction] GetRigidReconstructionIntParameter Failed");

            return ret;
        }

        public static void AllocOutputDataMemory()
        {
            InitialPointCloudPtrSize = false;
            OutVertex = new float[8 * 2500000];
            OutIndex = new int[2500000];
            TrackedPose = new float[16];
            //ExternalPose = new float[16];
            SectorIDList = new int[1000000];
            SectorVertNum = new int[1000000];
            SectorMeshIdNum = new int[1000000];
            Debug.Log("[ViveSR] [RigidReconstruction] AllocOutputMemory Done");

            ExportAdaptiveMesh = true;
            LiveAdaptiveMaxGridSize = ExportAdaptiveMaxGridSize = 64;
            LiveAdaptiveMinGridSize = ExportAdaptiveMinGridSize = 4;
            LiveAdaptiveErrorThres  = ExportAdaptiveErrorThres = 0.4f;
            
            // To use enum ReconstructionDataMask, It needs to assign each type.
            DataInfoPointCloud = ViveSR_Framework.CreateDataInfo(new IntPtr[] {
                Marshal.AllocCoTaskMem(sizeof(uint)),               // FRAME_SEQ
                Marshal.AllocCoTaskMem(sizeof(float) * 16),         // POSEMTX44
                Marshal.AllocCoTaskMem(sizeof(int)),                // NUM_VERTICES
                Marshal.AllocCoTaskMem(sizeof(int)),                // BYTEPERVERT
                Marshal.AllocCoTaskMem(sizeof(float) * 8 * 2500000),// VERTICES
                Marshal.AllocCoTaskMem(sizeof(int)),                // NUM_INDICES
                Marshal.AllocCoTaskMem(sizeof(int) * 2500000),      // INDICES
                IntPtr.Zero,                                        // CLDTYPE
                IntPtr.Zero,                                        // COLLIDERNUM
                IntPtr.Zero,                                        // CLD_NUM_VERTS
                IntPtr.Zero,                                        // CLD_NUMIDX
                IntPtr.Zero,                                        // CLD_VERTICES
                IntPtr.Zero,                                        // CLD_INDICES
                Marshal.AllocCoTaskMem(sizeof(int)),                // SECTOR_NUM
                Marshal.AllocCoTaskMem(sizeof(int) * 1000000),      // SECTOR_ID_LIST
                Marshal.AllocCoTaskMem(sizeof(int) * 1000000),      // SECTOR_VERT_NUM
                Marshal.AllocCoTaskMem(sizeof(int) * 1000000) });   // SECTOR_IDX_NUM
        }

        public static void ReleaseAllocOutputDataMemory()
        {
            OutVertex = null;
            OutIndex = null;
            TrackedPose = null;
            SectorIDList = null;
            SectorVertNum = null;
            SectorMeshIdNum = null;
            if (DataInfoPointCloud != null)
            {
                foreach (DataInfo data in DataInfoPointCloud)
                {
                    if (data.ptr != IntPtr.Zero)
                    {
                        Marshal.FreeCoTaskMem(data.ptr);
                    }
                }
                DataInfoPointCloud = null;
            }
        }

        public static bool GetRigidReconstructionFrame(ref int frame)
        {
            int result = (int)Error.FAILED;
            if (!UsingCallback)
            {
                if (!InitialPointCloudPtrSize)
                {
                    result = ViveSR_Framework.GetMultiDataSize(ViveSR_Framework.MODULE_ID_RIGID_RECONSTRUCTION, DataInfoPointCloud, DataInfoPointCloud.Length);
                    InitialPointCloudPtrSize = (result == (int)Error.WORK);
                }
                DataInfo[] dataInfoFrame = new DataInfo[] { DataInfoPointCloud[(int)ReconstructionDataMask.FRAME_SEQ] };
                result = ViveSR_Framework.GetMultiData(ViveSR_Framework.MODULE_ID_RIGID_RECONSTRUCTION, dataInfoFrame, dataInfoFrame.Length);
                if (result != (int)Error.WORK) return false;
                Marshal.Copy(DataInfoPointCloud[(int)ReconstructionDataMask.FRAME_SEQ].ptr, RawPointCloudFrameIndex, 0, RawPointCloudFrameIndex.Length);
            }
            frame = RawPointCloudFrameIndex[0];
            return true;
        }

        public static bool GetRigidReconstructionData(ref int frame, 
                                                      out float[] pose, 
                                                      ref int verticesNum, 
                                                      out float[] verticesBuff, 
                                                      ref int vertStrideInFloat, 
                                                      out int[] sectorIDList, 
                                                      ref int sectorNum, 
                                                      out int[] sectorVertNum, 
                                                      out int[] sectorMeshIdNum,
                                                      ref int indicesNum, 
                                                      out int[] indicesBuff)
        {
            if (!UsingCallback)
            {
                int result = (int)Error.FAILED;
                if (!InitialPointCloudPtrSize)
                {
                    result = ViveSR_Framework.GetMultiDataSize(ViveSR_Framework.MODULE_ID_RIGID_RECONSTRUCTION, DataInfoPointCloud, DataInfoPointCloud.Length);
                    InitialPointCloudPtrSize = (result == (int)Error.WORK);
                }
                result = ViveSR_Framework.GetMultiData(ViveSR_Framework.MODULE_ID_RIGID_RECONSTRUCTION, DataInfoPointCloud, DataInfoPointCloud.Length);
                if (result == (int)Error.WORK)
                {
                    ParseReconstructionPtrData();
                }
            }

            bool isUpdated = (verticesNum != VertNum);

            verticesNum = VertNum;
            indicesNum = IdxNum;
            frame = FrameSeq;
            vertStrideInFloat = VertStrideInByte / 4;
            verticesBuff = OutVertex;
            indicesBuff = OutIndex;
            pose = TrackedPose;
            sectorIDList = SectorIDList;
            sectorNum = SectorNum;
            sectorVertNum = SectorVertNum;
            sectorMeshIdNum = SectorMeshIdNum;

            return isUpdated;
        }

        public static int RegisterReconstructionCallback()
        {
            UsingCallback = true;
            return ViveSR_Framework.RegisterCallback(ViveSR_Framework.MODULE_ID_RIGID_RECONSTRUCTION, (int)ReconstructionCallback.BASIC, Marshal.GetFunctionPointerForDelegate((ViveSR_Framework.CallbackBasic)ReconstructionDataCallback));
        }

        public static int UnregisterReconstructionCallback()
        {
            UsingCallback = false;
            return ViveSR_Framework.UnregisterCallback(ViveSR_Framework.MODULE_ID_RIGID_RECONSTRUCTION, (int)ReconstructionCallback.BASIC, Marshal.GetFunctionPointerForDelegate((ViveSR_Framework.CallbackBasic)ReconstructionDataCallback));
        }

        private static void ReconstructionDataCallback(int key)
        {
            for (int i = 0; i < DataInfoPointCloud.Length; i++) ViveSR_Framework.GetPointer(key, i, ref DataInfoPointCloud[i].ptr);
            ParseReconstructionPtrData();
        }

        private static void ParseReconstructionPtrData()
        {
            Marshal.Copy(DataInfoPointCloud[(int)ReconstructionDataMask.FRAME_SEQ].ptr, RawPointCloudFrameIndex, 0, RawPointCloudFrameIndex.Length);
            Marshal.Copy(DataInfoPointCloud[(int)ReconstructionDataMask.POSEMTX44].ptr, TrackedPose, 0, TrackedPose.Length);
            Marshal.Copy(DataInfoPointCloud[(int)ReconstructionDataMask.NUM_VERTICES].ptr, RawPointCloudVerticeNum, 0, RawPointCloudVerticeNum.Length);
            Marshal.Copy(DataInfoPointCloud[(int)ReconstructionDataMask.BYTEPERVERT].ptr, RawPointCloudBytePerVetex, 0, RawPointCloudBytePerVetex.Length);
            Marshal.Copy(DataInfoPointCloud[(int)ReconstructionDataMask.VERTICES].ptr, OutVertex, 0, (VertNum * VertStrideInByte / sizeof(float)));
            Marshal.Copy(DataInfoPointCloud[(int)ReconstructionDataMask.NUM_INDICES].ptr, RawPointCloudIndicesNum, 0, RawPointCloudIndicesNum.Length);
            Marshal.Copy(DataInfoPointCloud[(int)ReconstructionDataMask.INDICES].ptr, OutIndex, 0, IdxNum /**sizeof(int)*/);

            Marshal.Copy(DataInfoPointCloud[(int)ReconstructionDataMask.SECTOR_NUM].ptr, RawPointCloudSectorNum, 0, RawPointCloudSectorNum.Length);
            Marshal.Copy(DataInfoPointCloud[(int)ReconstructionDataMask.SECTOR_ID_LIST].ptr, SectorIDList, 0, SectorNum);
            Marshal.Copy(DataInfoPointCloud[(int)ReconstructionDataMask.SECTOR_VERT_NUM].ptr, SectorVertNum, 0, SectorNum);
            Marshal.Copy(DataInfoPointCloud[(int)ReconstructionDataMask.SECTOR_IDX_NUM].ptr, SectorMeshIdNum, 0, SectorNum);
        }

        public static void ExportModel(string filename)
        {
            ExportStage = 0;
            ExportPercentage = 0;
            IsExportingMesh = true;

            ViveSR_Framework.SetParameterBool(ViveSR_Framework.MODULE_ID_RIGID_RECONSTRUCTION, (int)ReconstructionParam.EXPORT_ADAPTIVE_MODEL, ExportAdaptiveMesh);
            if (ExportAdaptiveMesh)
            {
                ViveSR_Framework.SetParameterFloat(ViveSR_Framework.MODULE_ID_RIGID_RECONSTRUCTION, (int)ReconstructionParam.ADAPTIVE_MAX_GRID, ExportAdaptiveMaxGridSize * 0.01f);   // cm to m
                ViveSR_Framework.SetParameterFloat(ViveSR_Framework.MODULE_ID_RIGID_RECONSTRUCTION, (int)ReconstructionParam.ADAPTIVE_MIN_GRID, ExportAdaptiveMinGridSize * 0.01f);
                ViveSR_Framework.SetParameterFloat(ViveSR_Framework.MODULE_ID_RIGID_RECONSTRUCTION, (int)ReconstructionParam.ADAPTIVE_ERROR_THRES, ExportAdaptiveErrorThres);
            }           
            ViveSR_Framework.RegisterCallback(ViveSR_Framework.MODULE_ID_RIGID_RECONSTRUCTION, (int)ReconstructionCallback.EXPORT_PROGRESS, Marshal.GetFunctionPointerForDelegate((ExportProgressCallback)UpdateExportProgress));
            ViveSR_Framework.SetCommandString(ViveSR_Framework.MODULE_ID_RIGID_RECONSTRUCTION, (int)(ReconstructionCmd.EXPORT_MODEL_FOR_UNITY), filename);
        }

        private static void UpdateExportProgress(int stage, int percentage)
        {
            // Fixed: The export stage should be saving mesh model first then extracting collider;
            if      (stage == (int)ReconstructionExportStage.STAGE_EXTRACTING_MODEL)    ExportStage = 0;
            else if (stage == (int)ReconstructionExportStage.STAGE_COMPACTING_TEXTURE)  ExportStage = 1;
            else if (stage == (int)ReconstructionExportStage.STAGE_SAVING_MODEL_FILE)   ExportStage = 2;
            else if (stage == (int)ReconstructionExportStage.STAGE_EXTRACTING_COLLIDER) ExportStage = 3;            
            ExportPercentage = percentage;

            //if (stage == (int)ReconstructionExportStage.STAGE_EXTRACTING_MODEL)
            //    Debug.Log("Extracting Model: " + percentage + "%");
            //else if (stage == (int)ReconstructionExportStage.STAGE_COMPACTING_TEXTURE)
            //    Debug.Log("Compacting Textures: " + percentage + "%");
            //else if (stage == (int)ReconstructionExportStage.STAGE_EXTRACTING_COLLIDER)
            //    Debug.Log("Extracting Collider: " + percentage + "%");
            //else if (stage == (int)ReconstructionExportStage.STAGE_SAVING_MODEL_FILE)
            //    Debug.Log("Saving Model: " + percentage + "%");

            if (ExportStage == 3 && ExportPercentage == 100)
            {
                StopScanning();
                Debug.Log("[ViveSR] [RigidReconstruction] Finish Exporting");
            }
        }

        public static void GetExportProgress(ref int stage, ref int percentage)
        {
            stage = ExportStage;
            percentage = ExportPercentage;
        }

        public static void GetExportProgress(ref int percentage)
        {
            percentage = ExportStage * 25 + (int)(ExportPercentage * 0.25f);
        }

        public static void EnableLiveMeshExtraction(bool enable)
        {
            ViveSR_Framework.SetCommandBool(ViveSR_Framework.MODULE_ID_RIGID_RECONSTRUCTION, (int)(ReconstructionCmd.EXTRACT_POINT_CLOUD), enable);
        }

        public static void SetLiveMeshExtractionMode(ReconstructionLiveMeshExtractMode mode)
        {
            ViveSR_Framework.SetCommandInt(ViveSR_Framework.MODULE_ID_RIGID_RECONSTRUCTION, (int)(ReconstructionCmd.EXTRACT_VERTEX_NORMAL), (int)mode);
        }

        public static void StartScanning()
        {            
            ViveSR_Framework.SetCommandBool(ViveSR_Framework.MODULE_ID_RIGID_RECONSTRUCTION, (int)(ReconstructionCmd.START), true);
            IsScanning = true;
        }

        public static void StopScanning()
        {
            IsScanning = false;
            IsExportingMesh = false;
            ViveSR_Framework.SetCommandBool(ViveSR_Framework.MODULE_ID_RIGID_RECONSTRUCTION, (int)(ReconstructionCmd.STOP), true);
        }
    }

}