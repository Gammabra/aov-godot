using AshesOfVelsingrad.Utilities;

namespace AshesOfVelsingrad.Systems;

public interface IItemSystem
{
    int Id { get; }
    string? Name { get; }
    bool ConsumesTurn { get; }
    void Use(IUnitSystem user, IUnitSystem? target, IMapSystem? map);
}
