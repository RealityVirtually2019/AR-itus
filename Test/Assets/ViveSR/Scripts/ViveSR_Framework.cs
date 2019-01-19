using System.Runtime.InteropServices;

namespace Vive.Plugin.SR
{
    public static class ViveSR_Framework
    {
        [DllImport("ViveSR_API")] private static extern int ViveSR_Initial();
        [DllImport("ViveSR_API")] private static extern int ViveSR_Stop();

        [DllImport("ViveSR_API")] private static extern int ViveSR_CreateModule(int ModuleType, ref int moduleID);
        [DllImport("ViveSR_API")] private static extern int ViveSR_StartModule(int moduleID);
        [DllImport("ViveSR_API")] private static extern int ViveSR_ModuleLink(int moduleIDfrom, int moduleIDto, int mode);

        [DllImport("ViveSR_API")] private static extern int ViveSR_GetMultiDataSize(int moduleID, DataInfo[] data, int size);
        [DllImport("ViveSR_API")] private static extern int ViveSR_GetMultiData(int moduleID, DataInfo[] data, int size);

        [DllImport("ViveSR_API")] private static extern int ViveSR_RegisterCallback(int moduleID, int type, System.IntPtr callback);
        [DllImport("ViveSR_API")] private static extern int ViveSR_UnregisterCallback(int moduleID, int type, System.IntPtr callback);

        [DllImport("ViveSR_API")] private static extern int ViveSR_GetParameterBool(int moduleID, int type, ref bool parameter);
        [DllImport("ViveSR_API")] private static extern int ViveSR_SetParameterBool(int moduleID, int type, bool parameter);
        [DllImport("ViveSR_API")] private static extern int ViveSR_GetParameterInt(int moduleID, int type, ref int parameter);
        [DllImport("ViveSR_API")] private static extern int ViveSR_SetParameterInt(int moduleID, int type, int parameter);
        [DllImport("ViveSR_API")] private static extern int ViveSR_GetParameterFloat(int moduleID, int type, ref float parameter);
        [DllImport("ViveSR_API")] private static extern int ViveSR_SetParameterFloat(int moduleID, int type, float parameter);
        [DllImport("ViveSR_API")] private static extern int ViveSR_GetParameterDouble(int moduleID, int type, ref double parameter);
        [DllImport("ViveSR_API")] private static extern int ViveSR_SetParameterDouble(int moduleID, int type, double parameter);
        [DllImport("ViveSR_API")] private static extern int ViveSR_GetParameterString(int moduleID, int type, ref string parameter);
        [DllImport("ViveSR_API")] private static extern int ViveSR_SetParameterString(int moduleID, int type, string parameter);
        [DllImport("ViveSR_API")] private static extern int ViveSR_GetParameterStruct(int moduleID, int type, ref System.IntPtr parameter);
        [DllImport("ViveSR_API")] private static extern int ViveSR_SetParameterStruct(int moduleID, int type, System.IntPtr parameter);
        [DllImport("ViveSR_API")] private static extern int ViveSR_GetParameterNativePtr(int moduleID, int type, ref System.IntPtr parameter);
        [DllImport("ViveSR_API")] private static extern int ViveSR_SetParameterNativePtr(int moduleID, int type, System.IntPtr parameter);
        [DllImport("ViveSR_API")] private static extern int ViveSR_GetParameterFloatArray(int moduleID, int type, ref float[] parameter);
        [DllImport("ViveSR_API")] private static extern int ViveSR_SetParameterFloatArray(int moduleID, int type, float[] parameter);

        [DllImport("ViveSR_API")] private static extern int ViveSR_SetCommandBool(int moduleID, int type, bool content);
        [DllImport("ViveSR_API")] private static extern int ViveSR_SetCommandInt(int moduleID, int type, int content);
        [DllImport("ViveSR_API")] private static extern int ViveSR_SetCommandFloat(int moduleID, int type, float content);
        [DllImport("ViveSR_API")] private static extern int ViveSR_SetCommandString(int moduleID, int type, string content);
        [DllImport("ViveSR_API")] private static extern int ViveSR_SetCommandFloat3(int moduleID, int type, float content0, float content1, float content2);
        [DllImport("ViveSR_API")] private static extern int ViveSR_ChangeModuleLinkStatus(int from, int to, int mode);

        [DllImport("ViveSR_API")] private static extern int ViveSR_GetPointer(int key, int type, ref System.IntPtr ptr);
        [DllImport("ViveSR_API")] private static extern int ViveSR_SetLogLevel(int level);

        [DllImport("ViveSR_API")] public static extern void UnityRenderEvent(int eventID);

        public delegate void CallbackBasic(int key);
        public static int MODULE_ID_SEETHROUGH;
        public static int MODULE_ID_DEPTH;
        public static int MODULE_ID_RIGID_RECONSTRUCTION;
        public static int MODULE_ID_HUMAN_DETECTION;
        public static int MODULE_ID_AI_VISION;

        public static int Initial()
        {
            return ViveSR_Initial();
        }

        public static int Stop()
        {
            return ViveSR_Stop();
        }

        public static int CreateModule(int moduleType, ref int moduleID)
        {
            return ViveSR_CreateModule(moduleType, ref moduleID);
        }

        public static int StartModule(int moduleID)
        {
            int result = ViveSR_StartModule(moduleID);
            return result;
        }

        public static int ModuleLink(int moduleIDfrom, int moduleIDto, int mode)
        {
            return ViveSR_ModuleLink(moduleIDfrom, moduleIDto, mode);
        }

        public static int GetMultiDataSize(int moduleID, DataInfo[] data, int size)
        {
            return ViveSR_GetMultiDataSize(moduleID, data, size);
        }

        public static int GetMultiData(int moduleID, DataInfo[] data, int size)
        {
            return ViveSR_GetMultiData(moduleID, data, size);
        }

        public static int RegisterCallback(int moduleID, int type, System.IntPtr callback)
        {
            return ViveSR_RegisterCallback(moduleID, type, callback);
        }

        public static int UnregisterCallback(int moduleID, int type, System.IntPtr callback)
        {
            return ViveSR_UnregisterCallback(moduleID, type, callback);
        }

        public static int GetParameterBool(int moduleID, int type, ref bool parameter)
        {
            return ViveSR_GetParameterBool(moduleID, type, ref parameter);
        }
        public static int SetParameterBool(int moduleID, int type, bool parameter)
        {
            return ViveSR_SetParameterBool(moduleID, type, parameter);
        }

        public static int GetParameterInt(int moduleID, int type, ref int parameter)
        {
            return ViveSR_GetParameterInt(moduleID, type, ref parameter);
        }
        public static int SetParameterInt(int moduleID, int type, int parameter)
        {
            return ViveSR_SetParameterInt(moduleID, type, parameter);
        }

        public static int GetParameterFloat(int moduleID, int type, ref float parameter)
        {
            return ViveSR_GetParameterFloat(moduleID, type, ref parameter);
        }
        public static int SetParameterFloat(int moduleID, int type, float parameter)
        {
            return ViveSR_SetParameterFloat(moduleID, type, parameter);
        }

        public static int GetParameterDouble(int moduleID, int type, ref double parameter)
        {
            return ViveSR_GetParameterDouble(moduleID, type, ref parameter);
        }
        public static int SetParameterDouble(int moduleID, int type, double parameter)
        {
            return ViveSR_SetParameterDouble(moduleID, type, parameter);
        }

        public static int SetParameterString(int moduleID, int type, string parameter)
        {
            return ViveSR_SetParameterString(moduleID, type, parameter);
        }

        public static int GetParameterStruct(int moduleID, int type, ref System.IntPtr parameter)
        {
            return ViveSR_GetParameterStruct(moduleID, type, ref parameter);
        }
        public static int SetParameterStruct(int moduleID, int type, System.IntPtr parameter)
        {
            return ViveSR_SetParameterStruct(moduleID, type, parameter);
        }
        public static int GetParameterStruct<T>(int moduleID, int type, ref T parameter)
        {
            System.IntPtr pointer = Marshal.AllocCoTaskMem(Marshal.SizeOf(parameter));
            int result = ViveSR_GetParameterStruct(moduleID, type, ref pointer);
            parameter = (T)Marshal.PtrToStructure(pointer, typeof(T));
            Marshal.FreeCoTaskMem(pointer);
            return result;
        }
        public static int SetParameterStruct<T>(int moduleID, int type, T parameter)
        {
            System.IntPtr pointer = Marshal.AllocCoTaskMem(Marshal.SizeOf(parameter));
            Marshal.StructureToPtr(parameter, pointer, true);
            int result = SetParameterStruct(moduleID, type, pointer);
            Marshal.FreeCoTaskMem(pointer);
            return result;
        }

        public static int GetParameterNativePtr(int moduleID, int type, ref System.IntPtr parameter)
        {
            return ViveSR_GetParameterNativePtr(moduleID, type, ref parameter);
        }
        public static int SetParameterNativePtr(int moduleID, int type, System.IntPtr parameter)
        {
            return ViveSR_SetParameterNativePtr(moduleID, type, parameter);
        }

        public static int GetParameterFloatArray(int moduleID, int type, ref float[] parameter)
        {
            return ViveSR_GetParameterFloatArray(moduleID, type, ref parameter);
        }
        public static int SetParameterFloatArray(int moduleID, int type, float[] parameter)
        {
            return ViveSR_SetParameterFloatArray(moduleID, type, parameter);
        }

        public static int SetCommandBool(int moduleID, int type, bool content)
        {
            return ViveSR_SetCommandBool(moduleID, type, content);
        }
        public static int SetCommandInt(int moduleID, int type, int content)
        {
            return ViveSR_SetCommandInt(moduleID, type, content);
        }
        public static int SetCommandFloat(int moduleID, int type, float content)
        {
            return ViveSR_SetCommandFloat(moduleID, type, content);
        }
        public static int SetCommandString(int moduleID, int type, string content)
        {
            return ViveSR_SetCommandString(moduleID, type, content);
        }
        public static int SetCommandFloat3(int moduleID, int type, float content0, float content1, float content2)
        {
            return ViveSR_SetCommandFloat3(moduleID, type, content0, content1, content2);
        }
        public static int ChangeModuleLinkStatus(int from, int to, int mode)
        {
            return ViveSR_ChangeModuleLinkStatus(from, to, mode);
        }

        public static int GetPointer(int key, int type, ref System.IntPtr ptr)
        {
            return ViveSR_GetPointer(key, type, ref ptr);
        }
        public static int SetLogLevel(int level)
        {
            return ViveSR_SetLogLevel(level);
        }

        public static DataInfo[] CreateDataInfo(System.IntPtr[] ptr)
        {
            DataInfo[] dataInfos = new DataInfo[ptr.Length];
            for (int i = 0; i < dataInfos.Length; i++)
            {
                dataInfos[i].mask = i;
                dataInfos[i].ptr = ptr[i];
            }
            return dataInfos;
        }
    }
}