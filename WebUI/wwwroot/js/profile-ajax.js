(() => {
    'use strict';
    document.addEventListener('submit', async event => {
        const form = event.target.closest('[data-remove-favorite]'); if (!form) return;
        event.preventDefault(); const profile = form.closest('.luxe-profile'); if (profile.dataset.busy) return;
        const ui = window.XspaInteractions, button = form.querySelector('button');
        profile.dataset.busy = 'true'; form.setAttribute('aria-busy', 'true'); button.disabled = true;
        try {
            const data = await ui.send(form);
            if (data.isFavorite) throw new Error(ui.text('Favori kaldırılamadı.', 'Could not remove favorite.'));
            const card = form.closest('[data-favorite-card]'); card.classList.add('xspa-leave');
            await new Promise(resolve => setTimeout(resolve, window.matchMedia('(prefers-reduced-motion: reduce)').matches ? 0 : 250));
                card.remove();
                document.querySelector('[data-favorites-total]').textContent = data.remainingCount;
                const empty = document.querySelector('[data-favorites-empty]');
                if (!document.querySelector('[data-favorite-card]')) {
                    empty.hidden = false; empty.classList.add('xspa-enter');
                    empty.querySelector('[data-favorites-first-page]').hidden = data.remainingCount === 0;
                    empty.querySelector('[data-favorites-empty-label]').textContent = data.remainingCount === 0
                        ? ui.text('Henüz favori yazınız yok.', 'You have no favorite articles yet.')
                        : ui.text('Bu sayfadaki favoriler kaldırıldı.', 'All favorites on this page have been removed.');
                }
            ui.toast(data.message);
        } catch (error) { ui.toast(error.message, true); button.disabled = false; }
        finally { delete profile.dataset.busy; form.removeAttribute('aria-busy'); }
    });
})();
