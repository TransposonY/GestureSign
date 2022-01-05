namespace GestureSign.Common.InterProcessCommunication
{
    public enum IpcCommands
    {
        StartControlPanel,
        StartTeaching,
        StopTraining,
        LoadApplications,
        LoadGestures,
        LoadConfiguration,
        GotGesture,
        ConfigReload,
        SynDeviceState,
        Exit
    }
}
