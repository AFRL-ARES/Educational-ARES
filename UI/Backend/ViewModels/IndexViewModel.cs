using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace UI.Backend.ViewModels;

public class IndexViewModel : ReactiveObject
{
  public IndexViewModel()
  {
    var currentMonth = DateTime.Parse("2019-12-01");
    Revenue2019 =
    [
      new()
      {
        Date = DateTime.Parse("2019-01-01"),
        Revenue = 234000
      },
      new()
      {
        Date = DateTime.Parse("2019-02-01"),
        Revenue = 269000
      },
      new()
      {
        Date = DateTime.Parse("2019-03-01"),
        Revenue = 233000
      },
      new()
      {
        Date = DateTime.Parse("2019-04-01"),
        Revenue = 244000
      },
      new()
      {
        Date = DateTime.Parse("2019-05-01"),
        Revenue = 214000
      },
      new()
      {
        Date = DateTime.Parse("2019-06-01"),
        Revenue = 253000
      },
      new()
      {
        Date = DateTime.Parse("2019-07-01"),
        Revenue = 274000
      },
      new()
      {
        Date = DateTime.Parse("2019-08-01"),
        Revenue = 284000
      },
      new()
      {
        Date = DateTime.Parse("2019-09-01"),
        Revenue = 273000
      },
      new()
      {
        Date = DateTime.Parse("2019-10-01"),
        Revenue = 282000
      },
      new()
      {
        Date = DateTime.Parse("2019-11-01"),
        Revenue = 289000
      },
      new()
      {
        Date = DateTime.Parse("2019-12-01"),
        Revenue = 294000
      }
    ];

    Revenue2020 =
    [
      new()
      {
        Date = DateTime.Parse("2019-01-01"),
        Revenue = 334000
      },
      new()
      {
        Date = DateTime.Parse("2019-02-01"),
        Revenue = 369000
      },
      new()
      {
        Date = DateTime.Parse("2019-03-01"),
        Revenue = 333000
      },
      new()
      {
        Date = DateTime.Parse("2019-04-01"),
        Revenue = 344000
      },
      new()
      {
        Date = DateTime.Parse("2019-05-01"),
        Revenue = 314000
      },
      new()
      {
        Date = DateTime.Parse("2019-06-01"),
        Revenue = 353000
      },
      new()
      {
        Date = DateTime.Parse("2019-07-01"),
        Revenue = 374000
      },
      new()
      {
        Date = DateTime.Parse("2019-08-01"),
        Revenue = 384000
      },
      new()
      {
        Date = DateTime.Parse("2019-09-01"),
        Revenue = 373000
      },
      new()
      {
        Date = DateTime.Parse("2019-10-01"),
        Revenue = 382000
      },
      new()
      {
        Date = DateTime.Parse("2019-11-01"),
        Revenue = 389000
      },
      new()
      {
        Date = DateTime.Parse("2019-12-01"),
        Revenue = 394000
      }
    ];

    // Observable.Interval(TimeSpan.FromSeconds(1)).Subscribe(_ =>
    // {
    //   var currArray = Revenue2019.ToList();
    //   currentMonth = currentMonth.AddMonths(1);
    //   var di = new DataItem
    //   {
    //     Date = currentMonth,
    //     Revenue = new Random().NextDouble() * 400000
    //   };
    //   currArray.Add(di);
    //   Revenue2019 = currArray;
    // });
  }

  [Reactive] public List<DataItem> Revenue2019 { get; set; }

  [Reactive] public List<DataItem> Revenue2020 { get; set; }
}
public class DataItem
{
  public DateTime Date { get; set; }
  public double Revenue { get; set; }
}
