using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReportOnFailure.Interfaces
{
    public interface IApiResolver : IReportResolverAsync<IApiReporter>, IReportResolverSync<IApiReporter>
    {
    }
}
