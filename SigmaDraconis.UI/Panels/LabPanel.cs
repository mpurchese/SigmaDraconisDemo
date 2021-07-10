namespace SigmaDraconis.UI
{
    using Draconis.Shared;
    using Draconis.UI;
    using Language;
    using Shared;
    using System.Collections.Generic;
    using System.Linq;
    using World.Projects;
    using WorldInterfaces;

    public class LabPanel : BuildingPanel, IThingPanel
    {
        private readonly PowerButtonWithUsageDisplay powerButton;
        private readonly SpeedDisplay speedDisplay;
        private readonly WorkRateTooltip speedDisplayTooltip;
        private readonly SimpleTooltip speedDisplayNoModifiersTooltip;
        private readonly BuildingStatusControl statusControl;
        private readonly TextLabel selectedProjectLabel;
        private readonly Dictionary<ProjectButton, int> projectButtons = new Dictionary<ProjectButton, int>();
        private readonly Dictionary<int, ProjectTooltip> projectTooltips = new Dictionary<int, ProjectTooltip>();
        private readonly Dictionary<int, ProgressBar> projectProgressBars = new Dictionary<int, ProgressBar>();
        private int selectedProjectId;

        public LabPanel(IUIElement parent, int y, int h)
            : base(parent, y, h)
        {
            this.powerButton = new PowerButtonWithUsageDisplay(this, Scale(234), Scale(16), 78, true);
            this.AddChild(this.powerButton);
            this.powerButton.MouseLeftClick += this.OnOnOffButtonClick;

            this.speedDisplay = new SpeedDisplay(this, Scale(8), Scale(16));
            this.AddChild(this.speedDisplay);

            this.speedDisplayTooltip = new WorkRateTooltip(TooltipParent.Instance, this.speedDisplay);
            TooltipParent.Instance.AddChild(this.speedDisplayTooltip);

            this.speedDisplayNoModifiersTooltip = UIHelper.AddSimpleTooltip(this, this.speedDisplay, GetString(StringsForThingPanels.WorkRate), GetString(StringsForThingPanels.NoCurrentModifiers));

            this.statusControl = new BuildingStatusControl(this, Scale(36), Scale(40), Scale(248), Scale(20), true);
            this.AddChild(this.statusControl);
            this.statusControl.PriorityChanged += this.OnPriorityChanged;

            this.selectedProjectLabel = new TextLabel(this, 0, Scale(68), this.W, Scale(20), "", UIColour.DefaultText);
            this.AddChild(this.selectedProjectLabel);
            this.UpdateSelectedProjectLabel();
        }

        public override void Update()
        {
            if (this.IsBuildingUiVisible && this.Thing is ILab lab)
            {
                this.powerButton.IsVisible = true;
                this.speedDisplay.IsVisible = true;
                this.statusControl.IsVisible = true;
                this.selectedProjectLabel.IsVisible = true;
                foreach (var button in projectButtons.Keys) button.IsVisible = true;
                foreach (var bar in projectProgressBars.Values) bar.IsVisible = true;

                this.powerButton.IsOn = lab.IsLabSwitchedOn;
                this.powerButton.EnergyOutput = -lab.EnergyUseRate.KWh;

                if (lab.SelectedProjectTypeId != this.selectedProjectId)
                {
                    this.selectedProjectId = lab.SelectedProjectTypeId;
                    this.UpdateSelectedProjectLabel();
                }

                this.statusControl.WorkPriority = lab.LabPriority;
                this.statusControl.ProgressFraction = lab.Progress.GetValueOrDefault();

                if (lab.WorkRateEffects?.Any() == true)
                {
                    this.speedDisplayTooltip.UpdateModifiers(lab.WorkRateEffects.ToDictionary(kv => LanguageManager.GetCardName(kv.Key), kv => kv.Value + 100));
                    this.speedDisplayTooltip.IsEnabled = true;
                    this.speedDisplayNoModifiersTooltip.IsEnabled = false;
                }
                else
                {
                    this.speedDisplayTooltip.IsEnabled = false;
                    this.speedDisplayNoModifiersTooltip.IsEnabled = true;
                }

                switch (lab.LabStatus)
                {
                    case LabStatus.Offline:
                        this.statusControl.SetStatus(BuildingDisplayStatus.Offline, UIColour.RedText);
                        this.speedDisplay.Speed = 0;
                        break;
                    case LabStatus.NoPower:
                        this.statusControl.SetStatus(BuildingDisplayStatus.NoPower, UIColour.GreenText);
                        this.speedDisplay.Speed = 0;
                        break;
                    case LabStatus.SelectProject:
                        this.statusControl.SetStatus(BuildingDisplayStatus.Standby, UIColour.OrangeText);
                        this.speedDisplay.Speed = 0;
                        break;
                    case LabStatus.WaitingForColonist:
                        this.statusControl.SetStatus(BuildingDisplayStatus.Waiting, UIColour.OrangeText);
                        this.speedDisplay.Speed = 0;
                        break;
                    case LabStatus.InProgress:
                        this.statusControl.SetStatus(BuildingDisplayStatus.InProgress, UIColour.GreenText);
                        this.speedDisplay.Speed = lab.WorkRateEffects == null ? 0 : lab.WorkRateEffects.Values.Sum() + 100;
                        break;
                }

                foreach (var pair in this.projectButtons)
                {
                    var project = ProjectManager.GetDefinition(pair.Value);
                    pair.Key.IsOverlayVisible = project.IsDone;
                    pair.Key.IsSelected = pair.Value == lab.SelectedProjectTypeId;
                    pair.Key.IsEnabled = project.IsDone || ProjectManager.CanDoProject(lab.ThingType, pair.Value);
                    pair.Key.IsLocked = !project.IsDone && !ProjectManager.CanDoProject(lab.ThingType, pair.Value);
                    var progressBar = projectProgressBars[pair.Value];
                    progressBar.Fraction = 1.0 - (project.RemainingWork / (double)project.TotalWork);
                    progressBar.BarColour = project.IsDone ? UIColour.GreenText : UIColour.ProgressBar;
                }
            }
            else
            {
                this.powerButton.IsVisible = false;
                this.speedDisplay.IsVisible = false;
                this.statusControl.IsVisible = false;
                this.selectedProjectLabel.IsVisible = false;
                foreach (var button in projectButtons.Keys) button.IsVisible = false;
                foreach (var bar in projectProgressBars.Values) bar.IsVisible = false;
            }

            base.Update();
        }

        protected override void HandleLanguageChange()
        {
            this.speedDisplayNoModifiersTooltip.SetTitle(GetString(StringsForThingPanels.WorkRate));
            this.speedDisplayNoModifiersTooltip.SetText(GetString(StringsForThingPanels.NoCurrentModifiers));
            this.UpdateSelectedProjectLabel();
            base.HandleLanguageChange();
        }

        protected IconButton AddProjectButton(int x, int y, int projectId, string texturePath)
        {
            var button = new ProjectButton(this, Scale(x), Scale(y), texturePath);
            button.MouseLeftClick += this.OnProjectButtonClick;
            this.projectButtons.Add(button, projectId);
            this.AddChild(button);

            var progressBar = new ProgressBar(this, Scale(x), Scale(y + 48), Scale(48), Scale(4)) { BarColour = UIColour.ProgressBar };
            this.projectProgressBars.Add(projectId, progressBar);
            this.AddChild(progressBar);

            var project = ProjectManager.GetDefinition(projectId);
            var title = project.DisplayName.ToUpperInvariant();
            var tooltip = new ProjectTooltip(this, button, project, title);
            TooltipParent.Instance.AddChild(tooltip, this);

            this.projectTooltips.Add(projectId, tooltip);

            return button;
        }

        private void UpdateSelectedProjectLabel()
        {
            var formatStr = GetString(StringsForThingPanels.SelectedProject);
            var project = this.selectedProjectId > 0 ? ProjectManager.GetDefinition(this.selectedProjectId) : null;
            var name = project?.DisplayName.ToUpperInvariant() ?? GetString(StringsForThingPanels.None);
            this.selectedProjectLabel.Text = string.Format(formatStr, name);
        }

        private void OnOnOffButtonClick(object sender, MouseEventArgs e)
        {
            this.powerButton.IsOn = !this.powerButton.IsOn;
            (this.building as ILab).IsLabSwitchedOn = this.powerButton.IsOn;
        }

        private void OnPriorityChanged(object sender, MouseEventArgs e)
        {
            if (this.building is ILab m) m.LabPriority = this.statusControl.WorkPriority;
        }

        private void OnProjectButtonClick(object sender, MouseEventArgs e)
        {
            var button = sender as ProjectButton;
            if (button.IsSelected)
            {
                button.IsSelected = false;
                this.selectedProjectId = 0;
                if (this.building is ILab lab) lab.SetProject(0);
            }
            else if (button.IsEnabled && ProjectManager.CanDoProject(this.building.ThingType, this.projectButtons[button]))
            {
                button.IsSelected = true;
                this.selectedProjectId = this.projectButtons[button];
                if (this.building is ILab lab) lab.SetProject(this.selectedProjectId);
            }

            this.UpdateSelectedProjectLabel();
        }
    }
}
