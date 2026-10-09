using System;

namespace GameFoundation.Sdk
{
    public sealed class SdkInitializationContext
    {
        public SdkEnvironment Environment { get; }
        public string UserId { get; }
        public bool ConsentGranted { get; }
        public TimeSpan AdapterTimeout { get; }
        public bool ContinueAfterFailure { get; }

        /// <summary>
        /// 创建一次不包含供应商密钥的 SDK 初始化上下文。
        /// </summary>
        public SdkInitializationContext(
            SdkEnvironment environment,
            string userId,
            bool consentGranted,
            TimeSpan? adapterTimeout = null,
            bool continueAfterFailure = true)
        {
            var resolvedTimeout = adapterTimeout ?? TimeSpan.FromSeconds(20);
            if (resolvedTimeout <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(adapterTimeout), "Adapter timeout must be positive.");
            }

            Environment = environment;
            UserId = userId ?? string.Empty;
            ConsentGranted = consentGranted;
            AdapterTimeout = resolvedTimeout;
            ContinueAfterFailure = continueAfterFailure;
        }
    }
}
