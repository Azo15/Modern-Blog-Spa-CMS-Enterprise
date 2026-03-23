// theme-toggle.js

document.addEventListener('DOMContentLoaded', () => {

    // 1️ İlk önce sayfa açıldığında temayı uygula
    const savedTheme = localStorage.getItem('theme') || 'light';
    document.documentElement.setAttribute('data-bs-theme', savedTheme);

    // 2️ Şimdi TinyMCE'yi başlat
    function getCurrentTheme() {
        return document.documentElement.getAttribute('data-bs-theme') || 'light';
    }

    function initTinyMCE() {
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
        localStorage.setItem('theme', newTheme);
        updateThemeIcon(newTheme);
        initTinyMCE(); // Tema değiştiğinde TinyMCE'yi yeniden başlat
    });

    // Sistem değişimi dinleme
    const prefersDarkMode = window.matchMedia('(prefers-color-scheme: dark)');
    prefersDarkMode.addEventListener('change', (e) => {
        if (!localStorage.getItem('theme')) { // Sadece kullanıcı elle tema seçmemişse sistem temasını uygula
            const newSystemTheme = e.matches ? 'dark' : 'light';
            document.documentElement.setAttribute('data-bs-theme', newSystemTheme);
            updateThemeIcon(newSystemTheme);
            initTinyMCE(); // Sistem teması değiştiğinde TinyMCE'yi yeniden başlat
        }
    });
});