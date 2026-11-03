using ClosedXML.Excel;
using PDS.Domain.Enums;

namespace PDS.Service.Services.Reporting;

/// <summary>RF-RE-06 Excel (.xlsx) con ClosedXML: encabezado fijo, subtotales en negrita y banner de contexto.</summary>
public sealed class XlsxReportRenderer : IReportRenderer
{
    public ReportFormat Format => ReportFormat.Xlsx;

    public ReportRenderResult Render(ReportTable table)
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.AddWorksheet("Reporte");
        var lastCol = Math.Max(1, table.Columns.Count);

        var r = 1;
        SetBanner(ws, r++, lastCol, table.Title, bold: true, size: 13);
        SetBanner(ws, r++, lastCol, ReportLabelFormatter.ContextLine(table.Context));
        SetBanner(ws, r++, lastCol, ReportLabelFormatter.GeneratedLine(table.Context));
        if (!string.IsNullOrWhiteSpace(table.Note))
            SetBanner(ws, r++, lastCol, table.Note!);
        r++; // fila en blanco

        var headerRow = r;
        for (var c = 0; c < table.Columns.Count; c++)
        {
            var cell = ws.Cell(headerRow, c + 1);
            cell.Value = table.Columns[c].Header;
            cell.Style.Font.Bold = true;
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#003876");
        }
        r++;

        foreach (var row in table.Rows)
        {
            for (var c = 0; c < table.Columns.Count; c++)
            {
                var cell = ws.Cell(r, c + 1);
                cell.Value = row.Cells.ElementAtOrDefault(c) ?? string.Empty;
                if (table.Columns[c].IsNumeric)
                    cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                if (row.IsSubtotal || row.IsGrandTotal)
                    cell.Style.Font.Bold = true;
            }
            r++;
        }

        ws.SheetView.FreezeRows(headerRow);
        ws.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return new ReportRenderResult
        {
            Content = stream.ToArray(),
            ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            FileExtension = "xlsx"
        };
    }

    private static void SetBanner(IXLWorksheet ws, int row, int lastCol, string text, bool bold = false, int size = 10)
    {
        var cell = ws.Cell(row, 1);
        cell.Value = text;
        cell.Style.Font.Bold = bold;
        cell.Style.Font.FontSize = size;
        ws.Range(row, 1, row, lastCol).Merge();
    }
}
