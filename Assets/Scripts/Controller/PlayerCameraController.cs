using System;
using UnityEngine;
using CarMirrorSimulator.Core;
using CarMirrorSimulator.Input;

namespace CarMirrorSimulator.Controller
{
    public class PlayerCameraController:MonoBehaviour,IControllable,IMoveable<Vector3>,IRotatable<Vector3>
    {
        [Header("【移動入力】")]
        [SerializeField] private KeyboardAxisBinding _xDir = new (KeyCode.D,KeyCode.A);
        [SerializeField] private KeyboardAxisBinding _yDir = new (KeyCode.E,KeyCode.Q);
        [SerializeField] private KeyboardAxisBinding _zDir = new (KeyCode.W,KeyCode.S);

        [Header("【回転入力】")]
        [SerializeField] private MouseAxisBinding _xAxis = new ("Mouse Y");
        [SerializeField] private MouseAxisBinding _yAxis = new ("Mouse X");

        [Header("【移動速度】")]
        [SerializeField] private float _moveSpeed = 1f;
        [Header("【回転角度】"), Tooltip("1秒間に回転する角度")]
        [SerializeField] private Vector2 _rotateAngles = Vector2.one;

        private bool _isMove = false;

        private Vector3 _moveDir = Vector3.zero;
        private Vector2 _rotateAxis = Vector2.zero;

        private void Update()
        {
            OnUpdateMove();
            OnUpdateRotation();
        }

        /// <summary>
        /// 入力をもとに移動処理
        /// </summary>
        private void OnUpdateMove()
        {
            if (TryGetMoveInput() && _isMove)
            {
                Move(_moveDir * _moveSpeed * Time.deltaTime);
            }
        }

        /// <summary>
        /// 入力をもとに回転処理
        /// </summary>
        private void OnUpdateRotation()
        {
            if (UnityEngine.Input.GetMouseButton(1) && TryGetRotateInput())
            {
                Rotate(_rotateAxis * _rotateAngles * Time.deltaTime);
            }
        }


        /// <summary>
        /// 回転入力取得
        /// </summary>
        private bool TryGetRotateInput()
        {
            _rotateAxis.Set(_xAxis.GetValue(), _yAxis.GetValue());
            return _rotateAxis != Vector2.zero;
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
        public void ActivateControl() => _isMove = true;

        /// <summary>
        /// 操作終了
        /// </summary>
        public void DeactivateControl() => _isMove = false;

        /// <summary>
        /// 原点回転
        /// </summary>
        /// <param name="eulerAngles">回転角度</param>
        public void Rotate(Vector3 eulerAngles)
        {
            // 正規化
            // transform.eulerAnglesは0~360で返すので、そのままclampすると
            // minの設定値を超えるとmax側へ修正されるので、-180~180に収めるようにする。
            float angleX = Util.NormalizeEulerAngle(transform.eulerAngles.x + eulerAngles.x);
            float angleY = Util.NormalizeEulerAngle(transform.eulerAngles.y + eulerAngles.y);

            // 制限付き角度回転
            transform.rotation = Quaternion.Euler(
                Mathf.Clamp(angleX, -180f,180f),
                Mathf.Clamp(angleY, -180f,180f),
                0f
            );
        }

        /// <summary>
        /// 移動処理
        /// </summary>
        public void Move(Vector3 Direction)
        {
            transform.position += Direction;
        }
    }
}