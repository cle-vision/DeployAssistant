using DeployAssistant.Model;
using DeployAssistant.View;
using SimpleBinaryVCS.DataComponent;
using SimpleBinaryVCS.Model;
using SimpleBinaryVCS.Utils;
using SimpleBinaryVCS.View;
using System.Collections.ObjectModel;
using System.Windows.Input;
using WPF = System.Windows;
namespace SimpleBinaryVCS.ViewModel
{
    public enum VMState
    {
        Idle,
        Calculating
    }
    public class FileTrackViewModel : ViewModelBase
    {
        private ObservableCollection<ProjectFile>? _changedFileList; 
        public ObservableCollection<ProjectFile> ChangedFileList 
        {
            get => _changedFileList ??= [];
            set
            {
                _changedFileList = value;
                OnPropertyChanged("ChangedFileList");
            }
        }
        private ProjectFile? _srcProjectFile;
        public ProjectFile? SrcProjectFile
        {
            get => _srcProjectFile ??= null;
            set
            {
                _srcProjectFile = value;
                OnPropertyChanged("SelectedItem");
            }
        }

        private ProjectFile? _selectedItem; 
        public ProjectFile? SelectedItem
        {
            get => _selectedItem ??= null; 
            set
            {
                _selectedItem = value;
                OnPropertyChanged("SelectedItem"); 
            }
        }

        private ICommand? clearNewfiles;
        public ICommand ClearNewfiles => clearNewfiles ??= new RelayCommand(ClearFiles, CanClearFiles);

        private ICommand? refreshDeployFileList;
        public ICommand RefreshDeployFileList => refreshDeployFileList ??= new RelayCommand(RefreshFilesList, CanRefresh);

        private ICommand? revertChange;
        public ICommand RevertChange => revertChange ??= new RelayCommand(RevertIntegrityCheckFile);

        private ICommand? checkProjectIntegrity; 
        public ICommand CheckProjectIntegrity => checkProjectIntegrity ??= new RelayCommand(MainProjectIntegrityTest, CanRunIntegrityTest);

        private ICommand? stageChanges;
        public ICommand StageChanges => stageChanges ??= new RelayCommand(StageNewChanges, CanStageChanges);

        private ICommand? addForRestore;
        public ICommand AddForRestore => addForRestore ??= new RelayCommand(RestoreFile, CanRestoreFile);

        private ICommand? getDeployedProjectInfo;
        public ICommand? GetDeployedProjectInfo => getDeployedProjectInfo ??= new RelayCommand(OpenDeployedProjectInfo, CanOpenDeployedProjectInfo);

        private ICommand? compareDeployedProjectWithMain;
        public ICommand? CompareDeployedProjectWithMain => compareDeployedProjectWithMain ??= new RelayCommand(CompareSrcProjWithMain, CanCompareSrcProjWithMain);
        
        private ICommand? srcSimilarityWithBackups;
        public ICommand? SrcSimilarityWithBackups => srcSimilarityWithBackups ??= new RelayCommand(GetSimilaritiesWithLocal, CanCompareSrcProjWithMain);

        private void GetSimilaritiesWithLocal(object obj)
        {
            _metaDataManager.RequestProjectCompatibility(_srcProjectData); 
        }

        private bool CanCompareSrcProjWithMain(object obj)
        {
            if (_metaDataState != MetaDataState.Idle) return false;
            if (_srcProjectData ==  null) return false;
            return true; 
        }

        private void CompareSrcProjWithMain(object obj)
        {
            _metaDataManager.RequestProjVersionDiff(_srcProjectData);
        }

        private ICommand? getDeploySrcDir;
        public ICommand GetDeploySrcDir => getDeploySrcDir ??= new RelayCommand(SetDeploySrcDirectory, CanSetDeployDir);
        
        private readonly MetaDataManager _metaDataManager;
        private MetaDataState _metaDataState = MetaDataState.Idle;
        private ProjectData? _srcProjectData;
        private ProjectData? _dstProjData;
        string? _deploySrcPath;
        public FileTrackViewModel()
        {
            _metaDataManager = App.MetaDataManager;
            _metaDataManager.OverlappedFileSortEventHandler += OverlapFileSortCallBack; 
            _metaDataManager.SrcProjectLoadedEventHandler += SrcProjectDataCallBack;
            _metaDataManager.PreStagedChangesEventHandler += PreStagedChangesCallBack;
            _metaDataManager.IntegrityCheckCompleteEventHandler += ProjectIntegrityCheckCallBack;
            _metaDataManager.FileChangesEventHandler += MetaDataManager_FileChangeCallBack;
            _metaDataManager.ManagerStateEventHandler += MetaDataManager_IssueEventCallBack;
            _metaDataManager.ProjLoadedEventHandler += MetaDataManager_ProjLoadedCallBack;
            _metaDataManager.ProjComparisonCompleteEventHandler += MetaDataManager_ProjComparisonCompleteCallBack;
            _metaDataManager.SimilarityCheckCompleteEventHandler += MetaDataManager_SimilarityCheckCompleteCallBack;
        }

        private bool CanRefresh(object obj)
        {
            if (_metaDataState != MetaDataState.Idle) return false;
            if (_deploySrcPath == null) return false;
            return true; 
        }

        private bool CanSetDeployDir(object obj)
        {
            if (_metaDataState != MetaDataState.Idle) return false;
            if (_dstProjData == null) return false;
            return true;
        }
        private void SetDeploySrcDirectory(object obj)
        {
            try
            {
                var openUpdateDir = new FolderBrowserDialog();
                if (openUpdateDir.ShowDialog() == DialogResult.OK)
                {
                    _srcProjectData = null;
                    _deploySrcPath = openUpdateDir.SelectedPath;
                    _metaDataManager.RequestSrcDataRetrieval(_deploySrcPath);
                }
                else
                {
                    _deploySrcPath = null; 
                    openUpdateDir.Dispose();
                    return;
                }
                openUpdateDir.Dispose();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        
        private bool CanStageChanges(object obj) 
        {
            if (_metaDataState != MetaDataState.Idle) return false; 
            return ChangedFileList.Count != 0; 
        }
        private void StageNewChanges(object obj)
        {
            _metaDataManager.RequestStageChanges();
        }

        private bool CanOpenDeployedProjectInfo(object obj)
        {
            return _srcProjectData != null;
        }
        public void OpenDeployedProjectInfo(object obj)
        {
            var mainWindow = obj as WPF.Window;
            IntegrityLogWindow logWindow = new(_srcProjectData)
            {
                Owner = mainWindow,
                WindowStartupLocation = WPF.WindowStartupLocation.CenterOwner
            };
            logWindow.Show();
        }

        private bool CanClearFiles(object obj) 
        {
            if (_metaDataState != MetaDataState.Idle) return false;
            return ChangedFileList.Count != 0; 
        }
        private void ClearFiles(object obj)
        {
            _metaDataManager.RequestClearStagedFiles();
        }
        
        private bool CanRestoreFile(object? obj)
        {
            if (_metaDataState != MetaDataState.Idle) return false;
            if (obj is not ProjectFile projFile) return false;

            if ((projFile.DataState & DataState.Deleted) != 0) return true;
            if (((projFile.DataState & DataState.Added) == 0) && !projFile.IsDstFile) return true;

            return false; 
        }

        private void RestoreFile(object? obj)
        {
            if (obj is ProjectFile file)
            {
                _metaDataManager.RequestFileRestore(file, DataState.Restored);
            }
        }
        
        private void RefreshFilesList(object? obj)
        {
            
            _metaDataManager.RequestClearStagedFiles();
            _metaDataManager.RequestSrcDataRetrieval(_deploySrcPath);
        }

        private void RevertIntegrityCheckFile(object? obj)
        {
            if (_selectedItem is ProjectFile file)
            {
                if ((file.DataState & DataState.IntegrityChecked) == 0)
                {
                    MessageBox.Show("Only Applicable for Integrity Check Failed Files");
                    return;
                }
                _metaDataManager.RequestRevertChange(file);
            }
        }

        private bool CanRunIntegrityTest(object Sender)
        {
            return _metaDataState == MetaDataState.Idle; 
        }
        private void MainProjectIntegrityTest(object sender)
        {
            Task.Run(() => _metaDataManager.RequestProjectIntegrityCheck());
        }

        #region Receive Callback From Model 
        private void MetaDataManager_ProjLoadedCallBack(object projObj)
        {
            if (projObj is not ProjectData projectData) return;
            _dstProjData = projectData;
        }

        private void OverlapFileSortCallBack(List<ChangedFile> overlappedFileObj, List<ChangedFile> newFileObj)
        {
            OverlapFileWindow overlapFileWindow = new(overlappedFileObj, newFileObj)
            {
                Owner = WPF.Application.Current.MainWindow,
                WindowStartupLocation = WPF.WindowStartupLocation.CenterOwner
            };
            overlapFileWindow.Show();
        }

        private void PreStagedChangesCallBack(object changedFileList)
        {
            if (changedFileList is ObservableCollection<ProjectFile> projectFileList)
            {
                ChangedFileList = projectFileList;
            }
        }

        private void MetaDataManager_FileChangeCallBack(ObservableCollection<ProjectFile> changedFileList)
        {
            ChangedFileList = changedFileList;
        }

        private void ProjectIntegrityCheckCallBack(string changeLog, ObservableCollection<ProjectFile> changedFileList)
        {
            if (changedFileList == null) { MessageBox.Show("Model Binding Issue: ChangedList is Empty"); return; }

            WPF.Application.Current.Dispatcher.Invoke((Delegate)(() =>
            {
                var mainWindow = WPF.Application.Current.MainWindow;
                IntegrityLogWindow logWindow = new (_dstProjData, changeLog, changedFileList)
                {
                    Owner = mainWindow,
                    WindowStartupLocation = WPF.WindowStartupLocation.CenterOwner
                };
                logWindow.Show();
            }));
        }

        private void MetaDataManager_ProjComparisonCompleteCallBack(ProjectData srcProject, ProjectData dstProject, List<ChangedFile> diff)
        {
            WPF.Application.Current.Dispatcher.Invoke(() =>
            {
                var mainWindow = WPF.Application.Current.MainWindow;
                VersionDiffWindow versionDiffWindow = new(srcProject, dstProject, diff)
                {
                    Owner = mainWindow,
                    WindowStartupLocation = WPF.WindowStartupLocation.CenterOwner
                };
                versionDiffWindow.Show();
            });
        }

        private void MetaDataManager_IssueEventCallBack(MetaDataState state)
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                _metaDataState = state;
                ((MainWindow)System.Windows.Application.Current.MainWindow).UpdateLayout();
                
            });
        }

        private void MetaDataManager_SimilarityCheckCompleteCallBack(ProjectData data, List<ProjectSimilarity> diffList)
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                var mainWindow = App.Current.MainWindow;
                VersionComparisonWindow versionCompWindow = new(data, diffList)
                {
                    Owner = mainWindow,
                    WindowStartupLocation = WPF.WindowStartupLocation.CenterOwner
                };
                versionCompWindow.Show();
            });
        }

        private void SrcProjectDataCallBack(object? srcProjectDataObj)
        {
            if (srcProjectDataObj is not ProjectData srcProjectData)
            {
                _srcProjectData = null;
                return;
            }
            else
            {
                _srcProjectData = srcProjectData;
            }
        }
        #endregion
    }
}
#region Deprecated 


#endregion