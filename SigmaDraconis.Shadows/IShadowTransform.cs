namespace SigmaDraconis.Shadows
{
    using Microsoft.Xna.Framework;

    public interface IShadowTransform
    {
        Vector3 Apply(Vector3 source);
    }
}
