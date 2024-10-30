using DeployAssistant.Model;
using DeployManager.Model;
using SimpleBinaryVCS;
using SimpleBinaryVCS.Interfaces;
using SimpleBinaryVCS.Model;
using SimpleBinaryVCS.Utils;
using System.IO;

namespace DeployManager.DataComponent
{

    public class SettingManager : IManager
    {
        public event Action<MetaDataState>? ManagerStateEventHandler;
        public event Action<string>? SetPrevProjectEventHandler;
        public event Action<ProjectIgnoreData>? UpdateIgnoreListEventHandler;
        public ProjectIgnoreData _projectIgnoreData; 

        private readonly string? DAMetaFilePath;
        private string? ignoreMetaFilePath;
        
        private const string _configFilename = "DeployAssistant.config";
        private const string _projIgnoreFilename = "DeployAssistant.ignore";
        public SettingManager()
        {
            try
            {
                string defaultWindowDocumentPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                DAMetaFilePath = Path.Combine(defaultWindowDocumentPath, _configFilename);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public void Awake()
        {
            try
            {
                if (File.Exists(DAMetaFilePath))
                {
                    if (!FileHandlerTool.TryDeserializeJsonData(DAMetaFilePath, out LocalConfigData? localConfigData))
                    { 
                        return; 
                    }
                    var result = MessageBox.Show($"Recent Destination Project Path Found: Proceed with this Destination? {localConfigData!.LastOpenedDstPath}",
                        "Import Previous Destination Project", MessageBoxButtons.YesNo);
                    if (result == DialogResult.Yes)
                    {
                        SetPrevProjectEventHandler?.Invoke(localConfigData.LastOpenedDstPath);
                    }
                    else
                    {
                        MessageBox.Show("Couldn't Retrieve ");
                        return;
                    }
                }
            }
            //GetConfigData
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

        }
        public void RegisterDefaultSettings(string dstPath)
        {
            SetRecentDstDirectory(dstPath);

        }

        public void SetRecentDstDirectory(string dstPath)
        {
            LocalConfigData localConfig = new (dstPath);
            FileHandlerTool.TrySerializeJsonData(DAMetaFilePath, localConfig);
        }
        #region Request Calls
        #endregion

        #region CallBacks 
        public void MetaDataManager_MetaDataLoadedCallBack(object projectMetaDataObj)
        {
            if (projectMetaDataObj is not ProjectMetaData projectMetaData) return;
            ignoreMetaFilePath = Path.Combine(projectMetaData.ProjectPath, _projIgnoreFilename);
            
            try
            {
                if (File.Exists(ignoreMetaFilePath))
                {
                    if (!FileHandlerTool.TryDeserializeJsonData(ignoreMetaFilePath, out ProjectIgnoreData? projectIgnoreData))
                    {
                        MessageBox.Show($"Setting Manager Project Ignore Error, Couldn't Deserialize IgnoreData");
                        return;
                    }
                    else
                    {
                        _projectIgnoreData = projectIgnoreData!;
                    }
                }
                else
                {
                    _projectIgnoreData = new ProjectIgnoreData(projectMetaData.ProjectName);
                    _projectIgnoreData.ConfigureDefaultIgnore(projectMetaData.ProjectName);
                    if (!FileHandlerTool.TrySerializeJsonData(ignoreMetaFilePath, _projectIgnoreData))
                    {
                        MessageBox.Show($"Setting Manager Project Ignore Error, Couldn't initialize IgnoreData");
                        return;
                    }
                }
                UpdateIgnoreListEventHandler?.Invoke(_projectIgnoreData);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Setting Manager Project Ignore Error {ex.Message}");
            }
        }
        #endregion
    }
}