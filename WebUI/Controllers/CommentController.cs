using BusinessLayer.CQRS.Commands;
using BusinessLayer.Security;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace WebUI.Controllers;
[Authorize(Policy=AccessPolicies.Member)]
public sealed class CommentController(ISender sender) : Controller
{
    [HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(int blogId,string content,int? parentCommentId,string slug,CancellationToken ct)
    {
        try{await sender.Send(new CreateCommentCommand(blogId,content ?? "",parentCommentId),ct);TempData["CommentSuccess"]="Yorumunuz onay için gönderildi.";}
        catch(ValidationException ex){TempData["CommentError"]=ex.Message;}
        return RedirectToAction("Post","Blog",new{slug});
    }
    [HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(int id,string content,string slug,CancellationToken ct)
    {
        try{if(!await sender.Send(new UpdateCommentCommand(id,content ?? ""),ct))return NotFound();TempData["CommentSuccess"]="Yorum güncellendi; yeniden onay bekliyor.";}
        catch(ValidationException ex){TempData["CommentError"]=ex.Message;}
        return RedirectToAction("Post","Blog",new{slug});
    }
    [HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id,string slug,CancellationToken ct)
    {
        if(!await sender.Send(new DeleteCommentCommand(id),ct))return NotFound();
        return RedirectToAction("Post","Blog",new{slug});
    }
}
