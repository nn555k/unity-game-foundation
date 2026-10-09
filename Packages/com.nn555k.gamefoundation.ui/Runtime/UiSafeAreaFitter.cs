using UnityEngine;

namespace GameFoundation.UI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class UiSafeAreaFitter : MonoBehaviour
    {
        private RectTransform mRectTransform;
        private Rect mLastSafeArea;
        private Vector2Int mLastScreenSize;

        /// <summary>
        /// 缓存 RectTransform，避免刷新路径重复查询组件。
        /// </summary>
        private void Awake()
        {
            mRectTransform = GetComponent<RectTransform>();
        }

        /// <summary>
        /// 启用时立即应用当前设备安全区域。
        /// </summary>
        private void OnEnable()
        {
            ApplyIfChanged(true);
        }

        /// <summary>
        /// 仅在分辨率或安全区域改变时更新锚点，不产生逐帧集合分配。
        /// </summary>
        private void Update()
        {
            ApplyIfChanged(false);
        }

        /// <summary>
        /// 将像素安全区域转换为归一化锚点。
        /// </summary>
        private void ApplyIfChanged(bool force)
        {
            if (!mRectTransform)
            {
                return;
            }

            var safeArea = Screen.safeArea;
            var screenSize = new Vector2Int(Screen.width, Screen.height);
            if (!force && safeArea == mLastSafeArea && screenSize == mLastScreenSize)
            {
                return;
            }

            mLastSafeArea = safeArea;
            mLastScreenSize = screenSize;
            if (screenSize.x <= 0 || screenSize.y <= 0)
            {
                return;
            }

            mRectTransform.anchorMin = new Vector2(safeArea.xMin / screenSize.x, safeArea.yMin / screenSize.y);
            mRectTransform.anchorMax = new Vector2(safeArea.xMax / screenSize.x, safeArea.yMax / screenSize.y);
            mRectTransform.offsetMin = Vector2.zero;
            mRectTransform.offsetMax = Vector2.zero;
        }
    }
}
