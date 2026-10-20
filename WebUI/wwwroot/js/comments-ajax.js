(() => {
    'use strict';
    document.addEventListener('submit', async event => {
        const form = event.target.closest('.luxe-comment-form,.luxe-comment-delete');
        if (!form) return;
        event.preventDefault(); const section = form.closest('#comments'), ui = window.XspaInteractions;
        if (section.dataset.busy || !form.reportValidity()) return;
        section.dataset.busy = 'true'; form.setAttribute('aria-busy', 'true');
        const buttons = [...section.querySelectorAll('button')]; buttons.forEach(button => button.disabled = true);
        let saved = false;
        try {
            const data = await ui.send(form); saved = true;
            form.reset(); ui.toast(data.message);
            // Re-render only this fragment. Razor encodes plain text and respects approval/ownership.
            const response = await fetch(section.dataset.commentsUrl, { credentials: 'same-origin', cache: 'no-store' });
            if (!response.ok || !response.headers.get('content-type')?.includes('text/html')) throw new Error();
            const doc = new DOMParser().parseFromString(await response.text(), 'text/html');
            const next = doc.querySelector('#comments'); if (!next) throw new Error();
            next.classList.add('xspa-enter'); section.replaceWith(next);
        } catch (error) {
            ui.toast(saved ? ui.text('Yorum kaydedildi. Güncel listeyi görmek için sayfayı daha sonra yenileyin.', 'Saved. Refresh later to see the updated list.') : error.message, true);
        } finally { delete section.dataset.busy; form.removeAttribute('aria-busy'); buttons.forEach(button => button.disabled = false); }
    });
})();
