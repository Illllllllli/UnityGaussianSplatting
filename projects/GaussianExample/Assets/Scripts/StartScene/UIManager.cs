using UnityEngine;

namespace StartScene
{
    public class UIManager : MonoBehaviour
    {
        public static void ExitGame()
        {
            Application.Quit();
#if UNITY_EDITOR
            // 在Unity编辑器中，此调用没有效果
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }
    }
}