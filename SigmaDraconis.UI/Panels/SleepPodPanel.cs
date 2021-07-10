namespace SigmaDraconis.UI
{
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using Draconis.Shared;
    using Draconis.UI;
    using Language;
    using Shared;
    using World;
    using WorldInterfaces;
    
    public class SleepPodPanel : BuildingPanel, IThingPanel
    {
        protected readonly PowerButtonWithUsageDisplay powerButton;
        protected readonly TemperatureDisplay temperatureDisplay;
        private readonly TextLabel statusLabel;
        private readonly LeftRightPicker ownerPicker;
        private readonly IconButton confirmButton;
        private readonly SimpleTooltip confirmButtonTooltip;
        private readonly IconButton cancelButton;

        public SleepPodPanel(IUIElement parent, int y)
            : base(parent, y)
        {
            this.powerButton = new PowerButtonWithUsageDisplay(this, Scale(218), Scale(16), displayBoxHeight: true);
            this.AddChild(this.powerButton);
            this.powerButton.MouseLeftClick += this.OnOnOffButtonClick;

            this.temperatureDisplay = new TemperatureDisplay(this, Scale(10), Scale(16));
            this.AddChild(this.temperatureDisplay);

            this.statusLabel = new TextLabel(this, 0, Scale(40), Scale(320), Scale(20), GetString(StringsForThingPanels.SleepPodVacant), UIColour.DefaultText);
            this.AddChild(this.statusLabel);

            this.ownerPicker = new LeftRightPicker(this, Scale(10), Scale(56), Scale(260), new List<string>() { GetString(StringsForThingPanels.OwnerPublic) }, 0) { IsVisible = false };
            this.ownerPicker.Tags = new List<object> { 0 };
            this.AddChild(this.ownerPicker);

            this.confirmButton = new IconButton(this, Scale(272), Scale(56), "Textures\\Icons\\Tick", 1f, true);
            this.confirmButton.MouseLeftClick += this.ConfirmButtonClick;
            this.AddChild(this.confirmButton);

            this.cancelButton = new IconButton(this, Scale(292), Scale(56), "Textures\\Icons\\Cross", 1f, true);
            this.cancelButton.MouseLeftClick += this.CancelButtonClick;
            this.AddChild(this.cancelButton);

            this.confirmButtonTooltip = UIHelper.AddSimpleTooltip(this, this.confirmButton, GetString(StringsForThingPanels.SleepPodOwnerLockInfo));
            UIHelper.AddSimpleTooltip(this, this.powerButton, StringsForThingPanels.SleepPodOnOffTooltip);
        }

        private void ConfirmButtonClick(object sender, MouseEventArgs e)
        {
            if (this.Thing is ISleepPod pod)
            {
                var owner = (int)this.ownerPicker.SelectedTag;
                if (pod.OwnerID.GetValueOrDefault() != (int)this.ownerPicker.SelectedTag && (owner == 0 || pod.OwnerChangeTimer <= 0))
                {
                    pod.OwnerID = owner > 0 ? owner : (int?)null;
                    if (owner > 0) pod.OwnerChangeTimer = 86400;
                }
            }
        }

        private void CancelButtonClick(object sender, MouseEventArgs e)
        {
            if (this.Thing is ISleepPod pod)
            {
                this.ownerPicker.SelectedIndex = this.ownerPicker.Tags.IndexOf(pod.OwnerID.GetValueOrDefault());
                this.confirmButton.IsEnabled = false;
                this.cancelButton.IsEnabled = false;
            }
        }

        private void OnOnOffButtonClick(object sender, MouseEventArgs e)
        {
            this.powerButton.IsOn = !this.powerButton.IsOn;
            (this.building as ISleepPod).IsHeaterSwitchedOn = this.powerButton.IsOn;
        }

        public override void Update()
        {
            if (this.IsBuildingUiVisible && this.Thing is ISleepPod pod)
            {
                this.statusLabel.IsVisible = true;
                this.ownerPicker.IsVisible = true;
                this.confirmButton.IsVisible = true;
                this.temperatureDisplay.IsVisible = true;
                this.powerButton.IsVisible = true;
                this.confirmButton.IsVisible = true;
                this.cancelButton.IsVisible = true;

                this.powerButton.EnergyOutput = -pod.EnergyUseRate.KWh;
                var temperature = (int)Math.Round(pod.Temperature);
                var colour = temperature >= 30 ? UIColour.OrangeText : (temperature >= 0 ? UIColour.GreenText : UIColour.LightBlueText);
                this.temperatureDisplay.SetTemperature(temperature, colour);

                var occupierID = pod.MainTile.ThingsPrimary.OfType<IColonist>().FirstOrDefault()?.Id;
                if (occupierID.HasValue && World.GetThing(occupierID.Value) is IColonist colonist)
                {
                    this.statusLabel.Text = $"{GetString(StringsForThingPanels.OccupiedBy)} {colonist.ShortName} - {GetSkillName(colonist)}.";
                }
                else
                {
                    this.statusLabel.Text = GetString(StringsForThingPanels.SleepPodVacant);
                }

                if (pod.OwnerChangeTimer > 0)
                {
                    var hours = pod.OwnerChangeTimer / 3600;
                    var minutes = (pod.OwnerChangeTimer % 3600) / 60;
                    this.confirmButtonTooltip.SetTitle($"{GetString(StringsForThingPanels.CanReassignIn)} {hours:D2}:{minutes:D2}");
                }
                else
                {
                    this.confirmButtonTooltip.SetTitle(GetString(StringsForThingPanels.SleepPodOwnerLockInfo));
                }

                this.UpdateOwnerPicker(pod, occupierID);

                if (this.powerButton.IsOn != pod.IsHeaterSwitchedOn) this.powerButton.IsOn = pod.IsHeaterSwitchedOn;

                var indexChanged = this.ownerPicker.SelectedIndex != this.ownerPicker.Tags.IndexOf(pod.OwnerID.GetValueOrDefault()) && (pod.OwnerID.GetValueOrDefault() == 0 || pod.OwnerChangeTimer <= 0);
                this.confirmButton.IsEnabled = indexChanged;
                this.cancelButton.IsEnabled = indexChanged;
            }
            else
            {
                this.statusLabel.IsVisible = false;
                this.ownerPicker.IsVisible = false;
                this.confirmButton.IsVisible = false;
                this.temperatureDisplay.IsVisible = false;
                this.powerButton.IsVisible = false;
                this.confirmButton.IsVisible = false;
                this.cancelButton.IsVisible = false;
            }

            base.Update();
        }

        public override void Hide()
        {
            if (this.confirmButton.IsEnabled && this.Thing is ISleepPod p && this.ownerPicker.Tags.Contains(p.OwnerID.GetValueOrDefault()))
            {
                this.ownerPicker.SelectedIndex = this.ownerPicker.Tags.IndexOf(p.OwnerID.GetValueOrDefault());
                this.confirmButton.IsEnabled = false;
                this.cancelButton.IsEnabled = false;
            }

            base.Hide();
        }

        protected override void OnBuildingChanged()
        {
            base.OnBuildingChanged();
            this.Update();
        }

        protected override void HandleLanguageChange()
        {
            if (this.Thing is ISleepPod pod)
            {
                var occupierID = pod.MainTile.ThingsPrimary.OfType<IColonist>().FirstOrDefault()?.Id;
                this.UpdateOwnerPicker(pod, occupierID, true);
            }

            base.HandleLanguageChange();
        }

        private void UpdateOwnerPicker(ISleepPod pod, int? occupierID, bool forceUpdate = false)
        {
            var colonistsWithPods = World.GetThings<ISleepPod>(ThingType.SleepPod).Where(p => p != pod && p.OwnerID.HasValue).Select(p => p.OwnerID.Value).Where(c => c != pod.OwnerID).ToHashSet();
            var colonistsForPicker = World.Colonists.Where(c => 
                (!pod.OwnerID.HasValue || pod.OwnerID == c.Id || pod.OwnerChangeTimer <= 0)
                    && (!occupierID.HasValue || occupierID == c.Id || pod.OwnerID == c.Id)
                    && !colonistsWithPods.Contains(c.Id))
                .ToList();

            if (forceUpdate)
            {
                this.UpdateOwnerPicker(pod, colonistsForPicker);
                return;
            }

            var newTags = new List<int>();
            if (pod.OwnerChangeTimer <= 0) newTags.Add(0);
            newTags.AddRange(colonistsForPicker.Select(a => a.Id));
            if (newTags.Count != this.ownerPicker.Tags.Count)
            {
                this.UpdateOwnerPicker(pod, colonistsForPicker);
                return;
            }

            for (var i = 0; i < newTags.Count; i++)
            {
                if (newTags[i] != (int)this.ownerPicker.Tags[i])
                {
                    this.UpdateOwnerPicker(pod, colonistsForPicker);
                    return;
                }
            }
        }

        private void UpdateOwnerPicker(ISleepPod pod, List<IColonist> allColonists)
        {
            // TODO: Update picker options
            this.ownerPicker.Tags.Clear();
            if (pod.OwnerChangeTimer <= 0) this.ownerPicker.Tags.Add(0);
            this.ownerPicker.Tags.AddRange(allColonists.Select(a => a.Id).OfType<object>());

            var list = new List<string>();
            if (pod.OwnerChangeTimer <= 0) list.Add(GetString(StringsForThingPanels.OwnerPublic));
            list.AddRange(allColonists.Select(a => $"{GetString(StringsForThingPanels.Owner)}: {a.ShortName} - {GetSkillName(a)}"));
            this.ownerPicker.UpdateOptions(list, this.ownerPicker.Tags.IndexOf(pod.OwnerID.GetValueOrDefault()));
        }

        private static string GetSkillName(IColonist colonist)
        {
            return LanguageManager.Get<SkillType>(colonist.Skill);
        }
    }
}
