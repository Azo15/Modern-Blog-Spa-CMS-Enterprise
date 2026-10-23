using System.Globalization;
using System.Security.Cryptography;
using BusinessLayer.Abstract;
using BusinessLayer.CQRS.Commands;
using BusinessLayer.CQRS.Queries;
using BusinessLayer.Dtos;
using BusinessLayer.Security;
using DataAccesLayer.Abstract;
using DataAccesLayer.Concrete;
using EntityLayer.Concrete;
using EntityLayer.Enum;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BusinessLayer.CQRS.Handlers;

public sealed class CreateAppointmentHandler(AppDbContext db, IAppointmentEmailQueue queue,
    IManagerNotifier notifier, ILogger<CreateAppointmentHandler> logger, ICurrentUserService currentUser, INotificationService notifications) : IRequestHandler<CreateAppointmentCommand, AppointmentReceiptDto>
{
    public async Task<AppointmentReceiptDto> Handle(CreateAppointmentCommand request, CancellationToken cancellationToken)
    {
        var existing = await db.Appointments.AsNoTracking().SingleOrDefaultAsync(x => x.ClientRequestId == request.ClientRequestId, cancellationToken);
        if (existing is not null)
        {
            if (existing.CreatedBy != currentUser.UserId) throw new ValidationException("Bu talep başka bir oturumda oluşturulmuş. Yeni bir talep başlatın.");
            return Replay(existing, request);
        }
        var service = await db.Services.AsNoTracking().Where(x => x.Id == request.ServiceId && x.IsPublish)
            .Select(x => new { x.Id, x.Title }).SingleOrDefaultAsync(cancellationToken);
        if (service is null) throw new ValidationException([new ValidationFailure(nameof(request.ServiceId), "Bu hizmet şu anda randevuya açık değil.")]);

        var appointment = new Appointment
        {
            ServiceId = service.Id, ServiceTitleSnapshot = service.Title ?? "Spa ritüeli",
            FullName = request.FullName.Trim(), Phone = request.Phone.Trim(), Email = request.Email.Trim(),
            AppointmentDate = request.AppointmentDate, TimeSlot = request.TimeSlot, GuestCount = request.GuestCount,
            GiftCode = string.IsNullOrWhiteSpace(request.GiftCode) ? null : request.GiftCode.Trim().ToUpperInvariant(),
            Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
            Status = AppointmentStatus.Pending, ReferenceCode = "#XSPA-" + Convert.ToHexString(RandomNumberGenerator.GetBytes(6)),
            ClientRequestId = request.ClientRequestId
        };
        var delivery = new AppointmentEmailDelivery { Appointment = appointment };
        db.Appointments.Add(appointment);
        db.AppointmentEmailDeliveries.Add(delivery);
        try { await db.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateException)
        {
            // The unique database index also protects simultaneous retries across application instances.
            var retry = await db.Appointments.AsNoTracking().SingleOrDefaultAsync(x => x.ClientRequestId == request.ClientRequestId, cancellationToken);
            if (retry is null) throw;
            if (retry.CreatedBy != currentUser.UserId) throw new ValidationException("Talep kimliği başka bir oturuma ait.");
            db.Entry(appointment).State = EntityState.Detached;
            db.Entry(delivery).State = EntityState.Detached;
            return Replay(retry, request);
        }

        try { await notifications.PublishAsync(new($"appointment:{appointment.Id}","Yeni randevu talebi","Yeni bir randevu talebi incelemenizi bekliyor.",EntityLayer.Enum.NotificationType.AppointmentRequest,appointment.Id),cancellationToken); }
        catch (Exception error) when (error is not OperationCanceledException) { logger.LogWarning("Appointment notification will be recovered ({ErrorType}).",error.GetType().Name); }
        try
        {
            var jobId = queue.Enqueue(delivery.Id);
            await db.AppointmentEmailDeliveries.Where(x => x.Id == delivery.Id).ExecuteUpdateAsync(update => update
                .SetProperty(x => x.HangfireJobId, jobId).SetProperty(x => x.LastEnqueuedAtUtc, DateTime.UtcNow), cancellationToken);
        }
        catch (Exception error) when (error is not OperationCanceledException)
        {
            logger.LogWarning(error, "Appointment {AppointmentId} saved; its durable receipt will be recovered.", appointment.Id);
        }

        try
        {
            await notifier.NotifyAsync("Yeni Rezervasyon", $"{appointment.FullName} - {appointment.ServiceTitleSnapshot} ({appointment.AppointmentDate.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture)} {appointment.TimeSlot})", cancellationToken);
        }
        catch (Exception error) when (error is not OperationCanceledException)
        {
            logger.LogWarning(error, "Appointment {AppointmentId} saved; live manager notification was unavailable.", appointment.Id);
        }
        return Receipt(appointment, true);
    }

    private static AppointmentReceiptDto Receipt(Appointment appointment, bool isNew) =>
        new(appointment.ReferenceCode, appointment.AppointmentDate, appointment.TimeSlot, appointment.Status, isNew);

    private static AppointmentReceiptDto Replay(Appointment existing, CreateAppointmentCommand request)
    {
        if (existing.ServiceId != request.ServiceId || existing.AppointmentDate != request.AppointmentDate ||
            existing.TimeSlot != request.TimeSlot || existing.GuestCount != request.GuestCount ||
            existing.FullName != request.FullName.Trim() || existing.Phone != request.Phone.Trim() ||
            !string.Equals(existing.Email, request.Email.Trim(), StringComparison.OrdinalIgnoreCase) ||
            (existing.Notes ?? "") != (request.Notes?.Trim() ?? "") ||
            (existing.GiftCode ?? "") != (request.GiftCode?.Trim().ToUpperInvariant() ?? ""))
            throw new ValidationException([new ValidationFailure(nameof(request.ClientRequestId),
                $"{existing.ReferenceCode} kodlu talebiniz zaten kaydedildi. Değişiklik için bu kodla ekibimize ulaşın; mevcut kayıt değiştirilmedi.")
                { ErrorCode = "AppointmentReplayConflict" }]);
        return Receipt(existing, false);
    }
}

public sealed class AppointmentAdminHandlers(AppDbContext db, ICurrentUserService currentUser, IAuthorizationService authorization, IAppointmentEmailQueue? queue = null, ILogger<AppointmentAdminHandlers>? logger = null)
    : IRequestHandler<GetAppointmentsQuery, PaginatedList<AppointmentAdminDto>>, IRequestHandler<ChangeAppointmentStatusCommand, bool>
{
    private async Task RequireManager()
    {
        if (!(await authorization.AuthorizeAsync(currentUser.User, AccessPolicies.ContentManager)).Succeeded)
            throw new UnauthorizedAccessException();
    }

    public async Task<PaginatedList<AppointmentAdminDto>> Handle(GetAppointmentsQuery request, CancellationToken cancellationToken)
    {
        await RequireManager();
        var query = db.Appointments.AsNoTracking();
        if (request.Status.HasValue) query = query.Where(x => x.Status == request.Status.Value);
        var count = await query.CountAsync(cancellationToken);
        var items = await query.OrderBy(x => x.Status).ThenBy(x => x.AppointmentDate).ThenBy(x => x.TimeSlot).ThenByDescending(x => x.Id)
            .Skip((request.PageIndex - 1) * request.PageSize).Take(request.PageSize)
            .Select(x => new AppointmentAdminDto(x.Id, x.ReferenceCode, x.FullName, x.Phone, x.Email,
                x.ServiceTitleSnapshot, x.AppointmentDate, x.TimeSlot, x.GuestCount, x.Notes, x.Status, x.CreatedDate, x.GiftCode))
            .ToListAsync(cancellationToken);
        return new(items, count, request.PageIndex, request.PageSize);
    }

    public async Task<bool> Handle(ChangeAppointmentStatusCommand request, CancellationToken cancellationToken)
    {
        await RequireManager();
        var appointment = await db.Appointments.SingleOrDefaultAsync(x => x.Id == request.Id, cancellationToken);
        if (appointment is null) return false;
        if (appointment.Status == request.Status) return true;
        if (appointment.Status == AppointmentStatus.Cancelled || request.Status == AppointmentStatus.Pending)
            throw new ValidationException([new ValidationFailure("Status", "İptal edilen randevu yeniden onaylanamaz. Yeni bir randevu talebi oluşturun.")]);
        appointment.Status = request.Status;
        AppointmentEmailDelivery? confirmation = null;
        if (request.Status == AppointmentStatus.Confirmed)
        {
            confirmation = await db.AppointmentEmailDeliveries.SingleOrDefaultAsync(d => d.AppointmentId == appointment.Id && d.IsConfirmation, cancellationToken);
            if (confirmation is null)
            {
                confirmation = new AppointmentEmailDelivery { Appointment = appointment, IsConfirmation = true };
                db.AppointmentEmailDeliveries.Add(confirmation);
            }
            else
            {
                // Reconfirming a rescheduled booking must not collide with the unique outbox index.
                confirmation.SentAtUtc = null; confirmation.LastEnqueuedAtUtc = null;
                confirmation.HangfireJobId = null; confirmation.LastError = null;
                confirmation.MessageId = $"{Guid.NewGuid():N}@xspa.local";
            }
        }
        // Status change and delivery intent commit in the same database transaction.
        await db.SaveChangesAsync(cancellationToken);
        if (confirmation is not null && queue is not null)
        {
            try
            {
                var job = queue.Enqueue(confirmation.Id);
                await db.AppointmentEmailDeliveries.Where(x => x.Id == confirmation.Id).ExecuteUpdateAsync(u => u
                    .SetProperty(x => x.HangfireJobId, job).SetProperty(x => x.LastEnqueuedAtUtc, DateTime.UtcNow), cancellationToken);
            }
            catch (Exception error) when (error is not OperationCanceledException)
            { logger?.LogWarning("Confirmation delivery {DeliveryId} awaits recovery: {ErrorType}", confirmation.Id, error.GetType().Name); }
        }
        return true;
    }
}
