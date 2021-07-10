namespace SigmaDraconis.Renderers
{
    using Shared;

    /// <summary>
    /// Seperate shadow render for wind turbines is for performance reasons
    /// </summary>
    public class WindTurbineShadowRenderer : ShadowRendererOld, IRenderer
    {
        protected override bool IsThingTypeIncluded(ThingType thingType)
        {
            return thingType == ThingType.WindTurbine;
        }
    }
}
