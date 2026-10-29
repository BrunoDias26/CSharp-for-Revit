//System
using System.Reflection;

// Autodesk
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Events;
using CodeLab.General;


// CodeLab
using CodeLab.Utilities;
using Nice3point.Revit.Extensions.UI;
using AvailabilityNames = CodeLab.General.Availability.AvailabilityNames;


// This application belongs to the root namespace
namespace CodeLab
{
    // Implementing the interface for application
    public class Application : IExternalApplication
    {
        // Make a private uiCltApp
        private static UIControlledApplication _uiCltApp;

        // This will run on startup
        public Result OnStartup(UIControlledApplication uiCtrlApp)
        {
            // Store _uiCltApp, register on idling
            _uiCltApp = uiCtrlApp;

            try 
            {
                _uiCltApp.Idling += RegisterUiApp;
            }
            catch 
            {
                Globals.UiApp = null;
                Globals.UsernameRevit = null;

            }

            // Registering Globals
            Globals.RegisterProperties(uiCtrlApp);
            Globals.RegisterTooltips("CodeLab.Resources.Files.Tooltips");


            // Code logic here
            var Tab = Ribbon_Utilities.AddRibbonTab(uiCtrlApp, Globals.AddinName);

            // Create dimension panel (Bruno's own tools come first, on the left)
            var panelDimension = Ribbon_Utilities.AddRibbonPanelToTab(uiCtrlApp, Globals.AddinName, "Dimension");

            // Pipe from Grid directly on the panel (large icon, no pulldown)
            Ribbon_Utilities.AddPushButtonToPanel(panelDimension, "Pipe from Grid", "CodeLab.Commands.General.PipeFromGrid_Button", availability: AvailabilityNames.Project);

            // Create course panel: everything below came from the course lessons and will be removed later
            var panelCourse = Ribbon_Utilities.AddRibbonPanelToTab(uiCtrlApp, Globals.AddinName, "Course");

            // Course: test buttons
            var Button = Ribbon_Utilities.AddPushButtonToPanel(panelCourse, "Test Button", "CodeLab.Commands.General.Command_Button",availability: AvailabilityNames.ZeroDoc);

            var pulldown = Ribbon_Utilities.AddPulldownButtonToPanel(panelCourse, "Test Pulldown", "CodeLab.Commands.General.Command_Pulldown");
            var pulldownBtn1 = Ribbon_Utilities.AddPushButtonToPulldown(pulldown, "Pulldown Button 1", "CodeLab.Commands.General.Command_1Button");
            var pulldownBtn2 = Ribbon_Utilities.AddPushButtonToPulldown(pulldown, "Pulldown Button 2", "CodeLab.Commands.General.Command_2Button");
            var pulldownBtn3 = Ribbon_Utilities.AddPushButtonToPulldown(pulldown, "Pulldown Button 3", "CodeLab.Commands.General.Command_3Button");

            var stack1Data = Ribbon_Utilities.AddPulldownButtonData("Stack 1", "CodeLab.Commands.General.Command_Stack1");
            var stack2Data = Ribbon_Utilities.AddPulldownButtonData("Stack 2", "CodeLab.Commands.General.Command_Stack2");
            var stack3Data = Ribbon_Utilities.AddPulldownButtonData("Stack 3", "CodeLab.Commands.General.Command_Stack3");

            var stack = panelCourse.AddStackedItems(stack1Data, stack2Data, stack3Data);
            var pulldownStack1 = stack[0] as PulldownButton;
            var pulldownStack2 = stack[1] as PulldownButton;
            var pulldownStack3 = stack[2] as PulldownButton;

            Ribbon_Utilities.AddPushButtonToPulldown(pulldownStack1, "Button 1", "CodeLab.Commands.General.Command_Stack1");
            Ribbon_Utilities.AddPushButtonToPulldown(pulldownStack2, "Button 2", "CodeLab.Commands.General.Command_Stack2");
            Ribbon_Utilities.AddPushButtonToPulldown(pulldownStack3, "Button 3", "CodeLab.Commands.General.Command_Stack3");

            var splitButton = Ribbon_Utilities.AddSplitButtonToPanel(panelCourse, "Split 1");
            var splitbutton1 = Ribbon_Utilities.AddPushButtonToPulldown(splitButton, "Split 1", "CodeLab.Commands.General.Command_Split1");
            var splitbutton2 = Ribbon_Utilities.AddPushButtonToPulldown(splitButton, "Split 2", "CodeLab.Commands.General.Command_Split2");
            var splitbutton3 = Ribbon_Utilities.AddPushButtonToPulldown(splitButton, "Split 3", "CodeLab.Commands.General.Command_Split3");

            // Course: a separator line between the test buttons and the lesson tools
            panelCourse.AddSeparator();

            // Add revision pulldown
            var pulldownRevision = Ribbon_Utilities.AddPulldownButtonToPanel(panelCourse,"Revision", "CodeLab.Commands.General.Revision_Button");

            // Add pushbuttons to revision
            Ribbon_Utilities.AddPushButtonToPulldown(pulldownRevision, "Revision1", "CodeLab.Commands.General.Revision1_Button",availability: AvailabilityNames.Project);

            // Add worksets pulldown
            var pulldownWorkset = Ribbon_Utilities.AddPulldownButtonToPanel(panelCourse,"Workset", "CodeLab.Commands.General.Workset_Button");

            // Add pushbuttons to workset
            Ribbon_Utilities.AddPushButtonToPulldown(pulldownWorkset, "Create workset(s)", "CodeLab.Commands.General.Workset1_Button", availability: AvailabilityNames.Workshared);

            // Add selection pulldown
            var pulldownSelection = Ribbon_Utilities.AddPulldownButtonToPanel(panelCourse,"Select", "CodeLab.Commands.General.Select_Button");

            // Add pushbuttons to selection
            Ribbon_Utilities.AddPushButtonToPulldown(pulldownSelection, "Select titleblocks on sheets", "CodeLab.Commands.General.Select1_Button", availability: AvailabilityNames.Selection_SheetsOnly);
            Ribbon_Utilities.AddPushButtonToPulldown(pulldownSelection, "Select rooms", "CodeLab.Commands.General.Select2_Button", availability: AvailabilityNames.Project);
            Ribbon_Utilities.AddPushButtonToPulldown(pulldownSelection, "Select hidden elements", "CodeLab.Commands.General.Select3_Button", availability: AvailabilityNames.Project);

            // Add import pulldown
            var pulldownImport = Ribbon_Utilities.AddPulldownButtonToPanel(panelCourse,"Import", "CodeLab.Commands.General.Import_Button");

            // Add pushbuttons to import
            Ribbon_Utilities.AddPushButtonToPulldown(pulldownImport, "Import sheets from Excel", "CodeLab.Commands.General.Import1_Button", availability: AvailabilityNames.Project);

            // Add export pulldown
            var pulldownExport = Ribbon_Utilities.AddPulldownButtonToPanel(panelCourse,"Export", "CodeLab.Commands.General.Export_Button");

            // Add pushbuttons to export
            Ribbon_Utilities.AddPushButtonToPulldown(pulldownExport, "Export sheets to PDF", "CodeLab.Commands.General.Export1_Button", availability: AvailabilityNames.Project);

            // Testing button (directly on the panel, no pulldown)
            Ribbon_Utilities.AddPushButtonToPanel(panelCourse, "Testing", "CodeLab.Commands.General.Testing_Button", availability: AvailabilityNames.Project);

            return Result.Succeeded;


        }

        // This will run on shutdown
        public Result OnShutdown(UIControlledApplication uiCtrlApp)
        {
            return Result.Succeeded;
        }

        // On idling, register UiApp/UserName
        private static void RegisterUiApp(object sender, IdlingEventArgs e)
        {
            _uiCltApp.Idling -= RegisterUiApp;

            if (sender is UIApplication uiApp)
            {
                Globals.UiApp = uiApp;
                Globals.UsernameRevit = uiApp.Application.Username;
            }
             
        }

    }

}