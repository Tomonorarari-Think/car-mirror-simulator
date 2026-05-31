using UnityEngine;
using CarMirrorSimulator.Core;

namespace CarMirrorSimulator.Controller
{
    /// <summary>
    /// ミラー制御基底クラス
    /// </summary>
    public abstract class MirrorController : MonoBehaviour, IControllable
    {
        [Header("【回転原点】")]
        [SerializeField] protected Transform _rotateOrigin;

        /// <summary>
        /// 起動時処理
        /// </summary>
        private void Awake()
        {
            Util.AssertNotNull(_rotateOrigin);
            enabled = false;
        }

        /// <summary>
        /// フレーム間更新
        /// </summary>
        private void Update()
        {
            OnUpdateRotation();
        }

        /// <summary>
        /// 入力をもとに回転処理
        /// </summary>
        protected abstract void OnUpdateRotation();

        /// <summary>
        /// 入力取得
        /// </summary>
        /// <returns>入力有無</returns>
        protected abstract bool TryGetRotateInput();

        /// <summary>
        /// 操作開始
        /// </summary>
        public void ActivateControl() => enabled = true;

        /// <summary>
        /// 操作終了
        /// </summary>
        public void DeactivateControl() => enabled = false;
    }
}
// --- EOF ---
