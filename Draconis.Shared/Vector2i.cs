namespace Draconis.Shared
{
    using System;
    using ProtoBuf;

    [Serializable]
    [ProtoContract]
    public class Vector2i
    {
        [ProtoMember(10)]
        public int X { get; set; } = 0;

        [ProtoMember(11)]
        public int Y { get; set; } = 0;

        public Vector2i() { }

        public Vector2i(int x, int y)
        {
            this.X = x;
            this.Y = y;
        }

        public Vector2i Clone()
        {
            return new Vector2i(this.X, this.Y);
        }

        public override bool Equals(object obj)
        {
            if (obj is Vector2i)
            {
                return this == (obj as Vector2i);
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
                return hash;
            }
        }

        public static bool operator ==(Vector2i left, Vector2i right)
        {
            if (right is null && left is null)
            {
                return true;
            }

            if (right is null || left is null)
            {
                return false;
            }

            return left.X == right.X && left.Y == right.Y;
        }

        public static bool operator !=(Vector2i left, Vector2i right)
        {
            return !(left == right);
        }

        public static Vector2i operator +(Vector2i left, Vector2i right)
        {
            return new Vector2i(right.X + left.X, right.Y + left.Y);
        }

        public static Vector2i operator -(Vector2i left, Vector2i right)
        {
            return new Vector2i(left.X - right.X, left.Y - right.Y);
        }

        public static Vector2i operator /(Vector2i left, double right)
        {
            return new Vector2i((int)(left.X / right), (int)(left.Y / right));
        }

        public static Vector2i operator *(Vector2i left, int right)
        {
            return new Vector2i(left.X * right, left.Y * right);
        }

        public static Vector2i operator *(Vector2i left, Vector2i right)
        {
            return new Vector2i(left.X * right.X, left.Y * right.Y);
        }

        public static implicit operator Vector2f(Vector2i d)
        {
            return new Vector2f(d.X, d.Y);
        }

        public static double DistanceSqr(Vector2i v1, Vector2i v2)
        {
            return (((double)v1.X - (double)v2.X) * ((double)v1.X - (double)v2.X)) + (((double)v1.Y - (double)v2.Y) * ((double)v1.Y - (double)v2.Y));
        }

        public static double Distance(Vector2i v1, Vector2i v2)
        {
            return Math.Sqrt(Vector2i.DistanceSqr(v1, v2));
        }

        public static Vector2i Parse(string str)
        {
            var split = str.Split(',');
            if (split.Length != 2)
            {
                throw new Exception("Failed to parse Vector2i from string");
            }

            try
            {
                var x = int.Parse(split[0]);
                var y = int.Parse(split[1]);
                return new Vector2i(x, y);
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to parse Vector2i from string", ex);
            }
        }

        public static Vector2i SafeParse(string str)
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

        public float Length()
        {
            return (float)Math.Sqrt((this.X * this.X) + (this.Y * this.Y));
        }

        public override string ToString()
        {
            return $"{this.X}, {this.Y}";
        }
    }
}
