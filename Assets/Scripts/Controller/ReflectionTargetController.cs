using UnityEngine;
using CarMirrorSimulator.Core;
using CarMirrorSimulator.Input;

namespace CarMirrorSimulator.Controller
{
    /// <summary>
    /// 鏡に映るオブジェクト操作
    /// </summary>
    public class ReflectionTargetController : MonoBehaviour, IControllable, IMoveable<Vector3>
    {
        [Header("【オブジェクト】")]
        [SerializeField] private Transform _target;

        [Header("【入力】")]
        [SerializeField] private KeyboardAxisBinding _xDir = new(KeyCode.D, KeyCode.A);
        [SerializeField] private KeyboardAxisBinding _zDir = new(KeyCode.W, KeyCode.S);
        [SerializeField] private KeyboardAxisBinding _yDir = new(KeyCode.E, KeyCode.Q);

        [Header("【移動速度】")]
        [SerializeField] private float _moveSpeed = 1f;

        private Vector3 _moveDir = Vector3.zero;
        private Vector3 _initPos = Vector3.zero;

        /// <summary>
        /// 起動時処理
        /// </summary>
        private void Awake()
        {
            Util.AssertNotNull(_target);
            _initPos = _target.position;
            enabled = false;
        }

        /// <summary>
        /// フレーム間更新
        /// </summary>
        private void Update()
        {
            OnUpdateMove();
        }

        /// <summary>
        /// 入力をもとに移動処理
        /// </summary>
        private void OnUpdateMove()
        {
            if (TryGetMoveInput())
            {
                Move(_moveDir * _moveSpeed * Time.deltaTime);
            }
        }

        /// <summary>
        /// 移動入力取得
        /// </summary>
        private bool TryGetMoveInput()
        {
            _moveDir.Set(_xDir.GetValue(), _yDir.GetValue(), _zDir.GetValue());
            return _moveDir != Vector3.zero;
        }

        /// <summary>
        /// 操作開始
        /// </summary>
        public void ActivateControl() => enabled = true;

        /// <summary>
        /// 操作終了
        /// </summary>
        public void DeactivateControl() => enabled = false;

        /// <summary>
        /// 移動処理
        /// </summary>
        public void Move(Vector3 Direction)
        {
            transform.position += Direction;
        }

        /// <summary>
        /// 座標初期化
        /// </summary>
        public void PositionReset()
        {
            _target.position = _initPos;
        }
    }
}
// --- EOF ---
