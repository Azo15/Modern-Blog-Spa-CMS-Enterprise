using BusinessLayer.CQRS.Queries;
using BusinessLayer.Security;
using BusinessLayer.ValidationRules;
using EntityLayer.Concrete;
using EntityLayer.Enum;
namespace BusinessLayer.Tests;
public sealed class AdminDashboardTests
{
    [Fact]
    public async Task DashboardRequiresManagerAndZeroFillsFourteenDays()
    {
        using var fixture = new Stage2Fixture();
        fixture.User.User = AuthorizationTests.Principal(AppRoles.RegisteredUser,"1");
        await Assert.ThrowsAsync<UnauthorizedAccessException>(()=>fixture.Sender.Send(new GetAdminDashboardQuery()));
        fixture.User.User = AuthorizationTests.Principal(AppRoles.Admin,"1");
        var report = await fixture.Sender.Send(new GetAdminDashboardQuery());
        Assert.Equal(14, report.Trend.Count); Assert.All(report.Trend,x=>Assert.Equal(0,x.Count));
        Assert.Equal(0, report.Pending); Assert.False(report.SmtpConfigured);
    }

    [Fact]
    public async Task DashboardExcludesCancelledAndDeletedBookingsAndUsesCurrentTariff()
    {
        using var fixture = new Stage2Fixture(); fixture.As(AppRoles.Admin);
        var today=DateOnly.FromDateTime(AppointmentSchedule.LocalNow);
        fixture.Db.Services.Add(new Service {Id=1,Title="Masaj",Content="60 dk — 650 ₺",IsPublish=true});
        Appointment Booking(int id, AppointmentStatus status, string email, int guests) => new()
        {
            Id=id,ServiceId=1,ServiceTitleSnapshot="Masaj",FullName="Test Guest",Email=email,Phone="05380000000",
            AppointmentDate=today,TimeSlot="14:00",Status=status,GuestCount=guests,ReferenceCode="TEST-"+id,ClientRequestId=Guid.NewGuid()
        };
        fixture.Db.Appointments.AddRange(Booking(1,AppointmentStatus.Pending,"guest@example.test",2),Booking(2,AppointmentStatus.Confirmed,"GUEST@example.test",3),Booking(3,AppointmentStatus.Cancelled,"cancelled@example.test",4));
        var deleted=Booking(4,AppointmentStatus.Pending,"deleted@example.test",4);deleted.IsDeleted=true;fixture.Db.Appointments.Add(deleted);
        await fixture.Db.SaveChangesAsync();
        fixture.Db.Remove(deleted); await fixture.Db.SaveChangesAsync();
        var report=await fixture.Sender.Send(new GetAdminDashboardQuery());
        Assert.Equal(2,report.Today);Assert.Equal(1,report.Pending);Assert.Equal(1,report.Customers);Assert.Equal(5,report.Guests);
        Assert.Equal(650m,report.EstimatedMonthlyRevenue);Assert.Equal(2,Assert.Single(report.Distribution).Count);
    }
}
