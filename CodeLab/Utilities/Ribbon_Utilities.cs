using System.Diagnostics;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Autodesk.Revit.UI;
using CodeLab.General;

namespace CodeLab.Utilities
{
    public class Ribbon_Utilities
    {
        // Method to create a Tab

        /// <summary>
        /// Adds a ribbon tab with the specified name.
        /// Attempts to create the tab and returns a Revit `Result` value indicating success or failure.
        /// </summary>
        /// <param name="uiCtrlApp">The `UIControlledApplication` instance used to create the tab.</param>
        /// <param name="tabName">The desired name for the new ribbon tab.</param>
        /// <returns>`Result.Succeeded` if the tab was created; otherwise `Result.Failed`.</returns>
        public static Result AddRibbonTab(UIControlledApplication uiCtrlApp, string tabName)
        {
            try
            {
                uiCtrlApp.CreateRibbonTab(tabName);
                return Result.Succeeded;
            }

            catch
            {
                Debug.WriteLine($"Error: Could not create tab {tabName}");
                return Result.Failed;
            }
        }

        // Method to create a Panel

        /// <summary>
        /// Adds a ribbon panel to the specified ribbon tab.
        /// Creates the panel and returns the created `RibbonPanel` or `null` on failure.
        /// </summary>
        /// <param name="uiCtrlApp">The `UIControlledApplication` instance used to create the panel.</param>
        /// <param name="tabName">The name of the tab to which the panel will be added.</param>
        /// <param name="panelName">The desired name for the new panel.</param>
        /// <returns>The created `RibbonPanel` on success; otherwise `null`.</returns>
        public static RibbonPanel AddRibbonPanelToTab(UIControlledApplication uiCtrlApp, string tabName, string panelName)
        {
            try
            {
                var ribbonPanel = uiCtrlApp.CreateRibbonPanel(tabName, panelName);
            }
            catch
            {
                Debug.WriteLine($"Error: Could not create panel {panelName} in tab {tabName}");
                return null;
            }

            return GetRibbonPanelByName(uiCtrlApp, tabName, panelName);
        }

        // Method to get a panel
        public static RibbonPanel GetRibbonPanelByName(UIControlledApplication uiCtrolApp, string TabName, string PanelName)
        {
            var ribbonPanels = uiCtrolApp.GetRibbonPanels(TabName);

            foreach (var panel in ribbonPanels)
            {
                if (panel.Name == PanelName)
                {
                    return panel;
                }
            }

            return null;
        }


        /// <summary>
        /// Create a `PushButtonData` object.
        /// </summary>
        /// <param name="buttonName">The name the user sees</param>
        /// <param name="ClassName">The full class Name the button runs</param>
        /// <returns>PushButtonData object</returns>
        public static PushButtonData NewPushButtonData(string buttonName, string ClassName)
        {

            // Get our base name
            var BaseName = CommandToBaseName(ClassName);


            // Create a Data Object
            var pushButtonData = new PushButtonData(BaseName, buttonName, Globals.AssemblyPath, ClassName);

            // Set the values
            pushButtonData.ToolTip = LookupTooltip(BaseName);
            pushButtonData.LargeImage = GetIcon(BaseName, resolution: 32);
            pushButtonData.Image = GetIcon(BaseName, resolution: 32);
      
            return pushButtonData;
        }

        /// <summary>
        /// Create a `PulldownButtonData` object.
        /// </summary>
        /// <param name="buttonName">The name the user sees</param>
        /// <param name="ClassName">The full class Name the button runs</param>
        /// <returns>PulldownButtonData object</returns>

        public static PulldownButtonData AddPulldownButtonData(string buttonName, string ClassName)
        {

            // Get our base name
            var BaseName = CommandToBaseName(ClassName);


            // Create a Data Object
            var pulldownButtonData = new PulldownButtonData(BaseName, buttonName);

            // Set the values
            pulldownButtonData.ToolTip = LookupTooltip(BaseName);
            pulldownButtonData.LargeImage = GetIcon(BaseName, resolution: 32);
            pulldownButtonData.Image = GetIcon(BaseName, resolution: 32);

            return pulldownButtonData;
        }




        // Method to add button to panel

        /// <summary>
        /// Adds a push button to the specified ribbon panel.
        /// Creates a PushButtonData using the provided command class name,
        /// sets the button tooltip and icons from embedded resources, and returns the created PushButton.
        /// Returns null if the panel is null or if the push button could not be created.
        /// </summary>
        /// <param name="panel">The target `RibbonPanel` to which the push button will be added.</param>
        /// <param name="buttonName">The visible label for the button.</param>
        /// <param name="ClassName">The fully-qualified command class name (for example: `CodeLab.Commands.General.MyCommand`)</param>
        /// <param name="availability">Optional availability class name (use Availability.AvailabilityNames). If null, Revit's default is used.</param>
        /// <returns></returns>
        public static PushButton AddPushButtonToPanel(RibbonPanel panel, string buttonName, string ClassName, string availability = null)
        {
            if (panel is null)
            {
                Debug.WriteLine($"Error: Could not create {buttonName}");
                return null;
            }

            // Create a Data Object

            var pushButtonData = Ribbon_Utilities.NewPushButtonData(buttonName, ClassName);

            if (panel.AddItem(pushButtonData) is PushButton pushButton)
            {
                // Set availability
                if (availability is not null)
                {
                    pushButton.AvailabilityClassName = availability;
                }

                return pushButton;
            }

            else
            {
                Debug.WriteLine($"Error: Could not create {buttonName}");
                return null;
            }
        }

        /// <summary>
        /// Adds a pulldown button to the specified ribbon panel.
        /// Creates a `PulldownButtonData` using the provided class name (for the internal id),
        /// sets the pulldown tooltip and icons from embedded resources, and returns the created `PulldownButton`.
        /// Returns `null` if the panel is null or if the pulldown could not be created.
        /// </summary>
        /// <param name="panel">The target `RibbonPanel` to which the pulldown will be added.</param>
        /// <param name="buttonName">The visible label for the pulldown.</param>
        /// <param name="ClassName">The fully-qualified class name used to derive the pulldown internal name (for example: `CodeLab.Commands.General.Command_Pulldown`).</param>
        /// <returns>The created `PulldownButton` on success; otherwise `null`.</returns>
        public static PulldownButton AddPulldownButtonToPanel(RibbonPanel panel, string buttonName, string ClassName)
        {
            if (panel is null)
            {
                Debug.WriteLine($"Error: Could not create pulldown {buttonName}");
                return null;
            }

            // Create PulldownButtonData using helper (derives internal name, sets tooltip/images)
            var pulldownData = Ribbon_Utilities.AddPulldownButtonData(buttonName, ClassName);

            if (panel.AddItem(pulldownData) is PulldownButton pulldown)
            {
                return pulldown;
            }
            else
            {
                Debug.WriteLine($"Error: Could not create pulldown {buttonName}");
                return null;
            }
        }

        /// <summary>
        /// Creates and adds an empty SplitButton container to the specified ribbon panel.
        /// Generates a unique internal identifier automatically using the main button's name.
        /// </summary>
        /// <param name="panel">The ribbon panel where the split button will be added.</param>
        /// <param name="mainButtonName">The name of the primary command button, used as the base text and internal ID.</param>
        /// <returns>The created <see cref="SplitButton"/> if successful; otherwise, <see langword="null"/>.</returns>
        public static SplitButton AddSplitButtonToPanel(RibbonPanel panel, string mainButtonName)
        {
            if (panel is null) return null;

            // 1. Geramos o ID interno automaticamente concatenando um prefixo ao nome do botão
            string internalId = $"split_{mainButtonName.Replace(" ", "")}";

            // 2. O texto inicial do container passa a ser o próprio nome do botão principal
            var splitButtonData = new SplitButtonData(internalId, mainButtonName);

            if (panel.AddItem(splitButtonData) is SplitButton splitButton)
            {
                return splitButton;
            }
            return null;
        }
        /// <summary>
        /// Adds a push button to the specified `PulldownButton`.
        /// Creates a `PushButtonData` using the provided command class name, adds it to the pulldown,
        /// sets the button tooltip and icons from embedded resources, and returns the created `PushButton`.
        /// Returns `null` if the pulldown is null or if the push button could not be created.
        /// </summary>
        /// <param name="pulldownButton">The `PulldownButton` to which the new push button will be added.</param>
        /// <param name="buttonName">The visible label for the new push button.</param>
        /// <param name="className">The fully-qualified command class name (for example: `CodeLab.Commands.General.MyCommand`).</param>
        /// <param name="availability">Optional availability class name (use Availability.AvailabilityNames). If null, Revit's default is used.</param>
        /// <returns>The created `PushButton` on success; otherwise `null`.</returns>
        public static PushButton AddPushButtonToPulldown(PulldownButton pulldownButton, string buttonName, string className, string availability = null)
        {
            if (pulldownButton is null)
            {
                Debug.WriteLine($"Error: Pulldown is null. Could not create {buttonName}");
                return null;
            }

            // Build PushButtonData using helper (sets tooltip/images)
            var pushButtonData = Ribbon_Utilities.NewPushButtonData(buttonName, className);

            try
            {
                // Prefer AddPushButton when available on PulldownButton
                // Fall back to AddItem when necessary
                PushButton pushButton = null;

                // If PulldownButton exposes AddPushButton:
                // (Use reflection to call AddPushButton if the compile-time API differs in some Revit versions)
                var addPushMethod = pulldownButton.GetType().GetMethod("AddPushButton", new[] { typeof(PushButtonData) });
                if (addPushMethod is not null)
                {
                    var result = addPushMethod.Invoke(pulldownButton, new object[] { pushButtonData });
                    pushButton = result as PushButton;
                }
                else
                {
                    // fallback
                    if (pulldownButton.AddPushButton(pushButtonData) is PushButton pb)
                    {
                        pushButton = pb;
                    }
                }

                if (pushButton is not null)
                {
                    // Set availability
                    if (availability is not null)
                    {
                        pushButton.AvailabilityClassName = availability;
                    }

                    return pushButton;
                }
            }
            catch
            {
                Debug.WriteLine($"Error: Could not create {buttonName} in pulldown {pulldownButton.Name}");
            }

            return null;
        }

        // Method to get base name
        public static string CommandToBaseName(string commandName)
        {
            return commandName.Replace("CodeLab.Commands.General.", "");
        }


        // Method to get a value from a Dictionary Key

        public static string LookupTooltip(string key, string failValue = null)
        {

            failValue ??= "Tooltip not found";

            if (Globals.Tooltips.TryGetValue(key, out string value))
            {
                return value;
            }

            return failValue;

        }


        // Method to get an icon as an image source

        public static ImageSource GetIcon(string baseName, int resolution = 32)
        {
            var resourcePath = $"CodeLab.Resources.Icons{resolution}.{baseName}{resolution}.png";

            using (var stream = Globals.Assembly.GetManifestResourceStream(resourcePath))
            {

                if (stream is null) { return null; }

                var decoder = new PngBitmapDecoder(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.Default);

                if (decoder.Frames.Count > 0)
                {
                    return decoder.Frames.First();
                }
                else
                {
                    return null;
                }
            }
        }

    }
}

