namespace SigmaDraconis.Shared
{
    using System;
    using ProtoBuf;

    [ProtoContract]
    public struct Energy : IComparable
    {
        public double KWh
        {
            get
            {
                return this.Joules / 3600000.0;
            }
            set
            {
                this.Joules = (long)(value * 3600000.0);
            }
        }

        [ProtoMember(1)]
        public long Joules { get; set; }

        public Energy(long joules)
        {
            this.Joules = joules;
        }

        public static Energy FromKwH(double kWh)
        {
            return new Energy((long)(kWh * 3600000.0));
        }

        public static implicit operator long(Energy rhs)
        {
            return rhs.Joules;
        }

        public static implicit operator Energy(long rhs)
        {
            return new Energy(rhs);
        }

        public int CompareTo(object obj)
        {
            if (obj is Energy e) return this.Joules.CompareTo(e.Joules);
            return this.Joules.CompareTo(obj);
        }

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return this.Joules.GetHashCode();
        }

        public override string ToString()
        {
            if (this.KWh >= 1.0)
            {
                return $"{this.KWh:F1} kW";
            }
            else
            {
                return $"{this.KWh * 1000:F0} W";
            }
        }

        public static bool operator ==(Energy left, Energy right)
        {
            return left.Joules == right.Joules;
        }

        public static bool operator !=(Energy left, Energy right)
        {
            return !(left == right);
        }

        public static bool operator <(Energy left, Energy right)
        {
            return left.Joules < right.Joules;
        }

        public static bool operator >(Energy left, Energy right)
        {
            return left.Joules > right.Joules;
        }

        public static Energy operator +(Energy left, int right)
        {
            return new Energy(left.Joules + right);
        }

        public static Energy operator -(Energy left, int right)
        {
            return new Energy(left.Joules - right);
        }

        public static Energy operator *(Energy left, int right)
        {
            return new Energy(left.Joules * right);
        }

        public static Energy operator /(Energy left, int right)
        {
            return new Energy(left.Joules / right);
        }

        public static Energy operator +(Energy left, double right)
        {
            return new Energy(left.Joules + (long)right);
        }

        public static Energy operator -(Energy left, double right)
        {
            return new Energy(left.Joules - (long)right);
        }

        public static Energy operator *(Energy left, double right)
        {
            return new Energy((long)(left.Joules * right));
        }

        public static Energy operator /(Energy left, double right)
        {
            return new Energy((long)(left.Joules / right));
        }

        public static double operator /(Energy left, Energy right)
        {
            return left.KWh / right.KWh;
        }

        public static Energy Clamp(Energy val, Energy min, Energy max)
        {
            if (val.CompareTo(min) < 0) return min;
            else if (val.CompareTo(max) > 0) return max;
            else return val;
        }
    }
}
