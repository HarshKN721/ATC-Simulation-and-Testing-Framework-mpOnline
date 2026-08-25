using AtcSimulationFramework.Api.Models;

namespace AtcSimulationFramework.Api.Services.Kinematics;

public interface IKinematicsEngine
{
    void AdvanceAircraftPhysics(Aircraft aircraft, double dtSeconds);
}
