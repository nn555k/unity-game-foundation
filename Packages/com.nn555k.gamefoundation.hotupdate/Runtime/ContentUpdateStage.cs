namespace GameFoundation.HotUpdate
{
    public enum ContentUpdateStage
    {
        Idle,
        Checking,
        Downloading,
        Verifying,
        Committing,
        RollingBack,
        Completed,
        Failed,
        Canceled
    }
}
