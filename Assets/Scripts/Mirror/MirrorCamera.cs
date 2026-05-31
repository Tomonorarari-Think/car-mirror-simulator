using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CarMirrorSimulator.Mirror
{
    /// <summary>
    /// 車載ミラー用カメラコンポーネント。
    /// ビュー行列にミラー平面の反射行列を乗算し、
    /// オブリーク投影と組み合わせてリアルタイム反射映像を RenderTexture に描画する。
    /// </summary>
    /// <remarks>
    /// セットアップ手順:
    /// 1. ミラーカメラ用 GameObject に Camera コンポーネントと本スクリプトを追加する。
    /// 2. Main Camera と MirrorSurface を Inspector で設定する。
    /// 3. ミラーカメラの depth を Main Camera より小さい値（例: -10）にする。
    ///    → URP は depth 昇順でカメラをレンダリングするため、ミラーが先に描画される。
    /// 4. PC_Renderer（または使用中の Renderer）に MirrorRenderFeature を追加する。
    ///    → GL.invertCulling による裏面カリング補正が有効になる。
    /// </remarks>
    [RequireComponent(typeof(Camera))]
    [AddComponentMenu("CarMirror/Mirror Camera")]
    public class MirrorCamera : MonoBehaviour
    {
        // ---- アクティブなミラーカメラの静的レジストリ ----
        // MirrorRenderFeature が O(1) で参照できるよう HashSet で管理する
        internal static readonly HashSet<Camera> ActiveMirrorCameras = new();

        // ---- Inspector 設定 ----

        [Header("参照")]
        [Tooltip("反射計算の基準となるメインカメラ")]
        [SerializeField] private Camera _mainCamera;

        [Tooltip("ミラー平面の位置・法線を提供する MirrorSurface")]
        [SerializeField] private MirrorSurface _mirrorSurface;

        [Header("RenderTexture")]
        [Tooltip("RenderTexture の横解像度（ピクセル）。縦は Main Camera のアスペクト比から自動計算される")]
        [SerializeField] private int _textureWidth = 512;

        [Tooltip("解像度スケール（0.25〜1.0）: 値を下げると負荷が減る")]
        [SerializeField, Range(0.25f, 1f)] private float _resolutionScale = 1f;

        [Header("パフォーマンス")]
        [Tooltip("何フレームに 1 回ミラーを更新するか（1=毎フレーム, 2=30fps 相当 @60fps）")]
        [SerializeField, Range(1, 4)] private int _frameSkip = 1;

        // ---- 内部状態 ----

        private Camera _mirrorCam;
        private RenderTexture _renderTexture;
        private int _frameCounter;

        /// <summary>ミラーカメラが描画先とする RenderTexture。MirrorSurface から参照される。</summary>
        public RenderTexture RenderTexture => _renderTexture;

        // ---- Unity ライフサイクル ----

        private void Awake()
        {
            _mirrorCam = GetComponent<Camera>();

            // メインカメラが未設定の場合はシーン内の Camera.main を使用する
            if (_mainCamera == null)
                _mainCamera = Camera.main;
        }

        private void OnEnable()
        {
            CreateRenderTexture();

            // アクティブレジストリに登録（MirrorRenderFeature が参照する）
            ActiveMirrorCameras.Add(_mirrorCam);

            // URP のカメラレンダリング前後にコールバックを登録する
            // OnPreRender は URP では呼ばれないため RenderPipelineManager を使う
            RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
            RenderPipelineManager.endCameraRendering += OnEndCameraRendering;
        }

        private void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
            RenderPipelineManager.endCameraRendering -= OnEndCameraRendering;

            ActiveMirrorCameras.Remove(_mirrorCam);

            ReleaseRenderTexture();
        }

        private void LateUpdate()
        {
            // フレームスキップ: _frameSkip フレームに 1 回だけ描画する
            // カメラを無効にすると URP が描画をスキップし、RenderTexture は前フレームの内容を保持する
            _frameCounter++;
            bool shouldRender = (_frameCounter % _frameSkip == 0);

            // 可視性チェック: ミラーメッシュが画面外なら描画しない
            bool isVisible = _mirrorSurface != null && _mirrorSurface.IsVisible;

            _mirrorCam.enabled = shouldRender && isVisible;
        }

        // ---- コールバック ----

        /// <summary>
        /// ミラーカメラのレンダリング直前に呼ばれる。
        /// 反射行列を計算してビュー行列とオブリーク投影行列を更新し、
        /// GL.invertCulling を有効にして裏面カリングの反転を補正する。
        /// </summary>
        private void OnBeginCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            if (camera != _mirrorCam) return;
            if (_mainCamera == null || _mirrorSurface == null) return;

            // 反射行列によりハンドネスが反転するため、カリングを逆にする
            // （MirrorRenderFeature でも同様の処理を行うが、フォールバックとして残す）
            GL.invertCulling = true;

            UpdateMirrorMatrices();
        }

        /// <summary>
        /// ミラーカメラのレンダリング直後に呼ばれる。
        /// GL.invertCulling をリセットして後続のカメラに影響しないようにする。
        /// </summary>
        private void OnEndCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            if (camera != _mirrorCam) return;
            GL.invertCulling = false;
        }

        // ---- 行列計算 ----

        /// <summary>
        /// ミラーカメラのビュー行列とオブリーク投影行列を更新する。
        /// </summary>
        /// <remarks>
        /// ビュー行列の更新:
        ///   reflectedViewMatrix = mainCamera.worldToCameraMatrix × reflectionMatrix
        /// これにより、ミラーカメラはミラー平面に対してメインカメラを鏡映した
        /// 位置・姿勢から世界を見ることになる。
        ///
        /// オブリーク投影の更新:
        ///   ミラー平面を near クリップ平面として設定することで、
        ///   ミラー背面のオブジェクトが映り込むのを防ぐ。
        /// </remarks>
        private void UpdateMirrorMatrices()
        {
            Vector3 mirrorPos = _mirrorSurface.MirrorPosition;
            Vector3 mirrorNormal = _mirrorSurface.MirrorNormal;

            // ワールド空間の平面方程式 (a, b, c, d) を算出する
            Vector4 worldPlane = MirrorReflectionMatrix.WorldPlaneToVector4(mirrorPos, mirrorNormal);

            // 反射行列 M = I - 2*N*N^T（同次座標拡張版）を計算する
            Matrix4x4 reflectionMatrix = MirrorReflectionMatrix.CalculateReflectionMatrix(worldPlane);

            // ビュー行列に反射を適用:
            //   mirrorViewMatrix = mainViewMatrix × reflectionMatrix
            _mirrorCam.worldToCameraMatrix = _mainCamera.worldToCameraMatrix * reflectionMatrix;

            // フラスタムカリングの精度向上のため、transform 位置も反射後の位置に更新する
            // (worldToCameraMatrix の上書きはレンダリングに使われ、transform はカリングに使われる)
            Vector3 reflectedPosition = reflectionMatrix.MultiplyPoint(_mainCamera.transform.position);
            transform.position = reflectedPosition;

            // ミラー平面をカメラ空間へ変換してオブリーク投影行列を構築する
            // 手前側 (sideSign=1) をクリップして平面背後を除去する
            Vector4 cameraSpacePlane = MirrorReflectionMatrix.GetCameraSpacePlane(
                _mirrorCam, mirrorPos, mirrorNormal, sideSign: 1f);

            // メインカメラの FoV・アスペクト比を基準に、ミラー平面で斜めにクリップした投影行列を生成する
            _mirrorCam.projectionMatrix = _mainCamera.CalculateObliqueMatrix(cameraSpacePlane);
        }

        // ---- RenderTexture 管理 ----

        /// <summary>
        /// 解像度スケールを考慮した RenderTexture を生成してミラーカメラに割り当てる。
        /// </summary>
        /// <remarks>
        /// 縦解像度はメインカメラのアスペクト比から自動計算する。
        /// スクリーン空間 UV が正しく機能するには
        /// 「ミラーカメラの投影行列のアスペクト比」＝「RenderTexture のアスペクト比」
        /// が一致している必要があるため、メインカメラのアスペクトを基準にする。
        /// </remarks>
        private void CreateRenderTexture()
        {
            int w = Mathf.Max(1, Mathf.RoundToInt(_textureWidth * _resolutionScale));

            // メインカメラのアスペクト比（例: 16/9）から縦解像度を算出する
            float aspect = _mainCamera != null ? _mainCamera.aspect : 16f / 9f;
            int h = Mathf.Max(1, Mathf.RoundToInt(w / aspect));

            _renderTexture = new RenderTexture(w, h, 24, RenderTextureFormat.Default)
            {
                name = $"MirrorRT_{gameObject.name}",
                // アンチエイリアシングはコストが高いため 1（無効）をデフォルトにする
                antiAliasing = 1,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            _renderTexture.Create();

            _mirrorCam.targetTexture = _renderTexture;
            _mirrorCam.aspect = (float)w / h;
        }

        /// <summary>
        /// RenderTexture を解放してミラーカメラへの参照を切る。
        /// </summary>
        private void ReleaseRenderTexture()
        {
            if (_renderTexture == null) return;
            _mirrorCam.targetTexture = null;
            _renderTexture.Release();
            Destroy(_renderTexture);
            _renderTexture = null;
        }

#if UNITY_EDITOR
        /// <summary>
        /// Inspector でパラメータを変更したとき、プレイモード中であれば RenderTexture を再生成する。
        /// </summary>
        private void OnValidate()
        {
            if (!Application.isPlaying || _mirrorCam == null) return;
            ReleaseRenderTexture();
            CreateRenderTexture();
            // MirrorSurface に RenderTexture の更新を通知する
            _mirrorSurface?.RefreshRenderTexture();
        }
#endif
    }
}
