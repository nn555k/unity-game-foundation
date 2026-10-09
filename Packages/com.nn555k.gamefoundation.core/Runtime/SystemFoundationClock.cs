using System;
using UnityEngine;

namespace GameFoundation.Core
{
    public sealed class SystemFoundationClock : IFoundationClock
    {
        public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
        public float RealtimeSinceStartup => Time.realtimeSinceStartup;
    }
}
