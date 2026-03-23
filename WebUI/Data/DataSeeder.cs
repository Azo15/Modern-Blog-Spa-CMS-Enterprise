using DataAccesLayer.Concrete;
using EntityLayer.Concrete;
using Microsoft.AspNetCore.Identity;

namespace WebUI.Data
{
    public static class DataSeeder
    {
        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

            // ──────────────────── KULLANICI ────────────────────
            AppUser? seederUser = await userManager.FindByEmailAsync("admin@spa.com");
            if (seederUser == null)
            {
                seederUser = new AppUser
                {
                    UserName = "admin@spa.com",
                    Email = "admin@spa.com",
                    FirstName = "Spa",
                    LastName = "Admin",
                    EmailConfirmed = true
                };
                await userManager.CreateAsync(seederUser, "Admin123!");
            }

            // ──────────────────── KATEGORİLER ────────────────────
            if (!context.Catagories.Any())
            {
                var categories = new List<Catagory>
                {
                    new() { Title = "Cilt Bakımı",          IsPublish = true, CreatedDate = DateTime.Now },
                    new() { Title = "Masaj Teknikleri",     IsPublish = true, CreatedDate = DateTime.Now },
                    new() { Title = "Wellness & Meditasyon",IsPublish = true, CreatedDate = DateTime.Now },
                    new() { Title = "Aromaterapi",          IsPublish = true, CreatedDate = DateTime.Now },
                    new() { Title = "Beslenme & Detoks",    IsPublish = true, CreatedDate = DateTime.Now },
                    new() { Title = "Hamam & Sauna",        IsPublish = true, CreatedDate = DateTime.Now },
                    new() { Title = "Anti-Aging",           IsPublish = true, CreatedDate = DateTime.Now },
                    new() { Title = "Saç Bakımı",           IsPublish = true, CreatedDate = DateTime.Now },
                    new() { Title = "Yoga & Pilates",       IsPublish = true, CreatedDate = DateTime.Now },
                    new() { Title = "Genel Sağlık",         IsPublish = true, CreatedDate = DateTime.Now },
                };
                context.Catagories.AddRange(categories);
                await context.SaveChangesAsync();
            }

            var cats = context.Catagories.ToList();

            // ──────────────────── BLOGLAR ────────────────────
            if (!context.Blogs.Any())
            {
                var userId = seederUser.Id;
                var blogs = new List<Blog>
                {
                    // Cilt Bakımı (CatId 1)
                    new() { Title = "Kış Aylarında Cilt Neminin Önemi", SubTitle = "Soğuk havada cildinizi koruyun", Content = "<p>Kış aylarında hava nemi düşer ve cildiniz kurumaya başlar. Günde en az 2 litre su içmek ve nemlendirici kullanmak cildinizin sağlığını korur. Doğal zeytinyağlı kremler bu dönemde idealdir.</p>", Image = "blog/cilt-nem.jpg", CatagoryId = cats[0].Id, AppUserId = userId, IsPublish = true, CreatedDate = DateTime.Now.AddDays(-29) },
                    new() { Title = "C Vitamini Serumu Neden Kullanmalısınız?", SubTitle = "Parlaklık ve canlılık için", Content = "<p>C vitamini serumu cilt tonu eşitsizliklerini giderir, leke açar ve antioksidan koruması sağlar. Sabah rutininize ekleyin ve farkı haftalarca görün.</p>", Image = "blog/c-vitamini.jpg", CatagoryId = cats[0].Id, AppUserId = userId, IsPublish = true, CreatedDate = DateTime.Now.AddDays(-27) },
                    new() { Title = "Peeling Nedir ve Ne Zaman Yapılmalı?", SubTitle = "Hücre yenilemenin sırrı", Content = "<p>Kimyasal ya da mekanik peeling, ölü hücreleri uzaklaştırır ve cildi yeniler. Haftada 1-2 kez uygulamak yeterlidir; hassas ciltler için daha az sıklık önerilir.</p>", Image = "blog/peeling.jpg", CatagoryId = cats[0].Id, AppUserId = userId, IsPublish = true, CreatedDate = DateTime.Now.AddDays(-25) },

                    // Masaj Teknikleri (CatId 2)
                    new() { Title = "İsveç Masajı ve Faydaları", SubTitle = "Kasları gevşeten klasik teknik", Content = "<p>İsveç masajı, uzun ve kayıcı vuruşlarla yapılır. Kan dolaşımını iyileştirir, kas gerginliğini azaltır ve derin bir rahatlama sağlar. Haftada bir seans uzun vadeli fayda sunar.</p>", Image = "blog/isvec-masaj.jpg", CatagoryId = cats[1].Id, AppUserId = userId, IsPublish = true, CreatedDate = DateTime.Now.AddDays(-24) },
                    new() { Title = "Derin Doku Masajı Kimler İçin Uygundur?", SubTitle = "Kronik ağrılar için çözüm", Content = "<p>Derin doku masajı, kasların alt katmanlarına ulaşarak kronik ağrıyı ve kas spazmını çözer. Atletler ve masa başı çalışanlar için özellikle önerilir.</p>", Image = "blog/derin-doku.jpg", CatagoryId = cats[1].Id, AppUserId = userId, IsPublish = true, CreatedDate = DateTime.Now.AddDays(-22) },
                    new() { Title = "Refleksoloji: Ayak Noktalarından Şifa", SubTitle = "Vücudun haritası ayaklarda", Content = "<p>Refleksoloji, ayakta bulunan basınç noktalarına belirli tekniklerle baskı uygulanarak organların uyarılması prensibine dayanır. Stres azaltmada ve enerji dengelenmesinde etkilidir.</p>", Image = "blog/refleksoloji.jpg", CatagoryId = cats[1].Id, AppUserId = userId, IsPublish = true, CreatedDate = DateTime.Now.AddDays(-20) },

                    // Wellness & Meditasyon (CatId 3)
                    new() { Title = "5 Dakikalık Nefes Meditasyonu", SubTitle = "İş yerinde yapabileceğiniz teknikler", Content = "<p>Derin nefes alma teknikleri kortizol seviyesini düşürür ve anı farkındalığı artırır. Gözlerinizi kapatın, 4 saniye nefes alın, 4 saniye tutun, 6 saniye verin. Beş tekrar yeterlidir.</p>", Image = "blog/meditasyon.jpg", CatagoryId = cats[2].Id, AppUserId = userId, IsPublish = true, CreatedDate = DateTime.Now.AddDays(-18) },
                    new() { Title = "Mindfulness ve Günlük Hayat", SubTitle = "Her anda tam anlamıyla var olmak", Content = "<p>Mindfulness, geçmiş ya da geleceğe takılmadan şu ana odaklanmaktır. Yemek yerken, yürürken veya uyumadan önce birkaç dakika bilinçli nefes egzersizi yapmak başlangıç için yeterlidir.</p>", Image = "blog/mindfulness.jpg", CatagoryId = cats[2].Id, AppUserId = userId, IsPublish = true, CreatedDate = DateTime.Now.AddDays(-16) },
                    new() { Title = "Dijital Detoks: Telefondan Uzak Kalmak", SubTitle = "Zihin sağlığı için ekran molası", Content = "<p>Günde en az 2 saat ekrandan uzak bir 'dijital suskunluk' dönemi geçirmek uyku kalitesini ve odaklanmayı istatistiksel olarak %37 artırmaktadır. Yatmadan 1 saat önce başlayın.</p>", Image = "blog/digital-detoks.jpg", CatagoryId = cats[2].Id, AppUserId = userId, IsPublish = true, CreatedDate = DateTime.Now.AddDays(-14) },

                    // Aromaterapi (CatId 4)
                    new() { Title = "Lavanta Yağının 7 Faydası", SubTitle = "Doğanın en çok yönlü esansı", Content = "<p>Lavanta esansiyel yağı; uyku düzenini iyileştirir, kaygıyı azaltır, hafıza ağrısını dindirir, cilt tahrişini giderir, yara iyileşmesini hızlandırır, böcekleri uzaklaştırır ve saç büyümesini destekler.</p>", Image = "blog/lavanta.jpg", CatagoryId = cats[3].Id, AppUserId = userId, IsPublish = true, CreatedDate = DateTime.Now.AddDays(-13) },
                    new() { Title = "Difüzör Kullanımı Rehberi", SubTitle = "Odanızı aromaterapi cennetine çevirin", Content = "<p>Difüzör seçerken ultrasonik modeller tercih edin. Dam başına 3-5 damla yağ yeterlidir. Çay ağacı ve ökaliptüs kombinasyonu sabah, lavanta ve papatya karışımı gece için idealdir.</p>", Image = "blog/difuzor.jpg", CatagoryId = cats[3].Id, AppUserId = userId, IsPublish = true, CreatedDate = DateTime.Now.AddDays(-11) },

                    // Beslenme & Detoks (CatId 5)
                    new() { Title = "3 Günlük Bahar Detoks Programı", SubTitle = "İçinizi baharla temizleyin", Content = "<p>1. gün: Yalnızca sıvı gıdalar. 2. gün: Çiğ sebze ve meyve. 3. gün: Hafif pişirilmiş yiyecekler. Bol su, yeşil çay ve taze sıkılmış meyve suları tüm program boyunca serbesttir.</p>", Image = "blog/detoks.jpg", CatagoryId = cats[4].Id, AppUserId = userId, IsPublish = true, CreatedDate = DateTime.Now.AddDays(-10) },
                    new() { Title = "Kolajen Üretimini Artıran 8 Besin", SubTitle = "Mutfağınızdan güzellik reçetesi", Content = "<p>C vitamini (kivi, biber), çinko (kabak çekirdeği, yumurta), bakır (kakao, susam) ve glisin (kemik suyu, jelatin) kolajen sentezini destekler. Bu besinleri günlük diyetinize ekleyin.</p>", Image = "blog/kollajen.jpg", CatagoryId = cats[4].Id, AppUserId = userId, IsPublish = true, CreatedDate = DateTime.Now.AddDays(-9) },

                    // Hamam & Sauna (CatId 6)
                    new() { Title = "Türk Hamamı Deneyimi: Neler Beklemelisiniz?", SubTitle = "Geleneksel temizlik ritüeli", Content = "<p>Geleneksel Türk hamamında önce soyunma odası (camekan), ardından ılıklık (ılıcak) ve sıcaklık (hararet) bölümlerine geçilir. Kese ve köpük masajı cildi derinlemesine temizler.</p>", Image = "blog/hamam.jpg", CatagoryId = cats[5].Id, AppUserId = userId, IsPublish = true, CreatedDate = DateTime.Now.AddDays(-8) },
                    new() { Title = "Sauna ve Bağışıklık Sistemi İlişkisi", SubTitle = "Düzenli saunanın bilimsel kanıtları", Content = "<p>Haftada 2-3 kez yapılan sauna seanları, belediyenin bağışıklık hücrelerini aktive eder. Finlandiya'da yapılan uzun dönem araştırmalar kalp hastalığı riskini %40 azalttığını göstermiştir.</p>", Image = "blog/sauna.jpg", CatagoryId = cats[5].Id, AppUserId = userId, IsPublish = true, CreatedDate = DateTime.Now.AddDays(-7) },

                    // Anti-Aging (CatId 7)
                    new() { Title = "Retinol Kullanımının Püf Noktaları", SubTitle = "Yanlış kullanmaktan kaçının", Content = "<p>Retinol güçlü bir A vitamini türevidir. Başlangıçta haftada 2 gece kullanın. Güneş koruyucu olmadan gündüz uygulamayın. Hamilelikte kesinlikle kullanmayın. 6-8 haftada sonuç görürsünüz.</p>", Image = "blog/retinol.jpg", CatagoryId = cats[6].Id, AppUserId = userId, IsPublish = true, CreatedDate = DateTime.Now.AddDays(-6) },
                    new() { Title = "Gözaltı Kırışıklıklarına Karşı Doğal Yöntemler", SubTitle = "Kimyasal olmadan genç görünün", Content = "<p>Soğuk papatya çayı poşeti, buz küresi masajı ve göz çevresi için özel jeller gözaltı kırışıklıklarını belirgin biçimde azaltır. Uyku düzeni ve güneş koruyucu en büyük destekçinizdir.</p>", Image = "blog/gozalti.jpg", CatagoryId = cats[6].Id, AppUserId = userId, IsPublish = true, CreatedDate = DateTime.Now.AddDays(-5) },

                    // Saç Bakımı (CatId 8)
                    new() { Title = "Keratin Tedavisi Nedir ve Ne Kadar Sürer?", SubTitle = "Pürüzsüz saçların sırrı", Content = "<p>Keratin tedavisi, saçın dış katmanını doldurarak friziness ve kıvrımı azaltır. Teknik olarak 2-4 saat sürer ve 3-5 ay kalıcı olabilir. Sülfatsız şampuan kullanımı ömrü uzatır.</p>", Image = "blog/keratin.jpg", CatagoryId = cats[7].Id, AppUserId = userId, IsPublish = true, CreatedDate = DateTime.Now.AddDays(-4) },
                    new() { Title = "Hindistan Cevizi Yağının Saça Faydaları", SubTitle = "Mutfak malzemesiyle saç bakımı", Content = "<p>Saf hindistan cevizi yağı, saç teline nüfuz ederek protein kaybını önler. Yıkama öncesi maske olarak 30 dakika uygulamak yeterlidir. Yağlı saçlar için ucuçlardan köklere gidin.</p>", Image = "blog/hindistan-cevizi-yag.jpg", CatagoryId = cats[7].Id, AppUserId = userId, IsPublish = true, CreatedDate = DateTime.Now.AddDays(-3) },

                    // Yoga & Pilates (CatId 9)
                    new() { Title = "Sabah Yogası: 10 Dakikada Güne Başlangıç", SubTitle = "Uyku sersemliğini hızla atın", Content = "<p>Yataktan kalkar kalkmaz; kedi-inek hareketi, çocuk pozu ve güneş selamlama serisi kan dolaşımını hızlandırır ve enerji verir. DVD ya da uygulama gerekmez, birkaç mat yeterli.</p>", Image = "blog/sabah-yoga.jpg", CatagoryId = cats[8].Id, AppUserId = userId, IsPublish = true, CreatedDate = DateTime.Now.AddDays(-2) },
                    new() { Title = "Pilates ve Duruş Bozukluğu", SubTitle = "Masa başı çalışanlar için çözüm", Content = "<p>Pilates core kaslarını güçlendirerek omurga desteğini artırır. Haftada 3 seans düzenli pilates, 3 ay içinde postür bozukluğunu gözle görülür şekilde iyileştirir.</p>", Image = "blog/pilates-durus.jpg", CatagoryId = cats[8].Id, AppUserId = userId, IsPublish = true, CreatedDate = DateTime.Now.AddDays(-1) },

                    // Genel Sağlık (CatId 10)
                    new() { Title = "Düzenli Su İçmenin Cilde Etkisi", SubTitle = "En ucuz güzellik sırrı", Content = "<p>Günlük 2-2,5 litre su içmek; cilt nem dengesini korur, gözeneklerin görünümünü azaltır ve toksik maddelerin atılımını hızlandırır. Sabah aç karnına içilen 1 bardak ılık su özellikle faydalıdır.</p>", Image = "blog/su-icleme.jpg", CatagoryId = cats[9].Id, AppUserId = userId, IsPublish = true, CreatedDate = DateTime.Now },
                    new() { Title = "Uyku Kalitesi Cilt Sağlığını Nasıl Etkiler?", SubTitle = "Güzellik uykusu gerçek mi?", Content = "<p>Uyku sırasında salgılanan büyüme hormonu hücre yenilenmesini tetikler. 7-9 saat kaliteli uyku alan bireylerin ciltleri %30 daha verimli onarılır. Yan uyumak kırışıklık oluşturabilir; sırtüstü uyumak önerilir.</p>", Image = "blog/uyku-cilt.jpg", CatagoryId = cats[9].Id, AppUserId = userId, IsPublish = true, CreatedDate = DateTime.Now },
                    new() { Title = "Egzersiz Sonrası Cilt Bakımı Rutini", SubTitle = "Terin ardından ne yapmalısınız", Content = "<p>Egzersiz sonrası ilk 20 dakika içinde yüzünüzü yıkayın. Gözenekleri tıkamayan hafif nemlendirici kullanın. Ter birikimi akne ve cilt tahrişine yol açar; bu nedenle temizlik önemlidir.</p>", Image = "blog/egzersiz-cilt.jpg", CatagoryId = cats[0].Id, AppUserId = userId, IsPublish = true, CreatedDate = DateTime.Now },
                    new() { Title = "Stres ve Cilt: Kortizolün Etkisi", SubTitle = "Stres neden sivilce yapar?", Content = "<p>Kortizol hormonu yağ bezlerini uyararak sebum üretimini artırır. Kronik stres; akne, egzema ve sedef hastalığını tetikleyebilir. Meditasyon ve egzersiz kortizolü doğal yolla düşürür.</p>", Image = "blog/stres-cilt.jpg", CatagoryId = cats[0].Id, AppUserId = userId, IsPublish = true, CreatedDate = DateTime.Now },
                    new() { Title = "Güneş Koruyucu Her Mevsim Şart mı?", SubTitle = "Bulutlu günlerde de UV var", Content = "<p>UVA ışınları bulutları geçer ve cam camı deler. Kış ve ilkbaharda da SPF 30+ kullanmak erken yaşlanmayı ve cilt kanserini önler. Kimyasal filtreler güneş öncesi, mineral filtreler hemen uygulanabilir.</p>", Image = "blog/gunes-koruyucu.jpg", CatagoryId = cats[0].Id, AppUserId = userId, IsPublish = true, CreatedDate = DateTime.Now },
                    new() { Title = "Spa Öncesi Yapılmaması Gerekenler", SubTitle = "Seans veriminizi artırın", Content = "<p>Spa randevusundan 24 saat önce tüy alma işlemi yaptırmayın. Ağır yemek yemeyin, alkol almayın. Yoğun egzersiz yapmaktan kaçının. Bu önlemler hem konforunuzu artırır hem de ürünlerin emilimini iyileştirir.</p>", Image = "blog/spa-oncesi.jpg", CatagoryId = cats[1].Id, AppUserId = userId, IsPublish = true, CreatedDate = DateTime.Now },
                    new() { Title = "Hamilelikte Güvenli Spa Hizmetleri", SubTitle = "Anneye uygun dokunuşlar", Content = "<p>Hamileliğin 2. trimesterinden itibaren prenatal masaj güvenlidir; 45 dakikayı geçmemeli ve yüzüstü pozisyon kesinlikle kullanılmamalıdır. Esansiyel yağlar için doktor onayı alın.</p>", Image = "blog/hamilelik-spa.jpg", CatagoryId = cats[2].Id, AppUserId = userId, IsPublish = true, CreatedDate = DateTime.Now },
                    new() { Title = "İlk Kez Spa'ya Gidenler İçin Rehber", SubTitle = "Ne giyilir, ne beklenir?", Content = "<p>İlk spa ziyaretinizde rahat iç çamaşırı giyin; tesis genellikle havlu sağlar. Değerli takı ve elektronik cihazlarınızı evde bırakın. Seans öncesi 15 dakika erken gelinmesi önerilir.</p>", Image = "blog/ilk-spa.jpg", CatagoryId = cats[2].Id, AppUserId = userId, IsPublish = true, CreatedDate = DateTime.Now },
                };
                context.Blogs.AddRange(blogs);
                await context.SaveChangesAsync();
            }

            // ──────────────────── SLIDER ────────────────────
            if (!context.Sliders.Any())
            {
                context.Sliders.AddRange(
                    new Slider { Title = "Bedeninizi Yenileyin, Ruhunuzu Dinlendirin", Content = "Premium spa hizmetlerimizle kendinizi özel hissedin. Uzman terapistlerimiz size özel programlar hazırlıyor.", Image = "slider/slider1.jpg", IsPublish = true, CreatedDate = DateTime.Now },
                    new Slider { Title = "Doğanın Saf Enerjisiyle Buluşun", Content = "Organik ürünler ve geleneksel tekniklerle hazırlanmış ritüellerimizle bedeninizi arındırın.", Image = "slider/slider2.jpg", IsPublish = true, CreatedDate = DateTime.Now },
                    new Slider { Title = "Lüksü Kolunuzun Altına Alın", Content = "5 yıldızlı spa deneyimini yaşayın. Rezervasyonunuzu hemen oluşturun, özel indirimlerden yararlanın.", Image = "slider/slider3.jpg", IsPublish = true, CreatedDate = DateTime.Now }
                );
                await context.SaveChangesAsync();
            }

            // ──────────────────── HİZMETLER ────────────────────
            if (!context.Services.Any())
            {
                context.Services.AddRange(
                    // Öne çıkanlar (Prominence = true)
                    new Service { Title = "Klasik İsveç Masajı", Content = "60 dk — 650 ₺", Details = "Kas gerginliğini gideren, kan dolaşımını artıran rahatlatıcı masaj. Aromaterapi yağları ile uygulanır.", Image = "service/isvec-masaj.jpg", Prominence = true, IsPublish = true, CreatedDate = DateTime.Now },
                    new Service { Title = "Türk Hamamı Ritüeli", Content = "90 dk — 850 ₺", Details = "Geleneksel kese ve köpük masajı ile derin temizlik. Cildiniz ipek gibi olacak.", Image = "service/hamam.jpg", Prominence = true, IsPublish = true, CreatedDate = DateTime.Now },
                    new Service { Title = "Altın Yüz Maskesi", Content = "45 dk — 950 ₺", Details = "24 ayar altın özlü maske ile cilt tonu eşitlemesi ve anti-aging etkisi.", Image = "service/altin-maske.jpg", Prominence = true, IsPublish = true, CreatedDate = DateTime.Now },
                    new Service { Title = "Derin Doku Masajı", Content = "75 dk — 750 ₺", Details = "Kronik kas ağrıları ve spazm için profesyonel derin doku tekniği.", Image = "service/derin-doku.jpg", Prominence = true, IsPublish = true, CreatedDate = DateTime.Now },
                    new Service { Title = "Aromaterapi Masajı", Content = "60 dk — 700 ₺", Details = "Doğal esansiyel yağlarla stres gidericiden uyku düzenleyiciye tüm ihtiyaçlar için.", Image = "service/aromaterapi.jpg", Prominence = true, IsPublish = true, CreatedDate = DateTime.Now },
                    new Service { Title = "Çiftler Masajı", Content = "90 dk — 1.400 ₺", Details = "Özel VIP odamızda sevdiğinizle birlikte masaj keyfi. Şampanya ikram edilir.", Image = "service/ciftler-masaj.jpg", Prominence = true, IsPublish = true, CreatedDate = DateTime.Now },
                    new Service { Title = "Refleksoloji", Content = "45 dk — 550 ₺", Details = "Ayak basınç noktalarından vücudun denge merkezini uyarma tekniği.", Image = "service/refleksoloji.jpg", Prominence = true, IsPublish = true, CreatedDate = DateTime.Now },
                    new Service { Title = "Sauna & Buhar Odası", Content = "60 dk — 400 ₺", Details = "Finlandiya tipi kuru sauna + Türk buhar odası kombinasyonu.", Image = "service/sauna.jpg", Prominence = true, IsPublish = true, CreatedDate = DateTime.Now },
                    new Service { Title = "Keratin Saç Bakımı", Content = "120 dk — 1.200 ₺", Details = "Profesyonel keratin tedavisi ile pürüzsüz, parlak ve frizden arındırılmış saçlar.", Image = "service/keratin.jpg", Prominence = true, IsPublish = true, CreatedDate = DateTime.Now },
                    new Service { Title = "Cilt Peeling Seansı", Content = "50 dk — 600 ₺", Details = "Kimyasal peeling ile ölü deri temizleme, cilt yenileme ve ton eşitleme.", Image = "service/peeling.jpg", Prominence = true, IsPublish = true, CreatedDate = DateTime.Now },
                    // Diğerleri
                    new Service { Title = "Bebek Masajı Kursu", Content = "45 dk — 350 ₺", Details = "Ebeveynlere bebek masajı teknikleri öğretilir.", Image = "service/bebek-masaj.jpg", Prominence = false, IsPublish = true, CreatedDate = DateTime.Now },
                    new Service { Title = "Hamilelik Masajı", Content = "60 dk — 700 ₺", Details = "2. trimesterden itibaren uygulanan, anneye özel prenatal masaj.", Image = "service/hamilelik-masaj.jpg", Prominence = false, IsPublish = true, CreatedDate = DateTime.Now }
                );
                await context.SaveChangesAsync();
            }

            // ──────────────────── GALERİ ────────────────────
            if (!context.Galaries.Any())
            {
                var galleryItems = new List<Galary>
                {
                    new() { Title = "VIP Masaj Odası", Image = "galery/oda1.jpg", IsPublish = true, CreatedDate = DateTime.Now },
                    new() { Title = "Hamam Alanı", Image = "galery/hamam.jpg", IsPublish = true, CreatedDate = DateTime.Now },
                    new() { Title = "Sauna Bölümü", Image = "galery/sauna.jpg", IsPublish = true, CreatedDate = DateTime.Now },
                    new() { Title = "Resepsiyon", Image = "galery/resepsiyon.jpg", IsPublish = true, CreatedDate = DateTime.Now },
                    new() { Title = "Aromaterapi Koleksiyonu", Image = "galery/aromaterapi.jpg", IsPublish = true, CreatedDate = DateTime.Now },
                    new() { Title = "Dinlenme Salonu", Image = "galery/salon.jpg", IsPublish = true, CreatedDate = DateTime.Now },
                    new() { Title = "Çift Masaj Odası", Image = "galery/cift-oda.jpg", IsPublish = true, CreatedDate = DateTime.Now },
                    new() { Title = "Havuz & Jakuzi", Image = "galery/havuz.jpg", IsPublish = true, CreatedDate = DateTime.Now },
                    new() { Title = "Makyaj & Saç Bakım Stüdyosu", Image = "galery/studyo.jpg", IsPublish = true, CreatedDate = DateTime.Now },
                    new() { Title = "Dış Terapi Bahçesi", Image = "galery/bahce.jpg", IsPublish = true, CreatedDate = DateTime.Now },
                    new() { Title = "Premium Ürün Vitrini", Image = "galery/urunler.jpg", IsPublish = true, CreatedDate = DateTime.Now },
                    new() { Title = "Yoga & Meditasyon Odası", Image = "galery/yoga.jpg", IsPublish = true, CreatedDate = DateTime.Now },
                };
                context.Galaries.AddRange(galleryItems);
                await context.SaveChangesAsync();
            }

            // ──────────────────── HAKKIMIZDA ────────────────────
            if (!context.Abouts.Any())
            {
                context.Abouts.Add(new About
                {
                    Title = "Wellness & Spa Merkezi",
                    Content = "<p>2009 yılında kurulan merkezimiz, on beş yılı aşkın deneyimiyle şehrin kalbinde huzur ve güzelliğin buluşma noktası olmaya devam etmektedir. Sertifikalı terapistlerimiz, en yeni teknikler ve özenle seçilmiş organik ürünlerle sizin için kişisel bir deneyim tasarlar.</p>",
                    CardTitle1 = "Uzman Terapistler",
                    CardContent1 = "Her biri uluslararası sertifikalara sahip 20+ uzman terapistimiz ile hizmetinizdeyiz.",
                    CardTitle2 = "Organik Ürünler",
                    CardContent2 = "Kimyasal içerik barındırmayan, doğanın saf özünden elde edilmiş ürünler kullanıyoruz.",
                    Image = "about/about-main.jpg",
                    IsPublish = true,
                    CreatedDate = DateTime.Now
                });
                await context.SaveChangesAsync();
            }

            // ──────────────────── İLETİŞİM ────────────────────
            if (!context.Contacts.Any())
            {
                context.Contacts.Add(new Contact
                {
                    Adres = "Atatürk Cad. No:45 Kat:2, Kırklareli Merkez",
                    Phone = "+90 288 555 0123",
                    Mail = "info@wellnessspa.com",
                    InstagramUrl = "https://instagram.com/wellnessspa",
                    FacebookUrl = "https://facebook.com/wellnessspa",
                    TwitterUrl = "https://twitter.com/wellnessspa",
                    YoutubeUrl = "https://youtube.com/@wellnessspa",
                    IsPublish = true,
                    CreatedDate = DateTime.Now
                });
                await context.SaveChangesAsync();
            }
        }
    }
}
