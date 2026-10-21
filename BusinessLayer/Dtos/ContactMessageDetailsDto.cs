using EntityLayer.Concrete;
namespace BusinessLayer.Dtos;
public sealed record ContactReplyDto(int Id, string ReplyMessage, string RepliedByUserName, DateTime RepliedAt, bool IsMailSent, int AttemptCount);
public sealed record ContactMessageDetailsDto(ContactForm Message, IReadOnlyList<ContactReplyDto> Replies, int Page, int TotalReplies);
