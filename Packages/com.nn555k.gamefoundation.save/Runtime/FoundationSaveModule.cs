using System;
using GameFoundation.Core;
using QFramework;

namespace GameFoundation.Save
{
    public static class FoundationSaveModule
    {
        /// <summary>
        /// 注册通用存储和序列化实现，调用方可预先注册自定义实现进行替换。
        /// </summary>
        public static void Register(IArchitecture architecture, ISaveStore store = null, ISaveSerializer serializer = null)
        {
            if (architecture == null)
            {
                throw new ArgumentNullException(nameof(architecture));
            }

            FoundationCoreModule.Register(architecture);
            if (architecture.GetUtility<ISaveStore>() == null)
            {
                architecture.RegisterUtility<ISaveStore>(store ?? new FileSaveStore());
            }

            if (architecture.GetUtility<ISaveSerializer>() == null)
            {
                architecture.RegisterUtility<ISaveSerializer>(serializer ?? new JsonUtilitySaveSerializer());
            }
        }
    }
}
