using SimpleBinaryVCS.Model;
using System.IO;
using System.Text.Json.Serialization;

namespace DeployAssistant.Model
{
    public class ProjectIgnoreData
    {
        public string ProjectName { get; set; }
        //Following .ignore functionality is designed mostly for the part of Integration Process. 
        public List<RecordedFile> IgnoreFileList { get; set; }
        [JsonConstructor]
        public ProjectIgnoreData() { }

        public ProjectIgnoreData(string ProjectName)
        {
            this.ProjectName = ProjectName;
            IgnoreFileList = new List<RecordedFile>()
            {
                new RecordedFile("ProjectMetaData.bin" , ProjectDataType.File, IgnoreType.All),
                new RecordedFile("*.ignore" , ProjectDataType.File, IgnoreType.All),
                new RecordedFile("*.VersionLog", ProjectDataType.File, IgnoreType.All),
                new RecordedFile("Export_XLSX", ProjectDataType.Directory, IgnoreType.All),
                new RecordedFile("ProductionRecord.db" , ProjectDataType.File, IgnoreType.All),
                new RecordedFile("configfilepath.txt" , ProjectDataType.File, IgnoreType.All),
                new RecordedFile("msg_format.dat" , ProjectDataType.File, IgnoreType.All),
                new RecordedFile("*.deploy" , ProjectDataType.File, IgnoreType.All),
                new RecordedFile("en-US", ProjectDataType.Directory, IgnoreType.Integration),
                new RecordedFile("ko-KR", ProjectDataType.Directory, IgnoreType.Integration),
                new RecordedFile("Resources", ProjectDataType.Directory, IgnoreType.Integration)
            };
        }

        public void FilterProjectFileList(ref List<ProjectFile> fileList, IgnoreType ignoreType)
        {
            List<ProjectFile> excludingList = [];
            foreach (ProjectFile file in fileList)
            {
                if (PartOfIgnore(file, ignoreType)) excludingList.Add(file);
            }
            fileList = fileList.Except(excludingList).ToList();
        }

        public void FilterProjectRelFileDirList(ref List<string> relFileDirList, IgnoreType ignoreType)
        {
            List<string> excludingList = [];
            foreach (string relFileDir in relFileDirList)
            {
                if (PartOfIgnoreFileDir(relFileDir, ignoreType)) excludingList.Add(relFileDir);
            }
            relFileDirList = relFileDirList.Except(excludingList).ToList();
        }
        public void FilterProjectRelDirList(ref List<string> relDirList, IgnoreType ignoreType)
        {
            List<string> excludingList = [];
            foreach (string relDir in relDirList)
            {
                if (PartOfIgnoreDir(relDir, ignoreType)) excludingList.Add(relDir);
            }
            relDirList = relDirList.Except(excludingList).ToList();
        }
        public void FilterChangedFileList(ref List<ChangedFile> changedFileList)
        {
            List<ChangedFile> excludingList = []; 
            foreach (ChangedFile file in changedFileList)
            {
                if (file.SrcFile == null || file.SrcFile.DataName == "")
                {
                    if (PartOfIgnore(file.DstFile)) excludingList.Add(file);
                }
                else
                {
                    if (PartOfIgnore(file.SrcFile)) excludingList.Add(file);
                }
            }
            changedFileList = changedFileList.Except(excludingList).ToList();
        }

        public void FilterChangedFileList(ref List<ChangedFile> changedFileList, IgnoreType ignoreType)
        {
            List<ChangedFile> excludingList = [];
            foreach (ChangedFile file in changedFileList)
            {
                if (file.SrcFile == null || file.SrcFile.DataName == "")
                {
                    if (PartOfIgnore(file.DstFile, ignoreType)) excludingList.Add(file);
                }
                else
                {
                    if (PartOfIgnore(file.SrcFile, ignoreType)) excludingList.Add(file);
                }
            }
            changedFileList = changedFileList.Except(excludingList).ToList();
        }
        public void FilterChangedFileList(List<ChangedFile> changedFileList, IgnoreType ignoreType, out int sigDiffCount)
        {
            List<ChangedFile> excludingList = [];
            List<ChangedFile> sigDiffList = []; 
            foreach (ChangedFile file in changedFileList)
            {
                if (file.SrcFile == null || file.SrcFile.DataName == "")
                {
                    if (PartOfIgnore(file.DstFile, ignoreType)) excludingList.Add(file);
                }
                else
                {
                    if (PartOfIgnore(file.SrcFile, ignoreType)) excludingList.Add(file);
                }
            }
            sigDiffList = changedFileList.Except(excludingList).ToList();
            sigDiffCount = sigDiffList.Count;
        }

        public void ConfigureDefaultIgnore(string projName)
        {
            string backupDir = $"Backup_{projName}";
            IgnoreFileList.Add(new RecordedFile(backupDir, ProjectDataType.Directory, IgnoreType.IntegrityCheck));
            string exportDir = $"Export_{projName}";
            IgnoreFileList.Add(new RecordedFile(exportDir, ProjectDataType.Directory, IgnoreType.IntegrityCheck));
        }
        
        public (List<string>, List<string>) GetIgnoreFilesAndDirPaths(string searchPath, IgnoreType requestedIgnoreType)
        {
            List<string> excludedFiles = new List<string>();
            List<string> excludedDirs = new List<string>(); 
            try
            {
                foreach (RecordedFile ignoreData in IgnoreFileList)
                {
                    if ((requestedIgnoreType & ignoreData.IgnoreType) == 0) continue; 
                    if (ignoreData.DataType == ProjectDataType.File)
                    {
                        excludedFiles.AddRange(Directory.GetFiles(searchPath, ignoreData.DataName, SearchOption.AllDirectories));
                    }
                    else
                    {
                        var identifiedDirs = Directory.GetDirectories(searchPath, ignoreData.DataName, SearchOption.AllDirectories);
                        if (identifiedDirs == null) continue;
                        foreach (string identifiedDir in identifiedDirs)
                        {
                            excludedDirs.Add(identifiedDir);
                            GetFilesAndDirsFromDirectory(identifiedDir, excludedFiles, excludedDirs);
                        }
                    }
                }
                return (excludedFiles, excludedDirs);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Critical Error while trying to get ignore files for deploy, {ex.Message}");
                return (new List<string>(), new List<string>());
            }
        }
        
        private void GetFilesAndDirsFromDirectory(string directoryPath, List<string> excludedFiles, List<string> excludedDirs)
        {
            try
            {
                var files = Directory.GetFiles(directoryPath, "*.*", SearchOption.AllDirectories);
                excludedFiles.AddRange(files);
                var dirs = Directory.GetDirectories(directoryPath, "*.*", SearchOption.AllDirectories);
                excludedDirs.AddRange(dirs);
            }
            catch (Exception ex)
            {
                // Handle case where access to directory is denied
                MessageBox.Show($"Error: Access denied to directory: {directoryPath} {ex.Message}");
                return;
            }
        }

        private bool PartOfIgnore(ProjectFile projectFile)
        {
            foreach (RecordedFile file in IgnoreFileList)
            {
                if (file.DataType == ProjectDataType.File)
                {
                    if (projectFile.DataName.Equals(file.DataName, StringComparison.OrdinalIgnoreCase)) return true;
                }
                else
                {
                    if (projectFile.DataRelPath.Contains(file.DataName, StringComparison.OrdinalIgnoreCase)) return true;
                }
            }
            return false;
        }

        private bool PartOfIgnore(ProjectFile projectFile, IgnoreType ignoreType)
        {
            foreach (RecordedFile file in IgnoreFileList)
            {
                if ((ignoreType & file.IgnoreType) == 0) continue; 
                if (file.DataType == ProjectDataType.File)
                {
                    if (projectFile.DataName == file.DataName) return true;
                }
                else
                {
                    if (projectFile.DataRelPath.Contains(file.DataName, StringComparison.OrdinalIgnoreCase)) return true;
                }
            }
            return false;
        }

        private bool PartOfIgnoreDir(string relativePath, IgnoreType ignoreType)
        {
            foreach (RecordedFile file in IgnoreFileList)
            {
                if ((ignoreType & file.IgnoreType) == 0) continue;
                if (file.DataType == ProjectDataType.File)
                {
                    continue;
                }
                else
                {
                    if (relativePath.Contains(file.DataName, StringComparison.OrdinalIgnoreCase)) return true;
                }
            }
            return false;
        }

        private bool PartOfIgnoreFileDir(string relativePath, IgnoreType ignoreType)
        {
            foreach (RecordedFile file in IgnoreFileList)
            {
                if ((ignoreType & file.IgnoreType) == 0) continue;
                if (file.DataType == ProjectDataType.File)
                {
                    string pathFileName = Path.GetFileName(relativePath);
                    if (file.DataName.Equals(pathFileName, StringComparison.OrdinalIgnoreCase)) return true;
                }
                else
                {
                    continue; 
                }
            }
            return false;
        }
    }
}