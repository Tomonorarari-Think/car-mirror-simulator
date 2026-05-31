using System;

namespace CarMirrorSimulator.Input
{
    /// <summary>
    /// マウス入力
    /// </summary>
    [Serializable]
    public class MouseAxisBinding : AxisBinding
    {
        public MouseAxisBinding(string name)
        {
            axisName = name;
        }

        /// <summary>
        /// 旧InputSystemマウス入力取得名
        /// </summary>
        public string axisName = "Mouse X"; // "Mouse X" or "Mouse Y"

        /// <summary>
        /// マウス入力倍率
        /// </summary>
        public float sensitivity = 1f;

        /// <summary>
        /// 入力取得
        /// </summary>
        public override float GetValue() => UnityEngine.Input.GetAxis(axisName) * sensitivity;
    }
}
// --- EOF ---
