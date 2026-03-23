window.addEventListener('DOMContentLoaded', () => {
    const rememberCheckbox = document.getElementById('RememberMe');
    const emailInput = document.getElementById('Email');
    const loginForm = document.getElementById('loginForm');
    const loginBtn = document.getElementById('loginBtn');

    if (rememberCheckbox && emailInput) {
        const rememberedEmail = localStorage.getItem('rememberedEmail');
        if (rememberedEmail) {
            emailInput.value = rememberedEmail;
            rememberCheckbox.checked = true;
        }
    }

    if (loginForm && loginBtn) {
        loginForm.addEventListener('submit', function (event) {
            if (typeof $ !== 'undefined' && $(loginForm).length > 0 && typeof $(loginForm).valid === 'function') {
                if (!$(loginForm).valid()) {
                    if (loginBtn) {
                        loginBtn.disabled = false;
                        loginBtn.textContent = "Giriş Yap";
                    }
                    return;
                }
            }
            if (loginBtn) {
                loginBtn.disabled = true;
                loginBtn.textContent = "Giriş yapılıyor...";
            }

            if (rememberCheckbox && emailInput) {
                if (rememberCheckbox.checked) {
                    localStorage.setItem('rememberedEmail', emailInput.value);
                } else {
                    localStorage.removeItem('rememberedEmail');
                }
            }
        });
    }
});

function togglePassword(id, icon) {
    const input = document.getElementById(id);
    if (!input) return;
    const isPassword = input.type === "password";
    input.type = isPassword ? "text" : "password";
    icon.classList.toggle("fa-eye");
    icon.classList.toggle("fa-eye-slash");
}