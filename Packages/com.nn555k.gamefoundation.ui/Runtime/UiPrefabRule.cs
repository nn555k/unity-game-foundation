using System;

namespace GameFoundation.UI
{
    [Serializable]
    public sealed class UiPrefabRule
    {
        public UiPrefabKind Kind;
        public string RootSuffix;
        public string ContentRootName;
        public bool RequireDimBackground;
        public string[] OptionalRootChildren = Array.Empty<string>();
        public bool MoveUnknownRootChildrenIntoContent = true;
    }
}
