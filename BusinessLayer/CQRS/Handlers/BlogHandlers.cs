using System.Linq.Expressions;
using BusinessLayer.Abstract;
using BusinessLayer.CQRS.Commands;
using BusinessLayer.CQRS.Queries;
using BusinessLayer.Dtos;
using BusinessLayer.Security;
using DataAccesLayer.Abstract;
using DataAccesLayer.Concrete;
using EntityLayer.Concrete;
using EntityLayer.Enum;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace BusinessLayer.CQRS.Handlers;

internal static class BlogProjection
{
    internal static readonly Expression<Func<Blog, BlogAdminDto>> Admin = b => new BlogAdminDto(b.Id,b.Title,b.SubTitle,b.Slug,b.Image,b.CatagoryId,b.AppUserId,b.Content,b.PublishStatus,b.CreatedDate,b.UpdatedDate,b.DeletedDate,b.DeletedBy);
    internal static readonly Expression<Func<Blog, BlogListDto>> Public = b => new BlogListDto(b.Id,b.Title,b.SubTitle,b.Slug,b.Image,b.CatagoryId,b.Catagory == null ? null : b.Catagory.Title,b.AppUser == null ? null : b.AppUser.FirstName + " " + b.AppUser.LastName,b.CreatedDate);
    internal static async Task Require(IAuthorizationService auth, ICurrentUserService user, string policy, Blog? resource = null)
    { if (!(await auth.AuthorizeAsync(user.User, resource, policy)).Succeeded) throw new UnauthorizedAccessException(); }
    internal static async Task<PaginatedList<T>> Page<T>(IQueryable<T> query, int page, int size, CancellationToken ct)
        => new(await query.Skip((page-1)*size).Take(size).ToListAsync(ct), await query.CountAsync(ct), page, size);
}

public sealed class GetBlogsQueryHandler(AppDbContext db, ICurrentUserService user, IAuthorizationService auth) : IRequestHandler<GetBlogsQuery, PaginatedList<BlogAdminDto>>
{
    public async Task<PaginatedList<BlogAdminDto>> Handle(GetBlogsQuery request, CancellationToken ct)
    {
        await BlogProjection.Require(auth,user,AccessPolicies.BlogAuthor);
        var query = db.Blogs.AsNoTracking();
        if (!(await auth.AuthorizeAsync(user.User,AccessPolicies.ContentManager)).Succeeded)
        {
            if (!int.TryParse(user.UserId,out var id)) throw new UnauthorizedAccessException();
            query = query.Where(b=>b.AppUserId==id);
        }
        if (request.CategoryId.HasValue) query=query.Where(b=>b.CatagoryId==request.CategoryId);
        return await BlogProjection.Page(query.OrderByDescending(b=>b.CreatedDate).ThenByDescending(b=>b.Id).Select(BlogProjection.Admin),request.PageIndex,request.PageSize,ct);
    }
}

public sealed class GetBlogByIdQueryHandler(AppDbContext db, ICurrentUserService user, IAuthorizationService auth) : IRequestHandler<GetBlogByIdQuery, BlogAdminDto?>
{
    public async Task<BlogAdminDto?> Handle(GetBlogByIdQuery request, CancellationToken ct)
    {
        await BlogProjection.Require(auth,user,AccessPolicies.BlogAuthor);
        var resource=await db.Blogs.AsNoTracking().Where(b=>b.Id==request.Id).Select(b=>new Blog {Id=b.Id,AppUserId=b.AppUserId}).SingleOrDefaultAsync(ct);
        if(resource is null)return null;
        await BlogProjection.Require(auth,user,AccessPolicies.BlogResource,resource);
        return await db.Blogs.AsNoTracking().Where(b=>b.Id==request.Id).Select(BlogProjection.Admin).SingleOrDefaultAsync(ct);
    }
}

public sealed class GetPublicBlogsQueryHandler(AppDbContext db, ICacheService cache) : IRequestHandler<GetPublicBlogsQuery, PaginatedList<BlogListDto>>
{
    public async Task<PaginatedList<BlogListDto>> Handle(GetPublicBlogsQuery request,CancellationToken ct)
    {
        var query=db.Blogs.AsNoTracking().Where(b=>!b.IsDeleted && b.PublishStatus==PublishStatus.Published);
        if(request.CategoryId.HasValue)query=query.Where(b=>b.CatagoryId==request.CategoryId);
        if(request.ExcludeId.HasValue)query=query.Where(b=>b.Id!=request.ExcludeId);
        // A lightweight DB fingerprint also protects publication rules across nodes during Redis outages.
        var stamp=await query.GroupBy(b=>1).Select(g=>new {Count=g.Count(),Last=g.Max(b=>b.UpdatedDate ?? b.CreatedDate),Id=g.Max(b=>b.Id)}).SingleOrDefaultAsync(ct);
        var prefix=request.CategoryId.HasValue ? $"blogs_cat_{request.CategoryId}" : "blogs_all";
        var key=$"{prefix}:p{request.PageIndex}:s{request.PageSize}:exclude{request.ExcludeId}:n{stamp?.Count ?? 0}:v{stamp?.Last.Ticks ?? 0}:id{stamp?.Id ?? 0}";
        var cached=await cache.GetAsync<PaginatedList<BlogListDto>>(key);
        if(cached is not null)return cached;
        var items=await query.OrderByDescending(b=>b.CreatedDate).ThenByDescending(b=>b.Id).Skip((request.PageIndex-1)*request.PageSize).Take(request.PageSize).Select(BlogProjection.Public).ToListAsync(ct);
        var result=new PaginatedList<BlogListDto>(items,stamp?.Count ?? 0,request.PageIndex,request.PageSize);
        await cache.SetAsync(key,result,TimeSpan.FromMinutes(10));
        return result;
    }
}

public sealed class GetPublicBlogBySlugQueryHandler(AppDbContext db, IHtmlContentSanitizer sanitizer) : IRequestHandler<GetPublicBlogBySlugQuery,BlogDetailDto?>
{
    public async Task<BlogDetailDto?> Handle(GetPublicBlogBySlugQuery request,CancellationToken ct)
    {
        var detail=await db.Blogs.AsNoTracking().Where(b=>!b.IsDeleted && b.PublishStatus==PublishStatus.Published && b.Slug==request.Slug)
            .Select(b=>new BlogDetailDto(b.Id,b.Title,b.SubTitle,b.Slug,b.Image,b.CatagoryId,b.Catagory==null?null:b.Catagory.Title,b.AppUser==null?null:b.AppUser.FirstName+" "+b.AppUser.LastName,b.CreatedDate,b.Content)).FirstOrDefaultAsync(ct);
        return detail is null ? null : detail with {Content=sanitizer.Sanitize(detail.Content)};
    }
}

public sealed class GetTrashedBlogsQueryHandler(AppDbContext db,ICurrentUserService user,IAuthorizationService auth) : IRequestHandler<GetTrashedBlogsQuery,PaginatedList<BlogAdminDto>>
{
    public async Task<PaginatedList<BlogAdminDto>> Handle(GetTrashedBlogsQuery request,CancellationToken ct)
    {
        await BlogProjection.Require(auth,user,AccessPolicies.ContentManager);
        return await BlogProjection.Page(db.Blogs.IgnoreQueryFilters().AsNoTracking().Where(b=>b.IsDeleted).OrderByDescending(b=>b.DeletedDate).ThenByDescending(b=>b.Id).Select(BlogProjection.Admin),request.PageIndex,request.PageSize,ct);
    }
}
public sealed class GetCategoryOptionsQueryHandler(AppDbContext db,ICurrentUserService user,IAuthorizationService auth) : IRequestHandler<GetCategoryOptionsQuery,List<CategoryOptionDto>>
{
    public async Task<List<CategoryOptionDto>> Handle(GetCategoryOptionsQuery request,CancellationToken ct)
    {
        await BlogProjection.Require(auth,user,AccessPolicies.BlogAuthor);
        return await db.Catagories.AsNoTracking().OrderBy(c=>c.Title).Select(c=>new CategoryOptionDto(c.Id,c.Title)).ToListAsync(ct);
    }
}

public sealed class CreateBlogCommandHandler(IBlogWorkflowService workflow,IBlogImageStorage images,ICurrentUserService user,IAuthorizationService auth,AppDbContext db) : IRequestHandler<CreateBlogCommand,int>
{
    public async Task<int> Handle(CreateBlogCommand r,CancellationToken ct)
    {
        await BlogProjection.Require(auth,user,AccessPolicies.BlogAuthor);
        if(!await db.Catagories.AnyAsync(c=>c.Id==r.CatagoryId,ct))throw new FluentValidation.ValidationException("Kategori bulunamadı.");
        var image=await images.StoreAsync(r.ImageFile,ct);
        return await workflow.CreateAsync(new Blog {Title=r.Title,SubTitle=r.SubTitle,Content=r.Content,CatagoryId=r.CatagoryId,Image=image},ct);
    }
}
public sealed class UpdateBlogCommandHandler(IBlogWorkflowService workflow,IBlogImageStorage images,ISender sender,AppDbContext db) : IRequestHandler<UpdateBlogCommand,bool>
{
    public async Task<bool> Handle(UpdateBlogCommand r,CancellationToken ct)
    {
        if(await sender.Send(new GetBlogByIdQuery(r.Id),ct) is null)return false;
        if(!await db.Catagories.AnyAsync(c=>c.Id==r.CatagoryId,ct))throw new FluentValidation.ValidationException("Kategori bulunamadı.");
        var image=await images.StoreAsync(r.ImageFile,ct);
        return await workflow.UpdateAsync(r.Id,r.Title,r.SubTitle,r.Content,r.CatagoryId,image,ct);
    }
}
public sealed class DeleteBlogCommandHandler(IBlogWorkflowService workflow) : IRequestHandler<DeleteBlogCommand,bool>
{public Task<bool> Handle(DeleteBlogCommand r,CancellationToken ct)=>workflow.DeleteAsync(r.Id,ct);}
public sealed class ChangeBlogStatusCommandHandler(IBlogWorkflowService workflow) : IRequestHandler<ChangeBlogStatusCommand,bool>
{public Task<bool> Handle(ChangeBlogStatusCommand r,CancellationToken ct)=>workflow.ChangeStatusAsync(r.Id,r.Status,ct);}
public sealed class RestoreBlogCommandHandler(IBlogWorkflowService workflow) : IRequestHandler<RestoreBlogCommand,bool>
{public Task<bool> Handle(RestoreBlogCommand r,CancellationToken ct)=>workflow.RestoreAsync(r.Id,ct);}
