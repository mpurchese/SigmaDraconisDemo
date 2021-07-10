namespace Draconis.Shared
{
    using System;
    using System.Globalization;
    using ProtoBuf;

    [Serializable]
    [ProtoContract]
    public class Vector2f
    {
        [ProtoMember(1)]
        public float X { get; set; } = 0;

        [ProtoMember(2)]
        public float Y { get; set; } = 0;

        public Vector2f() { }

        public Vector2f(float x, float y)
        {
            this.X = x;
            this.Y = y;
        }

        public static Vector2f Zero => new Vector2f(0, 0);

        public Vector2f Clone()
        {
            return new Vector2f(this.X, this.Y);
        }

        public override bool Equals(object obj)
        {
            if (obj is Vector2f)
            {
                return this == (obj as Vector2f);
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

        public static bool operator ==(Vector2f left, Vector2f right)
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

        public static bool operator !=(Vector2f left, Vector2f right)
        {
            return !(left == right);
        }

        public static Vector2f operator +(Vector2f left, Vector2f right)
        {
            return new Vector2f(right.X + left.X, right.Y + left.Y);
        }

        public static Vector2f operator -(Vector2f left, Vector2f right)
        {
            return new Vector2f(left.X - right.X, left.Y - right.Y);
        }

        public static Vector2f operator /(Vector2f left, double right)
        {
            return new Vector2f((int)(left.X / right), (int)(left.Y / right));
        }

        public static Vector2f operator *(Vector2f left, int right)
        {
            return new Vector2f(left.X * right, left.Y * right);
        }

        public static Vector2f operator *(Vector2f left, Vector2f right)
        {
            return new Vector2f(left.X * right.X, left.Y * right.X);
        }

        public static Vector2f Parse(string str)
        {
            var split = str.Split(',');
            if (split.Length != 2)
            {
                throw new Exception("Failed to parse Vector2f from string");
            }

            try
            {
                var x = float.Parse(split[0], NumberStyles.Any, CultureInfo.InvariantCulture);
                var y = float.Parse(split[1], NumberStyles.Any, CultureInfo.InvariantCulture);
                return new Vector2f(x, y);
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to parse Vector2f from string", ex);
            }
        }

        public static Vector2f SafeParse(string str)
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

        public Vector2f Rotate(float angle)
        {
            float cosTheta = (float)Math.Cos(angle);
            float sinTheta = (float)Math.Sin(angle);
            return new Vector2f(cosTheta * this.X - sinTheta * this.Y, sinTheta * this.X + cosTheta * this.Y);
        }

        /// <summary>
        /// Returns the angle in radians clockwise from (0, -1)
        /// </summary>
        /// <returns></returns>
        public float Angle()
        {
            if (this.X < 0) return (Mathf.PI * 2f) + (float)Math.Atan2(this.X, -this.Y);
            return (float)Math.Atan2(this.X, -this.Y);
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
