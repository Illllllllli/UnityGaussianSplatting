using System;
using GaussianSplatting.Editor;
using GaussianSplatting.Runtime;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

//监听鼠标事件
namespace GSTestScene
{
    public class UserActionListener : MonoBehaviour
    {
        //gs对象和相机
        public GameObject gaussianSplats;
        public Camera mainCamera;

        private bool _isMousePressed;
        private Vector3 _cameraMovement = Vector3.zero;

        private GaussianSplatRenderer gsRenderer => gaussianSplats?.GetComponent<GaussianSplatRenderer>();
        private Vector2 _previousMousePosition = Vector2.zero;

        private UserAction _userAction;

        // 框选GS时，记录鼠标开始拖动时的坐标
        private Vector2 _mouseStartSelectPos = Vector2.zero;

        // 位移编辑中，记录鼠标单次移动的累积量
        private Vector2 _mouseTranslateAccumulation = Vector2.zero;

        // 旋转编辑中，记录鼠标开始移动时的旋转体中心坐标，相机前轴和累计旋转量
        private Vector3 _selectedCenterRotate = Vector3.positiveInfinity;
        private Vector3 _cameraForwardRotate = Vector3.positiveInfinity;

        private float _mouseRotateAccumulation = 0f;

        // 缩放编辑中，记录鼠标开始移动时的缩放体中心坐标和累计缩放量
        private Vector3 _selectedCenterScale = Vector3.positiveInfinity;
        private float _mouseScaleAccumulation = 1f;

        /// <summary>
        /// 绑定框选区域更新相关输入
        /// </summary>
        private void BindSelectAreaInput()
        {
            // 鼠标左键按下
            _userAction.Player.MouseLeftClick.performed += ctx =>
            {
                _isMousePressed = true;
                //当前为选择模式且鼠标不在UI上且不是在编辑状态下
                if (Status.playMode == PlayMode.Select && Status.selectEditMode == SelectEditMode.None &&
                    !GsTools.IsPointerOverUIObject())
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
                    _mouseStartSelectPos = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
                }
            };
            // 鼠标左键抬起
            _userAction.Player.MouseLeftClick.canceled += ctx =>
            {
                _isMousePressed = false;
                if (Status.playMode == PlayMode.Select)
                {
                    _mouseStartSelectPos = Vector2.zero;
                }
            };
            // 监听鼠标移动
            _userAction.Player.MouseMovement.performed += ctx =>
            {
                if (!_isMousePressed) return;
                if (Status.isInteractive < 0) return;
                if (GsTools.IsPointerOverUIObject()) return;
                //选择模式下移动（选择GS）
                if (Status.playMode == PlayMode.Select && Status.selectEditMode == SelectEditMode.None)
                {
                    bool isSubtract = ((Input.GetKey(KeyCode.LeftControl)) || (Input.GetKey(KeyCode.RightControl)));
                    Vector2 rectStart = _mouseStartSelectPos;
                    Vector2 rectEnd = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
                    //由于HandleUtility.GUIPointToScreenPixelCoordinate似乎不能在点击停顿的状态下正常使用，
                    //所以判断条件需要进行修改,仅在鼠标拖动时更新选区

                    if (_previousMousePosition.Equals(rectEnd))
                    {
                        return;
                    }

                    _previousMousePosition = rectEnd;

                    //y坐标偏移
                    rectStart.y = mainCamera.pixelHeight - rectStart.y;
                    rectEnd.y = mainCamera.pixelHeight - rectEnd.y;
                    Rect rect = GsTools.FromToRect(rectStart, rectEnd);
                    Vector2 rectMin = HandleUtility.GUIPointToScreenPixelCoordinate(rect.min);
                    Vector2 rectMax = HandleUtility.GUIPointToScreenPixelCoordinate(rect.max);
                    gsRenderer.EditUpdateSelection(rectMin, rectMax, mainCamera, isSubtract);
                    GaussianSplatRendererEditor.RepaintAll();
                }
            };
        }

        /// <summary>
        /// 绑定模式切换输入
        /// </summary>
        private void BindModeChangeInput()
        {
            //切换模式
            _userAction.Player.SwitchViewMode.performed += ctx => { Status.SwitchViewMode(); };
            _userAction.Player.SwitchSelectMode.performed += ctx => { Status.SwitchSelectMode(); };
        }

        /// <summary>
        /// 绑定相机变换输入
        /// </summary>
        private void BindCameraTransformInput()
        {
            //移动视角(x,y,z分别为左右，上下，前后)
            _userAction.Player.Akey.performed += ctx => _cameraMovement.x--;
            _userAction.Player.Akey.canceled += ctx => _cameraMovement.x++;
            _userAction.Player.Dkey.performed += ctx => _cameraMovement.x++;
            _userAction.Player.Dkey.canceled += ctx => _cameraMovement.x--;
            _userAction.Player.Wkey.performed += ctx => _cameraMovement.z++;
            _userAction.Player.Wkey.canceled += ctx => _cameraMovement.z--;
            _userAction.Player.Skey.performed += ctx => _cameraMovement.z--;
            _userAction.Player.Skey.canceled += ctx => _cameraMovement.z++;
            _userAction.Player.Ekey.performed += ctx => _cameraMovement.y++;
            _userAction.Player.Ekey.canceled += ctx => _cameraMovement.y--;
            _userAction.Player.Qkey.performed += ctx => _cameraMovement.y--;
            _userAction.Player.Qkey.canceled += ctx => _cameraMovement.y++;

            // 鼠标滚轮滑动更改相机FoV
            _userAction.Player.MouseWheel.performed += ctx =>
            {
                if (Status.isInteractive < 0) return;
                float scrollDelta = ctx.ReadValue<Vector2>().y;
                // 反转坐标轴
                float newFoV = mainCamera.fieldOfView - (scrollDelta / Status.MouseScrollBase);
                if (newFoV is >= Status.CameraFovMin and <= Status.CameraFovMax)
                {
                    mainCamera.fieldOfView = newFoV;
                }
            };
            //旋转视角
            _userAction.Player.MouseMovement.performed += ctx =>
            {
                if (!_isMousePressed) return;
                if (Status.isInteractive < 0) return;
                if (GsTools.IsPointerOverUIObject()) return;
                if (Status.playMode == PlayMode.Select) return;

                {
                    Vector2 mouseDelta = ctx.ReadValue<Vector2>();

                    mainCamera.transform.Rotate(-mouseDelta.y * Status.rotateSensitivity,
                        mouseDelta.x * Status.rotateSensitivity, 0);
                    //消去z轴分量
                    Vector3 angles = mainCamera.transform.localEulerAngles;
                    angles.z = 0;
                    mainCamera.transform.localEulerAngles = angles;
                }
            };
        }

        /// <summary>
        /// 绑定快捷编辑功能（如Ctrl+A,Ctrl+I)
        /// </summary>
        private void BindSelectFuncInput()
        {
            _userAction.Player.CtrlI.performed += ctx =>
            {
                if (Status.playMode == PlayMode.Select)
                {
                    gsRenderer.EditInvertSelection();
                }
            };
            _userAction.Player.CtrlA.performed += ctx =>
            {
                if (Status.playMode == PlayMode.Select)
                {
                    gsRenderer.EditSelectAll();
                }
            };
            _userAction.Player.CtrlShiftA.performed += ctx =>
            {
                if (Status.playMode == PlayMode.Select)
                {
                    gsRenderer.EditDeselectAll();
                }
            };
            _userAction.Player.Delete.performed += ctx =>
            {
                if (Status.playMode == PlayMode.Select)
                {
                    gsRenderer.EditDeleteSelected();
                }
            };
        }

        /// <summary>
        /// 绑定编辑模式输入
        /// </summary>
        private void BindSelectModeInput()
        {
            // 绑定鼠标监听输入
            _userAction.Player.MouseMovement.performed += ctx =>
            {
                if (Status.isInteractive < 0) return;
                if (Status.selectEditMode == SelectEditMode.None) return;
                // 获取鼠标偏移
                Vector2 mouseDelta = ctx.ReadValue<Vector2>();
                switch (Status.selectEditMode)
                {
                    case SelectEditMode.Translate:
                        // 加入累积量
                        _mouseTranslateAccumulation += mouseDelta;
                        // 根据相机参数将偏移转换为世界空间位移
                        Vector3 deltaWorld = GsTools.ScreenDeltaToWorldDelta(mouseDelta, mainCamera);
                        // 转换为本地空间位移
                        Vector3 deltaLocal = gsRenderer.transform.InverseTransformDirection(deltaWorld);
                        // 应用位移
                        gsRenderer.EditTranslateSelection(deltaLocal);
                        break;
                    case SelectEditMode.Rotate:
                        // 加入累积量
                        // 鼠标位移的X轴用于旋转
                        _mouseRotateAccumulation += mouseDelta.x;
                        gsRenderer.EditStorePosMouseDown();
                        gsRenderer.EditStoreOtherMouseDown();
                        gsRenderer.EditRotateSelection(_selectedCenterRotate, gsRenderer.transform.localToWorldMatrix,
                            gsRenderer.transform.worldToLocalMatrix,
                            Quaternion.AngleAxis(mouseDelta.x * Status.EditRotateSensitivity, _cameraForwardRotate));
                        break;
                    case SelectEditMode.Scale:
                        // 加入累积量
                        // 鼠标位移的Y轴用于缩放
                        _mouseScaleAccumulation *= mouseDelta.y;
                        gsRenderer.EditStorePosMouseDown();
                        gsRenderer.EditStoreOtherMouseDown();
                        float scale =1+ Status.EditScaleSensitivity * mouseDelta.y;
                        Vector3 scaleVector = new Vector3(scale, scale, scale);
                        gsRenderer.EditScaleSelection(_selectedCenterScale, gsRenderer.transform.localToWorldMatrix,
                            gsRenderer.transform.worldToLocalMatrix, scaleVector);
                        break;
                    case SelectEditMode.None:
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            };
            _userAction.Player.MouseLeftClick.performed += ctx =>
            {
                // 在编辑过程中，点击鼠标左键以结束位移编辑
                if (Status.selectEditMode != SelectEditMode.None)
                {
                    Status.EndSelectEdit();
                    // 清空累积量
                    _mouseTranslateAccumulation = Vector2.zero;
                    _selectedCenterRotate = Vector3.positiveInfinity;
                    _cameraForwardRotate = Vector3.positiveInfinity;
                    _mouseRotateAccumulation = 0f;
                    _mouseScaleAccumulation = 1f;
                }
            };
            // 对选定区域开始位移操作
            _userAction.Player.Gkey.performed += ctx =>
            {
                // 判断
                if (Status.playMode != PlayMode.Select) return;
                if (Status.isInteractive < 0) return;
                // 切换到位移模式
                Status.StartSelectTranslateEdit();
            };
            _userAction.Player.Rkey.performed += ctx =>
            {
                // 判断
                if (Status.playMode != PlayMode.Select) return;
                if (Status.isInteractive < 0) return;
                // 开始旋转前的记录
                _selectedCenterRotate = GsTools.GetSelectionCenterLocal(gsRenderer);
                _cameraForwardRotate = mainCamera.transform.forward;
                // 切换到旋转模式
                Status.StartSelectRotateEdit();
            };
            _userAction.Player.Tkey.performed += ctx =>
            {
                // 判断
                if (Status.playMode != PlayMode.Select) return;
                if (Status.isInteractive < 0) return;
                // 开始缩放前的记录
                _selectedCenterScale = GsTools.GetSelectionCenterLocal(gsRenderer);
                // 切换到旋转模式
                Status.StartSelectScaleEdit();
            };
        }

        /// <summary>
        /// 绑定其他输入（如打开编辑面板/弹出回到主菜单页面等）
        /// </summary>
        private void BindOtherInput()
        {
            _userAction.Player.ShowEditPanel.performed += ctx => GetComponent<EditManager>().HandleEditClick();
            _userAction.Player.Escape.performed += ctx => GetComponent<MainUIManager>().SetBackCheckPanelActive(true);
        }


        private void Start()
        {
            Debug.Log(Status.isInteractive);
            // 初始化
            _userAction = new UserAction();
            _userAction.Enable();
            BindSelectAreaInput();
            BindModeChangeInput();
            BindCameraTransformInput();
            BindSelectFuncInput();
            BindSelectModeInput();
            BindOtherInput();
        }


        /// <summary>
        /// 在这里面进行相机移动
        /// </summary>
        private void Update()
        {
            // 判断当前是否有修饰键
            if (Keyboard.current.shiftKey.isPressed || Keyboard.current.ctrlKey.isPressed) return;
            if (Status.isInteractive < 0) return;
            //相机移动
            if (Vector3.Magnitude(_cameraMovement) > 0.1f)
            {
                // 考虑FoV对移动速度的影响
                float fovRatio = Mathf.Pow(mainCamera.fieldOfView / Status.BaseFoV, 0.8f);
                //按本地坐标移动
                mainCamera.transform.Translate(fovRatio * Status.translateSensitivity * _cameraMovement);
            }
        }

        private void OnDestroy()
        {
            _userAction?.Disable();
        }
    }
}