using UnityEngine;
using CarMirrorSimulator.Core;
using CarMirrorSimulator.Input;

namespace CarMirrorSimulator.Controller
{
    /// <summary>
    /// サイドミラー制御
    /// </summary>
    public class SideMirrorController : MirrorController, IRotatable<Vector2>
    {
        [Header("【入力】")]
        [SerializeField] private KeyboardAxisBinding _xAxis = new(KeyCode.W, KeyCode.S);
        [SerializeField] private KeyboardAxisBinding _yAxis = new(KeyCode.D, KeyCode.A);

        [Header("【角度制限】")]
        [SerializeField] private Vector2 _rotMinEulerAngles = Vector2.one * (-Mathf.PI * Mathf.Rad2Deg);
        [SerializeField] private Vector2 _rotMaxEulerAngles = Vector2.one * (Mathf.PI * Mathf.Rad2Deg);

        [Header("【回転角度】"), Tooltip("1秒間に回転する角度")]
        [SerializeField] private Vector2 _rotateAngles = Vector2.one;

        private Vector2 _rotateAxis = Vector2.zero;

        /// <summary>
        /// 入力をもとに回転処理
        /// </summary>
        protected override void OnUpdateRotation()
        {
            if (TryGetRotateInput())
            {
                Rotate(_rotateAxis * _rotateAngles * Time.deltaTime);
            }
        }

        /// <summary>
        /// 回転入力取得
        /// </summary>
        protected override bool TryGetRotateInput()
        {
            _rotateAxis.Set(_xAxis.GetValue(), _yAxis.GetValue());
            return _rotateAxis != Vector2.zero;
        }

        /// <summary>
        /// 原点回転
        /// </summary>
        /// <param name="eulerAngles">回転角度</param>
        public void Rotate(Vector2 eulerAngles)
        {
            // 正規化
            // transform.eulerAnglesは0~360で返すので、そのままclampすると
            // minの設定値を超えるとmax側へ修正されるので、-180~180に収めるようにする。
            float angleX = Util.NormalizeEulerAngle(_rotateOrigin.eulerAngles.x + eulerAngles.x);
            float angleY = Util.NormalizeEulerAngle(_rotateOrigin.eulerAngles.y + eulerAngles.y);

            // 制限付き角度回転
            _rotateOrigin.rotation = Quaternion.Euler(
                Mathf.Clamp(angleX, _rotMinEulerAngles.x, _rotMaxEulerAngles.x),
                Mathf.Clamp(angleY, _rotMinEulerAngles.y, _rotMaxEulerAngles.y),
                0f
            );
        }
    }
}
// --- EOF ---
