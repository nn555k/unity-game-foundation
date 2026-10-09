using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace GameFoundation.Sdk.Tests
{
    public sealed class SdkHostTests
    {
        private sealed class RecordingAdapter : ISdkAdapter
        {
            private readonly IList<string> mCalls;

            public string Id { get; }
            public int Order { get; }
            public bool RequiresConsent { get; }

            /// <summary>
            /// 创建记录初始化顺序的测试 Adapter。
            /// </summary>
            public RecordingAdapter(string id, int order, bool requiresConsent, IList<string> calls)
            {
                Id = id;
                Order = order;
                RequiresConsent = requiresConsent;
                mCalls = calls;
            }

            /// <summary>
            /// 记录当前 Adapter 的初始化调用。
            /// </summary>
            public Task InitializeAsync(SdkInitializationContext context, CancellationToken cancellationToken)
            {
                mCalls.Add(Id);
                return Task.CompletedTask;
            }

            /// <summary>
            /// 测试 Adapter 不处理用户标识。
            /// </summary>
            public void SetUserId(string userId)
            {
            }

            /// <summary>
            /// 测试 Adapter 不处理焦点回调。
            /// </summary>
            public void OnApplicationFocusChanged(bool hasFocus)
            {
            }

            /// <summary>
            /// 测试 Adapter 不处理暂停回调。
            /// </summary>
            public void OnApplicationPauseChanged(bool isPaused)
            {
            }
        }

        private sealed class NonCancelableAdapter : ISdkAdapter
        {
            public string Id => "non-cancelable";
            public int Order => 0;
            public bool RequiresConsent => false;

            /// <summary>
            /// 返回永不完成的任务，用于验证 Host 能独立执行硬超时。
            /// </summary>
            public Task InitializeAsync(SdkInitializationContext context, CancellationToken cancellationToken)
            {
                return new TaskCompletionSource<bool>().Task;
            }

            /// <summary>
            /// 超时测试不处理用户标识。
            /// </summary>
            public void SetUserId(string userId)
            {
            }

            /// <summary>
            /// 超时测试不处理焦点回调。
            /// </summary>
            public void OnApplicationFocusChanged(bool hasFocus)
            {
            }

            /// <summary>
            /// 超时测试不处理暂停回调。
            /// </summary>
            public void OnApplicationPauseChanged(bool isPaused)
            {
            }
        }

        /// <summary>
        /// 验证 Host 按 Order 初始化且跳过未授权 Adapter。
        /// </summary>
        [Test]
        public void InitializeAsyncOrdersAdaptersAndHonorsConsent()
        {
            var calls = new List<string>();
            var host = new SdkHost(new ISdkAdapter[]
            {
                new RecordingAdapter("late", 20, false, calls),
                new RecordingAdapter("consent", 10, true, calls),
                new RecordingAdapter("early", 0, false, calls)
            });

            host.InitializeAsync(new SdkInitializationContext(SdkEnvironment.Development, "user", false))
                .GetAwaiter()
                .GetResult();

            Assert.That(calls, Is.EqualTo(new[] { "early", "late" }));
            Assert.That(host.Statuses[1].State, Is.EqualTo(SdkAdapterState.WaitingForConsent));
        }

        /// <summary>
        /// 验证供应商忽略取消令牌时 Host 仍能在期限内返回超时状态。
        /// </summary>
        [UnityTest]
        public IEnumerator InitializeAsyncTimesOutNonCancelableAdapter()
        {
            var host = new SdkHost(new[] { new NonCancelableAdapter() });
            var context = new SdkInitializationContext(
                SdkEnvironment.Development,
                string.Empty,
                true,
                TimeSpan.FromMilliseconds(10));

            var initialization = host.InitializeAsync(context);
            while (!initialization.IsCompleted)
            {
                yield return null;
            }

            initialization.GetAwaiter().GetResult();

            Assert.That(host.Statuses[0].State, Is.EqualTo(SdkAdapterState.TimedOut));
        }
    }
}
