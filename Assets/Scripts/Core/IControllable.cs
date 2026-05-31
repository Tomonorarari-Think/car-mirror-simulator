namespace CarMirrorSimulator.Core
{
    /// <summary>
    /// 操作対象
    /// </summary>
    public interface IControllable
    {
        /// <summary>
        /// 操作開始
        /// </summary>
        public void ActivateControl();

        /// <summary>
        /// 操作終了
        /// </summary>
        public void DeactivateControl();
    }
}
// --- EOF ---
