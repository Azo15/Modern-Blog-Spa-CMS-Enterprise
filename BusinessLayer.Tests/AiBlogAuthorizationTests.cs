using BusinessLayer.Abstract;
using BusinessLayer.CQRS.Commands;
using BusinessLayer.CQRS.Handlers;
using BusinessLayer.Dtos;
using BusinessLayer.Security;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Moq;
namespace BusinessLayer.Tests;
public sealed class AiBlogAuthorizationTests
{
    [Theory]
    [InlineData(AppRoles.Admin,true)] [InlineData(AppRoles.Staff,true)] [InlineData(AppRoles.Customer,false)]
    public async Task OnlyPanelRolesCanGenerateDrafts(string role,bool allowed)
    {
        using var fixture=new Stage2Fixture();fixture.As(role);using var provider=AuthorizationTests.Provider();
        var ai=new Mock<IAiBlogService>();ai.Setup(x=>x.GenerateAsync(It.IsAny<AiBlogPrompt>(),It.IsAny<CancellationToken>())).ReturnsAsync(new AiBlogDraftDto("Title","Subtitle","Summary","<p>Draft</p>"));
        var handler=new GenerateAiBlogHandler(ai.Object,fixture.Db,fixture.User,provider.GetRequiredService<IAuthorizationService>());
        if(allowed){var result=await handler.Handle(new("Spa rituals",1),default);Assert.Equal("Title",result.Title);ai.Verify(x=>x.GenerateAsync(It.Is<AiBlogPrompt>(p=>p.Category=="A"),It.IsAny<CancellationToken>()),Times.Once);}
        else{await Assert.ThrowsAsync<UnauthorizedAccessException>(()=>handler.Handle(new("Spa rituals",1),default));ai.VerifyNoOtherCalls();}
    }
    [Theory]
    [InlineData("",1,"tr-TR","luxury")] [InlineData("Spa rituals",0,"tr-TR","luxury")] [InlineData("Spa rituals",1,"xx","luxury")] [InlineData("Spa rituals",1,"tr-TR","unknown")]
    public async Task PipelineRejectsInvalidGenerationBeforeCallingProvider(string topic,int category,string language,string tone)
    {using var fixture=new Stage2Fixture();await Assert.ThrowsAsync<ValidationException>(()=>fixture.Sender.Send(new GenerateAiBlogCommand(topic,category,language,tone)));}
}
