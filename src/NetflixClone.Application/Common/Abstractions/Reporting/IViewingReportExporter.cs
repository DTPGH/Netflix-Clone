using NetflixClone.Application.Admin.Reports;
namespace NetflixClone.Application.Common.Abstractions.Reporting;
public interface IViewingReportExporter
{
    byte[] Generate(AdminViewingReport report);
}
