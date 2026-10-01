import docx
from docx.shared import Inches, Pt, RGBColor
from docx.enum.text import WD_ALIGN_PARAGRAPH
from docx.enum.table import WD_TABLE_ALIGNMENT, WD_ALIGN_VERTICAL
from docx.oxml import OxmlElement
from docx.oxml.ns import qn

def set_cell_background(cell, fill_hex):
    tcPr = cell._tc.get_or_add_tcPr()
    shd = OxmlElement('w:shd')
    shd.set(qn('w:val'), 'clear')
    shd.set(qn('w:color'), 'auto')
    shd.set(qn('w:fill'), fill_hex)
    tcPr.append(shd)

def set_cell_margins(cell, top=100, bottom=100, left=150, right=150):
    tcPr = cell._tc.get_or_add_tcPr()
    tcMar = OxmlElement('w:tcMar')
    for m, val in [('top', top), ('bottom', bottom), ('left', left), ('right', right)]:
        node = OxmlElement(f'w:{m}')
        node.set(qn('w:w'), str(val))
        node.set(qn('w:type'), 'dxa')
        tcMar.append(node)
    tcPr.append(tcMar)

def create_document():
    doc = docx.Document()
    
    # Page Margins
    for section in doc.sections:
        section.top_margin = Inches(1)
        section.bottom_margin = Inches(1)
        section.left_margin = Inches(1)
        section.right_margin = Inches(1)
        
    # Styles Setup
    normal_style = doc.styles['Normal']
    normal_style.font.name = 'Calibri'
    normal_style.font.size = Pt(11)
    normal_style.font.color.rgb = RGBColor(51, 51, 51)
    
    # Primary & Secondary Colors
    COLOR_NAVY = RGBColor(27, 54, 93)      # #1B365D - Main Titles
    COLOR_STEEL = RGBColor(53, 94, 148)    # #355E94 - H2 Subtitles
    COLOR_DARK = RGBColor(33, 37, 41)      # Body text
    COLOR_TEAL = RGBColor(0, 128, 128)     # Accent / Highlights
    
    # Title
    p_title = doc.add_paragraph()
    p_title.alignment = WD_ALIGN_PARAGRAPH.CENTER
    run_title = p_title.add_run("STAJ BİTİRME PROJESİ\nGELİŞİM VE DÖNÜŞÜM RENGİ SİSTEM ŞARTNAMESİ")
    run_title.font.name = 'Calibri'
    run_title.font.size = Pt(22)
    run_title.font.bold = True
    run_title.font.color.rgb = COLOR_NAVY
    p_title.paragraph_format.space_after = Pt(4)
    
    # Subtitle
    p_sub = doc.add_paragraph()
    p_sub.alignment = WD_ALIGN_PARAGRAPH.CENTER
    run_sub = p_sub.add_run("Modern Blog & CMS Web Uygulamasının Enterprise Seviyeye Taşınması İçin Mevcut Durum, Hedeflenen Durum, Yeni Rol Hiyerarşisi ve Teknik Özellikler Raporu")
    run_sub.font.size = Pt(12)
    run_sub.font.italic = True
    run_sub.font.color.rgb = COLOR_STEEL
    p_sub.paragraph_format.space_after = Pt(20)
    
    # Metadata Table
    table_meta = doc.add_table(rows=4, cols=2)
    table_meta.alignment = WD_TABLE_ALIGNMENT.CENTER
    meta_data = [
        ("Proje Adı:", "Modern Blog / Spa Merkezi CMS (.NET 9.0 N-Tier Architecture)"),
        ("Rapor Hazırlayan:", "Senior Software Engineer & Software Architecture Consultant"),
        ("Tarih:", "29 Eylül 2026"),
        ("Doküman Statüsü:", "Staj Bitirme Projesi Teknik Şartnamesi (Teknik Onaylı)")
    ]
    for i, (k, v) in enumerate(meta_data):
        row = table_meta.rows[i]
        c0, c1 = row.cells[0], row.cells[1]
        c0.paragraphs[0].add_run(k).bold = True
        c0.paragraphs[0].runs[0].font.color.rgb = COLOR_NAVY
        c1.paragraphs[0].add_run(v)
        set_cell_background(c0, "F0F4F8")
        set_cell_background(c1, "FAFAFA")
        set_cell_margins(c0, 80, 80, 120, 120)
        set_cell_margins(c1, 80, 80, 120, 120)
        
    doc.add_paragraph().paragraph_format.space_after = Pt(15)
    
    # Helper functions for sections
    def add_h1(text):
        h = doc.add_paragraph()
        run = h.add_run(text)
        run.font.size = Pt(16)
        run.font.bold = True
        run.font.color.rgb = COLOR_NAVY
        h.paragraph_format.space_before = Pt(18)
        h.paragraph_format.space_after = Pt(8)
        return h

    def add_h2(text):
        h = doc.add_paragraph()
        run = h.add_run(text)
        run.font.size = Pt(13)
        run.font.bold = True
        run.font.color.rgb = COLOR_STEEL
        h.paragraph_format.space_before = Pt(12)
        h.paragraph_format.space_after = Pt(6)
        return h

    def add_p(text, bold_prefix=None):
        p = doc.add_paragraph()
        p.paragraph_format.space_after = Pt(6)
        p.paragraph_format.line_spacing = 1.15
        if bold_prefix:
            r_pre = p.add_run(bold_prefix)
            r_pre.bold = True
            r_pre.font.color.rgb = COLOR_NAVY
        p.add_run(text)
        return p

    def add_bullet(text, bold_prefix=None):
        p = doc.add_paragraph(style='List Bullet')
        p.paragraph_format.space_after = Pt(4)
        p.paragraph_format.line_spacing = 1.15
        if bold_prefix:
            r_pre = p.add_run(bold_prefix)
            r_pre.bold = True
            r_pre.font.color.rgb = COLOR_STEEL
        p.add_run(text)
        return p

    # --- BÖLÜM 1: MEVCUT DURUM ANALİZİ (AS-IS) ---
    add_h1("1. MEVCUT DURUM ANALİZİ (AS-IS STATUS)")
    add_p("Bu bölümde, projenin mevcut kod altyapısı, kullanılan teknolojiler, mimari yapı ve bu yapının sınırlılıkları detaylandırılmıştır.")
    
    add_h2("1.1. Mevcut Teknolojik Altyapı ve Mimari")
    add_bullet(".NET 9.0 ASP.NET Core MVC framework'ü üzerine inşa edilmiştir.", "Framework: ")
    add_bullet("EntityLayer, DataAccesLayer, BusinessLayer ve WebUI olmak üzere 4 temel katmandan oluşur.", "Mimari Desen: ")
    add_bullet("SQL Server Express LocalDB ve Entity Framework Core (Code-First yaklaşımı) kullanılmaktadır.", "Veritabanı & ORM: ")
    add_bullet("ASP.NET Core Identity (AppUser, IdentityRole<int>) ile temel kimlik doğrulama sağlanmaktadır.", "Kimlik Yönetimi: ")
    add_bullet("AutoMapper (Nesne Dönüşümleri) ve FluentValidation (Doğrulama Kuralları) entegredir.", "Kütüphaneler: ")
    add_bullet("About, AboutCard, AppUser, Blog, Catagory, Contact, ContactForm, Galary, Service, SiteTable, Slider.", "Mevcut Entity'ler: ")

    add_h2("1.2. Mevcut Durumun Güçlü Yönleri")
    add_bullet("Katmanların sorumluluklarının ayrılmış olması kodun okunabilirliğini ve yönetimini kolaylaştırmaktadır.")
    add_bullet("EF Core Migrations ve DataSeeder yapısı sayesinde veritabanı kurulumu ve örnek veriler otomatize edilmiştir.")
    add_bullet("FluentValidation ile form girdileri iş kuralı katmanında doğrulanmaktadır.")

    add_h2("1.3. Mevcut Durumun Sınırlılıkları ve Eksiklikleri")
    add_bullet("Yetkilendirme mekanizması yalnızca temel düzeydedir. Tek tip Admin rolü bulunmakta, ince taneli (fine-grained) rol ve izin hiyerarşisi yoktur.", "Yetkilendirme Eksikliği: ")
    add_bullet("Veriler veritabanından fiziksel olarak silinmektedir (Hard Delete). Silme işlemlerinde geri alma imkanı ve audit (iz sürme) kaydı yoktur.", "Veri Güvenliği Eksikliği: ")
    add_bullet("Tüm veri istekleri doğrudan veritabanına atılmaktadır. Sık erişilen veriler (menüler, site ayarları, popüler bloglar) için Caching mekanizması yoktur.", "Performans Eksikliği: ")
    add_bullet("Loglar sadece konsola yazılmaktadır. Hataların merkezi takibi ve dosya/veritabanı loglaması mevcut değildir.", "İzlenebilirlik Eksikliği: ")
    add_bullet("Sistemde senkron işlemler ağırlıktadır. E-posta gönderimi gibi süreçler kullanıcıyı bekletmektedir.", "Asenkron Altyapı Eksikliği: ")
    add_bullet("Admin paneline anlık bildirim düşmesini sağlayan gerçek zamanlı (WebSocket/SignalR) iletişim altyapısı yoktur.", "Gerçek Zamanlı İletişim Eksikliği: ")
    add_bullet("Projede hiçbir Unit Test veya Integration Test katmanı bulunmamaktadır.", "Kalite & Test Eksikliği: ")
    add_bullet("Dış sistemlerin (mobil uygulama, SPA) veri tüketebileceği RESTful API veya Swagger dokümantasyonu yoktur.", "API Eksikliği: ")

    # --- BÖLÜM 2: HEDEFLENEN DURUM (TO-BE) ---
    add_h1("2. HEDEFLENEN DURUM (TO-BE STATUS)")
    add_p("Hedeflenen durum, projenin sadece bir web sitesi değil; enterprise seviyede güvenlik, performans, sürdürülebilirlik ve test edilebilirliğe sahip tam teşekküllü bir Kurumsal İçerik Yönetim Platformuna (Enterprise CMS) dönüştürülmesini amaçlar.")

    add_h2("2.1. Temel Hedefler ve Mimari Dönüşüm Vizyonu")
    add_bullet("İş mantığı ve veri sorgularının ayrılmasıyla daha temiz, sürdürülebilir ve test edilebilir bir yapı.", "Clean Architecture & CQRS: ")
    add_bullet("4 seviyeli Rol tabanlı (RBAC) ve İzin tabanlı (Claim-Based) dinamik yetkilendirme.", "Gelişmiş Rol Hiyerarşisi: ")
    add_bullet("Redis Distributed Caching ile veritabanı yükünün %70 oranında azaltılması ve sayfa açılış hızlarının artırılması.", "Yüksek Performans & Caching: ")
    add_bullet("SignalR entegrasyonu ile yeni mesaj, yorum ve sistem olaylarında Admin paneline anlık canlı bildirim düşmesi.", "Real-Time Bildirimler: ")
    add_bullet("Hangfire ile zamanlanmış görevler (bülten gönderimi, raporlama, geçici dosya temizliği) ve arka plan e-posta gönderimi.", "Asenkron İş Yönetimi: ")
    add_bullet("Verilerin kaybolmasını önleyen Soft Delete mekanizması ve veriyi kimin ne zaman değiştirdiğini kaydeden Audit Trail.", "Veri Bütünlüğü & Iz Sürme: ")
    add_bullet("Business katmanı için xUnit ve Moq ile en az %70 test kapsama oranı (Code Coverage).", "Tam Test Kapsamı: ")
    add_bullet("Swagger UI destekli RESTful API katmanı ve Docker/Docker-Compose desteği.", "API & Konteynerizasyon: ")

    # --- BÖLÜM 3: YENİ EKLENECEK ROL VE YETKİLENDİRME MİMARİSİ ---
    add_h1("3. YENİ EKLENECEK ROL VE YETKİLENDİRME MİMARİSİ (RBAC)")
    add_p("Mevcut sistemdeki tekil Admin yapısı kaldırılarak, kurumsal ölçekte 6 farklı rol ve sorumluluk alanı (Role-Based Access Control) tanımlanacaktır.")

    table_roles = doc.add_table(rows=7, cols=3)
    table_roles.alignment = WD_TABLE_ALIGNMENT.CENTER
    headers = ["Rol Adı", "Kullanım Amacı / Tanım", "Erişim ve Yetki Kapsamı"]
    
    for j, h in enumerate(headers):
        cell = table_roles.rows[0].cells[j]
        cell.paragraphs[0].add_run(h).bold = True
        cell.paragraphs[0].runs[0].font.color.rgb = RGBColor(255, 255, 255)
        set_cell_background(cell, "1B365D")
        set_cell_margins(cell, 100, 100, 100, 100)

    roles_info = [
        ("System Admin (Süper Yönetici)", "Sistemin tam yetkili sahibidir.", "Rol ve Kullanıcı yönetimi, Sistem Ayarları, Log/Audit İnceleme, Veritabanı Yedekleme, Tüm İçerikleri Silme/Düzenleme."),
        ("Content Manager (İçerik Yöneticisi)", "Blog, Hizmet ve Galeri içerik süreçlerini yönetir.", "Tüm Blog, Hizmet, Galeri ekleme/güncelleme/silme, Kategori yönetimi, Yorum Onaylama/Reddetme yetkisi."),
        ("Author (Yazar)", "Blog yazısı üreten içerik yazarlarıdır.", "Kendi blog yazılarını yazma/taslak olarak kaydetme/güncelleme, kendi yazılarına gelen yorumları görüntüleme. Başkalarının yazısına müdahale edemez."),
        ("Moderator (Moderatör)", "Kullanıcı etkileşimi ve iletişimi denetler.", "Gelen iletişim formlarını inceleme/yanıtlama, Yorum denetimi (Spam/Küfür filtresi kontrolü), Kullanıcı engelini yönetme."),
        ("Registered User (Kayıtlı Üye)", "Siteye üye olmuş standart kullanıcılardır.", "Profil düzenleme, Bloglara yorum yapma, Yorumlarını güncelleme/silme, Blogları favorilere ekleme ve beğeni/tepki verme."),
        ("Visitor (Anonim Ziyaretçi)", "Siteyi ziyaret eden üye olmayan kullanıcılardır.", "Yayınlanmış blog, galeri ve hizmetleri görüntüleme, Arama yapma, İletişim formu doldurma.")
    ]

    for idx, data in enumerate(roles_info):
        row = table_roles.rows[idx + 1]
        for col_idx, text in enumerate(data):
            cell = row.cells[col_idx]
            cell.paragraphs[0].add_run(text)
            bg = "F0F4F8" if idx % 2 == 0 else "FFFFFF"
            set_cell_background(cell, bg)
            set_cell_margins(cell, 80, 80, 100, 100)

    doc.add_paragraph().paragraph_format.space_after = Pt(15)

    # --- BÖLÜM 4: DETAYLI YENİ ÖZELLİKLER VE TEKNİK ŞARTNAME ---
    add_h1("4. DETAYLI YENİ ÖZELLİKLER VE TEKNİK ŞARTNAME")
    add_p("Aşağıda projenin yeni sürümünde yer alacak 12 temel teknik özellik ve uygulama detayları listelenmiştir.")

    features = [
        ("Özellik 1: CQRS ve MediatR Katmanı Entegrasyonu",
         "Business katmanında karmaşıklaşan servis yapılarını ayrıştırmak için Command Query Responsibility Segregation (CQRS) uygulanacaktır. MediatR kütüphanesi entegre edilerek Controller'lar içerisindeki iş mantığı bağımlılığı kaldırılacaktır."),
        
        ("Özellik 2: Soft Delete ve Global Query Filter + Audit Trail",
         "Fiziksel silme yerine IsDeleted=true bayrağı çekilecektir. EF Core HasQueryFilter ile silinmiş veriler otomatik olarak sorgulardan gizlenecektir. Ayrıca her kayıtta CreatedDate, CreatedBy, UpdatedDate, UpdatedBy ve DeletedBy bilgileri saklanacaktır."),

        ("Özellik 3: SignalR Canlı Bildirim & Canlı İletişim Paneli",
         "ASP.NET Core SignalR kullanılarak WebSocket bağlantısı kurulacaktır. Ziyaretçi iletişim formu doldurduğunda veya yeni yorum yapıldığında Admin Dashboard paneline sayfa yenilenmeden sesli ve görsel anlık bildirim düşecektir."),

        ("Özellik 4: Redis Distributed Cache & Performans İyileştirmesi",
         "Sık erişilen Blog listeleri, Kategori bilgileri ve Site Ayarları Redis üzerinde önbelleklenecektir. Veri güncellendiğinde Cache Invalidated mekanizması ile önbellek otomatik tazelenecektir."),

        ("Özellik 5: Hangfire Arka Plan İş Yönetimi",
         "E-posta gönderimleri, haftalık özet bülteni dağıtımı ve sistem temizlik görevleri Hangfire aracılığıyla asenkron arka plan işleri (Background Jobs) olarak çalıştırılacaktır."),

        ("Özellik 6: Gelişmiş Çok Seviyeli (Nested) Yorum & Moderasyon Sistemi",
         "Blog yazıları altında kullanıcıların yorumlara yanıt verebileceği hiyerarşik (Parent-Child) yorum yapısı kurulacaktır. Yorumlar Admin/Moderatör onayına sunulacak, otomatik spam/küfür filtresinden geçecektir."),

        ("Özellik 7: Otomatik WebP Görsel Sıkıştırma & Cloud Storage",
         "Yüklenen tüm blog ve galeri görselleri SixLabors.ImageSharp kütüphanesi ile otomatik olarak WebP formatına dönüştürülecek, %60 oranında sıkıştırılacak ve Cloud Storage (AWS S3 veya Local Storage Abstraction) üzerinde saklanacaktır."),

        ("Özellik 8: Serilog & Merkezi Exception/Log Yönetimi",
         "Uygulama genelindeki tüm loglar Serilog ile yapılandırılacak; hatalar, güvenlik ihlalleri ve kritik işlemler dosyaya ve Seq / Elasticsearch ortamına JSON olarak kaydedilecektir. ExceptionHandlingMiddleware ile kullanıcıya dostça hata sayfaları gösterilecektir."),

        ("Özellik 9: Multi-Language / Çoklu Dil Desteği (i18n)",
         "ASP.NET Core IStringLocalizer altyapısı kullanılarak dinamik dil değiştirme (Türkçe/İngilizce) desteği eklenecektir. Menüler, sabit yazılar ve dinamik içerikler dil dosyalarından (Resource Files) okunacaktır."),

        ("Özellik 10: PDF & Excel Raporlama Modülü",
         "DinkToPdf ve EPPlus kütüphaneleri kullanılarak Admin panelindeki İletişim Formları, Kullanıcı Listeleri ve Blog İstatistikleri tek tıkla PDF veya Excel dosyası olarak indirilebilecektir."),

        ("Özellik 11: xUnit & Moq ile Birim (Unit) ve Entegrasyon Testleri",
         "Business katmanındaki iş kuralları ve yetkilendirme servisleri için xUnit, Moq ve FluentAssertions kullanılarak Unit Test projesi oluşturulacaktır."),

        ("Özellik 12: RESTful API Katmanı, Swagger & Docker Desteği",
         "Mobil ve SPA uygulamalarının veri tüketebilmesi için /api/v1/ controller'ları eklenecek, Swagger UI ile API dokümantasyonu sağlanacaktır. Proje Dockerfile ve docker-compose.yml ile konteynerize edilecektir.")
    ]

    for title, desc in features:
        add_h2(title)
        add_p(desc)

    # --- BÖLÜM 5: DÖNÜŞÜM VE UYGULAMA YOL HARİTASI ---
    add_h1("5. DÖNÜŞÜM VE UYGULAMA YOL HARİTASI (ROADMAP & TIMELINE)")
    add_p("Projenin staj süresi içerisinde tamamlanması için 6 haftalık, 3 aşamalı (Faz) uygulama takvimi planlanmıştır.")

    table_road = doc.add_table(rows=4, cols=3)
    table_road.alignment = WD_TABLE_ALIGNMENT.CENTER
    headers_r = ["Uygulama Fazı", "Zaman Aralığı", "Tamamlanacak Modüller & Çalışmalar"]
    
    for j, h in enumerate(headers_r):
        cell = table_road.rows[0].cells[j]
        cell.paragraphs[0].add_run(h).bold = True
        cell.paragraphs[0].runs[0].font.color.rgb = RGBColor(255, 255, 255)
        set_cell_background(cell, "355E94")
        set_cell_margins(cell, 100, 100, 100, 100)

    road_info = [
        ("FAZ 1: Temel Mimari & Güvenlik", "1. ve 2. Hafta", "Soft Delete & Audit Trail altyapısı, 6 Seviyeli Rol Mimarisi (RBAC) kurulumu, Rich Text Editor entegrasyonu, Serilog merkezi loglama."),
        ("FAZ 2: Performans & Real-Time", "3. ve 4. Hafta", "CQRS & MediatR dönüşümü, Redis Distributed Cache entegrasyonu, SignalR canlı bildirim altyapısı, Nested Yorum & Moderasyon sistemi."),
        ("FAZ 3: Enterprise & DevOps", "5. ve 6. Hafta", "Hangfire arka plan işleri, WebP görsel sıkıştırma, RESTful API & Swagger, xUnit Testleri (%70+ coverage) ve Docker/Docker-Compose kurulumu.")
    ]

    for idx, data in enumerate(road_info):
        row = table_road.rows[idx + 1]
        for col_idx, text in enumerate(data):
            cell = row.cells[col_idx]
            cell.paragraphs[0].add_run(text)
            bg = "F0F4F8" if idx % 2 == 0 else "FFFFFF"
            set_cell_background(cell, bg)
            set_cell_margins(cell, 80, 80, 100, 100)

    doc.add_paragraph().paragraph_format.space_after = Pt(15)

    # --- BÖLÜM 6: JÜRİ SUNUMU VE SONUÇ TAVSİYELERİ ---
    add_h1("6. JÜRİ SUNUMU VE SONUÇ TAVSİYELERİ")
    add_p("Staj bitirme projesi jürisine veya teknik mülakatçıya sunum yaparken şu hususlara vurgu yapılması başarı oranını artıracaktır:")
    add_bullet("Projenin sıradan bir CRUD uygulaması olmadığını, Clean Architecture ve CQRS prensiplerine uygun mimariyle geliştirildiğini vurgulayın.", "Mimari Vurgu: ")
    add_bullet("Redis Caching ve WebP görsel sıkıştırmasının sayfa yüklenme sürelerine etkisini grafiklerle gösterin.", "Performans Vurgu: ")
    add_bullet("SignalR ile canlı bildirim düşmesini sunum sırasında anlık olarak test ettirin (Telefon veya ikinci tarayıcıdan form doldurarak).", "Canlı İnovasyon: ")
    add_bullet("Yazılan xUnit Unit testlerini ve Code Coverage raporunu göstererek kod kalitesini kanıtlayın.", "Kalite Kanıtı: ")
    add_bullet("Projenin Docker ile tek komutla (docker-compose up) çalıştırılabilir olduğunu gösterin.", "DevOps Hazırlığı: ")

    doc.save(r"c:\Users\ismai\OneDrive\Pictures\2026_1.Term_Projects\Moden_Blog_Site\Moden_Blog_Site\Staj_Bitirme_Projesi_Gelisim_Raporu.docx")
    print("Document successfully created!")

if __name__ == "__main__":
    create_document()
