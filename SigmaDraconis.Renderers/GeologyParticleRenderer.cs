namespace SigmaDraconis.Renderers
{
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;

    using Draconis.Shared;
    using Shared;
    using World;
    using WorldInterfaces;

    public class GeologyParticleRenderer : RendererBase, IRenderer
    {
        private readonly HashSet<int> activeRows = new HashSet<int>();

        public int Layer { get; set; }

        public override void LoadContent()
        {
            base.LoadContent();
            this.effect = Content.Load<Effect>("Effects\\SmokeEffect").Clone();
            this.effect.Parameters["xTexture"].SetValue(Content.Load<Texture2D>("Textures\\Particles\\GeologyParticle"));
        }

        protected override void GenerateBuffersForInvalidatedRows()
        {
            foreach (var r in this.activeRows) this.InvalidateRow(r);
            base.GenerateBuffersForInvalidatedRows();
        }

        public override void Update(WorldTime time, Vector2f scrollPos, float zoom, bool isPaused = false)
        {
            this.effect.Parameters["xColour"].SetValue(new Vector4(0.1f, 0.15f, 0.85f, 1));
            this.effect.CurrentTechnique = this.effect.Techniques["SimpleTechnique"];

            base.Update(time, scrollPos, zoom, isPaused);
        }

        protected override void GenerateBuffersForRow(int rowNum)
        {
            var colonists = World.GetThings<IColonist>(ThingType.Colonist)
                .Where(a => a.RenderRow == rowNum && a.ActivityType == ColonistActivityType.Geology && a.RaisedArmsFrame == 18
                    && ((this.Layer == 1 && (a.FacingDirection == Direction.NE || a.FacingDirection == Direction.NW)) || (this.Layer == 2 && (a.FacingDirection == Direction.SE || a.FacingDirection == Direction.SW))))
                .ToList();

            var size = colonists.Count() * 32;
            VertexPositionColorTexture[] vertex = new VertexPositionColorTexture[size];
            int i = 0;

            if (size > 0 && !this.activeRows.Contains(rowNum)) this.activeRows.Add(rowNum);
            else if (size == 0 && this.activeRows.Contains(rowNum)) this.activeRows.Remove(rowNum);

            // Particles
            foreach (var colonist in colonists)
            {
                if (colonist.FacingDirection == Direction.NE || colonist.FacingDirection == Direction.NW)
                {
                    for (int j = 0; j < 8; j++)
                    {
                        var x = colonist.RenderPos.X + (colonist.FacingDirection == Direction.NE ? 3.5f : -3.5f);
                        var y = colonist.RenderPos.Y - 5;
                        var frame = (float)((colonist.MainTile.MineResourceSurveyProgress + (j * 0.00625f)) * 200f) % 10f;
                        y += Mathf.Min(frame * 0.5f, 3f);
                        x += frame * (colonist.FacingDirection == Direction.NE ? 0.1f : -0.1f);
                        var sizey = 0.3f + (0.003f * (frame * frame * frame));
                        var sizex = sizey * 2f;
                        var alpha = 1f - (frame * 0.1f);
                        var c = new Color(alpha, alpha, alpha, alpha);

                        vertex[i] = new VertexPositionColorTexture { Position = new Vector3(x - sizex, y - sizey, 0), TextureCoordinate = new Vector2(1, 0), Color = c };
                        vertex[i + 1] = new VertexPositionColorTexture { Position = new Vector3(x + sizex, y - sizey, 0), TextureCoordinate = new Vector2(0, 0), Color = c };
                        vertex[i + 2] = new VertexPositionColorTexture { Position = new Vector3(x - sizex, y + sizey, 0), TextureCoordinate = new Vector2(1, 1), Color = c };
                        vertex[i + 3] = new VertexPositionColorTexture { Position = new Vector3(x + sizex, y + sizey, 0), TextureCoordinate = new Vector2(0, 1), Color = c };
                        i += 4;
                    }
                }
                else if (colonist.FacingDirection == Direction.SE)
                {
                    for (int j = 0; j < 8; j++)
                    {
                        var x = colonist.RenderPos.X + 3.0f;
                        var y = colonist.RenderPos.Y - 1.2f;
                        var frame = (float)((colonist.MainTile.MineResourceSurveyProgress + (j * 0.00625f)) * 200f) % 10f;
                        y += Mathf.Min(frame * 0.5f, 3f);
                        x += frame * 0.1f;
                        var sizey = 0.3f + (0.003f * (frame * frame * frame));
                        var sizex = sizey * 2f;
                        var alpha = 1f - (frame * 0.1f);
                        var c = new Color(alpha, alpha, alpha, alpha);

                        vertex[i] = new VertexPositionColorTexture { Position = new Vector3(x - sizex, y - sizey, 0), TextureCoordinate = new Vector2(1, 0), Color = c };
                        vertex[i + 1] = new VertexPositionColorTexture { Position = new Vector3(x + sizex, y - sizey, 0), TextureCoordinate = new Vector2(0, 0), Color = c };
                        vertex[i + 2] = new VertexPositionColorTexture { Position = new Vector3(x - sizex, y + sizey, 0), TextureCoordinate = new Vector2(1, 1), Color = c };
                        vertex[i + 3] = new VertexPositionColorTexture { Position = new Vector3(x + sizex, y + sizey, 0), TextureCoordinate = new Vector2(0, 1), Color = c };
                        i += 4;
                    }
                }
                else if (colonist.FacingDirection == Direction.SW)
                {
                    for (int j = 0; j < 8; j++)
                    {
                        var x = colonist.RenderPos.X - 3.0f;
                        var y = colonist.RenderPos.Y - 1.2f;
                        var frame = (float)((colonist.MainTile.MineResourceSurveyProgress + (j * 0.00625f)) * 200f) % 10f;
                        y += Mathf.Min(frame * 0.5f, 3f);
                        x -= frame * 0.1f;
                        var sizey = 0.3f + (0.003f * (frame * frame * frame));
                        var sizex = sizey * 2f;
                        var alpha = 1f - (frame * 0.1f);
                        var c = new Color(alpha, alpha, alpha, alpha);

                        vertex[i] = new VertexPositionColorTexture { Position = new Vector3(x - sizex, y - sizey, 0), TextureCoordinate = new Vector2(1, 0), Color = c };
                        vertex[i + 1] = new VertexPositionColorTexture { Position = new Vector3(x + sizex, y - sizey, 0), TextureCoordinate = new Vector2(0, 0), Color = c };
                        vertex[i + 2] = new VertexPositionColorTexture { Position = new Vector3(x - sizex, y + sizey, 0), TextureCoordinate = new Vector2(1, 1), Color = c };
                        vertex[i + 3] = new VertexPositionColorTexture { Position = new Vector3(x + sizex, y + sizey, 0), TextureCoordinate = new Vector2(0, 1), Color = c };
                        i += 4;
                    }
                }
            }

            this.SetBufferData(rowNum, vertex, size);
        }
    }
}
