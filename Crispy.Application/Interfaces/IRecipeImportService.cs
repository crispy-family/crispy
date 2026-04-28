using System.Threading;
using System.Threading.Tasks;
using Crispy.Application.DTOs;

namespace Crispy.Application.Interfaces
{
    public interface IRecipeImportService
    {
        Task<ImportedRecipeDto?> ImportAsync(string url, CancellationToken cancellationToken = default);
    }
}