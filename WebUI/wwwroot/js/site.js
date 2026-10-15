(() => {
    'use strict';
    const ready = () => {
        const en = document.documentElement.lang.toLowerCase().startsWith('en');
        const reduced = window.matchMedia('(prefers-reduced-motion: reduce)');
        const text = (tr, english) => en ? english : tr;
        document.body.classList.add('js-ready');
        document.documentElement.classList.remove('js-boot');
        if (window.AOS && !reduced.matches) AOS.init({ duration: 800, once: true, offset: 45, easing: 'ease-out-cubic' });
        if (window.Fancybox) Fancybox.bind('[data-fancybox]', { hideScrollbar: false });
        const header = document.getElementById('site-header');
        const nav = document.getElementById('navbarNav');
        const menuToggle = document.querySelector('.luxe-menu-toggle');
        const scroll = () => header?.classList.toggle('is-scrolled', window.scrollY > 35);
        scroll();
        window.addEventListener('scroll', scroll, { passive: true });
        nav?.addEventListener('show.bs.collapse', () => header?.classList.add('menu-open'));
        nav?.addEventListener('hidden.bs.collapse', () => header?.classList.remove('menu-open'));
        document.addEventListener('keydown', event => {
            if (event.key === 'Escape' && nav?.classList.contains('show') && window.bootstrap) {
                bootstrap.Collapse.getOrCreateInstance(nav).hide();
                menuToggle?.focus();
            }
        });
        window.addEventListener('resize', () => {
            if (window.innerWidth >= 1200 && header?.classList.contains('menu-open') && window.bootstrap) bootstrap.Collapse.getOrCreateInstance(nav).hide();
        }, { passive: true });

        const themeToggle = document.getElementById('theme-toggle');
        const setTheme = (theme, persist) => {
            const dark = theme === 'dark';
            document.documentElement.dataset.theme = dark ? 'dark' : 'light';
            themeToggle?.setAttribute('aria-pressed', String(dark));
            themeToggle?.setAttribute('aria-label', dark ? text('Açık temaya geç', 'Switch to light theme') : text('Koyu temaya geç', 'Switch to dark theme'));
            const icon = themeToggle?.querySelector('i');
            if (icon) icon.className = dark ? 'fa-regular fa-sun' : 'fa-regular fa-moon';
            document.querySelector('meta[name="theme-color"]')?.setAttribute('content', dark ? '#081C15' : '#FDFBF7');
            if (persist) { try { localStorage.setItem('xspa-theme', theme); } catch { /* Private browsing may deny storage. */ } }
        };
        setTheme(document.documentElement.dataset.theme, false);
        themeToggle?.addEventListener('click', () => setTheme(document.documentElement.dataset.theme === 'dark' ? 'light' : 'dark', true));
        window.addEventListener('storage', event => {
            if (event.key === 'xspa-theme') setTheme(event.newValue === 'dark' ? 'dark' : 'light', false);
        });

        document.querySelectorAll('[data-login-required]').forEach(button => {
            button.addEventListener('click', async () => {
                const message = button.dataset.loginMessage || text('Bu yazıyı beğenmek veya favorilerinize eklemek için üye girişi yapmalısınız.', 'Please sign in to react to this article or save it to your favourites.');
                const url = new URL(button.dataset.loginUrl || '/dashboard/giris', window.location.origin);
                if (url.origin !== window.location.origin) return;
                if (window.Swal) {
                    const result = await Swal.fire({ icon: 'info', title: text('Ritüelinize katılın', 'Join the experience'), text: message, showCancelButton: true, confirmButtonText: button.dataset.loginConfirm || text('Giriş yap', 'Sign in'), cancelButtonText: button.dataset.loginCancel || text('Keşfetmeye devam et', 'Keep exploring'), customClass: { popup: 'luxe-alert', confirmButton: 'luxe-alert-confirm' } });
                    if (result.isConfirmed) window.location.assign(url.href);
                } else if (window.confirm(message)) window.location.assign(url.href);
            });
        });
        document.querySelectorAll('[data-reply-toggle]').forEach(button => {
            const panel = document.getElementById(button.getAttribute('aria-controls'));
            if (!panel) return;
            button.addEventListener('click', () => {
                const open = button.getAttribute('aria-expanded') !== 'true';
                button.setAttribute('aria-expanded', String(open));
                panel.hidden = false;
                panel.inert = !open;
                // Force the collapsed grid to lay out before animating its first opening.
                if (open) { panel.getBoundingClientRect(); panel.classList.add('is-open'); requestAnimationFrame(() => panel.querySelector('textarea, input:not([type="hidden"]), button')?.focus({ preventScroll: true })); }
                else {
                    panel.classList.remove('is-open');
                    if (reduced.matches) panel.hidden = true;
                    else panel.addEventListener('transitionend', () => { if (button.getAttribute('aria-expanded') !== 'true') panel.hidden = true; }, { once: true });
                }
            });
        });

        document.querySelectorAll('[data-newsletter-form]').forEach(form => {
            const status = form.querySelector('[data-newsletter-status]');
            const button = form.querySelector('[type="submit"]');
            form.addEventListener('submit', async event => {
                event.preventDefault();
                if (!form.reportValidity() || form.dataset.busy === 'true') return;
                form.dataset.busy = 'true';
                form.setAttribute('aria-busy', 'true');
                if (button) button.disabled = true;
                if (status) { status.textContent = text('Kaydınız alınıyor…', 'Saving your request…'); status.dataset.state = 'pending'; }
                try {
                    const response = await fetch(form.action, { method: 'POST', body: new FormData(form), credentials: 'same-origin', headers: { 'X-Requested-With': 'XMLHttpRequest', 'Accept': 'application/json' } });
                    const json = response.headers.get('content-type')?.includes('application/json') ? await response.json() : null;
                    if (!response.ok || !json?.success) throw new Error(json?.message || text('Kaydınız alınamadı. Lütfen yeniden deneyin.', 'We could not save your request. Please try again.'));
                    if (status) { status.textContent = json.message; status.dataset.state = 'success'; }
                    form.reset();
                } catch (error) {
                    if (status) { status.textContent = error instanceof Error ? error.message : text('Bağlantınızı kontrol edip yeniden deneyin.', 'Check your connection and try again.'); status.dataset.state = 'error'; }
                } finally {
                    delete form.dataset.busy;
                    form.removeAttribute('aria-busy');
                    if (button) button.disabled = false;
                }
            });
        });

        const finePointer = window.matchMedia('(hover: hover) and (pointer: fine)');
        document.querySelectorAll('.luxe-service-card, .luxe-blog-card, .luxe-gallery-item').forEach(card => {
            let frame = 0;
            const reset = () => { cancelAnimationFrame(frame); card.style.removeProperty('transform'); card.classList.remove('is-tilting'); };
            card.addEventListener('pointermove', event => {
                if (reduced.matches || !finePointer.matches || event.pointerType === 'touch') return;
                cancelAnimationFrame(frame);
                frame = requestAnimationFrame(() => {
                    const rect = card.getBoundingClientRect();
                    const x = Math.max(-.5, Math.min(.5, (event.clientX - rect.left) / rect.width - .5));
                    const y = Math.max(-.5, Math.min(.5, (event.clientY - rect.top) / rect.height - .5));
                    card.classList.add('is-tilting');
                    card.style.transform = 'perspective(1100px) rotateX(' + (-y * 5) + 'deg) rotateY(' + (x * 5) + 'deg) translateY(-4px)';
                });
            });
            card.addEventListener('pointerleave', reset);
            card.addEventListener('pointercancel', reset);
            reduced.addEventListener('change', reset);
        });

        const modal = document.getElementById('vip-reservation');
        const form = document.getElementById('vip-form');
        if (!modal || !form) return;
        const panels = [...form.querySelectorAll('[data-vip-step]')];
        const date = form.querySelector('#vip-date'), time = form.querySelector('#vip-time');
        const name = form.querySelector('#vip-name'), phone = form.querySelector('#vip-phone');
        const error = form.querySelector('.luxe-vip-error');
        let step = 0, completed = false;
        const turkeyNow = () => {
            const parts = new Intl.DateTimeFormat('en-CA', { timeZone: 'Europe/Istanbul', year: 'numeric', month: '2-digit', day: '2-digit', hour: '2-digit', minute: '2-digit', hourCycle: 'h23' }).formatToParts(new Date());
            const get = key => parts.find(part => part.type === key)?.value;
            return { day: get('year') + '-' + get('month') + '-' + get('day'), hour: Number(get('hour')) + Number(get('minute')) / 60 };
        };
        const updateTimes = () => {
            const now = turkeyNow();
            date.min = now.day;
            date.setCustomValidity(date.value && date.value < now.day ? text('Geçmiş bir tarih seçilemez.', 'Please choose today or a future date.') : '');
            [...time.options].forEach(option => {
                if (option.dataset.endHour) option.disabled = date.value === now.day && Number(option.dataset.endHour) <= now.hour;
            });
            if (time.selectedOptions[0]?.disabled) time.value = '';
        };
        const service = () => form.querySelector('[name="vipService"]:checked')?.value || '';
        const displayDate = () => date.value ? new Intl.DateTimeFormat(en ? 'en-GB' : 'tr-TR', { dateStyle: 'long', timeZone: 'Europe/Istanbul' }).format(new Date(date.value + 'T12:00:00+03:00')) : '';
        const renderStep = (next, focus = true) => {
            step = next;
            panels.forEach((panel, index) => { panel.hidden = index !== step; });
            modal.querySelectorAll('[data-vip-progress]').forEach((item, index) => {
                item.classList.toggle('is-complete', index < step);
                if (index === step) item.setAttribute('aria-current', 'step'); else item.removeAttribute('aria-current');
            });
            form.querySelector('[data-vip-back]').hidden = step === 0;
            form.querySelector('[data-vip-next]').hidden = step === panels.length - 1;
            form.querySelector('[data-vip-submit]').hidden = step !== panels.length - 1;
            error.hidden = true;
            form.querySelector('.luxe-vip-summary').textContent = [service(), displayDate(), time.value].filter(Boolean).join(' · ');
            if (focus) panels[step].querySelector('legend')?.focus({ preventScroll: true });
        };
        const validate = index => {
            updateTimes();
            name.value = name.value.trim();
            name.setCustomValidity(name.value.length < 2 || name.value.length > 100 ? text('Adınızı 2–100 karakter olarak girin.', 'Please enter a name between 2 and 100 characters.') : '');
            const digits = phone.value.replace(/\D/g, '').length;
            phone.setCustomValidity(digits < 10 || digits > 15 ? text('10–15 rakam içeren geçerli bir telefon numarası girin.', 'Please enter a phone number with 10 to 15 digits.') : '');
            for (const input of panels[index].querySelectorAll('input, select')) {
                if (!input.checkValidity()) { input.reportValidity(); return false; }
            }
            return true;
        };
        date.addEventListener('change', updateTimes);
        phone.addEventListener('input', () => phone.setCustomValidity(''));
        name.addEventListener('input', () => name.setCustomValidity(''));
        form.querySelector('[data-vip-next]').addEventListener('click', () => { if (validate(step)) renderStep(step + 1); });
        form.querySelector('[data-vip-back]').addEventListener('click', () => renderStep(Math.max(0, step - 1)));
        modal.addEventListener('show.bs.modal', () => {
            updateTimes();
            if (nav?.classList.contains('show') && window.bootstrap) bootstrap.Collapse.getOrCreateInstance(nav).hide();
        });
        modal.addEventListener('shown.bs.modal', () => panels[step].querySelector('legend')?.focus({ preventScroll: true }));
        modal.addEventListener('hidden.bs.modal', () => {
            if (completed) { form.reset(); completed = false; renderStep(0, false); }
        });
        form.addEventListener('submit', event => {
            event.preventDefault();
            for (let index = 0; index < panels.length; index++) {
                renderStep(index, false);
                if (!validate(index)) { renderStep(index); return; }
            }
            const message = text('Merhaba XSPA, VIP randevu ön talebi oluşturmak istiyorum.', 'Hello XSPA, I would like to request a VIP appointment.') + '\n\n' +
                text('Hizmet', 'Service') + ': ' + service() + '\n' + text('Tarih', 'Date') + ': ' + displayDate() + '\n' + text('Saat', 'Time') + ': ' + time.value + '\n' + text('Ad Soyad', 'Name') + ': ' + name.value + '\n' + text('Telefon', 'Phone') + ': ' + phone.value.trim() + '\n\n' + text('Uygunluk bilgisini paylaşabilir misiniz?', 'Could you confirm availability?');
            const url = 'https://wa.me/905388787000?text=' + encodeURIComponent(message);
            window.open(url, '_blank', 'noopener,noreferrer');
            completed = true;
            const acknowledge = () => {
            if (!reduced.matches) {
                const confetti = document.createElement('div');
                confetti.className = 'luxe-confetti';
                confetti.setAttribute('aria-hidden', 'true');
                for (let index = 0; index < 34; index++) {
                    const piece = document.createElement('span');
                    piece.style.setProperty('--x', Math.random() * 100 + 'vw');
                    piece.style.setProperty('--delay', Math.random() * .4 + 's');
                    piece.style.setProperty('--drift', (Math.random() - .5) * 240 + 'px');
                    confetti.append(piece);
                }
                document.body.append(confetti);
                setTimeout(() => confetti.remove(), 2300);
            }
            if (window.Swal) {
                const link = document.createElement('a');
                link.href = url; link.target = '_blank'; link.rel = 'noopener noreferrer';
                link.textContent = text('WhatsApp açılmadıysa buradan devam edin', 'If WhatsApp did not open, continue here');
                Swal.fire({ icon: 'success', title: text('İlginiz için teşekkürler.', 'Thank you for choosing XSPA.'), text: text('Ön talebiniz hazır. WhatsApp’ta mesajı göndererek tamamlayın; ekibimiz randevu uygunluğunu sizinle teyit edecek.', 'Your request is ready. Send the message on WhatsApp; our team will confirm appointment availability.'), footer: link, confirmButtonText: text('Harika', 'Lovely'), customClass: { popup: 'luxe-alert' } });
            } else {
                error.hidden = false;
                error.textContent = text('Ön talebiniz WhatsApp’ta hazır. Mesajı göndererek tamamlayın.', 'Your request is ready on WhatsApp. Send the message to continue.');
            }
            };
            const instance = window.bootstrap ? bootstrap.Modal.getInstance(modal) : null;
            if (instance && window.Swal) {
                // Bootstrap restores focus on hidden; open the next dialog afterwards.
                modal.addEventListener('hidden.bs.modal', acknowledge, { once: true });
                instance.hide();
            } else acknowledge();
        });
        updateTimes();
        renderStep(0, false);
    };
    if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', ready); else ready();
})();
