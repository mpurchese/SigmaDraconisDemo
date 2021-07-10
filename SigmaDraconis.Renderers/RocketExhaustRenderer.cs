namespace SigmaDraconis.Renderers
{
    using System;
    using System.Linq;

    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;

    using World;
    using World.Particles;
    using Draconis.Shared;
    using Shared;

    public class RocketExhaustRenderer : RendererBase, IRenderer
    {
        private long lastUpdateFrame;

        public int Layer { get; set; } = 1;

        public override void LoadContent()
        {
            base.LoadContent();
            this.effect = Content.Load<Effect>("Effects\\SmokeEffect").Clone();
            this.effect.Parameters["xTexture"].SetValue(Content.Load<Texture2D>("Textures\\Particles\\Smoke"));
        }

        protected override void GenerateBuffersForInvalidatedRows()
        {
            if (World.WorldTime.FrameNumber >= this.lastUpdateFrame && World.WorldTime.FrameNumber < this.lastUpdateFrame + 2) return; // Update at 30fps
            this.lastUpdateFrame = World.WorldTime.FrameNumber;

            foreach (var row in RocketExhaustSimulator.RowsForRendererUpdate.Union(LanderExhaustSimulator.RowsForRendererUpdate)) this.GenerateBuffersForRow(row);

            if (this.Layer == 2)
            {
                LanderExhaustSimulator.ClearRowsForRendererUpdate();
            }
        }

        public override void Update(WorldTime time, Vector2f scrollPos, float zoom, bool isPaused = false)
        {
            var light = 1f - World.WorldLight.NightLightFactor;
            var r = light + (0.35f * (1f - light));
            var g = light + (0.5f * (1f - light));
            var b = light + (0.7f * (1f - light));
            this.effect.Parameters["xColour"].SetValue(new Vector4(r, g, b, 1));
            this.effect.CurrentTechnique = this.effect.Techniques["SimpleTechnique"];
            base.Update(time, scrollPos, zoom, isPaused);
        }

        protected override void GenerateBuffersForRow(int rowNum)
        {
            var particles1 = RocketExhaustSimulator.GetParticlesForRenderer(rowNum, this.Layer);
            var particles2 = LanderExhaustSimulator.GetParticlesForRenderer(rowNum, this.Layer);
            var bufferSize = ((particles1?.Length ?? 0) + (particles2?.Length ?? 0)) * 4;
            VertexPositionColorTexture[] vertex = new VertexPositionColorTexture[bufferSize];
            int i = 0;

            if (bufferSize > 0)
            {
                if (particles1 == null) particles1 = new SmokeParticle[0];
                if (particles2 == null) particles2 = new SmokeParticle[0];
                var dustG = 0.5f;
                var dustB = 0.4f;
                foreach (var particle in particles1.Concat(particles2))
                {
                    var size = particle.Size;
                    var x = particle.X - (size * 0.5f);
                    var y = particle.Y - particle.Z - (size * 0.5f);

                    float width = size;
                    float height = size;

                    var heat = MathHelper.Min(1f, Math.Max(0, (particle.Temperature - 750)) / 1000f);
                    var alpha = particle.Alpha * particle.AlphaScale * 8f;
                    var alphaPoint6 = alpha * 0.6f;
                    var c = particle.ParticleType == SmokeParticleType.Dust
                        ? new Color(alphaPoint6, alpha * dustG, alpha * dustB, alpha)
                        : new Color(alphaPoint6 * (1f + (heat * 0.4f)), alphaPoint6 * (1f + (heat * 0.1f)), alphaPoint6 * (1f - heat), alpha);
                    vertex[i] = new VertexPositionColorTexture { Position = new Vector3(x, y, 0), TextureCoordinate = new Vector2(0, 0), Color = c };
                    vertex[i + 1] = new VertexPositionColorTexture { Position = new Vector3(x + width, y, 0), TextureCoordinate = new Vector2(1, 0), Color = c };
                    vertex[i + 2] = new VertexPositionColorTexture { Position = new Vector3(x, y + height, 0), TextureCoordinate = new Vector2(0, 1), Color = c };
                    vertex[i + 3] = new VertexPositionColorTexture { Position = new Vector3(x + width, y + height, 0), TextureCoordinate = new Vector2(1, 1), Color = c };

                    i += 4;
                }
            }

            this.SetBufferData(rowNum, vertex, bufferSize);
        }
    }
}
