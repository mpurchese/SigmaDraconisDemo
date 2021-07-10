namespace SigmaDraconis.Renderers
{
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;

    using World;
    using World.Particles;
    using Draconis.Shared;
    using SigmaDraconis.Shared;

    public class SmokeRenderer : RendererBase, IRenderer
    {
        public override void LoadContent()
        {
            base.LoadContent();
            this.effect = Content.Load<Effect>("Effects\\SmokeEffect").Clone();
            this.effect.Parameters["xTexture"].SetValue(Content.Load<Texture2D>("Textures\\Particles\\Smoke"));
        }

        protected override void GenerateBuffersForInvalidatedRows()
        {
            foreach (var row in SmokeSimulator.RowsForRendererUpdate.Keys)
            {
                this.GenerateBuffersForRow(row);
            }

            SmokeSimulator.RowsForRendererUpdate.Clear();
        }

        public override void Update(WorldTime time, Vector2f scrollPos, float zoom, bool isPaused = false)
        {
            var light = 1f - World.WorldLight.NightLightFactor;
            var r = light + (0.35f * (1f - light));
            var g = light + (0.5f * (1f - light));
            var b = light + (0.7f * (1f - light));
            this.effect.Parameters["xColour"].SetValue(new Vector4(r, g, b, 1));
            this.effect.CurrentTechnique = this.effect.Techniques["LitTechnique"];

            base.Update(time, scrollPos, zoom, isPaused);
        }

        protected override void GenerateBuffersForRow(int rowNum)
        {
            var particles = SmokeSimulator.GetParticles(rowNum).Where(p => p.IsVisible && p.Alpha > 1 / 255f).ToList();
            var bufferSize = particles.Count * 4;
            VertexPositionColorTexture[] vertex = new VertexPositionColorTexture[bufferSize];
            int i = 0;

            Color c;
            var lightByTile = new Dictionary<int, float>();
            foreach (var particle in particles)
            {
                var size = particle.Size;
                var x = particle.X - (size * 0.5f);
                var y = particle.Y - particle.Z - (size * 0.5f);

                float width = size;
                float height = size;

                var alpha = particle.Alpha * 8f;
                var lamplight = 0f;
                if (World.WorldLight.NightLightFactor > 0)
                {
                    if (lightByTile.ContainsKey(particle.TileIndex)) lamplight = lightByTile[particle.TileIndex];
                    else
                    {
                        var tile = World.GetSmallTile(particle.TileIndex);
                        if (tile != null)
                        {
                            lamplight = tile.LightSources.Values.Where(l => l.IsOn).Sum(h => h.Amount);
                            lightByTile.Add(tile.Index, lamplight);
                        }
                    }
                }

                var brightness = alpha * (particle.ParticleType == SmokeParticleType.Dark ? 0.4f : 0.5f);
                c = new Color(brightness, lamplight, 0, alpha);

                vertex[i] = new VertexPositionColorTexture { Position = new Vector3(x, y, 0), TextureCoordinate = new Vector2(0, 0), Color = c };
                vertex[i + 1] = new VertexPositionColorTexture { Position = new Vector3(x + width, y, 0), TextureCoordinate = new Vector2(1, 0), Color = c };
                vertex[i + 2] = new VertexPositionColorTexture { Position = new Vector3(x, y + height, 0), TextureCoordinate = new Vector2(0, 1), Color = c };
                vertex[i + 3] = new VertexPositionColorTexture { Position = new Vector3(x + width, y + height, 0), TextureCoordinate = new Vector2(1, 1), Color = c };
                i += 4;
            }

            this.SetBufferData(rowNum, vertex, bufferSize);
        }
    }
}
