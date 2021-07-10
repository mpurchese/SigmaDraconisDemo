namespace SigmaDraconis.Shared
{
    using System;
    using ProtoBuf;
    using Draconis.Shared;

    [Serializable]
    [ProtoContract]
    public class Circle
    {
        [ProtoMember(1)]
        public Vector2f Centre { get; set; } = Vector2f.Zero;

        [ProtoMember(2)]
        public float Radius { get; set; } = 0;

        public Circle() { }

        public Circle(Vector2f centre, float radius)
        {
            this.Centre = centre.Clone();
            this.Radius = radius;
        }

        public Circle(float x, float y, float radius)
        {
            this.Centre.X = x;
            this.Centre.Y = y;
            this.Radius = radius;
        }

        public Circle Clone()
        {
            return new Circle(this.Centre.X, this.Centre.Y, this.Radius);
        }

        public override bool Equals(object obj)
        {
            if (obj is Circle)
            {
                return this == (obj as Circle);
            }

            return false;
        }

        public override int GetHashCode()
        {
            unchecked // Overflow is fine, just wrap
            {
                int hash = 17;
                hash = hash * 23 + this.Centre.X.GetHashCode();
                hash = hash * 23 + this.Centre.Y.GetHashCode();
                hash = hash * 23 + this.Radius.GetHashCode();
                return hash;
            }
        }

        public static bool operator ==(Circle left, Circle right)
        {
            if (right is null && left is null)
            {
                return true;
            }

            if (right is null || left is null)
            {
                return false;
            }

            return left.Centre.X == right.Centre.X && left.Centre.Y == right.Centre.Y && left.Radius == right.Radius;
        }

        public static bool operator !=(Circle left, Circle right)
        {
            return !(left == right);
        }

        public override string ToString()
        {
            return $"Circle X = {this.Centre.X}, Y = {this.Centre.Y}, R = {this.Radius}";
        }
    }
}
