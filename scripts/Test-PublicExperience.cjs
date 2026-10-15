/* Local UI regression checks. Install Playwright or supply NODE_PATH; never sends a WhatsApp message. */
const { chromium } = require('playwright');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const base = process.env.BASE_URL || 'http://localhost:5157';
const output = path.resolve('.artifacts/public-experience');
fs.mkdirSync(output, { recursive: true });
const checks = [], errors = [];
const check = (name, ok) => { assert.ok(ok, name); checks.push(name); };
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

    await page.locator('.luxe-nav-book').click();
    await page.locator('#vip-reservation.show').waitFor(); await pause(page);
    check('VIP first step is visible', await page.locator('[data-vip-step="0"]').isVisible());
    await page.locator('[data-vip-next]').click();
    check('VIP requires service selection', await page.locator('[data-vip-step="0"]').isVisible());
    await page.locator('.luxe-vip-service').filter({ hasText: 'Türk Hamamı Ritüeli' }).click();
    await page.locator('[data-vip-next]').click();
    const min = await page.locator('#vip-date').getAttribute('min');
    const shifted = delta => { const value = new Date(min + 'T12:00:00Z'); value.setUTCDate(value.getUTCDate() + delta); return value.toISOString().slice(0, 10); };
    await page.locator('#vip-date').fill(shifted(-1));
    await page.locator('#vip-time').selectOption('12:00–15:00');
    await page.locator('[data-vip-next]').click();
    check('VIP rejects a past date', await page.locator('[data-vip-step="1"]').isVisible());
    const chosenDate = shifted(1);
    await page.locator('#vip-date').fill(chosenDate);
    await page.locator('[data-vip-next]').click();
    await page.locator('#vip-name').fill('A');
    await page.locator('#vip-phone').fill('+90 555 000 00 00');
    await page.locator('[data-vip-submit]').click();
    check('VIP rejects one-letter names', await page.locator('#vip-name').evaluate(input => input.validity.customError));
    await page.locator('#vip-name').fill('Arayüz Testi');
    await page.locator('#vip-phone').fill('12345678901234567890123');
    await page.locator('[data-vip-submit]').click();
    check('VIP rejects phone numbers over fifteen digits', await page.locator('#vip-phone').evaluate(input => input.validity.customError));
    check('Invalid VIP data never opens WhatsApp', await page.evaluate(() => window.__drafts.length === 0));
    await page.locator('#vip-phone').fill('+90 555 000 00 00');
    await page.screenshot({ path: path.join(output, 'vip-dark-summary.png') });
    await page.locator('[data-vip-submit]').click();
    await page.locator('.swal2-popup').waitFor();
    await page.waitForFunction(() => document.querySelector('.swal2-popup')?.contains(document.activeElement));
    await pause(page);
    const drafts = await page.evaluate(() => window.__drafts);
    check('VIP creates exactly one WhatsApp draft', drafts.length === 1);
    const url = new URL(drafts[0]), message = url.searchParams.get('text');
    const formatted = new Intl.DateTimeFormat('tr-TR', { dateStyle: 'long', timeZone: 'Europe/Istanbul' }).format(new Date(chosenDate + 'T12:00:00+03:00'));
    check('WhatsApp recipient and request are correct', url.hostname === 'wa.me' && url.pathname === '/905388787000' && message.includes('Türk Hamamı Ritüeli') && message.includes('Arayüz Testi') && message.includes('12:00–15:00'));
    check('Turkish date stays correct in Honolulu browser timezone', message.includes(formatted));
    check('Thank-you focus stays inside active dialog', await page.locator('.swal2-popup').evaluate(dialog => dialog.contains(document.activeElement)));
    check('Thank-you does not claim confirmed appointment', (await page.locator('.swal2-html-container').innerText()).includes('uygunluğunu'));
    await page.locator('.swal2-confirm').click();
    await page.locator('.luxe-nav-book').click(); await pause(page);
    check('Completed VIP request resets to first step', await page.locator('[data-vip-step="0"]').isVisible());
    await page.screenshot({ path: path.join(output, 'vip-dark-desktop.png') });
    await page.locator('#vip-reservation [data-bs-dismiss]').click(); await pause(page);
    await page.locator('#theme-toggle').click();
    await page.locator('.luxe-nav-book').click(); await pause(page);
    await page.screenshot({ path: path.join(output, 'vip-light-desktop.png') });
    await page.locator('#vip-reservation [data-bs-dismiss]').click(); await pause(page);

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
    await page.locator('#vip-reservation [data-bs-dismiss]').click(); await pause(page);

    await page.emulateMedia({ reducedMotion: 'reduce' }); await go('/');
    check('Reduced motion disables preloader', await page.locator('.luxe-preloader').evaluate(el => getComputedStyle(el).display === 'none'));
    check('Reduced motion prevents tilt', await page.locator('.luxe-service-card').first().evaluate(card => !card.style.transform));
    check('No browser JavaScript errors', errors.length === 0);
    fs.writeFileSync(path.join(output, 'results.json'), JSON.stringify({ passed: checks.length, checks, errors, newsletterTransport: 'UI-only mocked; durable persistence covered by xUnit', whatsapp: 'window.open intercepted; nothing transmitted' }, null, 2));
    console.log(checks.length + ' UI checks passed; results and screenshots: ' + output);
    await browser.close();
})().catch(error => { fs.writeFileSync(path.join(output, 'failure.json'), JSON.stringify({ checks, errors, error: error.stack }, null, 2)); console.error(error); process.exit(1); });
