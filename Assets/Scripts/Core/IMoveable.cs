namespace CarMirrorSimulator.Core
{
    /// <summary>
    /// 軸移動
    /// </summary>
    public interface IMoveable<T>
    {
        public void Move(T Direction);
    }
}
// --- EOF ---
