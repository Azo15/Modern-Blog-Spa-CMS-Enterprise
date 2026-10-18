-- Approved XSPA service catalog, exported from local database. No accounts or private data.
-- Insert missing titles only; repeat execution is safe. Existing services are never modified.
SET XACT_ABORT ON;
BEGIN TRANSACTION;
DECLARE @inserted int = 0;
IF NOT EXISTS (SELECT 1 FROM dbo.Services WITH (UPDLOCK, HOLDLOCK) WHERE Title = N'Klasik İsveç Masajı')
BEGIN
INSERT dbo.Services (Title,Content,Details,Image,Prominence,IsPublish,IsDeleted,CreatedDate,CreatedBy) VALUES (N'Klasik İsveç Masajı',N'60 dk — 650 ₺',N'Kas gerginliğini gideren, kan dolaşımını artıran rahatlatıcı masaj. Aromaterapi yağları ile uygulanır.',N'img/service-isvec.webp',1,1,0,SYSUTCDATETIME(),N'catalog-transfer-20261018');
SET @inserted += 1;
END;
IF NOT EXISTS (SELECT 1 FROM dbo.Services WITH (UPDLOCK, HOLDLOCK) WHERE Title = N'Türk Hamamı Ritüeli')
BEGIN
INSERT dbo.Services (Title,Content,Details,Image,Prominence,IsPublish,IsDeleted,CreatedDate,CreatedBy) VALUES (N'Türk Hamamı Ritüeli',N'90 dk — 850 ₺',N'Geleneksel kese ve köpük masajı ile derin temizlik. Cildiniz ipek gibi olacak.',N'img/service-hamam.webp',1,1,0,SYSUTCDATETIME(),N'catalog-transfer-20261018');
SET @inserted += 1;
END;
IF NOT EXISTS (SELECT 1 FROM dbo.Services WITH (UPDLOCK, HOLDLOCK) WHERE Title = N'Altın Yüz Maskesi')
BEGIN
INSERT dbo.Services (Title,Content,Details,Image,Prominence,IsPublish,IsDeleted,CreatedDate,CreatedBy) VALUES (N'Altın Yüz Maskesi',N'45 dk — 950 ₺',N'24 ayar altın özlü maske ile cilt tonu eşitlemesi ve anti-aging etkisi.',N'img/service-altinmaske.webp',1,1,0,SYSUTCDATETIME(),N'catalog-transfer-20261018');
SET @inserted += 1;
END;
IF NOT EXISTS (SELECT 1 FROM dbo.Services WITH (UPDLOCK, HOLDLOCK) WHERE Title = N'Derin Doku Masajı')
BEGIN
INSERT dbo.Services (Title,Content,Details,Image,Prominence,IsPublish,IsDeleted,CreatedDate,CreatedBy) VALUES (N'Derin Doku Masajı',N'75 dk — 750 ₺',N'Kronik kas ağrıları ve spazm için profesyonel derin doku tekniği.',N'img/service-derindoku.webp',1,1,0,SYSUTCDATETIME(),N'catalog-transfer-20261018');
SET @inserted += 1;
END;
IF NOT EXISTS (SELECT 1 FROM dbo.Services WITH (UPDLOCK, HOLDLOCK) WHERE Title = N'Aromaterapi Masajı')
BEGIN
INSERT dbo.Services (Title,Content,Details,Image,Prominence,IsPublish,IsDeleted,CreatedDate,CreatedBy) VALUES (N'Aromaterapi Masajı',N'60 dk — 700 ₺',N'Doğal esansiyel yağlarla stres gidericiden uyku düzenleyiciye tüm ihtiyaçlar için.',N'img/service-aromaterapi.webp',1,1,0,SYSUTCDATETIME(),N'catalog-transfer-20261018');
SET @inserted += 1;
END;
IF NOT EXISTS (SELECT 1 FROM dbo.Services WITH (UPDLOCK, HOLDLOCK) WHERE Title = N'Çiftler Masajı')
BEGIN
INSERT dbo.Services (Title,Content,Details,Image,Prominence,IsPublish,IsDeleted,CreatedDate,CreatedBy) VALUES (N'Çiftler Masajı',N'90 dk — 1.400 ₺',N'Özel VIP odamızda sevdiğinizle birlikte masaj keyfi. Şampanya ikram edilir.',N'img/service-ciftler.webp',1,1,0,SYSUTCDATETIME(),N'catalog-transfer-20261018');
SET @inserted += 1;
END;
IF NOT EXISTS (SELECT 1 FROM dbo.Services WITH (UPDLOCK, HOLDLOCK) WHERE Title = N'Refleksoloji')
BEGIN
INSERT dbo.Services (Title,Content,Details,Image,Prominence,IsPublish,IsDeleted,CreatedDate,CreatedBy) VALUES (N'Refleksoloji',N'45 dk — 550 ₺',N'Ayak basınç noktalarından vücudun denge merkezini uyarma tekniği.',N'img/service-refleksoloji.webp',1,1,0,SYSUTCDATETIME(),N'catalog-transfer-20261018');
SET @inserted += 1;
END;
IF NOT EXISTS (SELECT 1 FROM dbo.Services WITH (UPDLOCK, HOLDLOCK) WHERE Title = N'Sauna & Buhar Odası')
BEGIN
INSERT dbo.Services (Title,Content,Details,Image,Prominence,IsPublish,IsDeleted,CreatedDate,CreatedBy) VALUES (N'Sauna & Buhar Odası',N'60 dk — 400 ₺',N'Finlandiya tipi kuru sauna + Türk buhar odası kombinasyonu.',N'img/service-sauna.webp',1,1,0,SYSUTCDATETIME(),N'catalog-transfer-20261018');
SET @inserted += 1;
END;
IF NOT EXISTS (SELECT 1 FROM dbo.Services WITH (UPDLOCK, HOLDLOCK) WHERE Title = N'Keratin Saç Bakımı')
BEGIN
INSERT dbo.Services (Title,Content,Details,Image,Prominence,IsPublish,IsDeleted,CreatedDate,CreatedBy) VALUES (N'Keratin Saç Bakımı',N'120 dk — 1.200 ₺',N'Profesyonel keratin tedavisi ile pürüzsüz, parlak ve frizden arındırılmış saçlar.',N'img/service-keratin.webp',1,1,0,SYSUTCDATETIME(),N'catalog-transfer-20261018');
SET @inserted += 1;
END;
IF NOT EXISTS (SELECT 1 FROM dbo.Services WITH (UPDLOCK, HOLDLOCK) WHERE Title = N'Cilt Peeling Seansı')
BEGIN
INSERT dbo.Services (Title,Content,Details,Image,Prominence,IsPublish,IsDeleted,CreatedDate,CreatedBy) VALUES (N'Cilt Peeling Seansı',N'50 dk — 600 ₺',N'Kimyasal peeling ile ölü deri temizleme, cilt yenileme ve ton eşitleme.',N'img/service-peeling.webp',1,1,0,SYSUTCDATETIME(),N'catalog-transfer-20261018');
SET @inserted += 1;
END;
IF NOT EXISTS (SELECT 1 FROM dbo.Services WITH (UPDLOCK, HOLDLOCK) WHERE Title = N'Bebek Masajı Kursu')
BEGIN
INSERT dbo.Services (Title,Content,Details,Image,Prominence,IsPublish,IsDeleted,CreatedDate,CreatedBy) VALUES (N'Bebek Masajı Kursu',N'45 dk — 350 ₺',N'Ebeveynlere bebek masajı teknikleri öğretilir.',N'img/service-bebek.webp',0,1,0,SYSUTCDATETIME(),N'catalog-transfer-20261018');
SET @inserted += 1;
END;
IF NOT EXISTS (SELECT 1 FROM dbo.Services WITH (UPDLOCK, HOLDLOCK) WHERE Title = N'Hamilelik Masajı')
BEGIN
INSERT dbo.Services (Title,Content,Details,Image,Prominence,IsPublish,IsDeleted,CreatedDate,CreatedBy) VALUES (N'Hamilelik Masajı',N'60 dk — 700 ₺',N'2. trimesterden itibaren uygulanan, anneye özel prenatal masaj.',N'img/service-hamilelik.webp',0,1,0,SYSUTCDATETIME(),N'catalog-transfer-20261018');
SET @inserted += 1;
END;
COMMIT TRANSACTION;
SELECT @inserted AS InsertedServices;
SELECT Id,Title,Content,Image FROM dbo.Services WHERE IsPublish=1 AND IsDeleted=0 ORDER BY Id;
