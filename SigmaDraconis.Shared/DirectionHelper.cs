namespace SigmaDraconis.Shared
{
    using Draconis.Shared;

    public class DirectionHelper
    {
        public static Direction Reverse(Direction direction)
        {
            switch (direction)
            {
                case Direction.N:
                    return Direction.S;
                case Direction.NE:
                    return Direction.SW;
                case Direction.E:
                    return Direction.W;
                case Direction.SE:
                    return Direction.NW;
                case Direction.S:
                    return Direction.N;
                case Direction.SW:
                    return Direction.NE;
                case Direction.W:
                    return Direction.E;
                case Direction.NW:
                    return Direction.SE;
            }

            return direction;
        }

        public static Direction AntiClockwise45(Direction direction)
        {
            switch (direction)
            {
                case Direction.N:
                    return Direction.NW;
                case Direction.NE:
                    return Direction.N;
                case Direction.E:
                    return Direction.NE;
                case Direction.SE:
                    return Direction.E;
                case Direction.S:
                    return Direction.SE;
                case Direction.SW:
                    return Direction.S;
                case Direction.W:
                    return Direction.SW;
                case Direction.NW:
                    return Direction.W;
            }

            return direction;
        }

        public static Direction AntiClockwise90(Direction direction)
        {
            switch (direction)
            {
                case Direction.N:
                    return Direction.W;
                case Direction.NE:
                    return Direction.NW;
                case Direction.E:
                    return Direction.N;
                case Direction.SE:
                    return Direction.NE;
                case Direction.S:
                    return Direction.E;
                case Direction.SW:
                    return Direction.SE;
                case Direction.W:
                    return Direction.S;
                case Direction.NW:
                    return Direction.SW;
            }

            return direction;
        }

        public static Direction Clockwise90(Direction direction)
        {
            switch (direction)
            {
                case Direction.N:
                    return Direction.E;
                case Direction.NE:
                    return Direction.SE;
                case Direction.E:
                    return Direction.S;
                case Direction.SE:
                    return Direction.SW;
                case Direction.S:
                    return Direction.W;
                case Direction.SW:
                    return Direction.NW;
                case Direction.W:
                    return Direction.N;
                case Direction.NW:
                    return Direction.NE;
            }

            return direction;
        }

        public static Direction Clockwise45(Direction direction)
        {
            switch (direction)
            {
                case Direction.N:
                    return Direction.NE;
                case Direction.NE:
                    return Direction.E;
                case Direction.E:
                    return Direction.SE;
                case Direction.SE:
                    return Direction.S;
                case Direction.S:
                    return Direction.SW;
                case Direction.SW:
                    return Direction.W;
                case Direction.W:
                    return Direction.NW;
                case Direction.NW:
                    return Direction.N;
            }

            return direction;
        }

        public static Direction Clockwise135(Direction direction)
        {
            switch (direction)
            {
                case Direction.N:
                    return Direction.SE;
                case Direction.NE:
                    return Direction.S;
                case Direction.E:
                    return Direction.SW;
                case Direction.SE:
                    return Direction.W;
                case Direction.S:
                    return Direction.NW;
                case Direction.SW:
                    return Direction.N;
                case Direction.W:
                    return Direction.NE;
                case Direction.NW:
                    return Direction.E;
            }

            return direction;
        }

        public static Direction AntiClockwise135(Direction direction)
        {
            switch (direction)
            {
                case Direction.N:
                    return Direction.SW;
                case Direction.NE:
                    return Direction.W;
                case Direction.E:
                    return Direction.NW;
                case Direction.SE:
                    return Direction.N;
                case Direction.S:
                    return Direction.NE;
                case Direction.SW:
                    return Direction.E;
                case Direction.W:
                    return Direction.SE;
                case Direction.NW:
                    return Direction.S;
            }

            return direction;
        }

        public static float GetAngleFromDirection(Direction direction)
        {
            switch (direction)
            {
                case Direction.N:
                    return 0;
                case Direction.NE:
                    return Mathf.PI * 0.25f;
                case Direction.E:
                    return Mathf.PI * 0.5f;
                case Direction.SE:
                    return Mathf.PI * 0.75f;
                case Direction.S:
                    return Mathf.PI;
                case Direction.SW:
                    return Mathf.PI * 1.25f;
                case Direction.W:
                    return Mathf.PI * 1.5f;
                case Direction.NW:
                    return Mathf.PI * 1.75f;
            }

            return 0;
        }

        public static Direction GetDirectionFromAngle(float angle)
        {
            while (angle > Mathf.PI * 2f) angle -= Mathf.PI * 2f;
            while (angle < 0) angle += Mathf.PI * 2f;

            if (angle < Mathf.PI * 0.125f || angle >= Mathf.PI * 1.875f) return Direction.N;
            if (angle < Mathf.PI * 0.375f) return Direction.NE;
            if (angle < Mathf.PI * 0.625f) return Direction.E;
            if (angle < Mathf.PI * 0.875f) return Direction.SE;
            if (angle < Mathf.PI * 1.125f) return Direction.S;
            if (angle < Mathf.PI * 1.375f) return Direction.SW;
            if (angle < Mathf.PI * 1.625f) return Direction.W;

            return Direction.NW;
        }

        public static Vector2i GetRenderOffsetFromTilePosition(Direction direction)
        {
            var result = new Vector2i();

            if (direction == Direction.N)
            {
                result.Y = -20;
            }
            else if (direction == Direction.NE)
            {
                result.X = 20;
                result.Y = -10;
            }
            else if (direction == Direction.E)
            {
                result.X = 40;
            }
            else if (direction == Direction.SE)
            {
                result.X = 20;
                result.Y = 10;
            }
            if (direction == Direction.S)
            {
                result.Y = 20;
            }
            else if (direction == Direction.SW)
            {
                result.X = -20;
                result.Y = 10;
            }
            else if (direction == Direction.W)
            {
                result.X = -40;
            }
            else if (direction == Direction.NW)
            {
                result.X = -20;
                result.Y = -10;
            }

            return result;
        }

        public static Direction GetDirectionFromAdjacentPositions(int x1, int y1, int x2, int y2)
        {
            Direction result = Direction.None;
            if (x2 > x1 && y2 < y1)
            {
                result = Direction.N;
            }
            else if (x2 > x1 && y2 == y1)
            {
                result = Direction.NE;
            }
            else if (x2 > x1 && y2 > y1)
            {
                result = Direction.E;
            }
            else if (x2 == x1 && y2 > y1)
            {
                result = Direction.SE;
            }
            else if (x2 < x1 && y2 > y1)
            {
                result = Direction.S;
            }
            else if (x2 < x1 && y2 == y1)
            {
                result = Direction.SW;
            }
            else if (x2 < x1 && y2 < y1)
            {
                result = Direction.W;
            }
            else if (x2 == x1 && y2 < y1)
            {
                result = Direction.NW;
            }

            return result;
        }
    }
}
