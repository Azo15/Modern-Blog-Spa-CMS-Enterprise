using Microsoft.Extensions.Configuration;
using BusinessLayer.Abstract;
using BusinessLayer.Concrete;
using BusinessLayer.CQRS.Commands;
using BusinessLayer.Jobs;
using BusinessLayer.Security;
using EntityLayer.Concrete;
using EntityLayer.Enum;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace BusinessLayer.Tests;
public sealed class ContactReplyTests
{
    private static async Task<ContactForm> Message(Stage2Fixture f)
    {
        var message = new ContactForm { FullName = "Guest", Email = "guest@example.test", Subject = "Question", Message = "Hello" };
        f.Db.ContactForms.Add(message); await f.Db.SaveChangesAsync(); return message;
    }
    [Theory]
    [InlineData(AppRoles.Admin, true)]
    [InlineData(AppRoles.Staff, true)]
    [InlineData(AppRoles.Customer, false)]
    [InlineData(null, false)]
    public async Task OnlyActiveManagersCanCreateDurableReplies(string? role, bool allowed)
    {
        using var f = new Stage2Fixture(); f.As(role); var message = await Message(f);
        var command = new ReplyContactMessageCommand(message.Id, "Thank you", Guid.NewGuid());
        if (!allowed) { await Assert.ThrowsAsync<UnauthorizedAccessException>(() => f.Sender.Send(command)); Assert.Empty(await f.Db.ContactReplies.ToListAsync()); return; }
        var result = await f.Sender.Send(command); Assert.True(result.Queued);
        f.Db.ChangeTracker.Clear(); var saved = await f.Db.ContactForms.SingleAsync(); Assert.True(saved.IsReplied); Assert.True(saved.IsRead); Assert.Equal(ReservationStatus.Replied, saved.Status);
        var reply = await f.Db.ContactReplies.SingleAsync(); Assert.Equal("2", reply.RepliedByUserId); Assert.Equal("guest@example.test", reply.RecipientEmail); Assert.False(reply.IsMailSent); Assert.Equal(reply.RepliedAt, saved.LastRepliedAt);
    }
    [Fact]
    public async Task QueueOutageRetainsReplyAndRecoveryAndRetriesDoNotDuplicateIt()
    {
        using var f = new Stage2Fixture(); f.As(AppRoles.Staff); var message = await Message(f);
        f.ReplyQueue.Setup(q => q.Enqueue(It.IsAny<int>())).Throws(new IOException("Queue unavailable"));
        var command = new ReplyContactMessageCommand(message.Id, "Our answer", Guid.NewGuid());
        var result = await f.Sender.Send(command); Assert.False(result.Queued);
        Assert.Equal(result.Id, (await f.Sender.Send(command)).Id); Assert.Single(await f.Db.ContactReplies.ToListAsync());
        await Assert.ThrowsAsync<FluentValidation.ValidationException>(() => f.Sender.Send(command with { ReplyMessage = "Changed intent" }));
        f.ReplyQueue.Setup(q => q.Enqueue(It.IsAny<int>())).Returns("recovered-job");
        await new ContactReplyRecoveryJob(f.Db, f.ReplyQueue.Object, NullLogger<ContactReplyRecoveryJob>.Instance).RunAsync(default);
        f.Db.ChangeTracker.Clear(); Assert.Equal("recovered-job", (await f.Db.ContactReplies.SingleAsync()).HangfireJobId);
    }
    [Fact]
    public async Task MailJobRetriesFailuresMarksOnlySuccessAndDoesNotResendSentReply()
    {
        using var f = new Stage2Fixture(); f.As(AppRoles.Admin); var message = await Message(f);
        var result = await f.Sender.Send(new ReplyContactMessageCommand(message.Id, "Reply", Guid.NewGuid()));
        var mail = new Mock<IContactReplyMailService>(); mail.Setup(m => m.SendAsync(It.IsAny<ContactReply>(), It.IsAny<CancellationToken>())).ThrowsAsync(new IOException("private credential"));
        var job = new ContactReplyEmailJob(f.Db, mail.Object, NullLogger<ContactReplyEmailJob>.Instance);
        await Assert.ThrowsAsync<IOException>(() => job.SendAsync(result.Id, default)); f.Db.ChangeTracker.Clear();
        var pending = await f.Db.ContactReplies.SingleAsync(); Assert.False(pending.IsMailSent); Assert.Null(pending.LeaseToken); Assert.Equal("IOException", pending.LastError);
        mail.Setup(m => m.SendAsync(It.IsAny<ContactReply>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        await job.SendAsync(result.Id, default); await job.SendAsync(result.Id, default); f.Db.ChangeTracker.Clear();
        Assert.True((await f.Db.ContactReplies.SingleAsync()).IsMailSent); mail.Verify(m => m.SendAsync(It.IsAny<ContactReply>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }
    [Fact]
    public async Task MissingSmtpDoesNotCreateReplyOrMarkMessageReplied()
    {
        using var f = new Stage2Fixture(); f.As(AppRoles.Admin); var message = await Message(f);
        f.ReplyMail.Setup(m => m.EnsureConfiguredAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new SmtpConfigurationException("SMTP ayarları eksik."));
        await Assert.ThrowsAsync<FluentValidation.ValidationException>(() => f.Sender.Send(new ReplyContactMessageCommand(message.Id, "Keep my draft", Guid.NewGuid())));
        f.Db.ChangeTracker.Clear();
        Assert.Empty(await f.Db.ContactReplies.ToListAsync());
        Assert.False((await f.Db.ContactForms.SingleAsync()).IsReplied);
        f.ReplyQueue.Verify(q => q.Enqueue(It.IsAny<int>()), Times.Never);
    }
    [Theory]
    [InlineData(null, 0, false, "kuyruğunda")]
    [InlineData("InvalidOperationException", 1, false, "SMTP ayarlarını")]
    [InlineData("AuthenticationException", 1, false, "uygulama şifresini")]
    [InlineData("CryptographicException", 1, false, "okunamıyor")]
    [InlineData("IOException", 4, false, "başarısız")]
    [InlineData("AuthenticationException", 4, true, "SMTP sunucusuna iletildi")]
    public void DeliveryStatusDistinguishesFailureFromPendingAndSent(string? error, int attempts, bool sent, string expected)
    {
        var reply = new BusinessLayer.Dtos.ContactReplyDto(1, "Hello", "Admin", DateTime.UtcNow, sent, attempts, error);
        Assert.Contains(expected, reply.DeliveryStatus);
        Assert.DoesNotContain(error ?? "private-secret", reply.DeliveryStatus);
    }
    [Fact]
    public async Task SmtpPreflightUsesEffectiveSettingsWithoutSendingMail()
    {
        using var f = new Stage2Fixture();
        var settings = new Mock<ISiteTableService>(); settings.Setup(x => x.GetQueryable()).Returns(f.Db.SiteTables);
        var missing = new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build();
        await Assert.ThrowsAsync<SmtpConfigurationException>(() => new ContactReplyMailService(settings.Object, missing).EnsureConfiguredAsync(default));
        var values = new Dictionary<string, string?> { ["Smtp:Host"] = "smtp.gmail.com", ["Smtp:Port"] = "587", ["Smtp:Username"] = "sender@example.test", ["Smtp:SenderEmail"] = "sender@example.test", ["Smtp:Password"] = "test-only-not-a-real-password" };
        var valid = new Microsoft.Extensions.Configuration.ConfigurationBuilder().AddInMemoryCollection(values).Build();
        await new ContactReplyMailService(settings.Object, valid).EnsureConfiguredAsync(default);
        values["Smtp:Port"] = "25";
        var invalid = new Microsoft.Extensions.Configuration.ConfigurationBuilder().AddInMemoryCollection(values).Build();
        await Assert.ThrowsAsync<SmtpConfigurationException>(() => new ContactReplyMailService(settings.Object, invalid).EnsureConfiguredAsync(default));
    }
    [Fact]
    public void EmailTemplateEncodesReplyAndSendsOnlyToMessageRecipient()
    {
        using var mail = ContactReplyMailService.CreateMessage(new ContactReply { RecipientEmail = "guest@example.test", Subject = "Question", ReplyMessage = "<script>alert('x')</script>\nNext line" }, "team@example.test");
        Assert.Contains("&lt;script&gt;", mail.HtmlBody); Assert.DoesNotContain("<script>", mail.HtmlBody);
        Assert.Equal("guest@example.test", Assert.Single(mail.To.Mailboxes).Address); Assert.Empty(mail.Cc); Assert.Empty(mail.Bcc); Assert.Contains("Next line", mail.TextBody);
    }
}
