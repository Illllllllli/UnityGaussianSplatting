using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using GaussianSplatting.Runtime;
using GSTestScene;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace StartScene
{
    public class SceneLoader : MonoBehaviour
    {
        private static TipsManager _tipsManager;

        private void Start()
        {
            // 预加载资产列表
            StartCoroutine(LoadGaussianSplatAssets());
        }

        // 所有已存在的GS资产列表
        public static readonly List<GaussianSplatAsset> ExistingGaussianSplatAssets = new();

        /// <summary>
        /// 加载默认路径下的资产
        /// </summary>
        public static IEnumerator LoadGaussianSplatAssets()
        {
            _tipsManager = MainUIManager.ShowTip("Searching Assets...").GetComponent<TipsManager>();
            _tipsManager.SetButtonInteractable(false);
            yield return null;
            ExistingGaussianSplatAssets.Clear();
            LoadGaussianSplatAssetsFromPath(Status.SceneFileRootEditor, false);
            LoadGaussianSplatAssetsFromPath(Status.SceneFileRootPlayer, true);
            _tipsManager.SetTipText($"Found {ExistingGaussianSplatAssets.Count} results");
            _tipsManager.SetButtonInteractable(true);
        }
        /// <summary>
        /// 加载指定文件夹下所有单层子文件夹中的 GaussianSplatAsset 序列化资产。
        /// </summary>
        /// <param name="parentFolderPath">父文件夹路径（绝对路径）</param>
        /// <param name="serialized">是否被序列化（存储在persistentDataPath的情况）</param>
        /// <returns>包含所有找到的 GaussianSplatAsset 的列表</returns>
        private static void LoadGaussianSplatAssetsFromPath(string parentFolderPath, bool serialized)
        {
            _tipsManager.SetTipText($"start loading scene assets from folder {parentFolderPath}...");
            if (!Directory.Exists(parentFolderPath)) return;
            // 获取父文件夹下所有一级子文件夹
            string[] subFolders = Directory.GetDirectories(parentFolderPath);

            foreach (string subFolder in subFolders)
            {
                // 二进制序列化存储(.dat)
                if (serialized)
                {
                    string[] filePaths = Directory.GetFiles(subFolder, $"*{Status.SceneFileSuffixPlayer}");
                    BinaryFormatter formatter = new BinaryFormatter();
                    foreach (string filePath in filePaths)
                    {
                        try
                        {
                            using FileStream stream = new FileStream(filePath, FileMode.Open);
                            GaussianSplatAssetData assetData = formatter.Deserialize(stream) as GaussianSplatAssetData;
                            ExistingGaussianSplatAssets.Add(assetData?.GetAsset());
                        }
                        catch (System.Exception e)
                        {
                            _tipsManager.SetTipText($"Load Asset Failed: {filePath}\n{e.Message}");
                        }
                    }
                }
                //在资产文件夹中以.asset存储
                else
                {
                    string folderName = Path.GetFileName(subFolder);
                    // 加载子文件夹中的所有 GaussianSplatAsset 文件
                    GaussianSplatAsset[] folderAssets =
                        Resources.LoadAll<GaussianSplatAsset>(Path.Combine(Status.SceneFileName, folderName));

                    if (folderAssets is { Length: > 0 })
                    {
                        ExistingGaussianSplatAssets.AddRange(folderAssets);
                    }
                }
            }

            _tipsManager.SetTipText($"load finished. find {ExistingGaussianSplatAssets.Count} results");
        }

        public static void LoadScene(GaussianSplatAsset asset)
        {
            Status.GaussianSplatAssets.Add(asset);
            SceneManager.LoadScene("GSTestScene");
        }
        
    }
}