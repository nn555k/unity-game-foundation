using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace GameFoundation.Save
{
    public sealed class FileSaveStore : ISaveStore
    {
        private readonly string mDirectory;

        /// <summary>
        /// 创建使用指定目录的文件存储，目录只在首次写入时创建。
        /// </summary>
        public FileSaveStore(string directory = null)
        {
            mDirectory = string.IsNullOrWhiteSpace(directory)
                ? Path.Combine(Application.persistentDataPath, "GameFoundation", "Saves")
                : Path.GetFullPath(directory);
        }

        /// <summary>
        /// 从主文件读取 UTF-8 存档，读取异常由调用方作为缺失处理。
        /// </summary>
        public bool TryRead(string slot, out string value)
        {
            return TryReadPath(PathFor(slot), out value);
        }

        /// <summary>
        /// 从备份文件读取 UTF-8 存档。
        /// </summary>
        public bool TryReadBackup(string slot, out string value)
        {
            return TryReadPath(BackupPathFor(slot), out value);
        }

        /// <summary>
        /// 通过临时文件和备份文件替换目标，避免部分写入破坏唯一副本。
        /// </summary>
        public void Write(string slot, string value)
        {
            Directory.CreateDirectory(mDirectory);
            var path = PathFor(slot);
            var backupPath = BackupPathFor(slot);
            var temporaryPath = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            File.WriteAllText(temporaryPath, value ?? string.Empty, new UTF8Encoding(false));

            try
            {
                if (!File.Exists(path))
                {
                    File.Move(temporaryPath, path);
                    return;
                }

                try
                {
                    File.Replace(temporaryPath, path, backupPath);
                }
                catch (PlatformNotSupportedException)
                {
                    ReplaceWithFallback(temporaryPath, path, backupPath);
                }
                catch (NotSupportedException)
                {
                    ReplaceWithFallback(temporaryPath, path, backupPath);
                }
            }
            finally
            {
                if (File.Exists(temporaryPath))
                {
                    File.Delete(temporaryPath);
                }
            }
        }

        /// <summary>
        /// 原子恢复主文件且不让已损坏主文件覆盖已验证备份。
        /// </summary>
        public void Restore(string slot, string value)
        {
            Directory.CreateDirectory(mDirectory);
            var path = PathFor(slot);
            var temporaryPath = path + "." + Guid.NewGuid().ToString("N") + ".restore.tmp";
            File.WriteAllText(temporaryPath, value ?? string.Empty, new UTF8Encoding(false));

            try
            {
                if (!File.Exists(path))
                {
                    File.Move(temporaryPath, path);
                    return;
                }

                try
                {
                    File.Replace(temporaryPath, path, null);
                }
                catch (PlatformNotSupportedException)
                {
                    File.Copy(temporaryPath, path, true);
                }
                catch (NotSupportedException)
                {
                    File.Copy(temporaryPath, path, true);
                }
            }
            finally
            {
                if (File.Exists(temporaryPath))
                {
                    File.Delete(temporaryPath);
                }
            }
        }

        /// <summary>
        /// 删除指定槽位的主文件和备份文件。
        /// </summary>
        public void Delete(string slot)
        {
            DeleteIfExists(PathFor(slot));
            DeleteIfExists(BackupPathFor(slot));
        }

        /// <summary>
        /// 在不支持 File.Replace 的平台保留旧值并移动临时文件。
        /// </summary>
        private static void ReplaceWithFallback(string temporaryPath, string path, string backupPath)
        {
            File.Copy(path, backupPath, true);
            File.Delete(path);
            try
            {
                File.Move(temporaryPath, path);
            }
            catch
            {
                if (!File.Exists(path) && File.Exists(backupPath))
                {
                    File.Copy(backupPath, path, true);
                }

                throw;
            }
        }

        /// <summary>
        /// 安全读取存在的文件并在 I/O 异常时返回失败。
        /// </summary>
        private static bool TryReadPath(string path, out string value)
        {
            try
            {
                if (!File.Exists(path))
                {
                    value = null;
                    return false;
                }

                value = File.ReadAllText(path, Encoding.UTF8);
                return true;
            }
            catch (IOException)
            {
                value = null;
                return false;
            }
            catch (UnauthorizedAccessException)
            {
                value = null;
                return false;
            }
        }

        /// <summary>
        /// 构建不会逃逸基础目录的主文件路径。
        /// </summary>
        private string PathFor(string slot)
        {
            return Path.Combine(mDirectory, Sanitize(slot) + ".json");
        }

        /// <summary>
        /// 构建与主文件相邻的备份文件路径。
        /// </summary>
        private string BackupPathFor(string slot)
        {
            return PathFor(slot) + ".backup";
        }

        /// <summary>
        /// 将槽位名称限制为文件系统安全字符。
        /// </summary>
        private static string Sanitize(string slot)
        {
            if (string.IsNullOrWhiteSpace(slot))
            {
                return "default";
            }

            var characters = slot.Trim().ToCharArray();
            for (var index = 0; index < characters.Length; index++)
            {
                var character = characters[index];
                if (!char.IsLetterOrDigit(character) && character != '-' && character != '_' && character != '.')
                {
                    characters[index] = '_';
                }
            }

            return new string(characters);
        }

        /// <summary>
        /// 删除存在的文件，缺失文件视为已经完成。
        /// </summary>
        private static void DeleteIfExists(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }
}
