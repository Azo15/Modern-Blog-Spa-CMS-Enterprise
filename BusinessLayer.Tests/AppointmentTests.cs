using System.Reflection;
using BusinessLayer.Abstract;
using BusinessLayer.Container;
using BusinessLayer.CQRS.Commands;
using BusinessLayer.CQRS.Queries;
using BusinessLayer.Jobs;
using BusinessLayer.Security;
using BusinessLayer.ValidationRules;
using DataAccesLayer.Abstract;
using DataAccesLayer.Concrete;
using EntityLayer.Concrete;
using EntityLayer.Enum;
using FluentValidation;
using Hangfire;
using MediatR;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace BusinessLayer.Tests;

public sealed class AppointmentTests
{
    private sealed class Fixture : IDisposable
    {
        private readonly SqliteConnection connection = new("Data Source=:memory:");
        private readonly ServiceProvider provider;
        private readonly IServiceScope scope;
        public AppDbContext Db { get; }
        public ISender Sender { get; }
        public Stage2Fixture.TestUser User { get; } = new();
        public Mock<IAppointmentEmailQueue> Queue { get; } = new();
        public Mock<IAppointmentMailService> Mail { get; } = new();
        public Mock<IManagerNotifier> Notifier { get; } = new();

        public Fixture()
        {
            connection.Open();
            var services = new ServiceCollection().AddLogging();
            services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
            services.AddSingleton<ICurrentUserService>(User);
            services.AddDbContext<AppDbContext>(options => options.UseSqlite(connection));
            services.AddAuthorization(AccessPolicies.Configure);
            services.ContainerDependencies();
            Queue.Setup(queue => queue.Enqueue(It.IsAny<int>())).Returns("test-appointment-job");
            Mail.Setup(mail => mail.SendReceiptAsync(It.IsAny<Appointment>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            Notifier.Setup(notifier => notifier.NotifyAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            services.AddSingleton(Queue.Object);
            services.AddSingleton(Notifier.Object);
            provider = services.BuildServiceProvider();
            scope = provider.CreateScope();
            Db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            Sender = scope.ServiceProvider.GetRequiredService<ISender>();
            Db.Database.EnsureCreated();
            Db.Services.AddRange(new Service { Id = 1, Title = "İsveç Masajı", IsPublish = true },
                new Service { Id = 2, Title = "Unpublished ritual", IsPublish = false },
                new Service { Id = 3, Title = "Deleted ritual", IsPublish = true });
            Db.SaveChanges();
            Db.Remove(Db.Services.Single(service => service.Id == 3));
            Db.SaveChanges(); Db.ChangeTracker.Clear();
            As(null);
        }
        public void As(string? role) => User.User = AuthorizationTests.Principal(role, "1", authenticated: role is not null);
        public AppointmentEmailJob Job() => new(Db, Mail.Object, NullLogger<AppointmentEmailJob>.Instance);
        public AppointmentEmailRecoveryJob Recovery() => new(Db, Queue.Object, NullLogger<AppointmentEmailRecoveryJob>.Instance);
        public void Dispose() { scope.Dispose(); provider.Dispose(); connection.Dispose(); }
    }

    private static CreateAppointmentCommand Request() => new(1, DateOnly.FromDateTime(AppointmentSchedule.LocalNow).AddDays(1),
        "14:00", 1, "İsmail Şahin", "+90 538 878 70 00", "guest@example.test", "Hassas cilt", Guid.NewGuid());

    [Theory]
    [InlineData(1)] [InlineData(2)] [InlineData(3)] [InlineData(4)]
    public async Task AnonymousGuestCanRequestPublishedServiceForOneToFourGuests(int guests)
    {
        using var fixture = new Fixture();
        var receipt = await fixture.Sender.Send(Request() with { GuestCount = guests });
        Assert.True(receipt.IsNew);
        Assert.Matches("^#XSPA-[A-F0-9]{12}$", receipt.ReferenceCode);
        Assert.Equal(AppointmentStatus.Pending, receipt.Status);
        var appointment = Assert.Single(await fixture.Db.Appointments.AsNoTracking().ToListAsync());
        Assert.Equal(guests, appointment.GuestCount);
        Assert.Null(appointment.CreatedBy);
        Assert.Equal("İsveç Masajı", appointment.ServiceTitleSnapshot);
        Assert.False(appointment.IsDeleted);
    }

    [Theory]
    [InlineData("past")] [InlineData("too-far")] [InlineData("slot")] [InlineData("email")]
    [InlineData("phone")] [InlineData("guest-zero")] [InlineData("guest-five")]
    [InlineData("request-id")] [InlineData("name")] [InlineData("trim-name")] [InlineData("notes")] [InlineData("service")]
    public async Task PipelineRejectsInvalidRequestsBeforeAnyPersistenceOrExternalWork(string invalid)
    {
        using var fixture = new Fixture();
        var request = Request();
        request = invalid switch
        {
            "past" => request with { AppointmentDate = DateOnly.FromDateTime(AppointmentSchedule.LocalNow).AddDays(-1) },
            "too-far" => request with { AppointmentDate = DateOnly.FromDateTime(AppointmentSchedule.LocalNow).AddDays(91) },
            "slot" => request with { TimeSlot = "03:00" }, "email" => request with { Email = "invalid" },
            "phone" => request with { Phone = "<script>" }, "guest-zero" => request with { GuestCount = 0 },
            "guest-five" => request with { GuestCount = 5 }, "request-id" => request with { ClientRequestId = Guid.Empty },
            "name" => request with { FullName = "A" }, "trim-name" => request with { FullName = " A " },
            "notes" => request with { Notes = new string('n', 2001) },
            _ => request with { ServiceId = 0 }
        };
        await Assert.ThrowsAsync<ValidationException>(() => fixture.Sender.Send(request));
        Assert.Empty(await fixture.Db.Appointments.ToListAsync());
        Assert.Empty(await fixture.Db.AppointmentEmailDeliveries.ToListAsync());
        fixture.Queue.VerifyNoOtherCalls(); fixture.Notifier.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(2)] [InlineData(3)] [InlineData(999)]
    public async Task UnpublishedDeletedAndMissingServicesCannotReceiveReservations(int serviceId)
    {
        using var fixture = new Fixture();
        await Assert.ThrowsAsync<ValidationException>(() => fixture.Sender.Send(Request() with { ServiceId = serviceId }));
        Assert.Empty(await fixture.Db.Appointments.ToListAsync());
        fixture.Queue.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task AppointmentAndOutboxAreCommittedBeforeEnqueueAndManagerNotification()
    {
        using var fixture = new Fixture();
        fixture.Queue.Setup(queue => queue.Enqueue(It.IsAny<int>())).Callback<int>(id =>
        {
            Assert.Equal(1, fixture.Db.Appointments.Count());
            Assert.True(fixture.Db.AppointmentEmailDeliveries.Any(delivery => delivery.Id == id));
        }).Returns("committed-appointment-job");
        await fixture.Sender.Send(Request());
        var delivery = await fixture.Db.AppointmentEmailDeliveries.AsNoTracking().SingleAsync();
        Assert.Equal("committed-appointment-job", delivery.HangfireJobId);
        Assert.NotNull(delivery.LastEnqueuedAtUtc);
        fixture.Notifier.Verify(notifier => notifier.NotifyAsync("Yeni Rezervasyon", It.Is<string>(message => message.Contains("İsmail Şahin") && message.Contains("14:00")), It.IsAny<CancellationToken>()), Times.Once);
        fixture.Mail.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task QueueOutagePreservesRequestAndDurableEmailAndRecoveryCanEnqueueLater()
    {
        using var fixture = new Fixture();
        fixture.Queue.Setup(queue => queue.Enqueue(It.IsAny<int>())).Throws(new IOException("Queue down"));
        var receipt = await fixture.Sender.Send(Request());
        Assert.True(receipt.IsNew);
        Assert.Single(await fixture.Db.Appointments.ToListAsync());
        var pending = Assert.Single(await fixture.Db.AppointmentEmailDeliveries.AsNoTracking().ToListAsync());
        Assert.Null(pending.HangfireJobId); Assert.Null(pending.SentAtUtc);
        fixture.Queue.Setup(queue => queue.Enqueue(It.IsAny<int>())).Returns("recovered-appointment-job");
        await fixture.Recovery().RecoverAsync(CancellationToken.None);
        Assert.Equal("recovered-appointment-job", (await fixture.Db.AppointmentEmailDeliveries.AsNoTracking().SingleAsync()).HangfireJobId);
    }

    [Fact]
    public async Task RetriedBrowserRequestHasOneReferenceOneOutboxAndOneNotification()
    {
        using var fixture = new Fixture();
        var request = Request();
        var first = await fixture.Sender.Send(request);
        var retry = await fixture.Sender.Send(request);
        Assert.True(first.IsNew); Assert.False(retry.IsNew);
        Assert.Equal(first.ReferenceCode, retry.ReferenceCode);
        Assert.Single(await fixture.Db.Appointments.ToListAsync());
        Assert.Single(await fixture.Db.AppointmentEmailDeliveries.ToListAsync());
        fixture.Queue.Verify(queue => queue.Enqueue(It.IsAny<int>()), Times.Once);
        fixture.Notifier.Verify(notifier => notifier.NotifyAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("notes")] [InlineData("guests")] [InlineData("date")]
    public async Task ReusingRequestIdWithChangedDataRejectsReplayWithoutChangingOriginal(string changedField)
    {
        using var fixture = new Fixture();
        var original = Request();
        var receipt = await fixture.Sender.Send(original);
        var changed = changedField switch
        {
            "notes" => original with { Notes = "A different health request" },
            "guests" => original with { GuestCount = 4 },
            _ => original with { AppointmentDate = original.AppointmentDate.AddDays(1) }
        };

        var error = await Assert.ThrowsAsync<ValidationException>(() => fixture.Sender.Send(changed));
        Assert.Contains(error.Errors, failure => failure.ErrorCode == "AppointmentReplayConflict");
        var saved = Assert.Single(await fixture.Db.Appointments.AsNoTracking().ToListAsync());
        Assert.Equal(receipt.ReferenceCode, saved.ReferenceCode);
        Assert.Equal(original.AppointmentDate, saved.AppointmentDate);
        Assert.Equal(original.GuestCount, saved.GuestCount);
        Assert.Equal(original.Notes, saved.Notes);
        Assert.Single(await fixture.Db.AppointmentEmailDeliveries.ToListAsync());
        fixture.Queue.Verify(queue => queue.Enqueue(It.IsAny<int>()), Times.Once);
        fixture.Notifier.Verify(notifier => notifier.NotifyAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RelationalConstraintsRejectDuplicateReferenceRequestAndOutbox()
    {
        using var fixture = new Fixture();
        var request = Request(); await fixture.Sender.Send(request);
        fixture.Db.ChangeTracker.Clear();
        var existing = await fixture.Db.Appointments.AsNoTracking().SingleAsync();
        foreach (var conflict in new[] { "request", "reference" })
        {
            fixture.Db.Appointments.Add(new Appointment { FullName = "Another guest", Phone = "05388787000", Email = "another@example.test", ServiceId = 1,
                ServiceTitleSnapshot = "İsveç Masajı", AppointmentDate = request.AppointmentDate, TimeSlot = "14:00",
                ClientRequestId = conflict == "request" ? existing.ClientRequestId : Guid.NewGuid(), ReferenceCode = conflict == "reference" ? existing.ReferenceCode : "#XSPA-ANOTHER" });
            await Assert.ThrowsAsync<DbUpdateException>(() => fixture.Db.SaveChangesAsync());
            fixture.Db.ChangeTracker.Clear();
        }
        fixture.Db.AppointmentEmailDeliveries.Add(new AppointmentEmailDelivery { AppointmentId = existing.Id });
        await Assert.ThrowsAsync<DbUpdateException>(() => fixture.Db.SaveChangesAsync());
    }

    [Theory]
    [InlineData(AppRoles.Admin, true)] [InlineData(AppRoles.Staff, true)]

    [InlineData(AppRoles.Customer, false)] [InlineData(null, false)]
    public async Task OnlyAdminAndContentManagerCanReadOrModerateAppointments(string? role, bool allowed)
    {
        using var fixture = new Fixture();
        await fixture.Sender.Send(Request()); fixture.As(role);
        if (allowed)
        {
            Assert.Single((await fixture.Sender.Send(new GetAppointmentsQuery())).Items);
            Assert.True(await fixture.Sender.Send(new ChangeAppointmentStatusCommand(1, AppointmentStatus.Confirmed)));
        }
        else
        {
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => fixture.Sender.Send(new GetAppointmentsQuery()));
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => fixture.Sender.Send(new ChangeAppointmentStatusCommand(1, AppointmentStatus.Confirmed)));
            Assert.Equal(AppointmentStatus.Pending, (await fixture.Db.Appointments.SingleAsync()).Status);
        }
    }

    [Fact]
    public async Task AdminStatusTransitionsAreIdempotentAndCancelledRequestCannotBeReopened()
    {
        using var fixture = new Fixture(); await fixture.Sender.Send(Request()); fixture.As(AppRoles.Admin);
        Assert.True(await fixture.Sender.Send(new ChangeAppointmentStatusCommand(1, AppointmentStatus.Confirmed)));
        Assert.True(await fixture.Sender.Send(new ChangeAppointmentStatusCommand(1, AppointmentStatus.Confirmed)));
        Assert.True(await fixture.Sender.Send(new ChangeAppointmentStatusCommand(1, AppointmentStatus.Cancelled)));
        await Assert.ThrowsAsync<ValidationException>(() => fixture.Sender.Send(new ChangeAppointmentStatusCommand(1, AppointmentStatus.Confirmed)));
        await Assert.ThrowsAsync<ValidationException>(() => fixture.Sender.Send(new ChangeAppointmentStatusCommand(1, AppointmentStatus.Pending)));
        Assert.False(await fixture.Sender.Send(new ChangeAppointmentStatusCommand(999, AppointmentStatus.Cancelled)));
    }

    [Fact]
    public async Task ManagerQueriesUseStatusFilterAndPagination()
    {
        using var fixture = new Fixture();
        for (var index = 0; index < 5; index++) await fixture.Sender.Send(Request());
        fixture.As(AppRoles.Staff);
        await fixture.Sender.Send(new ChangeAppointmentStatusCommand(1, AppointmentStatus.Confirmed));
        var pending = await fixture.Sender.Send(new GetAppointmentsQuery(AppointmentStatus.Pending, 2, 2));
        Assert.Equal(4, pending.TotalCount); Assert.Equal(2, pending.Items.Count); Assert.True(pending.HasPreviousPage); Assert.False(pending.HasNextPage);
        Assert.Single((await fixture.Sender.Send(new GetAppointmentsQuery(AppointmentStatus.Confirmed))).Items);
        await Assert.ThrowsAsync<ValidationException>(() => fixture.Sender.Send(new GetAppointmentsQuery(PageIndex: 0)));
    }

    [Fact]
    public async Task SuccessfulEmailIsIdempotentWithStableMessageId()
    {
        using var fixture = new Fixture(); await fixture.Sender.Send(Request()); fixture.Db.ChangeTracker.Clear();
        var delivery = await fixture.Db.AppointmentEmailDeliveries.SingleAsync();
        await fixture.Job().SendAsync(delivery.Id, CancellationToken.None);
        await fixture.Job().SendAsync(delivery.Id, CancellationToken.None);
        fixture.Mail.Verify(mail => mail.SendReceiptAsync(It.IsAny<Appointment>(), delivery.MessageId, It.IsAny<CancellationToken>()), Times.Once);
        Assert.NotNull((await fixture.Db.AppointmentEmailDeliveries.AsNoTracking().SingleAsync()).SentAtUtc);
    }

    [Fact]
    public async Task SmtpFailureKeepsAppointmentAndReleasesLeaseForAutomaticRetry()
    {
        using var fixture = new Fixture(); await fixture.Sender.Send(Request()); fixture.Db.ChangeTracker.Clear();
        var delivery = await fixture.Db.AppointmentEmailDeliveries.SingleAsync();
        fixture.Mail.Setup(mail => mail.SendReceiptAsync(It.IsAny<Appointment>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ThrowsAsync(new IOException("SMTP unavailable"));
        await Assert.ThrowsAsync<IOException>(() => fixture.Job().SendAsync(delivery.Id, CancellationToken.None));
        var stored = await fixture.Db.AppointmentEmailDeliveries.AsNoTracking().SingleAsync();
        Assert.Null(stored.SentAtUtc); Assert.Null(stored.LeaseToken); Assert.Equal(1, stored.AttemptCount); Assert.Equal("IOException", stored.LastError);
        Assert.Single(await fixture.Db.Appointments.ToListAsync());
        Assert.Equal(3, typeof(AppointmentEmailJob).GetMethod(nameof(AppointmentEmailJob.SendAsync))!.GetCustomAttribute<AutomaticRetryAttribute>()!.Attempts);
    }

    [Fact]
    public async Task ActiveDeliveryLeasePreventsDuplicateEmail()
    {
        using var fixture = new Fixture(); await fixture.Sender.Send(Request()); fixture.Db.ChangeTracker.Clear();
        var delivery = await fixture.Db.AppointmentEmailDeliveries.SingleAsync();
        delivery.LeaseToken = "other-worker"; delivery.LeaseExpiresAtUtc = DateTime.UtcNow.AddMinutes(2);
        await fixture.Db.SaveChangesAsync(); fixture.Db.ChangeTracker.Clear();
        await fixture.Job().SendAsync(delivery.Id, CancellationToken.None);
        fixture.Mail.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GiftCodeIsNormalizedPersistedAndIncludedInReplayProtection()
    {
        using var fixture = new Fixture();
        var request = Request() with { GiftCode = "vip-2026" };
        await fixture.Sender.Send(request);
        Assert.Equal("VIP-2026", (await fixture.Db.Appointments.AsNoTracking().SingleAsync()).GiftCode);
        Assert.False((await fixture.Sender.Send(request with { GiftCode = "VIP-2026" })).IsNew);
        await Assert.ThrowsAsync<ValidationException>(() => fixture.Sender.Send(request with { GiftCode = "OTHER" }));
    }

    [Theory]
    [InlineData("<script>")] [InlineData("ab")] [InlineData("VIP CODE")]
    public async Task InvalidGiftCodeCannotCreateAppointment(string code)
    {
        using var fixture = new Fixture();
        await Assert.ThrowsAsync<ValidationException>(() => fixture.Sender.Send(Request() with { GiftCode = code }));
        Assert.Empty(await fixture.Db.Appointments.ToListAsync());
    }

    [Fact]
    public async Task ConfirmationIsDurableAndRepeatedApprovalDoesNotDuplicateIt()
    {
        using var fixture = new Fixture();
        await fixture.Sender.Send(Request()); fixture.As(AppRoles.Admin);
        await fixture.Sender.Send(new ChangeAppointmentStatusCommand(1, AppointmentStatus.Confirmed));
        await fixture.Sender.Send(new ChangeAppointmentStatusCommand(1, AppointmentStatus.Confirmed));
        Assert.Equal(2, await fixture.Db.AppointmentEmailDeliveries.CountAsync());
        Assert.Single(await fixture.Db.AppointmentEmailDeliveries.Where(x => x.IsConfirmation).ToListAsync());
    }

    [Fact]
    public async Task CancelledAppointmentNeverSendsQueuedConfirmation()
    {
        using var fixture = new Fixture();
        await fixture.Sender.Send(Request()); fixture.As(AppRoles.Admin);
        await fixture.Sender.Send(new ChangeAppointmentStatusCommand(1, AppointmentStatus.Confirmed));
        var id = await fixture.Db.AppointmentEmailDeliveries.Where(x => x.IsConfirmation).Select(x => x.Id).SingleAsync();
        await fixture.Sender.Send(new ChangeAppointmentStatusCommand(1, AppointmentStatus.Cancelled));
        await fixture.Job().SendAsync(id, CancellationToken.None);
        fixture.Mail.Verify(x => x.SendReceiptAsync(It.IsAny<Appointment>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
