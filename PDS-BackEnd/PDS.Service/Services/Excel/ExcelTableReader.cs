using ClosedXML.Excel;
using System.Globalization;
using System.Text;

namespace PDS.Service.Services.Excel;

/// <summary>
/// Fila leída de una hoja de Excel: número de fila original (base 1, como la ve el usuario)
/// y celdas como texto crudo, indexadas por el encabezado normalizado.
/// </summary>
public sealed record ExcelRow(int RowNumber, IReadOnlyDictionary<string, string> Cells)
{
    public string Get(string normalizedHeader)
    {
        if (Cells.TryGetValue(normalizedHeader, out var value))
            return value;

        var targetKey = StripOpcional(normalizedHeader);
        foreach (var (key, val) in Cells)
        {
            if (StripOpcional(key) == targetKey)
                return val;
        }

        return string.Empty;
    }

    private static string StripOpcional(string str)
    {
        if (string.IsNullOrEmpty(str)) return string.Empty;
        var idx = str.IndexOf("(opcional)", StringComparison.OrdinalIgnoreCase);
        if (idx >= 0)
            str = str.Remove(idx, 10);
        return str.Trim();
    }
}

/// <summary>Resultado de leer una hoja: encabezados normalizados (en orden) + filas de datos no vacías.</summary>
public sealed record ExcelTable(IReadOnlyList<string> Headers, IReadOnlyList<ExcelRow> Rows);

/// <summary>
/// Infra común de importación Excel (M1): lee una hoja como tabla de strings con encabezados
/// normalizados (sin acentos, minúsculas, espacios colapsados) — el mismo criterio de matching
/// por nombre que usaba el CSV del mock. Los importadores (PEI M3, POA M6, matriz M8) parsean
/// tipos y validan negocio encima de esto.
/// </summary>
public static class ExcelTableReader
{
    /// <summary>
    /// Lee la hoja indicada (o la primera) asumiendo que <paramref name="headerRow"/> contiene los
    /// encabezados y los datos comienzan en la fila siguiente. Filas totalmente vacías se omiten.
    /// </summary>
    public static ExcelTable Read(Stream stream, string? worksheetName = null, int headerRow = 1, int maxRows = 5000, int maxColumns = 100)
    {
        using var workbook = new XLWorkbook(stream);
        return Read(workbook, worksheetName, headerRow, maxRows, maxColumns);
    }

    /// <summary>
    /// Lee una hoja desde un workbook ya abierto.
    /// </summary>
    public static ExcelTable Read(IXLWorkbook workbook, string? worksheetName = null, int headerRow = 1, int maxRows = 5000, int maxColumns = 100)
    {
        var worksheet = worksheetName is null
            ? workbook.Worksheets.First()
            : workbook.Worksheets.FirstOrDefault(ws =>
                  string.Equals(ws.Name, worksheetName, StringComparison.OrdinalIgnoreCase))
              ?? throw new InvalidOperationException($"El archivo no contiene la hoja '{worksheetName}'.");

        return Read(worksheet, headerRow, maxRows, maxColumns);
    }

    /// <summary>
    /// Lee una hoja de cálculo individual.
    /// </summary>
    public static ExcelTable Read(IXLWorksheet worksheet, int headerRow = 1, int maxRows = 5000, int maxColumns = 100)
    {
        var lastColumn = worksheet.LastColumnUsed()?.ColumnNumber() ?? 0;
        var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 0;

        if (maxColumns > 0 && lastColumn > maxColumns)
            throw new InvalidOperationException($"El archivo excede el límite máximo de {maxColumns} columnas permitidas.");

        if (maxRows > 0 && lastRow > maxRows)
            throw new InvalidOperationException($"El archivo excede el límite máximo de {maxRows:N0} filas permitidas.");

        var headers = new List<string>();
        for (var col = 1; col <= lastColumn; col++)
        {
            headers.Add(Normalize(worksheet.Cell(headerRow, col).GetString()));
        }

        var rows = new List<ExcelRow>();
        for (var rowNumber = headerRow + 1; rowNumber <= lastRow; rowNumber++)
        {
            var cells = new Dictionary<string, string>(StringComparer.Ordinal);
            var hasContent = false;

            for (var col = 1; col <= lastColumn; col++)
            {
                var header = headers[col - 1];
                if (header.Length == 0)
                    continue;

                var value = worksheet.Cell(rowNumber, col).GetString().Trim();
                if (value.Length > 0)
                    hasContent = true;

                // Ante encabezados duplicados gana el primero (mismo criterio que el CSV del mock).
                cells.TryAdd(header, value);
            }

            if (hasContent)
                rows.Add(new ExcelRow(rowNumber, cells));
        }

        return new ExcelTable(headers, rows);
    }

    /// <summary>
    /// Normaliza un texto para matching por nombre: minúsculas, sin acentos, espacios colapsados.
    /// Debe mantenerse alineada con la normalización usada por los importadores en catálogos.
    /// </summary>
    public static string Normalize(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        var formD = text.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(formD.Length);
        var previousWasSpace = false;

        foreach (var ch in formD)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark)
                continue;

            if (char.IsWhiteSpace(ch))
            {
                if (!previousWasSpace && builder.Length > 0)
                    builder.Append(' ');
                previousWasSpace = true;
                continue;
            }

            builder.Append(ch);
            previousWasSpace = false;
        }

        return builder.ToString().TrimEnd();
    }
}
