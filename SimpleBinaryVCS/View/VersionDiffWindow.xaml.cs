using SimpleBinaryVCS.Model;
using SimpleBinaryVCS.ViewModel;
using System.Windows;
using System.Windows.Controls;

namespace SimpleBinaryVCS.View
{
    /// <summary>
    /// Interaction logic for VersionDiffWindow.xaml
    /// </summary>
    public partial class VersionDiffWindow : Window
    {
        
        public VersionDiffWindow(ProjectData srcProject, ProjectData dstProject, List<ChangedFile> diff)
        {
            InitializeComponent();
            VersionDiffViewModel versionDiffVM = new (srcProject, dstProject, diff);
            DataContext = versionDiffVM;
        }

        private void FileFilterKeyword_TextChanged(object sender, TextChangedEventArgs e)
        {
            DiffItemsList.Items.Filter = FilterFilesMethod;
        }

        private bool FilterFilesMethod(object obj)
        {
            var file = (ProjectFile)obj;

            return file.DataName.Contains(FilterDiffInput.Text, StringComparison.OrdinalIgnoreCase);
        }
    }
}
