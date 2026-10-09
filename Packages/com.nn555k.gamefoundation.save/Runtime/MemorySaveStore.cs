using System.Collections.Generic;

namespace GameFoundation.Save
{
    public sealed class MemorySaveStore : ISaveStore
    {
        private readonly Dictionary<string, string> mValues = new Dictionary<string, string>();
        private readonly Dictionary<string, string> mBackups = new Dictionary<string, string>();

        /// <summary>
        /// 从内存读取主存档，主要用于测试或无磁盘运行环境。
        /// </summary>
        public bool TryRead(string slot, out string value)
        {
            return mValues.TryGetValue(slot, out value);
        }

        /// <summary>
        /// 从内存读取上一次成功写入前的备份。
        /// </summary>
        public bool TryReadBackup(string slot, out string value)
        {
            return mBackups.TryGetValue(slot, out value);
        }

        /// <summary>
        /// 将旧值转为备份后写入新值。
        /// </summary>
        public void Write(string slot, string value)
        {
            if (mValues.TryGetValue(slot, out var previous))
            {
                mBackups[slot] = previous;
            }

            mValues[slot] = value;
        }

        /// <summary>
        /// 恢复主值但不覆盖最后一个已知可用备份。
        /// </summary>
        public void Restore(string slot, string value)
        {
            mValues[slot] = value;
        }

        /// <summary>
        /// 删除指定槽位的主值和备份值。
        /// </summary>
        public void Delete(string slot)
        {
            mValues.Remove(slot);
            mBackups.Remove(slot);
        }
    }
}
