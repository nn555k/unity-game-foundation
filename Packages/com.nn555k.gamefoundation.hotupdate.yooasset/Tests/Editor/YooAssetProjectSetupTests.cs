using GameFoundation.HotUpdate.Providers.YooAsset.Editor;
using NUnit.Framework;
using UnityEngine;
using YooAsset.Editor;

namespace GameFoundation.HotUpdate.YooAsset.Tests
{
    public sealed class YooAssetProjectSetupTests
    {
        /// <summary>Proves repeat initialization keeps one visible package, group, and collector and preserves custom rules.</summary>
        [Test]
        public void DefaultsAreVisibleAndIdempotent()
        {
            var settings = ScriptableObject.CreateInstance<AssetBundleCollectorSetting>();
            try
            {
                YooAssetProjectSetup.ConfigureEmpty(settings, "DefaultPackage", "Assets/GameContent");
                settings.Packages[0].Groups[0].GroupName = "CustomGroup";
                YooAssetProjectSetup.ConfigureEmpty(settings, "OtherPackage", "Assets/OtherContent");
                Assert.That(settings.ShowPackageView, Is.True);
                Assert.That(settings.Packages.Count, Is.EqualTo(1));
                Assert.That(settings.Packages[0].PackageName, Is.EqualTo("DefaultPackage"));
                Assert.That(settings.Packages[0].Groups[0].GroupName, Is.EqualTo("CustomGroup"));
                Assert.That(settings.Packages[0].Groups[0].Collectors.Count, Is.EqualTo(1));
                Assert.That(settings.Packages[0].Groups[0].Collectors[0].CollectPath, Is.EqualTo("Assets/GameContent"));
            }
            finally { Object.DestroyImmediate(settings); }
        }
    }
}
