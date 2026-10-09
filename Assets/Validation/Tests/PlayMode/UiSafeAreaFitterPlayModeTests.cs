using System.Collections;
using GameFoundation.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace GameFoundation.Validation.PlayModeTests
{
    public sealed class UiSafeAreaFitterPlayModeTests
    {
        /// <summary>
        /// 验证 Safe Area 组件在真实 MonoBehaviour 生命周期中生成合法锚点。
        /// </summary>
        [UnityTest]
        public IEnumerator SafeAreaFitterAppliesNormalizedAnchors()
        {
            var gameObject = new GameObject("SafeArea", typeof(RectTransform), typeof(UiSafeAreaFitter));
            yield return null;

            var rect = gameObject.GetComponent<RectTransform>();
            Assert.That(rect.anchorMin.x, Is.InRange(0f, 1f));
            Assert.That(rect.anchorMin.y, Is.InRange(0f, 1f));
            Assert.That(rect.anchorMax.x, Is.InRange(0f, 1f));
            Assert.That(rect.anchorMax.y, Is.InRange(0f, 1f));
            Assert.That(rect.anchorMax.x, Is.GreaterThanOrEqualTo(rect.anchorMin.x));
            Assert.That(rect.anchorMax.y, Is.GreaterThanOrEqualTo(rect.anchorMin.y));

            Object.Destroy(gameObject);
            yield return null;
        }
    }
}
