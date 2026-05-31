// MirrorSurfaceVector.shader
// MirrorCameraVector（ベクトル演算方式）対応のミラー面シェーダー。
//
// 構成:
//   Pass 1 (StencilWrite / SRPDefaultUnlit)
//     ミラーメッシュの形状をステンシルバッファに書き込む。色は描画しない。
//     → 楕円形などの任意メッシュが「窓」の輪郭になる。
//
//   Pass 2 (MirrorForwardLit / UniversalForward)
//     ステンシルが一致した領域のみ、プロジェクティブ UV でミラー映像を描画する。
//     → ミラーカメラの VP 行列（_MirrorVP）で各頂点をカメラ空間へ投影し UV を決定する。
//
// 行列方式 (MirrorSurface.shader) との違い:
//   行列方式はスクリーン空間 UV が使える（worldToCameraMatrix に反射行列を直接乗算するため）。
//   ベクトル方式はカメラが物理的に別位置にいるため、プロジェクティブ UV が必要。
//
// C# 側の設定（MirrorCameraVector.cs）:
//   GL.GetGPUProjectionMatrix(proj, true) * view を _MirrorVP として
//   MaterialPropertyBlock 経由で毎フレーム設定する。

Shader "CarMirror/MirrorSurfaceVector"
{
    Properties
    {
        // ミラーカメラの RenderTexture（MirrorCameraVector.cs が MaterialPropertyBlock で設定する）
        _MirrorTex ("Mirror Render Texture", 2D) = "white" {}

        // 水平反転トグル
        [Toggle] _FlipX ("Flip X（水平反転）", Float) = 0

        // 明るさ調整（1.0 = 原色のまま）
        _Brightness ("Brightness", Range(0.5, 2.0)) = 1.0

        // ミラー面の彩度（1.0 = 原色のまま、0.0 = グレースケール）
        _Saturation ("Saturation", Range(0.0, 1.0)) = 1.0

        // ステンシル ID（0〜255）。複数ミラーを使う場合は個別に設定する
        [IntRange] _StencilID ("Stencil ID", Range(0, 255)) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderType"     = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue"          = "Geometry"
        }

        // ─────────────────────────────────────────────────────────────────────
        // Pass 1: ステンシル書き込みパス（SRPDefaultUnlit → UniversalForward より先に描画）
        //
        // ミラーメッシュの形状を Ref 値としてステンシルバッファへ書き込む。
        // ColorMask 0 で色出力なし・ZWrite On で深度のみ書く。
        // ─────────────────────────────────────────────────────────────────────
        Pass
        {
            Name "StencilWrite"
            Tags { "LightMode" = "SRPDefaultUnlit" }

            ColorMask 0
            ZWrite On
            ZTest LEqual
            Cull Back

            Stencil
            {
                Ref   [_StencilID]
                Comp  Always
                Pass  Replace   // ステンシルに _StencilID を書き込む
            }

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            // ColorMask 0 のため実行されないが、コンパイルには必要
            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                return 0;
            }
            ENDHLSL
        }

        // ─────────────────────────────────────────────────────────────────────
        // Pass 2: ミラー映像描画パス（プロジェクティブテクスチャマッピング）
        //
        // ステンシルが _StencilID と一致した領域のみ描画する。
        // 各頂点のワールド座標を _MirrorVP でミラーカメラクリップ空間へ変換し
        // フラグメントでパースペクティブ除算して RenderTexture の UV を求める。
        //
        // _MirrorVP = GL.GetGPUProjectionMatrix(mirrorCam.projectionMatrix, true)
        //           * mirrorCam.worldToCameraMatrix
        // を C# から MaterialPropertyBlock で毎フレーム設定すること。
        // ─────────────────────────────────────────────────────────────────────
        Pass
        {
            Name "MirrorForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            ZWrite On
            ZTest LEqual
            Cull Back

            Stencil
            {
                Ref   [_StencilID]
                Comp  Equal     // Pass 1 で書き込んだ領域のみ通過
                Pass  Keep
            }

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // ---- テクスチャ & サンプラー ----
            TEXTURE2D(_MirrorTex);
            SAMPLER(sampler_MirrorTex);

            // ---- マテリアルプロパティ（GPU 定数バッファ） ----
            CBUFFER_START(UnityPerMaterial)
                float4   _MirrorTex_ST;
                // ミラーカメラの GPU 用 ViewProjection 行列。
                // C# で GL.GetGPUProjectionMatrix(proj, true) * view を渡すこと。
                float4x4 _MirrorVP;
                float    _FlipX;
                float    _Brightness;
                float    _Saturation;
            CBUFFER_END

            // ---- 頂点シェーダー入力 ----
            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            // ---- フラグメントシェーダー入力 ----
            struct Varyings
            {
                float4 positionHCS   : SV_POSITION;
                // ミラーカメラクリップ空間での同次座標（除算前）
                float4 mirrorClipPos : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            // ---- 頂点シェーダー ----
            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float3 worldPos      = TransformObjectToWorld(input.positionOS.xyz);
                output.positionHCS   = TransformObjectToHClip(input.positionOS.xyz);

                // ワールド座標 → ミラーカメラクリップ空間へ投影
                // w 除算はフラグメントで行う（パースペクティブ補正のため補間後に実施）
                output.mirrorClipPos = mul(_MirrorVP, float4(worldPos, 1.0));

                return output;
            }

            // ---- フラグメントシェーダー ----
            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                // パースペクティブ除算: クリップ座標 → NDC [-1,1] → UV [0,1]
                // GL.GetGPUProjectionMatrix(proj, renderIntoTexture:true) 使用時は
                // Y 反転がプラットフォームごとに吸収されているため追加補正は不要
                float2 uv = input.mirrorClipPos.xy / input.mirrorClipPos.w;
                uv = uv * 0.5 + 0.5;

                // _FlipX が 1 のとき U 座標を反転（左右ミラー映像の調整）
                uv.x = lerp(uv.x, 1.0 - uv.x, _FlipX);

                // RenderTexture からミラー映像をサンプリングする
                half4 col = SAMPLE_TEXTURE2D(_MirrorTex, sampler_MirrorTex, uv);

                // 明るさを乗算で調整する
                col.rgb *= _Brightness;

                // 彩度を調整する（NTSC 輝度係数）
                half luminance = dot(col.rgb, half3(0.299h, 0.587h, 0.114h));
                col.rgb = lerp(half3(luminance, luminance, luminance), col.rgb, _Saturation);

                return col;
            }
            ENDHLSL
        }

        // シャドウキャスターパス
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma vertex   ShadowPassVertex
            #pragma fragment ShadowPassFragment
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4   _MirrorTex_ST;
                float4x4 _MirrorVP;
                float    _FlipX;
                float    _Brightness;
                float    _Saturation;
            CBUFFER_END
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
