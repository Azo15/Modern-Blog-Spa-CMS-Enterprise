namespace BusinessLayer.Abstract;

public interface IAppointmentEmailQueue
{
    string Enqueue(int deliveryId);
}
