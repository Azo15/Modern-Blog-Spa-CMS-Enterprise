
// Contact form submission
document.querySelector('form').addEventListener('submit', function (e) {
    e.preventDefault();

    // Get form data
    const formData = new FormData(this);
    const name = document.getElementById('name').value;
    const email = document.getElementById('email').value;
    const phone = document.getElementById('phone').value;
    const service = document.getElementById('service').value;
    const message = document.getElementById('message').value;

    // Simple validation
    if (!name || !email || !phone || !service) {
        alert('Lütfen tüm zorunlu alanları doldurunuz.');
        return;
    }

    // Simulate form submission
    alert('Randevu talebiniz başarıyla gönderildi! En kısa sürede sizinle iletişime geçeceğiz.');
    this.reset();
});