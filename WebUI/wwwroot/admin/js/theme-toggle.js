// theme-toggle.js

document.addEventListener('DOMContentLoaded', () => {

    // 1️ İlk önce sayfa açıldığında temayı uygula
    let savedTheme = 'dark';
    try { savedTheme = localStorage.getItem('xspa-admin-theme') === 'light' ? 'light' : 'dark'; } catch { }
    document.documentElement.setAttribute('data-bs-theme', savedTheme);

    // 2️ Şimdi TinyMCE'yi başlat
    function getCurrentTheme() {
        return document.documentElement.getAttribute('data-bs-theme') || 'light';
    }

    function initTinyMCE() {
        if (typeof tinymce === 'undefined' || !document.getElementById('tinymce')) return;
        tinymce.triggerSave();
        // Mevcut editörleri kaldır (yeniden başlatma için önemli)
        tinymce.remove('#tinymce'); // Selector'ünüzü burada da belirtmek daha güvenli

        const theme = getCurrentTheme();
        const isDarkMode = theme === 'dark';

        tinymce.init({
            selector: '#tinymce',

            base_url: '/admin/vendor/tinymce/', // TinyMCE ana klasörünün sunucu köküne göre yolu.
            suffix: '.min',                    // '.min.js', '.min.css' gibi küçültülmüş dosyaları kullanmasını söyler.
            plugins: 'accordion advlist anchor autolink autosave charmap code codesample directionality emoticons fullscreen help image importcss insertdatetime link lists media nonbreaking pagebreak preview quickbars save searchreplace table visualblocks visualchars wordcount',
            toolbar: 'undo redo | blocks fontfamily fontsize | bold italic underline strikethrough | link image media table | align lineheight | numlist bullist indent outdent | emoticons charmap | removeformat',

            skin: isDarkMode ? 'oxide-dark' : 'oxide',
            content_css: isDarkMode ? 'dark' : 'default',
            content_style: isDarkMode ? 'body { background: #091e17; color: #f3eee1; } a { color: #e2c57e; }' : 'body { background: #fffdf7; color: #16382b; } a { color: #775b20; }',

            // İsteğe bağlı: Menü çubuğunun ve araç çubuklarının yapışkan olmasını isterseniz
            //menubar: 'file edit view insert format tools table help',
            promotion: false, // TinyMCE Cloud reklamını kaldırır (Self-hosted için geçerli)
            height: 500, // Varsayılan yükseklik
        });
    }

    initTinyMCE(); // Tema ayarlandıktan sonra başlatıyoruz

    // Tema değiştirme butonu
    const themeToggleBtn = document.getElementById('theme-toggle');
    if (!themeToggleBtn) return;

    const themeIcon = themeToggleBtn.querySelector('i');
    if (!themeIcon) return;

    const updateThemeIcon = (theme) => {
        if (theme === 'dark') {
            themeIcon.className = 'fa-solid fa-sun icon-md';
        } else {
            themeIcon.className = 'fa-solid fa-moon icon-md';
        }
    };

    updateThemeIcon(savedTheme); // İlk ikon durumunu ayarla

    themeToggleBtn.addEventListener('click', () => {
        const currentTheme = document.documentElement.getAttribute('data-bs-theme');
        const newTheme = currentTheme === 'dark' ? 'light' : 'dark';

        document.documentElement.setAttribute('data-bs-theme', newTheme);
        try { localStorage.setItem('xspa-admin-theme', newTheme); } catch { }
        updateThemeIcon(newTheme);
        initTinyMCE(); // Tema değiştiğinde TinyMCE'yi yeniden başlat
    });

});
