using System.IO;

namespace AppSettingsKing
{
    /// <summary>
    /// 
    /// </summary>
    public interface IDataFormatter
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        byte[] Process(byte[] buffer);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        byte[] ProcessBack(byte[] buffer);
    }
}