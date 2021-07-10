namespace SigmaDraconis.Renderers
{
    using System.Linq;

    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;

    using World.Particles;

    public class MicobotParticleRenderer : RendererBase, IRenderer
    {
        public int Layer { get; set; }

        public override void LoadContent()
        {
            base.LoadContent();

            var texture = Content.Load<Texture2D>("Textures\\Particles\\MicrobotParticle");
            this.effect = Content.Load<Effect>("Effects\\SimpleEffect").Clone();
            this.effect.Parameters["xTexture"].SetValue(texture);
            this.effect.CurrentTechnique = this.effect.Techniques["SimpleTechnique"];

            this.textureSize = new Vector2(texture.Width, texture.Height);
        }

        protected override void GenerateBuffersForInvalidatedRows()
        {
            foreach (var r in MicrobotParticleController.RowsForRendererUpdate)
            {
                this.InvalidateRow(r);
            }

            base.GenerateBuffersForInvalidatedRows();
            MicrobotParticleController.RowsForRendererUpdate.Clear();
        }

        protected override void GenerateBuffersForRow(int rowNum)
        {
            var particles = MicrobotParticleController.GetParticles(rowNum);
            var size = particles.Count() * 4;
            VertexPositionTexture[] vertex = new VertexPositionTexture[size];
            int i = 0;
            var colourCount = this.textureSize.X / this.textureSize.Y;

            // Particles
            foreach (var particle in particles)
            {
                var x = particle.X - (particle.Size * 0.5f);
                var y = particle.Y - particle.Z - (particle.Size * 0.5f);

                var tx1 = (particle.ColourIndex) / colourCount;
                var tx2 = (particle.ColourIndex + 1) / colourCount;

                vertex[i] = new VertexPositionTexture { Position = new Vector3(x, y, 0), TextureCoordinate = new Vector2(tx1, 0) };
                vertex[i + 1] = new VertexPositionTexture { Position = new Vector3(x + particle.Size, y, 0), TextureCoordinate = new Vector2(tx2, 0) };
                vertex[i + 2] = new VertexPositionTexture { Position = new Vector3(x, y + particle.Size, 0), TextureCoordinate = new Vector2(tx1, 1) };
                vertex[i + 3] = new VertexPositionTexture { Position = new Vector3(x + particle.Size, y + particle.Size, 0), TextureCoordinate = new Vector2(tx2, 1) };

                i += 4;
            }

            this.SetBufferData(rowNum, vertex, size);
        }
    }
}
