namespace SigmaDraconis.Commentary.Operators
{
    internal class MoreThanOperator : Operator
    {
        internal override bool Test(bool lhs, bool rhs)
        {
            return lhs == true && rhs == false;
        }

        internal override bool Test(int lhs, int rhs)
        {
            return lhs > rhs;
        }

        internal override bool Test(float lhs, float rhs)
        {
            return lhs > rhs;
        }

        internal override bool Test(double lhs, double rhs)
        {
            return lhs > rhs;
        }

        internal override bool Test(string lhs, string rhs)
        {
            return lhs.CompareTo(rhs) > 0;
        }
    }
}
