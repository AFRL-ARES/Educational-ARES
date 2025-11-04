using Google.Protobuf;

namespace AresService.DeviceDbLoaders;

public record LoadableConfig<TDeviceConfig>(string Id, TDeviceConfig DeviceConfig) where TDeviceConfig : IMessage, new();
