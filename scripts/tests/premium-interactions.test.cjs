const assert = require('node:assert/strict');
const { readFileSync } = require('node:fs');
const { resolve } = require('node:path');
const { test } = require('node:test');
const { runInNewContext } = require('node:vm');
const source = readFileSync(resolve(__dirname, '../../WebUI/wwwroot/js/site.js'), 'utf8');

function boot(now = '2026-10-16T06:00:00Z', reduced = false) {
    const makeClasses = () => {
        const values = new Set();
        return { add: value => values.add(value), remove: value => values.delete(value), contains: value => values.has(value),
            toggle: (value, active) => active ? values.add(value) : values.delete(value) };
    };
    const badge = { classList: makeClasses(), textContent: '' };
    const body = { classList: makeClasses() };
    const listeners = {}, windowListeners = {}, timers = [], navigations = [];
    const document = { readyState: 'complete', body, documentElement: { lang: 'tr-TR', dataset: { theme: 'light' }, classList: makeClasses() },
        querySelectorAll: selector => selector === '[data-business-status]' ? [badge] : [],
        getElementById: () => null, querySelector: () => null,
        addEventListener: (event, handler) => { (listeners[event] ||= []).push(handler); } };
    const window = { matchMedia: () => ({ matches: reduced, addEventListener() {} }), scrollY: 0,
        addEventListener: (event, handler) => { windowListeners[event] = handler; },
        location: { href: 'http://localhost:5157/ana-sayfa', origin: 'http://localhost:5157', assign: href => navigations.push(href) } };
    class Clock extends Date { constructor(...args) { super(...(args.length ? args : [now])); } }
    runInNewContext(source, { window, document, Date: Clock, URL, URLSearchParams, console,
        setInterval() {}, setTimeout: (callback, delay) => { timers.push({ callback, delay }); return timers.length; }, clearTimeout() {} });
    const click = (href, attributes = [], modifiers = {}) => {
        const link = { href: new URL(href, window.location.href).href, target: attributes.includes('blank') ? '_blank' : '',
            hasAttribute: name => attributes.includes(name), getAttribute: () => href };
        let prevented = false;
        for (const handler of listeners.click) handler({ target: { closest: selector => selector === '[data-reply-toggle]' ? null : link }, button: 0, defaultPrevented: false,
            preventDefault: () => { prevented = true; }, ...modifiers });
        return prevented;
    };
    return { badge, body, click, timers, navigations, windowListeners };
}

for (const [utc, expected] of [
    ['2026-10-16T05:59:00Z', '● Kapalı'], ['2026-10-16T06:00:00Z', '● Açık'],
    ['2026-10-16T20:59:00Z', '● Açık'], ['2026-10-16T21:00:00Z', '● Kapalı']
]) test(`Istanbul opening-hours boundary: ${utc}`, () => assert.equal(boot(utc).badge.textContent, expected));

test('internal navigation fades out, removes entry animation and restores on history return', () => {
    const page = boot();
    assert.equal(page.click('/hakkimizda'), true);
    assert.equal(page.body.classList.contains('page-leaving'), true);
    assert.equal(page.body.classList.contains('page-entering'), false);
    page.timers.find(timer => timer.delay === 200).callback();
    assert.deepEqual(page.navigations, ['http://localhost:5157/hakkimizda']);
    page.windowListeners.pageshow();
    assert.equal(page.body.classList.contains('page-leaving'), false);
});

for (const [href, attrs] of [
    ['#', []], ['/ana-sayfa#rituals', []], ['https://example.test/', []], ['mailto:info@example.test', []],
    ['/img/gallery-1.webp', ['data-fancybox']], ['/Reports/Users?format=Excel', []], ['/report.pdf', []],
    ['/hakkimizda', ['blank']], ['/hakkimizda', ['download']], ['/hakkimizda', ['data-bs-toggle']]
]) test(`navigation exception: ${href} ${attrs}`, () => assert.equal(boot().click(href, attrs), false));

test('reduced motion and modifier clicks navigate without intercepting', () => {
    assert.equal(boot(undefined, true).click('/hakkimizda'), false);
    assert.equal(boot().click('/hakkimizda', [], { ctrlKey: true }), false);
});

test('watchdog restores content if a navigation is cancelled', () => {
    const page = boot(); page.click('/hakkimizda');
    page.timers.find(timer => timer.delay === 1800).callback();
    assert.equal(page.body.classList.contains('page-leaving'), false);
});
