using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Vive.Plugin.SR
{
    public enum ColliderShapeType
    {
        UNDEFINED = 0,
        CONVEX_SHAPE = 1,
        BOUND_RECT_SHAPE = 2,
        MESH_SHAPE = 4,
    }    

    public enum PlaneOrientation
    {
        UNDEFINED = 0,
        HORIZONTAL = 8,
        VERTICAL = 16,
        OBLIQUE = 32,
        FRAGMENT = 64,
    }

    public enum ColliderCondition
    {
        NONE,
        LARGEST,
        CLOSEST,
        FURTHEST
    }

    [ExecuteInEditMode]
    public class ViveSR_StaticColliderInfo : MonoBehaviour
    {
        [SerializeField] private ColliderShapeType _shapeType;
        [SerializeField] private PlaneOrientation _orientation;
        public float ApproxArea = 0.0f;
        public Vector3 GroupNormal = Vector3.zero;
        public float RectWidth = 0.0f;
        public float RectHeight = 0.0f;
        public Vector3 RectRightAxis = Vector3.right;
        [HideInInspector] 
        [SerializeField] private uint PropBits;

        private int _planeID = -1;
        private int _sceneObjID = -1;
        private SceneUnderstandingObjectType _semanticType = SceneUnderstandingObjectType.NONE;

        [SerializeField] private ViveSR_StaticColliderInfo _corresConvexCld = null;
        [SerializeField] private ViveSR_StaticColliderInfo _corresBoundingRectCld = null;
        [SerializeField] private ViveSR_StaticColliderInfo _corresMeshCld = null;

        public int PlaneID 
        { 
            get { return _planeID;   }
            set { _planeID = value;  } 
        }

        public int SceneObjectID
        {
            get { return _sceneObjID;  }
            set { _sceneObjID = value; }
        }

        public SceneUnderstandingObjectType SemanticType
        {
            get { return _semanticType;  }
            set { _semanticType = value; }
        }

        public PlaneOrientation Orientation { get { return _orientation; } }

        private float new_width, new_height;

        void Awake()
        {
            PropBits = (uint)_shapeType | (uint)_orientation;
        }

        public void SetBit(uint bit)
        {
            if (bit == (uint)ColliderShapeType.CONVEX_SHAPE || bit == (uint)ColliderShapeType.BOUND_RECT_SHAPE || bit == (uint)ColliderShapeType.MESH_SHAPE)
                _shapeType = (ColliderShapeType)bit;
            else if (bit == (uint)PlaneOrientation.HORIZONTAL || bit == (uint)PlaneOrientation.VERTICAL || bit == (uint)PlaneOrientation.OBLIQUE || bit == (uint)PlaneOrientation.FRAGMENT)
                _orientation = (PlaneOrientation)bit;

            PropBits = (uint)_shapeType | (uint)_orientation;
        }

        public bool CheckHasAllBit(uint bit)
        {
            return ((PropBits & bit) == bit);
        }

        public void SetCorrespondingColliderOfType(ColliderShapeType type, ViveSR_StaticColliderInfo info)
        {
            if (type == ColliderShapeType.CONVEX_SHAPE)
            {
                _corresConvexCld = info;
            }
            else if (type == ColliderShapeType.BOUND_RECT_SHAPE)
            {
                _corresBoundingRectCld = info;
            }
            else if (type == ColliderShapeType.MESH_SHAPE)
            {
                _corresMeshCld = info;
            }
        }

        public ViveSR_StaticColliderInfo GetCorrespondingColliderOfType(ColliderShapeType type)
        {
            if (type == ColliderShapeType.CONVEX_SHAPE)
            {
                return _corresConvexCld;
            }
            else if (type == ColliderShapeType.BOUND_RECT_SHAPE)
            {
                return _corresBoundingRectCld;
            }
            else if (type == ColliderShapeType.MESH_SHAPE)
            {
                return _corresMeshCld;
            }
            else
            {
                return null;
            }
        }

        // To-Do: return raycastPositions ??
        public void GetColliderUsableLocations(float intervalW, float intervalH, float surf_shift, List<Vector3> outLocations, out Quaternion rotation)
        {
            outLocations.Clear();
            rotation = new Quaternion();

            ViveSR_StaticColliderInfo bb_cldInfo = GetCorrespondingColliderOfType(ColliderShapeType.BOUND_RECT_SHAPE);
            if (bb_cldInfo != null)
            {
                // Get collider axes
                Vector3 bb_center = bb_cldInfo.GetComponent<MeshRenderer>().bounds.center;
                Vector3 right = bb_cldInfo.RectRightAxis;
                Vector3 up = bb_cldInfo.GroupNormal;
                Vector3 forward = Vector3.Cross(right, up);
                forward.Normalize();
                right = Vector3.Cross(up, forward);
                right.Normalize();
                float bb_width = bb_cldInfo.RectWidth;
                float bb_height = bb_cldInfo.RectHeight;
                // return rotation
                rotation.SetLookRotation(forward, up);

                // Check if collider exists in each position by Raycast
                for (float j = -bb_height / 2 + intervalH / 2; j <= bb_height / 2; j += intervalH)
                {
                    for (float i = -bb_width / 2 + intervalW / 2; i <= bb_width / 2; i += intervalW)
                    {
                        Vector3 pos = bb_center + right * i + forward * j;
                        Vector3 pos_raycast = pos + up * 0.1f;
                        RaycastHit hit;
                        Physics.Raycast(pos_raycast, -up, out hit);
                        if (hit.collider != null && hit.collider.gameObject.name == this.gameObject.name)
                            outLocations.Add(pos + up * surf_shift);
                    }
                }
            }
        }

        public void GetColliderUsableLocationsWithRightAxis(float intervalW, float intervalH, float surf_shift, List<Vector3> outLocations, out Quaternion rotation, ref Vector3 rightVec)
        {
            outLocations.Clear();
            rotation = new Quaternion();

            ViveSR_StaticColliderInfo bb_cldInfo = GetCorrespondingColliderOfType(ColliderShapeType.BOUND_RECT_SHAPE);
            if (bb_cldInfo != null)
            {
                // Get collider axes
                Vector3 bb_center = bb_cldInfo.GetComponent<MeshRenderer>().bounds.center;
                Vector3 up = bb_cldInfo.GroupNormal;
                Vector3 forward = Vector3.Cross(rightVec, up);
                forward.Normalize();
                rightVec = Vector3.Cross(up, forward);
                rightVec.Normalize();
                float bb_width = bb_cldInfo.RectWidth;
                float bb_height = bb_cldInfo.RectHeight;

                // return rotation
                rotation.SetLookRotation(forward, up);

                // Calculate new range
                new_height = new_width = Mathf.Sqrt(Mathf.Pow(bb_height, 2) + Mathf.Pow(bb_width, 2));
                // Check if collider exists in each position by Raycast
                for (float j = -new_height / 2 + intervalH / 2; j <= new_height / 2; j += intervalH)
                {
                    for (float i = -new_width / 2 + intervalW / 2; i <= new_width / 2; i += intervalW)
                    {
                        Vector3 pos = bb_center + rightVec * i + forward * j;
                        Vector3 pos_raycast = pos + up * 0.1f;
                        RaycastHit hit;
                        Physics.Raycast(pos_raycast, -up, out hit);
                        if (hit.collider != null && hit.collider.gameObject.name == this.gameObject.name)
                            outLocations.Add(pos + up * surf_shift);
                    }
                }
            }
        }

    }
}

