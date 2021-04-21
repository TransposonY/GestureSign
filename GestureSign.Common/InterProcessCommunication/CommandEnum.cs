namespace GestureSign.Common.InterProcessCommunication
{
    public enum CommandEnum
    {
        StartControlPanel,
        StartTeaching,
        StopTraining,
        LoadApplications,
        LoadGestures,
        LoadConfiguration,
        GotGesture,
        ConfigReload,
        SynTouchPadState,
        Exit
    }
}
