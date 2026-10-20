(() => {
    'use strict';
    if (window.XspaAjax) return;
    const nativeFetch = window.fetch.bind(window);
    const text = (tr, en) => document.documentElement.lang.startsWith('en') ? en : tr;
    let loginPrompt = null;
    function promptLogin() {
        if (loginPrompt) return loginPrompt;
        loginPrompt = (async () => {
            const message = text('Oturumunuz sona erdi. İşleminize devam etmek için giriş yapın.', 'Your session has expired. Sign in to continue.');
            const confirmed = window.Swal ? (await window.Swal.fire({ icon: 'info', title: text('Tekrar hoş geldiniz', 'Welcome back'), text: message,
                showCancelButton: true, confirmButtonText: text('Giriş yap', 'Sign in'), cancelButtonText: text('Burada kal', 'Stay here'),
                customClass: { popup: 'xspa-auth-prompt', confirmButton: 'xspa-auth-confirm' } })).isConfirmed : window.confirm(message);
            if (confirmed) window.location.assign('/dashboard/giris?ReturnUrl=' + encodeURIComponent(window.location.pathname + window.location.search));
        })().finally(() => { loginPrompt = null; });
        return loginPrompt;
    }
    // All fetch consumers (including existing admin/VIP flows) share this same-origin boundary.
    window.fetch = async (input, options) => {
        const request = new Request(input, options);
        if (new URL(request.url).origin !== window.location.origin) return nativeFetch(request);
        const headers = new Headers(request.headers);
        const mutation = !['GET', 'HEAD', 'OPTIONS'].includes(request.method.toUpperCase());
        headers.set('X-Requested-With', 'XMLHttpRequest');
        if (mutation) {
            const header = document.querySelector('meta[name="xspa-csrf-header"]')?.content || 'RequestVerificationToken';
            if (!headers.has(header)) {
                let token;
                const type = headers.get('content-type') || '';
                if (type.includes('multipart/form-data') || type.includes('application/x-www-form-urlencoded')) {
                    token = (await request.clone().formData()).get('__RequestVerificationToken');
                }
                token ||= document.querySelector('meta[name="xspa-csrf"]')?.content || document.querySelector('input[name="__RequestVerificationToken"]')?.value;
                if (typeof token === 'string' && token) headers.set(header, token);
            }
            if (!headers.has('Accept')) headers.set('Accept', 'application/json');
        }
        // Never forward a mutation's CSRF header through an unexpected redirect.
        const response = await nativeFetch(new Request(request, { headers, redirect: mutation ? 'error' : request.redirect }));
        if (response.status === 401) await promptLogin();
        return response;
    };
    async function postForm(form, body = new FormData(form)) {
        if (new URL(form.action, window.location.origin).origin !== window.location.origin) throw new Error(text('Geçersiz işlem adresi.', 'Invalid form destination.'));
        const response = await window.fetch(form.action, { method: 'POST', body, credentials: 'same-origin', headers: { Accept: 'application/json' } });
        const data = response.headers.get('content-type')?.includes('json') ? await response.json() : null;
        if (!response.ok || !data?.success) {
            const error = new Error(data?.message || text('İşlem tamamlanamadı. Lütfen tekrar deneyin.', 'Please try again.'));
            error.errors = data?.errors; error.status = response.status; throw error;
        }
        return data;
    }
    window.XspaAjax = { postForm };
})();
