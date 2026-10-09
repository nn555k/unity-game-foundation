using System;
using System.Collections.Generic;
using GameFoundation.Core;
using NUnit.Framework;

namespace GameFoundation.Save.Tests
{
    public sealed class VersionedSaveServiceTests
    {
        [Serializable]
        private sealed class TestData
        {
            public int Value;
        }

        private sealed class TestClock : IFoundationClock
        {
            public DateTimeOffset UtcNow => new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero);
            public float RealtimeSinceStartup => 0f;
        }

        private sealed class IncrementMigration : ISaveMigration
        {
            public int FromVersion => 1;
            public int ToVersion => 2;

            /// <summary>
            /// 将测试值加一以证明迁移链确实执行。
            /// </summary>
            public string Migrate(string payload)
            {
                var serializer = new JsonUtilitySaveSerializer();
                var data = serializer.Deserialize<TestData>(payload);
                data.Value++;
                return serializer.Serialize(data);
            }
        }

        /// <summary>
        /// 验证当前版本存档可完整往返。
        /// </summary>
        [Test]
        public void SaveAndLoadRoundTripsData()
        {
            var service = CreateService(1, new MemorySaveStore());
            service.Save(new TestData { Value = 42 });

            var result = service.Load();

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value.Value, Is.EqualTo(42));
            Assert.That(result.UsedBackup, Is.False);
        }

        /// <summary>
        /// 验证连续版本迁移后会返回升级后的数据。
        /// </summary>
        [Test]
        public void LoadRunsMigrationChain()
        {
            var store = new MemorySaveStore();
            var oldService = CreateService(1, store);
            oldService.Save(new TestData { Value = 5 });
            var newService = CreateService(2, store, new[] { new IncrementMigration() });

            var result = newService.Load();

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value.Value, Is.EqualTo(6));
        }

        /// <summary>
        /// 验证主档损坏时恢复备份，并使下一次读取重新走主档。
        /// </summary>
        [Test]
        public void LoadRestoresBackupWithoutLosingIt()
        {
            var store = new MemorySaveStore();
            var service = CreateService(1, store);
            service.Save(new TestData { Value = 10 });
            service.Save(new TestData { Value = 20 });
            store.Write("test", "corrupt-primary");

            var recovered = service.Load();
            var secondLoad = service.Load();

            Assert.That(recovered.IsSuccess, Is.True);
            Assert.That(recovered.UsedBackup, Is.True);
            Assert.That(recovered.Value.Value, Is.EqualTo(20));
            Assert.That(secondLoad.IsSuccess, Is.True);
            Assert.That(secondLoad.UsedBackup, Is.False);
            Assert.That(secondLoad.Value.Value, Is.EqualTo(20));
        }

        /// <summary>
        /// 验证存在但损坏且无备份的槽位不会被误报为缺失。
        /// </summary>
        [Test]
        public void LoadReportsCorruptPrimaryWhenBackupIsMissing()
        {
            var store = new MemorySaveStore();
            store.Write("test", "corrupt-primary");
            var service = CreateService(1, store);

            var result = service.Load();

            Assert.That(result.Status, Is.EqualTo(SaveLoadStatus.Corrupt));
        }

        /// <summary>
        /// 构建使用内存存储的测试服务，避免测试写入玩家目录。
        /// </summary>
        private static VersionedSaveService<TestData> CreateService(
            int version,
            ISaveStore store,
            IEnumerable<ISaveMigration> migrations = null)
        {
            return new VersionedSaveService<TestData>(
                "test",
                version,
                store,
                new JsonUtilitySaveSerializer(),
                new TestClock(),
                null,
                migrations);
        }
    }
}
