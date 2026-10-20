(() => {
    'use strict';
    document.addEventListener('submit', async event => {
        const form = event.target.closest('[data-cancel-appointment],[data-edit-appointment]'); if (!form) return;
        event.preventDefault(); if (form.dataset.busy || !form.reportValidity()) return;
        const ui = window.XspaInteractions, buttons = [...form.querySelectorAll('button')];
        form.dataset.busy = 'true'; form.setAttribute('aria-busy', 'true'); buttons.forEach(button => button.disabled = true);
        let status = form.querySelector('[data-booking-result]');
        if (!status) { status = document.createElement('p'); status.dataset.bookingResult = ''; status.setAttribute('role', 'status'); form.append(status); }
        try {
            const data = await ui.send(form); ui.toast(data.message);
            if (form.hasAttribute('data-cancel-appointment')) {
                const card = form.closest('.booking-card'), badge = card.querySelector('.booking-status');
                badge.className = 'booking-status booking-status-cancelled xspa-enter'; badge.textContent = ui.text('İptal Edildi', 'Cancelled'); badge.setAttribute('role', 'status');
                card.querySelector('.booking-actions')?.remove(); card.querySelector('.booking-cancel-confirm')?.remove();
            } else {
                form.elements.ExpectedUpdatedDate.value = data.updatedDate || '';
                status.className = 'booking-notice xspa-enter'; status.textContent = data.message;
            }
        } catch (error) {
            status.className = 'booking-errors';
            status.textContent = [error.message, ...Object.values(error.errors || {}).flat()].join(' '); ui.toast(error.message, true);
        } finally { delete form.dataset.busy; form.removeAttribute('aria-busy'); buttons.forEach(button => button.disabled = false); }
    });
})();
