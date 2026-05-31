using UnityEngine;

namespace CarMirrorSimulator.Core
{
    /// <summary>
    /// 共通処理
    /// </summary>
    public static class Util
    {
        /// <summary>
        /// オイラー角の正規化
        /// </summary>
        /// <param name="eulerAngles"></param>
        /// <returns>-180~180に正規化された角度</returns>
        public static float NormalizeEulerAngle(float eulerAngles)
        {
            // 0〜360度の範囲に収める
            eulerAngles = (eulerAngles % 360f + 360f) % 360f;

            // 180度を超えていたら、-180〜180度の範囲に変換する
            if (eulerAngles > 180f)
            {
                eulerAngles -= 360f;
            }

            return eulerAngles;
        }

        /// <summary>
        /// オブジェクトがNullの場合に、エラーログを出力して停止する
        /// </summary>
        public static void AssertNotNull(object obj, string message = "致命的なエラー: オブジェクトがNullです。")
        {
            if (obj == null)
            {
                // エディタ上での実行を一時停止
                #if UNITY_EDITOR
                Debug.LogError(message);
                UnityEditor.EditorApplication.isPaused = true;
                #else
                // アプリ強制終了
                Application.Quit();
                #endif
            }
        }
    }
}
// --- EOF ---
