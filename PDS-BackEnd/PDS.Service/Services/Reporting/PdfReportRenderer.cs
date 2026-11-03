using PDS.Domain.Enums;
using QuestPDF.Fluent;
using QuestPDF.Helpers;

namespace PDS.Service.Services.Reporting;

/// <summary>
/// RF-RE-06 PDF con QuestPDF: encabezado con título + etiqueta de contexto, pie con marca de tiempo
/// y usuario generador, subtotales en negrita. La licencia Community se fija en el arranque (Program).
/// </summary>
public sealed class PdfReportRenderer : IReportRenderer
{
    public ReportFormat Format => ReportFormat.Pdf;

    public ReportRenderResult Render(ReportTable table)
    {
        var bytes = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(24);
                page.DefaultTextStyle(x => x.FontSize(8));

                page.Header().Column(col =>
                {
                    col.Item().Text(table.Title).FontSize(13).Bold();
                    col.Item().Text(ReportLabelFormatter.ContextLine(table.Context)).FontSize(8);
                });

                page.Content().PaddingVertical(6).Table(t =>
                {
                    t.ColumnsDefinition(cd =>
                    {
                        foreach (var _ in table.Columns) cd.RelativeColumn();
                    });

                    t.Header(header =>
                    {
                        foreach (var column in table.Columns)
                            header.Cell().Background("#003876").Padding(3)
                                .Text(column.Header).FontColor("#FFFFFF").Bold();
                    });

                    foreach (var row in table.Rows)
                    {
                        for (var c = 0; c < table.Columns.Count; c++)
                        {
                            var container = t.Cell().Padding(3);
                            if (table.Columns[c].IsNumeric) container = container.AlignRight();
                            if (row.IsGrandTotal) container = container.Background("#EEF2F7");

                            var span = container.Text(row.Cells.ElementAtOrDefault(c) ?? string.Empty);
                            if (row.IsSubtotal || row.IsGrandTotal) span.Bold();
                        }
                    }
                });

                page.Footer().Column(col =>
                {
                    if (!string.IsNullOrWhiteSpace(table.Note))
                        col.Item().Text(table.Note!).FontSize(7).Italic();
                    col.Item().Text(ReportLabelFormatter.GeneratedLine(table.Context)).FontSize(7);
                });
            });
        }).GeneratePdf();

        return new ReportRenderResult { Content = bytes, ContentType = "application/pdf", FileExtension = "pdf" };
    }
}
