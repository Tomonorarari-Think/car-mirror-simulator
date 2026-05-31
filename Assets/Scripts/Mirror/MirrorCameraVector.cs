using UnityEngine;

namespace CarMirrorSimulator.Mirror
{
    /// <summary>
    /// ベクトル演算方式のミラーカメラ制御コンポーネント。
    /// Vector3.Reflect と Quaternion.LookRotation を使ってミラーカメラの
    /// 位置・回転を毎フレーム更新する。
    /// </summary>
    /// <remarks>
    /// 行列方式 (MirrorCamera.cs) との違い:
    /// - worldToCameraMatrix を書き換えないため GL.invertCulling が不要
    /// - MirrorRenderFeature を Renderer Asset に追加する必要がない
    /// - RenderTexture はミラーカメラに直接設定する（本スクリプトは管理しない）
    ///
    /// セットアップ手順:
    /// 1. シーンにミラーカメラ用 GameObject を作成し Camera コンポーネントを追加する
    /// 2. カメラの Target Texture に事前作成した RenderTexture を設定する
    /// 3. Quad に RenderTexture を参照するマテリアルを設定する
    /// 4. 任意の GameObject に本スクリプトを追加し、Inspector で参照を設定する
    /// </remarks>
    [AddComponentMenu("CarMirror/Mirror Camera (Vector)")]
    public class MirrorCameraVector : MonoBehaviour
    {
        // ---- Inspector 設定 ----

        [Header("参照")]
        [Tooltip("反射計算の基準となるメインカメラ（未設定時は Camera.main を使用）")]
        [SerializeField] private Camera _mainCamera;

        [Tooltip("制御するミラーカメラ（Target Texture に RenderTexture を設定しておく）")]
        [SerializeField] private Camera _mirrorCamera;

        [Tooltip("ミラー面の Transform（Quad や Plane の Transform を設定する）")]
        [SerializeField] private Transform _mirrorTransform;

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

        // ---- Unity ライフサイクル ----

        private void Awake()
        {
            // mainCamera が未設定の場合はシーン内の Camera.main で補完する
            if (_mainCamera == null)
                _mainCamera = Camera.main;
        }

        private void LateUpdate()
        {
            // 必須参照が揃っていない場合はスキップする
            if (_mainCamera == null || _mirrorCamera == null || _mirrorTransform == null)
                return;

            UpdateMirrorCamera();
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
