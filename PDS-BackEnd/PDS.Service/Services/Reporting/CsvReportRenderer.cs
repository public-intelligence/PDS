using PDS.Domain.Enums;
using System.Text;

namespace PDS.Service.Services.Reporting;

/// <summary>
/// RF-RE-06 CSV: escritor mínimo, UTF-8 con BOM, todos los campos entrecomillados (estable y seguro
/// frente a separadores). Incluye líneas de metadatos del contexto antes de los datos.
/// </summary>
public sealed class CsvReportRenderer : IReportRenderer
{
    public ReportFormat Format => ReportFormat.Csv;

    public ReportRenderResult Render(ReportTable table)
    {
        var sb = new StringBuilder();
        AppendLine(sb, table.Title);
        AppendLine(sb, ReportLabelFormatter.ContextLine(table.Context));
        AppendLine(sb, ReportLabelFormatter.GeneratedLine(table.Context));
        if (!string.IsNullOrWhiteSpace(table.Note))
            AppendLine(sb, table.Note);
        sb.Append("\r\n");

        AppendRow(sb, table.Columns.Select(c => c.Header));
        foreach (var row in table.Rows)
            AppendRow(sb, Enumerable.Range(0, table.Columns.Count).Select(i => row.Cells.ElementAtOrDefault(i) ?? string.Empty));

        var bytes = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true).GetBytes(sb.ToString());
        return new ReportRenderResult { Content = bytes, ContentType = "text/csv", FileExtension = "csv" };
    }

    private static void AppendLine(StringBuilder sb, string value)
        => sb.Append(Escape(value)).Append("\r\n");

    private static void AppendRow(StringBuilder sb, IEnumerable<string> cells)
        => sb.Append(string.Join(",", cells.Select(Escape))).Append("\r\n");

    private static string Escape(string? value)
        => "\"" + (value ?? string.Empty).Replace("\"", "\"\"") + "\"";
}
