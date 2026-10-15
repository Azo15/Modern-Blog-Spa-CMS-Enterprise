using EntityLayer.Concrete;

namespace BusinessLayer.Abstract;

public interface IAppointmentMailService
{
    Task SendReceiptAsync(Appointment appointment, string messageId, CancellationToken cancellationToken = default);
}
