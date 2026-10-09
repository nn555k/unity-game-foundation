using System.IO;
using GameFoundation.UI.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace GameFoundation.UI.Tests
{
    public sealed class UiPrefabValidatorTests
    {
        private const string TestFolder = "Assets/__GameFoundationUiTests";
        private const string TestPrefabPath = TestFolder + "/SamplePopup.prefab";
        private UiPrefabConventionProfile mProfile;

        /// <summary>
        /// 为每个测试创建独立的默认 Profile 和临时资源目录。
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            if (!AssetDatabase.IsValidFolder(TestFolder))
            {
                AssetDatabase.CreateFolder("Assets", "__GameFoundationUiTests");
            }

            mProfile = ScriptableObject.CreateInstance<UiPrefabConventionProfile>();
        }

        /// <summary>
        /// 删除测试创建的 Prefab 和临时 Profile，不污染项目资源。
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            AssetDatabase.DeleteAsset(TestFolder);
            Object.DestroyImmediate(mProfile);
        }

        /// <summary>
        /// 验证缺少 PopupContent 与 DimBackground 的 Prefab 会被拒绝。
        /// </summary>
        [Test]
        public void ValidateRejectsMissingPopupStructure()
        {
            CreatePrefab();

            var report = UiPrefabValidator.Validate(TestPrefabPath, mProfile, UiPrefabKind.Popup);

            Assert.That(report.IsValid, Is.False);
            Assert.That(report.Issues.Count, Is.GreaterThanOrEqualTo(2));
        }

        /// <summary>
        /// 验证规范化工具可以补齐最小弹窗结构并通过同一校验器。
        /// </summary>
        [Test]
        public void NormalizeCreatesValidPopupStructure()
        {
            CreatePrefab();

            var result = FigmaPrefabNormalizer.Apply(TestPrefabPath, mProfile, UiPrefabKind.Popup);

            Assert.That(result.Validation.IsValid, Is.True, result.Validation.ToMultilineString());
            Assert.That(result.Actions.Count, Is.GreaterThan(0));
        }

        /// <summary>
        /// 创建只含根 RectTransform 的最小测试 Prefab。
        /// </summary>
        private static void CreatePrefab()
        {
            var root = new GameObject(Path.GetFileNameWithoutExtension(TestPrefabPath), typeof(RectTransform));
            PrefabUtility.SaveAsPrefabAsset(root, TestPrefabPath);
            Object.DestroyImmediate(root);
        }
    }
}
