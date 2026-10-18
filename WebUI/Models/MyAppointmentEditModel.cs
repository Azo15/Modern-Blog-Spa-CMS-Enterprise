using System.ComponentModel.DataAnnotations;
using BusinessLayer.Dtos;
namespace WebUI.Models;
public sealed class MyAppointmentEditModel
{
    public int Id { get; set; }
    [Range(1,int.MaxValue)] public int ServiceId { get; set; }
    public DateOnly AppointmentDate { get; set; }
    [Required] public string TimeSlot { get; set; } = "";
    [Range(1,4)] public int GuestCount { get; set; }
    [StringLength(2000)] public string? Notes { get; set; }
    public DateTime? ExpectedUpdatedDate { get; set; }
    public IReadOnlyList<ReservationServiceDto> Services { get; set; } = [];
}
