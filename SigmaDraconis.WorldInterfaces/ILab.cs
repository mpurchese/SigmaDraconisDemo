namespace SigmaDraconis.WorldInterfaces
{
    using Shared;

    public interface ILab : IColonistJobProvider, IColonistInteractive, IBuildableThing, IEnergyConsumer
    {
        Energy UpdateLab();
        void SetProject(int projectTypeId);
        LabStatus LabStatus { get; }
        double? Progress { get; }
        int SelectedProjectTypeId { get; }
        string ScreenFrame { get; }
        WorkPriority LabPriority { get; set; }
        //string ScreenFrameL { get; }
        //string ScreenFrameC { get; }
        //string ScreenFrameR { get; }
        int AssignedColonistDistance { get; set; }
        bool IsLabSwitchedOn { get; set; }
    }
}
