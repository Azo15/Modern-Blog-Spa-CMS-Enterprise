using DataAccesLayer.Concrete;
using EntityLayer.Concrete;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace WebUI.Data
{
    public static class DataSeeder
    {
        private static void Ensure(IdentityResult result)
        {
            if (!result.Succeeded) throw new InvalidOperationException(string.Join("; ", result.Errors.Select(e => e.Description)));
        }

        public static IReadOnlyList<Service> CreateServiceCatalog() =>
        [
            new Service { Title = "Klasik İsveç Masajı", Content = "60 dk — 650 ₺", Details = "Kas gerginliğini gideren, kan dolaşımını artıran rahatlatıcı masaj. Aromaterapi yağları ile uygulanır.", Image = "img/service-isvec.webp", Prominence = true, IsPublish = true, CreatedDate = DateTime.UtcNow },
            new Service { Title = "Türk Hamamı Ritüeli", Content = "90 dk — 850 ₺", Details = "Geleneksel kese ve köpük masajı ile derin temizlik. Cildiniz ipek gibi olacak.", Image = "img/service-hamam.webp", Prominence = true, IsPublish = true, CreatedDate = DateTime.UtcNow },
            new Service { Title = "Altın Yüz Maskesi", Content = "45 dk — 950 ₺", Details = "24 ayar altın özlü maske ile cilt tonu eşitlemesi ve anti-aging etkisi.", Image = "img/service-altinmaske.webp", Prominence = true, IsPublish = true, CreatedDate = DateTime.UtcNow },
            new Service { Title = "Derin Doku Masajı", Content = "75 dk — 750 ₺", Details = "Kronik kas ağrıları ve spazm için profesyonel derin doku tekniği.", Image = "img/service-derindoku.webp", Prominence = true, IsPublish = true, CreatedDate = DateTime.UtcNow },
            new Service { Title = "Aromaterapi Masajı", Content = "60 dk — 700 ₺", Details = "Doğal esansiyel yağlarla stres gidericiden uyku düzenleyiciye tüm ihtiyaçlar için.", Image = "img/service-aromaterapi.webp", Prominence = true, IsPublish = true, CreatedDate = DateTime.UtcNow },
            new Service { Title = "Çiftler Masajı", Content = "90 dk — 1.400 ₺", Details = "Özel VIP odamızda sevdiğinizle birlikte masaj keyfi. Şampanya ikram edilir.", Image = "img/service-ciftler.webp", Prominence = true, IsPublish = true, CreatedDate = DateTime.UtcNow },
            new Service { Title = "Refleksoloji", Content = "45 dk — 550 ₺", Details = "Ayak basınç noktalarından vücudun denge merkezini uyarma tekniği.", Image = "img/service-refleksoloji.webp", Prominence = true, IsPublish = true, CreatedDate = DateTime.UtcNow },
            new Service { Title = "Sauna & Buhar Odası", Content = "60 dk — 400 ₺", Details = "Finlandiya tipi kuru sauna + Türk buhar odası kombinasyonu.", Image = "img/service-sauna.webp", Prominence = true, IsPublish = true, CreatedDate = DateTime.UtcNow },
            new Service { Title = "Keratin Saç Bakımı", Content = "120 dk — 1.200 ₺", Details = "Profesyonel keratin tedavisi ile pürüzsüz, parlak ve frizden arındırılmış saçlar.", Image = "img/service-keratin.webp", Prominence = true, IsPublish = true, CreatedDate = DateTime.UtcNow },
            new Service { Title = "Yosun Peeling", Content = "50 dk — 600 ₺", Details = "Yosun içerikli bakım ile nazik peeling ve cilt bakım ritüeli.", Image = "img/service-peeling.webp", Prominence = true, IsPublish = true, CreatedDate = DateTime.UtcNow },
            new Service { Title = "Bebek Masajı Kursu", Content = "45 dk — 350 ₺", Details = "Ebeveynlere bebek masajı teknikleri öğretilir.", Image = "img/service-bebek.webp", Prominence = false, IsPublish = true, CreatedDate = DateTime.UtcNow },
            new Service { Title = "Hamilelik Masajı", Content = "60 dk — 700 ₺", Details = "2. trimesterden itibaren uygulanan, anneye özel prenatal masaj.", Image = "img/service-hamilelik.webp", Prominence = false, IsPublish = true, CreatedDate = DateTime.UtcNow }
        ];

        // Only fill missing catalog entries. Never overwrite prices, publication state or soft deletions.
        public static async Task<int> SyncServiceCatalogAsync(AppDbContext db, CancellationToken ct = default)
        {
            var strategy = db.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
                var titles = await db.Services.IgnoreQueryFilters().Select(x => x.Title).ToListAsync(ct);
                var existing = new HashSet<string>(titles.Where(x => x != null).Select(x => x!.Trim()), StringComparer.OrdinalIgnoreCase);
                var missing = CreateServiceCatalog().Where(x => !existing.Contains(x.Title!) &&
                    !(x.Title == "Yosun Peeling" && existing.Contains("Cilt Peeling Seansı"))).ToArray();
                db.Services.AddRange(missing);
                try
                {
                    await db.SaveChangesAsync(ct);
                    await transaction.CommitAsync(ct);
                    return missing.Length;
                }
                catch
                {
                    // A transient SQL retry must not retain failed Added entities from this attempt.
                    foreach (var service in missing) db.Entry(service).State = EntityState.Detached;
                    throw;
                }
            });
        }

        public static async Task SeedRolesAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var manager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<int>>>();
            foreach (var role in BusinessLayer.Security.AppRoles.All)
                if (!await manager.RoleExistsAsync(role)) Ensure(await manager.CreateAsync(new IdentityRole<int>(role)));
            // Migrate existing memberships before removing retired roles. Never create demo accounts in production.
            var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
            foreach (var legacy in new[] { "Content Manager", "Author", "Moderator", "Registered User" })
            {
                var retired = await manager.FindByNameAsync(legacy);
                if (retired is null) continue;
                foreach (var user in await users.GetUsersInRoleAsync(legacy))
                {
                    var target = legacy == "Registered User" ? BusinessLayer.Security.AppRoles.Customer : BusinessLayer.Security.AppRoles.Staff;
                    if (!await users.IsInRoleAsync(user, BusinessLayer.Security.AppRoles.Admin) && !await users.IsInRoleAsync(user, target))
                        Ensure(await users.AddToRoleAsync(user, target));
                    Ensure(await users.RemoveFromRoleAsync(user, legacy));
                    Ensure(await users.UpdateSecurityStampAsync(user));
                }
                Ensure(await manager.DeleteAsync(retired));
            }
            // A legacy account with several memberships keeps the highest of the three levels.
            foreach (var level in new[] { BusinessLayer.Security.AppRoles.Admin, BusinessLayer.Security.AppRoles.Staff })
                foreach (var user in await users.GetUsersInRoleAsync(level))
                {
                    var redundant = (await users.GetRolesAsync(user)).Where(role => role != level &&
                        (role == BusinessLayer.Security.AppRoles.Customer || level == BusinessLayer.Security.AppRoles.Admin && role == BusinessLayer.Security.AppRoles.Staff)).ToArray();
                    if (redundant.Length == 0) continue;
                    Ensure(await users.RemoveFromRolesAsync(user, redundant));
                    Ensure(await users.UpdateSecurityStampAsync(user));
                }
            // Visitor is an anonymous access profile, never an Identity role.
            var visitor = await manager.FindByNameAsync("Visitor");
            if (visitor != null) Ensure(await manager.DeleteAsync(visitor));
        }

        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            if (!scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>().IsDevelopment()) return;
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<int>>>();

            // Test accounts and sample content are development-only.
            await SeedRolesAsync(serviceProvider);
            await SyncServiceCatalogAsync(context);
            var accounts = new[] {
                ("admin@spa.com", BusinessLayer.Security.AppRoles.Admin),
                ("editor@spa.com", BusinessLayer.Security.AppRoles.Staff),
                ("author@spa.com", BusinessLayer.Security.AppRoles.Staff),
                ("moderator@spa.com", BusinessLayer.Security.AppRoles.Staff),
                ("user@spa.com", BusinessLayer.Security.AppRoles.Customer)
            };
            foreach (var (email, role) in accounts)
            {
                var user = await userManager.FindByEmailAsync(email);
                if (user == null)
                {
                    user = new AppUser { UserName = email, Email = email, FirstName = "Test", LastName = role, EmailConfirmed = true };
                    Ensure(await userManager.CreateAsync(user, "P@ssword123!"));
                }
                else
                {
                    user.EmailConfirmed = true;
                    Ensure(await userManager.UpdateAsync(user));
                    if (!await userManager.CheckPasswordAsync(user, "P@ssword123!"))
                        Ensure(await userManager.ResetPasswordAsync(user, await userManager.GeneratePasswordResetTokenAsync(user), "P@ssword123!"));
                }
                // Development demo display names are stable on both fresh and existing databases.
                var displayName = email switch
                {
                    "admin@spa.com" => ("XSPA", "Ekibi"),
                    "editor@spa.com" => ("XSPA", "Editörü"),
                    "author@spa.com" => ("Wellness", "Uzmanı"),
                    _ => (user.FirstName, user.LastName)
                };
                if (user.FirstName != displayName.Item1 || user.LastName != displayName.Item2)
                {
                    user.FirstName = displayName.Item1;
                    user.LastName = displayName.Item2;
                    Ensure(await userManager.UpdateAsync(user));
                }
                // Keep demo accounts deterministic and prevent stale elevated roles.
                var existingRoles = await userManager.GetRolesAsync(user);
                if (existingRoles.Any(r => r != role))
                    Ensure(await userManager.RemoveFromRolesAsync(user, existingRoles.Where(r => r != role)));
                if (!await userManager.IsInRoleAsync(user, role)) Ensure(await userManager.AddToRoleAsync(user, role));
            }
            var publicCache = scope.ServiceProvider.GetService<BusinessLayer.Abstract.ICacheService>();
            if (publicCache is not null) await publicCache.RemoveByPrefixAsync("blogs_");
            var seederUser = await userManager.FindByEmailAsync("admin@spa.com")
                ?? throw new InvalidOperationException("Development admin account was not created.");

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
                    new() { PublishStatus = EntityLayer.Enum.PublishStatus.Published, Title = "Kış Aylarında Cilt Neminin Önemi", SubTitle = "Soğuk havada cildinizi koruyun", Content = "<p>Kış aylarında hava nemi düşer ve cildiniz kurumaya başlar. Günde en az 2 litre su içmek ve nemlendirici kullanmak cildinizin sağlığını korur. Doğal zeytinyağlı kremler bu dönemde idealdir.</p>", Image = "blog/cilt-nem.jpg", CatagoryId = cats[0].Id, AppUserId = userId, IsPublish = true, CreatedDate = DateTime.Now.AddDays(-29) },
                    new() { PublishStatus = EntityLayer.Enum.PublishStatus.Published, Title = "C Vitamini Serumu Neden Kullanmalısınız?", SubTitle = "Parlaklık ve canlılık için", Content = "<p>C vitamini serumu cilt tonu eşitsizliklerini giderir, leke açar ve antioksidan koruması sağlar. Sabah rutininize ekleyin ve farkı haftalarca görün.</p>", Image = "blog/c-vitamini.jpg", CatagoryId = cats[0].Id, AppUserId = userId, IsPublish = true, CreatedDate = DateTime.Now.AddDays(-27) },
                    new() { PublishStatus = EntityLayer.Enum.PublishStatus.Published, Title = "Peeling Nedir ve Ne Zaman Yapılmalı?", SubTitle = "Hücre yenilemenin sırrı", Content = "<p>Kimyasal ya da mekanik peeling, ölü hücreleri uzaklaştırır ve cildi yeniler. Haftada 1-2 kez uygulamak yeterlidir; hassas ciltler için daha az sıklık önerilir.</p>", Image = "blog/peeling.jpg", CatagoryId = cats[0].Id, AppUserId = userId, IsPublish = true, CreatedDate = DateTime.Now.AddDays(-25) },

                    // Masaj Teknikleri (CatId 2)
                    new() { PublishStatus = EntityLayer.Enum.PublishStatus.Published, Title = "İsveç Masajı ve Faydaları", SubTitle = "Kasları gevşeten klasik teknik", Content = "<p>İsveç masajı, uzun ve kayıcı vuruşlarla yapılır. Kan dolaşımını iyileştirir, kas gerginliğini azaltır ve derin bir rahatlama sağlar. Haftada bir seans uzun vadeli fayda sunar.</p>", Image = "blog/isvec-masaj.jpg", CatagoryId = cats[1].Id, AppUserId = userId, IsPublish = true, CreatedDate = DateTime.Now.AddDays(-24) },
                    new() { PublishStatus = EntityLayer.Enum.PublishStatus.Published, Title = "Derin Doku Masajı Kimler İçin Uygundur?", SubTitle = "Kronik ağrılar için çözüm", Content = "<p>Derin doku masajı, kasların alt katmanlarına ulaşarak kronik ağrıyı ve kas spazmını çözer. Atletler ve masa başı çalışanlar için özellikle önerilir.</p>", Image = "blog/derin-doku.jpg", CatagoryId = cats[1].Id, AppUserId = userId, IsPublish = true, CreatedDate = DateTime.Now.AddDays(-22) },
                    new() { PublishStatus = EntityLayer.Enum.PublishStatus.Published, Title = "Refleksoloji: Ayak Noktalarından Şifa", SubTitle = "Vücudun haritası ayaklarda", Content = "<p>Refleksoloji, ayakta bulunan basınç noktalarına belirli tekniklerle baskı uygulanarak organların uyarılması prensibine dayanır. Stres azaltmada ve enerji dengelenmesinde etkilidir.</p>", Image = "blog/refleksoloji.jpg", CatagoryId = cats[1].Id, AppUserId = userId, IsPublish = true, CreatedDate = DateTime.Now.AddDays(-20) },

                    // Wellness & Meditasyon (CatId 3)
                    new() { PublishStatus = EntityLayer.Enum.PublishStatus.Published, Title = "5 Dakikalık Nefes Meditasyonu", SubTitle = "İş yerinde yapabileceğiniz teknikler", Content = "<p>Derin nefes alma teknikleri kortizol seviyesini düşürür ve anı farkındalığı artırır. Gözlerinizi kapatın, 4 saniye nefes alın, 4 saniye tutun, 6 saniye verin. Beş tekrar yeterlidir.</p>", Image = "blog/meditasyon.jpg", CatagoryId = cats[2].Id, AppUserId = userId, IsPublish = true, CreatedDate = DateTime.Now.AddDays(-18) },
                    new() { PublishStatus = EntityLayer.Enum.PublishStatus.Published, Title = "Mindfulness ve Günlük Hayat", SubTitle = "Her anda tam anlamıyla var olmak", Content = "<p>Mindfulness, geçmiş ya da geleceğe takılmadan şu ana odaklanmaktır. Yemek yerken, yürürken veya uyumadan önce birkaç dakika bilinçli nefes egzersizi yapmak başlangıç için yeterlidir.</p>", Image = "blog/mindfulness.jpg", CatagoryId = cats[2].Id, AppUserId = userId, IsPublish = true, CreatedDate = DateTime.Now.AddDays(-16) },
                    new() { PublishStatus = EntityLayer.Enum.PublishStatus.Published, Title = "Dijital Detoks: Telefondan Uzak Kalmak", SubTitle = "Zihin sağlığı için ekran molası", Content = "<p>Günde en az 2 saat ekrandan uzak bir 'dijital suskunluk' dönemi geçirmek uyku kalitesini ve odaklanmayı istatistiksel olarak %37 artırmaktadır. Yatmadan 1 saat önce başlayın.</p>", Image = "blog/digital-detoks.jpg", CatagoryId = cats[2].Id, AppUserId = userId, IsPublish = true, CreatedDate = DateTime.Now.AddDays(-14) },

                    // Aromaterapi (CatId 4)
                    new() { PublishStatus = EntityLayer.Enum.PublishStatus.Published, Title = "Lavanta Yağının 7 Faydası", SubTitle = "Doğanın en çok yönlü esansı", Content = "<p>Lavanta esansiyel yağı; uyku düzenini iyileştirir, kaygıyı azaltır, hafıza ağrısını dindirir, cilt tahrişini giderir, yara iyileşmesini hızlandırır, böcekleri uzaklaştırır ve saç büyümesini destekler.</p>", Image = "blog/lavanta.jpg", CatagoryId = cats[3].Id, AppUserId = userId, IsPublish = true, CreatedDate = DateTime.Now.AddDays(-13) },
                    new() { PublishStatus = EntityLayer.Enum.PublishStatus.Published, Title = "Difüzör Kullanımı Rehberi", SubTitle = "Odanızı aromaterapi cennetine çevirin", Content = "<p>Difüzör seçerken ultrasonik modeller tercih edin. Dam başına 3-5 damla yağ yeterlidir. Çay ağacı ve ökaliptüs kombinasyonu sabah, lavanta ve papatya karışımı gece için idealdir.</p>", Image = "blog/difuzor.jpg", CatagoryId = cats[3].Id, AppUserId = userId, IsPublish = true, CreatedDate = DateTime.Now.AddDays(-11) },

                    // Beslenme & Detoks (CatId 5)
                    new() { PublishStatus = EntityLayer.Enum.PublishStatus.Published, Title = "3 Günlük Bahar Detoks Programı", SubTitle = "İçinizi baharla temizleyin", Content = "<p>1. gün: Yalnızca sıvı gıdalar. 2. gün: Çiğ sebze ve meyve. 3. gün: Hafif pişirilmiş yiyecekler. Bol su, yeşil çay ve taze sıkılmış meyve suları tüm program boyunca serbesttir.</p>", Image = "blog/detoks.jpg", CatagoryId = cats[4].Id, AppUserId = userId, IsPublish = true, CreatedDate = DateTime.Now.AddDays(-10) },
                    new() { PublishStatus = EntityLayer.Enum.PublishStatus.Published, Title = "Kolajen Üretimini Artıran 8 Besin", SubTitle = "Mutfağınızdan güzellik reçetesi", Content = "<p>C vitamini (kivi, biber), çinko (kabak çekirdeği, yumurta), bakır (kakao, susam) ve glisin (kemik suyu, jelatin) kolajen sentezini destekler. Bu besinleri günlük diyetinize ekleyin.</p>", Image = "blog/kollajen.jpg", CatagoryId = cats[4].Id, AppUserId = userId, IsPublish = true, CreatedDate = DateTime.Now.AddDays(-9) },

                    // Hamam & Sauna (CatId 6)
                    new() { PublishStatus = EntityLayer.Enum.PublishStatus.Published, Title = "Türk Hamamı Deneyimi: Neler Beklemelisiniz?", SubTitle = "Geleneksel temizlik ritüeli", Content = "<p>Geleneksel Türk hamamında önce soyunma odası (camekan), ardından ılıklık (ılıcak) ve sıcaklık (hararet) bölümlerine geçilir. Kese ve köpük masajı cildi derinlemesine temizler.</p>", Image = "blog/hamam.jpg", CatagoryId = cats[5].Id, AppUserId = userId, IsPublish = true, CreatedDate = DateTime.Now.AddDays(-8) },
                    new() { PublishStatus = EntityLayer.Enum.PublishStatus.Published, Title = "Sauna ve Bağışıklık Sistemi İlişkisi", SubTitle = "Düzenli saunanın bilimsel kanıtları", Content = "<p>Haftada 2-3 kez yapılan sauna seanları, belediyenin bağışıklık hücrelerini aktive eder. Finlandiya'da yapılan uzun dönem araştırmalar kalp hastalığı riskini %40 azalttığını göstermiştir.</p>", Image = "blog/sauna.jpg", CatagoryId = cats[5].Id, AppUserId = userId, IsPublish = true, CreatedDate = DateTime.Now.AddDays(-7) },

                    // Anti-Aging (CatId 7)
                    new() { PublishStatus = EntityLayer.Enum.PublishStatus.Published, Title = "Retinol Kullanımının Püf Noktaları", SubTitle = "Yanlış kullanmaktan kaçının", Content = "<p>Retinol güçlü bir A vitamini türevidir. Başlangıçta haftada 2 gece kullanın. Güneş koruyucu olmadan gündüz uygulamayın. Hamilelikte kesinlikle kullanmayın. 6-8 haftada sonuç görürsünüz.</p>", Image = "blog/retinol.jpg", CatagoryId = cats[6].Id, AppUserId = userId, IsPublish = true, CreatedDate = DateTime.Now.AddDays(-6) },
                    new() { PublishStatus = EntityLayer.Enum.PublishStatus.Published, Title = "Gözaltı Kırışıklıklarına Karşı Doğal Yöntemler", SubTitle = "Kimyasal olmadan genç görünün", Content = "<p>Soğuk papatya çayı poşeti, buz küresi masajı ve göz çevresi için özel jeller gözaltı kırışıklıklarını belirgin biçimde azaltır. Uyku düzeni ve güneş koruyucu en büyük destekçinizdir.</p>", Image = "blog/gozalti.jpg", CatagoryId = cats[6].Id, AppUserId = userId, IsPublish = true, CreatedDate = DateTime.Now.AddDays(-5) },

                    // Saç Bakımı (CatId 8)
                    new() { PublishStatus = EntityLayer.Enum.PublishStatus.Published, Title = "Keratin Tedavisi Nedir ve Ne Kadar Sürer?", SubTitle = "Pürüzsüz saçların sırrı", Content = "<p>Keratin tedavisi, saçın dış katmanını doldurarak friziness ve kıvrımı azaltır. Teknik olarak 2-4 saat sürer ve 3-5 ay kalıcı olabilir. Sülfatsız şampuan kullanımı ömrü uzatır.</p>", Image = "blog/keratin.jpg", CatagoryId = cats[7].Id, AppUserId = userId, IsPublish = true, CreatedDate = DateTime.Now.AddDays(-4) },
                    new() { PublishStatus = EntityLayer.Enum.PublishStatus.Published, Title = "Hindistan Cevizi Yağının Saça Faydaları", SubTitle = "Mutfak malzemesiyle saç bakımı", Content = "<p>Saf hindistan cevizi yağı, saç teline nüfuz ederek protein kaybını önler. Yıkama öncesi maske olarak 30 dakika uygulamak yeterlidir. Yağlı saçlar için ucuçlardan köklere gidin.</p>", Image = "blog/hindistan-cevizi-yag.jpg", CatagoryId = cats[7].Id, AppUserId = userId, IsPublish = true, CreatedDate = DateTime.Now.AddDays(-3) },

                    // Yoga & Pilates (CatId 9)
                    new() { PublishStatus = EntityLayer.Enum.PublishStatus.Published, Title = "Sabah Yogası: 10 Dakikada Güne Başlangıç", SubTitle = "Uyku sersemliğini hızla atın", Content = "<p>Yataktan kalkar kalkmaz; kedi-inek hareketi, çocuk pozu ve güneş selamlama serisi kan dolaşımını hızlandırır ve enerji verir. DVD ya da uygulama gerekmez, birkaç mat yeterli.</p>", Image = "blog/sabah-yoga.jpg", CatagoryId = cats[8].Id, AppUserId = userId, IsPublish = true, CreatedDate = DateTime.Now.AddDays(-2) },
                    new() { PublishStatus = EntityLayer.Enum.PublishStatus.Published, Title = "Pilates ve Duruş Bozukluğu", SubTitle = "Masa başı çalışanlar için çözüm", Content = "<p>Pilates core kaslarını güçlendirerek omurga desteğini artırır. Haftada 3 seans düzenli pilates, 3 ay içinde postür bozukluğunu gözle görülür şekilde iyileştirir.</p>", Image = "blog/pilates-durus.jpg", CatagoryId = cats[8].Id, AppUserId = userId, IsPublish = true, CreatedDate = DateTime.Now.AddDays(-1) },

                    // Genel Sağlık (CatId 10)
                    new() { PublishStatus = EntityLayer.Enum.PublishStatus.Published, Title = "Düzenli Su İçmenin Cilde Etkisi", SubTitle = "En ucuz güzellik sırrı", Content = "<p>Günlük 2-2,5 litre su içmek; cilt nem dengesini korur, gözeneklerin görünümünü azaltır ve toksik maddelerin atılımını hızlandırır. Sabah aç karnına içilen 1 bardak ılık su özellikle faydalıdır.</p>", Image = "blog/su-icleme.jpg", CatagoryId = cats[9].Id, AppUserId = userId, IsPublish = true, CreatedDate = DateTime.Now },
                    new() { PublishStatus = EntityLayer.Enum.PublishStatus.Published, Title = "Uyku Kalitesi Cilt Sağlığını Nasıl Etkiler?", SubTitle = "Güzellik uykusu gerçek mi?", Content = "<p>Uyku sırasında salgılanan büyüme hormonu hücre yenilenmesini tetikler. 7-9 saat kaliteli uyku alan bireylerin ciltleri %30 daha verimli onarılır. Yan uyumak kırışıklık oluşturabilir; sırtüstü uyumak önerilir.</p>", Image = "blog/uyku-cilt.jpg", CatagoryId = cats[9].Id, AppUserId = userId, IsPublish = true, CreatedDate = DateTime.Now },
                    new() { PublishStatus = EntityLayer.Enum.PublishStatus.Published, Title = "Egzersiz Sonrası Cilt Bakımı Rutini", SubTitle = "Terin ardından ne yapmalısınız", Content = "<p>Egzersiz sonrası ilk 20 dakika içinde yüzünüzü yıkayın. Gözenekleri tıkamayan hafif nemlendirici kullanın. Ter birikimi akne ve cilt tahrişine yol açar; bu nedenle temizlik önemlidir.</p>", Image = "blog/egzersiz-cilt.jpg", CatagoryId = cats[0].Id, AppUserId = userId, IsPublish = true, CreatedDate = DateTime.Now },
                    new() { PublishStatus = EntityLayer.Enum.PublishStatus.Published, Title = "Stres ve Cilt: Kortizolün Etkisi", SubTitle = "Stres neden sivilce yapar?", Content = "<p>Kortizol hormonu yağ bezlerini uyararak sebum üretimini artırır. Kronik stres; akne, egzema ve sedef hastalığını tetikleyebilir. Meditasyon ve egzersiz kortizolü doğal yolla düşürür.</p>", Image = "blog/stres-cilt.jpg", CatagoryId = cats[0].Id, AppUserId = userId, IsPublish = true, CreatedDate = DateTime.Now },
                    new() { PublishStatus = EntityLayer.Enum.PublishStatus.Published, Title = "Güneş Koruyucu Her Mevsim Şart mı?", SubTitle = "Bulutlu günlerde de UV var", Content = "<p>UVA ışınları bulutları geçer ve cam camı deler. Kış ve ilkbaharda da SPF 30+ kullanmak erken yaşlanmayı ve cilt kanserini önler. Kimyasal filtreler güneş öncesi, mineral filtreler hemen uygulanabilir.</p>", Image = "blog/gunes-koruyucu.jpg", CatagoryId = cats[0].Id, AppUserId = userId, IsPublish = true, CreatedDate = DateTime.Now },
                    new() { PublishStatus = EntityLayer.Enum.PublishStatus.Published, Title = "Spa Öncesi Yapılmaması Gerekenler", SubTitle = "Seans veriminizi artırın", Content = "<p>Spa randevusundan 24 saat önce tüy alma işlemi yaptırmayın. Ağır yemek yemeyin, alkol almayın. Yoğun egzersiz yapmaktan kaçının. Bu önlemler hem konforunuzu artırır hem de ürünlerin emilimini iyileştirir.</p>", Image = "blog/spa-oncesi.jpg", CatagoryId = cats[1].Id, AppUserId = userId, IsPublish = true, CreatedDate = DateTime.Now },
                    new() { PublishStatus = EntityLayer.Enum.PublishStatus.Published, Title = "Hamilelikte Güvenli Spa Hizmetleri", SubTitle = "Anneye uygun dokunuşlar", Content = "<p>Hamileliğin 2. trimesterinden itibaren prenatal masaj güvenlidir; 45 dakikayı geçmemeli ve yüzüstü pozisyon kesinlikle kullanılmamalıdır. Esansiyel yağlar için doktor onayı alın.</p>", Image = "blog/hamilelik-spa.jpg", CatagoryId = cats[2].Id, AppUserId = userId, IsPublish = true, CreatedDate = DateTime.Now },
                    new() { PublishStatus = EntityLayer.Enum.PublishStatus.Published, Title = "İlk Kez Spa'ya Gidenler İçin Rehber", SubTitle = "Ne giyilir, ne beklenir?", Content = "<p>İlk spa ziyaretinizde rahat iç çamaşırı giyin; tesis genellikle havlu sağlar. Değerli takı ve elektronik cihazlarınızı evde bırakın. Seans öncesi 15 dakika erken gelinmesi önerilir.</p>", Image = "blog/ilk-spa.jpg", CatagoryId = cats[2].Id, AppUserId = userId, IsPublish = true, CreatedDate = DateTime.Now },
                };
                foreach (var blog in blogs) blog.Slug = WebUI.Helpers.GlobalHelper.ToSlug(blog.Title ?? "");
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
                    Phone = "+90 538 878 70 00",
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

            // Repair missing media and formerly shared editorial paths; valid custom uploads are preserved.
            var media = scope.ServiceProvider.GetRequiredService<WebUI.Services.PublicMediaService>();
            foreach (var contact in context.Contacts.Where(c => c.Phone != "+90 538 878 70 00"))
                contact.Phone = "+90 538 878 70 00";
            var assignedEditorialImages = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var blog in context.Blogs.OrderBy(b => b.Id))
            {
                var image = media.BlogImage(blog.Title, blog.Image, blog.Id);
                if (image.StartsWith("/img/", StringComparison.Ordinal) && !assignedEditorialImages.Add(image))
                    image = WebUI.Services.PublicMediaService.Placeholder("blog", blog.Id, blog.Title);
                blog.Image = image.TrimStart('/');
            }
            foreach (var service in context.Services.OrderBy(s => s.Id))
            {
                var image = media.ServiceImage(service.Title, service.Image, service.Id);
                if (image.StartsWith("/img/", StringComparison.Ordinal) && !assignedEditorialImages.Add(image))
                    image = WebUI.Services.PublicMediaService.Placeholder("service", service.Id, service.Title);
                service.Image = image.TrimStart('/');
            }
            var slideIndex = 0;
            foreach (var slide in context.Sliders.OrderBy(s => s.Id))
                slide.Image = media.Image(slide.Image, $"img/hero-{++slideIndex % 3 switch { 1 => 1, 2 => 2, _ => 3 }}.webp").TrimStart('/');
            foreach (var gallery in context.Galaries.OrderBy(g => g.Id))
            {
                var image = media.GalleryImage(gallery.Title, gallery.Image, gallery.Id);
                if (image.StartsWith("/img/", StringComparison.Ordinal) && !assignedEditorialImages.Add(image))
                    image = WebUI.Services.PublicMediaService.Placeholder("gallery", gallery.Id, gallery.Title);
                gallery.Image = image.TrimStart('/');
            }
            foreach (var about in context.Abouts)
                about.Image = media.Image(about.Image, "img/about-retreat.webp").TrimStart('/');
            await context.SaveChangesAsync();
        }
    }
}
