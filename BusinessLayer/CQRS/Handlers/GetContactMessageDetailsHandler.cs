using BusinessLayer.Abstract;
using BusinessLayer.CQRS.Queries;
using BusinessLayer.Dtos;
using BusinessLayer.Security;
using DataAccesLayer.Abstract;
using DataAccesLayer.Concrete;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
namespace BusinessLayer.CQRS.Handlers;
public sealed class GetContactMessageDetailsHandler(AppDbContext db, ICurrentUserService current, IActiveMemberService members, IAuthorizationService auth)
    : IRequestHandler<GetContactMessageDetailsQuery, ContactMessageDetailsDto?>
{
    public async Task<ContactMessageDetailsDto?> Handle(GetContactMessageDetailsQuery request, CancellationToken ct)
    {
        await members.RequireAsync(ct);
        if (!(await auth.AuthorizeAsync(current.User, AccessPolicies.Moderator)).Succeeded) throw new UnauthorizedAccessException();
        var message = await db.ContactForms.AsNoTracking().SingleOrDefaultAsync(x => x.Id == request.Id, ct);
        if (message is null) return null;
        var replies = db.ContactReplies.AsNoTracking().Where(x => x.ContactFormId == request.Id);
        var count = await replies.CountAsync(ct); var page = Math.Clamp(request.Page, 1, Math.Max(1, (count + 19) / 20));
        var items = await replies.OrderByDescending(x => x.RepliedAt).ThenByDescending(x => x.Id).Skip((page - 1) * 20).Take(20)
            .Select(x => new ContactReplyDto(x.Id, x.ReplyMessage, x.RepliedByUserName, x.RepliedAt, x.IsMailSent, x.AttemptCount)).ToListAsync(ct);
        return new(message, items, page, count);
    }
}
