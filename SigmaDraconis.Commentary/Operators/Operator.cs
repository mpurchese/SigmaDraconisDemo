namespace SigmaDraconis.Commentary.Operators
{
    using System;

    internal abstract class Operator
    {
        internal abstract bool Test(bool lhs, bool rhs);
        internal abstract bool Test(int lhs, int rhs);
        internal abstract bool Test(float lhs, float rhs);
        internal abstract bool Test(double lhs, double rhs);
        internal abstract bool Test(string lhs, string rhs);

        internal static Operator FromString(string str)
        {
            switch (str)
            {
                case "<": return new LessThanOperator();
                case ">": return new MoreThanOperator();
                case "=": return new EqualsOperator();
            }

            return null;
        }
    }
}
