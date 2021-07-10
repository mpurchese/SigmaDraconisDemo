namespace SigmaDraconis.UI
{
    using System;
    using Draconis.Input;
    using Draconis.UI;
    using Microsoft.Xna.Framework;
    using Shared;
    using Settings;
    using Sound;

    public abstract class ScreenBase : Screen
    {
        protected static bool isToggleFullScreenRequested;
        protected static bool isChangeScreenResRequested;
        protected static bool isDisplaySettingsRevertRequested;
        protected static bool isChangingSettings;
        protected static bool autoConfirmSettingsChange;
        protected static int previousDisplayWidth;
        protected static int previousDisplayHeight;
        protected static int windowedDisplayWidth;
        protected static int windowedDisplayHeight;
        protected static bool previousIsFullScreen;

        protected readonly GraphicsDeviceManager graphicsManager;

        protected ConfirmSettingsDialog confirmSettingsDialog;
        protected SettingsDialog optionsDialog;
        protected KeyboardControlsDialog keyboardControlsDialog;

        public event EventHandler<DisplaySettingsChangeRequestEventArgs> DisplaySettingsChangeRequest;
        public event EventHandler<EventArgs> DisplaySettingsChanged;

        public ScreenBase(GameWindow gameWindow, GraphicsDeviceManager deviceManager) : base(gameWindow)
        {
            this.graphicsManager = deviceManager;
            windowedDisplayWidth = SettingsManager.GetSettingInt(SettingGroup.Graphics, SettingNames.WindowScreenSizeX).GetValueOrDefault(1600);
            windowedDisplayHeight = SettingsManager.GetSettingInt(SettingGroup.Graphics, SettingNames.WindowScreenSizeY).GetValueOrDefault(900);
        }

        protected void InitBaseDialogs(IUIElement dialogParent)
        {
            this.optionsDialog = new SettingsDialog(dialogParent, this.graphicsManager.IsFullScreen);
            this.confirmSettingsDialog = new ConfirmSettingsDialog(dialogParent);
            this.keyboardControlsDialog = new KeyboardControlsDialog(dialogParent);
            dialogParent.AddChild(this.optionsDialog);
            dialogParent.AddChild(this.confirmSettingsDialog);
            dialogParent.AddChild(this.keyboardControlsDialog);

            this.optionsDialog.OkClick += this.OnOptionsDialogOk;
            this.optionsDialog.CancelClick += this.OnOptionsDialogClose;

            this.keyboardControlsDialog.OkClick += this.OnKeyboardControlsDialogClose;
            this.keyboardControlsDialog.CancelClick += this.OnKeyboardControlsDialogClose;

            this.confirmSettingsDialog.OkClick += this.ConfirmSettingsDialogOk;
            this.confirmSettingsDialog.CancelClick += this.ConfirmSettingsDialogCancel;
        }

        protected void DoDisplaySettings()
        {
            var changed = false;

            if (isDisplaySettingsRevertRequested)
            {
                this.DisplaySettingsChangeRequest?.Invoke(this, new DisplaySettingsChangeRequestEventArgs(previousDisplayWidth, previousDisplayHeight, this.graphicsManager.IsFullScreen != previousIsFullScreen));
                isDisplaySettingsRevertRequested = false;
                isToggleFullScreenRequested = false;
                isChangeScreenResRequested = false;
                this.optionsDialog.ResetSettings(previousIsFullScreen);
            }
            else if (isChangeScreenResRequested)
            {
                previousDisplayWidth = UIStatics.Graphics.Viewport.Width;
                previousDisplayHeight = UIStatics.Graphics.Viewport.Height;
                if (!this.graphicsManager.IsFullScreen)
                {
                    windowedDisplayWidth = UIStatics.Graphics.Viewport.Width;
                    windowedDisplayHeight = UIStatics.Graphics.Viewport.Height;
                }

                this.DisplaySettingsChangeRequest?.Invoke(this, new DisplaySettingsChangeRequestEventArgs(this.optionsDialog.DisplayWidth, this.optionsDialog.DisplayHeight, false));
                isChangeScreenResRequested = false;
                if (isToggleFullScreenRequested) isChangingSettings = true;
                changed = true;
            }
            else if (isToggleFullScreenRequested)
            {
                previousIsFullScreen = this.graphicsManager.IsFullScreen;

                if (!this.graphicsManager.IsFullScreen && !isChangingSettings)
                {
                    previousDisplayWidth = UIStatics.Graphics.Viewport.Width;
                    previousDisplayHeight = UIStatics.Graphics.Viewport.Height;
                    windowedDisplayWidth = previousDisplayWidth;
                    windowedDisplayHeight = previousDisplayHeight;
                }

                if (this.graphicsManager.IsFullScreen)
                {
                    this.DisplaySettingsChangeRequest?.Invoke(this, new DisplaySettingsChangeRequestEventArgs(windowedDisplayWidth, windowedDisplayHeight, true));
                }
                else
                {
                    this.DisplaySettingsChangeRequest?.Invoke(this, new DisplaySettingsChangeRequestEventArgs(this.optionsDialog.DisplayWidth, this.optionsDialog.DisplayHeight, true));
                }

                isToggleFullScreenRequested = false;
                isChangingSettings = false;
                changed = true;
            }

            if (changed && !isChangingSettings)
            {
                this.optionsDialog.ResetSettings(this.graphicsManager.IsFullScreen);
                if (autoConfirmSettingsChange)
                {
                    this.DisplaySettingsChanged?.Invoke(this, new EventArgs());
                    previousIsFullScreen = this.graphicsManager.IsFullScreen;
                    autoConfirmSettingsChange = false;
                    SettingsManager.SetSetting(SettingGroup.Graphics, SettingNames.IsFullScreen, this.graphicsManager.IsFullScreen);
                    SettingsManager.Save();
                }
                else
                {
                    this.OpenDialog(this.confirmSettingsDialog);
                    this.confirmSettingsDialog.StartTimer();
                }
            }
        }

        public virtual void ToggleFullScreen()
        {
            this.optionsDialog.ToggleIsFullScreen();
            isToggleFullScreenRequested = true;
            autoConfirmSettingsChange = true;
        }

        protected virtual void ConfirmSettingsDialogOk(object sender, EventArgs e)
        {
            this.CloseDialog(this.confirmSettingsDialog);
            this.DisplaySettingsChanged?.Invoke(this, new EventArgs());
        }

        protected virtual void ConfirmSettingsDialogCancel(object sender, EventArgs e)
        {
            isDisplaySettingsRevertRequested = true;
            this.CloseDialog(this.confirmSettingsDialog);
            this.OpenDialog(this.optionsDialog);
        }

        protected virtual void OnOptionsDialogOk(object sender, EventArgs e)
        {
            this.CloseDialog(this.optionsDialog);

            if (this.optionsDialog.IsFullScreen != this.graphicsManager.IsFullScreen) isToggleFullScreenRequested = true;
            if (this.optionsDialog.IsFullScreen && (UIStatics.Graphics.Viewport.Width != this.optionsDialog.DisplayWidth || UIStatics.Graphics.Viewport.Height != this.optionsDialog.DisplayHeight)) isChangeScreenResRequested = true;

            if (this.graphicsManager.IsFullScreen && isToggleFullScreenRequested) autoConfirmSettingsChange = true;

            SoundManager.GlobalVolume = this.optionsDialog.SoundVolume / 100f;
            SettingsManager.SetSetting(SettingGroup.Sound, SettingNames.SoundVolume, this.optionsDialog.SoundVolume);
            SettingsManager.Save();
        }

        protected virtual void OnOptionsDialogClose(object sender, EventArgs e)
        {
            this.optionsDialog.ResetSettings(this.graphicsManager.IsFullScreen);
            this.CloseDialog(this.optionsDialog);
        }

        protected virtual void OnKeyboardControlsDialogClose(object sender, EventArgs e)
        {
            this.CloseDialog(this.keyboardControlsDialog);
        }

        protected virtual void CloseDialog(Dialog dialog)
        {
            dialog.IsVisible = false;
            if (dialog.Parent == ModalBackgroundBox.Instance) ModalBackgroundBox.Instance.IsInteractive = false;
            KeyboardManager.FocusedElement = this;
        }

        protected virtual void OpenDialog(Dialog dialog)
        {
            dialog.Show();
            if (dialog.Parent == ModalBackgroundBox.Instance) ModalBackgroundBox.Instance.IsInteractive = true;
            KeyboardManager.FocusedElement = dialog;
        }
    }
}
