using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace CarMirrorSimulator.Mirror
{
    /// <summary>
    /// ミラーカメラ用 URP ScriptableRendererFeature。
    /// 反射行列の乗算によってハンドネスが反転するため、
    /// ミラーカメラのレンダリング前後で GL.invertCulling を切り替え、
    /// 裏面カリングの逆転を補正する。
    /// </summary>
    /// <remarks>
    /// セットアップ手順:
    /// 1. Project Settings > Graphics で使用している URP Renderer Asset を開く。
    ///    （Assets/Settings/PC_Renderer.asset など）
    /// 2. Add Renderer Feature ボタンから「Mirror Render Feature」を追加する。
    /// 3. 追加後は自動的にすべてのアクティブな MirrorCamera に適用される。
    /// </remarks>
    public class MirrorRenderFeature : ScriptableRendererFeature
    {
        // カリング反転 ON パス（ミラーカメラのレンダリング開始直前）
        private InvertCullingBeginPass _beginPass;

        // カリング反転 OFF パス（ミラーカメラのレンダリング終了直後）
        private InvertCullingEndPass _endPass;

        /// <summary>
        /// Renderer Feature の初期化。
        /// ScriptableRenderer が再生成されるたびに呼ばれる。
        /// </summary>
        public override void Create()
        {
            _beginPass = new InvertCullingBeginPass
            {
                // Opaque 描画の直前にカリングを反転させる
                renderPassEvent = RenderPassEvent.BeforeRenderingOpaques
            };

            _endPass = new InvertCullingEndPass
            {
                // 後処理後にカリングをリセットする（後続カメラへの影響を防ぐ）
                renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing
            };
        }

        /// <summary>
        /// 各カメラのレンダリング前に呼ばれ、パスをキューに積む。
        /// MirrorCamera.ActiveMirrorCameras を参照してミラーカメラのみに適用する。
        /// </summary>
        /// <param name="renderer">現在のフレームに使われる ScriptableRenderer</param>
        /// <param name="renderingData">カメラ・ライト等のレンダリング情報</param>
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            // アクティブなミラーカメラのセットを O(1) で参照する
            Camera currentCamera = renderingData.cameraData.camera;
            if (!MirrorCamera.ActiveMirrorCameras.Contains(currentCamera)) return;

            // ミラーカメラの場合のみ invertCulling 補正パスを追加する
            renderer.EnqueuePass(_beginPass);
            renderer.EnqueuePass(_endPass);
        }

        /// <summary>
        /// Renderer Feature が破棄されるときに呼ばれる。
        /// ScriptableRenderPass はアンマネージドリソースを持たないため、特別な解放処理は不要。
        /// </summary>
        protected override void Dispose(bool disposing) { }
    }

    /// <summary>
    /// ミラーカメラのレンダリング開始前に GL.invertCulling を有効にする ScriptableRenderPass。
    /// 反射行列の行列式が -1（ハンドネス反転）になるため、三角形の巻き順が逆転する。
    /// このパスでカリング方向を逆にすることで正面を正しく描画する。
    /// </summary>
    internal sealed class InvertCullingBeginPass : ScriptableRenderPass
    {
        // Render Graph パスに渡すデータ（GL.invertCulling はグローバルステートのためデータなし）
        // AddUnsafePass の型制約が class のため struct は使用不可
        private class PassData { }

        /// <summary>
        /// Unity 6 URP Render Graph モード用の実装。
        /// AddUnsafePass を使うことで Render Graph 内から即時 GL ステート変更を行える。
        /// </summary>
        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            // UnsafePass: RenderGraph のリソース管理外で生の GL コマンドを発行できる
            using var builder = renderGraph.AddUnsafePass<PassData>("MirrorInvertCullingEnable", out _);

            // パスのカリングを無効化する（何も描画しないパスは Render Graph に削除される恐れがある）
            builder.AllowPassCulling(false);

            // ラスタライザの巻き順判定を反転させる
            builder.SetRenderFunc(static (PassData _, UnsafeGraphContext _) =>
            {
                GL.invertCulling = true;
            });
        }

        /// <summary>
        /// 互換モード（Render Graph 無効時）用のフォールバック実装。
        /// Edit > Project Settings > Graphics > URP > Compatibility Mode が有効な場合に呼ばれる。
        /// </summary>
#pragma warning disable CS0672
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            GL.invertCulling = true;
        }
#pragma warning restore CS0672
    }

    /// <summary>
    /// ミラーカメラのレンダリング完了後に GL.invertCulling を無効に戻す ScriptableRenderPass。
    /// </summary>
    internal sealed class InvertCullingEndPass : ScriptableRenderPass
    {
        // AddUnsafePass の型制約が class のため struct は使用不可
        private class PassData { }

        /// <summary>
        /// Unity 6 URP Render Graph モード用の実装。
        /// レンダリング完了後に GL.invertCulling をリセットし、後続カメラへの影響を防ぐ。
        /// </summary>
        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            using var builder = renderGraph.AddUnsafePass<PassData>("MirrorInvertCullingDisable", out _);
            builder.AllowPassCulling(false);

            // メインカメラ以降の描画が正常になるようカリングをリセットする
            builder.SetRenderFunc(static (PassData _, UnsafeGraphContext _) =>
            {
                GL.invertCulling = false;
            });
        }

        /// <summary>
        /// 互換モード（Render Graph 無効時）用のフォールバック実装。
        /// </summary>
#pragma warning disable CS0672
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            GL.invertCulling = false;
        }
#pragma warning restore CS0672
    }
}
