const assert = require('node:assert/strict');
const { readFileSync } = require('node:fs');
const { resolve } = require('node:path');
const { test } = require('node:test');
const { runInNewContext } = require('node:vm');

const source = readFileSync(resolve(__dirname, '../../WebUI/wwwroot/js/site.js'), 'utf8');

function page({ aos, reduced = false, loading = true } = {}) {
    const classes = () => {
        const values = new Set();
        return { add: value => values.add(value), remove: value => values.delete(value), contains: value => values.has(value) };
    };
    const animated = Array.from({ length: 6 }, () => ({ classList: classes() }));
    const listeners = {};
    const attributes = {};
    const theme = { setAttribute: (key, value) => { attributes[key] = value; }, querySelector: () => null,
        addEventListener: (event, handler) => { listeners['theme:' + event] = handler; } };
    const document = {
        readyState: loading ? 'loading' : 'complete', body: { classList: classes() },
        documentElement: { lang: 'tr-TR', dataset: { theme: 'light' }, classList: classes() },
        getElementById: id => id === 'theme-toggle' ? theme : null,
        querySelectorAll: selector => selector === '[data-aos]' ? animated : [], querySelector: () => null,
        addEventListener: (event, handler) => { listeners[event] = handler; }
    };
    const warnings = [];
    const window = { AOS: aos, matchMedia: () => ({ matches: reduced, addEventListener() {} }),
        addEventListener() {}, scrollY: 0 };
    runInNewContext(source, { window, document, console: { warn: message => warnings.push(message) },
        localStorage: { setItem() {} }, URL, URLSearchParams, setTimeout, clearTimeout });
    return { document, animated, listeners, warnings, attributes };
}

for (const [name, aos] of [
    ['missing animation library', undefined],
    ['animation initialization throws', { init() { throw new Error('Simulated library failure'); } }],
    ['animation library never reveals offscreen elements', { init() {} }]
]) {
    test(`${name}: DOMContentLoaded reveals all marked content and other controls still initialize`, () => {
        const result = page({ aos });
        assert.equal(result.document.body.classList.contains('js-ready'), false);
        result.listeners.DOMContentLoaded();
        assert.ok(result.animated.every(node => node.classList.contains('aos-animate')));
        assert.equal(typeof result.listeners['theme:click'], 'function');
        result.listeners['theme:click']();
        assert.equal(result.document.documentElement.dataset.theme, 'dark');
    });
}

test('late script execution and reduced motion keep content visible without initializing AOS', () => {
    let calls = 0;
    const result = page({ loading: false, reduced: true, aos: { init() { calls++; } } });
    assert.equal(calls, 0);
    assert.ok(result.animated.every(node => node.classList.contains('aos-animate')));
    assert.equal(typeof result.listeners['theme:click'], 'function');
});
