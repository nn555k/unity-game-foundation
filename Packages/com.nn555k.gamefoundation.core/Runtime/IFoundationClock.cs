using System;
using QFramework;

namespace GameFoundation.Core
{
    public interface IFoundationClock : IUtility
    {
        DateTimeOffset UtcNow { get; }
        float RealtimeSinceStartup { get; }
    }
}
