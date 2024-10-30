using DeployAssistant.Model;
using SimpleBinaryVCS.Interfaces;
using SimpleBinaryVCS.Model;
using System.IO;
using System.Text;
using System.Text.Json;

namespace SimpleBinaryVCS.Utils
{
    public class FileHandlerTool
    {
        private static readonly JsonSerializerOptions _jsonSerializeOption = new() { WriteIndented = true };

        public static bool TrySerializeProjectData(ProjectData data, string filePath)
        {
            try
            {
                var jsonData = JsonSerializer.Serialize(data);
                var base64EncodedData = Convert.ToBase64String(Encoding.UTF8.GetBytes(jsonData));
                File.WriteAllText(filePath, base64EncodedData);
                return true; 
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error serializing ProjectData: " + ex.Message);
                return false;
            }
        }

        public static bool TryDeserializeProjectData(string filePath, out ProjectData? projectData)
        {
            try
            {
                var jsonDataBase64 = File.ReadAllText(filePath);
                var jsonDataBytes = Convert.FromBase64String(jsonDataBase64);
                string jsonString = Encoding.UTF8.GetString(jsonDataBytes);
                ProjectData? data = JsonSerializer.Deserialize<ProjectData>(jsonString);
                if (data != null)
                {
                    projectData = data;
                    return true; 
                }
                projectData = null;
                return false; 
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error deserializing ProjectData: " + ex.Message);
                projectData = null; 
                return false;
            }
        }

        public static bool TrySerializeProjectMetaData(ProjectMetaData data, string filePath)
        {
            try
            {
                var jsonData = JsonSerializer.Serialize(data);
                var base64EncodedData = Convert.ToBase64String(Encoding.UTF8.GetBytes(jsonData));
                File.WriteAllText(filePath, base64EncodedData);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error serializing ProjectMetaData: " + ex.Message);
                return false; 
            }
        }

        public static bool TryDeserializeProjectMetaData(string filePath, out ProjectMetaData? projectMetaData)
        {
            try
            {
                var jsonDataBase64 = File.ReadAllText(filePath);
                var jsonDataBytes = Convert.FromBase64String(jsonDataBase64);
                string jsonData = Encoding.UTF8.GetString(jsonDataBytes);
                ProjectMetaData? data = JsonSerializer.Deserialize<ProjectMetaData>(jsonData);
                if (data != null)
                {
                    projectMetaData = data;
                    return true;
                }
                else
                    projectMetaData = null;
                return false; 
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error deserializing ProjectMetaData: " + ex.Message);
                projectMetaData = null; 
                return false;
            }
        }

        public static bool TrySerializeJsonData<T>(string filePath, in T? serializingObject)
        {
            try
            {
                var jsonData = JsonSerializer.Serialize(serializingObject, _jsonSerializeOption);
                File.WriteAllText(filePath, jsonData);
                return true; 
            }
            catch
            {
                return false;
            }
        }

        public static bool TryDeserializeJsonData<T>(string filePath, out T? serializingObject)
        {
            try
            {
                var jsonDataBytes = File.ReadAllBytes(filePath);
                T? serializingObj = JsonSerializer.Deserialize<T>(jsonDataBytes);
                if (serializingObj != null)
                {
                    serializingObject = serializingObj;
                    return true;
                }
                else
                {
                    serializingObject = default;
                    return false;
                }
            }
            catch
            {
                serializingObject = default;
                return false;
            }
        }

        public static bool TryApplyFileChanges(List<ChangedFile> changes)
        {
            if (changes == null) 
            {
                return false;
            }
            try
            {
                foreach (ChangedFile file in changes)
                {
                    if ((file.DataState & DataState.IntegrityChecked) != 0) continue;
                    bool result = HandleData(file.SrcFile, file.DstFile, file.DataState);
                    if (!result) return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Couldn't Process File Changes : {ex.Message}");
                return false;
            }
        }

        public static bool HandleData(IProjectData dstData, DataState state)
        {
            bool result;
            if (dstData.DataType == ProjectDataType.File)
            {
                result = HandleFile(null, dstData.DataAbsPath, state);
            }
            else
            {
                result = HandleDirectory(null, dstData.DataAbsPath, state);
            }
            return result; 
        }

        public static bool HandleData(IProjectData? srcData, IProjectData dstData, DataState state)
        {
            bool result;
            if (dstData.DataType == ProjectDataType.File)
            {
                result = HandleFile(srcData?.DataAbsPath, dstData.DataAbsPath, state);
            }
            else
            {
                result = HandleDirectory(srcData?.DataAbsPath, dstData.DataAbsPath, state);
            }
            return result;
        }

        public static bool HandleData(string? srcPath, string dstPath, ProjectDataType type, DataState state)
        {
            bool result; 
            if (type == ProjectDataType.File)
            {
                result = HandleFile(srcPath, dstPath, state);
            }
            else
            {
                result = HandleDirectory(srcPath, dstPath, state);
            }
            return result;
        }

        public static void HandleData(string? srcPath, IProjectData dstData, DataState state)
        {
            if (dstData.DataType == ProjectDataType.File)
            {
                HandleFile(srcPath, dstData.DataAbsPath, state);
            }
            else
            {
                HandleDirectory(srcPath, dstData.DataAbsPath, state);
            }
        }

        public static bool HandleDirectory(string? srcPath, string dstPath, DataState state)
        {
            try
            {
                if ((state & DataState.Deleted) != 0)
                {
                    if (Directory.Exists(dstPath))
                    {
                        var subFiles = Directory.GetFiles(dstPath, "*", SearchOption.AllDirectories);
                        foreach (var subFile in subFiles)
                        {
                            HandleFile(null, subFile, DataState.Deleted);
                        }
                        Directory.Delete(dstPath, true);
                    }
                }
                else
                {
                    if (!Directory.Exists(dstPath))
                        Directory.CreateDirectory(dstPath);
                }
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message); return false; 
            }
        }

        public static bool HandleFile(string? srcPath, string dstPath, DataState state)
        {
            try
            {
                if ((state & DataState.Deleted) != 0)
                {
                    if (File.Exists(dstPath))
                    {
                        FileInfo fileInfo = new (dstPath);
                        if (fileInfo.IsReadOnly) 
                        {
                            fileInfo.IsReadOnly = false;
                        }
                        File.Delete(dstPath);
                    }
                    return true;
                }
                if (srcPath == null)
                {
                    MessageBox.Show($"Source File is null while File Handle state is {state}");
                    return false;
                }
                if ((state & DataState.Added) != 0)
                {
                    if (!Directory.Exists(Path.GetDirectoryName(dstPath))) 
                        Directory.CreateDirectory(Path.GetDirectoryName(dstPath));
                    if (File.Exists(dstPath))
                    {
                        FileInfo fileInfo = new (dstPath);
                        if (fileInfo.IsReadOnly) fileInfo.IsReadOnly = false;
                    }
                    if (File.Exists(srcPath))
                    {
                        File.Copy(srcPath, dstPath, true);
                    }
                }
                else
                {
                    if (!Directory.Exists(Path.GetDirectoryName(dstPath)))
                        Directory.CreateDirectory(Path.GetDirectoryName(dstPath));
                    if (srcPath == dstPath)
                    {
                        //MessageBox.Show($"Source File and Dst File path is same for {state.ToString()}, {dstPath}");
                        return false; 
                    }
                    if (File.Exists(dstPath))
                    {
                        FileInfo fileInfo = new (dstPath);
                        if (fileInfo.IsReadOnly) 
                        {
                            fileInfo.IsReadOnly = false;
                        }
                    }
                    if (File.Exists(srcPath))
                    {
                        if (srcPath != dstPath)
                        {
                            File.Copy(srcPath, dstPath, true);
                        }
                    }
                }
                return true; 
            }
            catch (UnauthorizedAccessException auex)
            {
                MessageBox.Show($"File Read Only Issue : {auex.Message}"); return false; 
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Issue While Handling File {ex.Message}"); return false; 
            }
        }

        public static bool MoveFile(string? srcPath, string dstPath)
        {
            try
            {
                if (!Directory.Exists(Path.GetDirectoryName(dstPath)))
                    Directory.CreateDirectory(Path.GetDirectoryName(dstPath));
                if (srcPath != dstPath)
                    File.Move(srcPath, dstPath, true); 
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Couldn't Move File {ex.Message}");
                return false; 
            }
        }

        public List<string> GetDirectories(string rootPath, Dictionary<string, string> ignoreDirs)
        {
            List<string> resultDirs = [];
            Stack<string> dirsToProcess = [];
            dirsToProcess.Push(rootPath);

            while (dirsToProcess.Count > 0)
            {
                string currentDir = dirsToProcess.Pop();
                string relativePath = Path.GetRelativePath(rootPath, currentDir);
            
                // Skip ignored directories
                if (ignoreDirs.ContainsKey(relativePath))
                {
                    continue;
                }
            
                resultDirs.Add(currentDir);
            
                try
                {
                    foreach (var dir in Directory.GetDirectories(currentDir))
                    {
                        string dirRelativePath = Path.GetRelativePath(rootPath, dir);
            
                        if (!ignoreDirs.ContainsKey(dirRelativePath))
                        {
                            dirsToProcess.Push(dir);
                        }
                    }
                }
                catch (UnauthorizedAccessException ex)
                {
                    Console.WriteLine($"Access denied to {currentDir}: {ex.Message}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing {currentDir}: {ex.Message}");
                }
            }
            
            return resultDirs;
        }
    }
}