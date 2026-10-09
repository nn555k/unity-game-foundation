namespace GameFoundation.Sdk
{
    public enum SdkAdapterState
    {
        NotStarted,
        WaitingForConsent,
        Initializing,
        Ready,
        Failed,
        TimedOut,
        Canceled
    }
}
