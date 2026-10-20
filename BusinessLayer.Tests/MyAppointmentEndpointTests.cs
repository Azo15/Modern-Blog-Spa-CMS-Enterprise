using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using BusinessLayer.ValidationRules;
using DataAccesLayer.Concrete;
using EntityLayer.Concrete;
using EntityLayer.Enum;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BusinessLayer.Tests;
public sealed class MyAppointmentEndpointTests
{
    private static HttpClient Client(PlatformWebFactory f) => f.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress=new Uri("https://localhost"),AllowAutoRedirect=false });
    private static async Task<string> Token(HttpClient c,string path="/") => WebUtility.HtmlDecode(Regex.Match(await c.GetStringAsync(path),"name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value);
    private static async Task Login(HttpClient c,string email="user@acceptance.test")
    {
        var r=await c.PostAsync("/dashboard/giris",new FormUrlEncodedContent(new Dictionary<string,string>{["Email"]=email,["Password"]=PlatformWebFactory.Password,["__RequestVerificationToken"]=await Token(c,"/dashboard/giris")}));
        Assert.Equal(HttpStatusCode.Redirect,r.StatusCode);
    }
    private static async Task Init(PlatformWebFactory f)
    {
        await f.InitializeAsync(); using var scope=f.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Services.Add(new Service{Id=1,Title="Customer ritual",IsPublish=true});await db.SaveChangesAsync();
    }
    private static async Task<int> Create(PlatformWebFactory f,HttpClient c)
    {
        var r=await c.PostAsync("/randevu",new FormUrlEncodedContent(new Dictionary<string,string>{["ServiceId"]="1",["AppointmentDate"]=DateOnly.FromDateTime(AppointmentSchedule.LocalNow).AddDays(2).ToString("yyyy-MM-dd"),["TimeSlot"]="14:00",["GuestCount"]="1",["FullName"]="Guest Name",["Phone"]="05380000000",["Email"]="user@acceptance.test",["ClientRequestId"]=Guid.NewGuid().ToString(),["__RequestVerificationToken"]=await Token(c)}));
        Assert.Equal(HttpStatusCode.Created,r.StatusCode);using var scope=f.Services.CreateScope();return await scope.ServiceProvider.GetRequiredService<AppDbContext>().Appointments.MaxAsync(a=>a.Id);
    }
    private static async Task<Appointment> Read(PlatformWebFactory f,int id)
    {using var scope=f.Services.CreateScope();return await scope.ServiceProvider.GetRequiredService<AppDbContext>().Appointments.AsNoTracking().SingleAsync(a=>a.Id==id);}
    private static async Task<Dictionary<string,string>> EditValues(HttpClient c,int id,DateTime? updated=null)
        => new(){["Id"]=id.ToString(),["ServiceId"]="1",["AppointmentDate"]=DateOnly.FromDateTime(AppointmentSchedule.LocalNow).AddDays(3).ToString("yyyy-MM-dd"),["TimeSlot"]="16:30",["GuestCount"]="2",["Notes"]="Changed by owner",["ExpectedUpdatedDate"]=updated?.ToString("O",CultureInfo.InvariantCulture)??"",["__RequestVerificationToken"]=await Token(c)};

    [Fact]
    public async Task AjaxRescheduleAndCancelUseHeaderCsrfAndRejectStaleChanges()
    {
        using var f = new PlatformWebFactory(); await Init(f); using var c = Client(f); await Login(c); var id = await Create(f, c);
        c.DefaultRequestHeaders.Add("Accept", "application/json");
        c.DefaultRequestHeaders.Add("RequestVerificationToken", await Token(c));
        var fields = await EditValues(c, id); fields.Remove("__RequestVerificationToken");
        var edit = await c.PostAsync($"/randevularim/{id}/duzenle", new FormUrlEncodedContent(fields)); Assert.Equal(HttpStatusCode.OK, edit.StatusCode);
        var state = System.Text.Json.JsonDocument.Parse(await edit.Content.ReadAsStringAsync());
        Assert.True(state.RootElement.GetProperty("success").GetBoolean());
        var updated = state.RootElement.GetProperty("updatedDate").GetString(); Assert.False(string.IsNullOrWhiteSpace(updated));
        var stale = await c.PostAsync($"/randevularim/{id}/iptal", new FormUrlEncodedContent(new Dictionary<string, string>()));
        Assert.Equal(HttpStatusCode.BadRequest, stale.StatusCode); Assert.Equal(AppointmentStatus.Pending, (await Read(f, id)).Status);
        var cancel = await c.PostAsync($"/randevularim/{id}/iptal", new FormUrlEncodedContent(new Dictionary<string, string> { ["expectedUpdatedDate"] = updated! }));
        Assert.Equal(HttpStatusCode.OK, cancel.StatusCode); Assert.Contains("\"status\":\"cancelled\"", await cancel.Content.ReadAsStringAsync());
        Assert.Equal(AppointmentStatus.Cancelled, (await Read(f, id)).Status);
    }

    [Theory]
    [InlineData("/Profile/RemoveFavorite")]
    [InlineData("/Comment/Create")]
    [InlineData("/BlogInteraction/Favorite")]
    [InlineData("/randevularim/1/iptal")]
    public async Task AnonymousAjaxMutationGets401WithoutHtmlRedirect(string path)
    {
        using var f = new PlatformWebFactory(); await Init(f); using var c = Client(f);
        c.DefaultRequestHeaders.Add("Accept", "application/json");
        var response = await c.PostAsync(path, new FormUrlEncodedContent(new Dictionary<string, string> { ["__RequestVerificationToken"] = await Token(c) }));
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode); Assert.Null(response.Headers.Location);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task OwnerCanListRescheduleAndCancelWithAuditAndStableReference()
    {
        using var f=new PlatformWebFactory();await Init(f);using var c=Client(f);await Login(c);var id=await Create(f,c);var original=await Read(f,id);
        Assert.Equal("5",original.CreatedBy);Assert.Contains(original.ReferenceCode,await c.GetStringAsync("/randevularim"));
        Assert.Equal(HttpStatusCode.Redirect,(await c.PostAsync($"/randevularim/{id}/duzenle",new FormUrlEncodedContent(await EditValues(c,id)))).StatusCode);
        var edited=await Read(f,id);Assert.Equal("16:30",edited.TimeSlot);Assert.Equal(2,edited.GuestCount);Assert.Equal(original.ReferenceCode,edited.ReferenceCode);Assert.Equal(original.FullName,edited.FullName);Assert.Equal("5",edited.CreatedBy);Assert.Equal("5",edited.UpdatedBy);Assert.NotNull(edited.UpdatedDate);
        var cancel=await c.PostAsync($"/randevularim/{id}/iptal",new FormUrlEncodedContent(new Dictionary<string,string>{["expectedUpdatedDate"]=edited.UpdatedDate?.ToString("O")??"",["__RequestVerificationToken"]=await Token(c)}));
        Assert.Equal(HttpStatusCode.Redirect,cancel.StatusCode);Assert.Equal(AppointmentStatus.Cancelled,(await Read(f,id)).Status);
        Assert.Equal(HttpStatusCode.Redirect,(await c.GetAsync($"/randevularim/{id}/duzenle")).StatusCode);
    }
    [Fact]
    public async Task OtherUserCannotReadEditOrCancelEvenWithForgedOwnerFields()
    {
        using var f=new PlatformWebFactory();await Init(f);using var owner=Client(f);await Login(owner);var id=await Create(f,owner);
        using var other=Client(f);await Login(other,"other-author@acceptance.test");Assert.DoesNotContain((await Read(f,id)).ReferenceCode,await other.GetStringAsync("/randevularim"));
        Assert.Equal(HttpStatusCode.NotFound,(await other.GetAsync($"/randevularim/{id}/duzenle")).StatusCode);
        var values=await EditValues(other,id);values["CreatedBy"]="5";values["Status"]="1";
        Assert.Equal(HttpStatusCode.NotFound,(await other.PostAsync($"/randevularim/{id}/duzenle",new FormUrlEncodedContent(values))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,(await other.PostAsync($"/randevularim/{id}/iptal",new FormUrlEncodedContent(values))).StatusCode);
        Assert.Equal(AppointmentStatus.Pending,(await Read(f,id)).Status);
    }
    [Fact]
    public async Task GuestBookingIsNeverClaimedByMatchingUnverifiedEmail()
    {
        using var f=new PlatformWebFactory();await Init(f);using var c=Client(f);var id=await Create(f,c);Assert.Null((await Read(f,id)).CreatedBy);
        await Login(c);Assert.DoesNotContain((await Read(f,id)).ReferenceCode,await c.GetStringAsync("/randevularim"));
        Assert.Equal(HttpStatusCode.NotFound,(await c.GetAsync($"/randevularim/{id}/duzenle")).StatusCode);
    }
    [Fact]
    public async Task AnonymousAndCsrfFreeChangesAreRejected()
    {
        using var f=new PlatformWebFactory();await Init(f);using var c=Client(f);Assert.Equal(HttpStatusCode.Redirect,(await c.GetAsync("/randevularim")).StatusCode);await Login(c);var id=await Create(f,c);
        Assert.Equal(HttpStatusCode.BadRequest,(await c.PostAsync($"/randevularim/{id}/iptal",new FormUrlEncodedContent(new Dictionary<string,string>()))).StatusCode);
    }
    [Fact]
    public async Task StaleAndInvalidChangesLeaveTheBookingUnchanged()
    {
        using var f=new PlatformWebFactory();await Init(f);using var c=Client(f);await Login(c);var id=await Create(f,c);
        var values=await EditValues(c,id);Assert.Equal(HttpStatusCode.Redirect,(await c.PostAsync($"/randevularim/{id}/duzenle",new FormUrlEncodedContent(values))).StatusCode);
        values["TimeSlot"]="20:00";Assert.Equal(HttpStatusCode.OK,(await c.PostAsync($"/randevularim/{id}/duzenle",new FormUrlEncodedContent(values))).StatusCode);Assert.Equal("16:30",(await Read(f,id)).TimeSlot);
        var latest=await Read(f,id);values=await EditValues(c,id,latest.UpdatedDate);values["AppointmentDate"]="2000-01-01";
        Assert.Equal(HttpStatusCode.OK,(await c.PostAsync($"/randevularim/{id}/duzenle",new FormUrlEncodedContent(values))).StatusCode);Assert.Equal(latest.AppointmentDate,(await Read(f,id)).AppointmentDate);
    }
    [Fact]
    public async Task ReconfirmationAfterCustomerEditReusesDurableConfirmationRecord()
    {
        using var f=new PlatformWebFactory();await Init(f);using var c=Client(f);await Login(c);var id=await Create(f,c);using var admin=Client(f);await Login(admin,"admin@acceptance.test");
        async Task Approve()=>Assert.Equal(HttpStatusCode.Redirect,(await admin.PostAsync("/AdminAppointment/Approve",new FormUrlEncodedContent(new Dictionary<string,string>{["id"]=id.ToString(),["__RequestVerificationToken"]=await Token(admin)}))).StatusCode);
        await Approve();var confirmed=await Read(f,id);Assert.Equal(AppointmentStatus.Confirmed,confirmed.Status);
        Assert.Equal(HttpStatusCode.Redirect,(await c.PostAsync($"/randevularim/{id}/duzenle",new FormUrlEncodedContent(await EditValues(c,id,confirmed.UpdatedDate)))).StatusCode);Assert.Equal(AppointmentStatus.Pending,(await Read(f,id)).Status);
        await Approve();Assert.Equal(AppointmentStatus.Confirmed,(await Read(f,id)).Status);using var scope=f.Services.CreateScope();Assert.Equal(1,await scope.ServiceProvider.GetRequiredService<AppDbContext>().AppointmentEmailDeliveries.CountAsync(d=>d.AppointmentId==id&&d.IsConfirmation));
    }
    [Fact]
    public async Task NavbarUsesCustomerNameAndShowsAppointmentLink()
    {
        using var f=new PlatformWebFactory();await Init(f);using var c=Client(f);await Login(c);var html=await c.GetStringAsync("/");
        Assert.Contains("Test Customer</span>",html);Assert.DoesNotContain("luxe-account-label\">user@",html);Assert.Contains("/randevularim",html);
    }
    [Theory]
    [InlineData(true)] [InlineData(false)]
    public async Task PastOrCancelledAppointmentsCannotBeEdited(bool past)
    {
        using var f=new PlatformWebFactory();await Init(f);using var c=Client(f);await Login(c);var id=await Create(f,c);
        using(var scope=f.Services.CreateScope())
        {
            var db=scope.ServiceProvider.GetRequiredService<AppDbContext>();var a=await db.Appointments.SingleAsync(a=>a.Id==id);
            if(past)a.AppointmentDate=DateOnly.FromDateTime(AppointmentSchedule.LocalNow).AddDays(-1);else a.Status=AppointmentStatus.Cancelled;
            await db.SaveChangesAsync();
        }
        var original=await Read(f,id);var values=await EditValues(c,id,original.UpdatedDate);
        Assert.Equal(HttpStatusCode.OK,(await c.PostAsync($"/randevularim/{id}/duzenle",new FormUrlEncodedContent(values))).StatusCode);
        var after=await Read(f,id);Assert.Equal(original.AppointmentDate,after.AppointmentDate);Assert.Equal(original.Status,after.Status);
    }
}
