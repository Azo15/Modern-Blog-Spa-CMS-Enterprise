using EntityLayer.Concrete;
using EntityLayer.Enum;
using Microsoft.EntityFrameworkCore;
namespace BusinessLayer.Tests;
public sealed class NotificationSchemaTests
{
    [Fact]
    public async Task EventsAreUniquePerRecipientAndReadStateIsIndependent()
    {
        using var f = new Stage2Fixture();
        f.Db.Notifications.AddRange(new Notification { RecipientUserId = 1, EventKey = "contact:1", Type = NotificationType.ContactMessage, Title = "Message" }, new Notification { RecipientUserId = 2, EventKey = "contact:1", Type = NotificationType.ContactMessage, Title = "Message", IsRead = true });
        await f.Db.SaveChangesAsync(); Assert.Equal(1, await f.Db.Notifications.CountAsync(x => !x.IsRead));
        f.Db.Notifications.Add(new Notification { RecipientUserId = 1, EventKey = "contact:1", Title = "Duplicate" });
        await Assert.ThrowsAsync<DbUpdateException>(() => f.Db.SaveChangesAsync());
    }
}
