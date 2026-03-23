
// Initialize AOS
AOS.init({
    duration: 1000,
    once: true
});

Fancybox.bind("[data-fancybox]", {
});

//Custom Toogle 
const btn = document.querySelector(".menu-btn");
const navbar = document.querySelector("#navbarNav");

btn.addEventListener("click", function () {
    btn.classList.toggle("active");
});

// Opsiyonel: Menü kapandığında animasyonu geri al
navbar.addEventListener("hidden.bs.collapse", function () {
    btn.classList.remove("active");
});

navbar.addEventListener("shown.bs.collapse", function () {
    btn.classList.add("active");
});

// Theme toggle functionality
function toggleTheme() {
    const html = document.documentElement;
    const currentTheme = html.getAttribute('data-theme');
    const newTheme = currentTheme === 'light' ? 'dark' : 'light';

    html.setAttribute('data-theme', newTheme);

    // Update toggle button text and icon
    const themeText = document.querySelector('.theme-text');
    const themeIcon = document.querySelector('.theme-toggle i');

    if (newTheme === 'dark') {
        themeIcon.className = 'fas fa-moon';
    } else {
        themeIcon.className = 'fas fa-sun';
    }

    // Save preference
    localStorage.setItem('theme', newTheme);
}

// Load saved theme
document.addEventListener('DOMContentLoaded', function () {
    const savedTheme = localStorage.getItem('theme') || 'light';
    const html = document.documentElement;
    const themeText = document.querySelector('.theme-text');
    const themeIcon = document.querySelector('.theme-toggle i');

    html.setAttribute('data-theme', savedTheme);

    if (savedTheme === 'dark') {
        themeIcon.className = 'fas fa-moon';
    } else {
        themeIcon.className = 'fas fa-sun';
    }
});

// Navbar scroll effect
window.addEventListener('scroll', function () {
    const navbar = document.querySelector('.navbar');
    if (window.scrollY > 50) {
        navbar.classList.add('scrolled');
    } else {
        navbar.classList.remove('scrolled');
    }
});


// Aktif Nav Link Belirleme 
const links = document.querySelectorAll('.navbar-nav .nav-link');
const currentPath = window.location.pathname.toLowerCase();

links.forEach(link => {
    const linkPath = new URL(link.href).pathname.toLowerCase();
    if (linkPath === currentPath) {
        links.forEach(l => l.classList.remove('active')); // öncekini sil
        link.classList.add('active'); // mevcut linke ekle
    }
});

function createParticle() {
    const particle = document.createElement('div');
    particle.style.cssText = `
    position: fixed;
    width: 4px;
    height: 4px;
    background: var(--primary-gold);
    border-radius: 50%;
    pointer-events: none;
    z-index: 1;
    left: ${Math.random() * 100}vw;
    animation: particle-float 6s linear infinite;
    `;

    document.body.appendChild(particle);

    setTimeout(() => {
        if (document.body.contains(particle)) {
            document.body.removeChild(particle);
        }
    }, 6000);
}

// Create particles periodically
setInterval(createParticle, 3000);

// Loading animation for elements
const observerOptions = {
    threshold: 0.1,
    rootMargin: '0px 0px -50px 0px'
};

const observer = new IntersectionObserver(function (entries) {
    entries.forEach(entry => {
        if (entry.isIntersecting) {
            entry.target.classList.add('visible');
        }
    });
}, observerOptions);

// Observe all fade-in elements
document.querySelectorAll('.fade-in').forEach(el => {
    observer.observe(el);
});

// ===== AJAX & SweetAlert2 Modernization =====

// Global AJAX Form Handler
$(document).on('submit', '.ajax-form', function (e) {
    e.preventDefault();
    const form = $(this);
    const url = form.attr('action');
    const formData = new FormData(this);

    Swal.fire({
        title: 'İşlem yapılıyor...',
        allowOutsideClick: false,
        didOpen: () => {
            Swal.showLoading();
        }
    });

    $.ajax({
        url: url,
        type: 'POST',
        data: formData,
        contentType: false,
        processData: false,
        success: function (response) {
            Swal.close();
            if (response.success) {
                Swal.fire({
                    icon: 'success',
                    title: 'Başarılı!',
                    text: response.message,
                    timer: 2000,
                    showConfirmButton: false
                }).then(() => {
                    if (form.data('redirect')) {
                        window.location.href = form.data('redirect');
                    } else {
                        location.reload();
                    }
                });
            } else {
                Swal.fire({
                    icon: 'error',
                    title: 'Hata!',
                    text: response.message || 'Bir hata oluştu.'
                });
            }
        },
        error: function () {
            Swal.close();
            Swal.fire({
                icon: 'error',
                title: 'Hata!',
                text: 'Sunucu ile iletişim kurulurken bir hata oluştu.'
            });
        }
    });
});

// Global SweetAlert2 Delete Handler
$(document).on('click', '.btn-delete-ajax', function (e) {
    e.preventDefault();
    const btn = $(this);
    const url = btn.attr('href') || btn.data('url');
    const title = btn.data('title') || 'Bu veriyi silmek istediğinize emin misiniz?';

    Swal.fire({
        title: 'Emin misiniz?',
        text: title,
        icon: 'warning',
        showCancelButton: true,
        confirmButtonColor: '#d33',
        cancelButtonColor: '#3085d6',
        confirmButtonText: 'Evet, sil!',
        cancelButtonText: 'İptal'
    }).then((result) => {
        if (result.isConfirmed) {
            $.ajax({
                url: url,
                type: 'POST',
                data: {
                    __RequestVerificationToken: $('input[name="__RequestVerificationToken"]').val()
                },
                success: function (response) {
                    if (response.success) {
                        Swal.fire(
                            'Silindi!',
                            response.message,
                            'success'
                        ).then(() => {
                            location.reload();
                        });
                    } else {
                        Swal.fire(
                            'Hata!',
                            response.message,
                            'error'
                        );
                    }
                },
                error: function () {
                    Swal.fire(
                        'Hata!',
                        'Sunucu ile iletişim kurulurken bir hata oluştu.',
                        'error'
                    );
                }
            });
        }
    });
});