namespace SigmaDraconis.WorldInterfaces
{
    public interface ICompostFactory : IFactoryBuilding
    {
        bool AllowMush { get; set; }
        bool AllowOrganics { get; set; }
    }
}
