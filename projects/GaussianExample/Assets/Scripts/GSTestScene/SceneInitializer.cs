using System;
using GaussianSplatting.Runtime;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

namespace GSTestScene
{
    public class SceneInitializer : MonoBehaviour
    {
        public GameObject gaussianSplatScenePrefab;

        // Start is called before the first frame update
        private void Awake()
        {
            if (Status.GaussianSplatAssets.Count == 0) return;
            //场景刚加载时调用
            foreach (GaussianSplatAsset gaussianSplatAsset in Status.GaussianSplatAssets)
            {
                GameObject gaussianSplats = Instantiate(gaussianSplatScenePrefab);
                gaussianSplats.GetComponent<GaussianSplatRenderer>().m_Asset = gaussianSplatAsset;
                // 为其他脚本设置GS对象
                // （由于其他脚本还没有适配多个物体，先这么写）
                GetComponent<MainUIManager>().gaussianSplats = gaussianSplats;
                GetComponent<UserActionListener>().gaussianSplats = gaussianSplats;
                GetComponent<EditManager>().gaussianSplats = gaussianSplats;
            }

            GetComponent<MainUIManager>().enabled = true;
            GetComponent<UserActionListener>().enabled = true;
            Status.SwitchViewMode();
        }
    }
}