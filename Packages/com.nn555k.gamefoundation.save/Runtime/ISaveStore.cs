using QFramework;

namespace GameFoundation.Save
{
    public interface ISaveStore : IUtility
    {
        /// <summary>
        /// 尝试读取槽位当前值。
        /// </summary>
        bool TryRead(string slot, out string value);

        /// <summary>
        /// 尝试读取槽位上一次成功写入前的备份值。
        /// </summary>
        bool TryReadBackup(string slot, out string value);

        /// <summary>
        /// 写入新值并将现有主值轮换为备份。
        /// </summary>
        void Write(string slot, string value);

        /// <summary>
        /// 用已验证的恢复值替换主值，同时保留现有备份。
        /// </summary>
        void Restore(string slot, string value);

        /// <summary>
        /// 删除槽位的主值与备份值。
        /// </summary>
        void Delete(string slot);
    }
}
