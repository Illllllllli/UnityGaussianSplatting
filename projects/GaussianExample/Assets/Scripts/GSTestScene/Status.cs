using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GaussianSplatting.Runtime;
using UnityEngine;
using UnityEngine.EventSystems;

namespace GSTestScene
{
    public class Status : MonoBehaviour
    {
        //灵敏度方位旋转
        private static float _rotateSensitivity;

        public static float rotateSensitivity =>
            isInteractive ? _rotateSensitivity : 0f;

        //灵敏度方位移动
        private static float _translateSensitivity;

        public static float translateSensitivity =>
            isInteractive ? _translateSensitivity : 0f;


        // 检查当前鼠标状态能否与场景交互
        public static bool isInteractive { get; private set; } = true;

        //查看器模式改变时触发回调
        public static event EventHandler<Mode> ModeChanged;

        private static Mode _mode;

        //查看器模式（浏览/选择/编辑）
        public static Mode mode
        {
            get => _mode;
            private set
            {
                if (_mode == value) return;
                _mode = value;
                ModeChanged?.Invoke(_mode, value);
            }
        }

        private static bool isInEditingMode = true;

        //场景文件保存路径
        public const string SceneFileName = "GaussianAssets";

        public static string sceneFileRoot => isInEditingMode
            ? Path.Join(Application.dataPath, "Resources", SceneFileName)
            : Path.Join(Application.persistentDataPath, SceneFileName);

        // 刚切换场景时需要加载的GS资产
        public static List<GaussianSplatAsset> GaussianSplatAssets=new(); 

        public static void UpdateRotateSensitivity(float value)
        {
            _rotateSensitivity = value;
        }

        public static void UpdateTranslateSensitivity(float value)
        {
            _translateSensitivity = value;
        }

        public static void UpdateIsInteractive(bool value)
        {
            isInteractive = value;
        }

        public static void SwitchSelectMode()
        {
            mode = Mode.Select;
            Debug.Log("select");
        }

        public static void SwitchViewMode()
        {
            mode = Mode.View;
            Debug.Log("view");
        }

        public static void SwitchEditMode()
        {
            mode = Mode.Edit;
            // gsRenderer.EditDeselectAll();
            Debug.Log("edit");
        }
    }

    public enum Mode
    {
        View,
        Select,
        Edit
    }

// 选择工具函数类
    internal static class SelectTool
    {
        //根据两个对角坐标创建矩形
        public static Rect FromToRect(Vector2 from, Vector2 to)
        {
            if (from.x > to.x)
                (from.x, to.x) = (to.x, from.x);
            if (from.y > to.y)
                (from.y, to.y) = (to.y, from.y);
            return new Rect(from.x, from.y, to.x - from.x, to.y - from.y);
        }

        /// 判断鼠标的位置是否在UI上
        public static bool IsPointerOverUIObject()
        {
            PointerEventData eventDataCurrentPosition = new PointerEventData(EventSystem.current);
#if !UNITY_EDITOR && (UNITY_ANDROID || UNITY_IPHONE)
        eventDataCurrentPosition.position = Input.GetTouch(0).position;
#else
            eventDataCurrentPosition.position = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
#endif
            List<RaycastResult> results = new List<RaycastResult>();
            if (EventSystem.current)
            {
                EventSystem.current.RaycastAll(eventDataCurrentPosition, results);
            }

            return results.Any(t => t.gameObject.layer == LayerMask.NameToLayer("UI"));
        }
    }
}