using System;

namespace CarMirrorSimulator.Input
{
    /// <summary>
    /// キーボード入力
    /// </summary>
    [Serializable]
    public class KeyboardAxisBinding : AxisBinding
    {
        /// <summary>
        /// コンストラクタ
        /// </summary>
        public KeyboardAxisBinding(UnityEngine.KeyCode posi, UnityEngine.KeyCode nega)
        {
            positive = posi;
            negative = nega;
        }

        /// <summary>
        /// プラス入力
        /// </summary>
        public UnityEngine.KeyCode positive;

        /// <summary>
        /// マイナス入力
        /// </summary>
        public UnityEngine.KeyCode negative;

        /// <summary>
        /// 入力取得
        /// </summary>
        public override float GetValue() =>
            (UnityEngine.Input.GetKey(positive) ? 1f : 0f) - (UnityEngine.Input.GetKey(negative) ? 1f : 0f);
    }
}
// --- EOF ---
