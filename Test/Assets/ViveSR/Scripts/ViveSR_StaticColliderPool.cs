using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Vive.Plugin.SR
{
    [ExecuteInEditMode]
    public class ViveSR_StaticColliderPool : MonoBehaviour
    {       
        private List<ViveSR_StaticColliderInfo> allColliders = new List<ViveSR_StaticColliderInfo>();
        private int numClds;

        //
        private List<ViveSR_StaticColliderInfo> _tempList = new List<ViveSR_StaticColliderInfo>();
        private List<Vector3> placer_locations = new List<Vector3>();
        private List<GameObject> placer_list = new List<GameObject>();
        private GameObject _defaultPlacer;
        public float placerIntervalW = 0.1f;
        public float placerIntervalH = 0.1f;
        public float placerUpShift = 0.03f;
        public float placerShowScale = 0.03f;

#if UNITY_EDITOR
        public ColliderShapeType queriedShape;
        public PlaneOrientation queriedOrient;
        public ColliderCondition queriedCondition;
        public bool doingQuery = false;
        private bool lastDoingQuery = false;        
        public bool doingExtractPos = false;
        private bool lastDoingExtractPos = false;
#endif        

        // data pre-proc
        static public bool ProcessDataAndGenColliderInfo(GameObject go)
        {
            // organize and category collider type
            bool hasCollider = false;
            MeshFilter[] mFilters = go.GetComponentsInChildren<MeshFilter>();
            int numRnds = mFilters.Length;
            for (int id = 0; id < numRnds; ++id)
            {
                ViveSR_StaticColliderInfo cldInfo = mFilters[id].gameObject.AddComponent<ViveSR_StaticColliderInfo>();
                SceneUnderstandingObjectType semantic_type = ViveSR_SceneUnderstanding.GetSemanticTypeFromObjName(go.name);
                cldInfo.SemanticType = semantic_type;
                if (semantic_type != SceneUnderstandingObjectType.NONE)
                {
                    string s_idx = go.name.Replace(ViveSR_SceneUnderstanding.SemanticTypeToString(semantic_type) + "_", "").Replace("_cld", "");
                    cldInfo.SceneObjectID = int.Parse(s_idx);
                }

                string meshName = mFilters[id].name;
                string newName = "";
                bool thisIsCLD = false;

                if (meshName.Contains("PlaneConvexCollider"))
                {
                    newName = "PlaneConvexCollider";
                    cldInfo.SetBit((int)ColliderShapeType.CONVEX_SHAPE);
                    thisIsCLD = true;
                }
                else if (meshName.Contains("PlaneBBCollider"))
                {
                    newName = "PlaneBBCollider";
                    cldInfo.SetBit((int)ColliderShapeType.BOUND_RECT_SHAPE);
                    thisIsCLD = true;
                }
                else if (meshName.Contains("PlaneMeshCollider"))
                {
                    newName = "PlaneMeshCollider";
                    cldInfo.SetBit((int)ColliderShapeType.MESH_SHAPE);
                    thisIsCLD = true;
                }

                if (meshName.Contains("Horizontal")) cldInfo.SetBit((int)PlaneOrientation.HORIZONTAL);
                else if (meshName.Contains("Vertical")) cldInfo.SetBit((int)PlaneOrientation.VERTICAL);
                else cldInfo.SetBit((int)PlaneOrientation.OBLIQUE);

                hasCollider = (hasCollider || thisIsCLD);
                if (!thisIsCLD)
                {
                    Component.DestroyImmediate(cldInfo);
                }
                else
                {
                    // parse area
                    int areaStringStartIdx = meshName.LastIndexOf("Area_");
                    if (areaStringStartIdx != -1)
                    {
                        areaStringStartIdx = areaStringStartIdx + 5;
                        string curString = meshName.Substring(areaStringStartIdx);
                        int areaStringEndIdx = curString.IndexOf("_");
                        cldInfo.ApproxArea = float.Parse(curString.Substring(0, areaStringEndIdx));
                    }
                    else
                    {
                        cldInfo.SetBit((int)PlaneOrientation.FRAGMENT);
                    }

                    // parse normal
                    int normalStringStartIdx = meshName.LastIndexOf("Normal_");
                    if (normalStringStartIdx != -1)
                    {
                        normalStringStartIdx = normalStringStartIdx + 7;
                        string curString = meshName.Substring(normalStringStartIdx);
                        int normalXEndIdx = curString.IndexOf("_");
                        cldInfo.GroupNormal.x = float.Parse(curString.Substring(0, normalXEndIdx));

                        curString = curString.Substring(normalXEndIdx + 1);
                        int normalYEndIdx = curString.IndexOf("_");
                        cldInfo.GroupNormal.y = float.Parse(curString.Substring(0, normalYEndIdx));

                        curString = curString.Substring(normalYEndIdx + 1);
                        int normalZEndIdx = curString.IndexOf("_");
                        cldInfo.GroupNormal.z = float.Parse(curString.Substring(0, normalZEndIdx));
                    }

                    // parse right axis
                    int rightStringStartIdx = meshName.LastIndexOf("Right_");
                    if (rightStringStartIdx != -1)
                    {
                        rightStringStartIdx = rightStringStartIdx + 6;
                        string curString = meshName.Substring(rightStringStartIdx);
                        int rightXEndIdx = curString.IndexOf("_");
                        cldInfo.RectRightAxis.x = float.Parse(curString.Substring(0, rightXEndIdx));

                        curString = curString.Substring(rightXEndIdx + 1);
                        int rightYEndIdx = curString.IndexOf("_");
                        cldInfo.RectRightAxis.y = float.Parse(curString.Substring(0, rightYEndIdx));

                        curString = curString.Substring(rightYEndIdx + 1);
                        int rightZEndIdx = curString.IndexOf("_");
                        cldInfo.RectRightAxis.z = float.Parse(curString.Substring(0, rightZEndIdx));
                    }

                    // parse width height
                    int whStringStartIdx = meshName.LastIndexOf("WH_");
                    if (whStringStartIdx != -1)
                    {
                        whStringStartIdx = whStringStartIdx + 3;
                        string curString = meshName.Substring(whStringStartIdx);
                        int widthEndIdx = curString.IndexOf("_");
                        cldInfo.RectWidth = float.Parse(curString.Substring(0, widthEndIdx));

                        curString = curString.Substring(widthEndIdx + 1);
                        int heightEndIdx = curString.IndexOf("_");
                        cldInfo.RectHeight = float.Parse(curString.Substring(0, heightEndIdx));
                    }

                    // parse id
                    int idxStringStartIdx = meshName.LastIndexOf("_");
                    cldInfo.PlaneID = (int.Parse(meshName.Substring(idxStringStartIdx + 1)));
                    newName = newName + "_" + meshName.Substring(idxStringStartIdx + 1);
                    mFilters[id].gameObject.name = newName;
                }
            }

            return hasCollider;
        }

        public void OrganizeHierarchy()
        {
            ViveSR_StaticColliderInfo[] infos = GetComponentsInChildren<ViveSR_StaticColliderInfo>(true);
            int len = infos.Length;

            // init list with length * null
            List<ViveSR_StaticColliderInfo> brInfoList = new List<ViveSR_StaticColliderInfo>(len);             
            List<ViveSR_StaticColliderInfo> cInfoList = new List<ViveSR_StaticColliderInfo>(len);            
            List<ViveSR_StaticColliderInfo> mInfoList = new List<ViveSR_StaticColliderInfo>(len);
            brInfoList.AddRange(Enumerable.Repeat((ViveSR_StaticColliderInfo)null, len));
            cInfoList.AddRange(Enumerable.Repeat((ViveSR_StaticColliderInfo)null, len));
            mInfoList.AddRange(Enumerable.Repeat((ViveSR_StaticColliderInfo)null, len));

            GameObject meshCldGroup = new GameObject("PlaneMeshColliderGroup");
            {
                meshCldGroup.transform.SetParent(transform);
                GameObject HorizontalGroup = new GameObject("Horizontal");
                GameObject VerticalGroup = new GameObject("Vertical");
                GameObject ObliqueGroup = new GameObject("Oblique");
                GameObject FragmentGroup = new GameObject("Fragment");
                {
                    HorizontalGroup.transform.SetParent(meshCldGroup.transform);
                    VerticalGroup.transform.SetParent(meshCldGroup.transform);
                    ObliqueGroup.transform.SetParent(meshCldGroup.transform);
                    FragmentGroup.transform.SetParent(meshCldGroup.transform);
                }                
            }
            meshCldGroup.SetActive(true);

            GameObject convexCldGroup = new GameObject("PlaneConvexColliderGroup");
            {
                convexCldGroup.transform.SetParent(transform);
                GameObject HorizontalGroup = new GameObject("Horizontal");
                GameObject VerticalGroup = new GameObject("Vertical");
                GameObject ObliqueGroup = new GameObject("Oblique");
                {
                    HorizontalGroup.transform.SetParent(convexCldGroup.transform);
                    VerticalGroup.transform.SetParent(convexCldGroup.transform);
                    ObliqueGroup.transform.SetParent(convexCldGroup.transform);
                }
            }
            convexCldGroup.SetActive(false);

            GameObject bbCldGroup = new GameObject("PlaneBoundingRectColliderGroup");
            {
                bbCldGroup.transform.SetParent(transform);
                GameObject HorizontalGroup = new GameObject("Horizontal");
                GameObject VerticalGroup = new GameObject("Vertical");
                GameObject ObliqueGroup = new GameObject("Oblique");
                {
                    HorizontalGroup.transform.SetParent(bbCldGroup.transform);
                    VerticalGroup.transform.SetParent(bbCldGroup.transform);
                    ObliqueGroup.transform.SetParent(bbCldGroup.transform);
                }
            }
            bbCldGroup.SetActive(false);
            

            for (int i = 0; i < len; ++i)
            {
                Transform parent = transform;
                ViveSR_StaticColliderInfo cldInfo = infos[i];
                if (cldInfo.CheckHasAllBit((uint)ColliderShapeType.MESH_SHAPE))
                {
                    parent = parent.Find("PlaneMeshColliderGroup");
                    cldInfo.SetCorrespondingColliderOfType(ColliderShapeType.MESH_SHAPE, cldInfo);
                    mInfoList[cldInfo.PlaneID] = cldInfo;
                }
                else if (cldInfo.CheckHasAllBit((uint)ColliderShapeType.CONVEX_SHAPE))
                {
                    parent = parent.Find("PlaneConvexColliderGroup");
                    cldInfo.SetCorrespondingColliderOfType(ColliderShapeType.CONVEX_SHAPE, cldInfo);
                    cInfoList[cldInfo.PlaneID] = cldInfo;
                }
                else if (cldInfo.CheckHasAllBit((uint)ColliderShapeType.BOUND_RECT_SHAPE))
                {
                    parent = parent.Find("PlaneBoundingRectColliderGroup");
                    cldInfo.SetCorrespondingColliderOfType(ColliderShapeType.BOUND_RECT_SHAPE, cldInfo);
                    brInfoList[cldInfo.PlaneID] = cldInfo;
                }

                if (cldInfo.CheckHasAllBit((uint)PlaneOrientation.HORIZONTAL)) parent = parent.Find("Horizontal");
                else if (cldInfo.CheckHasAllBit((uint)PlaneOrientation.VERTICAL)) parent = parent.Find("Vertical");
                else if (cldInfo.CheckHasAllBit((uint)PlaneOrientation.OBLIQUE)) parent = parent.Find("Oblique");
                else parent = parent.Find("Fragment"); // this should only appear in PlaneMesh
                cldInfo.transform.SetParent(parent, true);
                cldInfo.gameObject.AddComponent<MeshCollider>();

                MeshRenderer rnd = cldInfo.gameObject.GetComponent<MeshRenderer>();
                if (rnd)
                {
                    Material wireframe = new Material(Shader.Find("ViveSR/Wireframe"));
                    wireframe.SetFloat("_ZTest", 0);
                    wireframe.SetFloat("_Thickness", 0);
                    rnd.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    rnd.receiveShadows = false;
                    rnd.sharedMaterial = wireframe;
                    rnd.enabled = false;
                }

            }

            // get collider of other groups
            _LinkColliderInfo(ColliderShapeType.BOUND_RECT_SHAPE, brInfoList, new List<ViveSR_StaticColliderInfo>[] { cInfoList, mInfoList });
            _LinkColliderInfo(ColliderShapeType.CONVEX_SHAPE, cInfoList, new List<ViveSR_StaticColliderInfo>[] { brInfoList, mInfoList });
            _LinkColliderInfo(ColliderShapeType.MESH_SHAPE, mInfoList, new List<ViveSR_StaticColliderInfo>[] { brInfoList, cInfoList });
        }

        private void _LinkColliderInfo(ColliderShapeType type, List<ViveSR_StaticColliderInfo> srcList, List<ViveSR_StaticColliderInfo>[] dstListArray)
        {
            for (int listID = 0; listID < dstListArray.Length; ++listID)
            {
                List<ViveSR_StaticColliderInfo> dstList = dstListArray[listID];
                for ( int i = 0; i < dstList.Count; ++i )
                {
                    if (dstList[i] != null && srcList[i] != null)
                        dstList[i].SetCorrespondingColliderOfType(type, srcList[i]);
                }
            }
        }

        // Unity 
        void Start()
        {
            if ( Application.isPlaying )
            {
                _defaultPlacer = GameObject.CreatePrimitive(PrimitiveType.Cube);
                _defaultPlacer.hideFlags = HideFlags.HideInHierarchy;
                Destroy(_defaultPlacer.GetComponent<Collider>());
                MeshRenderer rend = _defaultPlacer.GetComponent<MeshRenderer>();
                rend.material.shader = Shader.Find("Unlit/Color");
                rend.material.SetColor("_Color", Color.red);
                rend.enabled = false;
            }            

            ViveSR_StaticColliderInfo[] infoArray = GetComponentsInChildren<ViveSR_StaticColliderInfo>(true);
            for (int i = 0; i < infoArray.Length; ++i)
                this._AddColliderInfo(infoArray[i]);
        }

        void OnDestroy()
        {
            if (_defaultPlacer)
                Destroy(_defaultPlacer);
        }

        private void _AddColliderInfo(ViveSR_StaticColliderInfo info)
        {
            if (!allColliders.Contains(info))
                allColliders.Add(info);

            numClds = allColliders.Count;
        }

        private void _GetAllColliderHasProps_Internal(uint[] props)
        {
            int numProps = props.Length;
            _tempList.Clear();

            uint bits = 0;
            for (int i = 0; i < numProps; ++i)
                bits |= props[i];

            for (int j = 0; j < numClds; ++j)
            {
                if (allColliders[j].CheckHasAllBit(bits))
                    _tempList.Add(allColliders[j]);
            }
        }

        public ViveSR_StaticColliderInfo GetClosestColliderWithProps(Vector3 testPos, uint[] props)
        {
            _GetAllColliderHasProps_Internal(props);    // get filtered info in tempList
            int found_id = -1;
            float min_dist = float.MaxValue;
            for (int i = 0; i < _tempList.Count; i++)
            {
                ViveSR_StaticColliderInfo info = _tempList[i];
                float dist = Vector3.Distance(info.GetComponent<MeshRenderer>().bounds.center, testPos);
                if (dist < min_dist)
                {
                    min_dist = dist;
                    found_id = i;
                }
            }

            return (found_id == -1) ? null : _tempList[found_id];
        }

        public ViveSR_StaticColliderInfo GetFurthestColliderWithProps(Vector3 testPos, uint[] props)
        {
            _GetAllColliderHasProps_Internal(props);    // get filtered info in tempList
            int found_id = -1;
            float max_dist = float.MinValue;
            for (int i = 0; i < _tempList.Count; i++)
            {
                ViveSR_StaticColliderInfo info = _tempList[i];
                float dist = Vector3.Distance(info.GetComponent<MeshRenderer>().bounds.center, testPos);
                if (dist > max_dist)
                {
                    max_dist = dist;
                    found_id = i;
                }
            }

            return (found_id == -1) ? null : _tempList[found_id];
        }

        public ViveSR_StaticColliderInfo GetLargestCollider(uint[] props)
        {
            _GetAllColliderHasProps_Internal(props);    // get filtered info in tempList
            int found_id = -1;
            float max_Area = float.MinValue;
            for (int i = 0; i < _tempList.Count; i++)
            {
                ViveSR_StaticColliderInfo info = _tempList[i];
                if (info.ApproxArea > max_Area)
                {
                    max_Area = info.ApproxArea;
                    found_id = i;
                }
            }

            return (found_id == -1) ? null : _tempList[found_id];
        }       

        // get colliders within customized height range
        public ViveSR_StaticColliderInfo[] GetColliderByHeightRange(ColliderShapeType shapeType, float lowestHeight, float highestHeight)
        {
            _tempList.Clear();

            if (lowestHeight <= highestHeight)
            {
                for (int i = 0; i < allColliders.Count; i++)
                {
                    ViveSR_StaticColliderInfo info = allColliders[i];
                    if (info.CheckHasAllBit((uint)shapeType))
                    {
                        Vector3 center = info.GetComponent<MeshRenderer>().bounds.center;
                        if (center.y >= lowestHeight && center.y <= highestHeight)
                            _tempList.Add(info);
                    }
                }
            }
            return _tempList.ToArray();
        }

        public ViveSR_StaticColliderInfo[] GetAllColliderHasProps(uint[] props)
        {
            _GetAllColliderHasProps_Internal(props);
            return _tempList.ToArray();
        }

        public ViveSR_StaticColliderInfo[] GetColliderWithPropsAndCondition(uint[] props, ColliderCondition condition, Vector3 testPos = new Vector3())
        {
            if (condition == ColliderCondition.NONE)
                return GetAllColliderHasProps(props);

            ViveSR_StaticColliderInfo info = null;            
            if (condition == ColliderCondition.LARGEST)
                info = GetLargestCollider(props);
            else if (condition == ColliderCondition.CLOSEST)
                info = GetClosestColliderWithProps(testPos, props);
            else if (condition == ColliderCondition.FURTHEST)
                info = GetFurthestColliderWithProps(testPos, props);

            _tempList.Clear();
            if (info) _tempList.Add(info);

            return _tempList.ToArray();
        }

        public SceneUnderstandingObjectType GetSemanticType()
        {
            SceneUnderstandingObjectType type = SceneUnderstandingObjectType.NONE;

            if (numClds > 0 && allColliders[0].SemanticType != type)
                type = allColliders[0].SemanticType;

            return type;
        }

#if UNITY_EDITOR
        void Update()
        {
            if (doingQuery && !lastDoingQuery)
            {
                ShowAllColliderWithPropsAndCondition(new uint[] { (uint)queriedShape, (uint)queriedOrient }, queriedCondition, Camera.main.transform.position);
                if (doingExtractPos)
                {
                    ClearPlacerList();
                    DrawAllExtractedPlacerLocations( _tempList.ToArray() );
                }
            }
            else if (!doingQuery && lastDoingQuery)
            {
                HideAllColliderRenderers();
                ClearPlacerList();
            }
            lastDoingQuery = doingQuery;

            // object placer
            if (doingExtractPos && !lastDoingExtractPos)
            {
                DrawAllExtractedPlacerLocations( _tempList.ToArray() );
            }
            else if (!doingExtractPos && lastDoingExtractPos)
            {
                ClearPlacerList();
            }
            lastDoingExtractPos = doingExtractPos;
        }
#endif
        public void ShowAllColliderWithPropsAndCondition(uint[] props, ColliderCondition condition = ColliderCondition.NONE, Vector3 testPos = new Vector3())
        {
            _tempList.Clear();
            HideAllColliderRenderers();

            GetColliderWithPropsAndCondition(props, condition, testPos);

            // draw
            int num = _tempList.Count;
            for (int i = 0; i < num; ++i)
            {
                MeshRenderer rnd = _tempList[i].GetComponent<MeshRenderer>();
                if (rnd == null)
                    rnd = _tempList[i].gameObject.AddComponent<MeshRenderer>();

                Material wireframe = new Material(Shader.Find("ViveSR/Wireframe"));
                wireframe.SetFloat("_ZTest", 0);
                wireframe.SetFloat("_Thickness", 0);
                rnd.sharedMaterial = wireframe;
                rnd.enabled = true;
            }
        }

        public void DrawAllExtractedPlacerLocations(ViveSR_StaticColliderInfo[] infoArray)
        {
            for (int infoIdx = 0; infoIdx < infoArray.Length; ++infoIdx)
            {
                //Vector3[] raycastPositions;
                Quaternion outRot;
                ViveSR_StaticColliderInfo info = infoArray[infoIdx];
                info.GetColliderUsableLocations(placerIntervalW, placerIntervalH, placerUpShift, placer_locations, out outRot);
                for (int i = 0; i < placer_locations.Count; i++)
                {
                    GameObject placer = GameObject.Instantiate(_defaultPlacer);
                    placer.GetComponent<MeshRenderer>().enabled = true;
                    placer.transform.localScale = new Vector3(placerShowScale, placerShowScale, placerShowScale);
                    placer.transform.position = placer_locations[i];
                    placer.transform.rotation = outRot;
                    placer_list.Add(placer);
                }
            }
        }

        public void HideAllColliderRenderers()
        {
            for (int i = 0; i < allColliders.Count; i++)
                allColliders[i].GetComponent<MeshRenderer>().enabled = false;
        }

        public void ClearPlacerList()
        {
            for (int i = 0; i < placer_list.Count; i++)
                Destroy(placer_list[i]);

            placer_list.Clear();
        }
    }
}