using System;
using System.Threading;
using System.Threading.Tasks;
using GameFoundation.Sdk;

namespace GameFoundation.Samples.Sdk
{
    public interface IProjectSdkBridge
    {
        /// <summary>
        /// Initializes the concrete vendor library owned by the consuming project.
        /// </summary>
        Task InitializeAsync(SdkInitializationContext context, CancellationToken cancellationToken);

        /// <summary>
        /// Updates the vendor-side user identity after consent and initialization.
        /// </summary>
        void SetUserId(string userId);

        /// <summary>
        /// Forwards focus changes to the concrete vendor library.
        /// </summary>
        void OnApplicationFocusChanged(bool hasFocus);

        /// <summary>
        /// Forwards pause changes to the concrete vendor library.
        /// </summary>
        void OnApplicationPauseChanged(bool isPaused);
    }

    public sealed class ProjectSdkAdapter : ISdkAdapter
    {
        private readonly IProjectSdkBridge mBridge;

        public string Id { get; }
        public int Order { get; }
        public bool RequiresConsent { get; }

        /// <summary>
        /// Creates one project adapter without placing vendor keys in the shared package.
        /// </summary>
        public ProjectSdkAdapter(
            string id,
            int order,
            bool requiresConsent,
            IProjectSdkBridge bridge)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("Adapter ID cannot be empty.", nameof(id));
            }

            Id = id;
            Order = order;
            RequiresConsent = requiresConsent;
            mBridge = bridge ?? throw new ArgumentNullException(nameof(bridge));
        }

        /// <summary>
        /// Delegates initialization while preserving the Host cancellation and timeout boundary.
        /// </summary>
        public Task InitializeAsync(SdkInitializationContext context, CancellationToken cancellationToken)
        {
            return mBridge.InitializeAsync(context, cancellationToken);
        }

        /// <summary>
        /// Delegates the normalized user identifier to the project bridge.
        /// </summary>
        public void SetUserId(string userId)
        {
            mBridge.SetUserId(userId);
        }

        /// <summary>
        /// Delegates application focus state only after the Host marks the adapter ready.
        /// </summary>
        public void OnApplicationFocusChanged(bool hasFocus)
        {
            mBridge.OnApplicationFocusChanged(hasFocus);
        }

        /// <summary>
        /// Delegates application pause state only after the Host marks the adapter ready.
        /// </summary>
        public void OnApplicationPauseChanged(bool isPaused)
        {
            mBridge.OnApplicationPauseChanged(isPaused);
        }
    }
}
