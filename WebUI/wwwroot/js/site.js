(() => {
    'use strict';
    const ready = () => {
        const en = document.documentElement.lang.toLowerCase().startsWith('en');
        const reduced = window.matchMedia('(prefers-reduced-motion: reduce)');
        const text = (tr, english) => en ? english : tr;
        document.body.classList.add('js-ready');
        document.documentElement.classList.remove('js-boot');
        // Scroll effects are optional: a failed init must never hide the page or stop its controls.
        const revealContent = () => document.querySelectorAll('[data-aos]').forEach(node => node.classList.add('aos-animate'));
        const scrollEffects = action => {
            try { action(); }
            catch { console.warn('XSPA: scroll effects are unavailable; page content remains visible.'); }
            finally { revealContent(); }
        };
        scrollEffects(() => {
            if (window.AOS && !reduced.matches) window.AOS.init({ duration: 800, once: true, offset: 45, easing: 'ease-out-cubic' });
        });
        if (window.Fancybox) Fancybox.bind('[data-fancybox]', { hideScrollbar: false });
        const blogSearch = document.getElementById('blog-search');
        if (blogSearch) {
            const blogCards = [...document.querySelectorAll('#blog-results .luxe-blog-card')];
            const normalize = value => value.toLocaleLowerCase(en ? 'en-US' : 'tr-TR').normalize('NFD').replace(/[\u0300-\u036f]/g, '').replace(/ı/g, 'i').trim();
            const filterBlogs = () => {
                const query = normalize(blogSearch.value);
                let count = 0;
                blogCards.forEach(card => {
                    const label = (card.querySelector('h3')?.textContent || '') + ' ' + (card.querySelector('.luxe-blog-category')?.textContent || '');
                    card.hidden = !!query && !normalize(label).includes(query);
                    if (!card.hidden) count++;
                });
                const empty = document.getElementById('blog-search-empty');
                if (empty) empty.hidden = !query || count > 0;
                const status = document.getElementById('blog-search-status');
                if (status) status.textContent = query ? count + text(' yazı bu sayfada eşleşti. Tüm arşivde aramak için Enter’a basın.', ' posts matched on this page. Press Enter to search the full archive.') : '';
            };
            blogSearch.addEventListener('keyup', filterBlogs);
            blogSearch.addEventListener('input', filterBlogs);
            // The existing GET form searches the complete, paginated archive on Enter.
        }
        const galleryFilters = document.querySelector('[data-gallery-filters]');
        if (galleryFilters) {
            const items = [...document.querySelectorAll('.luxe-gallery-grid [data-category]')];
            const filterAnimations = new WeakMap();
            galleryFilters.querySelectorAll('button').forEach(button => {
                const badge = button.querySelector('[data-filter-count]');
                if (badge) badge.textContent = String(items.filter(item => button.dataset.galleryFilter === 'all' || item.dataset.category === button.dataset.galleryFilter).length);
            });
            const applyGalleryFilter = category => {
                const previous = new Map(items.filter(item => !item.hidden).map(item => [item, item.getBoundingClientRect()]));
                let count = 0;
                items.forEach(item => {
                    item.hidden = category !== 'all' && item.dataset.category !== category;
                    item.dataset.fancybox = item.hidden ? 'gallery-filtered-out' : 'spa-gallery';
                    if (!item.hidden) count++;
                });
                let sequence = 0;
                items.forEach(item => {
                    filterAnimations.get(item)?.cancel();
                    if (item.hidden || reduced.matches || typeof item.animate !== 'function') return;
                    const before = previous.get(item), after = item.getBoundingClientRect();
                    const from = before ? `translate(${before.left - after.left}px, ${before.top - after.top}px)` : 'translateY(18px) scale(.97)';
                    const animation = item.animate([{ opacity: before ? 1 : 0, transform: from }, { opacity: 1, transform: 'none' }],
                        { duration: 420, delay: Math.min(sequence++ * 35, 140), easing: 'cubic-bezier(.22,1,.36,1)' });
                    filterAnimations.set(item, animation);
                });
                galleryFilters.querySelectorAll('button').forEach(button => button.setAttribute('aria-pressed', String(button.dataset.galleryFilter === category)));
                const status = document.querySelector('[data-gallery-count]');
                if (status) status.textContent = count + text(' fotoğraf', ' photographs');
                const empty = document.querySelector('[data-gallery-empty]');
                if (empty) empty.hidden = count > 0;
            };
            galleryFilters.hidden = false;
            galleryFilters.querySelectorAll('button').forEach(button => button.addEventListener('click', () => applyGalleryFilter(button.dataset.galleryFilter)));
            applyGalleryFilter('all');
        }
        const businessStatuses = [...document.querySelectorAll('[data-business-status]')];
        if (businessStatuses.length) {
            const updateBusinessStatus = () => {
                const hour = Number(new Intl.DateTimeFormat('en-US', { timeZone: 'Europe/Istanbul', hour: '2-digit', hourCycle: 'h23' }).format(new Date()));
                const open = hour >= 9 && hour < 24;
                businessStatuses.forEach(badge => {
                    badge.textContent = open ? text('● Açık', '● Open') : text('● Kapalı', '● Closed');
                    badge.classList.toggle('is-open', open);
                    badge.classList.toggle('is-closed', !open);
                });
            };
            updateBusinessStatus();
            setInterval(updateBusinessStatus, 60000);
        }
        if (!reduced.matches) document.body.classList.add('page-entering');
        let navigationPending = false;
        let navigationRestore;
        const restorePage = () => {
            document.body.classList.remove('page-leaving');
            navigationPending = false;
            clearTimeout(navigationRestore);
        };
        window.addEventListener('pageshow', restorePage);
        document.addEventListener('click', event => {
            if (event.defaultPrevented || event.button !== 0 || event.ctrlKey || event.metaKey || event.shiftKey || event.altKey || reduced.matches) return;
            const link = event.target.closest?.('a[href]');
            if (!link || (link.target && link.target !== '_self') || link.hasAttribute('download') || link.hasAttribute('data-fancybox') || link.hasAttribute('data-bs-toggle') || link.hasAttribute('data-no-transition')) return;
            const href = link.getAttribute('href');
            if (!href || href.startsWith('#')) return;
            const destination = new URL(link.href, window.location.href);
            if (!['http:', 'https:'].includes(destination.protocol) || destination.origin !== window.location.origin || destination.hash || /\.(pdf|xlsx?|zip|webp|jpe?g|png|svg)$/i.test(destination.pathname) || destination.pathname.startsWith('/Reports/')) return;
            if (destination.href === window.location.href) return;
            event.preventDefault();
            if (navigationPending) return;
            navigationPending = true;
            document.body.classList.remove('page-entering');
            document.body.classList.add('page-leaving');
            navigationRestore = setTimeout(restorePage, 1800);
            setTimeout(() => {
                try { window.location.assign(destination.href); }
                catch { restorePage(); }
            }, 200);
        });
        const announcement = document.getElementById('xspa-announcement');
        if (announcement) {
            let dismissed = false;
            try { dismissed = localStorage.getItem('xspa-announcement-hours-v1') === 'closed'; } catch { }
            announcement.hidden = dismissed;
            const syncAnnouncement = () => document.documentElement.style.setProperty('--announcement-height', announcement.hidden ? '0px' : `${announcement.getBoundingClientRect().height}px`);
            syncAnnouncement();
            document.getElementById('announcement-close')?.addEventListener('click', () => {
                announcement.hidden = true;
                try { localStorage.setItem('xspa-announcement-hours-v1', 'closed'); } catch { }
                syncAnnouncement();
            });
            if (typeof ResizeObserver !== 'undefined') new ResizeObserver(syncAnnouncement).observe(announcement);
            else window.addEventListener('resize', syncAnnouncement);
        }
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
        document.addEventListener('click', event => {
            const button = event.target.closest('[data-reply-toggle]');
            if (!button) return;
            const panel = document.getElementById(button.getAttribute('aria-controls'));
            if (!panel) return;
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

        document.querySelectorAll('[data-ritual-gallery]').forEach(gallery => {
            const stage = gallery.querySelector('[data-ritual-stage]');
            const links = [...gallery.querySelectorAll('[data-ritual-main]')];
            const buttons = [...gallery.querySelectorAll('[data-ritual-select]')];
            let active = 0, timer;
            buttons.forEach((button, index) => button.addEventListener('click', () => {
                clearTimeout(timer);
                if (index === active) { stage.classList.remove('is-switching'); return; }
                stage.classList.add('is-switching');
                timer = setTimeout(() => {
                    active = index;
                    links.forEach((link, i) => { link.hidden = i !== active; });
                    buttons.forEach((item, i) => item.setAttribute('aria-pressed', String(i === active)));
                    gallery.querySelector('[data-ritual-caption]').textContent = button.dataset.caption;
                    const photo = links[active].querySelector('img');
                    const reveal = () => requestAnimationFrame(() => stage.classList.remove('is-switching'));
                    if (photo.complete) reveal(); else { photo.addEventListener('load', reveal, { once: true }); photo.addEventListener('error', reveal, { once: true }); }
                }, reduced.matches ? 0 : 130);
            }));
        });
        const filters = document.querySelector('[data-service-filters]');
        if (filters) {
            const cards = [...document.querySelectorAll('[data-filtered-services] [data-service-category]')];
            const apply = category => {
                let count = 0;
                cards.forEach(card => {
                    card.hidden = category !== 'all' && category !== card.dataset.serviceCategory;
                    if (!card.hidden) { count++; card.classList.add('aos-animate'); }
                });
                filters.querySelectorAll('button').forEach(button => button.setAttribute('aria-pressed', String(button.dataset.serviceFilter === category)));
                document.querySelector('[data-service-count]').textContent = count + text(' ritüel sizi bekliyor.', ' rituals await you.');
                scrollEffects(() => window.AOS?.refresh());
            };
            filters.hidden = false;
            filters.querySelectorAll('button').forEach(button => button.addEventListener('click', () => apply(button.dataset.serviceFilter)));
            apply('all');
        }
        const testimonials = document.querySelector('.luxe-testimonials-swiper');
        if (testimonials && window.Swiper) {
            const slider = new Swiper(testimonials, {
                slidesPerView: 1, spaceBetween: 24, loop: true, speed: reduced.matches ? 0 : 650,
                autoplay: reduced.matches ? false : { delay: 7500, disableOnInteraction: false, pauseOnMouseEnter: true },
                navigation: { nextEl: '.luxe-testimonial-next', prevEl: '.luxe-testimonial-prev' },
                keyboard: { enabled: true, onlyInViewport: true },
                breakpoints: { 768: { slidesPerView: 2 }, 1200: { slidesPerView: 3 } },
                a11y: { enabled: true, prevSlideMessage: text('Önceki deneyim', 'Previous experience'), nextSlideMessage: text('Sonraki deneyim', 'Next experience') }
            });
            testimonials.addEventListener('focusin', () => slider.autoplay?.stop());
            testimonials.addEventListener('focusout', () => { if (!reduced.matches) slider.autoplay?.start(); });
            reduced.addEventListener('change', () => { if (reduced.matches) slider.autoplay?.stop(); });
        }

        const modal = document.getElementById('vip-reservation');
        const form = document.getElementById('vip-form');
        if (!modal || !form) return;
        const panels = [...form.querySelectorAll('[data-vip-step]')];
        const date = form.querySelector('#vip-date'), time = form.querySelector('#vip-time');
        const name = form.querySelector('#vip-name'), phone = form.querySelector('#vip-phone');
        const error = form.querySelector('.luxe-vip-error');
        const receipt = form.querySelector('[data-booking-success]');
        const actions = form.querySelector('.luxe-vip-actions');
        const progress = form.querySelector('.luxe-vip-progress');
        const submit = form.querySelector('[data-vip-submit]');
        const submitLabel = submit.querySelector('i')?.previousSibling;
        let step = 0, completed = false, busy = false;
        const newRequestId = () => {
            if (window.crypto.randomUUID) return window.crypto.randomUUID();
            const bytes = crypto.getRandomValues(new Uint8Array(16));
            bytes[6] = (bytes[6] & 15) | 64; bytes[8] = (bytes[8] & 63) | 128;
            const hex = [...bytes].map(value => value.toString(16).padStart(2, '0')).join('');
            return hex.slice(0, 8) + '-' + hex.slice(8, 12) + '-' + hex.slice(12, 16) + '-' + hex.slice(16, 20) + '-' + hex.slice(20);
        };
        const turkeyNow = () => {
            const parts = new Intl.DateTimeFormat('en-CA', { timeZone: 'Europe/Istanbul', year: 'numeric', month: '2-digit', day: '2-digit', hour: '2-digit', minute: '2-digit', hourCycle: 'h23' }).formatToParts(new Date());
            const get = key => parts.find(part => part.type === key)?.value;
            return { day: get('year') + '-' + get('month') + '-' + get('day'), hour: Number(get('hour')) + Number(get('minute')) / 60 };
        };
        const addDays = (day, count) => {
            const value = new Date(day + 'T12:00:00Z'); value.setUTCDate(value.getUTCDate() + count);
            return value.toISOString().slice(0, 10);
        };
        const updateTimes = () => {
            const now = turkeyNow();
            date.min = now.day; date.max = addDays(now.day, 90);
            date.setCustomValidity(date.value && (date.value < now.day || date.value > date.max) ? text('Bugün ile önümüzdeki 90 gün arasında bir tarih seçin.', 'Choose a date within the next 90 days.') : '');
            [...time.options].forEach(option => {
                const [hours, minutes] = option.value.split(':').map(Number);
                option.disabled = !!option.value && date.value === now.day && hours + minutes / 60 <= now.hour;
            });
            if (time.selectedOptions[0]?.disabled) time.value = '';
        };
        const selectedService = () => form.querySelector('[name="ServiceId"]:checked');
        const displayDate = () => date.value ? new Intl.DateTimeFormat(en ? 'en-GB' : 'tr-TR', { dateStyle: 'long', timeZone: 'Europe/Istanbul' }).format(new Date(date.value + 'T12:00:00+03:00')) : '';
        const renderStep = (next, focus = true) => {
            step = next;
            receipt.hidden = true; actions.hidden = false; progress.hidden = false;
            panels.forEach((panel, index) => { panel.hidden = index !== step; });
            modal.querySelectorAll('[data-vip-progress]').forEach((item, index) => {
                item.classList.toggle('is-complete', index < step);
                if (index === step) item.setAttribute('aria-current', 'step'); else item.removeAttribute('aria-current');
            });
            form.querySelector('[data-vip-back]').hidden = step === 0;
            form.querySelector('[data-vip-next]').hidden = step === panels.length - 1;
            submit.hidden = step !== panels.length - 1;
            error.hidden = true;
            form.querySelector('.luxe-vip-summary').textContent = [selectedService()?.dataset.serviceTitle, displayDate(), time.value, form.querySelector('#vip-guests').value + text(' kişi', ' guest(s)')].filter(Boolean).join(' · ');
            if (focus) panels[step].querySelector('legend')?.focus({ preventScroll: true });
            modal.querySelector('.modal-body').scrollTo({ top: 0, behavior: 'instant' });
        };
        const validate = index => {
            updateTimes();
            name.value = name.value.trim();
            name.setCustomValidity(name.value.length < 2 || name.value.length > 120 ? text('Adınızı 2–120 karakter olarak girin.', 'Please enter a name between 2 and 120 characters.') : '');
            const digits = phone.value.replace(/\D/g, '').length;
            phone.setCustomValidity(digits < 10 || digits > 15 ? text('10–15 rakam içeren geçerli bir telefon numarası girin.', 'Please enter a phone number with 10 to 15 digits.') : '');
            for (const input of panels[index].querySelectorAll('input, select, textarea')) {
                if (!input.checkValidity()) { input.reportValidity(); return false; }
            }
            return true;
        };
        form.querySelectorAll('input,select,textarea').forEach(input => input.addEventListener('input', () => input.setCustomValidity('')));
        date.addEventListener('change', updateTimes);
        form.querySelector('[data-vip-next]').addEventListener('click', () => { if (validate(step)) renderStep(step + 1); });
        form.querySelector('[data-vip-back]').addEventListener('click', () => { if (!busy) renderStep(Math.max(0, step - 1)); });
        const chooseService = id => {
            const option = [...form.querySelectorAll('[name="ServiceId"]')].find(input => input.value === String(id));
            if (option) { option.checked = true; renderStep(1, false); }
        };
        modal.addEventListener('show.bs.modal', event => {
            updateTimes();
            if (event.relatedTarget?.dataset.serviceId) chooseService(event.relatedTarget.dataset.serviceId);
            if (nav?.classList.contains('show') && window.bootstrap) bootstrap.Collapse.getOrCreateInstance(nav).hide();
        });
        modal.addEventListener('shown.bs.modal', () => panels[step].querySelector('legend')?.focus({ preventScroll: true }));
        modal.addEventListener('hide.bs.modal', event => { if (busy) event.preventDefault(); });
        modal.addEventListener('hidden.bs.modal', () => {
            if (completed) { form.reset(); completed = false; form.querySelector('#vip-request-id').value = newRequestId(); renderStep(0, false); }
        });
        const confetti = () => {
            if (reduced.matches) return;
            const container = document.createElement('div'); container.className = 'luxe-confetti'; container.setAttribute('aria-hidden', 'true');
            for (let index = 0; index < 34; index++) {
                const piece = document.createElement('span');
                piece.style.setProperty('--x', Math.random() * 100 + 'vw');
                piece.style.setProperty('--delay', Math.random() * .4 + 's');
                piece.style.setProperty('--drift', (Math.random() - .5) * 240 + 'px'); container.append(piece);
            }
            document.body.append(container); setTimeout(() => container.remove(), 2300);
        };
        form.addEventListener('submit', async event => {
            event.preventDefault();
            if (busy || completed) return;
            for (let index = 0; index < panels.length; index++) {
                renderStep(index, false);
                if (!validate(index)) return;
            }
            busy = true; submit.disabled = true; form.setAttribute('aria-busy', 'true');
            if (submitLabel) submitLabel.textContent = text('Talebiniz kaydediliyor… ', 'Saving your request… ');
            error.hidden = true;
            const timeout = new AbortController();
            const deadline = setTimeout(() => timeout.abort(), 20000);
            try {
                const response = await fetch(form.action, { method: 'POST', body: new FormData(form), credentials: 'same-origin', signal: timeout.signal, headers: { 'X-Requested-With': 'XMLHttpRequest', 'Accept': 'application/json' } });
                if (response.status === 429) throw new Error(text('Çok sayıda talep gönderdiniz. Bir dakika sonra tekrar deneyin.', 'Too many requests. Please try again in a minute.'));
                const result = response.headers.get('content-type')?.includes('application/json') ? await response.json() : null;
                if (!response.ok || !result?.success || typeof result.referenceCode !== 'string') {
                    if (result?.errors) {
                        for (const [key, messages] of Object.entries(result.errors)) {
                            const input = [...form.elements].find(item => item.name?.toLowerCase() === key.toLowerCase());
                            if (input && Array.isArray(messages)) {
                                input.setCustomValidity(messages[0]);
                                const index = panels.findIndex(panel => panel.contains(input));
                                if (index >= 0) renderStep(index);
                                input.reportValidity(); break;
                            }
                        }
                    }
                    throw new Error(result?.message || text('Talebiniz kaydedilemedi. Lütfen aynı form üzerinden yeniden deneyin.', 'We could not save your request. Please retry using this form.'));
                }
                completed = true;
                panels.forEach(panel => { panel.hidden = true; });
                actions.hidden = true; progress.hidden = true; receipt.hidden = false;
                receipt.querySelector('[data-booking-reference]').textContent = result.referenceCode;
                receipt.focus({ preventScroll: true }); modal.querySelector('.modal-body').scrollTop = 0;
                confetti();
            } catch (failure) {
                error.textContent = failure?.name === 'AbortError' ? text('Bağlantı uzun sürdü. Lütfen aynı form üzerinden yeniden deneyin.', 'The connection timed out. Please retry using this form.') : (failure instanceof Error ? failure.message : text('Bağlantınızı kontrol edip yeniden deneyin.', 'Check your connection and try again.'));
                error.hidden = false;
            } finally {
                clearTimeout(deadline);
                busy = false; submit.disabled = false; form.removeAttribute('aria-busy');
                if (submitLabel) submitLabel.textContent = text('Randevuyu onayla ', 'Confirm request ');
            }
        });
        form.querySelector('#vip-request-id').value = newRequestId();
        updateTimes(); renderStep(0, false);
        const query = new URLSearchParams(window.location.search);
        if (query.get('reservation') === '1' && window.bootstrap) {
            if (query.get('serviceId')) chooseService(query.get('serviceId'));
            bootstrap.Modal.getOrCreateInstance(modal).show();
        }
    };
    if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', ready); else ready();
})();
