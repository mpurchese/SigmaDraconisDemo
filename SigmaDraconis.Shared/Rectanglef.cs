namespace SigmaDraconis.Shared
{
    using System;
    using System.Globalization;
    using ProtoBuf;

    [Serializable]
    [ProtoContract]
    public class Rectanglef
    {
        [ProtoMember(1)]
        public float X { get; set; } = 0;

        [ProtoMember(2)]
        public float Y { get; set; } = 0;

        [ProtoMember(3)]
        public float Width { get; set; } = 0;

        [ProtoMember(4)]
        public float Height { get; set; } = 0;

        public Rectanglef() { }

        public Rectanglef(float x, float y, float width, float height)
        {
            this.X = x;
            this.Y = y;
            this.Width = width;
            this.Height = height;
        }

        public Rectanglef Clone()
        {
            return new Rectanglef(this.X, this.Y, this.Width, this.Height);
        }

        public override bool Equals(object obj)
        {
            if (obj is Rectanglef)
            {
                return this == (obj as Rectanglef);
            }

            return false;
        }

        public override int GetHashCode()
        {
            unchecked // Overflow is fine, just wrap
            {
                int hash = 17;
                hash = hash * 23 + this.X.GetHashCode();
                hash = hash * 23 + this.Y.GetHashCode();
                hash = hash * 23 + this.Width.GetHashCode();
                hash = hash * 23 + this.Height.GetHashCode();
                return hash;
            }
        }

        public static bool operator ==(Rectanglef left, Rectanglef right)
        {
            if (ReferenceEquals(right, null) && ReferenceEquals(left, null))
            {
                return true;
            }

            if (ReferenceEquals(right, null) || ReferenceEquals(left, null))
            {
                return false;
            }

            return left.X == right.X && left.Y == right.Y && left.Width == right.Width && left.Height == right.Height;
        }

        public static bool operator !=(Rectanglef left, Rectanglef right)
        {
            return !(left == right);
        }

        public static Rectanglef Parse(string str)
        {
            var split = str.Split(',');
            if (split.Length != 4)
            {
                throw new Exception("Failed to parse Rectanglef from string");
            }

            try
            {
                var x = float.Parse(split[0], NumberStyles.Any, CultureInfo.InvariantCulture);
                var y = float.Parse(split[1], NumberStyles.Any, CultureInfo.InvariantCulture);
                var width = float.Parse(split[2], NumberStyles.Any, CultureInfo.InvariantCulture);
                var height = float.Parse(split[3], NumberStyles.Any, CultureInfo.InvariantCulture);
                return new Rectanglef(x, y, width, height);
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to parse Rectanglef from string", ex);
            }
        }

        public static Rectanglef SafeParse(string str)
        {
            try
            {
                return Parse(str);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public override string ToString()
        {
            return $"{this.X}, {this.Y}, {this.Width}, {this.Height}";
        }
    }
}
