namespace BusinessLayer.Abstract;

public interface IProfanityFilterService
{
    bool IsAllowed(string content);
}
