using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace GameFoundation.HotUpdate.Tests
{
    public sealed class HotUpdateServiceTests
    {
        private sealed class TestBackend : IContentUpdateBackend
        {
            public bool FailVerification;
            public bool RollbackCalled;

            /// <summary>
            /// 返回一个固定大小的测试更新计划。
            /// </summary>
            public Task<ContentUpdatePlan> CheckAsync(ContentUpdateRequest request, CancellationToken cancellationToken)
            {
                return Task.FromResult(new ContentUpdatePlan(true, "1", "2", 100L));
            }

            /// <summary>
            /// 模拟一次完整下载进度。
            /// </summary>
            public Task DownloadAsync(ContentUpdatePlan plan, IProgress<ContentUpdateProgress> progress, CancellationToken cancellationToken)
            {
                progress.Report(new ContentUpdateProgress(ContentUpdateStage.Downloading, 100L, 100L));
                return Task.CompletedTask;
            }

            /// <summary>
            /// 根据测试开关模拟校验成功或失败。
            /// </summary>
            public Task VerifyAsync(ContentUpdatePlan plan, CancellationToken cancellationToken)
            {
                if (FailVerification)
                {
                    throw new InvalidOperationException("verification failed");
                }

                return Task.CompletedTask;
            }

            /// <summary>
            /// 模拟提交完成。
            /// </summary>
            public Task CommitAsync(ContentUpdatePlan plan, CancellationToken cancellationToken)
            {
                return Task.CompletedTask;
            }

            /// <summary>
            /// 记录失败路径是否调用了回滚。
            /// </summary>
            public Task RollbackAsync(ContentUpdatePlan plan, CancellationToken cancellationToken)
            {
                RollbackCalled = true;
                return Task.CompletedTask;
            }

            /// <summary>
            /// 测试后端不提供离线内容，确保失败结果不会被降级掩盖。
            /// </summary>
            public bool HasUsableLocalContent(ContentUpdateRequest request)
            {
                return false;
            }
        }

        /// <summary>
        /// 验证成功更新依次完成下载、校验与提交。
        /// </summary>
        [Test]
        public void PrepareAsyncCompletesUpdate()
        {
            var service = new HotUpdateService(new TestBackend());

            var result = service.PrepareAsync(new ContentUpdateRequest("catalog", "main"))
                .GetAwaiter()
                .GetResult();

            Assert.That(result.Code, Is.EqualTo(ContentUpdateResultCode.Updated));
            Assert.That(result.Version, Is.EqualTo("2"));
            Assert.That(service.Stage, Is.EqualTo(ContentUpdateStage.Completed));
        }

        /// <summary>
        /// 验证校验失败时执行回滚并返回失败状态。
        /// </summary>
        [Test]
        public void PrepareAsyncRollsBackAfterVerificationFailure()
        {
            var backend = new TestBackend { FailVerification = true };
            var service = new HotUpdateService(backend);

            var result = service.PrepareAsync(new ContentUpdateRequest("catalog", "main", false))
                .GetAwaiter()
                .GetResult();

            Assert.That(result.Code, Is.EqualTo(ContentUpdateResultCode.Failed));
            Assert.That(backend.RollbackCalled, Is.True);
        }
    }
}
