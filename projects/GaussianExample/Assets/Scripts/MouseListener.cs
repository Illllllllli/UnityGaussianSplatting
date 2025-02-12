using System;
using System.Collections;
using System.Collections.Generic;
using GaussianSplatting.Editor;
using GaussianSplatting.Runtime;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

//监听鼠标事件
public class MouseListener : MonoBehaviour
{
    public MouseAction mouseAction;
    public GameObject gaussianSplats;
    public Camera mainCamera;
    private bool isMousePressed = false;
    private GaussianSplatRenderer gsRenderer;
    private Vector2 previousMousePosition = Vector2.zero;

    private void Awake()
    {
        //获取gs渲染对象
        gsRenderer = gaussianSplats.GetComponent<GaussianSplatRenderer>();
        // 初始化鼠标映射
        mouseAction = new MouseAction();
        mouseAction.Enable();
        //为鼠标操作添加匿名回调
        //按下
        mouseAction.Player.PointDown.performed += ctx =>
        {
            isMousePressed = true;
            if (Status._mode == Mode.Select)
            {
                // 检测修饰键
                if (!((Input.GetKey(KeyCode.LeftShift)) || (Input.GetKey(KeyCode.RightShift)) ||
                      (Input.GetKey(KeyCode.LeftControl)) ||
                      (Input.GetKey(KeyCode.RightControl))))
                {
                    gsRenderer.EditDeselectAll();
                }

                gsRenderer.EditStoreSelectionMouseDown();
                GaussianSplatRendererEditor.RepaintAll();
                //更新拖拽的起始坐标
                SelectTool.MouseStartDragPos = Input.mousePosition;
            }

            Debug.Log("point down");
        };
        //抬起
        mouseAction.Player.PointUp.performed += ctx =>
        {
            isMousePressed = false;
            if (Status._mode == Mode.Select)
            {
                SelectTool.MouseStartDragPos = Vector2.zero;
            }

            Debug.Log("point up");
        };
    }


    private void Update()
    {
        //移动

        if (isMousePressed)
        {
            //选择状态下移动
            if (Status._mode == Mode.Select)
            {
                bool isSubtract = ((Input.GetKey(KeyCode.LeftControl)) || (Input.GetKey(KeyCode.RightControl)));
                Vector2 rectStart = SelectTool.MouseStartDragPos;
                Vector2 rectEnd = Input.mousePosition;
                //由于HandleUtility.GUIPointToScreenPixelCoordinate似乎不能在点击停顿的状态下正常使用，
                //所以判断条件需要进行修改,仅在鼠标拖动时更新选区
                if (previousMousePosition == rectEnd)
                {
                    return;
                }
                previousMousePosition = rectEnd;
                //y坐标偏移
                rectStart.y = mainCamera.pixelHeight - rectStart.y;
                rectEnd.y = mainCamera.pixelHeight - rectEnd.y;
                Rect rect = SelectTool.FromToRect(rectStart, rectEnd);
                Vector2 rectMin = HandleUtility.GUIPointToScreenPixelCoordinate(rect.min);
                Vector2 rectMax = HandleUtility.GUIPointToScreenPixelCoordinate(rect.max);
                // Debug.Log(rectMin.ToString()+rectMax.ToString());
                gsRenderer.EditUpdateSelection(rectMin, rectMax, mainCamera, isSubtract);
                GaussianSplatRendererEditor.RepaintAll();
            }
        }
    }

    private void OnDestroy()
    {
        mouseAction?.Disable();
    }
}