using System.ComponentModel.DataAnnotations;
using BusinessLayer.Abstract;
using BusinessLayer.CQRS.Commands;
using BusinessLayer.Security;
using DataAccesLayer.Abstract;
using DataAccesLayer.Concrete;
using EntityLayer.Concrete;
using EntityLayer.Enum;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BusinessLayer.CQRS.Handlers;

public sealed class ReplyContactMessageHandler(AppDbContext db, ICurrentUserService current,
    IActiveMemberService members, IAuthorizationService authorization, IContactReplyQueue queue,
    ILogger<ReplyContactMessageHandler> logger, IContactReplyMailService mail) : IRequestHandler<ReplyContactMessageCommand, ContactReplyResult>
{
    public async Task<ContactReplyResult> Handle(ReplyContactMessageCommand request, CancellationToken ct)
    {
        var userId = await members.RequireAsync(ct);
        if (!(await authorization.AuthorizeAsync(current.User, AccessPolicies.Moderator)).Succeeded) throw new UnauthorizedAccessException();
        var message = await db.ContactForms.SingleOrDefaultAsync(x => x.Id == request.ContactFormId, ct)
            ?? throw new FluentValidation.ValidationException("Mesaj bulunamadı.");
        if (string.IsNullOrWhiteSpace(message.Email) || !new EmailAddressAttribute().IsValid(message.Email))
            throw new FluentValidation.ValidationException("Mesajın e-posta adresi geçersiz; yanıt gönderilemez.");
        var existing = await db.ContactReplies.SingleOrDefaultAsync(x => x.ClientRequestId == request.ClientRequestId, ct);
        if (existing is not null) return CheckRetry(existing, request, userId);
        try { await mail.EnsureConfiguredAsync(ct); }
        catch (BusinessLayer.Concrete.SmtpConfigurationException error)
        { throw new FluentValidation.ValidationException(error.Message); }
        var user = await db.Users.AsNoTracking().SingleAsync(x => x.Id == userId, ct);
        var name = $"{user.FirstName} {user.LastName}".Trim();
        var reply = new ContactReply { ContactFormId = message.Id, ReplyMessage = request.ReplyMessage.Trim(),
            RepliedByUserId = userId.ToString(System.Globalization.CultureInfo.InvariantCulture),
            RepliedByUserName = string.IsNullOrEmpty(name) ? user.UserName ?? "XSPA Ekibi" : name,
            RecipientEmail = message.Email.Trim(), Subject = (message.Subject ?? "İletişim mesajınız").Replace('\r', ' ').Replace('\n', ' '),
            ClientRequestId = request.ClientRequestId, RepliedAt = DateTime.UtcNow };
        if (reply.Subject.Length > 250) reply.Subject = reply.Subject[..250];
        db.ContactReplies.Add(reply); message.IsReplied = true; message.IsRead = true;
        message.LastRepliedAt = reply.RepliedAt; message.Status = ReservationStatus.Replied;
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateException)
        {
            db.Entry(reply).State = EntityState.Detached;
            var duplicate = await db.ContactReplies.AsNoTracking().SingleOrDefaultAsync(x => x.ClientRequestId == request.ClientRequestId, ct);
            if (duplicate is null) throw;
            db.Entry(message).State = EntityState.Unchanged;
            return CheckRetry(duplicate, request, userId);
        }
        try
        {
            var jobId = queue.Enqueue(reply.Id);
            await db.ContactReplies.Where(x => x.Id == reply.Id).ExecuteUpdateAsync(s =>
                s.SetProperty(x => x.HangfireJobId, jobId).SetProperty(x => x.LastEnqueuedAtUtc, DateTime.UtcNow), ct);
            return new(reply.Id, true);
        }
        catch (Exception error) when (error is not OperationCanceledException)
        {
            logger.LogWarning("Reply {ReplyId} saved; queue recovery required ({ErrorType}).", reply.Id, error.GetType().Name);
            return new(reply.Id, false);
        }
    }
    private static ContactReplyResult CheckRetry(ContactReply reply, ReplyContactMessageCommand request, int userId)
    {
        if (reply.ContactFormId != request.ContactFormId || reply.RepliedByUserId != userId.ToString(System.Globalization.CultureInfo.InvariantCulture) || reply.ReplyMessage != request.ReplyMessage.Trim())
            throw new FluentValidation.ValidationException("İstek kimliği başka bir yanıt için kullanılmış. Sayfayı yenileyin.");
        return new(reply.Id, reply.IsMailSent || reply.LastEnqueuedAtUtc.HasValue);
    }
}
