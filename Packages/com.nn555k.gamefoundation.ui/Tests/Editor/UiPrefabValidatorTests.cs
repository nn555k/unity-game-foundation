using System.IO;
using GameFoundation.UI.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

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

        /// <summary>Structural normalization preserves both authored fitting choices and unrelated button presentation/input.</summary>
        [TestCase(false, Image.Type.Simple)]
        [TestCase(true, Image.Type.Sliced)]
        public void NormalizePreservesAuthoredButtonSettings(bool preserveAspect, Image.Type imageType)
        {
            var root = new GameObject("ImportedPopup", typeof(RectTransform));
            var buttonObject = new GameObject("AuthoredAction", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(root.transform, false);
            var image = buttonObject.GetComponent<Image>();
            image.preserveAspect = preserveAspect; image.type = imageType;
            image.color = new Color(.3f, .5f, .7f, .9f);
            var button = buttonObject.GetComponent<Button>();
            button.interactable = false;
            var colors = button.colors; colors.disabledColor = Color.magenta; button.colors = colors;
            var label = new GameObject("Caption", typeof(RectTransform), typeof(Text));
            label.transform.SetParent(buttonObject.transform, false);
            label.GetComponent<Text>().text = "Project-authored caption";
            PrefabUtility.SaveAsPrefabAsset(root, TestPrefabPath);
            Object.DestroyImmediate(root);
            var before = File.ReadAllBytes(TestPrefabPath);
            FigmaPrefabNormalizer.Preview(TestPrefabPath, mProfile, UiPrefabKind.Popup);
            CollectionAssert.AreEqual(before, File.ReadAllBytes(TestPrefabPath));
            var result = FigmaPrefabNormalizer.Apply(TestPrefabPath, mProfile, UiPrefabKind.Popup);
            Assert.That(result.Validation.IsValid, Is.True);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(TestPrefabPath);
            button = prefab.GetComponentInChildren<Button>(true);
            image = button.GetComponent<Image>();
            Assert.That(image.preserveAspect, Is.EqualTo(preserveAspect));
            Assert.That(image.type, Is.EqualTo(imageType));
            Assert.That(image.color, Is.EqualTo(new Color(.3f, .5f, .7f, .9f)));
            Assert.That(button.interactable, Is.False);
            Assert.That(button.colors.disabledColor, Is.EqualTo(Color.magenta));
            Assert.That(button.GetComponentInChildren<Text>(true).text, Is.EqualTo("Project-authored caption"));
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
