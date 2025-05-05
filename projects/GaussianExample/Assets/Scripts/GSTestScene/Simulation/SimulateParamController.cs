using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GSTestScene.Simulation
{
    /// <summary>
    /// 用于存放物理模拟参数控制相关操作
    /// </summary>
    public partial class GaussianSimulator
    {
        public Toggle zUpToggle;

        public Slider gravitySlider;
        public TextMeshProUGUI gravityNumber;

        public Slider frameDtSlider;
        public TextMeshProUGUI frameDtNumber;

        public Slider dampingCoefficientSlider;
        public TextMeshProUGUI dampingCoefficientNumber;

        public Slider xpbdRestIterSlider;
        public TextMeshProUGUI xpbdRestIterNumber;

        public Slider groundHeightSlider;
        public TextMeshProUGUI groundHeightNumber;

        public Toggle isRigidToggle;

        public Slider densitySlider;
        public TextMeshProUGUI densityNumber;

        public Slider eSlider;
        public TextMeshProUGUI eNumber;

        public Slider nuSlider;
        public TextMeshProUGUI nuNumber;

        /// <summary>
        /// 初始化UI模拟参数绑定
        /// </summary>
        private void InitializeParamUI()
        {
            zUpToggle.onValueChanged.AddListener(value => { isZUp = value ? 1 : -1; });
            gravitySlider.onValueChanged.AddListener(value =>
            {
                gravityNumber.SetText($"{value:f2}");
                gravity = value;
            });
            frameDtSlider.onValueChanged.AddListener(value =>
            {
                frameDtNumber.SetText($"{value:f4}");
                frameDt = value;
            });
            dampingCoefficientSlider.onValueChanged.AddListener(value =>
            {
                dampingCoefficientNumber.SetText($"{value:f2}");
                dampingCoefficient = value;
            });
            xpbdRestIterSlider.onValueChanged.AddListener(value =>
            {
                xpbdRestIterNumber.SetText($"{value}");
                xpbdRestIter = Mathf.RoundToInt(value);
            });
            groundHeightSlider.onValueChanged.AddListener(value =>
            {
                groundHeightNumber.SetText($"{value:f2}");
                groundHeight = value;
            });
            isRigidToggle.onValueChanged.AddListener(value => defaultIsRigid = value);
            densitySlider.onValueChanged.AddListener(value =>
            {
                densityNumber.SetText($"{value:f2}");
                defaultDensity = value;
            });
            eSlider.onValueChanged.AddListener(value =>
            {
                eNumber.SetText($"{value:f2}");
                defaultE = value;
            });
            nuSlider.onValueChanged.AddListener(value =>
            {
                nuNumber.SetText($"{value:f2}");
                defaultNu = value;
            });
        }
    }
}