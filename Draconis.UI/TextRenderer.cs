namespace Draconis.UI
{
    using System;
    using System.Collections.Generic;

    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;

    using Shared;

    public class TextRenderer : IDisposable
    {
        protected readonly Dictionary<int, TextRenderString> texts = new Dictionary<int, TextRenderString>();
        protected readonly Dictionary<int, DynamicVertexBuffer> vertexBuffers = new Dictionary<int, DynamicVertexBuffer>();
        protected readonly Dictionary<int, DynamicIndexBuffer> indexBuffers = new Dictionary<int, DynamicIndexBuffer>();
        protected readonly Dictionary<char, Vector2i> charTexCoords = new Dictionary<char, Vector2i>();

        protected readonly int firstAllowedChar = 32;
        protected BasicEffect effect;
        protected int textureWidth;
        protected int textureHeight;
        protected int viewWidth;
        protected int viewHeight;
        protected string texturePath;

        private bool isContentLoaded;
        private int appliedScale;

        public int LetterSpace { get; protected set; }
        public int LetterWidth { get; protected set; }
        public int LetterHeight { get; protected set; }
        public int LineHeight { get; protected set; }

        public TextRenderer(string texturePath)
        {
            this.texturePath = texturePath;
        }

        public void LoadContent()
        {
            var texture = UIStatics.Content.Load<Texture2D>(texturePath);
            this.textureWidth = texture.Width;
            this.textureHeight = texture.Height;

            this.LetterSpace = 7 * UIStatics.Scale / 100;
            this.LetterWidth = 8 * UIStatics.Scale / 100;
            this.LetterHeight = 14 * UIStatics.Scale / 100;
            this.LineHeight = 16 * UIStatics.Scale / 100;

            this.effect = new BasicEffect(UIStatics.Graphics)
            {
                Texture = texture,
                TextureEnabled = true,
                VertexColorEnabled = true,
                World = Matrix.Identity,
                View = Matrix.Identity
            };

            char c = (char)firstAllowedChar;
            for (int y = 0; y < this.textureHeight; y += this.LetterHeight)
            {
                for (int x = 0; x < this.textureWidth; x += this.LetterWidth)
                {
                    this.charTexCoords.Add(c, new Vector2i(x, y));
                    c++;
                }
            }

            this.isContentLoaded = true;
        }

        public void ReloadContent(string texturePath)
        {
            this.texturePath = texturePath;
            if (!this.isContentLoaded) return;

            var texture = UIStatics.Content.Load<Texture2D>(texturePath);
            this.textureWidth = texture.Width;
            this.textureHeight = texture.Height;

            this.LetterSpace = 7 * UIStatics.Scale / 100;
            this.LetterWidth = 8 * UIStatics.Scale / 100;
            this.LetterHeight = 14 * UIStatics.Scale / 100;
            this.LineHeight = 16 * UIStatics.Scale / 100;

            this.effect.Texture = texture;
            this.appliedScale = UIStatics.Scale;

            this.charTexCoords.Clear();
            char c = (char)firstAllowedChar;
            for (int y = 0; y < this.textureHeight; y += this.LetterHeight)
            {
                for (int x = 0; x < this.textureWidth; x += this.LetterWidth)
                {
                    this.charTexCoords.Add(c, new Vector2i(x, y));
                    c++;
                }
            }

            foreach (var kv in this.texts)
            {
                this.vertexBuffers[kv.Key] = this.GenerateVertexBuffer(this.vertexBuffers[kv.Key], kv.Value);
                this.indexBuffers[kv.Key] = this.GenerateIndexBuffer(this.indexBuffers[kv.Key], kv.Value);
            }
        }

        public virtual void SetText(int callerId, string newText, Vector2i position, Color colour, Vector2i scissorRectangeSize = null, bool hasShadow = false, int wordSpacing = 7)
        {
            if (string.IsNullOrEmpty(newText))
            {
                this.RemoveText(callerId);
                return;
            }

            if (this.texts.ContainsKey(callerId))
            {
                var str = this.texts[callerId];
                str.Postion = position;
                str.ScissorRectangeSize = scissorRectangeSize;
                if (str.Text == newText && str.Colour == colour && str.ScissorRectangeSize == scissorRectangeSize && str.HasShadow == hasShadow) return;  // No buffer update needed
                str.Text = newText;
                str.Colour = colour;
                str.HasShadow = hasShadow;
            }
            else
            {
                var str = new TextRenderString(callerId, newText, position, colour, scissorRectangeSize, hasShadow, wordSpacing);
                this.texts.Add(callerId, str);
                this.vertexBuffers.Add(callerId, null);
                this.indexBuffers.Add(callerId, null);
            }

            var text = this.texts[callerId];

            this.vertexBuffers[callerId] = this.GenerateVertexBuffer(this.vertexBuffers[callerId], text);
            this.indexBuffers[callerId] = this.GenerateIndexBuffer(this.indexBuffers[callerId], text);
        }

        public virtual void RemoveText(int callerId)
        {
            if (this.texts.ContainsKey(callerId))
            {
                var vb = this.vertexBuffers[callerId];
                var ib = this.indexBuffers[callerId];
                if (vb?.IsDisposed == false)
                {
                    vb.Dispose();
                }
                if (ib?.IsDisposed == false)
                {
                    ib.Dispose();
                }

                this.texts.Remove(callerId);
                this.vertexBuffers.Remove(callerId);
                this.indexBuffers.Remove(callerId);
            }
        }

        public virtual void Draw(int callerId)
        {
            if (!this.isContentLoaded) this.LoadContent();
            else if (this.appliedScale != UIStatics.Scale) this.ReloadContent(GetFontTexturePath());

            if (!this.texts.ContainsKey(callerId))
            {
                return;
            }

            var text = this.texts[callerId];
            if (text.Text.Length == 0)
            {
                return;
            }

            var vertexBuffer = this.vertexBuffers[callerId];
            if (vertexBuffer == null || vertexBuffer.IsContentLost || vertexBuffer.IsDisposed || vertexBuffer.VertexCount == 0)
            {
                vertexBuffer = this.GenerateVertexBuffer(this.vertexBuffers[callerId], text);
                if (vertexBuffer == null || vertexBuffer.IsContentLost || vertexBuffer.IsDisposed || vertexBuffer.VertexCount == 0)
                {
                    return;
                }

                this.vertexBuffers[callerId] = vertexBuffer;
            }

            var indexBuffer = this.indexBuffers[callerId];
            if (indexBuffer == null || indexBuffer.IsContentLost || indexBuffer.IsDisposed)
            {
                indexBuffer = this.GenerateIndexBuffer(this.indexBuffers[callerId], text);
                if (indexBuffer == null || indexBuffer.IsContentLost || indexBuffer.IsDisposed || indexBuffer.IndexCount == 0)
                {
                    return;
                }

                this.indexBuffers[callerId] = indexBuffer;
            }

            // Projection is used for position offset - this is used to fix a performance problem with panels
            this.viewWidth = UIStatics.Graphics.Viewport.Width;
            this.viewHeight = UIStatics.Graphics.Viewport.Height;
            Matrix projection = Matrix.CreateOrthographicOffCenter(-text.Postion.X, this.viewWidth - text.Postion.X, this.viewHeight - text.Postion.Y, -text.Postion.Y, 0, 1);
            this.effect.Projection = projection;

            var prevScissorRectangle = UIStatics.Graphics.ScissorRectangle;
            if (text.ScissorRectangeSize != null)
            {
                UIStatics.Graphics.ScissorRectangle = new Rectangle(text.Postion.X, text.Postion.Y, text.ScissorRectangeSize.X, text.ScissorRectangeSize.Y);
            }

            var prevSamplerState = UIStatics.Graphics.SamplerStates[0];
            UIStatics.Graphics.SamplerStates[0] = SamplerState.PointClamp;

            UIStatics.Graphics.SetVertexBuffer(vertexBuffer);
            UIStatics.Graphics.Indices = indexBuffer;
                
            this.effect.CurrentTechnique.Passes[0].Apply();
            UIStatics.Graphics.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, text.Text.Length * (text.HasShadow ? 4 : 2));

            UIStatics.Graphics.SamplerStates[0] = prevSamplerState;
            //  this.device.BlendState = prevBlendState;
            UIStatics.Graphics.ScissorRectangle = prevScissorRectangle;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual DynamicVertexBuffer GenerateVertexBuffer(DynamicVertexBuffer originalBuffer, TextRenderString str)
        {
            var size = str.Text.Length * (str.HasShadow ? 8 : 4);
            var vertexArray = new VertexPositionColorTexture[size];
            var wordSpace = str.WordSpacing * UIStatics.Scale / 100;

            var y = 1;
            int j = 0;
            if (str.HasShadow)
            {
                for (int i = 0, x = 1; i < str.Text.Length; ++i)
                {
                    var c = str.Text[i];
                    if (c == '|')
                    {
                        x = 1;
                        y += this.LineHeight;
                        continue;
                    }

                    if (!this.charTexCoords.ContainsKey(c))
                    {
                        continue;
                    }

                    var cc = this.charTexCoords[c];
                    var tx1 = cc.X / (float)this.textureWidth;
                    var tx2 = (cc.X + this.LetterWidth) / (float)this.textureWidth;
                    var ty1 = cc.Y / (float)this.textureHeight;
                    var ty2 = (cc.Y + this.LetterHeight) / (float)this.textureHeight;

                    vertexArray[j] = new VertexPositionColorTexture { Position = new Vector3(x, y, 0), TextureCoordinate = new Vector2(tx1, ty1), Color = Color.Black };
                    vertexArray[j + 1] = new VertexPositionColorTexture { Position = new Vector3(x + this.LetterWidth, y, 0), TextureCoordinate = new Vector2(tx2, ty1), Color = Color.Black };
                    vertexArray[j + 2] = new VertexPositionColorTexture { Position = new Vector3(x, y + this.LetterHeight, 0), TextureCoordinate = new Vector2(tx1, ty2), Color = Color.Black };
                    vertexArray[j + 3] = new VertexPositionColorTexture { Position = new Vector3(x + this.LetterWidth, y + this.LetterHeight, 0), TextureCoordinate = new Vector2(tx2, ty2), Color = Color.Black };

                    x += c == ' ' ? wordSpace : this.LetterSpace;
                    j += 4;
                }
            }

            y = 0;
            for (int i = 0, x = 0; i < str.Text.Length; ++i)
            {
                var c = str.Text[i];
                if (c == '|')
                {
                    x = 0;
                    y += this.LineHeight;
                    continue;
                }

                if (!this.charTexCoords.ContainsKey(c))
                {
                    continue;
                }

                var cc = this.charTexCoords[c];
                var tx1 = cc.X / (float)this.textureWidth;
                var tx2 = (cc.X + this.LetterWidth) / (float)this.textureWidth;
                var ty1 = cc.Y / (float)this.textureHeight;
                var ty2 = (cc.Y + this.LetterHeight) / (float)this.textureHeight;

                vertexArray[j] = new VertexPositionColorTexture { Position = new Vector3(x, y, 0), TextureCoordinate = new Vector2(tx1, ty1), Color = str.Colour };
                vertexArray[j + 1] = new VertexPositionColorTexture { Position = new Vector3(x + this.LetterWidth, y, 0), TextureCoordinate = new Vector2(tx2, ty1), Color = str.Colour };
                vertexArray[j + 2] = new VertexPositionColorTexture { Position = new Vector3(x, y + this.LetterHeight, 0), TextureCoordinate = new Vector2(tx1, ty2), Color = str.Colour };
                vertexArray[j + 3] = new VertexPositionColorTexture { Position = new Vector3(x + this.LetterWidth, y + this.LetterHeight, 0), TextureCoordinate = new Vector2(tx2, ty2), Color = str.Colour };

                x += c == ' ' ? wordSpace : this.LetterSpace;
                j += 4;
            }

            if (originalBuffer != null)
            {
                if (originalBuffer.IsContentLost || originalBuffer.IsDisposed || originalBuffer.VertexCount < size)
                {
                    // Original buffer is invalid or too small
                    originalBuffer.Dispose();
                    originalBuffer = null;
                }
            }

            var newBuffer = originalBuffer ?? new DynamicVertexBuffer(UIStatics.Graphics, VertexPositionColorTexture.VertexDeclaration, size, BufferUsage.WriteOnly);
            newBuffer.SetData(vertexArray);

            return newBuffer;
        }

        protected virtual DynamicIndexBuffer GenerateIndexBuffer(DynamicIndexBuffer originalBuffer, TextRenderString str)
        {
            var size = str.Text.Length * (str.HasShadow ? 12 : 6);
            var values = new short[size];

            for (short i = 0, j = 0; i < size; i += 6, j += 4)
            {
                values[i] = j;
                values[i + 1] = (short)(j + 1);
                values[i + 2] = (short)(j + 2);
                values[i + 3] = (short)(j + 2);
                values[i + 4] = (short)(j + 1);
                values[i + 5] = (short)(j + 3);
            }

            if (originalBuffer != null && (originalBuffer.IsContentLost || originalBuffer.IsDisposed || originalBuffer.IndexCount < size))
            {
                // Original buffer is invalid or too small
                originalBuffer.Dispose();
                originalBuffer = null;
            }

            var newBuffer = originalBuffer ?? new DynamicIndexBuffer(UIStatics.Graphics, IndexElementSize.SixteenBits, size, BufferUsage.WriteOnly);
            newBuffer.SetData(values);
            return newBuffer;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                foreach (var id in this.texts.Keys)
                {
                    this.RemoveText(id);
                }
            }
        }

        private static string GetFontTexturePath()
        {
            if (UIStatics.Scale == 200) return "Textures\\Fonts\\SpaceMono17";
            if (UIStatics.Scale == 150) return "Textures\\Fonts\\SpaceMono13";
            return "Textures\\Fonts\\SpaceMono8";
        }
    }
}
