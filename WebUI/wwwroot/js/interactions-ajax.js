(() => {
    'use strict';
    const en = document.documentElement.lang.startsWith('en');
    const text = (tr, english) => en ? english : tr;
    function toast(message, error = false) {
        let region = document.querySelector('[data-xspa-toasts]');
        if (!region) {
            region = document.createElement('div'); region.dataset.xspaToasts = '';
            region.className = 'xspa-toasts'; region.setAttribute('aria-live', 'polite'); document.body.append(region);
        }
        const item = document.createElement('div'); item.className = 'xspa-toast' + (error ? ' is-error' : '');
        item.textContent = message; region.append(item); setTimeout(() => item.remove(), 6000);
    }
    async function send(form, body = new FormData(form)) {
        const response = await fetch(form.action, { method: 'POST', body, credentials: 'same-origin',
            headers: { 'X-Requested-With': 'XMLHttpRequest', Accept: 'application/json' } });
        const data = response.headers.get('content-type')?.includes('json') ? await response.json() : null;
        if (!response.ok || !data?.success) throw new Error(data?.message || text('İşlem tamamlanamadı. Lütfen tekrar deneyin.', 'Please try again.'));
        return data;
    }
    window.XspaInteractions = { send, toast, text };
    document.addEventListener('submit', async event => {
        const form = event.target.closest('[data-blog-interaction]'); if (!form) return;
        event.preventDefault(); const section = form.closest('.luxe-interaction');
        if (section.dataset.busy) return;
        section.dataset.busy = 'true'; section.setAttribute('aria-busy', 'true');
        const buttons = [...section.querySelectorAll('button')]; buttons.forEach(button => button.disabled = true);
        const button = form.querySelector('button'); button.classList.add('xspa-pulse');
        try {
            const body = new FormData(form);
            if (body.has('reactionType') && button.getAttribute('aria-pressed') === 'true') body.set('reactionType', '');
            const data = await window.XspaInteractions.send(form, body);
            const favorite = section.querySelector('.luxe-favorite-button');
            if (favorite) {
                favorite.classList.toggle('is-selected', data.isFavorite); favorite.setAttribute('aria-pressed', String(data.isFavorite));
                const label = favorite.querySelector('[data-favorite-label]');
                if (label) label.textContent = data.isFavorite ? label.dataset.remove : label.dataset.add;
                favorite.querySelector('[data-favorite-count]').textContent = data.favoriteCount;
                const icon = favorite.querySelector('i'); icon.classList.toggle('fa-solid', data.isFavorite); icon.classList.toggle('fa-regular', !data.isFavorite);
            }
            section.querySelectorAll('[name="reactionType"]').forEach(input => {
                const control = input.form.querySelector('button'), type = Number(input.value);
                control.classList.toggle('is-selected', type === data.reaction); control.setAttribute('aria-pressed', String(type === data.reaction));
                control.querySelector('.luxe-interaction-count').textContent = ({ 1: data.likeCount, 2: data.heartCount, 3: data.clapCount })[type];
            });
            toast(data.message);
        } catch (error) { toast(error.message, true); }
        finally { delete section.dataset.busy; section.removeAttribute('aria-busy'); buttons.forEach(control => control.disabled = false); button.classList.remove('xspa-pulse'); }
    });
})();
