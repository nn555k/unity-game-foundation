using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GameFoundation.Core;

namespace GameFoundation.Sdk
{
    public sealed class SdkHost : ISdkHost
    {
        private const string LogCategory = "Foundation.SDK";

        private readonly List<ISdkAdapter> mAdapters;
        private readonly Dictionary<string, SdkAdapterStatus> mStatuses = new Dictionary<string, SdkAdapterStatus>();
        private readonly IFoundationLogger mLogger;
        private bool mInitializing;

        public event Action<SdkAdapterStatus> StatusChanged;
        public IReadOnlyList<SdkAdapterStatus> Statuses => mAdapters
            .Select(adapter => mStatuses[adapter.Id])
            .ToArray();

        /// <summary>
        /// 创建按 Order 稳定排序的 SDK Host，并拒绝重复 Adapter ID。
        /// </summary>
        public SdkHost(IEnumerable<ISdkAdapter> adapters, IFoundationLogger logger = null)
        {
            mLogger = logger;
            mAdapters = (adapters ?? Array.Empty<ISdkAdapter>())
                .Where(adapter => adapter != null)
                .OrderBy(adapter => adapter.Order)
                .ThenBy(adapter => adapter.Id, StringComparer.Ordinal)
                .ToList();

            for (var index = 0; index < mAdapters.Count; index++)
            {
                var adapter = mAdapters[index];
                if (string.IsNullOrWhiteSpace(adapter.Id))
                {
                    throw new ArgumentException("SDK adapter ID cannot be empty.", nameof(adapters));
                }

                if (mStatuses.ContainsKey(adapter.Id))
                {
                    throw new ArgumentException($"Duplicate SDK adapter ID '{adapter.Id}'.", nameof(adapters));
                }

                mStatuses.Add(adapter.Id, new SdkAdapterStatus(adapter.Id, SdkAdapterState.NotStarted));
            }
        }

        /// <summary>
        /// 按稳定顺序初始化 Adapter，并将单个供应商失败隔离为可查询状态。
        /// </summary>
        public async Task InitializeAsync(
            SdkInitializationContext context,
            CancellationToken cancellationToken = default)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (mInitializing)
            {
                throw new InvalidOperationException("SDK initialization is already running.");
            }

            mInitializing = true;
            try
            {
                for (var index = 0; index < mAdapters.Count; index++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var adapter = mAdapters[index];
                    if (mStatuses[adapter.Id].State == SdkAdapterState.Ready)
                    {
                        continue;
                    }

                    if (adapter.RequiresConsent && !context.ConsentGranted)
                    {
                        SetStatus(adapter.Id, SdkAdapterState.WaitingForConsent, "Consent is required.");
                        continue;
                    }

                    var succeeded = await InitializeAdapterAsync(adapter, context, cancellationToken);
                    if (!succeeded && !context.ContinueAfterFailure)
                    {
                        break;
                    }
                }
            }
            finally
            {
                mInitializing = false;
            }
        }

        /// <summary>
        /// 将用户标识分发给所有 Adapter，并隔离供应商异常。
        /// </summary>
        public void SetUserId(string userId)
        {
            ForEachReadyAdapter(adapter => adapter.SetUserId(userId ?? string.Empty), "SetUserId");
        }

        /// <summary>
        /// 将应用焦点变化分发给已就绪 Adapter。
        /// </summary>
        public void HandleApplicationFocus(bool hasFocus)
        {
            ForEachReadyAdapter(adapter => adapter.OnApplicationFocusChanged(hasFocus), "ApplicationFocus");
        }

        /// <summary>
        /// 将应用暂停状态分发给已就绪 Adapter。
        /// </summary>
        public void HandleApplicationPause(bool isPaused)
        {
            ForEachReadyAdapter(adapter => adapter.OnApplicationPauseChanged(isPaused), "ApplicationPause");
        }

        /// <summary>
        /// 使用独立超时令牌初始化单个 Adapter，并转换异常状态。
        /// </summary>
        private async Task<bool> InitializeAdapterAsync(
            ISdkAdapter adapter,
            SdkInitializationContext context,
            CancellationToken cancellationToken)
        {
            SetStatus(adapter.Id, SdkAdapterState.Initializing);
            using (var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            {
                try
                {
                    var initialization = adapter.InitializeAsync(context, timeoutSource.Token);
                    if (initialization == null)
                    {
                        throw new InvalidOperationException($"SDK adapter '{adapter.Id}' returned a null initialization task.");
                    }

                    var timeout = Task.Delay(context.AdapterTimeout, cancellationToken);
                    if (await Task.WhenAny(initialization, timeout) != initialization)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        timeoutSource.Cancel();
                        ObserveLateFailure(initialization, adapter.Id);
                        SetStatus(adapter.Id, SdkAdapterState.TimedOut, $"Timed out after {context.AdapterTimeout.TotalSeconds:0.#} seconds.");
                        return false;
                    }

                    await initialization;
                    SetStatus(adapter.Id, SdkAdapterState.Ready);
                    return true;
                }
                catch (OperationCanceledException)
                {
                    SetStatus(adapter.Id, SdkAdapterState.Canceled, "Initialization was canceled.");
                    throw;
                }
                catch (Exception exception)
                {
                    SetStatus(adapter.Id, SdkAdapterState.Failed, exception.Message);
                    mLogger?.Error(LogCategory, $"{adapter.Id} initialization failed: {exception}");
                    return false;
                }
            }
        }

        /// <summary>
        /// 观察超时后才结束的供应商任务，避免未观察异常污染运行时日志。
        /// </summary>
        private void ObserveLateFailure(Task initialization, string adapterId)
        {
            _ = initialization.ContinueWith(
                task => mLogger?.Error(LogCategory, $"{adapterId} failed after timeout: {task.Exception}"),
                CancellationToken.None,
                TaskContinuationOptions.OnlyOnFaulted,
                TaskScheduler.Default);
        }

        /// <summary>
        /// 仅遍历已初始化 Adapter，避免向未授权或失败供应商发送生命周期事件。
        /// </summary>
        private void ForEachReadyAdapter(Action<ISdkAdapter> action, string operation)
        {
            for (var index = 0; index < mAdapters.Count; index++)
            {
                var adapter = mAdapters[index];
                if (mStatuses[adapter.Id].State != SdkAdapterState.Ready)
                {
                    continue;
                }

                try
                {
                    action(adapter);
                }
                catch (Exception exception)
                {
                    mLogger?.Error(LogCategory, $"{adapter.Id} {operation} failed: {exception}");
                }
            }
        }

        /// <summary>
        /// 保存并广播最新状态，保持调试列表和事件内容一致。
        /// </summary>
        private void SetStatus(string adapterId, SdkAdapterState state, string message = "")
        {
            var status = new SdkAdapterStatus(adapterId, state, message);
            mStatuses[adapterId] = status;
            StatusChanged?.Invoke(status);
        }
    }
}
