using System.Globalization;
using System.Linq.Expressions;
using BusinessLayer.Abstract;
using BusinessLayer.CQRS.Commands;
using BusinessLayer.CQRS.Queries;
using BusinessLayer.Dtos;
using BusinessLayer.ValidationRules;
using DataAccesLayer.Concrete;
using EntityLayer.Concrete;
using EntityLayer.Enum;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BusinessLayer.CQRS.Handlers;

public sealed class MyAppointmentHandlers(AppDbContext db, IActiveMemberService members,
    IManagerNotifier notifier, ILogger<MyAppointmentHandlers> logger) :
    IRequestHandler<GetMyAppointmentsQuery, PaginatedList<MyAppointmentDto>>,
    IRequestHandler<GetMyAppointmentQuery, MyAppointmentDto?>,
    IRequestHandler<UpdateMyAppointmentCommand, bool>, IRequestHandler<CancelMyAppointmentCommand, bool>
{
    // CreatedBy is assigned from the authenticated session by AppDbContext and immutable on updates.
    // Email matching never grants ownership, including guest reservations and unverified emails.
    private async Task<IQueryable<Appointment>> Owned(CancellationToken ct)
    {
        var id = (await members.RequireAsync(ct)).ToString(CultureInfo.InvariantCulture);
        return db.Appointments.Where(a => a.CreatedBy == id);
    }
    private static readonly Expression<Func<Appointment, MyAppointmentDto>> Projection = a =>
        new(a.Id, a.ReferenceCode, a.ServiceId, a.ServiceTitleSnapshot, a.AppointmentDate,
            a.TimeSlot, a.GuestCount, a.Notes, a.Status, a.UpdatedDate);

    public async Task<PaginatedList<MyAppointmentDto>> Handle(GetMyAppointmentsQuery r, CancellationToken ct)
    {
        var query = (await Owned(ct)).AsNoTracking();
        var count = await query.CountAsync(ct);
        var items = await query.OrderByDescending(a => a.AppointmentDate).ThenByDescending(a => a.Id)
            .Skip((r.PageIndex - 1) * 10).Take(10).Select(Projection).ToListAsync(ct);
        return new(items, count, r.PageIndex, 10);
    }
    public async Task<MyAppointmentDto?> Handle(GetMyAppointmentQuery r, CancellationToken ct)
        => await (await Owned(ct)).AsNoTracking().Where(a => a.Id == r.Id).Select(Projection).SingleOrDefaultAsync(ct);

    public async Task<bool> Handle(UpdateMyAppointmentCommand r, CancellationToken ct)
    {
        var appointment = await (await Owned(ct)).SingleOrDefaultAsync(a => a.Id == r.Id, ct);
        if (appointment is null) return false;
        EnsureEditable(appointment, r.ExpectedUpdatedDate);
        var service = await db.Services.SingleOrDefaultAsync(s => s.Id == r.ServiceId && s.IsPublish, ct);
        if (service is null) throw new ValidationException("Bu hizmet şu anda randevuya açık değil.");
        appointment.ServiceId = service.Id; appointment.ServiceTitleSnapshot = service.Title ?? "Spa ritüeli";
        appointment.AppointmentDate = r.AppointmentDate; appointment.TimeSlot = r.TimeSlot;
        appointment.GuestCount = r.GuestCount; appointment.Notes = r.Notes?.Trim();
        appointment.Status = AppointmentStatus.Pending;
        await Save(ct);
        await Notify("Randevu değişikliği", appointment, ct);
        return true;
    }
    public async Task<bool> Handle(CancelMyAppointmentCommand r, CancellationToken ct)
    {
        var appointment = await (await Owned(ct)).SingleOrDefaultAsync(a => a.Id == r.Id, ct);
        if (appointment is null) return false;
        EnsureEditable(appointment, r.ExpectedUpdatedDate);
        appointment.Status = AppointmentStatus.Cancelled;
        await Save(ct);
        await Notify("Müşteri randevuyu iptal etti", appointment, ct);
        return true;
    }
    private static void EnsureEditable(Appointment a, DateTime? expected)
    {
        if (a.UpdatedDate != expected) throw new ValidationException("Randevu başka bir işlemle güncellendi. Sayfayı yenileyin.");
        if (a.Status == AppointmentStatus.Cancelled || !AppointmentSchedule.IsFutureSlot(a.AppointmentDate, a.TimeSlot))
            throw new ValidationException("Geçmiş veya iptal edilmiş randevu değiştirilemez. Yeni talep oluşturabilirsiniz.");
    }
    private async Task Save(CancellationToken ct)
    {
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException) { throw new ValidationException("Randevu aynı anda güncellendi. Sayfayı yenileyin."); }
    }
    private async Task Notify(string title, Appointment a, CancellationToken ct)
    {
        try { await notifier.NotifyAsync(title, $"{a.ReferenceCode} · {a.ServiceTitleSnapshot}", ct); }
        catch (Exception e) when (e is not OperationCanceledException)
        { logger.LogWarning("Appointment {Id} saved; notification failed: {ErrorType}", a.Id, e.GetType().Name); }
    }
}
