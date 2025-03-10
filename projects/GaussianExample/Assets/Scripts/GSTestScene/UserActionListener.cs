using GaussianSplatting.Editor;
using GaussianSplatting.Runtime;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

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

        private GaussianSplatRenderer _gsRenderer;
        private Vector2 _previousMousePosition = Vector2.zero;

        private UserAction _userAction;
        // 框选GS时，记录鼠标开始拖动时的坐标
        private Vector2 _mouseStartSelectPos = Vector2.zero; 
        // 位移编辑中，记录鼠标单次移动的累积量
        private Vector2 _mouseTranslateAccumulation = Vector2.zero;
        /// <summary>
        /// 绑定鼠标输入
        /// </summary>
        private void BindMouseInput()
        {
            // 鼠标左键按下
            _userAction.Player.MouseLeftClick.performed += ctx =>
            {
                _isMousePressed = true;
                //当前为选择模式且鼠标不在UI上且不是在编辑状态下
                if (Status.playMode == PlayMode.Select &&Status.editMode==EditMode.None&& !GsTools.IsPointerOverUIObject())
                {
                    // 检测修饰键
                    if (!((Input.GetKey(KeyCode.LeftShift)) || (Input.GetKey(KeyCode.RightShift)) ||
                          (Input.GetKey(KeyCode.LeftControl)) ||
                          (Input.GetKey(KeyCode.RightControl))))
                    {
                        _gsRenderer.EditDeselectAll();
                    }

                    _gsRenderer.EditStoreSelectionMouseDown();
                    GaussianSplatRendererEditor.RepaintAll();
                    //更新拖拽的起始坐标
                    _mouseStartSelectPos = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
                }

                Debug.Log("point down");
            };
            // 鼠标左键抬起
            _userAction.Player.MouseLeftClick.canceled += ctx =>
            {
                _isMousePressed = false;
                if (Status.playMode == PlayMode.Select)
                {
                    _mouseStartSelectPos = Vector2.zero;
                }

                Debug.Log("point up");
            };
            // 监听鼠标移动
            _userAction.Player.MouseMovement.performed += ctx =>
            {
                if (!_isMousePressed) return;
                if (!Status.isInteractive) return;
                //选择模式下移动（选择GS）
                if (Status.playMode == PlayMode.Select && Status.editMode==EditMode.None)
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
                    _gsRenderer.EditUpdateSelection(rectMin, rectMax, mainCamera, isSubtract);
                    GaussianSplatRendererEditor.RepaintAll();
                }
                //其他模式下旋转视角
                else
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
            
            // 鼠标滚轮滑动
            _userAction.Player.MouseWheel.performed += ctx =>
            {
                float scrollDelta = ctx.ReadValue<Vector2>().y;
                // 反转坐标轴
                float newFoV = mainCamera.fieldOfView - (scrollDelta / Status.MouseScrollBase);
                if (newFoV is >= Status.CameraFovMin and <= Status.CameraFovMax)
                {
                    mainCamera.fieldOfView = newFoV;
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
            _userAction.Player.SwitchEditMode.performed += ctx => { Status.SwitchEditMode(); };
        }
        /// <summary>
        /// 绑定移动输入
        /// </summary>
        private void BindMovementInput()
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
                    _gsRenderer.EditInvertSelection();
                }
            };
            _userAction.Player.CtrlA.performed += ctx =>
            {
                if (Status.playMode == PlayMode.Select)
                {
                    _gsRenderer.EditSelectAll();
                }
            };
            _userAction.Player.CtrlShiftA.performed += ctx =>
            {
                if (Status.playMode == PlayMode.Select)
                {
                    _gsRenderer.EditDeselectAll();
                }
            };
            _userAction.Player.Delete.performed += ctx =>
            {
                if (Status.playMode == PlayMode.Select)
                {
                    _gsRenderer.EditDeleteSelected();
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
                if (Status.editMode == EditMode.Translate)
                {
                    // 获取鼠标偏移
                    Vector2 mouseDelta = ctx.ReadValue<Vector2>();
                    // 加入累积量
                    _mouseTranslateAccumulation += mouseDelta;
                    // 根据相机参数将偏移转换为世界空间位移
                    Vector3 deltaWorld = GsTools.ScreenDeltaToWorldDelta(mouseDelta, mainCamera);
                    // 转换为本地空间位移
                    Vector3 deltaLocal = _gsRenderer.transform.InverseTransformDirection(deltaWorld);
                    // 应用位移
                    _gsRenderer.EditTranslateSelection(deltaLocal);
                }
            };
            _userAction.Player.MouseLeftClick.performed += ctx =>
            {
                // 在位移编辑模式下，点击鼠标左键以结束位移编辑
                if (Status.editMode == EditMode.Translate)
                {
                    Status.EndSelectEdit();
                    // 清空位移累积量
                    _mouseTranslateAccumulation=Vector2.zero;
                }
            };
            // 对选定区域进行位移操作
            _userAction.Player.Gkey.performed += ctx =>
            {
                if (Status.playMode != PlayMode.Select) return;
                // 切换到位移模式
                Status.StartSelectTranslateEdit();
            };
        }
        
        private void OnEnable()
        {
            //获取gs渲染对象
            _gsRenderer = gaussianSplats.GetComponent<GaussianSplatRenderer>();
            // 初始化
            _userAction = new UserAction();
            _userAction.Enable();
            BindMouseInput();
            BindModeChangeInput();
            BindMovementInput();
            BindSelectFuncInput();
            BindSelectModeInput();
        }


        private void Update()
        {
            // 判断当前是否有修饰键
            if (Keyboard.current.shiftKey.isPressed || Keyboard.current.ctrlKey.isPressed) return;
            //相机移动
            if (Vector3.Magnitude(_cameraMovement) > 0.1f)
            {
                // 考虑FoV对移动速度的影响
                float fovRatio = Mathf.Pow(mainCamera.fieldOfView / Status.BaseFoV,0.8f);
                //按本地坐标移动
                mainCamera.transform.Translate(fovRatio * Status.translateSensitivity*_cameraMovement );
            }
        }

        private void OnDestroy()
        {
            _userAction?.Disable();
        }
    }
}