using System;
using GameFoundation.HotUpdate.Providers.YooAsset;
using NUnit.Framework;

namespace GameFoundation.HotUpdate.YooAsset.Tests
{
    public sealed class YooAssetContentUpdateOptionsTests
    {
        /// <summary>
        /// Verifies URL and tag normalization happens once at configuration creation.
        /// </summary>
        [Test]
        public void HostNormalizesUrlsAndTags()
        {
            var options = YooAssetContentUpdateOptions.Host(
                "Content",
                "https://cdn.example.com/root/",
                downloadTags: new[] { "base", " base ", string.Empty, "optional" });

            Assert.That(options.RemoteMainUrl, Is.EqualTo("https://cdn.example.com/root"));
            Assert.That(options.RemoteFallbackUrl, Is.EqualTo("https://cdn.example.com/root"));
            Assert.That(options.DownloadTags, Is.EqualTo(new[] { "base", "optional" }));
        }

        /// <summary>
        /// Rejects host settings that would otherwise create invalid download URLs at runtime.
        /// </summary>
        [Test]
        public void HostRequiresRemoteUrl()
        {
            Assert.Throws<ArgumentException>(() =>
                YooAssetContentUpdateOptions.Host("Content", string.Empty));
        }

        /// <summary>
        /// Keeps offline configuration independent from any remote service or editor package root.
        /// </summary>
        [Test]
        public void OfflineUsesOfflineMode()
        {
            var options = YooAssetContentUpdateOptions.Offline("Content");

            Assert.That(options.PlayMode, Is.EqualTo(FoundationYooAssetPlayMode.Offline));
            Assert.That(options.RemoteMainUrl, Is.Empty);
        }
    }
}
