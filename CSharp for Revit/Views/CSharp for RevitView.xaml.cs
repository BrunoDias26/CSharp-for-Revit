using CSharp_for_Revit.ViewModels;

namespace CSharp_for_Revit.Views
{
    public sealed partial class CSharp_for_RevitView
    {
        public CSharp_for_RevitView(CSharp_for_RevitViewModel viewModel)
        {
            DataContext = viewModel;
            InitializeComponent();
        }
    }
}