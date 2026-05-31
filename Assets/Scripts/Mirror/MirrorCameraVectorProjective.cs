using UnityEngine;

namespace CarMirrorSimulator.Mirror
{
    /// <summary>
    /// ベクトル演算方式のミラーカメラ制御コンポーネント（プロジェクティブ UV 対応版）。
    /// MirrorCameraVector の機能に加え、MirrorSurfaceVector シェーダーへの
    /// VP 行列・RenderTexture のバインドを毎フレーム自動で行う。
    /// </summary>
    /// <remarks>
    /// MirrorCameraVector との違い:
    /// - _MirrorVP（プロジェクティブ UV 用 VP 行列）を MaterialPropertyBlock で設定する
    /// - _MirrorTex（RenderTexture）を MaterialPropertyBlock で設定する
    /// - Renderer の参照が追加で必要
    ///
    /// セットアップ手順:
    /// 1. シーンにミラーカメラ用 GameObject を作成し Camera コンポーネントを追加する
    /// 2. カメラの Target Texture に事前作成した RenderTexture を設定する
    /// 3. ミラーメッシュのマテリアルに CarMirror/MirrorSurfaceVector を設定する
    /// 4. 任意の GameObject に本スクリプトを追加し、Inspector で参照を設定する
    /// </remarks>
    [AddComponentMenu("CarMirror/Mirror Camera (Vector Projective)")]
    public class MirrorCameraVectorProjective : MonoBehaviour
    {
        // Shader.PropertyToID はランタイムで一度だけ解決する
        private static readonly int s_MirrorVP  = Shader.PropertyToID("_MirrorVP");
        private static readonly int s_MirrorTex = Shader.PropertyToID("_MirrorTex");

        // ---- Inspector 設定 ----

        [Header("参照")]
        [Tooltip("反射計算の基準となるメインカメラ（未設定時は Camera.main を使用）")]
        [SerializeField] private Camera _mainCamera;

        [Tooltip("制御するミラーカメラ（Target Texture に RenderTexture を設定しておく）")]
        [SerializeField] private Camera _mirrorCamera;

        [Tooltip("ミラー面の Transform（Quad や Plane の Transform を設定する）")]
        [SerializeField] private Transform _mirrorTransform;

        [Tooltip("MirrorSurfaceVector マテリアルを持つミラーメッシュの Renderer")]
        [SerializeField] private Renderer _mirrorRenderer;

        [Header("ミラー法線")]
        [Tooltip("ミラー面の法線方向（ローカル空間）\n" +
                 "・Quad（デフォルト向き）: (0, 0, 1)\n" +
                 "・Plane（デフォルト向き）: (0, 1, 0)")]
        [SerializeField] private Vector3 _localNormal = Vector3.forward;

        [Header("微調整")]
        [Tooltip("ミラーカメラをミラー面から手前に配置するオフセット距離（m）\n" +
                 "ニアクリップ面がミラー面と交差するのを防ぐ")]
        [SerializeField, Min(0f)] private float _cameraOffset = 0.01f;

        [Tooltip("ジンバルロック検出の閾値\n" +
                 "反射ベクトルと up ベクトルの内積の絶対値がこの値を超えると\n" +
                 "up を mirrorTransform.right に切り替える")]
        [SerializeField, Range(0.9f, 1f)] private float _gimbalLockThreshold = 0.99f;

        // ---- 内部状態 ----

        // GC を減らすため MaterialPropertyBlock をキャッシュして再利用する
        private MaterialPropertyBlock _propertyBlock;

        // ---- Unity ライフサイクル ----

        private void Awake()
        {
            if (_mainCamera == null)
                _mainCamera = Camera.main;

            _propertyBlock = new MaterialPropertyBlock();
        }

        private void LateUpdate()
        {
            if (_mainCamera == null || _mirrorCamera == null || _mirrorTransform == null)
                return;

            UpdateMirrorCamera();
            UpdateShaderProperties();
        }

        // ---- シェーダープロパティ更新 ----

        /// <summary>
        /// MirrorSurfaceVector シェーダーに必要なプロパティを毎フレーム設定する。
        /// _MirrorVP: ミラーカメラの GPU 用 VP 行列（プロジェクティブ UV の基準）
        /// _MirrorTex: ミラーカメラの RenderTexture
        /// </summary>
        private void UpdateShaderProperties()
        {
            if (_mirrorRenderer == null) return;

            // GL.GetGPUProjectionMatrix で DirectX/OpenGL の Y 反転を吸収する
            // renderIntoTexture = true: RenderTexture 描画時の座標系に合わせる
            Matrix4x4 gpuProj = GL.GetGPUProjectionMatrix(_mirrorCamera.projectionMatrix, renderIntoTexture: true);
            Matrix4x4 vp      = gpuProj * _mirrorCamera.worldToCameraMatrix;

            _mirrorRenderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetMatrix(s_MirrorVP, vp);

            if (_mirrorCamera.targetTexture != null)
                _propertyBlock.SetTexture(s_MirrorTex, _mirrorCamera.targetTexture);

            _mirrorRenderer.SetPropertyBlock(_propertyBlock);
        }

        // ---- ミラーカメラ更新 ----

        /// <summary>
        /// ミラーカメラの位置と回転を反射ベクトルから更新する。
        /// </summary>
        private void UpdateMirrorCamera()
        {
            // ミラー法線をローカル空間からワールド空間へ変換する
            Vector3 mirrorNormal = _mirrorTransform.TransformDirection(_localNormal).normalized;

            // 入射ベクトル I: メインカメラ位置 → ミラー面中心 の方向（正規化）
            Vector3 I = (_mirrorTransform.position - _mainCamera.transform.position).normalized;

            // 反射ベクトル R を算出する
            // Vector3.Reflect の式: R = I - 2 * dot(I, N) * N
            Vector3 R = Vector3.Reflect(I, mirrorNormal);

            // up ベクトルを決定する
            // ミラーの up をそのまま使うのが最もシンプルで正確
            Vector3 mirrorUp = _mirrorTransform.up;

            // R と mirrorUp が平行に近いとき（|dot| > 閾値）は LookRotation が破綻する
            // ジンバルロック回避のため mirrorTransform.right にフォールバックする
            Vector3 upVec = (Mathf.Abs(Vector3.Dot(R, mirrorUp)) > _gimbalLockThreshold)
                ? _mirrorTransform.right
                : mirrorUp;

            // カメラ位置: ミラー面から法線方向に _cameraOffset だけ手前に配置する
            _mirrorCamera.transform.position = _mirrorTransform.position - mirrorNormal * _cameraOffset;

            // カメラ回転: 反射方向 R を向くよう Quaternion.LookRotation で設定する
            _mirrorCamera.transform.rotation = Quaternion.LookRotation(R, upVec);
        }

#if UNITY_EDITOR
        /// <summary>
        /// シーンビューでミラー法線の向きをシアン矢印で可視化する。
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            if (_mirrorTransform == null) return;

            Vector3 worldNormal = _mirrorTransform.TransformDirection(_localNormal).normalized;
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(_mirrorTransform.position, worldNormal * 0.3f);
        }
#endif
    }
}
