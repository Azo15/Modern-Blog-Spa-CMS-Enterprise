namespace BusinessLayer.Abstract;

public interface IContactEmailQueue
{
    string Enqueue(int deliveryId);
}
