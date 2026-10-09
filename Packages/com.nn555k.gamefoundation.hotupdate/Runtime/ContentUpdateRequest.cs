using System;

namespace GameFoundation.HotUpdate
{
    public sealed class ContentUpdateRequest
    {
        public string ContentGroup { get; }
        public string ContentId { get; }
        public bool AllowOfflineFallback { get; }

        /// <summary>
        /// 创建不包含业务路径的内容更新请求。
        /// </summary>
        public ContentUpdateRequest(string contentGroup, string contentId, bool allowOfflineFallback = true)
        {
            if (string.IsNullOrWhiteSpace(contentGroup))
            {
                throw new ArgumentException("Content group cannot be empty.", nameof(contentGroup));
            }

            ContentGroup = contentGroup;
            ContentId = contentId ?? string.Empty;
            AllowOfflineFallback = allowOfflineFallback;
        }
    }
}
