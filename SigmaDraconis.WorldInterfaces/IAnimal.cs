namespace SigmaDraconis.WorldInterfaces
{
    using Draconis.Shared;
    using Shared;

    public interface IAnimal : IMoveableThing, IAnimatedThing
    {
        float Rotation { get; set; }
        float? MovingAngle { get; set; }
        Vector2f Position { get; set; }
        Vector2f RenderPos { get; set; }

        int? RenderRow { get; }
        int? PrevRenderRow { get; set; }

        float CurrentSpeed { get; set; }
        Direction FacingDirection { get; set; }
        bool IsDead { get; }
        bool IsYoung { get; set; }
        int PrevTileIndex { get; }
        bool IsEating { get; }
        bool IsResting { get; }
        bool IsWaiting { get; set; }

        bool IsHungry { get; }
        bool IsThirsty { get; }
        bool IsTired { get; }

        float Acceleration { get; }

        void BeginEating();
        void FinishEating();
        void BeginResting();
        void FinishResting();

        void UpdateRenderRow();
        void UpdateRenderPos();
    }
}
