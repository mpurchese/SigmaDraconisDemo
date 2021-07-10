namespace SigmaDraconis.Shared
{
    using System;
    using ProtoBuf;

    [Serializable]
    [ProtoContract]
    public class Vector3i
    {
        [ProtoMember(1)]
        public int X { get; set; } = 0;

        [ProtoMember(2)]
        public int Y { get; set; } = 0;

        [ProtoMember(3)]
        public int Z { get; set; } = 0;

        public Vector3i() { }

        public Vector3i(int x, int y, int z)
        {
            this.X = x;
            this.Y = y;
            this.Z = z;
        }

        public Vector3i Clone()
        {
            return new Vector3i(this.X, this.Y, this.Z);
        }

        public override bool Equals(object obj)
        {
            if (obj is Vector3i)
            {
                return this == (obj as Vector3i);
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
                hash = hash * 23 + this.Z.GetHashCode();
                return hash;
            }
        }

        public static bool operator ==(Vector3i left, Vector3i right)
        {
            if (ReferenceEquals(right, null) && ReferenceEquals(left, null))
            {
                return true;
            }

            if (ReferenceEquals(right, null) || ReferenceEquals(left, null))
            {
                return false;
            }

            return left.X == right.X && left.Y == right.Y && left.Z == right.Z;
        }

        public static bool operator !=(Vector3i left, Vector3i right)
        {
            return !(left == right);
        }

        public static Vector3i operator +(Vector3i left, Vector3i right)
        {
            return new Vector3i(right.X + left.X, right.Y + left.Y, right.Z + left.Z);
        }

        public static Vector3i operator -(Vector3i left, Vector3i right)
        {
            return new Vector3i(left.X - right.X, left.Y - right.Y, left.Z - right.Z);
        }

        public static Vector3i operator /(Vector3i left, double right)
        {
            return new Vector3i((int)(left.X / right), (int)(left.Y / right), (int)(left.Z / right));
        }

        public static Vector3i operator *(Vector3i left, int right)
        {
            return new Vector3i(left.X * right, left.Y * right, left.Z * right);
        }

        public static Vector3i operator *(Vector3i left, Vector3i right)
        {
            return new Vector3i(left.X * right.X, left.Y * right.Y, left.Z * right.Z);
        }

        public static double DistanceSqr(Vector3i v1, Vector3i v2)
        {
            return ((v1.X - (double)v2.X) * (v1.X - (double)v2.X)) + (v1.Y - (double)v2.Y) * (v1.Y - (double)v2.Y) + (v1.Z - (double)v2.Z) * (v1.Z - (double)v2.Z);
        }

        public static double Distance(Vector3i v1, Vector3i v2)
        {
            return Math.Sqrt(DistanceSqr(v1, v2));
        }

        public static Vector3i Parse(string str)
        {
            var split = str.Split(',');
            if (split.Length != 3)
            {
                throw new Exception("Failed to parse Vector3i from string");
            }

            try
            {
                var x = int.Parse(split[0]);
                var y = int.Parse(split[1]);
                var z = int.Parse(split[2]);
                return new Vector3i(x, y, z);
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to parse Vector3i from string", ex);
            }
        }

        public static Vector3i SafeParse(string str)
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
            return (float)Math.Sqrt((this.X * this.X) + (this.Y * this.Y) + (this.Z * this.Z));
        }

        public override string ToString()
        {
            return $"{this.X}, {this.Y}, {this.Z}";
        }
    }
}
