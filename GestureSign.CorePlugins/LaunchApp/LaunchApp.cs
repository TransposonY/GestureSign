using System;
using System.Collections.Generic;
using GestureSign.Common.Localization;
using GestureSign.Common.Plugins;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace GestureSign.CorePlugins.LaunchApp
{
    public class LaunchApp : IPlugin
    {
        #region Private Variables 

        private LaunchAppView _gui;
        private KeyValuePair<string, string> _appInfo;

        #endregion

        //[DllImport("shell32.dll", CharSet = CharSet.Unicode, PreserveSig = false)]
        //private static extern void SHCreateItemFromParsingName(
        //          [In] [MarshalAs(UnmanagedType.LPWStr)] string pszPath,
        //          [In] IntPtr pbc,
        //          [In] [MarshalAs(UnmanagedType.LPStruct)] Guid iIdIShellItem,
        //          [Out] [MarshalAs(UnmanagedType.Interface, IidParameterIndex = 2)] out IShellItem iShellItem);

        //[DllImport("shell32.dll", CharSet = CharSet.Unicode, PreserveSig = false)]
        //private static extern void SHCreateShellItemArrayFromShellItem(
        //    [In] [MarshalAs(UnmanagedType.Interface, IidParameterIndex = 2)] IShellItem psi,
        //    [In] [MarshalAs(UnmanagedType.LPStruct)] Guid iIdIShellItem,
        //    [Out] [MarshalAs(UnmanagedType.Interface, IidParameterIndex = 2)] out IShellItemArray iShellItemArray);

        #region PInvoke Declarations

        #endregion

        #region Public Properties

        /// <summary>
        /// key:AppUserModelId
        /// value:ApplicationName
        /// </summary>
        public KeyValuePair<string, string> AppInfo
        {
            get { return _appInfo; }
            set { _appInfo = value; }
        }

        public string Name => LocalizationProvider.Instance.GetTextValue("CorePlugins.LaunchApp.Name");

        public string Description => AppInfo.Value == null ? LocalizationProvider.Instance.GetTextValue("CorePlugins.LaunchApp.Name") :
            string.Format(LocalizationProvider.Instance.GetTextValue("CorePlugins.LaunchApp.Description"), AppInfo.Value);

        public object GUI => _gui ?? (_gui = CreateGui());

        public bool ActivateWindowDefault
        {
            get { return false; }
        }

        public string Category => LocalizationProvider.Instance.GetTextValue("CorePlugins.LaunchApp.Category");

        public bool IsAction => true;

        public object Icon => IconSource.Windows;

        #endregion

        #region Public Methods

        public void Initialize()
        {

        }

        public bool Gestured(PointInfo actionPoint)
        {
            if (AppInfo.Key == null) return false;
            ApplicationActivationManager appActiveManager = new ApplicationActivationManager();
            try
            {
                uint pid;
                appActiveManager.ActivateApplication(AppInfo.Key, null, ActivateOptions.None, out pid);
            }
            catch
            {
                // ignored
            }
            //IShellItemArray array = GetShellItemArray(@"C:\temp\somefile.xyz");
            //appActiveManager.ActivateForFile("2c123c17-8b21-4eb8-8b7f-fdc35c8b7718_n2533ggrncqjt!App", array, "Open",
            //    out pid);


            //Process explorer = new Process();
            //explorer.StartInfo.FileName = "explorer.exe";
            //explorer.StartInfo.Arguments = @"shell:AppsFolder\" + AppInfo.Key;
            //explorer.Start();

            return true;
        }

        public bool Deserialize(string serializedData)
        {
            return PluginHelper.DeserializeSettings(serializedData, out _appInfo);
        }

        public string Serialize()
        {
            if (_gui != null)
                _appInfo = _gui.SelectedAppInfo;

            return PluginHelper.SerializeSettings(AppInfo);
        }

        #endregion

        #region ApplicationActivationManager

        //http://stackoverflow.com/q/12925748

        public enum ActivateOptions
        {
            None = 0x00000000,  // No flags set
            DesignMode = 0x00000001,  // The application is being activated for design mode, and thus will not be able to
                                      // to create an immersive window. Window creation must be done by design tools which
                                      // load the necessary components by communicating with a designer-specified service on
                                      // the site chain established on the activation manager.  The splash screen normally
                                      // shown when an application is activated will also not appear.  Most activations
                                      // will not use this flag.
            NoErrorUI = 0x00000002,  // Do not show an error dialog if the app fails to activate.                                
            NoSplashScreen = 0x00000004,  // Do not show the splash screen when activating the app.
        }

        [ComImport, Guid("2e941141-7f97-4756-ba1d-9decde894a3d"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        interface IApplicationActivationManager
        {
            // Activates the specified immersive application for the "Launch" contract, passing the provided arguments
            // string into the application.  Callers can obtain the process Id of the application instance fulfilling this contract.
            IntPtr ActivateApplication([In] String appUserModelId, [In] String arguments, [In] ActivateOptions options, [Out] out UInt32 processId);

            IntPtr ActivateForFile([In] String appUserModelId,
                [In] [MarshalAs(UnmanagedType.Interface, IidParameterIndex = 2)] /*IShellItemArray* */ IShellItemArray
                    itemArray, [In] String verb, [Out] out UInt32 processId);
            IntPtr ActivateForProtocol([In] String appUserModelId, [In] IntPtr /* IShellItemArray* */itemArray, [Out] out UInt32 processId);
        }

        [ComImport, Guid("45BA127D-10A8-46EA-8AB7-56EA9078943C")]//Application Activation Manager
        class ApplicationActivationManager : IApplicationActivationManager
        {
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)/*, PreserveSig*/]
            public extern IntPtr ActivateApplication([In] String appUserModelId, [In] String arguments, [In] ActivateOptions options, [Out] out UInt32 processId);

            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            public extern IntPtr ActivateForFile([In] String appUserModelId,
                [In] [MarshalAs(UnmanagedType.Interface, IidParameterIndex = 2)] /*IShellItemArray* */ IShellItemArray
                    itemArray, [In] String verb, [Out] out UInt32 processId);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            public extern IntPtr ActivateForProtocol([In] String appUserModelId, [In] IntPtr /* IShellItemArray* */itemArray, [Out] out UInt32 processId);
        }

        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("43826d1e-e718-42ee-bc55-a1e261c37bfe")]
        interface IShellItem
        {
        }

        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("b63ea76d-1f85-456f-a19c-48159efa858b")]
        interface IShellItemArray
        {
        }

        #endregion

        #region Private Methods

        private LaunchAppView CreateGui()
        {
            LaunchAppView newGui = new LaunchAppView() { DataContext = this };
            return newGui;
        }
        //private static IShellItemArray GetShellItemArray(string sourceFile)
        //{
        //    IShellItem item = GetShellItem(sourceFile);
        //    IShellItemArray array = GetShellItemArray(item);

        //    return array;
        //}

        //private static IShellItem GetShellItem(string sourceFile)
        //{
        //    IShellItem iShellItem = null;
        //    Guid iIdIShellItem = new Guid("43826d1e-e718-42ee-bc55-a1e261c37bfe");
        //    SHCreateItemFromParsingName(sourceFile, IntPtr.Zero, iIdIShellItem, out iShellItem);

        //    return iShellItem;
        //}

        //private static IShellItemArray GetShellItemArray(IShellItem shellItem)
        //{
        //    IShellItemArray iShellItemArray = null;
        //    Guid iIdIShellItemArray = new Guid("b63ea76d-1f85-456f-a19c-48159efa858b");
        //    SHCreateShellItemArrayFromShellItem(shellItem, iIdIShellItemArray, out iShellItemArray);

        //    return iShellItemArray;
        //}


        #endregion

        #region Host Control

        public IHostControl HostControl { get; set; }

        #endregion
    }
}
