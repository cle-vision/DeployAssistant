using SimpleBinaryVCS.DataComponent;
using SimpleBinaryVCS.Model;
using SimpleBinaryVCS.Utils;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace SimpleBinaryVCS.ViewModel
{
    public class VersionCheckViewModel : ViewModelBase
    {
        private string? _changeLog; 
        public string ChangeLog
        {
            get { return _changeLog ??= ""; }
            set
            {
                _changeLog = value;
                OnPropertyChanged("ChangeLog");
            }
        }
        private string? _updateLog;
        public string UpdateLog
        {
            get { return _updateLog ??= ""; }
            set
            {
                _updateLog = value;
                OnPropertyChanged("UpdateLog");
            }
        }

        private ICommand? similaritiesWithLocal;
        public ICommand? SimilaritiesWithLocal => similaritiesWithLocal ??= new RelayCommand(GetSimilarities);

        private void GetSimilarities(object? obj)
        {
            _metaDataManager.RequestProjectCompatibility(_projectData); 
        }

        private ICommand? conductIntegrate;
        public ICommand? ConductIntegrate => conductIntegrate ??= new RelayCommand(IntegrateToLocal);

        private void IntegrateToLocal(object? obj)
        {
            _metaDataManager.RequestProjectIntegrate(null, null, null); 
        }

        private ICommand? exportToXLSX;
        public ICommand ExportToXLSX => exportToXLSX ??= new RelayCommand(ExportXLSX, CanExportXLSX);

        private void ExportXLSX(object obj)
        {
            _metaDataManager.RequestExportProjectFilesXLSX(FileList, _projectData); 
        }

        private bool CanExportXLSX(object obj)
        {
            return FileList.Count > 0; 
        }

        private ObservableCollection<ProjectFile>? fileList;
        public ObservableCollection<ProjectFile> FileList
        {
            get=> fileList ??= [];
            set
            {
                fileList = value;
                OnPropertyChanged("FileList");
            }
        }
        private Dictionary<string, object>? _projectDataReview;
        public Dictionary<string, object> ProjectDataReview
        {
            get => _projectDataReview ??= []; 
            set
            {
                _projectDataReview = value;
                OnPropertyChanged("ProjectDataDetail");
            }
        }

        private readonly MetaDataManager _metaDataManager;
        private readonly ProjectData _projectData; 
        public VersionCheckViewModel(ProjectData projectData, string versionLog, ObservableCollection<ProjectFile> fileList)
        {
            _metaDataManager = App.MetaDataManager;
            _projectData = projectData;
            _updateLog = "Integrity Checking";
            _changeLog = versionLog;
            this.fileList = fileList;
        }

        public VersionCheckViewModel(ProjectData projectData)
        {
            _metaDataManager = App.MetaDataManager;

            _projectData = projectData;
            _projectDataReview = [];
            _projectData.RegisterProjectInfo(ProjectDataReview);
            FileList = _projectData.ProjectFilesObs;
            ChangeLog = _projectData.ChangeLog ?? "Undefined";
            UpdateLog = _projectData.UpdateLog ?? "Undefined";
        }
    }
}