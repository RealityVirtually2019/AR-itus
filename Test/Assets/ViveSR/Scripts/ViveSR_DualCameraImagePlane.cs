//========= Copyright 2017, HTC Corporation. All rights reserved. ===========
using System.Runtime.InteropServices;
using UnityEngine;

namespace Vive.Plugin.SR
{
    [RequireComponent(typeof(MeshFilter))]
    public class ViveSR_DualCameraImagePlane : MonoBehaviour
    {
        // The index of the camera where the camera image comes from.
        public DualCameraIndex CameraIndex;

        // The index of the camera that this game object is attached under.
        private DualCameraIndex AttachedCameraIndex;
        public bool IsForCalibration = false;

        [Header("Internal parameters")]
        public int DistortedImageWidth = 0;
        public int DistortedImageHeight = 0;
        public int UndistortedImageWidth = 0;
        public int UndistortedImageHeight = 0;
        public double DistortedCx = 0f;
        public double DistortedCy = 0f;
        public double UndistortedCx = 0f;
        public double UndistortedCy = 0f;
        public double FocalLength = 0f;
        public float[] UndistortionMap;
        public int MeshResolutionShrinkingRatio = 4;

        private int NumVertexRows = 0;
        private int NumVertexCols = 0;

        private MeshRenderer meshRnd;
        private Material defaultMat;
        private Mesh defaulQuadMesh;
        private Mesh planeMesh = null;
        private Mesh distortMesh = null;

        [HideInInspector]
        private UndistortionMethod UndistortMethod = UndistortionMethod.DEFISH_BY_SRMODULE;
        public Texture2D undistortMap = null;
        public Material defishMat;

        private Vector3 imagePlaneInitialLocalPosition;
        private Quaternion imagePlaneInitialLocalRotation;
        private Vector3 imagePlaneInitialLocalScale;

        public void Initial()
        {
            SetAttachedCameraIndex();
            StoreInitialLocalTransform();
            RestoreToInitialLocalTransform();

            if (ViveSR_DualCameraImageCapture.UndistortTextureIsNative)
            {
                EnableNativeMeshManipulation(false);
            }

            defaulQuadMesh = GetComponent<MeshFilter>().mesh;
            meshRnd = GetComponent<MeshRenderer>();
            if ( meshRnd ) defaultMat = meshRnd.sharedMaterial;
            
            if (MeshResolutionShrinkingRatio < 1)
                MeshResolutionShrinkingRatio = 1;
            NumVertexRows = DistortedImageHeight / MeshResolutionShrinkingRatio;
            NumVertexCols = DistortedImageWidth / MeshResolutionShrinkingRatio;

            if (UndistortMethod == UndistortionMethod.DEFISH_BY_MESH)
            {
                CreateMesh(NumVertexRows, NumVertexCols);
            }
            else if (UndistortMethod == UndistortionMethod.DEFISH_BY_SRMODULE)
            {
                if (planeMesh == null)
                {
                    SetCorrectSize();
                    SetUV(CameraIndex);
                }

                if (ViveSR_DualCameraImageCapture.UndistortTextureIsNative)
                {
                    InitializeNativeMeshManipulation();
                    SetLocalTransformToIdentity();
                    if (gameObject.activeInHierarchy)
                    {
                        EnableNativeMeshManipulation(true);
                    }
                }
            }
        }

        public void SetUndistortMethod( UndistortionMethod newMethod )
        {
            if (UndistortMethod == newMethod) return;

            if (ViveSR_DualCameraImageCapture.UndistortTextureIsNative)
            {
                EnableNativeMeshManipulation(false);

                if (UndistortMethod == UndistortionMethod.DEFISH_BY_SRMODULE)
                {
                    RestoreToInitialLocalTransform();
                }
            }

            UndistortMethod = newMethod;
            if (UndistortMethod == UndistortionMethod.DEFISH_BY_MESH)
            {
                if (distortMesh == null)
                    CreateMesh(NumVertexRows, NumVertexCols);
                else
                    GetComponent<MeshFilter>().sharedMesh = distortMesh;
            }
            else
            {
                if (planeMesh == null)
                {
                    SetCorrectSize();
                    SetUV(CameraIndex);
                }
                else
                {
                    GetComponent<MeshFilter>().sharedMesh = planeMesh;
                }
                
                if (UndistortMethod == UndistortionMethod.DEFISH_BY_SRMODULE &&
                    ViveSR_DualCameraImageCapture.UndistortTextureIsNative)
                {
                    InitializeNativeMeshManipulation();
                    SetLocalTransformToIdentity();
                    if (gameObject.activeInHierarchy)
                    {
                        EnableNativeMeshManipulation(true);
                    }
                }
            }

            if ( meshRnd )
                meshRnd.sharedMaterial = defaultMat;
        }

        void OnEnable()
        {
            if (UndistortMethod == UndistortionMethod.DEFISH_BY_SRMODULE &&
                ViveSR_DualCameraImageCapture.UndistortTextureIsNative)
            {
                EnableNativeMeshManipulation(true);
            }
        }

        void OnDisable()
        {
            if (ViveSR_DualCameraImageCapture.UndistortTextureIsNative)
            {
                EnableNativeMeshManipulation(false);
            }
        }

        private void InitDefishMap()
        {
            float[] defishedUV = new float[DistortedImageWidth * DistortedImageHeight * 2];
            byte[] defishedUV_byte = new byte[DistortedImageWidth * DistortedImageHeight * 8];

            for (int i = 0; i < DistortedImageWidth; ++i)
            {
                for (int j = 0; j < DistortedImageHeight; ++j)
                {
                    int idx = j * DistortedImageWidth + i;
                    defishedUV[idx * 2 + 0] = UndistortionMap[idx * 4 + 0] / DistortedImageWidth;
                    defishedUV[idx * 2 + 1] = UndistortionMap[idx * 4 + 1] / DistortedImageHeight;
                }
            }

            System.Buffer.BlockCopy(defishedUV, 0, defishedUV_byte, 0, defishedUV_byte.Length);
            undistortMap = new Texture2D(DistortedImageWidth, DistortedImageHeight, TextureFormat.RGFloat, false);            
            undistortMap.LoadRawTextureData(defishedUV_byte); undistortMap.Apply();

            if (defishMat) defishMat.SetTexture("_DefishTex", undistortMap);
        }

        /// <summary>
        /// Create a mesh to display the camera image.
        /// </summary>
        private void CreateMesh(int numVertexRows, int numVertexCols)
        {
            // Mesh topology, where (i,j) is the row-major vertex index:
            //  +--> x
            //  |
            //  v    (0,0)--(0,1)--(0,2)-- ..
            //  -y     |   /  |   /  |   /
            //         |  /   |  /   |  /
            //       (1,0)--(1,1)--(1,2)-- ..
            //         |   /  |   /  |   /
            //         ..     ..     ..

            // Create vertices, set UVs, and create normals.
            UndistortMesh();            

            // Create triangles.
            int[] triangles = new int[(numVertexRows - 1) * (numVertexCols - 1) * 2 * 3];
            for (int i = 0; i < numVertexRows - 1; ++i)
            {
                for (int j = 0; j < numVertexCols - 1; ++j)
                {
                    int triangleIndex = ((numVertexCols - 1) * i + j) * 2 * 3;
                    int vertex00Index = numVertexCols * i + j;
                    int vertex01Index = vertex00Index + 1;
                    int vertex10Index = vertex00Index + numVertexCols;
                    int vertex11Index = vertex10Index + 1;
                    triangles[triangleIndex + 0] = vertex00Index;
                    triangles[triangleIndex + 1] = vertex01Index;
                    triangles[triangleIndex + 2] = vertex10Index;
                    triangles[triangleIndex + 3] = vertex10Index;
                    triangles[triangleIndex + 4] = vertex01Index;
                    triangles[triangleIndex + 5] = vertex11Index;
                }
            }

            // Set the mesh by these mesh components.
            distortMesh.triangles = triangles;
            GetComponent<MeshFilter>().sharedMesh = distortMesh;
        }

        /// <summary>
        /// Deform the image plane mesh according to the undistortion map in order to undistort the camera image.
        /// </summary>
        private void UndistortMesh()
        {
            // Transform from pixel space to image plane space.
            // Pixel space:
            //     origin: the upper-left corner of the image
            //     x axis: the local x axis
            //     y axis: the local y axis
            //     z axis: the local z axis
            //     unit: 1 pixel
            // Image plane space:
            //     origin: the principal point of the image scaled from pixel-unit to meter-unit
            //     x axis: the local x axis
            //     y axis: the local y axis
            //     z axis: the local z axis
            //     unit: 1 meter

            // Calculate the length of a pixel in the real space unit.
            float imagePlaneDistance = transform.localPosition.z;
            float pixelLength = imagePlaneDistance / (float)FocalLength;

            // Create vectors with zero z values.
            // Convert from right-handed coordinates to the left-handed coordinates.
            // Also create uvs.
            Vector3[] pixelVertices = new Vector3[NumVertexRows * NumVertexCols];
            Vector2[] uvs = new Vector2[NumVertexRows * NumVertexCols];
            Vector3[] normals = new Vector3[NumVertexRows * NumVertexCols];
            for (int i = 0; i < NumVertexRows; ++i)
            {
                for (int j = 0; j < NumVertexCols; ++j)
                {
                    int undistortionMapIndex = (i * MeshResolutionShrinkingRatio * DistortedImageWidth + j * MeshResolutionShrinkingRatio) * 4;
                    //float distortedX = UndistortionMap[undistortionMapIndex];
                    //float distortedY = UndistortionMap[undistortionMapIndex + 1];
                    float distortedX = (j * MeshResolutionShrinkingRatio) + 0.5f;
                    float distortedY = (i * MeshResolutionShrinkingRatio) + 0.5f;
                    float undistortedX = UndistortionMap[undistortionMapIndex + 2] - (float)UndistortedCx;
                    float undistortedY = UndistortionMap[undistortionMapIndex + 3] - (float)UndistortedCy;
                    pixelVertices[i * NumVertexCols + j] = new Vector3(undistortedX * pixelLength, -1 * undistortedY * pixelLength, 0f);
                    uvs[i * NumVertexCols + j] = new Vector2(distortedX / (float) DistortedImageWidth, distortedY / (float) DistortedImageHeight);
                    normals[i * NumVertexCols + j] = new Vector3(0.0f, 0.0f, -1.0f);
                }
            }

            distortMesh = new Mesh();
            distortMesh.vertices = pixelVertices;
            distortMesh.uv = uvs;
            distortMesh.normals = normals;
        }

        /// <summary>
        /// Set the scale of the image plane according to the image size and the focal length.
        /// </summary>
        private void SetCorrectSize()
        {
            float imageWidth = UndistortedImageWidth;
            float imageHeight = UndistortedImageHeight;
            float imageAspectRatio = imageWidth / imageHeight;

            // Get the distance of the image plane to the camera.
            // ASSUME the plane is in the z direction of the camera.
            float imagePlaneDisanceZ = transform.localPosition.z;
            float focalLength = (float)FocalLength;

            // Calculate the correct size of the image plane according to the size of
            // the original images, the image plane distance and the focal length.
            planeMesh = Instantiate(defaulQuadMesh);    // Copy from the default quad mesh.
            Vector3[] originalVertices = planeMesh.vertices;  // Get a copy of the vertices of the plane.
            Vector3 upperRightMostVertex = new Vector3(-1e8f, -1e8f, 0);
            Vector3 lowerLeftMostVertex = new Vector3(1e8f, 1e8f, 0);
            for (int i = 0; i < originalVertices.Length; i++)
            {
                upperRightMostVertex = Vector3.Max(upperRightMostVertex, originalVertices[i]);
                lowerLeftMostVertex = Vector3.Min(lowerLeftMostVertex, originalVertices[i]);
            }
            float imagePlaneWidth = upperRightMostVertex.x - lowerLeftMostVertex.x;
            float imagePlaneHeight = upperRightMostVertex.y - lowerLeftMostVertex.y;
            float imagePlaneAspectRatio = imagePlaneWidth / imagePlaneHeight;
            // Create the transformation matrices.
            // Translate to the geometric center.
            Vector3 geometricCenter = (upperRightMostVertex + lowerLeftMostVertex) / 2;
            Matrix4x4 translationToGeomatricCenter = Matrix4x4.TRS(-1 * geometricCenter, Quaternion.identity, Vector3.one);
            // Scale x and y to fit the vertical FOV.
            float fovScaleFactor = ((imageHeight / focalLength) * imagePlaneDisanceZ) / imagePlaneHeight;
            Matrix4x4 scalingForCorrectFov = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(fovScaleFactor, fovScaleFactor, 1));
            // Scale x to fit the aspect ratio.
            float aspectRatioScaleFactor = imageAspectRatio / imagePlaneAspectRatio;
            Matrix4x4 scalingForCorrectAspectRatio = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(aspectRatioScaleFactor, 1, 1));
            // Translate back to the origin by the inverse matrix.
            // Combine all transformations.
            Matrix4x4 transformationForOriginalImage = translationToGeomatricCenter.inverse * scalingForCorrectAspectRatio * scalingForCorrectFov * translationToGeomatricCenter;
            // Apply the transformation.
            for (int i = 0; i < originalVertices.Length; i++)
            {
                originalVertices[i] = transformationForOriginalImage.MultiplyPoint3x4(originalVertices[i]);
            }
            // Assign the vertices for the correct image plane size.
            planeMesh.vertices = originalVertices;            
        }

        /// <summary>
        /// Mirror the uv because the origin point of images of Unity is different from the source image.
        /// </summary>
        /// <param name="eye"></param>
        private void SetUV(DualCameraIndex eye)
        {
            Vector2[] srcUV = planeMesh.uv;
            Vector2[] dstUV = new Vector2[srcUV.Length];
            for (int i = 0; i < srcUV.Length; i++)
                dstUV[i] = new Vector2(srcUV[i].x, srcUV[srcUV.Length - i - 1].y);
            planeMesh.uv = dstUV;

            GetComponent<MeshFilter>().sharedMesh = planeMesh;
        }

        /// <summary>
        /// Let the native plugin able to modify the mesh vertex buffer.
        /// Assume the mesh contains only vertices components.
        /// The vertex position of the mesh will be modified.
        /// </summary>
        private void InitializeNativeMeshManipulation()
        {
            Mesh mesh = GetComponent<MeshFilter>().mesh;

            // Get the vertices if the origin of the vertices was at TrackedCameraObject.
            Transform objectToWorldTransform = transform;
            ViveSR_TrackedCamera trackedCamera;
            if (AttachedCameraIndex == DualCameraIndex.LEFT)
            {
                trackedCamera = ViveSR_DualCameraRig.Instance.TrackedCameraLeft;
            }
            else
            {
                trackedCamera = ViveSR_DualCameraRig.Instance.TrackedCameraRight;
            }
            Transform cameraToWorldTransform = trackedCamera.transform;
            Vector3[] vertices = mesh.vertices;
            for (int i = 0; i < vertices.Length; ++i)
            {
                // Transform the vertex from the object space to the camera space.
                vertices[i] = cameraToWorldTransform.InverseTransformPoint(objectToWorldTransform.TransformPoint(vertices[i]));
            }

            // Send mesh buffers to the plugin.
            mesh.MarkDynamic();
            GCHandle gcVertices = GCHandle.Alloc(vertices, GCHandleType.Pinned);
            // Send data to ViveSR Framework.
            switch (CameraIndex)
            {
            case DualCameraIndex.LEFT:
                if (IsForCalibration)
                {
                    ViveSR_Framework.SetParameterNativePtr(ViveSR_Framework.MODULE_ID_SEETHROUGH, (int)SeeThroughParam.MESH_CALIB_NATIVE_VERTEX_BUFFER_PTR_L, mesh.GetNativeVertexBufferPtr(0));
                    ViveSR_Framework.SetParameterNativePtr(ViveSR_Framework.MODULE_ID_SEETHROUGH, (int)SeeThroughParam.MESH_CALIB_NATIVE_VERTEX_ARRAY_L, gcVertices.AddrOfPinnedObject());
                }
                else
                {
                    ViveSR_Framework.SetParameterNativePtr(ViveSR_Framework.MODULE_ID_SEETHROUGH, (int)SeeThroughParam.MESH_NATIVE_VERTEX_BUFFER_PTR_L, mesh.GetNativeVertexBufferPtr(0));
                    ViveSR_Framework.SetParameterNativePtr(ViveSR_Framework.MODULE_ID_SEETHROUGH, (int)SeeThroughParam.MESH_NATIVE_VERTEX_ARRAY_L, gcVertices.AddrOfPinnedObject());
                }
                break;
            case DualCameraIndex.RIGHT:
                if (IsForCalibration)
                {
                    ViveSR_Framework.SetParameterNativePtr(ViveSR_Framework.MODULE_ID_SEETHROUGH, (int)SeeThroughParam.MESH_CALIB_NATIVE_VERTEX_BUFFER_PTR_R, mesh.GetNativeVertexBufferPtr(0));
                    ViveSR_Framework.SetParameterNativePtr(ViveSR_Framework.MODULE_ID_SEETHROUGH, (int)SeeThroughParam.MESH_CALIB_NATIVE_VERTEX_ARRAY_R, gcVertices.AddrOfPinnedObject());
                }
                else
                {
                    ViveSR_Framework.SetParameterNativePtr(ViveSR_Framework.MODULE_ID_SEETHROUGH, (int)SeeThroughParam.MESH_NATIVE_VERTEX_BUFFER_PTR_R, mesh.GetNativeVertexBufferPtr(0));
                    ViveSR_Framework.SetParameterNativePtr(ViveSR_Framework.MODULE_ID_SEETHROUGH, (int)SeeThroughParam.MESH_NATIVE_VERTEX_ARRAY_R, gcVertices.AddrOfPinnedObject());
                }
                break;
            }
            gcVertices.Free();            

            // Set the mesh local bounding box large enough such that the mesh is always visible.
            Bounds bounds = mesh.bounds;
            bounds.extents = new Vector3(100f, 100f, 100f);
            mesh.bounds = bounds;
        }

        private void EnableNativeMeshManipulation(bool enable)
        {
            if (CameraIndex == DualCameraIndex.LEFT)
            {
                if (IsForCalibration)
                {
                    ViveSR_Framework.SetParameterBool(ViveSR_Framework.MODULE_ID_SEETHROUGH, (int)SeeThroughParam.ENABLE_CALIB_IMAGE_PLANE_MESH_L, enable);
                }
                else
                {
                    ViveSR_Framework.SetParameterBool(ViveSR_Framework.MODULE_ID_SEETHROUGH, (int)SeeThroughParam.ENABLE_IMAGE_PLANE_MESH_L, enable);
                }
            }
            if (CameraIndex == DualCameraIndex.RIGHT)
            {
                if (IsForCalibration)
                {
                    ViveSR_Framework.SetParameterBool(ViveSR_Framework.MODULE_ID_SEETHROUGH, (int)SeeThroughParam.ENABLE_CALIB_IMAGE_PLANE_MESH_R, enable);
                }
                else
                {
                    ViveSR_Framework.SetParameterBool(ViveSR_Framework.MODULE_ID_SEETHROUGH, (int)SeeThroughParam.ENABLE_IMAGE_PLANE_MESH_R, enable);
                }
            }
        }

        private void SetAttachedCameraIndex()
        {
            if (CameraIndex == DualCameraIndex.LEFT)
            {
                if (IsForCalibration)
                {
                    AttachedCameraIndex = DualCameraIndex.RIGHT;
                }
                else
                {
                    AttachedCameraIndex = DualCameraIndex.LEFT;
                }
            }
            else
            {
                if (IsForCalibration)
                {
                    AttachedCameraIndex = DualCameraIndex.LEFT;
                }
                else
                {
                    AttachedCameraIndex = DualCameraIndex.RIGHT;
                }
            }
        }

        private void StoreInitialLocalTransform()
        {
            imagePlaneInitialLocalPosition = transform.localPosition;
            imagePlaneInitialLocalRotation = transform.localRotation;
            imagePlaneInitialLocalScale = transform.localScale;
        }

        /// <summary>
        /// Move the image plane as a child of game object DualCamera (head),
        /// and set the local transform of the image plane to identity.
        /// </summary>
        private void SetLocalTransformToIdentity()
        {
            transform.parent = ViveSR_DualCameraRig.Instance.transform;
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
        }

        /// <summary>
        /// Move the image plane as a child of game object Anchor (left/right),
        /// and set the local transform of the image plane to the initial value.
        /// </summary>
        private void RestoreToInitialLocalTransform()
        {
            if (AttachedCameraIndex == DualCameraIndex.LEFT)
            {
                transform.parent = ViveSR_DualCameraRig.Instance.TrackedCameraLeft.Anchor.transform;
            }
            else
            {
                transform.parent = ViveSR_DualCameraRig.Instance.TrackedCameraRight.Anchor.transform;
            }
            transform.localPosition = imagePlaneInitialLocalPosition;
            transform.localRotation = imagePlaneInitialLocalRotation;
            transform.localScale = imagePlaneInitialLocalScale;
        }
    }
}