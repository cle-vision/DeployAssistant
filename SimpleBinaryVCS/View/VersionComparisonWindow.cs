using DeployAssistant.Model;
using DeployAssistant.ViewModel;
using SimpleBinaryVCS.Model;
using System.Windows;

namespace DeployAssistant.View
{
    /// <summary>
    /// Interaction logic for CompatibleVersionWindow.xaml
    /// </summary>
    public partial class VersionComparisonWindow : Window
    {
        public VersionComparisonWindow(ProjectData srcData, List<ProjectSimilarity> similarities)
        {
            InitializeComponent();
            VersionCompatibilityViewModel vcVM = new(srcData, similarities);
            DataContext = vcVM;
        }
    }
}
