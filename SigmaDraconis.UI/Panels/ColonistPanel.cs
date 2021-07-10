namespace SigmaDraconis.UI
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Draconis.Shared;
    using Draconis.UI;
    using Language;
    using Shared;
    using World.Fauna;
    using WorldInterfaces;

    public class ColonistPanel : PanelLeft, IThingPanelWithFollowButton
    {
        private readonly ColonistPanelColonistPortrait portrait;
        private readonly ColonistCurrentActionLabel actionLabel;
        private readonly ColonistPanelCardStrip cardStrip;

        private readonly HorizontalStack allowButtonStack;
        private readonly TickBoxIconButton allowBuildButton;
        private readonly TickBoxIconButton allowMaintainButton;
        private readonly TickBoxIconButton allowGatherButton;
        private readonly TickBoxIconButton allowFarmPlantButton;
        private readonly TickBoxIconButton allowFarmHarvestButton;
        private readonly TickBoxIconButton allowFruitHarvestButton;
        private readonly TickBoxIconButton allowLabsBotanistButton;
        private readonly TickBoxIconButton allowLabsGeologistButton;
        private readonly TickBoxIconButton allowLabsEngineerButton;
        private readonly TickBoxIconButton allowGeologyButton;

        private readonly LeftRightPicker workPolicyPicker;
        private readonly LeftRightPicker kekPolicyPicker;
        private readonly SimpleTooltip workPolicyTooltip;
        private readonly SimpleTooltip kekPolicyTooltip;

        private readonly HorizontalStack statDisplayStack;
        private readonly ColonistStatDisplay hungerDisplay;
        private readonly ColonistStatDisplay thirstDisplay;
        private readonly ColonistStatDisplay tirednessDisplay;
        private readonly ColonistStatDisplay stressDisplay;

        private readonly ColonistDietDisplay dietDisplay;
        
        private readonly CameraTrackingIconButton followButton;

        protected Colonist colonist;
        public IThing Thing
        {
            get { return this.colonist; }
            set
            {
                this.colonist = value as Colonist;
                this.titleLabel.Text = $"{this.colonist.ShortName} - {LanguageManager.Get<SkillType>(colonist.Skill)}".ToUpperInvariant();
                this.cardStrip.SetColonist(this.colonist);
                this.portrait.SetColonist(this.colonist);
                this.UpdateAllowButtons();
            }
        }

        public event EventHandler<EventArgs> FollowButtonClick;

        public ColonistPanel(IUIElement parent, int y)
            : base(parent, y, Scale(320), Scale(260), "")
        {
            this.portrait = new ColonistPanelColonistPortrait(this, Scale(8), Scale(22), null);
            this.AddChild(this.portrait);

            this.cardStrip = new ColonistPanelCardStrip(this, Scale(46), Scale(22));
            this.AddChild(this.cardStrip);

            this.actionLabel = new ColonistCurrentActionLabel(this, Scale(46), Scale(52), Scale(266)) { TextAlign = TextAlignment.MiddleCentre };
            this.AddChild(this.actionLabel);

            y = Scale(80);
            this.allowButtonStack = new HorizontalStack(this, 0, y, Scale(320), Scale(20), TextAlignment.MiddleCentre);
            this.AddChild(this.allowButtonStack);

            var activitiesIcon = new Icon("Textures\\Icons\\Activities", 10);

            this.allowGatherButton = new TickBoxIconButton(this.allowButtonStack, 0, 0, Scale(50), Scale(22), activitiesIcon, 0) { IsTicked = true };
            this.allowGatherButton.MouseLeftClick += this.OnAllowGatherButtonClick;
            this.allowButtonStack.AddChild(this.allowGatherButton);
            UIHelper.AddSimpleTooltip(this, this.allowGatherButton, StringsForColonistPanel.Gather);

            this.allowBuildButton = new TickBoxIconButton(this.allowButtonStack, 0, 0, Scale(50), Scale(22), activitiesIcon, 1) { IsTicked = true };
            this.allowBuildButton.MouseLeftClick += this.OnAllowBuildButtonClick;
            this.allowButtonStack.AddChild(this.allowBuildButton);
            UIHelper.AddSimpleTooltip(this, this.allowBuildButton, StringsForColonistPanel.Build);

            this.allowMaintainButton = new TickBoxIconButton(this.allowButtonStack, 0, 0, Scale(50), Scale(22), activitiesIcon, 2) { IsTicked = true };
            this.allowMaintainButton.MouseLeftClick += this.OnAllowMaintainButtonClick;
            this.allowButtonStack.AddChild(this.allowMaintainButton);
            UIHelper.AddSimpleTooltip(this, this.allowMaintainButton, StringsForColonistPanel.Maintain);

            this.allowFruitHarvestButton = new TickBoxIconButton(this.allowButtonStack, 0, 0, Scale(50), Scale(22), activitiesIcon, 5) { IsTicked = true, IsVisible = false };
            this.allowFruitHarvestButton.MouseLeftClick += this.OnAllowFruitHarvestButtonClick;
            this.allowButtonStack.AddChild(this.allowFruitHarvestButton);
            UIHelper.AddSimpleTooltip(this, this.allowFruitHarvestButton, StringsForColonistPanel.FruitHarvest);

            this.allowFarmPlantButton = new TickBoxIconButton(this.allowButtonStack, 0, 0, Scale(50), Scale(22), activitiesIcon, 3) { IsTicked = true, IsVisible = false };
            this.allowFarmPlantButton.MouseLeftClick += this.OnAllowFarmPlantButtonClick;
            this.allowButtonStack.AddChild(this.allowFarmPlantButton);
            UIHelper.AddSimpleTooltip(this, this.allowFarmPlantButton, StringsForColonistPanel.FarmPlant);

            this.allowFarmHarvestButton = new TickBoxIconButton(this.allowButtonStack, 0, 0, Scale(50), Scale(22), activitiesIcon, 4) { IsTicked = true, IsVisible = false };
            this.allowFarmHarvestButton.MouseLeftClick += this.OnAllowFarmHarvestButtonClick;
            this.allowButtonStack.AddChild(this.allowFarmHarvestButton);
            UIHelper.AddSimpleTooltip(this, this.allowFarmHarvestButton, StringsForColonistPanel.FarmHarvest);

            this.allowLabsBotanistButton = new TickBoxIconButton(this.allowButtonStack, 0, 0, Scale(50), Scale(22), activitiesIcon, 6) { IsTicked = true, IsVisible = false };
            this.allowLabsBotanistButton.MouseLeftClick += this.OnAllowLabsBotanyButtonClick;
            this.allowButtonStack.AddChild(this.allowLabsBotanistButton);
            UIHelper.AddSimpleTooltip(this, this.allowLabsBotanistButton, StringsForColonistPanel.ResearchBotanist);

            this.allowLabsGeologistButton = new TickBoxIconButton(this.allowButtonStack, 0, 0, Scale(50), Scale(22), activitiesIcon, 8) { IsTicked = true, IsVisible = false };
            this.allowLabsGeologistButton.MouseLeftClick += this.OnAllowLabsGeologyButtonClick;
            this.allowButtonStack.AddChild(this.allowLabsGeologistButton);
            UIHelper.AddSimpleTooltip(this, this.allowLabsGeologistButton, StringsForColonistPanel.ResearchGeologist);

            this.allowLabsEngineerButton = new TickBoxIconButton(this.allowButtonStack, 0, 0, Scale(50), Scale(22), activitiesIcon, 9) { IsTicked = true, IsVisible = false };
            this.allowLabsEngineerButton.MouseLeftClick += this.OnAllowLabsEngineeringButtonClick;
            this.allowButtonStack.AddChild(this.allowLabsEngineerButton);
            UIHelper.AddSimpleTooltip(this, this.allowLabsEngineerButton, StringsForColonistPanel.ResearchEngineer);

            this.allowGeologyButton = new TickBoxIconButton(this.allowButtonStack, 0, 0, Scale(50), Scale(22), activitiesIcon, 7) { IsTicked = true, IsVisible = false };
            this.allowGeologyButton.MouseLeftClick += this.OnAllowGeologyButtonClick;
            this.allowButtonStack.AddChild(this.allowGeologyButton);
            UIHelper.AddSimpleTooltip(this, this.allowGeologyButton, StringsForColonistPanel.Geology);

            var workPolicySettings = new List<WorkPolicy> { WorkPolicy.None, WorkPolicy.Relaxed, WorkPolicy.Normal, WorkPolicy.Forced };
            this.workPolicyPicker = new LeftRightEnumPicker<WorkPolicy>(this, 40, 108, 240, workPolicySettings, 2);
            this.workPolicyPicker.SelectedIndexChanged += this.OnWorkPolicyPickerSelectedIndexChanged;
            this.AddChild(this.workPolicyPicker);
            this.workPolicyTooltip = UIHelper.AddSimpleTooltip(this, this.workPolicyPicker, "", GetString(StringsForColonistPanel.WorkPolicyDetail), TextAlignment.TopLeft);

            var kekPolicySettings = new List<KekPolicy> { KekPolicy.Never, KekPolicy.Limited, KekPolicy.Normal, KekPolicy.AnyTime };
            this.kekPolicyPicker = new LeftRightEnumPicker<KekPolicy>(this, 40, 128, 240, kekPolicySettings, 2);
            this.kekPolicyPicker.SelectedIndexChanged += this.OnKekPolicyPickerSelectedIndexChanged;
            this.AddChild(this.kekPolicyPicker);
            this.kekPolicyTooltip = UIHelper.AddSimpleTooltip(this, this.kekPolicyPicker, "", GetString(StringsForColonistPanel.KekPolicyDetail), TextAlignment.TopLeft);

            this.statDisplayStack = this.AddChild(new HorizontalStack(this, 0, Scale(152), Scale(320), Scale(22), TextAlignment.MiddleCentre));

            this.hungerDisplay = new ColonistStatDisplay(this.statDisplayStack, 0, 0, StringsForColonistPanel.Hunger, 0.6, 0.8, 0.9, "Textures\\Misc\\ColonistPanelStatBackground");
            this.statDisplayStack.AddChild(this.hungerDisplay);
            this.hungerDisplay.SetTooltipText(GetString(StringsForColonistPanel.HungerDetail));

            this.thirstDisplay = new ColonistStatDisplay(this.statDisplayStack, 0, 0, StringsForColonistPanel.Thirst, 0.6, 0.8, 0.9, "Textures\\Misc\\ColonistPanelStatBackground");
            this.statDisplayStack.AddChild(this.thirstDisplay);
            this.thirstDisplay.SetTooltipText(GetString(StringsForColonistPanel.ThirstDetail));

            this.tirednessDisplay = new ColonistStatDisplay(this.statDisplayStack, 0, 0, StringsForColonistPanel.Tiredness, 0.75, 0.85, 0.95, "Textures\\Misc\\ColonistPanelTirednessBackground");
            this.statDisplayStack.AddChild(this.tirednessDisplay);

            this.stressDisplay = new ColonistStatDisplay(this.statDisplayStack, 0, 0, StringsForColonistPanel.Stress, 0.6, 0.8, 0.9, "Textures\\Misc\\ColonistPanelStatBackground");
            this.statDisplayStack.AddChild(this.stressDisplay);
            this.stressDisplay.SetTooltipText(GetString(StringsForColonistPanel.StressDetail));

            this.followButton = new CameraTrackingIconButton(this, 0, 0);
            this.followButton.MouseLeftClick += this.OnFollowButtonClick;
            this.AddChild(this.followButton);

            this.dietDisplay = this.AddChild(new ColonistDietDisplay(this, Scale(38), y + Scale(102)));
        }

        private void OnKekPolicyPickerSelectedIndexChanged(object sender, EventArgs e)
        {
            this.colonist.SetKekPolicy((KekPolicy)this.kekPolicyPicker.SelectedIndex);
        }

        private void OnWorkPolicyPickerSelectedIndexChanged(object sender, EventArgs e)
        {
            this.colonist.SetWorkPolicy((WorkPolicy)this.workPolicyPicker.SelectedIndex);
        }

        private void UpdateAllowButtons()
        {
            this.allowGatherButton.IsVisible = this.colonist.Skill != SkillType.Programmer;
            this.allowBuildButton.IsVisible = this.colonist.Skill == SkillType.Engineer;
            this.allowMaintainButton.IsVisible = this.colonist.Skill == SkillType.Engineer;
            this.allowFarmHarvestButton.IsVisible = this.colonist.Skill == SkillType.Botanist;
            this.allowFarmPlantButton.IsVisible = this.colonist.Skill == SkillType.Botanist;
            this.allowFruitHarvestButton.IsVisible = this.colonist.Skill == SkillType.Botanist;
            this.allowGeologyButton.IsVisible = this.colonist.Skill == SkillType.Geologist;
            this.allowLabsBotanistButton.IsVisible = this.colonist.Skill == SkillType.Botanist || this.colonist.Skill == SkillType.Programmer;
            this.allowLabsGeologistButton.IsVisible = this.colonist.Skill == SkillType.Geologist || this.colonist.Skill == SkillType.Programmer;
            this.allowLabsEngineerButton.IsVisible = this.colonist.Skill == SkillType.Engineer || this.colonist.Skill == SkillType.Programmer;
            this.allowButtonStack.LayoutInvalidated = true;
        }

        private void OnAllowBuildButtonClick(object sender, MouseEventArgs e)
        {
            this.colonist.WorkPriorities[ColonistPriority.Construct] = this.colonist.WorkPriorities[ColonistPriority.Construct] == 0 ? 9 : 0;
        }

        private void OnAllowMaintainButtonClick(object sender, MouseEventArgs e)
        {
            this.colonist.WorkPriorities[ColonistPriority.Maintain] = this.colonist.WorkPriorities[ColonistPriority.Maintain] == 0 ? 4 : 0;
        }

        private void OnAllowGatherButtonClick(object sender, MouseEventArgs e)
        {
            this.colonist.WorkPriorities[ColonistPriority.Deconstruct] = this.colonist.WorkPriorities[ColonistPriority.Deconstruct] == 0 ? 6 : 0;
            this.colonist.WorkPriorities[ColonistPriority.Haul] = this.colonist.WorkPriorities[ColonistPriority.Haul] == 0 ? 7 : 0;
        }

        private void OnAllowFarmPlantButtonClick(object sender, MouseEventArgs e)
        {
            this.colonist.WorkPriorities[ColonistPriority.FarmPlant] = this.colonist.WorkPriorities[ColonistPriority.FarmPlant] == 0 ? 8 : 0;
        }

        private void OnAllowFarmHarvestButtonClick(object sender, MouseEventArgs e)
        {
            this.colonist.WorkPriorities[ColonistPriority.FarmHarvest] = this.colonist.WorkPriorities[ColonistPriority.FarmHarvest] == 0 ? 8 : 0;
        }

        private void OnAllowFruitHarvestButtonClick(object sender, MouseEventArgs e)
        {
            this.colonist.WorkPriorities[ColonistPriority.FruitHarvest] = this.colonist.WorkPriorities[ColonistPriority.FruitHarvest] == 0 ? 9 : 0;
        }

        private void OnAllowGeologyButtonClick(object sender, MouseEventArgs e)
        {
            this.colonist.WorkPriorities[ColonistPriority.Geology] = this.colonist.WorkPriorities[ColonistPriority.Geology] == 0 ? 5 : 0;
        }

        private void OnAllowLabsBotanyButtonClick(object sender, MouseEventArgs e)
        {
            this.colonist.WorkPriorities[ColonistPriority.ResearchBotanist] = this.colonist.WorkPriorities[ColonistPriority.ResearchBotanist] == 0 ? 3 : 0;
        }

        private void OnAllowLabsGeologyButtonClick(object sender, MouseEventArgs e)
        {
            this.colonist.WorkPriorities[ColonistPriority.ResearchGeologist] = this.colonist.WorkPriorities[ColonistPriority.ResearchGeologist] == 0 ? 3 : 0;
        }

        private void OnAllowLabsEngineeringButtonClick(object sender, MouseEventArgs e)
        {
            this.colonist.WorkPriorities[ColonistPriority.ResearchEngineer] = this.colonist.WorkPriorities[ColonistPriority.ResearchEngineer] == 0 ? 3 : 0;
        }

        private void OnFollowButtonClick(object sender, MouseEventArgs e)
        {
            this.followButton.IsSelected = !this.followButton.IsSelected;
            this.IsContentChangedSinceDraw = true;
            this.FollowButtonClick?.Invoke(this, null);
        }

        public override void Update()
        {
            if (this.IsVisible && this.colonist != null)
            {
                if (this.followButton.IsSelected != (GameScreen.Instance.CameraTrackingThing == this.colonist))
                {
                    this.followButton.IsSelected = !this.followButton.IsSelected;
                    this.IsContentChangedSinceDraw = true;
                }

                this.kekPolicyPicker.SelectedIndex = (int)this.colonist.KekPolicy;
                this.workPolicyPicker.SelectedIndex = (int)this.colonist.WorkPolicy;
                this.dietDisplay.Colonist = this.colonist;

                if (colonist.IsDead)
                {
                    this.hungerDisplay.SetValue(0, 0);
                    this.thirstDisplay.SetValue(0, 0);
                    this.tirednessDisplay.SetValue(0, 0);
                    this.stressDisplay.SetValue(0, 0);
                    this.allowBuildButton.IsTicked = false;
                    this.allowMaintainButton.IsTicked = false;
                    this.allowFarmHarvestButton.IsTicked = false;
                    this.allowFarmPlantButton.IsTicked = false;
                    this.allowFruitHarvestButton.IsTicked = false;
                    this.allowLabsBotanistButton.IsTicked = false;
                    this.allowLabsEngineerButton.IsTicked = false;
                    this.allowLabsGeologistButton.IsTicked = false;
                    this.allowGeologyButton.IsTicked = false;
                    this.allowGatherButton.IsTicked = false;
                }
                else
                {
                    this.allowBuildButton.IsTicked = this.colonist.WorkPriorities[ColonistPriority.Construct] > 0;
                    this.allowMaintainButton.IsTicked = this.colonist.WorkPriorities[ColonistPriority.Maintain] > 0;
                    this.allowFarmHarvestButton.IsTicked = this.colonist.WorkPriorities[ColonistPriority.FarmHarvest] > 0;
                    this.allowFarmPlantButton.IsTicked = this.colonist.WorkPriorities[ColonistPriority.FarmPlant] > 0;
                    this.allowFruitHarvestButton.IsTicked = this.colonist.WorkPriorities[ColonistPriority.FruitHarvest] > 0;
                    this.allowLabsBotanistButton.IsTicked = this.colonist.WorkPriorities[ColonistPriority.ResearchBotanist] > 0;
                    this.allowLabsGeologistButton.IsTicked = this.colonist.WorkPriorities[ColonistPriority.ResearchGeologist] > 0;
                    this.allowLabsEngineerButton.IsTicked = this.colonist.WorkPriorities[ColonistPriority.ResearchEngineer] > 0;
                    this.allowGeologyButton.IsTicked = this.colonist.WorkPriorities[ColonistPriority.Geology] > 0;
                    this.allowGatherButton.IsTicked = this.colonist.WorkPriorities[ColonistPriority.Deconstruct] > 0;
                    this.hungerDisplay.SetValue(this.colonist.HungerDisplay, this.colonist.HungerRateOfChangeDisplay);
                    this.thirstDisplay.SetValue(this.colonist.ThirstDisplay, this.colonist.ThirstRateOfChangeDisplay);
                    this.tirednessDisplay.SetValue(this.colonist.TirednessDisplay, this.colonist.TirednessRateOfChangeDisplay);
                    this.tirednessDisplay.SetTooltipText(this.GetTirednessTooltipText());
                    this.stressDisplay.SetValue(this.colonist.StressDisplay, this.colonist.StressRateOfChangeDisplay);
                }
            }

            base.Update();
        }

        protected override void HandleLanguageChange()
        {
            this.hungerDisplay.SetTooltipText(GetString(StringsForColonistPanel.HungerDetail));
            this.thirstDisplay.SetTooltipText(GetString(StringsForColonistPanel.ThirstDetail));
            this.stressDisplay.SetTooltipText(GetString(StringsForColonistPanel.StressDetail));
            this.tirednessDisplay.SetTooltipText(this.GetTirednessTooltipText());
            this.workPolicyTooltip.SetText(GetString(StringsForColonistPanel.WorkPolicyDetail));
            this.kekPolicyTooltip.SetText(GetString(StringsForColonistPanel.KekPolicyDetail));
            base.HandleLanguageChange();
        }

        private string GetTirednessTooltipText()
        {
            string wakeHoursStr;
            if (this.colonist.Body.IsSleeping)
            {
                wakeHoursStr = GetString(StringsForColonistPanel.Sleeping, this.colonist.ShortName);
            }
            else if (this.colonist.FramesSinceWaking < 3600)
            {
                wakeHoursStr = GetString(StringsForColonistPanel.AwakeForLessThanOneHour, this.colonist.ShortName);
            }
            else if (this.colonist.FramesSinceWaking < 7200)
            {
                wakeHoursStr = GetString(StringsForColonistPanel.AwakeForOneHour, this.colonist.ShortName);
            }
            else
            {
                var hours = this.colonist.FramesSinceWaking / 3600;
                wakeHoursStr = GetString(StringsForColonistPanel.AwakeForNHours, this.colonist.ShortName, hours);
            }

            var detail = GetString(StringsForColonistPanel.TirednessDetail);
            return $"{wakeHoursStr}||{detail}";
        }

        protected static string GetString(StringsForColonistPanel key)
        {
            return LanguageManager.Get<StringsForColonistPanel>(key);
        }

        protected static string GetString(StringsForColonistPanel key, object arg0)
        {
            return LanguageManager.Get<StringsForColonistPanel>(key, arg0);
        }

        protected static string GetString(StringsForColonistPanel key, object arg0, object arg1)
        {
            return LanguageManager.Get<StringsForColonistPanel>(key, arg0, arg1);
        }
    }
}
