using MediatR;
namespace BusinessLayer.CQRS.Commands;
public sealed record ReplyContactMessageCommand(int ContactFormId, string ReplyMessage, Guid ClientRequestId) : IRequest<ContactReplyResult>;
public sealed record ContactReplyResult(int Id, bool Queued);
