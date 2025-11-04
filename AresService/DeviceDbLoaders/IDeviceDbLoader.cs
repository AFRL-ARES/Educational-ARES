using System.Threading.Tasks;

namespace AresService.DeviceDbLoaders;

public interface IDeviceDbLoader
{
  Task Load();
}
