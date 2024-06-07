using DeployAssistant.Model;
using SimpleBinaryVCS.Interfaces;
using System.Diagnostics;
using System.IO;
using System.Text.Json.Serialization;

namespace SimpleBinaryVCS.Model
{
    public class ProjectFile : IEquatable<ProjectFile>, IComparable<ProjectFile>, IProjectData
    {
        #region [JsonInclude]
        public ProjectDataType DataType { get; private set; }
        public long DataSize { get; set; }
        public string BuildVersion {  get; set; }
        public string DeployedProjectVersion { get; set; }
        public DateTime UpdatedTime { get; set; }
        public bool IsDstFile { get; set; }
        public DataState DataState { get; set; }
        public string DataName { get; set; }
        public string DataSrcPath { get; set; }
        public string DataRelPath { get; set; }
        public string DataHash { get; set; }
        #endregion

        #region [JsonIgnore] 
        [JsonIgnore] 
        public string DataAbsPath => Path.Combine(DataSrcPath, DataRelPath);
        [JsonIgnore]
        public string DataRelDir => DataType == ProjectDataType.Directory ? DataRelPath: Path.GetDirectoryName(DataRelPath) ?? "";
        #endregion

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public ProjectFile() { }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        [JsonConstructor]
        public ProjectFile(ProjectDataType DataType, long DataSize, string BuildVersion, string DeployedProjectVersion, 
            DateTime UpdatedTime, DataState DataState, string dataName, string dataSrcPath, string dataRelPath, string dataHash, bool IsDstFile) 
        {
            this.DataType = DataType;
            this.DataSize = DataSize;
            this.BuildVersion = BuildVersion;
            this.DeployedProjectVersion = DeployedProjectVersion;
            this.UpdatedTime = UpdatedTime;
            this.DataState = DataState;
            DataName = dataName;
            DataSrcPath = dataSrcPath;
            DataRelPath = dataRelPath;
            DataHash = dataHash;
            this.IsDstFile = IsDstFile;
        }
        #region Overloaded Constructors
        /// <summary>
        /// Lacks FileHash, DeployedProjectVersion, FileChangedState
        /// </summary>
        public ProjectFile(long fileSize, string? fileVersion, string fileName, string fileSrcPath, string fileRelPath, DataState changedState)
        {
            DataSize = fileSize;
            BuildVersion = fileVersion ?? "";
            DataName = fileName;
            DataSrcPath = fileSrcPath;
            DataRelPath = fileRelPath;
            DataHash = "";
            DeployedProjectVersion = "";
            UpdatedTime = DateTime.Now;
            DataState = changedState;
        }
        /// <summary>
        /// For PreStagedProjectFile Data Type = File 
        /// </summary>
        public ProjectFile(long DataSize, string? BuildVersion, string DataName, string DataSrcPath, string DataRelPath)
        {
            DataType = ProjectDataType.File;
            this.DataSize = DataSize;
            this.BuildVersion = BuildVersion ?? "";
            DeployedProjectVersion = "";
            UpdatedTime = DateTime.MinValue;
            DataState = DataState.PreStaged;
            this.DataName = DataName;
            this.DataSrcPath = DataSrcPath;
            this.DataRelPath= DataRelPath;
            DataHash = "";
        }
        /// <summary>
        /// For PreStagedProjectFile Data Type = Directory 
        /// </summary>
        public ProjectFile(string DataName, string DataSrcPath, string DataRelPath)
        {
            DataType = ProjectDataType.Directory;
            DataSize = 0;
            BuildVersion = "";
            DeployedProjectVersion = "";
            UpdatedTime = DateTime.MinValue;
            DataState = DataState.PreStaged;
            this.DataName = DataName;
            this.DataSrcPath = DataSrcPath;
            this.DataRelPath = DataRelPath;
            DataHash = "";
        }
        /// <summary>
        /// Deep Copy of ProjectFile
        /// </summary>
        /// <param name="srcData">Project File to Copy</param>
        public ProjectFile(ProjectFile srcData)
        {
            DataType = srcData.DataType;
            DataSize = srcData.DataSize;
            BuildVersion = srcData.BuildVersion;
            DeployedProjectVersion = srcData.DeployedProjectVersion;
            UpdatedTime = srcData.UpdatedTime;
            DataState = srcData.DataState;
            DataName = srcData.DataName;
            DataSrcPath = srcData.DataSrcPath;
            DataRelPath = srcData.DataRelPath;
            DataHash = srcData.DataHash;
        }
        public ProjectFile(ProjectFile updatedData, string deployedProjectVersion, string currentProjectPath)
        {
            DataType = updatedData.DataType;
            DataSize = updatedData.DataSize;
            BuildVersion = updatedData.BuildVersion;
            DeployedProjectVersion = deployedProjectVersion;
            UpdatedTime = DateTime.Now;
            DataState = updatedData.DataState;
            DataName = updatedData.DataName;
            DataSrcPath = currentProjectPath;
            DataRelPath = updatedData.DataRelPath;
            DataHash = updatedData.DataHash;
        }
        public ProjectFile(ProjectFile srcData, DataState state)
        {
            DataType = srcData.DataType;
            DataSize = srcData.DataSize;
            BuildVersion = srcData.BuildVersion;
            DeployedProjectVersion = srcData.DeployedProjectVersion;
            UpdatedTime = DateTime.Now;
            DataState = state;
            DataName = srcData.DataName;
            DataSrcPath = srcData.DataSrcPath;
            DataRelPath = srcData.DataRelPath;
            DataHash = srcData.DataHash;
        }
        public ProjectFile(ProjectFile srcData, DataState DataState, string dataSrcPath)
        {
            DataType = srcData.DataType;
            DataSize = srcData.DataSize;
            BuildVersion = srcData.BuildVersion;
            DeployedProjectVersion = srcData.DeployedProjectVersion;
            UpdatedTime = DateTime.Now;
            this.DataState = DataState;
            DataName = srcData.DataName;
            DataSrcPath = dataSrcPath;
            DataRelPath = srcData.DataRelPath;
            DataHash = srcData.DataHash;
        }
        public ProjectFile(string fileSrcPath, string fileRelPath, string? fileHash, DataState DataState, ProjectDataType dataType)
        {
            try
            {
                string fileFullPath = Path.Combine(fileSrcPath, fileRelPath);
                if (dataType == ProjectDataType.File)
                {
                    var fileInfo = FileVersionInfo.GetVersionInfo(fileFullPath);
                    DataSize = new FileInfo(fileFullPath).Length;
                    BuildVersion = fileInfo.FileVersion ?? "";
                }
                else
                {
                    DataSize = 0;
                    BuildVersion = "";
                }
                DeployedProjectVersion = "";
                DataSrcPath = fileSrcPath;
                DataName = Path.GetFileName(fileFullPath);
                DataRelPath = fileRelPath;
                DataHash = fileHash ?? "";
                UpdatedTime = DateTime.Now;
                this.DataState = DataState;
                DataType = dataType;
            }
            catch (Exception ex)
            {
                DataSize = 0;
                BuildVersion = "ERROR";
                DeployedProjectVersion = "ERROR";
                DataName = "ERROR";
                DataRelPath = "ERROR";
                DataSrcPath = "ERROR";
                DataHash = "ERROR";
                UpdatedTime = DateTime.Now;
                this.DataState = DataState.None; 
                DataType = ProjectDataType.File;
                Console.WriteLine(ex.ToString());
            }
        }
        // 
        /// <summary>
        /// Empty ProjectFile with Given DataType
        /// </summary>
        public ProjectFile (ProjectDataType dataType)
        {
            DataType = dataType;
            DataSize = 0;
            BuildVersion = "";
            DeployedProjectVersion = "";
            UpdatedTime = DateTime.MaxValue;
            DataState = DataState.None;
            DataName = "";
            DataSrcPath = "";
            DataRelPath = "";
            DataHash = "";
        }
        #endregion
        public int CompareTo(ProjectFile? other) 
        {
            if (UpdatedTime.CompareTo(other.UpdatedTime) == 0)
                return DataSize.CompareTo(other.DataSize);
            return UpdatedTime.CompareTo(other.UpdatedTime);
        }
        /// <summary>
        /// IEquatable Implementation: Checks Data Name
        /// </summary>
        public bool Equals(ProjectFile? other)
        {
            if (other == null)
            {
                MessageBox.Show($"Presented ProjectFile is Null for comparision with {this.DataName}"); 
                return false;
            }
            return other.DataName == this.DataName;
        }
    }
}