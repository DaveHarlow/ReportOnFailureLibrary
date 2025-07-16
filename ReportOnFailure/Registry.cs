using ReportOnFailure.Interfaces;
using System.IO.Compression;


namespace ReportOnFailure;

public class Registry : IRegistry
{

  public Registry()
  {
    reporters = new List<IReporter>();
  }

  public List<IReporter> reporters = new();

  public void RegisterReporter(IReporter reporter)
  {
    if (reporter == null) throw new ArgumentNullException(nameof(reporter));
    if (!reporters.Contains(reporter))
    {
      reporters.Add(reporter);
    }
  }

  public void UnRegisterReporter(IReporter reporter)
  {
    if (reporter == null) throw new ArgumentNullException(nameof(reporter));
    reporters.Remove(reporter);
  }

}