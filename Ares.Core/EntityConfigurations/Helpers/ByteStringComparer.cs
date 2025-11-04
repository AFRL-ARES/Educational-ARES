using Google.Protobuf;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Ares.Core.EntityConfigurations.Helpers;

public class ByteStringComparer : ValueComparer<ByteString>
{
  public ByteStringComparer()
      : base(
          (first, second) =>
              first == null && second == null ||
              first != null && second != null && first.SequenceEqual(second),
          byteStr => byteStr == null
              ? 0
              : byteStr.Aggregate(0, (me, when) => HashCode.Combine(me, when))
      )
  { }
}