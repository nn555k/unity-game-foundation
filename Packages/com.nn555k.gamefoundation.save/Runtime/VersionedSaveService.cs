using System;
using System.Collections.Generic;
using GameFoundation.Core;

namespace GameFoundation.Save
{
    public sealed class VersionedSaveService<T> where T : class, new()
    {
        private const string LogCategory = "Foundation.Save";

        private readonly string mSlot;
        private readonly int mCurrentVersion;
        private readonly ISaveStore mStore;
        private readonly ISaveSerializer mSerializer;
        private readonly IFoundationClock mClock;
        private readonly IFoundationLogger mLogger;
        private readonly Dictionary<int, ISaveMigration> mMigrations;

        /// <summary>
        /// 创建一个由项目数据类型、存储后端和迁移链共同组成的版本化存档服务。
        /// </summary>
        public VersionedSaveService(
            string slot,
            int currentVersion,
            ISaveStore store,
            ISaveSerializer serializer,
            IFoundationClock clock,
            IFoundationLogger logger,
            IEnumerable<ISaveMigration> migrations = null)
        {
            if (string.IsNullOrWhiteSpace(slot))
            {
                throw new ArgumentException("Save slot cannot be empty.", nameof(slot));
            }

            if (currentVersion <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(currentVersion));
            }

            mSlot = slot;
            mCurrentVersion = currentVersion;
            mStore = store ?? throw new ArgumentNullException(nameof(store));
            mSerializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            mClock = clock ?? throw new ArgumentNullException(nameof(clock));
            mLogger = logger;
            mMigrations = BuildMigrationMap(migrations);
        }

        /// <summary>
        /// 保存当前版本数据，并由存储后端负责原子替换与旧版本备份。
        /// </summary>
        public void Save(T value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            mStore.Write(mSlot, SerializeCurrent(value));
        }

        /// <summary>
        /// 先读取主存档，损坏时尝试备份，并在成功迁移后写回当前版本。
        /// </summary>
        public SaveLoadResult<T> Load()
        {
            SaveLoadResult<T> primaryFailure = null;
            if (mStore.TryRead(mSlot, out var primary))
            {
                var primaryResult = TryLoadRaw(primary, false, true);
                if (primaryResult.IsSuccess)
                {
                    return primaryResult;
                }

                primaryFailure = primaryResult;
                mLogger?.Warning(LogCategory, $"Primary slot '{mSlot}' failed: {primaryResult.Status}.");
            }

            if (mStore.TryReadBackup(mSlot, out var backup))
            {
                var backupResult = TryLoadRaw(backup, true, false);
                if (backupResult.IsSuccess)
                {
                    mStore.Restore(mSlot, SerializeCurrent(backupResult.Value));
                    return backupResult;
                }

                return backupResult;
            }

            return primaryFailure ??
                   new SaveLoadResult<T>(SaveLoadStatus.Missing, null, false, "Save slot does not exist.");
        }

        /// <summary>
        /// 删除主存档及其备份，供项目明确执行重置操作。
        /// </summary>
        public void Delete()
        {
            mStore.Delete(mSlot);
        }

        /// <summary>
        /// 验证 Envelope、执行连续迁移并反序列化最终业务数据。
        /// </summary>
        private SaveLoadResult<T> TryLoadRaw(string raw, bool usedBackup, bool persistMigration)
        {
            SaveEnvelope envelope;
            try
            {
                envelope = mSerializer.Deserialize<SaveEnvelope>(raw);
            }
            catch (Exception exception)
            {
                return Failure(SaveLoadStatus.Corrupt, usedBackup, exception);
            }

            if (envelope == null || !SaveChecksum.IsValid(envelope.Version, envelope.Payload, envelope.Checksum))
            {
                return new SaveLoadResult<T>(SaveLoadStatus.Corrupt, null, usedBackup, "Envelope checksum is invalid.");
            }

            if (envelope.Version > mCurrentVersion)
            {
                return new SaveLoadResult<T>(SaveLoadStatus.MigrationMissing, null, usedBackup, "Save version is newer than this application.");
            }

            var payload = envelope.Payload;
            var version = envelope.Version;
            while (version < mCurrentVersion)
            {
                if (!mMigrations.TryGetValue(version, out var migration))
                {
                    return new SaveLoadResult<T>(SaveLoadStatus.MigrationMissing, null, usedBackup, $"Missing migration from version {version}.");
                }

                try
                {
                    payload = migration.Migrate(payload);
                    version = migration.ToVersion;
                }
                catch (Exception exception)
                {
                    return Failure(SaveLoadStatus.MigrationFailed, usedBackup, exception);
                }
            }

            try
            {
                var value = mSerializer.Deserialize<T>(payload);
                if (value == null)
                {
                    return new SaveLoadResult<T>(SaveLoadStatus.DeserializeFailed, null, usedBackup, "Deserialized value is null.");
                }

                if (persistMigration && envelope.Version != mCurrentVersion)
                {
                    Save(value);
                }

                return new SaveLoadResult<T>(SaveLoadStatus.Success, value, usedBackup, string.Empty);
            }
            catch (Exception exception)
            {
                return Failure(SaveLoadStatus.DeserializeFailed, usedBackup, exception);
            }
        }

        /// <summary>
        /// 将当前数据封装为带版本、时间与校验值的可持久化文本。
        /// </summary>
        private string SerializeCurrent(T value)
        {
            var payload = mSerializer.Serialize(value);
            var envelope = new SaveEnvelope
            {
                Version = mCurrentVersion,
                SavedAtUtc = mClock.UtcNow.ToString("O"),
                Payload = payload,
                Checksum = SaveChecksum.Compute(mCurrentVersion, payload)
            };
            return mSerializer.Serialize(envelope);
        }

        /// <summary>
        /// 校验迁移链的版本方向并构建常数时间查找表。
        /// </summary>
        private static Dictionary<int, ISaveMigration> BuildMigrationMap(IEnumerable<ISaveMigration> migrations)
        {
            var map = new Dictionary<int, ISaveMigration>();
            if (migrations == null)
            {
                return map;
            }

            foreach (var migration in migrations)
            {
                if (migration == null || migration.FromVersion <= 0 || migration.ToVersion <= migration.FromVersion)
                {
                    throw new ArgumentException("Save migrations must advance from a positive version.", nameof(migrations));
                }

                if (map.ContainsKey(migration.FromVersion))
                {
                    throw new ArgumentException($"Duplicate migration from version {migration.FromVersion}.", nameof(migrations));
                }

                map.Add(migration.FromVersion, migration);
            }

            return map;
        }

        /// <summary>
        /// 将异常转换为不向业务层泄漏控制流的读取结果。
        /// </summary>
        private static SaveLoadResult<T> Failure(SaveLoadStatus status, bool usedBackup, Exception exception)
        {
            return new SaveLoadResult<T>(status, null, usedBackup, exception.Message);
        }
    }
}
