using UnityEngine;

namespace GameFoundation.Save
{
    public sealed class JsonUtilitySaveSerializer : ISaveSerializer
    {
        /// <summary>
        /// 使用 Unity JsonUtility 序列化项目拥有的数据对象。
        /// </summary>
        public string Serialize<T>(T value)
        {
            return JsonUtility.ToJson(value);
        }

        /// <summary>
        /// 使用 Unity JsonUtility 恢复项目拥有的数据对象。
        /// </summary>
        public T Deserialize<T>(string value)
        {
            return JsonUtility.FromJson<T>(value);
        }
    }
}
