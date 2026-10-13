using EntityLayer.Concrete;
using EntityLayer.Enum;

namespace BusinessLayer.Abstract;

public interface IBlogWorkflowService
{
    Task<int> CreateAsync(Blog blog, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(int id, string? title, string? subtitle, string? content, int? categoryId, string? image, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> ChangeStatusAsync(int id, PublishStatus status, CancellationToken cancellationToken = default);
    Task<List<Blog>> GetTrashAsync(CancellationToken cancellationToken = default);
    Task<bool> RestoreAsync(int id, CancellationToken cancellationToken = default);
}
