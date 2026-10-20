const { test } = require('node:test');
const assert = require('node:assert/strict');
const { readFileSync } = require('node:fs');
const { resolve } = require('node:path');
const { runInNewContext } = require('node:vm');
const source = readFileSync(resolve(__dirname, '../../WebUI/wwwroot/js/xspa-ajax.js'), 'utf8');
function boot(reply = () => new Response('{"success":true}', { headers: { 'content-type': 'application/json' } }), swal) {
    const requests = [], navigations = [];
    class BrowserRequest extends Request {
        constructor(input, init) { super(typeof input === 'string' ? new URL(input, 'https://xspa.test') : input, init); }
    }
    const window = { fetch: async request => { requests.push(request); return reply(request); }, Swal: swal, confirm: () => false,
        location: { origin: 'https://xspa.test', pathname: '/randevularim', search: '?page=2', assign: value => navigations.push(value) } };
    const document = { documentElement: { lang: 'tr' }, querySelector: selector => selector.includes('csrf-header') ? { content: 'RequestVerificationToken' } : selector.includes('csrf') ? { content: 'page-token' } : null };
    runInNewContext(source, { window, document, Request: BrowserRequest, Headers, URL, FormData, console });
    return { window, requests, navigations };
}
test('POST gets body CSRF token and preserves multipart payload', async () => {
    const page = boot(), body = new FormData(); body.set('__RequestVerificationToken', 'form-token'); body.set('content', 'Guest message');
    await page.window.fetch('/Comment/Create', { method: 'POST', body });
    const request = page.requests[0]; assert.equal(request.headers.get('RequestVerificationToken'), 'form-token');
    assert.equal(request.headers.get('X-Requested-With'), 'XMLHttpRequest'); assert.equal((await request.formData()).get('content'), 'Guest message');
    assert.equal(request.redirect, 'error');
});
test('JSON mutations use page token while existing explicit token is preserved', async () => {
    const page = boot(); await page.window.fetch('/endpoint', { method: 'PATCH', body: '{}', headers: { 'content-type': 'application/json' } });
    assert.equal(page.requests[0].headers.get('RequestVerificationToken'), 'page-token');
    await page.window.fetch('/endpoint', { method: 'DELETE', headers: { RequestVerificationToken: 'explicit' } });
    assert.equal(page.requests[1].headers.get('RequestVerificationToken'), 'explicit');
});
test('CSRF and AJAX headers never leak to a different origin', async () => {
    const page = boot(); await page.window.fetch('https://external.test/endpoint', { method: 'POST', body: 'public data' });
    assert.equal(page.requests[0].headers.get('RequestVerificationToken'), null); assert.equal(page.requests[0].headers.get('X-Requested-With'), null);
});
test('GET fragments do not receive a mutation token or forced JSON accept header', async () => {
    const page = boot(); await page.window.fetch('/yapilan-isler/article/yorumlar');
    assert.equal(page.requests[0].headers.get('RequestVerificationToken'), null); assert.equal(page.requests[0].headers.get('Accept'), null);
});
test('form helper rejects external actions before transmitting form content', async () => {
    const page = boot(), body = new FormData(); body.set('__RequestVerificationToken', 'private-token');
    await assert.rejects(page.window.XspaAjax.postForm({ action: 'https://external.test/post' }, body));
    assert.equal(page.requests.length, 0);
});
test('concurrent 401 responses share one login prompt and never retry mutation', async () => {
    let release, prompts = 0;
    const page = boot(() => new Response('{}', { status: 401 }), { fire: () => { prompts++; return new Promise(resolve => release = resolve); } });
    const first = page.window.fetch('/one'), second = page.window.fetch('/two');
    await new Promise(resolve => setImmediate(resolve)); release({ isConfirmed: false }); await Promise.all([first, second]);
    assert.equal(prompts, 1); assert.equal(page.requests.length, 2); assert.deepEqual(page.navigations, []);
});
test('confirmed expired session goes to local login with safe return URL', async () => {
    const page = boot(() => new Response('{}', { status: 401 }), { fire: async () => ({ isConfirmed: true }) });
    await page.window.fetch('/one'); assert.deepEqual(page.navigations, ['/dashboard/giris?ReturnUrl=%2Frandevularim%3Fpage%3D2']);
});
test('server validation errors reach form handlers without a success acknowledgement', async () => {
    const page = boot(() => new Response('{"success":false,"message":"Invalid","errors":{"Email":["Check email"]}}', { status: 400, headers: { 'content-type': 'application/json' } }));
    await assert.rejects(page.window.XspaAjax.postForm({ action: 'https://xspa.test/iletisim' }, new FormData()), error => error.status === 400 && error.errors.Email[0] === 'Check email');
});
test('failed HTML response cannot be interpreted as a saved form', async () => {
    const page = boot(() => new Response('<html>Error</html>', { status: 500, headers: { 'content-type': 'text/html' } }));
    await assert.rejects(page.window.XspaAjax.postForm({ action: 'https://xspa.test/iletisim' }, new FormData()));
});
