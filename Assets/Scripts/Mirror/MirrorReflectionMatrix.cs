using UnityEngine;

namespace CarMirrorSimulator.Mirror
{
    /// <summary>
    /// ミラー平面に対する反射行列を計算する静的ユーティリティクラス。
    /// ビュー行列への直接乗算を前提とした同次座標系の行列演算を提供する。
    /// </summary>
    /// <remarks>
    /// 使用する数学的基礎:
    /// 単位法線 N の平面に対する反射行列は M = I - 2 * N * N^T で定義される。
    /// 原点を通らない平面 (ax + by + cz + d = 0) への拡張として
    /// 4x4 同次座標行列を用いる。
    /// </remarks>
    public static class MirrorReflectionMatrix
    {
        /// <summary>
        /// 平面方程式 ax + by + cz + d = 0 に基づく 4x4 反射行列を計算する。
        /// </summary>
        /// <remarks>
        /// 行列の各要素は M_ij = δ_ij - 2 * n_i * n_j から導出される。
        /// (a, b, c) は単位法線ベクトルであること。d = -dot(N, P) で求まる。
        /// Unity の Matrix4x4 は行優先インデックス (m_row_col) を使用する。
        /// </remarks>
        /// <param name="plane">平面方程式係数 (a, b, c, d)。(a,b,c) は単位法線ベクトル</param>
        /// <returns>反射行列 M（4x4 同次座標行列）</returns>
        public static Matrix4x4 CalculateReflectionMatrix(Vector4 plane)
        {
            float a = plane.x;
            float b = plane.y;
            float c = plane.z;
            float d = plane.w;

            // 反射行列を単位行列から構築する
            // 対角成分: M_ii = 1 - 2 * n_i^2
            // 非対角成分: M_ij = -2 * n_i * n_j
            // 平行移動列: M_i3 = -2 * n_i * d  (原点を通らない平面の補正)
            var m = Matrix4x4.identity;

            // 第1行 (row 0)
            m.m00 = 1f - 2f * a * a;
            m.m01 = -2f * a * b;
            m.m02 = -2f * a * c;
            m.m03 = -2f * a * d;

            // 第2行 (row 1)
            m.m10 = -2f * b * a;
            m.m11 = 1f - 2f * b * b;
            m.m12 = -2f * b * c;
            m.m13 = -2f * b * d;

            // 第3行 (row 2)
            m.m20 = -2f * c * a;
            m.m21 = -2f * c * b;
            m.m22 = 1f - 2f * c * c;
            m.m23 = -2f * c * d;

            // 第4行は同次座標の規約により変化なし (row 3 は [0,0,0,1] のまま)

            return m;
        }

        /// <summary>
        /// ワールド空間の位置と法線からミラー平面の方程式係数 (a, b, c, d) を求める。
        /// </summary>
        /// <remarks>
        /// 平面上の点 P と単位法線 N から平面方程式 ax + by + cz + d = 0 を構築する。
        /// d = -dot(N, P) はスカラー距離項であり、原点から平面までの符号付き距離を表す。
        /// </remarks>
        /// <param name="position">ミラー平面上の任意の点（ワールド空間）</param>
        /// <param name="normal">ミラー平面の法線（ワールド空間、正規化済みでなくても可）</param>
        /// <returns>平面方程式係数 (a, b, c, d)</returns>
        public static Vector4 WorldPlaneToVector4(Vector3 position, Vector3 normal)
        {
            // 法線を正規化して単位ベクトルにする（行列演算の前提条件）
            Vector3 n = normal.normalized;

            // d = -dot(N, P) : 原点から平面までの距離項
            float d = -Vector3.Dot(n, position);

            return new Vector4(n.x, n.y, n.z, d);
        }

        /// <summary>
        /// ミラーカメラ空間に変換したクリップ平面パラメータを計算する。
        /// URP の <see cref="Camera.CalculateObliqueMatrix"/> に渡す入力値として使用する。
        /// </summary>
        /// <remarks>
        /// ビュー行列に反射行列を乗算した後のミラーカメラで呼び出すこと。
        /// オブリーク投影により、ミラー平面より後方のオブジェクトが
        /// near クリップ平面によって除去される。
        /// 法線の変換には逆転置行列が必要だが、スケールなし（等方変換）を前提に
        /// 回転部分のみの変換で代替する。
        /// </remarks>
        /// <param name="mirrorCamera">行列設定済みのミラーカメラ</param>
        /// <param name="position">ミラー平面上の点（ワールド空間）</param>
        /// <param name="normal">ミラー平面の法線（ワールド空間）</param>
        /// <param name="sideSign">クリップ方向（1f: 手前をクリップ, -1f: 奥をクリップ）</param>
        /// <returns>カメラ空間のクリップ平面 (a, b, c, d)</returns>
        public static Vector4 GetCameraSpacePlane(
            Camera mirrorCamera,
            Vector3 position,
            Vector3 normal,
            float sideSign = 1f)
        {
            // ワールド→カメラ空間への変換行列を取得（worldToCameraMatrix は設定済み）
            Matrix4x4 worldToCamera = mirrorCamera.worldToCameraMatrix;

            // 点をカメラ空間へ変換（同次座標の w=1 として処理）
            Vector3 positionInCameraSpace = worldToCamera.MultiplyPoint(position);

            // 法線をカメラ空間へ変換（w=0 ベクトルとして処理し、正規化・符号反転を適用）
            Vector3 normalInCameraSpace =
                worldToCamera.MultiplyVector(normal).normalized * sideSign;

            // d = -dot(N, P) をカメラ空間で算出
            float d = -Vector3.Dot(normalInCameraSpace, positionInCameraSpace);

            return new Vector4(normalInCameraSpace.x, normalInCameraSpace.y, normalInCameraSpace.z, d);
        }
    }
}
