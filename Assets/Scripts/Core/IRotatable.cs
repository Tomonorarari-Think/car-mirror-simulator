namespace CarMirrorSimulator.Core
{
    /// <summary>
    /// 軸回転
    /// </summary>
    public interface IRotatable<T>
    {
        public void Rotate(T eulerAngles);
    }
}
// --- EOF ---
