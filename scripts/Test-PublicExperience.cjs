/* Local UI regression checks. Install Playwright or supply NODE_PATH; does not send email or WhatsApp messages; booking transport is mocked. */
const { chromium } = require('playwright');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const base = process.env.BASE_URL || 'http://localhost:5157';
const output = path.resolve('.artifacts/public-experience');
fs.mkdirSync(output, { recursive: true });
const checks = [], errors = [];
const check = (name, ok) => { assert.ok(ok, name); checks.push(name); console.log('PASS: ' + name); };
const pause = page => page.waitForTimeout(450);
(async () => {
    const browser = await chromium.launch({ headless: true, channel: process.env.BROWSER_CHANNEL || 'msedge' });
    const context = await browser.newContext({ viewport: { width: 1440, height: 1000 }, timezoneId: 'Pacific/Honolulu' });
    await context.route('**/*', route => {
        const url = new URL(route.request().url());
        return url.origin === new URL(base).origin || ['data:', 'blob:'].includes(url.protocol) ? route.continue() : route.abort();
    });
    await context.addInitScript(() => { window.__drafts = []; window.open = url => { window.__drafts.push(url); return null; }; });
    const page = await context.newPage();
    page.setDefaultTimeout(12000);
    page.setDefaultNavigationTimeout(15000);
    page.on('pageerror', error => errors.push(error.message));
    const go = async route => {
        const response = await page.goto(base + route, { waitUntil: 'domcontentloaded' });
        check(route + ' returns 200', response.status() === 200);
        await pause(page);
    };
    const sweep = async () => {
        await page.evaluate(async () => {
            await document.fonts.ready;
            for (let y = 0; y < document.body.scrollHeight; y += 750) { window.scrollTo({ top: y, behavior: 'instant' }); await new Promise(resolve => setTimeout(resolve, 70)); }
            window.scrollTo({ top: 0, behavior: 'instant' });
            document.querySelectorAll('[data-aos]').forEach(element => element.classList.add('aos-animate'));
        });
        await pause(page);
    };
    for (const [label, route] of [['home', '/'], ['services', '/hizmetler'], ['blogs', '/yapilan-isler'], ['gallery', '/gorsel-galerisi'], ['contact', '/iletisim'], ['about', '/hakkimizda']]) {
        await go(route); await sweep();
        check(label + ' desktop has no overflow', await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth));
        check(label + ' images load', await page.evaluate(() => [...document.images].filter(img => !img.closest('#vip-reservation')).every(img => img.complete && img.naturalWidth > 0)));
        const photos = await page.locator('main img').evaluateAll(imgs => imgs.map(img => img.getAttribute('src')).filter(src => src && !src.endsWith('.svg')));
        check(label + ' has no repeated content photos', photos.length === new Set(photos).size);
        await page.screenshot({ path: path.join(output, label + '-light-desktop.png'), fullPage: true });
    }
    await page.locator('#theme-toggle').click();
    check('Theme switches to dark', await page.locator('html').getAttribute('data-theme') === 'dark');
    await page.reload(); await pause(page);
    check('Theme survives reload', await page.locator('html').getAttribute('data-theme') === 'dark');
    for (const [label, route] of [['home', '/'], ['services', '/hizmetler'], ['blogs', '/yapilan-isler'], ['contact', '/iletisim']]) {
        await go(route); await sweep();
        check(label + ' dark has no overflow', await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth));
        await page.screenshot({ path: path.join(output, label + '-dark-desktop.png'), fullPage: true });
    }
    check('Contact submit is enabled without reCAPTCHA', await page.locator('.luxe-contact-form [type=submit]').isEnabled());
    await go('/yapilan-isler');
    await page.locator('.luxe-blog-card h3 a').first().click();
    await page.locator('[data-login-required]').first().click();
    await page.locator('.swal2-popup').waitFor();
    check('Anonymous reaction shows sign-in prompt', (await page.locator('.swal2-html-container').innerText()).includes('üye girişi'));
    await page.locator('.swal2-confirm').click();
    await page.waitForURL('**/dashboard/giris');
    check('Prompt links to actual sign-in route', (await page.request.get(base + '/dashboard/giris')).status() === 200);

    await go('/');
    let newsletterMode = 'success';
    await page.route('**/iletisim/bulten', route => route.fulfill({
        status: newsletterMode === 'success' ? 200 : 503, contentType: 'application/json',
        body: JSON.stringify({ success: newsletterMode === 'success', message: newsletterMode === 'success' ? 'İlginiz için teşekkürler, kaydınız alındı.' : 'Talebiniz kaydedilemedi. Lütfen tekrar deneyin.' })
    }));
    let navigations = 0;
    page.on('framenavigated', frame => { if (frame === page.mainFrame()) navigations++; });
    await page.locator('#newsletter-email').fill('ui-only@example.test');
    await page.locator('[data-newsletter-form] [type=submit]').click();
    await page.locator('[data-newsletter-status][data-state=success]').waitFor();
    check('Newsletter shows inline acknowledgement without navigation', navigations === 0);
    newsletterMode = 'failure';
    await page.locator('#newsletter-email').fill('ui-failure@example.test');
    await page.locator('[data-newsletter-form] [type=submit]').click();
    await page.locator('[data-newsletter-status][data-state=error]').waitFor();
    check('Newsletter failure is shown without false success', (await page.locator('[data-newsletter-status]').innerText()).includes('kaydedilemedi'));
    check('Newsletter can retry after failure', await page.locator('[data-newsletter-form] [type=submit]').isEnabled());
    await page.unroute('**/iletisim/bulten');

    await go('/hizmetler');
    const serviceCount = await page.locator('[data-service-category]').count();
    let filterNavigation = 0;
    const countFilterNavigation = frame => { if (frame === page.mainFrame()) filterNavigation++; };
    page.on('framenavigated', countFilterNavigation);
    for (const category of ['massage', 'hammam', 'care', 'detox', 'all']) {
        await page.locator('[data-service-filter="' + category + '"]').click();
        check('Live category filter ' + category, await page.locator('[data-service-category]').evaluateAll((cards, key) => cards.every(card => card.hidden === (key !== 'all' && key !== card.dataset.serviceCategory)), category));
    }
    check('Filters keep the page without navigation', filterNavigation === 0);
    check('All filter restores every service', await page.locator('[data-service-category]:visible').count() === serviceCount);
    page.off('framenavigated', countFilterNavigation);
    for (const id of [1, 2, 3, 9, 12]) {
        await go('/hizmetler/' + id);
        const links = await page.locator('[data-ritual-main]').evaluateAll(items => items.map(item => item.getAttribute('href')));
        check('Service ' + id + ' has three unique photos', links.length >= 3 && new Set(links).size === links.length);
        await page.locator('[data-ritual-select="2"]').click();
        await page.waitForTimeout(300);
        check('Service ' + id + ' thumbnail changes main photo', await page.locator('[data-ritual-main]:visible').getAttribute('href') === links[2]);
        check('Service ' + id + ' main image preserves source ratio', await page.locator('[data-ritual-main]:visible img').evaluate(async img => { await img.decode(); const rect = img.getBoundingClientRect(); return Math.abs(rect.height - rect.width / img.naturalWidth * img.naturalHeight) < 2; }));
        if (id === 1) {
            await page.locator('[data-ritual-select="0"]').click(); await page.waitForTimeout(300);
            await page.locator('[data-ritual-select="1"]').click(); await page.locator('[data-ritual-select="0"]').click(); await page.waitForTimeout(300);
            check('Rapid gallery selection keeps last chosen thumbnail', await page.locator('[data-ritual-select="0"]').getAttribute('aria-pressed') === 'true');
            await page.locator('[data-ritual-main]:visible').click();
            await page.locator('.fancybox__container').waitFor();
            check('Ritual lightbox opens full-screen gallery', await page.locator('.fancybox__container').isVisible());
            await page.keyboard.press('Escape'); await pause(page);
            await page.locator('.luxe-faq-accordion .accordion-button').first().click(); await pause(page);
            check('Ritual FAQ expands', await page.locator('.luxe-faq-accordion .accordion-collapse').first().isVisible());
            check('Service price card shows price and duration', (await page.locator('.luxe-price-card').innerText()).includes('650') && (await page.locator('.luxe-price-card').innerText()).includes('60'));
            await page.screenshot({path:path.join(output,'ritual-gallery-desktop.png'),fullPage:true});
        }
    }
    await go('/iletisim'); await sweep();
    const map = await page.locator('.luxe-concierge-map iframe').boundingBox();
    check('Concierge map is a substantial framed card', map.height >= 350 && map.width < 1440);
    check('Contact contains three accessible FAQ questions', await page.locator('.luxe-faq-accordion .accordion-button').count() === 3);
    await page.screenshot({path:path.join(output,'concierge-desktop.png'),fullPage:true});
    await go('/');
    check('Guest experiences are explicitly illustrative', (await page.locator('.luxe-testimonials').innerText()).toLocaleLowerCase('tr-TR').includes('temsili'));
    const beforeSlide = await page.locator('.luxe-testimonials-swiper').evaluate(el=>el.swiper.realIndex);
    await page.locator('.luxe-testimonial-next').click(); await page.waitForTimeout(750);
    check('Guest experience slider moves', await page.locator('.luxe-testimonials-swiper').evaluate(el=>el.swiper.realIndex) !== beforeSlide);

    await go('/hizmetler/9');
    await page.locator('.luxe-ritual-description [data-service-id="9"]').click();
    await pause(page);
    check('Service booking automatically selects its own service', await page.locator('[name="ServiceId"]:checked').inputValue() === '9');
    check('Preselected booking starts at the date step', await page.locator('[data-vip-step="1"]').isVisible());
    const min = await page.locator('#vip-date').getAttribute('min');
    const shifted = delta => { const value = new Date(min + 'T12:00:00Z'); value.setUTCDate(value.getUTCDate() + delta); return value.toISOString().slice(0, 10); };
    await page.locator('#vip-date').fill(shifted(-1)); await page.locator('#vip-time').selectOption('14:00');
    await page.locator('[data-vip-next]').click();
    check('Online reservation rejects past dates', await page.locator('[data-vip-step="1"]').isVisible());
    const chosenDate = shifted(1);
    await page.locator('#vip-date').fill(chosenDate); await page.locator('#vip-guests').selectOption('4'); await page.locator('[data-vip-next]').click();
    await page.locator('#vip-name').fill('A'); await page.locator('#vip-phone').fill('+90 555 000 00 00');
    await page.locator('[data-vip-submit]').click();
    check('Online reservation rejects a one-letter name', await page.locator('#vip-name').evaluate(input=>input.validity.customError));
    await page.locator('#vip-name').fill('Arayüz Testi'); await page.locator('#vip-phone').fill('12345678901234567890123');
    await page.locator('[data-vip-submit]').click();
    check('Online reservation rejects invalid phone length', await page.locator('#vip-phone').evaluate(input=>input.validity.customError));
    await page.locator('#vip-phone').fill('+90 555 000 00 00'); await page.locator('#vip-email').fill('ui-only@example.test'); await page.locator('#vip-notes').fill('Arayüz kontrolü: çğışöüİ');
    const formatted = new Intl.DateTimeFormat('tr-TR',{dateStyle:'long',timeZone:'Europe/Istanbul'}).format(new Date(chosenDate+'T12:00:00+03:00'));
    check('Summary date stays correct in Honolulu timezone', (await page.locator('.luxe-vip-summary').innerText()).includes(formatted));
    let bookingAttempt = 0; const posted = [];
    await page.route('**/randevu',async route=>{
        if(route.request().method()!=='POST')return route.continue();
        posted.push(route.request().postData()); bookingAttempt++;
        await route.fulfill({status:bookingAttempt===1?503:201,contentType:'application/json',body:JSON.stringify(bookingAttempt===1?{success:false,message:'Talebiniz kaydedilemedi. Lütfen yeniden deneyin.'}:{success:true,referenceCode:'#XSPA-UI2026',appointmentDate:chosenDate,timeSlot:'14:00',status:'Pending'})});
    });
    await page.locator('[data-vip-submit]').click();
    await page.locator('.luxe-vip-error:not([hidden])').waitFor();
    check('A failed save never displays a reservation receipt', !await page.locator('[data-booking-success]').isVisible());
    check('A failed save allows a retry', await page.locator('[data-vip-submit]').isEnabled());
    await page.locator('[data-vip-submit]').click(); await page.locator('[data-booking-success]:visible').waitFor();
    const requestId = body => /name="ClientRequestId"\r?\n\r?\n([a-f0-9-]+)/i.exec(body)?.[1];
    check('Retry retains the same idempotency token', !!requestId(posted[0]) && requestId(posted[0])===requestId(posted[1]));
    check('Reservation posts all fields and CSRF token', ['ServiceId','AppointmentDate','TimeSlot','GuestCount','FullName','Phone','Email','Notes','__RequestVerificationToken'].every(key=>posted[1].includes('name="'+key+'"')));
    check('Success card displays returned reservation reference', await page.locator('[data-booking-reference]').innerText()==='#XSPA-UI2026');
    check('Online reservation never opens WhatsApp', await page.evaluate(()=>window.__drafts.length===0));
    check('Receipt explains that business confirmation follows', (await page.locator('[data-booking-success]').innerText()).includes('onay için'));
    check('Keyboard focus stays in the receipt', await page.locator('[data-booking-success]').evaluate(receipt=>receipt===document.activeElement||receipt.contains(document.activeElement)));
    await page.screenshot({path:path.join(output,'reservation-receipt-desktop.png')});
    await page.locator('[data-booking-success] [data-bs-dismiss]').click(); await pause(page);
    await page.locator('.luxe-nav-book').click(); await pause(page);
    check('Finished reservation resets to first step',await page.locator('[data-vip-step="0"]').isVisible());
    check('Online reservation offers all published services',await page.locator('[name="ServiceId"]').count()===serviceCount);
    await page.screenshot({path:path.join(output,'reservation-dark-desktop.png')});
    await page.locator('.luxe-modal-close').click(); await pause(page);
    await page.locator('#theme-toggle').click(); await page.locator('.luxe-nav-book').click(); await pause(page);
    await page.screenshot({path:path.join(output,'reservation-light-desktop.png')});
    await page.locator('.luxe-modal-close').click(); await pause(page);
    await page.unroute('**/randevu');

    await page.setViewportSize({ width: 390, height: 844 });
    for (const [label, route] of [['home', '/'], ['services', '/hizmetler'], ['blogs', '/yapilan-isler'], ['gallery', '/gorsel-galerisi'], ['contact', '/iletisim']]) {
        await go(route); await sweep();
        check(label + ' mobile has no overflow', await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth));
        await page.screenshot({ path: path.join(output, label + '-mobile.png'), fullPage: true });
    }
    await page.locator('.luxe-menu-toggle').click(); await pause(page);
    await page.locator('.luxe-nav-book').click(); await pause(page);
    check('VIP opens from mobile menu', await page.locator('#vip-reservation').isVisible());
    check('Mobile menu closes behind VIP dialog', await page.locator('.luxe-menu-toggle').getAttribute('aria-expanded') === 'false');
    check('VIP mobile has no overflow', await page.locator('#vip-reservation').evaluate(modal => modal.scrollWidth <= innerWidth));
    await page.screenshot({ path: path.join(output, 'vip-mobile.png') });
    await page.locator('.luxe-modal-close').click(); await pause(page);

    await page.emulateMedia({ reducedMotion: 'reduce' }); await go('/');
    check('Reduced motion disables preloader', await page.locator('.luxe-preloader').evaluate(el => getComputedStyle(el).display === 'none'));
    check('Reduced motion prevents tilt', await page.locator('.luxe-service-card').first().evaluate(card => !card.style.transform));
    check('No browser JavaScript errors', errors.length === 0);
    fs.writeFileSync(path.join(output, 'results.json'), JSON.stringify({ passed: checks.length, checks, errors, newsletterTransport: 'UI-only mocked; durable persistence covered by xUnit', bookingTransport: 'UI-only mocked; durable persistence/admin/outbox covered by xUnit', whatsapp: 'No WhatsApp redirect from online reservation' }, null, 2));
    console.log(checks.length + ' UI checks passed; results and screenshots: ' + output);
    await browser.close();
})().catch(error => { fs.writeFileSync(path.join(output, 'failure.json'), JSON.stringify({ checks, errors, error: error.stack }, null, 2)); console.error(error); process.exit(1); });
