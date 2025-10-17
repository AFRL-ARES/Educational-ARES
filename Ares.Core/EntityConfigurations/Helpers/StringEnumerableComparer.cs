using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Ares.Core.EntityConfigurations.Helpers;
public class StringEnumerableComparer : ValueComparer<IEnumerable<string>>
{
  public StringEnumerableComparer() : base(
            (c1, c2) =>
                c1 == null && c2 == null ||
                c1 != null && c2 != null && c1.SequenceEqual(c2),
            c => c == null
                ? 0
                : c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
            c => c.ToList()
        )
  {
  }
}
