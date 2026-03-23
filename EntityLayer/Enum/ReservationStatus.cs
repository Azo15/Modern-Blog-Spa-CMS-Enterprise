namespace EntityLayer.Enum
{
    using System.ComponentModel.DataAnnotations;

    public enum ReservationStatus
    {
        [Display(Name = "Şoför Ataması Bekleniyor")]
        SoforBeklemede = 0,       // Admin henüz şoför atamadı

        [Display(Name = "Şoför Yolda")]
        SoforYolda = 1,           // Şoför yolda

        [Display(Name = "Şoför Aracı Aldı")]
        SoforAraciAldi = 2,       // Şoför aracı aldı

        [Display(Name = "Müşteri Onayı Bekleniyor")]
        MusteriOnayBeklemede = 3, // Şoför onayladı, müşteri onayı bekleniyor

        [Display(Name = "Müşteri Onayladı")]
        MusteriOnayladi = 4,      // Müşteri aracı onayladı, artık yolda

        [Display(Name = "Araç Teslim Edildi")]
        AracTeslimEdildi = 5      // Araç teslim edildi
    }

}
