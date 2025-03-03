using System;
using System.Collections.Generic;
using System.IO;
using GaussianSplatting.Runtime;
using GSTestScene;
using UnityEngine;

namespace StartScene
{
    public class SceneTool : MonoBehaviour
    {
        private void Start()
        {
            // 预加载资产列表
            LoadGaussianSplatAssets(Status.sceneFileRoot);
        }
        // 所有已存在的GS资产列表
        public static readonly List<GaussianSplatAsset> ExistingGaussianSplatAssets=new ();
        /// <summary>
        /// 加载指定文件夹下所有单层子文件夹中的 GaussianSplatAsset 序列化资产。
        /// </summary>
        /// <param name="parentFolderPath">父文件夹路径（绝对路径）</param>
        /// <returns>包含所有找到的 GaussianSplatAsset 的列表</returns>
        private static void LoadGaussianSplatAssets(string parentFolderPath)
        {
            Debug.Log($"start loading scene assets from folder {parentFolderPath}...");
            // 获取父文件夹下所有一级子文件夹
            string[] subFolders = Directory.GetDirectories(parentFolderPath);

            foreach (string subFolder in subFolders)
            {
                string folderName = Path.GetFileName(subFolder);
                // 加载子文件夹中的所有 GaussianSplatAsset 文件
                GaussianSplatAsset[] folderAssets=  Resources.LoadAll<GaussianSplatAsset>(Path.Combine(Status.SceneFileName,folderName));

                if (folderAssets is { Length: > 0 })
                {
                    ExistingGaussianSplatAssets.AddRange(folderAssets);
                }
            }

            Debug.Log($"load finished. find {ExistingGaussianSplatAssets.Count} results");
        }

        //结束运行/切换场景时，清空资产列表
        private void OnDestroy()
        {
            ExistingGaussianSplatAssets.Clear();
        }
    }
}