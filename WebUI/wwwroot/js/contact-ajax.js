(() => {
    'use strict';
    document.addEventListener('submit', async event => {
        const form = event.target.closest('[data-contact-form],[data-newsletter-form]'); if (!form) return;
        event.preventDefault(); if (form.dataset.busy || !form.reportValidity()) return;
        const ui = window.XspaInteractions, button = form.querySelector('[type="submit"]');
        let status = form.querySelector('[data-newsletter-status],[data-contact-status]');
        if (!status) { status = document.createElement('p'); status.dataset.contactStatus = ''; status.setAttribute('role', 'status'); form.append(status); }
        form.dataset.busy = 'true'; form.setAttribute('aria-busy', 'true'); if (button) button.disabled = true;
        status.textContent = ui.text('Talebiniz kaydediliyor…', 'Saving your request…'); status.dataset.state = 'pending';
        form.querySelectorAll('[data-valmsg-for]').forEach(node => node.textContent = '');
        try {
            const data = await ui.send(form); form.reset(); status.textContent = data.message; status.dataset.state = 'success';
            if (window.Swal) await window.Swal.fire({ icon: 'success', title: ui.text('Teşekkür ederiz', 'Thank you'), text: data.message,
                confirmButtonText: ui.text('Tamam', 'Continue'), customClass: { popup: 'luxe-alert', confirmButton: 'luxe-alert-confirm' } });
            else ui.toast(data.message);
        } catch (error) {
            status.textContent = error.message; status.dataset.state = 'error';
            form.querySelectorAll('[data-valmsg-for]').forEach(node => {
                const errors = error.errors?.[node.dataset.valmsgFor];
                if (errors) node.textContent = Array.isArray(errors) ? errors.join(' ') : String(errors);
            });
            ui.toast(error.message, true);
        } finally {
            delete form.dataset.busy; form.removeAttribute('aria-busy'); if (button) button.disabled = false;
            if (form.querySelector('.g-recaptcha') && window.grecaptcha?.reset) window.grecaptcha.reset();
        }
    });
})();
