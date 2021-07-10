namespace SigmaDraconis.Renderers
{
    using Draconis.Shared;
    using System.Collections.Generic;
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Content;
    using Microsoft.Xna.Framework.Graphics;
    using Shared;
    using World.Buildings;
    using WorldInterfaces;

    public class PoleShadowRenderer : ShadowRendererOld, IRenderer
    {
        public override void LoadContent(GraphicsDevice graphicsDevice, ContentManager contentManager)
        {
            this.mainTexture = contentManager.Load<Texture2D>("Textures\\Shadows\\PoleShadow");
            this.textureSize = new Vector2(10, 2);
            base.LoadContent(graphicsDevice, contentManager);
            this.validateShadows = false;   // Don't conflict with other renderer when updating
        }

        protected override void LoadBasicEffect()
        {
            base.LoadBasicEffect();
            this.effect.CurrentTechnique = this.effect.Techniques["TreeTrunkShadowTechnique"];
        }

        protected override bool IsThingTypeIncluded(ThingType thingType)
        {
            return thingType.In(ThingType.WindTurbine, ThingType.RocketGantry);
        }

        protected override void GenerateVertexBufferForThing(IThingWithShadow thingWithShadow, List<Vector3> shadowModel, List<int> bufferPositions)
        {
            if (!(thingWithShadow is Building building)) return;

            var array = this.vertexArray as VertexPositionColorTexture[];
            var points = building.GetPoleShadowModel();
            var widthFactors = building.GetPoleShadowModelWidthFactors();
            if (points.Count == 0 || widthFactors.Count == 0) return;

            var bufferPos = 0;
            for (int i = 0; i < points.Count; i += 4)
            {
                var offset = bufferPositions[bufferPos] * 4;
                array[offset] = new VertexPositionColorTexture { Position = points[i], TextureCoordinate = new Vector2(0, 0), Color = new Color((widthFactors[i] + 50f) / 100f, 1, 1, building.ShadowAlpha) };
                array[offset + 1] = new VertexPositionColorTexture { Position = points[i + 1], TextureCoordinate = new Vector2(1, 0), Color = new Color((widthFactors[i + 1] + 50f) / 100f, 1, 1, building.ShadowAlpha) };
                array[offset + 2] = new VertexPositionColorTexture { Position = points[i + 2], TextureCoordinate = new Vector2(1, 1), Color = new Color((widthFactors[i + 2] + 50f) / 100f, 1, 1, building.ShadowAlpha) };
                array[offset + 3] = new VertexPositionColorTexture { Position = points[i + 3], TextureCoordinate = new Vector2(0, 1), Color = new Color((widthFactors[i + 3] + 50f) / 100f, 1, 1, building.ShadowAlpha) };
                bufferPos++;
            }

            WorldRenderer.Instance.InvalidateShadows();
        }
    }
}
