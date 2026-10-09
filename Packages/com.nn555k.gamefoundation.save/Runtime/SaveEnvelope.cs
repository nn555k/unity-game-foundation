using System;

namespace GameFoundation.Save
{
    [Serializable]
    public sealed class SaveEnvelope
    {
        public int Version;
        public string SavedAtUtc;
        public string Payload;
        public string Checksum;
    }
}
