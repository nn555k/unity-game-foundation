using UnityEngine;

namespace GameFoundation.Save
{
    public sealed class PlayerPrefsSaveStore : ISaveStore
    {
        private readonly string mPrefix;

        /// <summary>
        /// 创建带项目级前缀的 PlayerPrefs 存储，避免不同模块键名冲突。
        /// </summary>
        public PlayerPrefsSaveStore(string prefix)
        {
            mPrefix = string.IsNullOrWhiteSpace(prefix) ? "foundation.save." : prefix.Trim() + ".";
        }

        /// <summary>
        /// 读取 PlayerPrefs 主存档。
        /// </summary>
        public bool TryRead(string slot, out string value)
        {
            var key = Key(slot);
            if (!PlayerPrefs.HasKey(key))
            {
                value = null;
                return false;
            }

            value = PlayerPrefs.GetString(key);
            return true;
        }

        /// <summary>
        /// 读取写入前保留的 PlayerPrefs 备份。
        /// </summary>
        public bool TryReadBackup(string slot, out string value)
        {
            var key = BackupKey(slot);
            if (!PlayerPrefs.HasKey(key))
            {
                value = null;
                return false;
            }

            value = PlayerPrefs.GetString(key);
            return true;
        }

        /// <summary>
        /// 先保留旧值再提交新值，并立即刷新到平台存储。
        /// </summary>
        public void Write(string slot, string value)
        {
            var key = Key(slot);
            if (PlayerPrefs.HasKey(key))
            {
                PlayerPrefs.SetString(BackupKey(slot), PlayerPrefs.GetString(key));
            }

            PlayerPrefs.SetString(key, value ?? string.Empty);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// 用已验证值恢复主键，并保留现有备份键供再次降级。
        /// </summary>
        public void Restore(string slot, string value)
        {
            PlayerPrefs.SetString(Key(slot), value ?? string.Empty);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// 删除 PlayerPrefs 主存档与备份。
        /// </summary>
        public void Delete(string slot)
        {
            PlayerPrefs.DeleteKey(Key(slot));
            PlayerPrefs.DeleteKey(BackupKey(slot));
            PlayerPrefs.Save();
        }

        /// <summary>
        /// 构建经过槽位清理的主存档键。
        /// </summary>
        private string Key(string slot)
        {
            return mPrefix + Sanitize(slot);
        }

        /// <summary>
        /// 构建与主存档隔离的备份键。
        /// </summary>
        private string BackupKey(string slot)
        {
            return Key(slot) + ".backup";
        }

        /// <summary>
        /// 清理可能造成路径或命名歧义的槽位字符。
        /// </summary>
        private static string Sanitize(string slot)
        {
            return string.IsNullOrWhiteSpace(slot) ? "default" : slot.Trim().Replace("/", "_").Replace("\\", "_");
        }
    }
}
