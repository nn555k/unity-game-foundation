using System;
using UnityEngine;

namespace GameFoundation.UI
{
    [CreateAssetMenu(menuName = "Game Foundation/UI Prefab Convention", fileName = "UiPrefabConvention")]
    public sealed class UiPrefabConventionProfile : ScriptableObject
    {
        [SerializeField] private bool mRequireUniqueSiblingNames = true;
        [SerializeField] private bool mRejectGeneratedFigmaNames = true;
        [SerializeField, Range(0f, 1f)] private float mDimBackgroundAlpha = 0.97f;
        [SerializeField] private UiPrefabRule[] mRules = DefaultRules();

        public bool RequireUniqueSiblingNames => mRequireUniqueSiblingNames;
        public bool RejectGeneratedFigmaNames => mRejectGeneratedFigmaNames;
        public float DimBackgroundAlpha => mDimBackgroundAlpha;
        public UiPrefabRule[] Rules => mRules;

        /// <summary>
        /// 查找指定 Prefab 类型的规则，未配置时返回空。
        /// </summary>
        public UiPrefabRule FindRule(UiPrefabKind kind)
        {
            if (mRules == null)
            {
                return null;
            }

            for (var index = 0; index < mRules.Length; index++)
            {
                if (mRules[index] != null && mRules[index].Kind == kind)
                {
                    return mRules[index];
                }
            }

            return null;
        }

        /// <summary>
        /// 由项目 Editor 配置工厂写入项目特定规则。
        /// </summary>
        public void Configure(
            bool requireUniqueSiblingNames,
            bool rejectGeneratedFigmaNames,
            float dimBackgroundAlpha,
            UiPrefabRule[] rules)
        {
            mRequireUniqueSiblingNames = requireUniqueSiblingNames;
            mRejectGeneratedFigmaNames = rejectGeneratedFigmaNames;
            mDimBackgroundAlpha = Mathf.Clamp01(dimBackgroundAlpha);
            mRules = rules ?? Array.Empty<UiPrefabRule>();
        }

        /// <summary>
        /// 恢复不含任何项目业务节点的 Foundation 默认规则。
        /// </summary>
        private void Reset()
        {
            mRequireUniqueSiblingNames = true;
            mRejectGeneratedFigmaNames = true;
            mDimBackgroundAlpha = 0.97f;
            mRules = DefaultRules();
        }

        /// <summary>
        /// 创建 Popup、Page 等基础类型的默认层级规则。
        /// </summary>
        private static UiPrefabRule[] DefaultRules()
        {
            return new[]
            {
                Rule(UiPrefabKind.Popup, "Popup", "PopupContent", true),
                Rule(UiPrefabKind.Page, "Page", "Content", false),
                Rule(UiPrefabKind.Hud, "Hud", "Content", false),
                Rule(UiPrefabKind.Overlay, "Overlay", "Content", false),
                Rule(UiPrefabKind.ListItem, "Item", string.Empty, false),
                Rule(UiPrefabKind.Component, string.Empty, string.Empty, false)
            };
        }

        /// <summary>
        /// 创建单条默认规则并保持项目扩展节点为空。
        /// </summary>
        private static UiPrefabRule Rule(UiPrefabKind kind, string suffix, string contentRootName, bool requireDimBackground)
        {
            return new UiPrefabRule
            {
                Kind = kind,
                RootSuffix = suffix,
                ContentRootName = contentRootName,
                RequireDimBackground = requireDimBackground,
                OptionalRootChildren = Array.Empty<string>(),
                MoveUnknownRootChildrenIntoContent = !string.IsNullOrEmpty(contentRootName)
            };
        }
    }
}
