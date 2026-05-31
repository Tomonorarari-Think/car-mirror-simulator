using UnityEngine;

namespace CarMirrorSimulator.Mirror
{
    /// <summary>
    /// 車載ミラーの反射面を表すコンポーネント。
    /// ミラー平面の位置・法線を <see cref="MirrorCamera"/> に提供し、
    /// 生成された RenderTexture をマテリアルに MaterialPropertyBlock 経由でバインドする。
    /// </summary>
    /// <remarks>
    /// セットアップ手順:
    /// 1. ミラーメッシュの GameObject に本スクリプトを追加する。
    /// 2. MirrorCamera を Inspector で設定する。
    /// 3. ミラーメッシュのマテリアルに MirrorSurface シェーダーを設定する。
    /// 4. Inspector の「ローカル法線」でミラー面の向きを設定する。
    ///    - Quad (デフォルト向き): Vector3.forward (0, 0, 1)
    ///    - Plane (デフォルト向き): Vector3.up (0, 1, 0)
    /// </remarks>
    [RequireComponent(typeof(Renderer))]
    [AddComponentMenu("CarMirror/Mirror Surface")]
    public class MirrorSurface : MonoBehaviour
    {
        [Header("参照")]
        [Tooltip("このミラー面に対応する MirrorCamera")]
        [SerializeField] private MirrorCamera _mirrorCamera;

        [Header("ミラー平面")]
        [Tooltip("反射面の法線方向（ローカル空間）。\n" +
                 "Quad: (0, 0, 1)  ← デフォルト\n" +
                 "Plane: (0, 1, 0)")]
        [SerializeField] private Vector3 _localNormal = Vector3.forward;

        [Header("マテリアル設定")]
        [Tooltip("シェーダー側で RenderTexture を受け取るプロパティ名")]
        [SerializeField] private string _texturePropertyName = "_MirrorTex";

        // ---- 内部状態 ----
        private Renderer _renderer;

        // GC を減らすため MaterialPropertyBlock をキャッシュして再利用する
        private MaterialPropertyBlock _propertyBlock;

        // ---- プロパティ ----

        /// <summary>ミラー平面上の代表点（ワールド空間）。transform.position を返す。</summary>
        public Vector3 MirrorPosition => transform.position;

        /// <summary>
        /// ミラー反射面の法線（ワールド空間）。
        /// <see cref="_localNormal"/> をワールド空間へ変換して返す。
        /// Quad は (0,0,1)、Plane は (0,1,0) を設定すること。
        /// </summary>
        public Vector3 MirrorNormal => transform.TransformDirection(_localNormal).normalized;

        /// <summary>
        /// ミラーメッシュが現在フレームでカメラに映っているかどうか。
        /// Renderer.isVisible は前フレームの可視性を返すが、カリング判定として十分な精度を持つ。
        /// </summary>
        public bool IsVisible => _renderer != null && _renderer.isVisible;

        // ---- Unity ライフサイクル ----

        private void Awake()
        {
            _renderer = GetComponent<Renderer>();
            _propertyBlock = new MaterialPropertyBlock();
        }

        private void Start()
        {
            // 起動時に RenderTexture をマテリアルへバインドする
            RefreshRenderTexture();
        }

        // ---- 公開メソッド ----

        /// <summary>
        /// MirrorCamera (行列方式) の RenderTexture が変更されたとき（解像度変更など）に
        /// 外部から呼び出してマテリアルプロパティを更新する。
        /// </summary>
        public void RefreshRenderTexture()
        {
            if (_mirrorCamera == null || _mirrorCamera.RenderTexture == null) return;
            SetRenderTexture(_mirrorCamera.RenderTexture);
        }

        /// <summary>
        /// 任意の RenderTexture をマテリアルへバインドする。
        /// MirrorCameraVector（ベクトル方式）など、行列方式以外のカメラからも呼び出せる。
        /// </summary>
        /// <param name="renderTexture">バインドする RenderTexture</param>
        public void SetRenderTexture(RenderTexture renderTexture)
        {
            if (renderTexture == null) return;

            // MaterialPropertyBlock 経由で設定することで、
            // マテリアルアセット本体を変更せずにインスタンスごとにテクスチャを上書きする
            _renderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetTexture(_texturePropertyName, renderTexture);
            _renderer.SetPropertyBlock(_propertyBlock);
        }

#if UNITY_EDITOR
        /// <summary>
        /// シーンビューでミラー面の法線方向を Gizmo で可視化する。
        /// 青矢印: ミラー法線（カメラが反射する方向）
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            Vector3 worldNormal = transform.TransformDirection(_localNormal).normalized;
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(transform.position, worldNormal * 0.3f);

            Gizmos.color = new Color(0f, 0.5f, 1f, 0.3f);
            // ミラー面を半透明の四角形で可視化する
            Vector3 right = transform.right * transform.localScale.x * 0.5f;
            Vector3 up = transform.up * transform.localScale.y * 0.5f;
            Vector3 p = transform.position;
            Gizmos.DrawLine(p - right - up, p + right - up);
            Gizmos.DrawLine(p + right - up, p + right + up);
            Gizmos.DrawLine(p + right + up, p - right + up);
            Gizmos.DrawLine(p - right + up, p - right - up);
        }
#endif
    }
}
