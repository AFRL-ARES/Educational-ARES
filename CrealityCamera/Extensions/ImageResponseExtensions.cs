using AresCamera.Services;
using Google.Protobuf;

namespace AresCamera.Extensions;

public static class ImageResponseExtensions
{
  public static ImageResponse ToProto(this byte[] bytes)
  {
    var data = new ImageResponse();
    data.ImageData = ByteString.CopyFrom(bytes);
    return data;
  }
}
