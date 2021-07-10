namespace Draconis.Shared
{
    using System;
    using Microsoft.Xna.Framework.Input;

    public enum KeyboardEventType { keyPress, keyHold, keyRelease };

    public class KeyboardEventArgs : EventArgs
    {
        public KeyboardEventType EventType { get; set; }
        public Keys Key;
        public bool Handled { get; set; }

        public KeyboardEventArgs(KeyboardEventType keyboardEventType, Keys key)
        {
            this.EventType = keyboardEventType;
            this.Key = key;
            this.Handled = false;
        }
    }
}
