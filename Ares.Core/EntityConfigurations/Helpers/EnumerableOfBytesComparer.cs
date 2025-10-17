using Google.Protobuf;
using Microsoft.EntityFrameworkCore.ChangeTracking;

public class EnumerableOfByteComparer : ValueComparer<ByteString>
{
  public EnumerableOfByteComparer()
      : base(
          (c1, c2) =>
              (c1 == null && c2 == null) ||
              (c1 != null && c2 != null && c1.SequenceEqual(c2)),
          c => c == null
              ? 0
              : c.Aggregate(0, (a, v) => HashCode.Combine(a, v)),
          c => c
      )
  { }
}
