using Ares.Datamodel.Device;

namespace Ares.Core.Device.State.Export.StateGetters;

/// <summary>
/// This interface is responsible for getting the raw device states from the backend service
/// It's usually the first step in the state export
/// </summary>
public interface IDeviceStateGetter
{
  /// <summary>
  /// Based on a state request, returns raw device states per device id
  /// </summary>
  Task<IDictionary<string, IEnumerable<TState>>> GetStates<TState>(DeviceStateRequestFilter request) where TState : class, IDeviceState;
}
