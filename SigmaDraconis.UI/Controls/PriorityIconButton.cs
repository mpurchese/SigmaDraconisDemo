namespace SigmaDraconis.UI
{
    using Draconis.UI;
    using Shared;
    using Microsoft.Xna.Framework;

    public class PriorityIconButton : IconButton
    {
        private readonly WorkPriority minPriority;
        private readonly WorkPriority maxPriority;
        private WorkPriority currentPriority = WorkPriority.Normal;

        public WorkPriority PriorityLevel
        {
            get { return this.currentPriority; }
            set { if (this.currentPriority != value) { this.currentPriority = value; this.IsContentChangedSinceDraw = true; } }
        }

        public PriorityIconButton(IUIElement parent, int x, int y, string texturePath, WorkPriority minPriority = WorkPriority.Disabled, WorkPriority maxPriority = WorkPriority.Urgent)
            : base(parent, x, y, texturePath)
        {
            this.minPriority = minPriority;
            this.maxPriority = maxPriority;
        }

        public void IncreasePriority()
        {
            if (this.PriorityLevel < this.maxPriority) this.PriorityLevel++;
            else this.PriorityLevel = this.minPriority;

            this.IsContentChangedSinceDraw = true;
        }

        public void DecreasePriority()
        {
            if (this.PriorityLevel > this.minPriority) this.PriorityLevel--;
            else this.PriorityLevel = this.maxPriority;

            this.IsContentChangedSinceDraw = true;
        }

        protected override void SetWidthAndHeight()
        {
            var size = this.texture.Width / 10;
            this.W = Scale(size);
            this.H = Scale(size);
        }

        protected override Rectangle? GetTextureSourceRect()
        {
            var textureSize = this.texture.Width / 10;
            var y = 0;
            var size = Scale(textureSize);
            if (UIStatics.Scale == 200) y = textureSize * 5 / 2;
            else if (UIStatics.Scale == 150) y = textureSize;
            return new Rectangle((int)this.PriorityLevel * size, y, size, size);
        }
    }
}
