namespace SigmaDraconis.UI
{
    using Microsoft.Xna.Framework.Input;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Draconis.Input;
    using Draconis.Shared;
    using Draconis.UI;
    using Language;
    using Settings;
    using Shared;
    using Microsoft.Xna.Framework;

    public class KeyboardControlsDialog : Dialog
    {
        private static readonly HashSet<Keys> handledKeys = new HashSet<Keys>() {
            Keys.A, Keys.B, Keys.C, Keys.D, Keys.E, Keys.F, Keys.G, Keys.H, Keys.I, Keys.J, Keys.K, Keys.L, Keys.M,
            Keys.N, Keys.O, Keys.P, Keys.Q, Keys.R, Keys.S, Keys.T, Keys.U, Keys.V, Keys.W, Keys.X, Keys.Y, Keys.Z,
            Keys.D0, Keys.D1, Keys.D2, Keys.D3, Keys.D4, Keys.D5, Keys.D6, Keys.D7, Keys.D8, Keys.D9,
            Keys.F1, Keys.F2, Keys.F3, Keys.F4, Keys.F5, Keys.F6, Keys.F7, Keys.F8, Keys.F9, Keys.F10, Keys.F11, Keys.F12,
            Keys.NumPad0, Keys.NumPad1, Keys.NumPad2, Keys.NumPad3, Keys.NumPad4, Keys.NumPad5,
            Keys.NumPad6, Keys.NumPad7, Keys.NumPad8, Keys.NumPad9,
            Keys.Home, Keys.End, Keys.PageUp, Keys.PageDown,
            Keys.Add, Keys.Subtract, Keys.Divide, Keys.Multiply, Keys.Decimal,
            Keys.Space, Keys.Enter, Keys.Tab, Keys.Delete, Keys.Back,
            Keys.OemBackslash, Keys.OemCloseBrackets, Keys.OemComma, Keys.OemMinus, Keys.OemOpenBrackets,
            Keys.OemPeriod, Keys.OemPipe, Keys.OemPlus, Keys.OemQuestion, Keys.OemQuotes, Keys.OemSemicolon, Keys.OemTilde, Keys.Escape
        };

        private readonly VerticalScrollBar scrollBar;
        private readonly HorizontalStack buttonStack2;
        private readonly TextButton generalControlsButton;
        private readonly TextButton uiShortcutsButton;
        private readonly TextButton constructionShortcutsButton;
        private readonly TextButton saveButton;
        private readonly TextButton cancelButton;
        private readonly TextButton restoreDefaultsButton;
        private readonly Dictionary<string, KeyboardControlsDialogRow> rows = new Dictionary<string, KeyboardControlsDialogRow>();

        private readonly Dictionary<string, string> generalControlNames = new Dictionary<string, string>();
        private readonly Dictionary<string, string> uiShortcutNames = new Dictionary<string, string>();
        private readonly Dictionary<string, string> constructionShortcutNames = new Dictionary<string, string>();

        private readonly Dictionary<string, string> keySettings = new Dictionary<string, string>();

        private string selectedAction = "";

        public event EventHandler<EventArgs> OkClick;
        public event EventHandler<EventArgs> CancelClick;

        public KeyboardControlsDialog(IUIElement parent)
            : base(parent, Scale(646), Scale(328), GetString(StringsForKeyboardControlsDialog.Title))
        {
            this.IsVisible = false;

            var buttonStack1 = new HorizontalStack(this, 0, Scale(18), this.W, Scale(20), TextAlignment.TopCentre) { Spacing = 8 };
            this.AddChild(buttonStack1);

            this.generalControlsButton = new TextButton(buttonStack1, 0, 0, Scale(202), Scale(20), GetString(StringsForKeyboardControlsDialog.GeneralControls)) { IsHighlighted = true, BorderColourSelected = UIColour.LightBlueText };
            this.generalControlsButton.MouseLeftClick += this.OnGeneralControlsClick;
            buttonStack1.AddChild(this.generalControlsButton);

            this.uiShortcutsButton = new TextButton(buttonStack1, 0, 0, Scale(202), Scale(20), GetString(StringsForKeyboardControlsDialog.UIShortcuts));
            this.uiShortcutsButton.MouseLeftClick += this.OnUIShortcutsClick;
            buttonStack1.AddChild(this.uiShortcutsButton);

            this.constructionShortcutsButton = new TextButton(buttonStack1, 0, 0, Scale(202), Scale(20), GetString(StringsForKeyboardControlsDialog.ConstructionShortcuts));
            this.constructionShortcutsButton.MouseLeftClick += this.OnConstructionShortcutsClick;
            buttonStack1.AddChild(this.constructionShortcutsButton);

            this.scrollBar = new VerticalScrollBar(this, this.W - Scale(30) - 1, Scale(52), Scale(236), 10);
            this.AddChild(this.scrollBar);
            this.scrollBar.ScrollPositionChange += this.OnScrollPositionChange;

            this.scrollBar.IsVisible = true;

            this.buttonStack2 = new HorizontalStack(this, 0, this.H - Scale(30), this.W, Scale(20), TextAlignment.TopCentre) { Spacing = 16 };
            this.AddChild(this.buttonStack2);

            var saveStr = LanguageHelper.GetForButton(StringsForButtons.Save);
            this.saveButton = new TextButton(this.buttonStack2, 0, 0, Scale((saveStr.Length * 7) + 36), Scale(20), saveStr) { TextColour = UIColour.GreenText };
            this.saveButton.MouseLeftClick += this.OnOkClick;
            this.buttonStack2.AddChild(this.saveButton);

            var cancelStr = LanguageHelper.GetForButton(StringsForButtons.Cancel);
            this.cancelButton = new TextButton(this.buttonStack2, 0, 0, Scale((cancelStr.Length * 7) + 36), Scale(20), cancelStr) { TextColour = UIColour.RedText };
            this.cancelButton.MouseLeftClick += this.OnCancelClick;
            this.buttonStack2.AddChild(this.cancelButton);

            var restoreDefaultsStr = LanguageHelper.GetForButton(StringsForButtons.RestoreDefaults);
            this.restoreDefaultsButton = new TextButton(this.buttonStack2, 0, 0, Scale((restoreDefaultsStr.Length * 7) + 36), Scale(20), restoreDefaultsStr);
            this.restoreDefaultsButton.MouseLeftClick += this.OnRestoreDefaultsClick;
            this.buttonStack2.AddChild(this.restoreDefaultsButton);

            this.BuildNames();
        }

        public override void Update()
        {
            if (this.IsVisible && this.Parent is ModalBackgroundBox && this.backgroundColour.A != UIStatics.BackgroundAlpha)
            {
                this.backgroundColour = new Color(0, 0, 0, UIStatics.BackgroundAlpha);
                this.IsContentChangedSinceDraw = true;
            }

            base.Update();
        }

        public void Reset()
        {
            this.generalControlsButton.IsHighlighted = true;
            this.uiShortcutsButton.IsHighlighted = false;
            this.constructionShortcutsButton.IsHighlighted = false;
            this.scrollBar.ScrollPosition = 0;

            this.ResetKeySettings();
            this.UpdateButtons();
        }

        public void ResetKeySettings()
        {
            this.keySettings.Clear();
            foreach (var action in this.generalControlNames.Keys.Concat(this.uiShortcutNames.Keys).Concat(this.constructionShortcutNames.Keys))
            {
                var key = SettingsManager.GetKeysForAction(action).Where(k => !k.In("Left", "Right", "Up", "Down")).FirstOrDefault();
                this.keySettings.Add(action, key);
            }
        }

        public void ResetDefaultKeySettings()
        {
            this.keySettings.Clear();
            foreach (var action in this.generalControlNames.Keys.Concat(this.uiShortcutNames.Keys).Concat(this.constructionShortcutNames.Keys))
            {
                var key = SettingsManager.GetDefaultKeysForAction(action).Where(k => !k.In("Left", "Right", "Up", "Down")).FirstOrDefault();
                this.keySettings.Add(action, key);
            }
        }

        public override void HandleKeyRelease(Keys key)
        {
            if (this.IsVisible)
            {
                if (key == Keys.Escape)
                {
                    this.CancelClick?.Invoke(this, new EventArgs());
                }
                else if (key == Keys.Enter)
                {
                    this.OkClick?.Invoke(this, new EventArgs());
                }
            }

            base.HandleKeyRelease(key);
        }

        private void OnScrollPositionChange(object sender, EventArgs e)
        {
            this.UpdateButtons();
        }

        private void UpdateButtons()
        {
            this.selectedAction = "";
            if (this.generalControlsButton.IsHighlighted) this.UpdateButtons(this.generalControlNames);
            else if (this.uiShortcutsButton.IsHighlighted) this.UpdateButtons(this.uiShortcutNames);
            else if (this.constructionShortcutsButton.IsHighlighted) this.UpdateButtons(this.constructionShortcutNames);
        }

        private void UpdateButtons(Dictionary<string, string> controlNames)
        {
            foreach (var row in this.rows.Values) this.RemoveChild(row);
            this.rows.Clear();

            this.scrollBar.IsVisible = controlNames.Count > 10;
            this.scrollBar.MaxScrollPosition = controlNames.Count - 10;
            this.scrollBar.FractionVisible = 10f / controlNames.Count;

            var y = 52;
            var r = this.scrollBar.IsVisible ? 54 : 36;
            foreach (var action in controlNames.Keys.Skip(this.scrollBar.ScrollPosition).Take(10))
            {
                this.AddRow(action, controlNames[action], y, r);
                y += 24;
            }
        }

        private void AddRow(string action, string name, int y, int r)
        {
            if (!keySettings.ContainsKey(action)) keySettings.Add(action, SettingsManager.GetKeysForAction(action).Where(k => !k.In("Left", "Right", "Up", "Down")).FirstOrDefault());
            var key = keySettings[action];

            var x = this.W - Scale(r);   // Right-to left, nameButton is the filler 

            var row = new KeyboardControlsDialogRow(this, y, r, action, name, key);
            row.BeginChange += this.OnBeginChange;
            row.CancelChange += this.OnCancelChange;
            row.Clear += this.OnClear;

            this.rows.Add(action, row);
            this.AddChild(row);
        }

        private void OnBeginChange(object sender, EventArgs e)
        {
            var row = sender as KeyboardControlsDialogRow;
            this.selectedAction = row.Action;

            foreach (var otherRow in this.rows.Values.Where(r => r != row).ToList()) otherRow.IsEnabled = false;
        }

        private void OnCancelChange(object sender, EventArgs e)
        {
            this.selectedAction = "";

            foreach (var row in this.rows.Values.ToList()) row.IsEnabled = true;
        }

        private void OnClear(object sender, EventArgs e)
        {
            var row = sender as KeyboardControlsDialogRow;
            this.keySettings[row.Action] = "";
            this.UpdateButtons();
        }

        private void OnOkClick(object sender, MouseEventArgs e)
        {
            foreach (var kv in this.keySettings)
            {
                var oldKeys = SettingsManager.GetKeysForAction(kv.Key).Where(k => !k.In("Left", "Right", "Up", "Down")).ToList();
                foreach (var key in oldKeys.Where(k => k != kv.Value)) SettingsManager.RemoveKeySetting(key);
                if (!string.IsNullOrWhiteSpace(kv.Value) && !oldKeys.Contains(kv.Value)) SettingsManager.SetSetting(SettingGroup.Keys, kv.Value, kv.Key);
            }

            SettingsManager.Save();
            this.OkClick?.Invoke(this, new EventArgs());
        }

        private void OnGeneralControlsClick(object sender, MouseEventArgs e)
        {
            if (this.generalControlsButton.IsHighlighted) return;

            this.generalControlsButton.IsHighlighted = true;
            this.uiShortcutsButton.IsHighlighted = false;
            this.constructionShortcutsButton.IsHighlighted = false;
            this.scrollBar.ScrollPosition = 0;
            this.UpdateButtons();
        }

        private void OnUIShortcutsClick(object sender, MouseEventArgs e)
        {
            if (this.uiShortcutsButton.IsHighlighted) return;

            this.generalControlsButton.IsHighlighted = false;
            this.uiShortcutsButton.IsHighlighted = true;
            this.constructionShortcutsButton.IsHighlighted = false;
            this.scrollBar.ScrollPosition = 0;
            this.UpdateButtons();
        }

        private void OnConstructionShortcutsClick(object sender, MouseEventArgs e)
        {
            if (this.constructionShortcutsButton.IsHighlighted) return;

            this.generalControlsButton.IsHighlighted = false;
            this.uiShortcutsButton.IsHighlighted = false;
            this.constructionShortcutsButton.IsHighlighted = true;
            this.scrollBar.ScrollPosition = 0;
            this.UpdateButtons();
        }

        private void OnCancelClick(object sender, MouseEventArgs e)
        {
            this.CancelClick?.Invoke(this, new EventArgs());
        }

        private void OnRestoreDefaultsClick(object sender, MouseEventArgs e)
        {
            this.ResetDefaultKeySettings();
            this.UpdateButtons();
        }

        public override void HandleKeyPress(Keys key)
        {
            if (string.IsNullOrEmpty(this.selectedAction) || !handledKeys.Contains(key)) return;

            var keyName = key.ToString();
            if (key == Keys.OemOpenBrackets) keyName = "[";
            else if (key == Keys.OemCloseBrackets) keyName = "]";
            else if (key == Keys.OemPlus || key == Keys.Add) keyName = "+";
            else if (key == Keys.OemMinus || key == Keys.Subtract) keyName = "-";

            var sb = new StringBuilder();
            if (KeyboardManager.IsAlt) sb.Append("alt-");
            if (KeyboardManager.IsCtrl) sb.Append("ctrl-");
            if (KeyboardManager.IsShift) sb.Append("shift-");
            sb.Append(keyName);

            keyName = sb.ToString();
            foreach (var k in this.keySettings.Keys.ToList())
            {
                if (k == this.selectedAction) this.keySettings[k] = keyName;
                else if (this.keySettings[k] == keyName) this.keySettings[k] = "";
            }

            this.keySettings[this.selectedAction] = sb.ToString();
            this.UpdateButtons();
        }

        protected override void HandleLanguageChange()
        {
            this.titleLabel.Text = GetString(StringsForKeyboardControlsDialog.Title);

            this.generalControlsButton.Text = GetString(StringsForKeyboardControlsDialog.GeneralControls);
            this.constructionShortcutsButton.Text = GetString(StringsForKeyboardControlsDialog.ConstructionShortcuts);
            this.uiShortcutsButton.Text = GetString(StringsForKeyboardControlsDialog.UIShortcuts);

            this.saveButton.Text = LanguageHelper.GetForButton(StringsForButtons.Save);
            this.saveButton.W = Scale((this.saveButton.Text.Length * 7) + 36);
            this.cancelButton.Text = LanguageHelper.GetForButton(StringsForButtons.Cancel);
            this.cancelButton.W = Scale((this.cancelButton.Text.Length * 7) + 36);
            this.restoreDefaultsButton.Text = LanguageHelper.GetForButton(StringsForButtons.RestoreDefaults);
            this.restoreDefaultsButton.W = Scale((this.restoreDefaultsButton.Text.Length * 7) + 36);
            this.buttonStack2.LayoutInvalidated = true;

            this.BuildNames();
            this.UpdateButtons();

            base.HandleLanguageChange();
        }

        private void BuildNames()
        {
            this.generalControlNames.Clear();
            this.generalControlNames.Add("Scroll:Left", GetString(StringsForKeyboardControlsDialog.ScrollLeft));
            this.generalControlNames.Add("Scroll:Right", GetString(StringsForKeyboardControlsDialog.ScrollRight));
            this.generalControlNames.Add("Scroll:Up", GetString(StringsForKeyboardControlsDialog.ScrollUp));
            this.generalControlNames.Add("Scroll:Down", GetString(StringsForKeyboardControlsDialog.ScrollDown));
            this.generalControlNames.Add("Zoom:In", GetString(StringsForKeyboardControlsDialog.ZoomIn));
            this.generalControlNames.Add("Zoom:Out", GetString(StringsForKeyboardControlsDialog.ZoomOut));
            this.generalControlNames.Add("GameSpeed:Increase", GetString(StringsForKeyboardControlsDialog.IncreaseGameSpeed));
            this.generalControlNames.Add("GameSpeed:Decrease", GetString(StringsForKeyboardControlsDialog.DecreaseGameSpeed));
            this.generalControlNames.Add("TogglePause", GetString(StringsForKeyboardControlsDialog.TogglePause));
            this.generalControlNames.Add("ToggleFullScreen", GetString(StringsForKeyboardControlsDialog.ToggleFullScreen));
            this.generalControlNames.Add("RotateBlueprint:Left", GetString(StringsForKeyboardControlsDialog.RotateBlueprintLeft));
            this.generalControlNames.Add("RotateBlueprint:Right", GetString(StringsForKeyboardControlsDialog.RotateBlueprintRight));
            this.generalControlNames.Add("Locate:Colonist", GetString(StringsForKeyboardControlsDialog.LocateColonist));
            this.generalControlNames.Add("Locate:Lander", GetString(StringsForKeyboardControlsDialog.LocateLander));
            this.generalControlNames.Add("CameraTrack", GetString(StringsForKeyboardControlsDialog.CameraTrack));
            this.generalControlNames.Add("Screenshot", GetString(StringsForKeyboardControlsDialog.Screenshot));
            this.generalControlNames.Add("ScreenshotNoUI", GetString(StringsForKeyboardControlsDialog.ScreenshotNoUI));
            this.generalControlNames.Add("ToggleFrameRate", GetString(StringsForKeyboardControlsDialog.ToggleFrameRate));
            this.generalControlNames.Add("ToggleUI", GetString(StringsForKeyboardControlsDialog.ToggleUI));

            this.uiShortcutNames.Clear();
            this.uiShortcutNames.Add("Deconstruct", GetString(StringsForKeyboardControlsDialog.Deconstruct));
            this.uiShortcutNames.Add("Geology", GetString(StringsForKeyboardControlsDialog.Geology));
            this.uiShortcutNames.Add("Harvest", GetString(StringsForKeyboardControlsDialog.Harvest));
            this.uiShortcutNames.Add("Construct", GetString(StringsForKeyboardControlsDialog.Construct));
            this.uiShortcutNames.Add("Farm", GetString(StringsForKeyboardControlsDialog.Farm));
            this.uiShortcutNames.Add("Options", GetString(StringsForKeyboardControlsDialog.Options));
            this.uiShortcutNames.Add("Help", GetString(StringsForKeyboardControlsDialog.Help));
            this.uiShortcutNames.Add("Debug", GetString(StringsForKeyboardControlsDialog.Debug));
            this.uiShortcutNames.Add("CommentArchive", GetString(StringsForKeyboardControlsDialog.CommentArchive));
            this.uiShortcutNames.Add("Mothership", GetString(StringsForKeyboardControlsDialog.Mothership));
            this.uiShortcutNames.Add("ResourceMap", GetString(StringsForKeyboardControlsDialog.ResourceMap));
            this.uiShortcutNames.Add("ToggleRoof", GetString(StringsForKeyboardControlsDialog.ToggleRoof));
            this.uiShortcutNames.Add("Temperature", GetString(StringsForKeyboardControlsDialog.Temperature));

            var buildFormat = GetString(StringsForKeyboardControlsDialog.BuildThing);
            this.constructionShortcutNames.Clear();
            this.constructionShortcutNames.Add("Build:AlgaePool", string.Format(buildFormat, LanguageManager.GetName(ThingType.AlgaePool)));
            this.constructionShortcutNames.Add("Build:Battery", string.Format(buildFormat, LanguageManager.GetName(ThingType.Battery)));
            this.constructionShortcutNames.Add("Build:BiomassPower", string.Format(buildFormat, LanguageManager.GetName(ThingType.BiomassPower)));
            this.constructionShortcutNames.Add("Build:CoalPower", string.Format(buildFormat, LanguageManager.GetName(ThingType.CoalPower)));
            this.constructionShortcutNames.Add("Build:Biolab", string.Format(buildFormat, LanguageManager.GetName(ThingType.Biolab)));
            this.constructionShortcutNames.Add("Build:CharcoalMaker", string.Format(buildFormat, LanguageManager.GetName(ThingType.CharcoalMaker)));
            this.constructionShortcutNames.Add("Build:CompositesFactory", string.Format(buildFormat, LanguageManager.GetName(ThingType.CompositesFactory)));
            this.constructionShortcutNames.Add("Build:CompostFactory", string.Format(buildFormat, LanguageManager.GetName(ThingType.CompostFactory)));
            this.constructionShortcutNames.Add("Build:ConduitNode", string.Format(buildFormat, LanguageManager.GetName(ThingType.ConduitNode)));
            this.constructionShortcutNames.Add("Build:Cooker", string.Format(buildFormat, LanguageManager.GetName(ThingType.Cooker)));
            this.constructionShortcutNames.Add("Build:DirectionalHeater", string.Format(buildFormat, LanguageManager.GetName(ThingType.DirectionalHeater)));
            this.constructionShortcutNames.Add("Build:Door", string.Format(buildFormat, LanguageManager.GetName(ThingType.Door)));
            this.constructionShortcutNames.Add("Build:ElectricFurnace", string.Format(buildFormat, LanguageManager.GetName(ThingType.ElectricFurnace)));
            this.constructionShortcutNames.Add("Build:FuelFactory", string.Format(buildFormat, LanguageManager.GetName(ThingType.FuelFactory)));
            this.constructionShortcutNames.Add("Build:EnvironmentControl", string.Format(buildFormat, LanguageManager.GetName(ThingType.EnvironmentControl)));
            this.constructionShortcutNames.Add("Build:FoodDispenser", string.Format(buildFormat, LanguageManager.GetName(ThingType.FoodDispenser)));
            this.constructionShortcutNames.Add("Build:FoodStorage", string.Format(buildFormat, LanguageManager.GetName(ThingType.FoodStorage)));
            this.constructionShortcutNames.Add("Build:FoundationMetal", string.Format(buildFormat, LanguageManager.GetName(ThingType.FoundationMetal)));
            this.constructionShortcutNames.Add("Build:FoundationStone", string.Format(buildFormat, LanguageManager.GetName(ThingType.FoundationStone)));
            this.constructionShortcutNames.Add("Build:Generator", string.Format(buildFormat, LanguageManager.GetName(ThingType.Generator)));
            this.constructionShortcutNames.Add("Build:GeologyLab", string.Format(buildFormat, LanguageManager.GetName(ThingType.GeologyLab)));
            this.constructionShortcutNames.Add("Build:GlassFactory", string.Format(buildFormat, LanguageManager.GetName(ThingType.GlassFactory)));
            this.constructionShortcutNames.Add("Build:HydrogenBurner", string.Format(buildFormat, LanguageManager.GetName(ThingType.HydrogenBurner)));
            this.constructionShortcutNames.Add("Build:HydrogenStorage", string.Format(buildFormat, LanguageManager.GetName(ThingType.HydrogenStorage)));
            this.constructionShortcutNames.Add("Build:ItemsStorage", string.Format(buildFormat, LanguageManager.GetName(ThingType.ItemsStorage)));
            this.constructionShortcutNames.Add("Build:KekDispenser", string.Format(buildFormat, LanguageManager.GetName(ThingType.KekDispenser)));
            this.constructionShortcutNames.Add("Build:KekFactory", string.Format(buildFormat, LanguageManager.GetName(ThingType.KekFactory)));
            this.constructionShortcutNames.Add("Build:Lamp", string.Format(buildFormat, LanguageManager.GetName(ThingType.Lamp)));
            this.constructionShortcutNames.Add("Build:LaunchPad", string.Format(buildFormat, LanguageManager.GetName(ThingType.LaunchPad)));
            this.constructionShortcutNames.Add("Build:MaterialsLab", string.Format(buildFormat, LanguageManager.GetName(ThingType.MaterialsLab)));
            this.constructionShortcutNames.Add("Build:Mine", string.Format(buildFormat, LanguageManager.GetName(ThingType.Mine)));
            this.constructionShortcutNames.Add("Build:MushFactory", string.Format(buildFormat, LanguageManager.GetName(ThingType.MushFactory)));
            this.constructionShortcutNames.Add("Build:PlanterHydroponics", string.Format(buildFormat, LanguageManager.GetName(ThingType.PlanterHydroponics)));
            this.constructionShortcutNames.Add("Build:PlanterStone", string.Format(buildFormat, LanguageManager.GetName(ThingType.PlanterStone)));
            this.constructionShortcutNames.Add("Build:ResourceProcessor", string.Format(buildFormat, LanguageManager.GetName(ThingType.ResourceProcessor)));
            this.constructionShortcutNames.Add("Build:Roof", string.Format(buildFormat, LanguageManager.GetName(ThingType.Roof)));
            this.constructionShortcutNames.Add("Build:ShorePump", string.Format(buildFormat, LanguageManager.GetName(ThingType.ShorePump)));
            this.constructionShortcutNames.Add("Build:Silo", string.Format(buildFormat, LanguageManager.GetName(ThingType.Silo)));
            this.constructionShortcutNames.Add("Build:SleepPod", string.Format(buildFormat, LanguageManager.GetName(ThingType.SleepPod)));
            this.constructionShortcutNames.Add("Build:SoilSynthesiser", string.Format(buildFormat, LanguageManager.GetName(ThingType.SoilSynthesiser)));
            this.constructionShortcutNames.Add("Build:SolarCellFactory", string.Format(buildFormat, LanguageManager.GetName(ThingType.SolarCellFactory)));
            this.constructionShortcutNames.Add("Build:SolarPanelArray", string.Format(buildFormat, LanguageManager.GetName(ThingType.SolarPanelArray)));
            this.constructionShortcutNames.Add("Build:StoneFurnace", string.Format(buildFormat, LanguageManager.GetName(ThingType.StoneFurnace)));
            this.constructionShortcutNames.Add("Build:TableMetal", string.Format(buildFormat, LanguageManager.GetName(ThingType.TableMetal)));
            this.constructionShortcutNames.Add("Build:TableStone", string.Format(buildFormat, LanguageManager.GetName(ThingType.TableStone)));
            this.constructionShortcutNames.Add("Build:Wall", string.Format(buildFormat, LanguageManager.GetName(ThingType.Wall)));
            this.constructionShortcutNames.Add("Build:WaterDispenser", string.Format(buildFormat, LanguageManager.GetName(ThingType.WaterDispenser)));
            this.constructionShortcutNames.Add("Build:WaterPump", string.Format(buildFormat, LanguageManager.GetName(ThingType.WaterPump)));
            this.constructionShortcutNames.Add("Build:WaterStorage", string.Format(buildFormat, LanguageManager.GetName(ThingType.WaterStorage)));
            this.constructionShortcutNames.Add("Build:WindTurbine", string.Format(buildFormat, LanguageManager.GetName(ThingType.WindTurbine)));
        }

        private static string GetString(StringsForKeyboardControlsDialog key)
        {
            return LanguageManager.Get(typeof(StringsForKeyboardControlsDialog), key);
        }
    }
}
