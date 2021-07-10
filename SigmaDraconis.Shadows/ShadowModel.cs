namespace SigmaDraconis.Shadows
{
    using System.Collections.Generic;
    using System.Linq;

    public class ShadowModel
    {
        public List<ShadowQuad> Quads { get; set; }

        public ShadowModel(List<ShadowQuad> quads)
        {
            this.Quads = quads.ToList();
        }
    }
}
