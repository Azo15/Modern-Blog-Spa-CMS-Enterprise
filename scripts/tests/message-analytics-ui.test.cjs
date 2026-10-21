const { test } = require('node:test');
const assert = require('node:assert/strict');
const { readFileSync } = require('node:fs');
const { resolve } = require('node:path');
const { runInNewContext } = require('node:vm');
test('visitor charts preserve fourteen daily points and reduced motion', () => {
    const charts = [], points = Array.from({ length: 14 }, (_, i) => ({ Date: `${i + 1}.10`, Count: i }));
    class Chart { static defaults = {}; constructor(canvas, config) { charts.push({ canvas, config }); } }
    const elements = { 'visitor-trend-data': { textContent: JSON.stringify(points) }, 'visitor-trend': {}, 'visitor-device-data': { textContent: '[7,3]' }, 'visitor-devices': {} };
    runInNewContext(readFileSync(resolve(__dirname, '../../WebUI/wwwroot/js/admin-dashboard.js'), 'utf8'), { Chart, document: { getElementById: id => elements[id] }, window: { matchMedia: () => ({ matches: true }) } });
    assert.equal(charts.length, 2); assert.equal(charts[0].config.data.datasets[0].data.length, 14);
    assert.equal(charts[0].config.data.datasets[0].data[13], 13); assert.equal(charts[0].config.options.animation, false);
    assert.equal(charts[1].config.type, 'doughnut'); assert.deepEqual(Array.from(charts[1].config.data.datasets[0].data), [7, 3]);
});
function replyPage(post) {
    const listeners = {}, input = { value: 'A thoughtful answer', addEventListener() {}, focus() {} }, template = { value: '', addEventListener() {} };
    const result = {}, button = {}, badge = {}, counter = {}, requestId = { value: 'original-id' };
    const form = { dataset: { historyUrl: '/AdminMessage/ReplyHistory/1' }, elements: { clientRequestId: requestId }, reportValidity: () => true,
        setAttribute() {}, removeAttribute() {}, addEventListener: (name, callback) => listeners[name] = callback,
        querySelector: selector => ({ '[name="replyMessage"]': input, '[data-reply-template]': template, '[data-reply-result]': result, '[type="submit"]': button, '[data-reply-length]': counter })[selector] };
    const document = { visibilityState: 'visible', querySelector: selector => selector === '[data-message-reply]' ? form : selector === '[data-mark-read]' ? null : badge,
        getElementById: () => ({ dataset: { pendingMail: 'false' } }) };
    const window = { XspaAjax: { postForm: post }, fetch: async () => { throw new Error('History unavailable'); } };
    runInNewContext(readFileSync(resolve(__dirname, '../../WebUI/wwwroot/js/message-replies.js'), 'utf8'), { document, window, clearTimeout() {}, setTimeout() {} });
    return { input, requestId, button, result, submit: () => listeners.submit({ preventDefault() {} }) };
}
test('failed reply keeps draft and idempotency key and unlocks form', async () => {
    const page = replyPage(async () => { throw new Error('Validation failed'); }); await page.submit();
    assert.equal(page.input.value, 'A thoughtful answer'); assert.equal(page.requestId.value, 'original-id'); assert.equal(page.button.disabled, false); assert.equal(page.result.textContent, 'Validation failed');
});
test('saved reply with failed history refresh never resends or restores the old draft', async () => {
    let sends = 0; const page = replyPage(async () => { sends++; return { success: true, message: 'Queued', nextRequestId: 'new-id' }; }); await page.submit();
    assert.equal(sends, 1); assert.equal(page.input.value, ''); assert.equal(page.requestId.value, 'new-id'); assert.equal(page.button.disabled, false); assert.match(page.result.textContent, /tekrar göndermenize gerek yok/);
});
