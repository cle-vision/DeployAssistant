using DeployAssistant.Model;
using DeployManager.DataComponent;
using SimpleBinaryVCS.Interfaces;
using SimpleBinaryVCS.Model;
using SimpleBinaryVCS.Utils;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace SimpleBinaryVCS.DataComponent
{
    
    public class MetaDataManager : IManager
    {
        public string? CurrentProjectPath {  get; set; }

        public event Action<ObservableCollection<ProjectFile>>? FileChangesEventHandler;
        public event Action<List<ChangedFile>, List<ChangedFile>>? OverlappedFileSortEventHandler;
        public event Action<string>? ProjExportEventHandler; 
        public event Action<object>? StagedChangesEventHandler;
        public event Action<object>? PreStagedChangesEventHandler;
        public event Action<object?>? SrcProjectLoadedEventHandler;
        public event Action<object>? ProjLoadedEventHandler;
        public event Action<object>? MetaDataLoadedEventHandler;
        public event Action<object>? FetchRequestEventHandler;
        public event Action<Dictionary<string, ProjectFile>>? SrcProjectFilesHashedEventHandler; 
        public event Action<string, ObservableCollection<ProjectFile>>? IntegrityCheckCompleteEventHandler;
        public event Action<ProjectData, ProjectData, List<ChangedFile>>? ProjComparisonCompleteEventHandler;
        public event Action<MetaDataState> ManagerStateEventHandler;
        public event Action<ProjectIgnoreData> UpdateIgnoreListEventHandler;
        public event Action<ProjectData, List<ProjectSimilarity>>? SimilarityCheckCompleteEventHandler;
        private MetaDataState _currentState;
        private DeployMode _currentDeployMode;
        private DeployMode _prevDeployMode; 
        public DeployMode CurrentDeployMode
        {
            get => _currentDeployMode; 
            set => _currentDeployMode = value;
        }
        public MetaDataState CurrentState
        {
            get => _currentState;
            set
            {
                _currentState = value;
                ManagerStateEventHandler?.Invoke(_currentState);
            }
        }

        private ProjectMetaData? _projectMetaData;
        public ProjectMetaData? ProjectMetaData
        {
            get => _projectMetaData;
            private set
            {
                if (value == null) throw new ArgumentNullException(nameof(ProjectMetaData));
                _projectMetaData = value;
                CurrentProjectPath = value.ProjectPath;
                MetaDataLoadedEventHandler?.Invoke(value);
            }
        }

        private ProjectData? _mainProjectData; 
        public ProjectData? MainProjectData 
        {
            get => _mainProjectData;
            private set
            {
                if (value == null || value is not ProjectData) throw new ArgumentNullException(nameof(_mainProjectData));
                else if (ProjectMetaData == null) throw new ArgumentNullException(nameof(ProjectMetaData));
                _mainProjectData = new ProjectData(value);
                ProjectMetaData.SetProjectMain(_mainProjectData);
                SrcProjectLoadedEventHandler?.Invoke(null); 
                ProjLoadedEventHandler?.Invoke(_mainProjectData);
            }
        }
        private ProjectData? _srcProjectData; 
        public ProjectData? NewestProjectData
        {
            get
            {
                if (ProjectMetaData == null) return null;
                if (ProjectMetaData.ProjectDataList.First == null) return null;
                return ProjectMetaData.ProjectDataList.First.Value;
            }
        }

        private FileManager _fileManager;
        private BackupManager _backupManager;
        private UpdateManager _updateManager;
        private ExportManager _exportManager;
        private SettingManager _settingManager;
        private FileHandlerTool _fileHandlerTool;
        private HashTool _hashTool; 

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public MetaDataManager()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        {
            _fileHandlerTool = App.FileHandlerTool;
            _hashTool = App.HashTool;
        }

        public void Awake()
        {
            _backupManager = new BackupManager();
            _fileManager = new FileManager();
            _updateManager = new UpdateManager();
            _exportManager = new ExportManager();
            _settingManager = new SettingManager();

            MetaDataLoadedEventHandler += _backupManager.MetaDataManager_MetaDataLoadedCallBack;
            MetaDataLoadedEventHandler += _fileManager.MetaDataManager_MetaDataLoadedCallBack;
            MetaDataLoadedEventHandler += _updateManager.MetaDataManager_MetaDataLoadedCallBack;
            MetaDataLoadedEventHandler += _exportManager.MetaDataManager_MetaDataLoadedCallBack;
            MetaDataLoadedEventHandler += _settingManager.MetaDataManager_MetaDataLoadedCallBack;

            ProjLoadedEventHandler += _backupManager.MetaDataManager_ProjLoadedCallback;
            ProjLoadedEventHandler += _fileManager.MetaDataManager_ProjLoadedCallback;
            ProjLoadedEventHandler += _updateManager.MetaDataManager_ProjLoadedCallback;

            SrcProjectLoadedEventHandler += _updateManager.MetaDataManager_SrcProjectLoadedCallBack;
            StagedChangesEventHandler += _updateManager.MetaDataManager_StagedChangesCallBack;
            UpdateIgnoreListEventHandler += _fileManager.MetaDataManager_UpdateIgnoreListCallBack;
            SrcProjectFilesHashedEventHandler += _updateManager.MetaDataManager_SrcFilesHashedCallBack; 

            _backupManager.ProjectRevertEventHandler += ProjectChangeCallBack;
            _backupManager.ManagerStateEventHandler += ManagerStateCallBack;
            _backupManager.FetchCompleteEventHandler += BackupManager_FetchCompleteCallBack;

            _updateManager.ProjectUpdateEventHandler += ProjectChangeCallBack;
            _updateManager.ManagerStateEventHandler += ManagerStateCallBack;
            _updateManager.ReportFileDifferences += UpdateManager_ReportFileDifferencesCallBack;

            _fileManager.ManagerStateEventHandler += ManagerStateCallBack;
            _fileManager.DataPreStagedEventHandler += FileManager_DataPreStagedCallBack;
            _fileManager.DataStagedEventHandler += FileManager_DataStagedCallBack;
            _fileManager.OverlappedFileFoundEventHandler += FileManager_OverlappedFileFoundCallBack; 
            _fileManager.IntegrityCheckEventHandler += FileManager_IntegrityCheckCallBack;
            _fileManager.SrcProjectDataLoadedEventHandler += FileManager_SrcProjectLoadedCallBack;
            _fileManager.SrcFileHashedEventHandler += FileManager_SrcFileHashedCallBack;
            
            _exportManager.ManagerStateEventHandler += ManagerStateCallBack;
            _exportManager.ExportCompleteEventHandler += ExportManager_ExportCompleteCallBack;

            _settingManager.SetPrevProjectEventHandler += SettingManager_SetLastDstProjectCallBack;
            _settingManager.UpdateIgnoreListEventHandler += SettingManager_UpdateIgnoreListCallBack;

            _backupManager.Awake();
            _updateManager.Awake();
            _updateManager.Awake();
            _settingManager.Awake();
        }

        private void FileManager_SrcFileHashedCallBack(Dictionary<string, ProjectFile> hashedDict)
        {
            SrcProjectFilesHashedEventHandler?.Invoke(hashedDict); 
        }

        #region View Model Request Calls
        public bool RequestProjectRetrieval(string projectPath)
        {
            string projectMetaDataPath = $"{projectPath}\\ProjectMetaData.bin";

            try
            {
                CurrentState = MetaDataState.Retrieving;
                _fileHandlerTool.TryDeserializeProjectMetaData(projectMetaDataPath, out ProjectMetaData? retrievedData);
                if (retrievedData != null)
                {
                    if (retrievedData.ProjectPath != projectPath)
                    {
                        retrievedData.ProjectPath = projectPath;
                        retrievedData.ReconfigureProjectFiles(projectPath);
                    }

                    retrievedData.ProjectPath = projectPath;
                    retrievedData.ReconfigureProjectFileNames();
                    ProjectMetaData = retrievedData;
                    MainProjectData = retrievedData.ProjectMain;

                    _settingManager.SetRecentDstDirectory(projectPath);
                }
                else
                {
                    CurrentState = MetaDataState.Idle;
                    return false;
                }
            }
            catch (Exception ex)
            {
                CurrentState = MetaDataState.Idle;
                MessageBox.Show($"MetaDataManager TryRetrieveProject Error {ex.Message}");
                return false;
            }
            CurrentState = MetaDataState.Idle;
            return true;
        }

        public async void RequestProjectInitialization(string projectPath)
        {
            try
            {
                CurrentState = MetaDataState.Initializing;
                StringBuilder changeLog = new StringBuilder();
                ProjectMetaData newProjectRepo = new ProjectMetaData(Path.GetFileName(projectPath), projectPath);
                ProjectIgnoreData newIgnoreData = new ProjectIgnoreData(projectPath);
                newIgnoreData.ConfigureDefaultIgnore(newProjectRepo.ProjectName); 

                var ignoringFilesAndDirsTask = Task.Run(() =>
                    newIgnoreData.GetIgnoreFilesAndDirPaths(projectPath, IgnoreType.Initialization));
                var getFilesTask = Task.Run(() => Directory.GetFiles(projectPath, "*", SearchOption.AllDirectories));
                var getDirsTask = Task.Run(() => Directory.GetDirectories(projectPath, "*", SearchOption.AllDirectories));

                string[]? newProjectFiles = await getFilesTask; 
                string[]? newProjectDirs = await getDirsTask;
                (List<string> excludingFiles, List<string> excludingDirs) = await ignoringFilesAndDirsTask;

                newProjectFiles = newProjectFiles.Except(excludingFiles).ToArray();
                newProjectDirs = newProjectDirs.Except(excludingDirs).ToArray();

                if (newProjectFiles == null || newProjectDirs == null)
                { 
                    MessageBox.Show("Couldn't Get Project Files (And Or) Directories To MetaDataManager"); 
                    return; 
                }

                ProjectData newProjectData = new(projectPath);
                newProjectData.ProjectName = Path.GetFileName(projectPath);
                newProjectData.ConductedPC = Environment.MachineName;
                newProjectData.UpdatedVersion = GetProjectVersionName(newProjectData, true);
                changeLog.AppendLine($"Project Initialized");

                var options = new ParallelOptions { MaxDegreeOfParallelism = 
                    Convert.ToInt32(Math.Ceiling((Environment.ProcessorCount * 0.75) * 1.0)) };
                ConcurrentDictionary<string, ProjectFile> tempDict = new(StringComparer.OrdinalIgnoreCase);
                Parallel.ForEach(newProjectFiles, options, filePath =>
                {
                    var fileVersionInfo = FileVersionInfo.GetVersionInfo(filePath);
                    ProjectFile newFile = new ProjectFile
                        (
                        ProjectDataType.File,
                        new FileInfo(filePath).Length,
                        fileVersionInfo.FileVersion,
                        newProjectData.UpdatedVersion,
                        DateTime.Now,
                        DataState.None,
                        Path.GetFileName(filePath),
                        projectPath,
                        Path.GetRelativePath(projectPath, filePath),
                        "",
                        true, 
                        fileVersionInfo.ProductVersion
                        );
                    tempDict.TryAdd(newFile.DataRelPath, newFile); // Create ProjectFile object
                    _hashTool.GetFileMD5CheckSum(newFile);
                });
                
                newProjectData.ProjectFiles = new Dictionary<string, ProjectFile>(tempDict);

                foreach (string dirPath in newProjectDirs)
                {
                    ProjectFile newFile = new ProjectFile
                        (
                        ProjectDataType.Directory,
                        0,
                        "",
                        newProjectData.UpdatedVersion,
                        DateTime.Now,
                        DataState.None,
                        Path.GetFileName(dirPath),
                        projectPath,
                        Path.GetRelativePath(projectPath, dirPath),
                        "",
                        true, 
                        ""
                        );
                    newProjectData.ProjectFiles.TryAdd(newFile.DataRelPath, newFile);
                    newProjectData.ChangedFiles.Add(new ChangedFile(new ProjectFile(newFile), DataState.Added));
                    changeLog.AppendLine($"Added {newFile.DataName}");
                }

                newProjectData.UpdatedTime = DateTime.Now;
                newProjectData.ChangeLog = changeLog.ToString();
                newProjectData.NumberOfChanges = newProjectData.ProjectFilesObs.Count;

                foreach (ProjectFile projFile in newProjectData.ProjectFiles.Values)
                {
                    newProjectData.ChangedFiles.Add(new ChangedFile(new ProjectFile(projFile), DataState.Added));
                }

                ProjectMetaData = newProjectRepo;
                MainProjectData = newProjectData;
                TryGenerateSupplementDirectories(projectPath, newProjectData.ProjectName);
                _settingManager.SetRecentDstDirectory(projectPath);

                CurrentState = MetaDataState.Idle;
            }
            catch (Exception ex)
            {
                CurrentState = MetaDataState.Idle;
                MessageBox.Show($"MetaDataManager Error InitializeProject {ex.Message}");
                return;
            }
        }

        public void RequestSrcDataRetrieval(string deployedPath)
        {
             _fileManager.RetrieveDataSrc(deployedPath);
        }

        public void RequestStagedFileListRefresh(string deployedPath)
        {

        }

        public bool RequestFetchBackup()
        {
            bool result = _backupManager.FetchBackupProjectList();
            if (!result) return false;
            return true;
        }

        public async void RequestRevertProject(ProjectData? targetProject)
        {
            if (targetProject == null)
            {
                MessageBox.Show("Invalid Request For Backup: Targeting Project is Null");
                return;
            }
            if (_currentDeployMode != DeployMode.Unsafe)
            {
                _currentDeployMode = DeployMode.IntegrityCheck;
                var result = await _fileManager.MainProjectIntegrityCheck();
                if (result == false)
                {
                    _prevDeployMode = DeployMode.Safe; 
                    MessageBox.Show("Integrity Check Failed!");
                    return;
                }
                else
                {
                    _currentDeployMode = DeployMode.Safe;
                }
            }
            List<ChangedFile>? fileDifferences = _fileManager.FindVersionDifferences(targetProject, MainProjectData, true);
            _backupManager.RevertProject(targetProject, fileDifferences);
        }

        public void RequestProjectCleanRestore(ProjectData? targetProject)
        {
            List<ChangedFile>? fileDifferences = _fileManager.ProjectIntegrityCheck(targetProject);
            _backupManager.RevertProject(targetProject, fileDifferences);
        }

        public void RequestRevertChange(ProjectFile file)
        {
            _fileManager.RevertChange(file);
        }

        public void RequestStageChanges()
        {
            _fileManager.StageNewFilesAsync();
        }

        public void RequestClearStagedFiles()
        {
            _fileManager.ClearDeployedFileChanges();
        }

        public void RequestOverlappedFileAllocation(List<ChangedFile> overlapSorted, List<ChangedFile> newSorted)
        {
            _fileManager.HandleAbnormalFiles(overlapSorted, newSorted);
        }

        public void RequestProjectIntegrityCheck()
        {
            var integrityTask = _fileManager.MainProjectIntegrityCheck();
            if (!integrityTask.Result)
            {
                _currentDeployMode = DeployMode.IntegrityCheck;
            }
        }

        public void RequestFileRestore(ProjectFile targetFile, DataState state)
        {
            _fileManager.RegisterNewfile(targetFile, state);
        }

        public void RequestExportProjectBackup(ProjectData projectData)
        {
            _exportManager.ExportProject(projectData);
        }

        public void RequestExportProjectVersionLog(ProjectData projectData)
        {
            _exportManager.ExportProjectVersionLog(projectData);
        }

        public void RequestExportProjectVersionDiffFiles(List<ChangedFile> FileDiffs)
        {

        }

        public void RequestProjectCompatibility(ProjectData srcProjectData)
        {
            try
            {
                if (_projectMetaData == null) return;
                List<ProjectSimilarity> projectComparisons = []; 
                foreach (ProjectData projData in _projectMetaData.ProjectDataList)
                {
                    ProjectSimilarity similaraity = new ProjectSimilarity();
                    projectComparisons.Add(similaraity); 
                    //Compute the file differences 
                        try
                        {
                            int sigDiff = 0; 
                            List<ChangedFile>? identifiedDiff = _fileManager.FindVersionDifferences(srcProjectData, projData, out sigDiff);
                            similaraity.projData = projData;
                            similaraity.numDiffWithResources = identifiedDiff?.Count ?? -1; 
                            similaraity.numDiffWithoutResources = sigDiff;
                            similaraity.fileDifferences = identifiedDiff ?? [];
                        }
                        catch(Exception ex)
                        {
                            MessageBox.Show($"Error while collecting Project Compatibility {ex.Message}");
                            return;
                        }
                }
                SimilarityCheckCompleteEventHandler?.Invoke(srcProjectData, projectComparisons); 
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error while collecting Project Compatibility {ex.Message}");
                return; 
            }
        }

        public void RequestExportProjectFilesXLSX(ICollection<ProjectFile> projectFiles, ProjectData projData)
        {
            _exportManager.ExportProjectFilesXLSX(projData, projectFiles);
        }
        public void RequestProjectIntegrate(string? updaterName, string? updateLog, string? currentProjectPath)
        {
            try
            {
                List<ChangedFile>? fileDifferences = _fileManager.FindFilesForIntegration(_srcProjectData, MainProjectData);
                if (_updateManager.TryIntegrateSrcProject(_srcProjectData, fileDifferences))
                {
                    _updateManager.IntegrateProjectMain(updaterName, updateLog, currentProjectPath, fileDifferences);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        public async void RequestProjectUpdate(string? updaterName, string? updateLog, string? currentProjectPath)
        {
            try
            {
                if (_currentDeployMode == DeployMode.Safe)
                {
                    _currentDeployMode = DeployMode.IntegrityCheck;

                    var result = await _fileManager.MainProjectIntegrityCheck();
                    if (result == false)
                    {
                        MessageBox.Show("Integrity Check Failed!");
                        return;
                    }
                    else
                    {
                        _currentDeployMode = DeployMode.Safe;
                    }
                }

                _updateManager.UpdateProjectMain(updaterName, updateLog, currentProjectPath);
                if (_currentDeployMode == DeployMode.IntegrityCheck)
                {
                    _currentDeployMode = _prevDeployMode;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        public void RequestProjVersionDiff(ProjectData srcData)
        {
            List<ChangedFile>? fileDiff = _fileManager.FindVersionDifferences(srcData, MainProjectData);
            ProjComparisonCompleteEventHandler?.Invoke(srcData, MainProjectData, fileDiff);
        }

        
        #endregion

        #region Version Management Tools
        private string GetProjectVersionName(ProjectData projData, bool isNewProject = false)
        {
            if (!isNewProject)
            {
                return $"{projData.ProjectName}_{Environment.MachineName}_{DateTime.Now.ToString("yyyy_MM_dd")}_v{projData.RevisionNumber + 1}";
            }
            return $"{projData.ProjectName}_{Environment.MachineName}_{DateTime.Now.ToString("yyyy_MM_dd")}_v{projData.RevisionNumber}";
        }

        #endregion

        #region Callbacks
        public void UpdateManager_ReportFileDifferencesCallBack(ProjectData srcData, ProjectData destData, List<ChangedFile> fileDifferences)
        {
            ProjComparisonCompleteEventHandler?.Invoke(srcData, MainProjectData, fileDifferences);
        }
        private void FileManager_IntegrityCheckCallBack(string changeLog, List<ProjectFile> changedFileList)
        {
            IntegrityCheckCompleteEventHandler?.Invoke(changeLog, new ObservableCollection<ProjectFile>(changedFileList));
        }

        private void FileManager_DataPreStagedCallBack(object preStagedFileListObj)
        {
            if (preStagedFileListObj is not List<ProjectFile> preStagedFileList) return;
            ObservableCollection<ProjectFile> preStagedChangesObs = new ObservableCollection<ProjectFile>(preStagedFileList);
            FileChangesEventHandler?.Invoke(preStagedChangesObs);
        }

        private void FileManager_DataStagedCallBack(object stagedFileListObj)
        {
            if (stagedFileListObj is not List<ChangedFile> stagedFiles)
            {
                MessageBox.Show("Improper stagedFile parameter value returned");
                return;
            }
            ObservableCollection<ProjectFile> stagedChangesObs = new ObservableCollection<ProjectFile>();
            foreach (ChangedFile file in stagedFiles)
            {
                if (file.DstFile != null) stagedChangesObs.Add(file.DstFile);
            }
            FileChangesEventHandler?.Invoke(stagedChangesObs);
            StagedChangesEventHandler?.Invoke(stagedFiles);
        }

        private void FileManager_OverlappedFileFoundCallBack(object overlapFileListObj, object newFileListObj)
        {
            if (overlapFileListObj is not List<ChangedFile> overlapFileList || newFileListObj is not List<ChangedFile> newFileList) return;
            OverlappedFileSortEventHandler?.Invoke(overlapFileList, newFileList);
        }

        private void ProjectChangeCallBack(object projObj)
        {
            if (projObj is not ProjectData projData) return;
            this.MainProjectData = projData;
        }

        private void FileManager_SrcProjectLoadedCallBack(object? srcProjectObj)
        {
            if (srcProjectObj is null)
            {
                _srcProjectData = null;
                SrcProjectLoadedEventHandler?.Invoke(null);
            }
            if (srcProjectObj is not ProjectData srcProjectData)
            {
                return;
            }
            _srcProjectData = srcProjectData;
            SrcProjectLoadedEventHandler?.Invoke(_srcProjectData);
        }

        private void BackupManager_FetchCompleteCallBack(object backupListObj)
        {
            if (backupListObj is not ObservableCollection<ProjectData> backupList) return;
            FetchRequestEventHandler?.Invoke(backupListObj);
        }

        private void ExportManager_ExportCompleteCallBack(object exportPathObj)
        {
            if (exportPathObj is not string exportPath) return;
            ProjExportEventHandler?.Invoke(exportPath);
        }

        private void ManagerStateCallBack(MetaDataState state)
        {
            CurrentState = state;
        }

        private void SettingManager_SetLastDstProjectCallBack(string dstProjectPath)
        {
            if (!RequestProjectRetrieval(dstProjectPath))
            {
                MessageBox.Show("Project Data not found! Please Reconfigure Destination Path");
                return;
            }
        }

        private void SettingManager_UpdateIgnoreListCallBack(object projIgnoreDataObj)
        {
            if (projIgnoreDataObj is not ProjectIgnoreData projIgnoreData) return;
            UpdateIgnoreListEventHandler?.Invoke(projIgnoreData);
        }
        #endregion

        #region Temporary
        #region Exports
        /// <summary>
        /// Input: Requested Project Data 
        /// Output: All the project files, including projectData meta file
        /// in a @.projectParentDir/Exports/ProjectVersion
        /// </summary>
        /// <param name="projectData"></param>

        public void ExportProjectRepo(ProjectMetaData projectRepository)
        {

        }
        public void GenerateProjectDataHash(object obj)
        {

        }
        private bool TryGenerateSupplementDirectories(string projPath, string projName)
        {
            try
            {
                string backupPath = $"{projPath}\\Backup_{projName}";
                string exportPath = $"{projPath}\\Export_{projName}";
                if (!Directory.Exists(backupPath)) Directory.CreateDirectory(backupPath);
                if (!Directory.Exists(exportPath)) Directory.CreateDirectory(exportPath);
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Project Initialization Failed due to {ex.Message}");
                return false;   
            }
        }
        #endregion
        #endregion
    }
}