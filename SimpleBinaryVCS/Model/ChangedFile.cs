using DeployAssistant.Model;
using System.Text.Json.Serialization;

namespace SimpleBinaryVCS.Model
{
    public class ChangedFile
    {
        public ProjectFile? SrcFile { get; set; }
        public ProjectFile? DstFile { get; set;}
        public DataState DataState {  get; set; }
        public ChangedFile() { }
        public ChangedFile(ChangedFile srcChangedfile)
        {
            if (srcChangedfile.SrcFile != null)
            {
                SrcFile = new(srcChangedfile.SrcFile)
                {
                    IsDstFile = false
                };
            }
            else
                SrcFile = null;
            if (srcChangedfile.DstFile != null)
            {
                DstFile = new ProjectFile(srcChangedfile.DstFile)
                {
                    IsDstFile = true
                };
            }
            else
                DstFile = null;
            DataState = srcChangedfile.DataState;
        }
        public ChangedFile(ProjectFile DstFile, DataState DataState)
        {
            SrcFile = null;
            this.DstFile = DstFile;
            DstFile.IsDstFile = (DataState & DataState.Overlapped) == 0; 
            this.DataState = DataState;
        }
        public ChangedFile(ProjectFile SrcFile,  ProjectFile DstFile, DataState DataState, bool RegisterChanges)
        {
            this.SrcFile = SrcFile;
            SrcFile.IsDstFile = false; 
            this.DstFile = DstFile;
            DstFile.IsDstFile = (DataState & DataState.Overlapped) == 0;
            this.DataState = DataState;
        }
        [JsonConstructor]
        public ChangedFile(ProjectFile SrcFile, ProjectFile DstFile, DataState DataState)
        {
            this.SrcFile = SrcFile;
            this.DstFile = DstFile;
            this.DataState = DataState;
        }
    }
}
