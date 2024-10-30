using SimpleBinaryVCS.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeployAssistant.Model
{
    public class ProjectSimilarity
    {
        public ProjectData ProjData { get; set; }
        public int NumDiffWithoutResources { get; set; }
        public int NumDiffWithResources { get; set; }
        public List<ChangedFile> FileDifferences { get; set; }
        public ProjectSimilarity(ProjectData projData, int numDiffWithoutResources, int numDiffWithResources, List<ChangedFile> fileDifferences)
        {
            ProjData = projData;
            NumDiffWithoutResources = numDiffWithoutResources;
            NumDiffWithResources = numDiffWithResources;
            FileDifferences = fileDifferences;
        }

        public ProjectSimilarity() { }
    }
}