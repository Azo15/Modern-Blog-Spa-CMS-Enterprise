using BusinessLayer.Dtos;
namespace BusinessLayer.Abstract;
public interface IAiBlogService
{
    Task<AiBlogDraftDto> GenerateAsync(AiBlogPrompt prompt, CancellationToken cancellationToken = default);
}
