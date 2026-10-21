(() => {
    'use strict';
    const form = document.querySelector('[data-message-reply]'); if (!form) return;
    const input = form.querySelector('[name="replyMessage"]'), template = form.querySelector('[data-reply-template]');
    const result = form.querySelector('[data-reply-result]'), button = form.querySelector('[type="submit"]');
    const length = () => form.querySelector('[data-reply-length]').textContent = input.value.length + ' / 5000';
    const readForm = document.querySelector('[data-mark-read]');
    readForm?.addEventListener('submit', async event => {
        event.preventDefault(); const control = readForm.querySelector('button'); if (control.disabled) return;
        control.disabled = true; readForm.setAttribute('aria-busy', 'true');
        try { await window.XspaAjax.postForm(readForm); readForm.remove(); }
        catch (error) { readForm.querySelector('[role="status"]').textContent = error.message; control.disabled = false; }
        finally { readForm.removeAttribute('aria-busy'); }
    });
    input.addEventListener('input', length); length(); let busy = false, polls = 0, timer;
    template.addEventListener('change', () => { if (!template.value) return; if (input.value.trim() && !window.confirm('Yazdığınız metin seçili şablonla değiştirilsin mi?')) { template.value = ''; return; } input.value = template.value; length(); input.focus(); });
    async function refresh() {
        const response = await window.fetch(form.dataset.historyUrl, { credentials: 'same-origin', cache: 'no-store', headers: { Accept: 'text/html' } });
        if (!response.ok || !response.headers.get('content-type')?.includes('text/html')) throw new Error('Yanıt kaydedildi; geçmiş şu anda yenilenemedi.');
        const doc = new DOMParser().parseFromString(await response.text(), 'text/html'), history = doc.querySelector('#contact-reply-history');
        if (!history) throw new Error('Yanıt geçmişi alınamadı.');
        document.getElementById('contact-reply-history').replaceWith(history); schedule(history);
    }
    function schedule(history = document.getElementById('contact-reply-history')) {
        clearTimeout(timer);
        if (history?.dataset.pendingMail === 'true' && polls++ < 6) timer = setTimeout(() => {
            if (document.visibilityState === 'visible' && !busy) refresh().catch(() => {});
        }, 10000);
    }
    form.addEventListener('submit', async event => {
        event.preventDefault(); if (busy || !form.reportValidity()) return;
        busy = true; button.disabled = true; form.setAttribute('aria-busy', 'true'); result.textContent = 'Yanıtınız güvenle kaydediliyor…';
        let saved = false;
        try {
            const data = await window.XspaAjax.postForm(form); saved = true;
            readForm?.remove();
            input.value = ''; template.value = ''; length(); form.elements.clientRequestId.value = data.nextRequestId;
            result.textContent = data.message; result.className = 'text-success mt-3';
            const badge = document.querySelector('[data-message-reply-status]'); badge.textContent = 'Yanıtlandı'; badge.className = 'badge bg-label-success';
            polls = 0; await refresh();
            if (window.Swal) window.Swal.fire({ icon: 'success', title: 'Yanıt kaydedildi', text: data.message, confirmButtonText: 'Tamam', customClass: { popup: 'xspa-auth-prompt', confirmButton: 'xspa-auth-confirm' } });
        } catch (error) { result.textContent = saved ? 'Yanıt kaydedildi. Geçmişi görmek için sayfayı yenileyebilirsiniz; tekrar göndermenize gerek yok.' : error.message; result.className = 'text-warning mt-3'; }
        finally { busy = false; button.disabled = false; form.removeAttribute('aria-busy'); }
    });
    schedule();
})();
