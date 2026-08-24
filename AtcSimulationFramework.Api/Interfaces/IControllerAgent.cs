using AtcSimulationFramework.Api.Entities;

namespace AtcSimulationFramework.Api.Interfaces;

public interface IControllerAgent
{
    VectorCommand ResolveConflict(ConflictEvent conflict);
}
