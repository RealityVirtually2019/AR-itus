using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Events;

namespace Vive.Plugin.SR
{
    public class ViveSR : MonoBehaviour
    {
        public static FrameworkStatus FrameworkStatus { get; protected set; }
        public static string LastError { get; protected set; }
        public bool EnableAutomatically;

        //Notice: it needs to set enable before ViveSR initial
        [Header("[ViveSR Framework Pre-Setting]")] 
        public bool EnableSeeThroughModule;
        public bool EnableDepthModule;
        public bool EnableReconstructionModule;
        public bool EnableAIModule;

        public static bool EnableUnitySeeThrough = false;
        public static bool EnableUnityDepth = false;
        public static bool EnableUnityReconstruction = false;
        public static bool EnableUnityAI = false;

        [Header("[ViveSR Modules Unity Registration]")]
        public ViveSR_Module[] Modules = new ViveSR_Module[3];
        private Coroutine InitCoroutine = null; 

        [HideInInspector] public List<UnityAction> OnStartFailed = new List<UnityAction>();
        [HideInInspector] public List<UnityAction> OnStartComplete = new List<UnityAction>();

        private static ViveSR Mgr = null;
        public static ViveSR Instance
        {
            get
            {
                if (Mgr == null)
                {
                    Mgr = FindObjectOfType<ViveSR>();
                }
                if (Mgr == null)
                {
                    Debug.LogError("ViveSR does not be attached on GameObject");
                }
                return Mgr;
            }
        }

        void Start()
        {
            FrameworkStatus = FrameworkStatus.STOP;
            if (EnableAutomatically)
            {
                StartFramework();
            }
        }

        void OnDestroy()
        {
            StopFramework();
        }

        public void StartFramework()
        {
            if (InitCoroutine != null) return;
            InitCoroutine = StartCoroutine(StartFrameworkCoroutine());
        }

        private IEnumerator StartFrameworkCoroutine()
        {
            FrameworkStatus = FrameworkStatus.INITIAL;
            int result = (int)Error.WORK;
            // Before initialize framework
            yield return new WaitForEndOfFrame();

            if (result == (int)Error.WORK) result = ViveSR_InitialFramework();
            if (result == (int)Error.WORK) Debug.Log("[ViveSR] Initial Framework : " + result);
            else
            {
                FrameworkStatus = FrameworkStatus.ERROR;
                LastError = "[ViveSR] Initial Framework : " + result;
                Debug.LogError(LastError);
                for (int i = 0; i < OnStartFailed.Count; i++) if (OnStartFailed[i] != null) OnStartFailed[i]();
                StopFramework();
                yield break;
            }
            yield return new WaitForEndOfFrame();

            foreach (var module in Modules)
            {
                if (module != null) module.RightBeforeStartModule();
                yield return new WaitForEndOfFrame();
            }

            // Start framework
            if (result == (int)Error.WORK) result = ViveSR_StartFramework();
            if (result == (int)Error.WORK)
            {
                FrameworkStatus = FrameworkStatus.WORKING;
                Debug.Log("[ViveSR] Start Framework : " + result);
            }
            else
            {
                FrameworkStatus = FrameworkStatus.ERROR;
                LastError = "[ViveSR] Start Framework : " + result;
                Debug.LogError(LastError);
                for (int i = 0; i < OnStartFailed.Count; i++) if (OnStartFailed[i] != null) OnStartFailed[i]();
                StopFramework();
                yield break;
            }
            yield return new WaitForEndOfFrame();


            if (FrameworkStatus == FrameworkStatus.WORKING)
            {
                foreach (var module in Modules)
                {
                    if (module != null)
                    {
                        module.gameObject.SetActive(true);
                        if (!module.Initial())
                        {
                            module.gameObject.SetActive(false);
                        }
                    }
                    yield return new WaitForEndOfFrame();
                }
            }
            yield return new WaitForEndOfFrame();
            for (int i = 0; i < OnStartComplete.Count; i++) if (OnStartComplete[i] != null) OnStartComplete[i]();
        }

        public void StopFramework()
        {
            if (InitCoroutine != null)
            {
                StopCoroutine(InitCoroutine);
                InitCoroutine = null;
            }

            if (FrameworkStatus == FrameworkStatus.WORKING)
            {
                foreach (var module in Modules)
                {
                    if (module != null)
                    {
                        module.Release();
                        module.gameObject.SetActive(false);
                    }
                }
            }

            if (FrameworkStatus != FrameworkStatus.STOP)
            {
                int result = ViveSR_StopFramework();
                if (result != (int)Error.WORK)
                {
                    FrameworkStatus = FrameworkStatus.ERROR;
                    LastError = "[ViveSR] Stop Framework : " + result;
                    Debug.LogError(LastError);
                }
                else
                {
                    FrameworkStatus = FrameworkStatus.STOP;
                    Debug.Log("[ViveSR] Stop Framework");
                }
            }
            else
            {
                Debug.Log("[ViveSR] Stop Framework : not open");
            }
        }

        protected virtual int ViveSR_InitialFramework()
        {
            int result = (int)Error.FAILED;
            result = ViveSR_Framework.Initial();
            if (result != (int)Error.WORK){ Debug.Log("[ViveSR] ViveSR_Framework.Initial() Error " + result); }

            result = ViveSR_Framework.SetLogLevel(10);
            if (result != (int)Error.WORK) { Debug.Log("[ViveSR] ViveSR_Framework.SetLogLevel() Error "+ result); }

            if (EnableSeeThroughModule)
            {
                result = ViveSR_Framework.CreateModule((int)ModuleType.ENGINE_SEETHROUGH, ref ViveSR_Framework.MODULE_ID_SEETHROUGH);
                if (result != (int)Error.WORK) { Debug.LogWarning("[ViveSR] SEETHROUGH CreateModule Error " + result); return result; }
                else { EnableUnitySeeThrough = EnableSeeThroughModule; }
                if (EnableDepthModule)
                {
                    result = ViveSR_Framework.CreateModule((int)ModuleType.ENGINE_DEPTH, ref ViveSR_Framework.MODULE_ID_DEPTH);
                    if (result != (int)Error.WORK) { Debug.LogWarning("[ViveSR] DEPTH CreateModule Error " + result); return result; }
                    else { EnableUnityDepth = EnableDepthModule; }
                    if (EnableReconstructionModule)
                    {
                        if (EnableAIModule) {
                            result = ViveSR_Framework.CreateModule((int)ModuleType.ENGINE_AI_VISION, ref ViveSR_Framework.MODULE_ID_AI_VISION);
                            if (result != (int)Error.WORK) { Debug.LogWarning("[ViveSR] AI_SCENE CreateModule Error " + result); return result; }
                            else { EnableUnityAI = EnableAIModule; }
                        }
                        else
                        {
                            Debug.Log("[ViveSR][Warning] Disable EnableAIModule");
                        }

                        result = ViveSR_Framework.CreateModule((int)ModuleType.ENGINE_RIGID_RECONSTRUCTION, ref ViveSR_Framework.MODULE_ID_RIGID_RECONSTRUCTION);
                        if (result != (int)Error.WORK) { Debug.LogWarning("[ViveSR] RIGID_RECONSTRUCTION CreateModule Error " + result); return result; }
                        else { EnableUnityReconstruction = EnableReconstructionModule; }
                    }
                    else
                    {
                        Debug.Log("[ViveSR][Warning] Disable ReconstructionModule, EnableAIModule");
                    }
                }
                else
                {
                    Debug.Log("[ViveSR][Warning] Disable DepthModule, ReconstructionModule, EnableAIModule");
                }
            }
            else
            {
                Debug.Log("[ViveSR][Warning] Disable EnableSeeThroughModule, DepthModule, ReconstructionModule, EnableAIModule");
            }

            return result;
        }

        protected virtual int ViveSR_StartFramework()
        {
            int result = (int)Error.FAILED;

            #region Start Modules Flow
            if (EnableSeeThroughModule)
            {
                result = ViveSR_Framework.StartModule(ViveSR_Framework.MODULE_ID_SEETHROUGH);
                if (result != (int)Error.WORK) { Debug.LogWarning("[ViveSR] SEETHROUGH StartModule Error " + result); return result; }

                if (EnableDepthModule)
                {
                    result = ViveSR_Framework.StartModule(ViveSR_Framework.MODULE_ID_DEPTH);
                    if (result != (int)Error.WORK) { Debug.LogWarning("[ViveSR] DEPTH StartModule Error " + result); return result; }

                    if (EnableReconstructionModule)
                    {
                        if (EnableAIModule)
                        {
							// set AI_VISION module model path (default location as follows)
#if UNITY_EDITOR
                            string ModelPath = Application.dataPath + "/ViveSR/Plugins";
#else
							string ModelPath = Application.dataPath + "/Plugins";
#endif
                            AI_VisionModuleInfo Info = new AI_VisionModuleInfo();
                            ViveSR_Framework.GetParameterStruct(ViveSR_Framework.MODULE_ID_AI_VISION, (int)AIVisionParam.MODULE_INFO, ref Info);
                            Info.Model_Path = ModelPath;
                            Info.Model_PathLength = ModelPath.Length;
                            Info.ProcessUnit = 2;
                            ViveSR_Framework.SetParameterStruct(ViveSR_Framework.MODULE_ID_AI_VISION, (int)AIVisionParam.MODULE_INFO, Info);

                            result = ViveSR_Framework.StartModule(ViveSR_Framework.MODULE_ID_AI_VISION);
                            if (result == 1) Debug.LogWarning("[ViveSR] Please put the model folder in the assigned path: " + ModelPath + "/model");
                            if (result != (int)Error.WORK) { Debug.LogWarning("[ViveSR] AI_VISION StartModule Error " + result); return result; }
                        }

                        result = ViveSR_Framework.StartModule(ViveSR_Framework.MODULE_ID_RIGID_RECONSTRUCTION);
                        if (result != (int)Error.WORK) { Debug.LogWarning("[ViveSR] RIGID_RECONSTRUCTION StartModule Error " + result); return result; }

                    }
                }
            }
            #endregion Start Modules Flow

            #region Link Modules

            //result = ViveSR_Framework.ModuleLink(ViveSR_Framework.MODULE_ID_SEETHROUGH, ViveSR_Framework.MODULE_ID_DEPTH, (int)WorkLinkMethod.ACTIVE);
            //if (result != (int)Error.WORK) { Debug.LogWarning("[ViveSR] SEETHROUGH Link DEPTH Error " + result); return result; }

            if (EnableDepthModule && EnableReconstructionModule)
            {
                result = ViveSR_Framework.ModuleLink(ViveSR_Framework.MODULE_ID_DEPTH, ViveSR_Framework.MODULE_ID_RIGID_RECONSTRUCTION, (int)WorkLinkMethod.ACTIVE);
                if (result != (int)Error.WORK) { Debug.LogWarning("[ViveSR] DEPTH Link RIGID_RECONSTRUCTION Error " + result); return result; }
            }

            if (EnableReconstructionModule && EnableAIModule)
            {
                result = ViveSR_Framework.ModuleLink(ViveSR_Framework.MODULE_ID_RIGID_RECONSTRUCTION, ViveSR_Framework.MODULE_ID_AI_VISION, (int)WorkLinkMethod.ACTIVE);
                if (result != (int)Error.WORK) { Debug.LogWarning("[ViveSR] RIGID_RECONSTRUCTION Link AI_SCENE Error " + result); return result; }
            }
            #endregion Link Modules

            return result;
        }

        protected virtual int ViveSR_StopFramework()
        {
            return ViveSR_Framework.Stop();
        }
    }
}