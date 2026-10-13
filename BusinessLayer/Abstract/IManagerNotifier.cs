namespace BusinessLayer.Abstract;

public interface IManagerNotifier
{
    Task NotifyAsync(string title, string message, CancellationToken cancellationToken = default);
}
