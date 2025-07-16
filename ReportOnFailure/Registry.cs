using ReportOnFailure.Interfaces;
using System.IO.Compression;
using ReportOnFailure.Enums;


namespace ReportOnFailure;

public class Registry : IRegistry
{

  public Registry()
  {
    Reporters = new List<IReporter>();
  }

  public List<IReporter> Reporters { get; set; }

  public ExecutionMode ExecutionMode { get; set; } = ExecutionMode.Synchronous;

  public void RegisterReporter(IReporter reporter)
  {
    if (reporter == null) throw new ArgumentNullException(nameof(reporter));
    if (!Reporters.Contains(reporter))
    {
      Reporters.Add(reporter);
    }
  }

  public void UnRegisterReporter(IReporter reporter)
  {
    if (reporter == null) throw new ArgumentNullException(nameof(reporter));
    Reporters.Remove(reporter);
  }

}