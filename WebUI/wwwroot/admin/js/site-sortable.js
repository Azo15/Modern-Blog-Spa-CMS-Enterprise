// wwwroot/js/site-sortable.js

/**
 * Genel sürükle-bırak ile sıralama işlevini başlatır.
 * @param {object} options - Yapılandırma seçenekleri.
 * @param {string} options.containerSelector - Sürükle-bırak yapılacak öğelerin ana kapsayıcısının jQuery seçicisi (örn: "#sliderList", "#teamMemberList tbody").
 * @param {string} options.itemSelector - Sürükle-bırak yapılacak her bir öğenin jQuery seçicisi (örn: ".slider-item", "tr.team-member-item").
 * @param {string} options.handleSelector - Sürükleme işlemini başlatacak öğenin seçicisi (örn: ".drag-handle").
 * @param {string} options.saveButtonSelector - "Sıralamayı Kaydet" butonunun jQuery seçicisi (örn: "#saveOrderBtn").
 * @param {string} options.updateOrderUrl - Sıralama verilerini POST edilecek URL.
 * @param {string} [options.placeholderClass='sortable-placeholder'] - Sürükleme sırasında gösterilecek yer tutucunun CSS sınıfı.
 * @param {string} [options.draggingItemClass='dragging-item'] - Sürüklenmekte olan öğeye eklenecek CSS sınıfı.
 * @param {function} [options.onVisualUpdate] - (İsteğe bağlı) Sıralama değiştiğinde çağrılacak görsel güncelleme fonksiyonu.
 *                                              Parametreler: $container (jQuery nesnesi), itemSelector (string).
 * @param {function} [options.onSaveSuccess] - (İsteğe bağlı) Kaydetme başarılı olduğunda çağrılacak fonksiyon. Parametre: response.
 * @param {function} [options.onSaveError] - (İsteğe bağlı) Kaydetme hatalı olduğunda çağrılacak fonksiyon. Parametreler: response, errorText, xhr.
 * @param {string} [options.itemIdDataAttribute='id'] - Öğenin kimliğini içeren data attribute'unun adı (örn: 'id' için 'data-id').
 */
function initializeSortableOrder(options) {
    const defaults = {
        handleSelector: ".drag-handle",
        placeholderClass: "sortable-placeholder",
        draggingItemClass: "dragging-item",
        itemIdDataAttribute: "id", // 'data-id' attribute'undan 'id' kısmını alır
        onVisualUpdate: null,
        onSaveSuccess: function (response) {
            toastr.success(response.message || 'Sıralama başarıyla kaydedildi.');
        },
        onSaveError: function (response, errorText, xhr) {
            let message = 'Sıralama kaydedilirken bir hata oluştu.';
            if (response && response.message) {
                message = response.message;
            } else if (errorText) {
                message += ': ' + errorText;
            }
            if (xhr && xhr.responseText) {
                try {
                    const errorResponse = JSON.parse(xhr.responseText);
                    if (errorResponse && errorResponse.message) message = errorResponse.message;
                } catch (e) { /* JSON parse hatası olabilir, genel mesajı kullan */ }
            }
            toastr.error(message);
        }
    };

    const settings = $.extend({}, defaults, options);

    if (!$(settings.containerSelector).length) {
        console.error("Sortable: Ana kapsayıcı bulunamadı: " + settings.containerSelector);
        return;
    }
    if (!$(settings.saveButtonSelector).length) {
        console.error("Sortable: Kaydet butonu bulunamadı: " + settings.saveButtonSelector);
        return;
    }

    const $container = $(settings.containerSelector);
    const $saveButton = $(settings.saveButtonSelector);

    $container.sortable({
        handle: settings.handleSelector,
        placeholder: settings.placeholderClass,
        forcePlaceholderSize: true,
        items: settings.itemSelector,
        helper: (settings.itemSelector.toLowerCase().startsWith("tr")) ? function (e, ui) { // Tablo satırları için özel helper
            ui.children().each(function () {
                $(this).width($(this).width());
            });
            return ui;
        } : "clone", // Diğerleri için varsayılan
        start: function (event, ui) {
            ui.item.addClass(settings.draggingItemClass);
        },
        stop: function (event, ui) {
            ui.item.removeClass(settings.draggingItemClass);
        },
        update: function (event, ui) {
            if (typeof settings.onVisualUpdate === 'function') {
                settings.onVisualUpdate($container, settings.itemSelector);
            }
        }
    });

    $saveButton.click(function () {
        let itemsData = [];
        $container.find(settings.itemSelector).each(function (index) {
            itemsData.push({
                id: $(this).data(settings.itemIdDataAttribute),
                order: index + 1
            });
        });

        if (itemsData.length === 0) {
            toastr.info('Sıralanacak öğe bulunmuyor.');
            return;
        }

        const $btn = $(this);
        const originalIcon = $btn.find('i').attr('class');
        const originalText = $btn.find('span:first').text(); // İlk span'daki metni al

        $btn.prop('disabled', true);
        $btn.find('i').removeClass().addClass('fas fa-spinner fa-spin me-md-2');
        if ($btn.find('span:first').length) {
            $btn.find('span:first').text('Kaydediliyor...');
        }


        $.ajax({
            url: settings.updateOrderUrl,
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(itemsData),
            success: function (response) {
                if (response.success) {
                    settings.onSaveSuccess(response);
                } else {
                    settings.onSaveError(response, null, null);
                }
            },
            error: function (xhr, status, error) {
                settings.onSaveError(null, error, xhr);
            },
            complete: function () {
                $btn.prop('disabled', false);
                $btn.find('i').removeClass().addClass(originalIcon);
                if ($btn.find('span:first').length) {
                    $btn.find('span:first').text(originalText);
                }
            }
        });
    });

    // Sayfa yüklendiğinde de görsel güncellemeyi çağır (eğer varsa)
    if (typeof settings.onVisualUpdate === 'function') {
        settings.onVisualUpdate($container, settings.itemSelector);
    }
}