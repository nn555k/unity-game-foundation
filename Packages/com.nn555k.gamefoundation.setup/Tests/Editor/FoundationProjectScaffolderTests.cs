using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace GameFoundation.Setup.Editor.Tests
{
    public sealed class FoundationProjectScaffolderTests
    {
        /// <summary>
        /// 验证生成的 App 只注册选中的 Foundation 模块。
        /// </summary>
        [Test]
        public void BuildArchitectureSourceUsesSelectedModules()
        {
            var options = ValidOptions();
            options.IncludeHotUpdate = false;
            options.IncludeSdk = false;

            var source = FoundationProjectScaffolder.BuildArchitectureSource(options);

            StringAssert.Contains("FoundationCoreModule.Register(this)", source);
            StringAssert.Contains("FoundationSaveModule.Register(this)", source);
            StringAssert.Contains("FoundationUiModule.Register(this)", source);
            StringAssert.DoesNotContain("FoundationHotUpdateModule", source);
            StringAssert.DoesNotContain("FoundationSdkModule", source);
        }

        /// <summary>
        /// 验证脚手架拒绝可能逃逸 Assets 的代码根路径。
        /// </summary>
        [Test]
        public void ValidateOptionsRejectsEscapingPath()
        {
            var options = ValidOptions();
            options.CodeRoot = "Assets/../Outside";
            var report = new FoundationProjectSetupReport();

            var valid = FoundationProjectScaffolder.ValidateOptions(options, report);

            Assert.That(valid, Is.False);
            Assert.That(report.Errors.Count, Is.EqualTo(1));
        }

        /// <summary>Blank API options cannot silently generate a NewGame placeholder project.</summary>
        [Test]
        public void ValidateOptionsRequiresExplicitIdentity()
        {
            var report = new FoundationProjectSetupReport();
            Assert.That(FoundationProjectScaffolder.ValidateOptions(new FoundationProjectSetupOptions(), report), Is.False);
        }

        /// <summary>Rejects broad, escaping, malformed and Resources collection roots before any asset writes.</summary>
        [TestCase("Assets")]
        [TestCase("Assets/../Other")]
        [TestCase("Assets/GameContent/Resources")]
        [TestCase("Assets/GameContent/")]
        public void ValidateOptionsRejectsUnsafeContentRoot(string path)
        {
            var options = ValidOptions();
            options.ContentRoot = path;
            Assert.That(FoundationProjectScaffolder.ValidateOptions(options, new FoundationProjectSetupReport()), Is.False);
        }

        /// <summary>
        /// 验证生成的 asmdef 包含 QFramework、Core 与所选能力包。
        /// </summary>
        [Test]
        public void BuildAssemblyDefinitionUsesSelectedReferences()
        {
            var options = ValidOptions();
            options.IncludeSdk = false;

            var json = FoundationProjectScaffolder.BuildAssemblyDefinition(options);

            StringAssert.Contains("QFramework", json);
            StringAssert.Contains("GameFoundation.Core", json);
            StringAssert.Contains("GameFoundation.Save", json);
            StringAssert.DoesNotContain("GameFoundation.Sdk", json);
        }

        /// <summary>Exercises actual Resources loading and prefab instantiation with artwork outside Resources.</summary>
        [Test]
        public void ResourcesPrefabPreservesDirectReferenceArtwork()
        {
            var id = System.Guid.NewGuid().ToString("N");
            var rootPath = "Assets/__FoundationResourceFixture_" + id;
            GameObject source = null;
            GameObject instance = null;
            try
            {
                AssetDatabase.CreateFolder("Assets", "__FoundationResourceFixture_" + id);
                AssetDatabase.CreateFolder(rootPath, "Resources");
                AssetDatabase.CreateFolder(rootPath, "Sprites");
                var texture = new Texture2D(2, 2) { name = "FixtureTexture" };
                var artworkPath = rootPath + "/Sprites/Artwork.asset";
                AssetDatabase.CreateAsset(texture, artworkPath);
                var sprite = Sprite.Create(texture, new Rect(0f, 0f, 2f, 2f), Vector2.one * 0.5f);
                sprite.name = "FixtureSprite";
                AssetDatabase.AddObjectToAsset(sprite, texture);
                AssetDatabase.SaveAssetIfDirty(texture);
                source = new GameObject("FixturePopup", typeof(SpriteRenderer));
                source.GetComponent<SpriteRenderer>().sprite = sprite;
                var loadKey = "FixturePopup_" + id;
                PrefabUtility.SaveAsPrefabAsset(source, rootPath + "/Resources/" + loadKey + ".prefab");
                Object.DestroyImmediate(source);
                source = null;

                var prefab = Resources.Load<GameObject>(loadKey);
                Assert.That(prefab, Is.Not.Null);
                instance = Object.Instantiate(prefab);
                var loadedSprite = instance.GetComponent<SpriteRenderer>().sprite;
                Assert.That(loadedSprite, Is.Not.Null);
                Assert.That(AssetDatabase.GetAssetPath(loadedSprite), Is.EqualTo(artworkPath));
                Assert.That(loadedSprite.texture, Is.Not.Null);
                Assert.That(Resources.Load<GameObject>(loadKey + "_missing"), Is.Null);
            }
            finally
            {
                if (instance != null) Object.DestroyImmediate(instance);
                if (source != null) Object.DestroyImmediate(source);
                AssetDatabase.DeleteAsset(rootPath);
            }
        }

        /// <summary>
        /// 创建一组不会触发资源写入的合法测试选项。
        /// </summary>
        private static FoundationProjectSetupOptions ValidOptions()
        {
            return new FoundationProjectSetupOptions
            {
                ProjectName = "SampleGame",
                RootNamespace = "Studio.SampleGame",
                CodeRoot = "Assets/Scripts/Game",
                IncludeSave = true,
                IncludeHotUpdate = true,
                IncludeSdk = true,
                IncludeUi = true,
                CreateAssemblyDefinition = true,
                CreateUiConventionProfile = true
            };
        }
    }
}
