using SimpleBinaryVCS.Model;
using SimpleBinaryVCS.ViewModel;
using System.Collections.ObjectModel;
using System.Windows;

namespace SimpleBinaryVCS.View
{
    /// <summary>
    /// Interaction logic for IntegrityLogWindow.xaml
    /// </summary>
    public partial class IntegrityLogWindow : Window
    {
        public IntegrityLogWindow(ProjectData projData, string versionLog, ObservableCollection<ProjectFile> fileList)
        {
            InitializeComponent();
            VersionCheckViewModel versionCheckViewModel = new (projData, versionLog, fileList);
            DataContext = versionCheckViewModel;
        }

        public IntegrityLogWindow(ProjectData projectData)
        {
            InitializeComponent();
            VersionCheckViewModel versionCheckViewModel = new (projectData);
            DataContext = versionCheckViewModel;
        }

        private void FileFilterKeyword_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            BackupFileList.Items.Filter = FilterFilesMethod;
        }
        private bool FilterFilesMethod(object obj)
        {
            var file = (ProjectFile)obj;

            return file.DataName.Contains(FileFilterKeyword.Text, StringComparison.OrdinalIgnoreCase);
        }
    }
}