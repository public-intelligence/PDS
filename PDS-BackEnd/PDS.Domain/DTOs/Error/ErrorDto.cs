using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PDS.Domain.DTOs.Error;

public record ErrorDto
{
    public required string Title { get; init; }
    public required IDictionary<string, IEnumerable<string>> Detail { get; init; } = new Dictionary<string, IEnumerable<string>>();

    [SetsRequiredMembers]
    public ErrorDto(string title, IDictionary<string, IEnumerable<string>> detail)
    {
        Title = title;
        Detail = detail;
    }
}
