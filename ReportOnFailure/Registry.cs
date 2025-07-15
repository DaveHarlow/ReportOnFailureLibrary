using ReportOnFailure.Interfaces;

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
            // Assuming each reporter has a method to generate a report
            // This is a placeholder, actual implementation may vary
            var report = reporter.OutPutData();
            reports.Add(report);
        }
        return reports;
    }
}
