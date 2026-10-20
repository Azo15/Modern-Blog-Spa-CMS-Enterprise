using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
namespace WebUI.Infrastructure;

public static class GoogleLoginFailureHandler
{
    public static Task HandleAsync(RemoteFailureContext context)
    {
        // Do not copy provider responses, authorization codes or secrets into a URL or log.
        context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>()
            .CreateLogger("GoogleLogin").LogWarning("Google sign-in could not be completed. Failure type: {FailureType}", context.Failure?.GetType().Name);
        var tempData=context.HttpContext.RequestServices.GetRequiredService<ITempDataDictionaryFactory>().GetTempData(context.HttpContext);
        tempData["ErrorMessage"]="Google ile giriş tamamlanamadı. Lütfen yeniden deneyin veya e-posta ve parolanızla giriş yapın.";
        tempData.Save();
        context.HandleResponse();
        context.Response.Redirect("/dashboard/giris");
        return Task.CompletedTask;
    }
}
