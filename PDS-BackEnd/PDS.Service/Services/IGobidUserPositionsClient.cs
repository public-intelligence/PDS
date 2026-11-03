using System.Threading;
using System.Threading.Tasks;

namespace PDS.Service.Services;

public interface IGobidUserPositionsClient
{
    Task<string> GetUserPositionsJsonAsync(
        int organizationApplicationId,
        string? searchposition,
        int pageNumber,
        int pageSize,
        CancellationToken ct = default);
}
