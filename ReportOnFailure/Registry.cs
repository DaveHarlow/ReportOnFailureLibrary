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

  public List<string> GetAllReports()
  {
    var reports = new List<string>();
    foreach (var reporter in reporters)
    {
      var report = reporter.OutPutData();
      reports.Add(report);
    }

    return reports;
  }

  public void SaveAllReports(string filePath)
  {
    var reports = GetAllReports();
    File.WriteAllLines(filePath, reports);
  }

  public void SaveAllReportsAsZip(string filePath)
  {
    var reports = GetAllReports();
    using (var zip = new ZipArchive(File.Create(filePath), ZipArchiveMode.Create))
    {
      foreach (var report in reports)
      {
        var entry = zip.CreateEntry($"report_{Guid.NewGuid()}.txt");
        using (var writer = new StreamWriter(entry.Open()))
        {
          writer.Write(report);
        }
      }
    }
  }
}
