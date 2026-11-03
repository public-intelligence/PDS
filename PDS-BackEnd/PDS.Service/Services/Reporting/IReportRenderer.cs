using PDS.Domain.Enums;

namespace PDS.Service.Services.Reporting;

/// <summary>Estrategia de renderizado por formato (RF-RE-06). Una implementación por ReportFormat.</summary>
public interface IReportRenderer
{
    ReportFormat Format { get; }
    ReportRenderResult Render(ReportTable table);
}
