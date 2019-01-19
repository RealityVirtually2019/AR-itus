using System;
using UnityEngine;
using System.Linq;
using System.Threading;
using System.Collections.Generic;
using System.Collections;

namespace Vive.Plugin.SR
{
    public class ViveSR_RigidReconstructionRenderer : ViveSR_Module
    {
        [Header("Init Config")]
        public string ConfigFilePath = "";        

        [Header("Rendering Control")]
        public ReconstructionQuality FullSceneQuality = ReconstructionQuality.MID;
        [Range(300, 1000)]
        public int RefreshIntervalMS = 300;
        
        public static ReconstructionDisplayMode LiveMeshDisplayMode { get; set; }
        private int LastLiveMeshDisplayMode = 10;   // UN-DEFINED
        private static int ThreadPeriod = 15;

        public static bool EnableSector = true;
        public static int MaxActiveGO = 200;
        private bool LastEnableSector = false;
        private bool BackupSectorValue = true;
        private float SectorSizeInMeter = 0.8f;
        public static bool SetWireFrameOpaque = true;
        private bool LastSetWireFrameOpaque = true;

        // render
        private bool RenderWithEnableSector;

        [SerializeField] private Material LiveMeshMaterial;
        [SerializeField] private Material WireframeMaterial;

        [Header("Information")]
        [ReadOnly] public int VertexNum;
        [ReadOnly] public int IndexNum;
        [ReadOnly] public int ProcessedFrame;
        [ReadOnly] public int VertStrideInFloat;
        [ReadOnly] public int ColliderNum;
        [ReadOnly] public int SectorNum;
        [ReadOnly] public int NumOfActiveGO;
        private int LastProcessedFrame = 0;

        private List<int> OrderedList = new List<int>();

        // Data
        private float[] VertexData;
        private int[] IndexData;
        private int[] SectorIDList;
        private int[] SectorVertNum;
        private int[] SectorMeshIdNum;
        private GameObject LiveMeshesGroups = null;   // put all mesh inside
        
        private static Dictionary<int, GameObject> ShowGameObjs = new Dictionary<int, GameObject>();
        private static Dictionary<int, MeshFilter> ShowMeshFilters = new Dictionary<int, MeshFilter>();
        private Material UsingMaterial;        

        #region Multi-Thread Get Mesh Data
        // Multi-thread Parse Raw Data to Data List for Mesh object 
        private Thread MeshDataThread = null;
        private Coroutine MeshDataCoroutine = null; // IEnumerator for main thread Mesh
        private int NumSubMeshes = 0;
        private int LastMeshes = 0;
        private int NumLastMeshVert = 0;
        private int VertID = 0;
        private int MeshIdxID = 0;
        private static Dictionary<int, List<Vector3>> MeshDataVertices = new Dictionary<int, List<Vector3>>();
        private static Dictionary<int, List<int>> MeshDataIndices = new Dictionary<int, List<int>>();
        private static Dictionary<int, List<Color32>> MeshDataColors = new Dictionary<int, List<Color32>>();
        private static Dictionary<int, List<Vector3>> MeshDataNormals = new Dictionary<int, List<Vector3>>();

        private bool IsMeshUpdate = false;
        private bool IsCoroutineRunning = false;
        private bool IsThreadRunning = true;
        #endregion

        private ViveSR_RigidReconstructionRenderer() { }
        private static ViveSR_RigidReconstructionRenderer Mgr = null;
        public static ViveSR_RigidReconstructionRenderer Instance
        {
            get
            {
                if (Mgr == null)
                {
                    Mgr = FindObjectOfType<ViveSR_RigidReconstructionRenderer>();
                }
                if (Mgr == null)
                {
                    Debug.LogError("ViveSR_RigidReconstructionRenderer does not be attached on GameObject");
                }
                return Mgr;
            }
        }

        // set self-setting to the static param
        public override bool RightBeforeStartModule()
        {
            bool result = ViveSR_RigidReconstruction.InitRigidReconstructionParamFromFile(ConfigFilePath);
            if (!result)
            {
                Debug.Log("[ViveSR] [RigidReconstruction] Set Config By Config File");
            }
            else
            {
                Debug.Log("[ViveSR] [RigidReconstruction] Config File Not Found, Set Config From GameObject");
                ViveSR_Framework.SetParameterInt(ViveSR_Framework.MODULE_ID_RIGID_RECONSTRUCTION, (int)ReconstructionParam.CONFIG_QUALITY, (int)FullSceneQuality);
                //ViveSR_RigidReconstruction.InitRigidReconstructionParam();
            }

            WireframeMaterial.SetFloat("_Opaque", SetWireFrameOpaque ? 1.0f : 0.0f);
            return true;
        }

        private bool UpdateRuntimeParameter()
        {
            bool result = true;
            int ret = (int)Error.FAILED;

            // live mesh display mode
            if ((int)LiveMeshDisplayMode != LastLiveMeshDisplayMode)
            {
                HideAllLiveMeshes();
                result = SetMeshDisplayMode(LiveMeshDisplayMode) && result;
                LastLiveMeshDisplayMode = (int)LiveMeshDisplayMode;
            }

            // full scene quality
            ret = ViveSR_Framework.SetParameterInt(ViveSR_Framework.MODULE_ID_RIGID_RECONSTRUCTION, (int)(ReconstructionParam.CONFIG_QUALITY), (int)FullSceneQuality);
            if ( LiveMeshDisplayMode == ReconstructionDisplayMode.FULL_SCENE )
                LiveMeshMaterial.SetFloat("_PointSizeScaler", (FullSceneQuality == ReconstructionQuality.LOW)? 1.2f : 0.8f);
            result = result && (ret == (int)Error.WORK);

            // update live adaptive param
            if (LiveMeshDisplayMode == ReconstructionDisplayMode.ADAPTIVE_MESH)
            {
                ret = ViveSR_Framework.SetParameterFloat(ViveSR_Framework.MODULE_ID_RIGID_RECONSTRUCTION, (int)ReconstructionParam.ADAPTIVE_MAX_GRID, ViveSR_RigidReconstruction.LiveAdaptiveMaxGridSize * 0.01f);   // cm to m
                result = result && (ret == (int)Error.WORK);
                ret = ViveSR_Framework.SetParameterFloat(ViveSR_Framework.MODULE_ID_RIGID_RECONSTRUCTION, (int)ReconstructionParam.ADAPTIVE_MIN_GRID, ViveSR_RigidReconstruction.LiveAdaptiveMinGridSize * 0.01f);
                result = result && (ret == (int)Error.WORK);
                ret = ViveSR_Framework.SetParameterFloat(ViveSR_Framework.MODULE_ID_RIGID_RECONSTRUCTION, (int)ReconstructionParam.ADAPTIVE_ERROR_THRES, ViveSR_RigidReconstruction.LiveAdaptiveErrorThres);
                result = result && (ret == (int)Error.WORK);
            }

            if (EnableSector != LastEnableSector)
            {
                HideAllLiveMeshes();
                ViveSR_Framework.SetParameterBool(ViveSR_Framework.MODULE_ID_RIGID_RECONSTRUCTION, (int)(ReconstructionParam.ENABLE_FRUSTUM_CULLING), EnableSector);
                ViveSR_Framework.SetParameterBool(ViveSR_Framework.MODULE_ID_RIGID_RECONSTRUCTION, (int)(ReconstructionParam.ENABLE_SECTOR_GROUPER), EnableSector);
                ViveSR_Framework.SetParameterFloat(ViveSR_Framework.MODULE_ID_RIGID_RECONSTRUCTION, (int)(ReconstructionParam.SECTOR_SIZE), SectorSizeInMeter);                
                LastEnableSector = EnableSector;
            }

            if (SetWireFrameOpaque != LastSetWireFrameOpaque)
            {
                WireframeMaterial.SetFloat("_Opaque", SetWireFrameOpaque ? 1.0f : 0.0f);
                LastSetWireFrameOpaque = SetWireFrameOpaque;
            }

            // refresh rate
            ViveSR_Framework.SetParameterInt(ViveSR_Framework.MODULE_ID_RIGID_RECONSTRUCTION, (int)ReconstructionParam.MESH_REFRESH_INTERVAL, RefreshIntervalMS);
            return result;
        }

        private void ResetData()
        {
            VertexNum = 0;
            IndexNum = 0;
            SectorNum = 0;
            NumOfActiveGO = 0;
        }

        public bool SetMeshDisplayMode(ReconstructionDisplayMode displayMode)
        {
            ResetData();

            int result = (int)Error.FAILED;
            if (displayMode == ReconstructionDisplayMode.FIELD_OF_VIEW)
            {
                ViveSR_Framework.SetParameterBool(ViveSR_Framework.MODULE_ID_RIGID_RECONSTRUCTION, (int)(ReconstructionParam.ENABLE_FRUSTUM_CULLING), false);
                ViveSR_Framework.SetParameterBool(ViveSR_Framework.MODULE_ID_RIGID_RECONSTRUCTION, (int)(ReconstructionParam.ENABLE_SECTOR_GROUPER), false);
                result = ViveSR_Framework.SetParameterBool(ViveSR_Framework.MODULE_ID_RIGID_RECONSTRUCTION, (int)(ReconstructionParam.LITE_POINT_CLOUD_MODE), true);
                result = ViveSR_Framework.SetParameterBool(ViveSR_Framework.MODULE_ID_RIGID_RECONSTRUCTION, (int)(ReconstructionParam.FULL_POINT_CLOUD_MODE), false);
                result = ViveSR_Framework.SetParameterBool(ViveSR_Framework.MODULE_ID_RIGID_RECONSTRUCTION, (int)(ReconstructionParam.LIVE_ADAPTIVE_MODE), false);
                LiveMeshMaterial.SetFloat("_PointSizeScaler", 1.2f);
                UsingMaterial = LiveMeshMaterial;
                ThreadPeriod = 15;
                BackupSectorValue = EnableSector;
                EnableSector = false;
            }
            else if (displayMode == ReconstructionDisplayMode.FULL_SCENE)
            {
                result = ViveSR_Framework.SetParameterBool(ViveSR_Framework.MODULE_ID_RIGID_RECONSTRUCTION, (int)(ReconstructionParam.LITE_POINT_CLOUD_MODE), false);
                result = ViveSR_Framework.SetParameterBool(ViveSR_Framework.MODULE_ID_RIGID_RECONSTRUCTION, (int)(ReconstructionParam.FULL_POINT_CLOUD_MODE), true);
                result = ViveSR_Framework.SetParameterBool(ViveSR_Framework.MODULE_ID_RIGID_RECONSTRUCTION, (int)(ReconstructionParam.LIVE_ADAPTIVE_MODE), false);
                LiveMeshMaterial.SetFloat("_PointSizeScaler", (FullSceneQuality == ReconstructionQuality.LOW) ? 1.3f : 0.8f);
                UsingMaterial = LiveMeshMaterial;
                ThreadPeriod = 300;
                EnableSector = BackupSectorValue;
            }
            else if (displayMode == ReconstructionDisplayMode.ADAPTIVE_MESH)
            {
                result = ViveSR_Framework.SetParameterBool(ViveSR_Framework.MODULE_ID_RIGID_RECONSTRUCTION, (int)(ReconstructionParam.LITE_POINT_CLOUD_MODE), false);
                result = ViveSR_Framework.SetParameterBool(ViveSR_Framework.MODULE_ID_RIGID_RECONSTRUCTION, (int)(ReconstructionParam.FULL_POINT_CLOUD_MODE), false);
                result = ViveSR_Framework.SetParameterBool(ViveSR_Framework.MODULE_ID_RIGID_RECONSTRUCTION, (int)(ReconstructionParam.LIVE_ADAPTIVE_MODE), true);
                UsingMaterial = WireframeMaterial;
                ThreadPeriod = 300;
                EnableSector = BackupSectorValue;
            }
            foreach (KeyValuePair<int, GameObject> go in ShowGameObjs)
            {
                go.Value.GetComponent<MeshRenderer>().sharedMaterial = UsingMaterial;
            }

            if (result == (int)Error.WORK) { LiveMeshDisplayMode = displayMode; }

            return (result == (int)Error.WORK);
        }


        // Use this for initialization
        public override bool Initial()
        {
            if (!ViveSR.EnableUnityReconstruction)
            {
                return false;
            }
            if (ViveSR.FrameworkStatus == FrameworkStatus.WORKING)
            {
                ViveSR_RigidReconstruction.AllocOutputDataMemory();

                if (LiveMeshesGroups == null)
                {
                    LiveMeshesGroups = new GameObject("LiveMeshes");
                    LiveMeshesGroups.transform.SetParent(gameObject.transform, false);
                }

                if(ShowMeshFilters == null) 
                    ShowMeshFilters = new Dictionary<int, MeshFilter>();
                
                if(ShowGameObjs == null)
                    ShowGameObjs = new Dictionary<int, GameObject>();

                LiveMeshesGroups.SetActive(true);

                LiveMeshDisplayMode = ReconstructionDisplayMode.ADAPTIVE_MESH;
                SetMeshDisplayMode(LiveMeshDisplayMode);
                if (MeshDataThread == null)
                {
                    IsMeshUpdate = false;
                    IsCoroutineRunning = false;
                    IsThreadRunning = true;
                    MeshDataThread = new Thread(ExtractMeshDataThread) { IsBackground = true };
                    MeshDataThread.Start();
                }

                BackupSectorValue = EnableSector;

                return true;
            }
            
            return false;
            
        }

        // Update is called once per frame
        void Update()
        {
            if (ViveSR_RigidReconstruction.IsExportingMesh || ViveSR_SceneUnderstanding.IsExportingSceneUnderstandingInfo || !ViveSR_RigidReconstruction.IsScanning)
            {
                HideAllLiveMeshes();
            }
            if (ViveSR_RigidReconstruction.IsScanning)
            {
                // when exporting, don't update live extraction parameter                
                UpdateRuntimeParameter();
                if (IsMeshUpdate == true)
                    MeshDataCoroutine = StartCoroutine(RenderMeshDataIEnumerator());
            }
        }
        public void ResetParameter()
        {
            EnableSector = true;
            LastEnableSector = false;
            BackupSectorValue = true;
            SetWireFrameOpaque = true;
            LastSetWireFrameOpaque = true;
            NumSubMeshes = 0;
            LastMeshes = 0;
            NumLastMeshVert = 0;
            VertID = 0;
            MeshIdxID = 0;
        }
        public override bool Release()
        {
            if (!ViveSR.EnableUnityReconstruction)
            {
                return false;
            }
            ViveSR_RigidReconstruction.StopScanning();
            ViveSR_SceneUnderstanding.ResetParameter();
            IsThreadRunning = false;
            if (MeshDataThread != null)
            {
                MeshDataThread.Join();
                MeshDataThread.Abort();
                MeshDataThread = null;
            }
            if (IsCoroutineRunning == true)
            {
                StopCoroutine(MeshDataCoroutine);
                MeshDataCoroutine = null;
            }
            ViveSR_RigidReconstruction.ReleaseAllocOutputDataMemory();

            foreach (KeyValuePair<int, GameObject> data in ShowGameObjs)
            {
                int id = data.Key;

                MeshDataVertices[id].Clear();
                MeshDataIndices[id].Clear();
                MeshDataColors[id].Clear();
                MeshDataNormals[id].Clear();
            }
            OrderedList.Clear();
            MeshDataVertices.Clear();
            MeshDataIndices.Clear();
            MeshDataColors.Clear();
            MeshDataNormals.Clear();
            HideAllLiveMeshes();
            DestroyLiveMeshes();
            ResetParameter();
            return true;
        }

        private static void HideAllLiveMeshes()
        {
            foreach (KeyValuePair<int, GameObject> data in ShowGameObjs)
            {
                if (data.Value != null)
                    data.Value.SetActive(false);
            }
        }

        private static void DestroyLiveMeshes()
        {

            foreach (KeyValuePair<int, GameObject> data in ShowGameObjs)
            {
                if (data.Value != null)
                {
                    GameObject.Destroy(data.Value);
                }
            }
            foreach (KeyValuePair<int, MeshFilter> data in ShowMeshFilters)
            {
                if (data.Value != null)
                {
                    GameObject.Destroy(data.Value);
                }
            }
            ShowMeshFilters.Clear();
            ShowGameObjs.Clear();
            ShowMeshFilters = null;
            ShowGameObjs = null; 
        }

        private IEnumerator RenderMeshDataIEnumerator()
        {
            IsCoroutineRunning = true;

            if (IsMeshUpdate == true)
            {
                NumOfActiveGO = 0;
                if (RenderWithEnableSector)
                {
                    for (int i = 0; i < SectorNum; i++)
                        SetMeshData(SectorIDList[i]);

                    // Take out key one by one. Either add new mesh or add age of meshes that are not updated.
                    UpdateMeshAge();
                }
                else
                {
                    for (int i = 0; i < NumSubMeshes; i++)
                        SetMeshData(i);
                }

                foreach (KeyValuePair<int, GameObject> go in ShowGameObjs)
                {
                    go.Value.GetComponent<MeshRenderer>().sharedMaterial = UsingMaterial;
                }

                IsMeshUpdate = false;
            }
            IsCoroutineRunning = false;
            yield return 0;
        }

        private void SetMeshData(int id)
        {
            if (!ShowGameObjs.ContainsKey(id))
            {
                AddNewMesh(id);
            }
            ShowGameObjs[id].SetActive(true);

            ShowMeshFilters[id].sharedMesh.Clear();
            ShowMeshFilters[id].sharedMesh.SetVertices(MeshDataVertices[id]);
            ShowMeshFilters[id].sharedMesh.SetColors(MeshDataColors[id]); 
            ShowMeshFilters[id].sharedMesh.SetIndices(MeshDataIndices[id].ToArray(), (LiveMeshDisplayMode == ReconstructionDisplayMode.ADAPTIVE_MESH && (IndexNum > 0)) ? MeshTopology.Triangles : MeshTopology.Points, 0);
            if (MeshDataNormals[id].Count > 0)
            {
                ShowMeshFilters[id].sharedMesh.SetNormals(MeshDataNormals[id]);
            }
        }

        private void AddNewMesh(int id)
        {
            GameObject go = new GameObject("SubMesh_" + id);
            go.transform.SetParent(LiveMeshesGroups.transform, false);

            MeshRenderer renderer = go.AddComponent<MeshRenderer>();
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.sharedMaterial = LiveMeshMaterial;

            MeshFilter filter = go.AddComponent<MeshFilter>();
            filter.mesh = new Mesh();
            filter.mesh.MarkDynamic();

            ShowGameObjs.Add(id, go);
            ShowMeshFilters.Add(id, filter);
        }

        private void UpdateMeshAge()
        {
            for (int i = OrderedList.Count - 1; i >= 0 ; i--)
            {
                for (int j = SectorNum - 1; j >= 0 ; j--)
                {
                    if (OrderedList[i] == SectorIDList[j])
                    {
                        OrderedList.RemoveAt(i);
                        break;
                    }
                }
            }

            // Insert updated game obj in with age 0
            for (int i = 0; i < SectorNum; i++)
            {
                OrderedList.Insert(0, SectorIDList[i]);
            }

            // Hide the oldest ones that are over max number of game objects
            for (int i = MaxActiveGO; i < OrderedList.Count; i++)
            {
                int id = OrderedList[i];
                ShowGameObjs[id].SetActive(false);
            }

            NumOfActiveGO = Math.Min(OrderedList.Count, MaxActiveGO);
        }

        private void ExtractMeshDataThread()
        {
            while (IsThreadRunning == true)
            {
                try
                {
                    if (IsMeshUpdate == false && ViveSR_RigidReconstruction.IsScanning == true)
                    {
                        bool result = ViveSR_RigidReconstruction.GetRigidReconstructionFrame(ref ProcessedFrame);
                        if (ProcessedFrame != LastProcessedFrame && result == true)
                        {                            
                            LastProcessedFrame = ProcessedFrame;
                            float[] _camPose;
                            result = ViveSR_RigidReconstruction.GetRigidReconstructionData(ref ProcessedFrame, out _camPose, ref VertexNum, out VertexData, ref VertStrideInFloat, out SectorIDList, ref SectorNum, out SectorVertNum, out SectorMeshIdNum, ref IndexNum, out IndexData);
                            
                            if (result == true)
                            {
                                NumSubMeshes = 0;
                                LastProcessedFrame = ProcessedFrame;
                                RenderWithEnableSector = EnableSector;

                                if (LiveMeshDisplayMode != ReconstructionDisplayMode.ADAPTIVE_MESH)
                                    UpdatePointCloudDataList();
                                else if (IndexNum > 0)
                                    UpdateMeshesDataList();
                            }
                        }
                    }
                }
                catch (System.Exception e)
                {
                    NumSubMeshes = 0;
                    Debug.LogWarning(e.Message);
                }

                Thread.Sleep(ThreadPeriod); //Avoid too fast get data from SR SDK DLL 
            }
        }

        private void UpdateMeshesDataList()
        {
            Color32 colorDst = new Color32();

            if (EnableSector && SectorNum > 0)
            {
                VertID = 0;
                MeshIdxID = 0;
                for (int i = 0; i < SectorNum; i++)
                {
                    int meshID = SectorIDList[i];
                    if (!MeshDataVertices.ContainsKey(meshID))
                    {
                        MeshDataVertices[meshID] = new List<Vector3>();
                        MeshDataIndices[meshID] = new List<int>();
                        MeshDataColors[meshID] = new List<Color32>();
                        MeshDataNormals[meshID] = new List<Vector3>();
                    }
                    else
                    {
                        MeshDataVertices[meshID].Clear();
                        MeshDataIndices[meshID].Clear();
                        MeshDataColors[meshID].Clear();
                        MeshDataNormals[meshID].Clear();
                    } 

                    int vert_num = SectorVertNum[i];
                    int idx_num = SectorMeshIdNum[i];

                    for (int j = 0; j < vert_num; j++)
                    {
                        int startOffset = VertID * VertStrideInFloat;
                        float x = VertexData[startOffset + 0];
                        float y = VertexData[startOffset + 1];
                        float z = VertexData[startOffset + 2];
                        MeshDataVertices[meshID].Add(new Vector3(x, y, z));
                        VertID++;

                        byte[] bits = BitConverter.GetBytes(VertexData[startOffset + 3]);
                        colorDst.r = bits[0];
                        colorDst.g = bits[1];
                        colorDst.b = bits[2];
                        colorDst.a = bits[3];
                        MeshDataColors[meshID].Add(colorDst);
                    }

                    for (int j = 0; j < idx_num; j++)
                    {
                        MeshDataIndices[meshID].Add(IndexData[MeshIdxID]);
                        MeshIdxID++;
                    }
                }
                IsMeshUpdate = true;
            }
            else if (!EnableSector && SectorNum == 0)
            {
                List<int> idMapping = Enumerable.Repeat(-1, VertexNum).ToList();

                int triNum = IndexNum / 3;
                int numSubVert = 0;
                int numSubTri = 0;

                if (!MeshDataVertices.ContainsKey(NumSubMeshes))
                {
                    MeshDataVertices[NumSubMeshes] = new List<Vector3>();
                    MeshDataIndices[NumSubMeshes] = new List<int>();
                    MeshDataColors[NumSubMeshes] = new List<Color32>();
                    MeshDataNormals[NumSubMeshes] = new List<Vector3>();
                }
                else
                {
                    MeshDataVertices[NumSubMeshes].Clear();
                    MeshDataIndices[NumSubMeshes].Clear();
                    MeshDataColors[NumSubMeshes].Clear();
                    MeshDataNormals[NumSubMeshes].Clear();
                }           

                for (int triID = 0; triID < triNum; ++triID)
                {
                    // if this iteration will exceed the limitation, output to a new geometry first
                    if ((numSubVert + 3) > 65000 || (numSubTri + 1) > 65000)
                    {
                        NumSubMeshes++;

                        if (!MeshDataVertices.ContainsKey(NumSubMeshes))
                        {
                            MeshDataVertices[NumSubMeshes] = new List<Vector3>();
                            MeshDataIndices[NumSubMeshes] = new List<int>();
                            MeshDataColors[NumSubMeshes] = new List<Color32>();
                            MeshDataNormals[NumSubMeshes] = new List<Vector3>();
                        }
                        else
                        {
                            MeshDataVertices[NumSubMeshes].Clear();
                            MeshDataIndices[NumSubMeshes].Clear();
                            MeshDataColors[NumSubMeshes].Clear();
                            MeshDataNormals[NumSubMeshes].Clear();
                        }     

                        // clear the counter etc
                        idMapping = Enumerable.Repeat(-1, VertexNum).ToList();
                        numSubVert = numSubTri = 0;
                    }

                    for (uint i = 0; i < 3; ++i)
                    {
                        // insert vertices and get new ID
                        int vertID = IndexData[triID * 3 + i];
                        if (idMapping[vertID] == -1)                // haven't added this vertex yet
                        {
                            idMapping[vertID] = MeshDataVertices[NumSubMeshes].Count;   // old ID -> new ID
                            float x = VertexData[vertID * VertStrideInFloat + 0];
                            float y = VertexData[vertID * VertStrideInFloat + 1];
                            float z = VertexData[vertID * VertStrideInFloat + 2];
                            MeshDataVertices[NumSubMeshes].Add(new Vector3(x, y, z));

                            byte[] bits = BitConverter.GetBytes(VertexData[vertID * VertStrideInFloat + 3]);
                            colorDst.r = bits[0];
                            colorDst.g = bits[1];
                            colorDst.b = bits[2];
                            colorDst.a = bits[3];
                            MeshDataColors[NumSubMeshes].Add(colorDst);

                            ++numSubVert;
                        }
                        MeshDataIndices[NumSubMeshes].Add(idMapping[vertID]);
                    }
                    ++numSubTri;
                }

                ++NumSubMeshes;
                IsMeshUpdate = true;
            }
        }

        private void UpdatePointCloudDataList()
        {
            // vertStrideInFloat > 4 ==> has normal component
            bool withNormal = (VertStrideInFloat > 4);
            VertID = 0;

            if (EnableSector)
            {
                for (int i = 0; i < SectorNum; i++)
                {
                    UpdateSinglePointCloudData(SectorIDList[i], SectorVertNum[i], withNormal);
                }
                IsMeshUpdate = true;
            }
            else
            {
                NumSubMeshes = (int)Math.Ceiling((float)VertexNum / 65000);
                LastMeshes = (NumSubMeshes * 65000 == VertexNum) ? NumSubMeshes + 1 : NumSubMeshes;
                NumLastMeshVert = VertexNum - (65000 * (LastMeshes - 1));
                for (int i = 0; i < NumSubMeshes; i++)
                {
                    int numVerts = (i == NumSubMeshes - 1) ? NumLastMeshVert : 65000;
                    UpdateSinglePointCloudData(i, numVerts, withNormal);
                }
                IsMeshUpdate = true;
            }
        }

        private void UpdateSinglePointCloudData(int meshID, int numVert, bool withNormal)
        {
            Vector3 vertexDst = new Vector3();
            Color32 colorDst = new Color32();
            Vector3 normalDst = new Vector3();

            if (!MeshDataVertices.ContainsKey(meshID))
            {
                MeshDataVertices[meshID] = new List<Vector3>();
                MeshDataIndices[meshID] = new List<int>();
                MeshDataColors[meshID] = new List<Color32>();
                MeshDataNormals[meshID] = new List<Vector3>();
            }
            else
            {
                MeshDataVertices[meshID].Clear();
                MeshDataIndices[meshID].Clear();
                MeshDataColors[meshID].Clear();
                MeshDataNormals[meshID].Clear();
            }            

            for (int i = 0; i < numVert; ++i)
            {
                //int vertID = meshID * 65000 + i;
                int startOffset = VertID * VertStrideInFloat;
                float x = VertexData[startOffset + 0];
                float y = VertexData[startOffset + 1];
                float z = VertexData[startOffset + 2];
                vertexDst.Set(x, y, z);
                MeshDataVertices[meshID].Add(vertexDst);

                byte[] bits = BitConverter.GetBytes(VertexData[startOffset + 3]);
                colorDst.r = bits[0];
                colorDst.g = bits[1];
                colorDst.b = bits[2];
                colorDst.a = bits[3];
                MeshDataColors[meshID].Add(colorDst);

                if (withNormal)
                {
                    float norX = VertexData[startOffset + 4];
                    float norY = VertexData[startOffset + 5];
                    float norZ = VertexData[startOffset + 6];
                    normalDst.Set(norX, norY, norZ);
                    MeshDataNormals[meshID].Add(normalDst);
                }

                MeshDataIndices[meshID].Add(i);
                VertID++;
            }
        }
    }
}
