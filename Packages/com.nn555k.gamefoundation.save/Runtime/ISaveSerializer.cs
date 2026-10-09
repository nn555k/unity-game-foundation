using QFramework;

namespace GameFoundation.Save
{
    public interface ISaveSerializer : IUtility
    {
        /// <summary>
        /// 将指定值序列化为存储文本。
        /// </summary>
        string Serialize<T>(T value);

        /// <summary>
        /// 将存储文本反序列化为指定类型。
        /// </summary>
        T Deserialize<T>(string value);
    }
}
