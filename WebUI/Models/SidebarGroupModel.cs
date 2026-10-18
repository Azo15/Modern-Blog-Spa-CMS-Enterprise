namespace WebUI.Models;
public sealed record SidebarLink(string Controller,string Action,string Label,bool Ai=false)
{
    public string Icon => Ai ? "fa-wand-magic-sparkles" : Label switch
    {
        "İşletme Özeti"=>"fa-chart-pie", "Randevu Yönetimi"=>"fa-calendar-days", "Kullanıcılar"=>"fa-users",
        "Bloglar"=>"fa-book-open", "Hizmetler"=>"fa-spa", "Galeri"=>"fa-images", "Slider"=>"fa-sliders",
        "Mesajlar"=>"fa-envelope-open-text", "SMTP Ayarları"=>"fa-paper-plane", "Site Ayarları"=>"fa-gear",
        "Rol Yönetimi"=>"fa-user-shield", "Hesap Engelleme"=>"fa-user-lock", "Çöp Kutusu"=>"fa-trash-can",
        "Kategoriler"=>"fa-tags", "Yorum Moderasyonu"=>"fa-comments", "İletişim Bilgileri"=>"fa-address-card",
        "Hakkımızda"=>"fa-leaf", _=>"fa-layer-group"
    };
}
public sealed record SidebarGroupModel(string Id,string Title,string Icon,IReadOnlyList<SidebarLink> Links);
