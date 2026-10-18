using BusinessLayer.Abstract;
using BusinessLayer.CQRS.Commands;
using BusinessLayer.Dtos;
using BusinessLayer.Security;
using DataAccesLayer.Abstract;
using DataAccesLayer.Concrete;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
namespace BusinessLayer.CQRS.Handlers;
public sealed class GenerateAiBlogHandler(IAiBlogService ai,AppDbContext db,ICurrentUserService current,IAuthorizationService authorization) : IRequestHandler<GenerateAiBlogCommand,AiBlogDraftDto>
{
    public async Task<AiBlogDraftDto> Handle(GenerateAiBlogCommand request,CancellationToken token)
    {
        if(!(await authorization.AuthorizeAsync(current.User,AccessPolicies.ContentManager)).Succeeded)throw new UnauthorizedAccessException();
        var category=await db.Catagories.AsNoTracking().Where(x=>x.Id==request.CategoryId && x.IsPublish).Select(x=>x.Title).SingleOrDefaultAsync(token);
        if(string.IsNullOrWhiteSpace(category))throw new ValidationException("Yayında bir kategori seçin.");
        return await ai.GenerateAsync(new(request.Topic.Trim(),category,request.Language,request.Tone),token);
    }
}
