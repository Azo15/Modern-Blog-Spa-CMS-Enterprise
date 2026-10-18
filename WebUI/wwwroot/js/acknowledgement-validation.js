(function ($) {
    'use strict';
    if (!$ || !$.validator) return;
    // A checked checkbox has the string value "true", which a numeric range rule rejects.
    $.validator.addMethod('mustbetrue', function (_value, element) {
        return element.type === 'checkbox' && element.checked === true;
    });
    $.validator.unobtrusive.adapters.addBool('mustbetrue');
})(window.jQuery);
