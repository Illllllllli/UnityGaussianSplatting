using GaussianSplatting.Runtime;
using UnityEngine;
using UnityEngine.UI;

namespace GSTestScene
{
    public class UIManager : MonoBehaviour
    {
        public GameObject gaussianSplats;
        public GameObject uiCanvas;
        private GaussianSplatRenderer _gsRenderer;

        public Button viewButton;
        public Button selectButton;
        public Button editButton;

        private static readonly Color DisableColor = Color.white;
        private static readonly Color EnableColor = new Color(0.5f, 0.72f, 0.79f);

        // Start is called before the first frame update
        private void Start()
        {
            //绑定gs对象
            _gsRenderer = gaussianSplats.GetComponent<GaussianSplatRenderer>();
            //配置滚动条属性
            Slider[] sliders = uiCanvas.GetComponentsInChildren<Slider>();
            foreach (var slider in sliders)
            {
                switch (slider.gameObject.name)
                {
                    case "RotationSlider":
                        Status.UpdateRotateSensitivity(slider.value);
                        slider.onValueChanged.AddListener(Status.UpdateRotateSensitivity);
                        break;
                    case "TranslateSlider":
                        Status.UpdateTranslateSensitivity(slider.value);
                        slider.onValueChanged.AddListener(Status.UpdateTranslateSensitivity);
                        break;
                }
            }
            //切换到浏览模式
            SwitchViewMode();
        }

        //按钮使用
        public void SwitchViewMode()
        {
            Status.SwitchViewMode();
            SetButtonColor(viewButton, EnableColor);
            SetButtonColor(selectButton, DisableColor);
            SetButtonColor(editButton, DisableColor);
        }

        public void SwitchSelectMode()
        {
            Status.SwitchSelectMode();
            SetButtonColor(viewButton, DisableColor);
            SetButtonColor(selectButton, EnableColor);
            SetButtonColor(editButton, DisableColor);
        }

        public void SwitchEditMode()
        {
            Status.SwitchEditMode(_gsRenderer);
            SetButtonColor(viewButton, DisableColor);
            SetButtonColor(selectButton, DisableColor);
            SetButtonColor(editButton, EnableColor);
        }

        //更换按钮颜色
        private static void SetButtonColor(Button button, Color color)
        {
            Debug.Log(button);
            button.GetComponent<Image>().color = color;
        }
    }
}