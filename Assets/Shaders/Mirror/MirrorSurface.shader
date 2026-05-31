// MirrorSurface.shader
// 車載ミラー面用 URP シェーダー。
// MirrorCamera が RenderTexture に描画した反射映像を
// スクリーン空間 UV を使ってミラーメッシュ上に貼り付ける。
//
// スクリーン空間 UV を採用する理由:
//   ミラーカメラは worldToCameraMatrix を反射行列で変換して描画しているため、
//   スクリーン上のミラーメッシュの各ピクセルがそのまま RenderTexture の
//   対応ピクセルと一致する。メッシュ UV ではなくスクリーン座標を使うことで
//   正確な反射表現が得られる。

Shader "CarMirror/MirrorSurface"
{
    Properties
    {
        // ミラーカメラの RenderTexture（MirrorSurface.cs が MaterialPropertyBlock で設定する）
        _MirrorTex ("Mirror Render Texture", 2D) = "white" {}

        // 水平反転トグル（バックミラーは左右反転が必要な場合がある）
        [Toggle] _FlipX ("Flip X（水平反転）", Float) = 0

        // 明るさ調整（1.0 = 原色のまま）
        _Brightness ("Brightness", Range(0.5, 2.0)) = 1.0

        // ミラー面の彩度（1.0 = 原色のまま、0.0 = グレースケール）
        _Saturation ("Saturation", Range(0.0, 1.0)) = 1.0
    }

    SubShader
    {
        Tags
        {
            "RenderType"     = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue"          = "Geometry"
        }

        // ミラー面は片面描画（法線方向からのみ見える）
        Cull Back
        ZWrite On
        ZTest LEqual

        Pass
        {
            Name "MirrorSurface_ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // ---- テクスチャ & サンプラー ----
            TEXTURE2D(_MirrorTex);
            SAMPLER(sampler_MirrorTex);

            // ---- マテリアルプロパティ（CBuffer: GPU 定数バッファ） ----
            CBUFFER_START(UnityPerMaterial)
                float4 _MirrorTex_ST;
                float  _FlipX;
                float  _Brightness;
                float  _Saturation;
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
                float4 positionHCS : SV_POSITION;
                // スクリーン空間座標（パースペクティブ補正前の同次座標形式）
                float4 screenPos   : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            // ---- 頂点シェーダー ----
            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                // オブジェクト空間 → クリップ空間へ変換
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);

                // スクリーン空間 UV を計算する
                // ComputeScreenPos は [0, w] の同次座標を返す（w で割ることで [0,1] になる）
                // プラットフォーム差異（DirectX/OpenGL の Y 反転）は内部で吸収される
                output.screenPos = ComputeScreenPos(output.positionHCS);

                return output;
            }

            // ---- フラグメントシェーダー ----
            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                // パースペクティブ補正: w で除算して正規化スクリーン座標 [0,1] を得る
                float2 uv = input.screenPos.xy / input.screenPos.w;

                // _FlipX が 1 のとき U 座標を反転（左右ミラー映像の調整）
                uv.x = lerp(uv.x, 1.0 - uv.x, _FlipX);

                // RenderTexture からミラー映像をサンプリングする
                half4 col = SAMPLE_TEXTURE2D(_MirrorTex, sampler_MirrorTex, uv);

                // 明るさを乗算で調整する
                col.rgb *= _Brightness;

                // 彩度を調整する（輝度を求めてグレーとの線形補間）
                // NTSC 係数: Y = 0.299R + 0.587G + 0.114B
                half luminance = dot(col.rgb, half3(0.299h, 0.587h, 0.114h));
                col.rgb = lerp(half3(luminance, luminance, luminance), col.rgb, _Saturation);

                return col;
            }
            ENDHLSL
        }

        // シャドウキャスターパス（ミラーメッシュは影を落とすが、投影の正確さより
        // シーンへの馴染みを優先するため URP デフォルトの ShadowCaster を流用する）
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
                float4 _MirrorTex_ST;
                float  _FlipX;
                float  _Brightness;
                float  _Saturation;
            CBUFFER_END
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
