namespace AppSettingsKing
{
    /// <summary>
    /// 
    /// </summary>
    public class DefaultDataFormatter : IDataFormatter
    {
        #region IData formatter impl

        public byte[] Process(byte[] buffer)
        {
            return buffer;
        }

        public byte[] ProcessBack(byte[] buffer)
        {
            return buffer;
        }

        #endregion
    }
}