namespace Draconis.Input
{
    using System;
    using System.Collections.Generic;
    using Microsoft.Xna.Framework.Input;
    using UI;

    public static class KeyboardManager
    {
        private static KeyboardState lastKeyboardState;

        private static readonly List<Keys> handledKeys = new List<Keys>() {
            Keys.A, Keys.B, Keys.C, Keys.D, Keys.E, Keys.F, Keys.G, Keys.H, Keys.I, Keys.J, Keys.K, Keys.L, Keys.M,
            Keys.N, Keys.O, Keys.P, Keys.Q, Keys.R, Keys.S, Keys.T, Keys.U, Keys.V, Keys.W, Keys.X, Keys.Y, Keys.Z,
            Keys.D0, Keys.D1, Keys.D2, Keys.D3, Keys.D4, Keys.D5, Keys.D6, Keys.D7, Keys.D8, Keys.D9,
            Keys.F1, Keys.F2, Keys.F3, Keys.F4, Keys.F5, Keys.F6, Keys.F7, Keys.F8, Keys.F9, Keys.F10, Keys.F11, Keys.F12,
            Keys.NumPad0, Keys.NumPad1, Keys.NumPad2, Keys.NumPad3, Keys.NumPad4, Keys.NumPad5,
            Keys.NumPad6, Keys.NumPad7, Keys.NumPad8, Keys.NumPad9,
            Keys.Home, Keys.End, Keys.PageUp, Keys.PageDown,
            Keys.Add, Keys.Subtract, Keys.Divide, Keys.Multiply, Keys.Decimal,
            Keys.Left, Keys.Right, Keys.Up, Keys.Down,
            Keys.Space, Keys.Enter, Keys.Tab, Keys.Delete, Keys.Back,
            Keys.OemBackslash, Keys.OemCloseBrackets, Keys.OemComma, Keys.OemMinus, Keys.OemOpenBrackets,
            Keys.OemPeriod, Keys.OemPipe, Keys.OemPlus, Keys.OemQuestion, Keys.OemQuotes, Keys.OemSemicolon, Keys.OemTilde,
            Keys.Escape
        };

        private readonly static Dictionary<Keys, DateTime?> keyHoldHandledTimes = new Dictionary<Keys, DateTime?>();
        private readonly static int keyHoldInitialDelay = 500;
        private readonly static int keyHoldRepeatDelay = 50;

        public static IKeyboardHandler FocusedElement = null;

        public static bool IsAlt { get; private set; }
        public static bool IsCtrl { get; private set; }
        public static bool IsShift { get; private set; }

        /// <summary>
        /// Calls all assigned delegate actions.  This method should be called every frame.
        /// </summary>
        public static void Update()
        {
            var currentKeyboardState = Keyboard.GetState();

            var anyKeyDown = false;

            foreach (Keys key in handledKeys)
            {
                if (currentKeyboardState.IsKeyDown(key))
                {
                    anyKeyDown = true;
                    if (lastKeyboardState.IsKeyUp(key) && FocusedElement != null)
                    {
                        FocusedElement.HandleKeyPress(key);
                    }
                }

                if (currentKeyboardState.IsKeyDown(key) && lastKeyboardState.IsKeyDown(key))
                {
                    if (!keyHoldHandledTimes.ContainsKey(key))
                    {
                        keyHoldHandledTimes.Add(key, DateTime.UtcNow.AddMilliseconds(keyHoldInitialDelay));
                    }
                    else if (keyHoldHandledTimes[key] == null)
                    {
                        keyHoldHandledTimes[key] = DateTime.UtcNow.AddMilliseconds(keyHoldInitialDelay);
                    }
                    else if (keyHoldHandledTimes[key].Value.AddMilliseconds(keyHoldRepeatDelay) < DateTime.UtcNow && FocusedElement != null)
                    {
                        FocusedElement.HandleKeyHold(key);
                        keyHoldHandledTimes[key] = DateTime.UtcNow;
                    }
                }
                else if (keyHoldHandledTimes.ContainsKey(key))
                {
                    keyHoldHandledTimes[key] = null;
                }

                if (currentKeyboardState.IsKeyUp(key) && lastKeyboardState.IsKeyDown(key) && FocusedElement != null)
                {
                    FocusedElement.HandleKeyRelease(key);
                }
            }

            if (currentKeyboardState.IsKeyDown(Keys.LeftAlt) || currentKeyboardState.IsKeyDown(Keys.RightAlt))
            {
                IsAlt = true;
            }
            else if (!anyKeyDown)
            {
                IsAlt = false;
            }

            if (currentKeyboardState.IsKeyDown(Keys.LeftShift) || currentKeyboardState.IsKeyDown(Keys.RightShift))
            {
                IsShift = true;
            }
            else if (!anyKeyDown)
            {
                IsShift = false;
            }

            if (currentKeyboardState.IsKeyDown(Keys.LeftControl) || currentKeyboardState.IsKeyDown(Keys.RightControl))
            {
                IsCtrl = true;
            }
            else if (!anyKeyDown)
            {
                IsCtrl = false;
            }

            lastKeyboardState = currentKeyboardState;
        }
    }
}
