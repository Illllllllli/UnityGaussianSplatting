using System;
using System.IO;
using System.Linq;
using UnityEngine;

namespace StartScene
{
    public static class FileHelper
    {
        /// <summary>
        /// 验证文件夹名称是否合法（单层）
        /// </summary>
        /// <param name="folderName">文件夹名称</param>
        /// <param name="errorMessage">错误信息</param>
        /// <returns></returns>
        public static bool ValidateFolderName(string folderName, out string errorMessage)
        {
            errorMessage = "";

            // 空值检查
            if (string.IsNullOrWhiteSpace(folderName))
            {
                errorMessage = "Folder name cannot be empty";
                return false;
            }

            // 检查路径分隔符
            if (folderName.Contains(Path.DirectorySeparatorChar.ToString()) ||
                folderName.Contains(Path.AltDirectorySeparatorChar.ToString()))
            {
                errorMessage =
                    $"Name cannot contain path separators ({Path.DirectorySeparatorChar} or {Path.AltDirectorySeparatorChar})";
                return false;
            }

            // 检查相对路径符号
            if (folderName.Trim() == "." || folderName.Trim() == "..")
            {
                errorMessage = "Cannot use '.' or '..' as name";
                return false;
            }

            // 检查非法字符
            char[] invalidChars = Path.GetInvalidFileNameChars();
            foreach (char c in invalidChars)
            {
                if (folderName.Contains(c.ToString()))
                {
                    errorMessage = $"Name contains invalid character: '{c}'";
                    return false;
                }
            }

            return true;
        }


        /// <summary>
        /// 运行时文件夹复制方法
        /// </summary>
        /// <param name="sourcePath">源路径（推荐使用Application.streamingAssetsPath）</param>
        /// <param name="targetPath">目标路径（推荐使用Application.persistentDataPath）</param>
        /// <param name="overwrite">是否覆盖已存在文件</param>
        /// <param name="errorMessage">错误信息</param>
        /// <returns>是否成功</returns>
        public static bool CopyFolder(string sourcePath, string targetPath, bool overwrite, out string errorMessage)
        {
            errorMessage = "";
            try
            {
                // 验证源路径
                if (!Directory.Exists(sourcePath))
                {
                    errorMessage = $"Dir does not exist：{sourcePath}";
                    return false;
                }

                // 创建目标根目录
                Directory.CreateDirectory(targetPath);

                // 获取所有子目录
                string[] directories = Directory.GetDirectories(
                    sourcePath,
                    "*",
                    SearchOption.AllDirectories
                );

                // 在目标路径创建所有子目录（所有层级）
                foreach (string dirPath in directories)
                {
                    string relativePath = dirPath.Substring(sourcePath.Length).TrimStart(Path.DirectorySeparatorChar);
                    string newDirPath = Path.Combine(targetPath, relativePath);
                    Directory.CreateDirectory(newDirPath);
                }

                // 获取所有文件（排除.meta）
                string[] allFiles = Directory.GetFiles(
                    sourcePath,
                    "*.*",
                    SearchOption.AllDirectories
                ).Where(f => !f.EndsWith(".meta")).ToArray();

                // 复制所有文件
                foreach (string filePath in allFiles)
                {
                    string relativePath = filePath.Substring(sourcePath.Length).TrimStart(Path.DirectorySeparatorChar);
                    string destPath = Path.Combine(targetPath, relativePath);

                    // 执行文件复制
                    File.Copy(filePath, destPath, overwrite);
                }

                return true;
            }
            catch (Exception ex)
            {
                errorMessage = $"Copy Failed : {ex.GetType()} - {ex.Message}";
                return false;
            }
        }

        /// <summary>
        /// 删除指定路径（文件或文件夹）
        /// </summary>
        /// <param name="targetPath">目标路径</param>
        /// <param name="recursive">是否递归删除文件夹内容</param>
        /// <param name="errorMessage">错误信息</param>
        /// <returns>是否成功</returns>
        public static bool DeletePath(string targetPath, bool recursive, out string errorMessage)
        {
            errorMessage = "";
            try
            {
                // 检测路径是否存在
                if (!File.Exists(targetPath) && !Directory.Exists(targetPath))
                {
                    errorMessage = $"Path does not exist ：{targetPath}";
                    return false;
                }

                // 处理文件删除
                FileAttributes attr = File.GetAttributes(targetPath);
                if ((attr & FileAttributes.Directory) != FileAttributes.Directory)
                {
                    File.Delete(targetPath);
                    return true;
                }

                // 处理文件夹删除
                return DeleteDirectory(targetPath, recursive);
            }
            catch (Exception ex)
            {
                errorMessage = $"Delete failed ：{ex.GetType()} - {ex.Message}";
                return false;
            }
        }

        /// <summary>
        /// 递归删除文件夹及其内容
        /// </summary>
        /// <param name="directoryPath">文件夹路径</param>
        /// <param name="recursive">是否递归删除</param>
        /// <returns>是否删除成功</returns>
        private static bool DeleteDirectory(string directoryPath, bool recursive)
        {
            try
            {
                if (recursive)
                {
                    // 先删除所有子文件
                    string[] files = Directory.GetFiles(directoryPath, "*", SearchOption.AllDirectories);
                    foreach (string file in files)
                    {
                        File.SetAttributes(file, FileAttributes.Normal);
                        File.Delete(file);
                    }

                    // 再删除所有子文件夹
                    string[] dirs = Directory.GetDirectories(directoryPath);
                    foreach (string dir in dirs)
                    {
                        DeleteDirectory(dir, true);
                    }
                }

                // 最后删除目标文件夹
                Directory.Delete(directoryPath, recursive);
                return true;
            }
            catch (IOException ex)
            {
                Debug.LogError($"目录可能被占用：{ex.Message}");
                return false;
            }
            catch (UnauthorizedAccessException ex)
            {
                Debug.LogError($"权限不足：{ex.Message}");
                return false;
            }
        }
    }
}