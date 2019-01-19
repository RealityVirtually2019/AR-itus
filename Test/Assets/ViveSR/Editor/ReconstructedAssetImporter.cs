using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

namespace Vive.Plugin.SR
{
    public class ReconstructedAssetImporter : AssetPostprocessor
    {
        void OnPreprocessModel()
        {
            // assetPath;
            // assetImporter;
            if (!assetPath.Contains("/Recons3DAsset/"))
                return;

            ModelImporter importer = assetImporter as ModelImporter;
            importer.meshCompression = ModelImporterMeshCompression.Off;
            importer.optimizeMesh = false;
            importer.importBlendShapes = false;
            importer.isReadable = false;

            if (assetPath.Contains("/VertexColor/"))        // not used
                importer.importMaterials = false;
            else
                importer.importMaterials = true;

            importer.importNormals = ModelImporterNormals.None;
            importer.importTangents = ModelImporterTangents.None;

            if (assetPath.Contains("_cld.obj"))
                importer.importMaterials = false;
            else
                importer.materialSearch = ModelImporterMaterialSearch.Local;
        }

        void OnPreprocessTexture()
        {
            // assetPath;
            // assetImporter;
            if (!assetPath.Contains("/Recons3DAsset/"))
                return;

            TextureImporter importer = assetImporter as TextureImporter;
            importer.mipmapEnabled = false;
        }

        void OnPostprocessModel(GameObject curGO)
        {
            if (!assetPath.Contains("/Recons3DAsset/"))
                return;

            if (ViveSR_StaticColliderPool.ProcessDataAndGenColliderInfo(curGO) == true)
            {
                ViveSR_StaticColliderPool cldPool = curGO.AddComponent<ViveSR_StaticColliderPool>();
                cldPool.OrganizeHierarchy();
            }
            else
            {
                MeshRenderer[] rnds = curGO.GetComponentsInChildren<MeshRenderer>(true);
                int len = rnds.Length;
                for (int i = 0; i < len; ++i)
                    rnds[i].sharedMaterial.shader = Shader.Find("Unlit/Texture");
            }
        }
    }
}
