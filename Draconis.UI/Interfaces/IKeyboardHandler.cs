namespace Draconis.UI
{
    using Microsoft.Xna.Framework.Input;

    public interface IKeyboardHandler
    {
        void HandleKeyPress(Keys key);
        void HandleKeyHold(Keys key);
        void HandleKeyRelease(Keys key);
    }
}
