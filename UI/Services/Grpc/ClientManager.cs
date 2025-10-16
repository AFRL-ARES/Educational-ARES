using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Extensions.Options;
using System.Security.Cryptography.X509Certificates;
using UI.Settings;

namespace UI.Services.Grpc;

internal class ClientManager : IClientManager
{
  private readonly CertificateSettings _certificateOptions;
  private readonly ILogger<ClientManager> _logger;
  private readonly RemoteServiceSettings _remoteServiceSettings;
  private GrpcChannel? _channel;

  public ClientManager(IOptions<RemoteServiceSettings> remoteOptions,
    IOptions<CertificateSettings> certificateOptions,
    ILogger<ClientManager> logger)
  {
    _certificateOptions = certificateOptions.Value;
    _logger = logger;
    _remoteServiceSettings = remoteOptions.Value;
    CreateChannel().GetAwaiter().GetResult();
  }

  private async Task CreateChannel()
  {
    if (_channel is not null)
    {
      await _channel.ShutdownAsync();
      _channel.Dispose();
    }

    if (_remoteServiceSettings.ServerPort == 0 || string.IsNullOrEmpty(_remoteServiceSettings.ServerHost))
      return;

    var handler = new HttpClientHandler();
    try
    {
      var cert = new X509Certificate2(_certificateOptions.Path, _certificateOptions.Password);
      handler.ClientCertificates.Add(cert);
      handler.CheckCertificateRevocationList = false;
    }
    catch (Exception)
    {
      _logger.LogWarning("Unable to create a secure gRPC channel as the specified certificate was not found. Ensure that appsettings.json has the correct certificate path.");
      // throw new InvalidOperationException("Unable to create a secure gRPC channel as the specified certificate was not found. Ensure that appsettings.json has the correct certificate path.");
    }

    const int MaxCapacityBytes = 50 * 1024 * 1024;
    var serverUri = new UriBuilder("https", _remoteServiceSettings.ServerHost, _remoteServiceSettings.ServerPort ?? 443).Uri;
    var opts = new GrpcChannelOptions
    {
      HttpHandler = handler,
      //Quick fix, but should handle this better in the future
      MaxSendMessageSize = MaxCapacityBytes,
      MaxReceiveMessageSize = MaxCapacityBytes
    };

    _channel = GrpcChannel.ForAddress(serverUri, opts);
  }

  public T GetClient<T>() where T : ClientBase<T>
  {
    if (_channel is null)
      throw new NullReferenceException($"Couldn't create a client as {nameof(GrpcChannel)} was not created");

    var client = (T?)Activator.CreateInstance(typeof(T), _channel);
    if (client is null)
      throw new InvalidOperationException($"Unable to create client of type {nameof(T)}");

    return client;
  }
}
