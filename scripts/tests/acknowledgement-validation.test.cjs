const test = require('node:test'), assert = require('node:assert/strict');
const fs = require('node:fs'), vm = require('node:vm'), path = require('node:path');
const source = fs.readFileSync(path.join(__dirname, '../../WebUI/wwwroot/js/acknowledgement-validation.js'), 'utf8');
test('acknowledgement validates checkbox state, never numeric coercion of true', () => {
    let rule, adapter;
    const $ = {validator: {addMethod(name, fn) { assert.equal(name, 'mustbetrue'); rule = fn; },
        unobtrusive: {adapters: {addBool(name) { adapter = name; }}}}};
    vm.runInNewContext(source, {window: {jQuery: $}});
    assert.equal(adapter, 'mustbetrue');
    assert.equal(rule('true', {type: 'checkbox', checked: true}), true);
    assert.equal(rule('true', {type: 'checkbox', checked: false}), false);
    assert.equal(rule('true', {type: 'hidden', checked: true}), false);
});
