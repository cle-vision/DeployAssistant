using SimpleBinaryVCS.DataComponent;
using SimpleBinaryVCS.Model;
using SimpleBinaryVCS.Utils;
using System.Windows.Input;

namespace SimpleBinaryVCS.ViewModel
{
    public class VersionDiffViewModel(ProjectData srcProject, ProjectData dstProject, List<ChangedFile> diff) : ViewModelBase
    {
        private ProjectData? _srcProject = srcProject;
        public ProjectData? SrcProject
        {
            get => _srcProject;
            set
            {
                _srcProject = value;
                OnPropertyChanged(nameof(SrcProject));
            }
        }

        private ProjectData? _dstProject = dstProject;
        public ProjectData? DstProject
        {
            get => _dstProject;
            set
            {
                _dstProject = value;
                OnPropertyChanged(nameof(DstProject));
            }
        }

        private List<ChangedFile>? _diff = diff;
        public List<ChangedFile> Diff
        {
            get => _diff ??= [];
            set
            {
                _diff = value;
                OnPropertyChanged(nameof(Diff));
            }
        }

        private readonly MetaDataManager _metaDataManager = App.MetaDataManager;
        private ICommand? exportDiffFiles;
        public ICommand ExportDiffFiles => exportDiffFiles ??= new RelayCommand(ExportDiff, CanExportDiff);
        private bool CanExportDiff(object obj)
        {
            if (Diff.Count <= 0) return false;
            return true; 
        }
        private void ExportDiff(object obj)
        {
            //_metaDataManager.RequestExportProjectVersionDiffFiles(Diff);
        }
    }
}