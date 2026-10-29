using System.Collections;
using System.Globalization;
using System.Net.NetworkInformation;
using System.Resources;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.UI;
using Assembly = System.Reflection.Assembly;

namespace CodeLab.General
{
    public static class Globals
    {
        #region Properties

        // Applications
        public static UIControlledApplication UiCtrlApp { get; set; }
        public static ControlledApplication CtlApp { get; set; }
        public static UIApplication UiApp { get; set; }

        // Assembly
        public static Assembly Assembly { get; set; }
        public static string AssemblyPath { get; set; }

        // Revit Version
        public static string RevitVersion { get; set; }
        public static int RevitVersionInt { get; set; }

        // User Names
        public static string UsernameRevit { get; set; }
        public static string UsernameWindows { get; set; }

        // Guid and versioning
        public static string AddinVersionNumber { get; set; }
        public static string AddinVersionName { get; set; }
        public static string AddinName { get; set; }
        public static string AddinGuid { get; set; }

        // Dictionaries for resources

        public static Dictionary<string, string> Tooltips { get; set; } = new Dictionary<string, string>();

        #endregion

        #region Methods

        // Method to register properties    
        public static void RegisterProperties(UIControlledApplication uiCtrlApp)
        {
            UiCtrlApp = uiCtrlApp;
            CtlApp = uiCtrlApp.ControlledApplication;

            // UiApp set on Adling

            Assembly = Assembly.GetExecutingAssembly();
            AssemblyPath = Assembly.Location;

            RevitVersion = CtlApp.VersionNumber; // Isso vai extrair apenas "2025"
            RevitVersionInt = int.Parse(RevitVersion); // Agora o Parse funciona liso!

            // Revit username set on adling
            UsernameWindows = Environment.UserName;

            // Version: read from the DLL, so it always matches <Version> in the .csproj (the single place to change it)
            var assemblyVersion = Assembly.GetName().Version;

            if (assemblyVersion is null)
            {
                AddinVersionNumber = "0.0.0";
            }
            else
            {
                // ToString(3) = "1.0.0" (major.minor.patch, without the 4th number)
                AddinVersionNumber = assemblyVersion.ToString(3);
            }

            AddinVersionName = "WIP";

            // Product name = Ribbon tab name = .addin <Name> = DLL name ("CodeLab")
            AddinName = "CodeLab";
            AddinGuid = "9699F8D5-7F3C-467F-AC8B-F1695DECC5BC";
        }



        // Register our Tooltips
        public static void RegisterTooltips(string resourcePath)
        {
            var resourceManager = new ResourceManager(resourcePath, typeof(Application).Assembly);
            var resourceSet = resourceManager.GetResourceSet(CultureInfo.CurrentUICulture, true, true);

            foreach (DictionaryEntry entry in resourceSet)
            {
                string key = entry.Key.ToString();
                string value = entry.Value.ToString();
                Tooltips[key] = value;
            }

        }

        #endregion

    }
}
