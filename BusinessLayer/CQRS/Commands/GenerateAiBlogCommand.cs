using BusinessLayer.Dtos;
using MediatR;
namespace BusinessLayer.CQRS.Commands;
public sealed record GenerateAiBlogCommand(string Topic,int CategoryId,string Language="tr-TR",string Tone="luxury",int ReadingMinutes=4) : IRequest<AiBlogDraftDto>;
