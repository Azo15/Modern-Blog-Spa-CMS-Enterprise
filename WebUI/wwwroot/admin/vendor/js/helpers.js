// helpers.js - Güzelleþtirilmiþ Hali

(function (self, factory) {
    if (typeof exports === 'object' && typeof module === 'object') {
        module.exports = factory();
    } else if (typeof define === 'function' && define.amd) {
        define([], factory);
    } else {
        var R = factory();
        for (var T in R) {
            (typeof exports === 'object' ? exports : self)[T] = R[T];
        }
    }
})(self, function () {
    return (function () {
        "use strict";
        var __webpack_modules__ = {
            "./js/helpers.js": function (__unused_webpack_module, __webpack_exports__, __webpack_require__) {
                __webpack_require__.r(__webpack_exports__);
                __webpack_require__.d(__webpack_exports__, {
                    Helpers: function () {
                        return Helpers;
                    }
                });

                function _toArray(r) {
                    return _arrayWithHoles(r) || _iterableToArray(r) || _unsupportedIterableToArray(r) || _nonIterableRest();
                }

                function _iterableToArray(r) {
                    if (typeof Symbol !== "undefined" && r[Symbol.iterator] != null || r["@@iterator"] != null) {
                        return Array.from(r);
                    }
                }

                function _slicedToArray(r, e) {
                    return _arrayWithHoles(r) || _iterableToArrayLimit(r, e) || _unsupportedIterableToArray(r, e) || _nonIterableRest();
                }

                function _nonIterableRest() {
                    throw new TypeError("Invalid attempt to destructure non-iterable instance.\nIn order to be iterable, non-array objects must have a [Symbol.iterator]() method.");
                }

                function _unsupportedIterableToArray(r, a) {
                    if (r) {
                        if (typeof r === "string") {
                            return _arrayLikeToArray(r, a);
                        }
                        var t = {}.toString.call(r).slice(8, -1);
                        if (t === "Object" && r.constructor) {
                            t = r.constructor.name;
                        }
                        if (t === "Map" || t === "Set") {
                            return Array.from(r);
                        }
                        if (t === "Arguments" || /^(?:Ui|I)nt(?:8|16|32)(?:Clamped)?Array$/.test(t)) {
                            return _arrayLikeToArray(r, a);
                        }
                    }
                }

                function _arrayLikeToArray(r, a) {
                    if (a == null || a > r.length) {
                        a = r.length;
                    }
                    for (var e = 0, n = new Array(a); e < a; e++) {
                        n[e] = r[e];
                    }
                    return n;
                }

                function _iterableToArrayLimit(r, l) {
                    var t = r == null ? null : typeof Symbol !== "undefined" && r[Symbol.iterator] || r["@@iterator"];
                    if (t != null) {
                        var e, n, i, u, a = [],
                            f = true,
                            o = false;
                        try {
                            if (i = (t = t.call(r)).next, l === 0) {
                                if (Object(t) !== t) return;
                                f = false;
                            } else {
                                for (; !(f = (e = i.call(t)).done) && (a.push(e.value), a.length !== l); f = true);
                            }
                        } catch (r) {
                            o = true;
                            n = r;
                        } finally {
                            try {
                                if (!f && t.return != null && (u = t.return(), Object(u) !== u)) return;
                            } finally {
                                if (o) throw n;
                            }
                        }
                        return a;
                    }
                }

                function _arrayWithHoles(r) {
                    if (Array.isArray(r)) return r;
                }

                var TRANS_EVENTS = ['transitionend', 'webkitTransitionEnd', 'oTransitionEnd'];
                var TRANS_PROPERTIES = ['transition', 'MozTransition', 'webkitTransition', 'WebkitTransition', 'OTransition'];
                var INLINE_STYLES = "\n.layout-menu-fixed .layout-navbar-full .layout-menu,\n.layout-page {\n  padding-top: {navbarHeight}px !important;\n}\n.content-wrapper {\n  padding-bottom: {footerHeight}px !important;\n}";

                function requiredParam(name) {
                    throw new Error("Parameter required".concat(name ? ": `".concat(name, "`") : ''));
                }

                var Helpers = {
                    ROOT_EL: typeof window !== 'undefined' ? document.documentElement : null,
                    prefix: getComputedStyle(document.documentElement).getPropertyValue('--prefix').trim(),
                    LAYOUT_BREAKPOINT: 1200,
                    RESIZE_DELAY: 200,
                    menuPsScroll: null,
                    mainMenu: null,
                    _curStyle: null,
                    _styleEl: null,
                    _resizeTimeout: null,
                    _resizeCallback: null,
                    _transitionCallback: null,
                    _transitionCallbackTimeout: null,
                    _listeners: [],
                    _initialized: false,
                    _autoUpdate: false,

                    _scrollToActive: function _scrollToActive() {
                        var animate = arguments.length > 0 && arguments[0] !== undefined ? arguments[0] : false;
                        var duration = arguments.length > 1 && arguments[1] !== undefined ? arguments[1] : 500;
                        var layoutMenu = this.getLayoutMenu();
                        if (!layoutMenu) return;

                        var activeEl = layoutMenu.querySelector('li.menu-item.active:not(.open)');
                        if (activeEl) {
                            var easeInOutQuad = function (t, b, c, d) {
                                t /= d / 2;
                                if (t < 1) return c / 2 * t * t + b;
                                t -= 1;
                                return -c / 2 * (t * (t - 2) - 1) + b;
                            };

                            var element = this.getLayoutMenu().querySelector('.menu-inner');
                            if (typeof activeEl === 'string') {
                                activeEl = document.querySelector(activeEl);
                            }
                            if (typeof activeEl !== 'number') {
                                activeEl = activeEl.getBoundingClientRect().top + element.scrollTop;
                            }

                            if (activeEl < parseInt(element.clientHeight * 2 / 3, 10)) return;

                            var start = element.scrollTop;
                            var change = activeEl - start - parseInt(element.clientHeight / 2, 10);
                            var startDate = +new Date();

                            if (animate === true) {
                                var _animateScroll = function animateScroll() {
                                    var currentDate = +new Date();
                                    var currentTime = currentDate - startDate;
                                    var val = easeInOutQuad(currentTime, start, change, duration);
                                    element.scrollTop = val;
                                    if (currentTime < duration) {
                                        requestAnimationFrame(_animateScroll);
                                    } else {
                                        element.scrollTop = change;
                                    }
                                };
                                _animateScroll();
                            } else {
                                element.scrollTop = change;
                            }
                        }
                    },

                    _addClass: function _addClass(cls) {
                        var el = arguments.length > 1 && arguments[1] !== undefined ? arguments[1] : this.ROOT_EL;
                        if (el && el.length !== undefined) {
                            el.forEach(function (e) {
                                if (e) {
                                    cls.split(' ').forEach(function (c) {
                                        return e.classList.add(c);
                                    });
                                }
                            });
                        } else if (el) {
                            cls.split(' ').forEach(function (c) {
                                return el.classList.add(c);
                            });
                        }
                    },

                    _removeClass: function _removeClass(cls) {
                        var el = arguments.length > 1 && arguments[1] !== undefined ? arguments[1] : this.ROOT_EL;
                        if (el && el.length !== undefined) {
                            el.forEach(function (e) {
                                if (e) {
                                    cls.split(' ').forEach(function (c) {
                                        return e.classList.remove(c);
                                    });
                                }
                            });
                        } else if (el) {
                            cls.split(' ').forEach(function (c) {
                                return el.classList.remove(c);
                            });
                        }
                    },

                    _toggleClass: function _toggleClass() {
                        var el = arguments.length > 0 && arguments[0] !== undefined ? arguments[0] : this.ROOT_EL;
                        var cls1 = arguments.length > 1 ? arguments[1] : undefined;
                        var cls2 = arguments.length > 2 ? arguments[2] : undefined;
                        if (el.classList.contains(cls1)) {
                            el.classList.replace(cls1, cls2);
                        } else {
                            el.classList.replace(cls2, cls1);
                        }
                    },

                    _hasClass: function _hasClass(cls) {
                        var el = arguments.length > 1 && arguments[1] !== undefined ? arguments[1] : this.ROOT_EL;
                        var result = false;
                        cls.split(' ').forEach(function (c) {
                            if (el.classList.contains(c)) result = true;
                        });
                        return result;
                    },

                    _findParent: function _findParent(el, cls) {
                        if (el && el.tagName.toUpperCase() === 'BODY' || el.tagName.toUpperCase() === 'HTML') return null;
                        el = el.parentNode;
                        while (el && el.tagName.toUpperCase() !== 'BODY' && !el.classList.contains(cls)) {
                            el = el.parentNode;
                        }
                        el = el && el.tagName.toUpperCase() !== 'BODY' ? el : null;
                        return el;
                    },

                    _triggerWindowEvent: function _triggerWindowEvent(name) {
                        if (typeof window === 'undefined') return;
                        if (document.createEvent) {
                            var event;
                            if (typeof Event === 'function') {
                                event = new Event(name);
                            } else {
                                event = document.createEvent('Event');
                                event.initEvent(name, false, true);
                            }
                            window.dispatchEvent(event);
                        } else {
                            window.fireEvent("on".concat(name), document.createEventObject());
                        }
                    },

                    _triggerEvent: function _triggerEvent(name) {
                        this._triggerWindowEvent("layout".concat(name));
                        this._listeners.filter(function (listener) {
                            return listener.event === name;
                        }).forEach(function (listener) {
                            return listener.callback.call(null);
                        });
                    },

                    _updateInlineStyle: function _updateInlineStyle() {
                        var navbarHeight = arguments.length > 0 && arguments[0] !== undefined ? arguments[0] : 0;
                        var footerHeight = arguments.length > 1 && arguments[1] !== undefined ? arguments[1] : 0;
                        if (!this._styleEl) {
                            this._styleEl = document.createElement('style');
                            this._styleEl.type = 'text/css';
                            document.head.appendChild(this._styleEl);
                        }
                        var newStyle = INLINE_STYLES.replace(/{navbarHeight}/gi, navbarHeight).replace(/{footerHeight}/gi, footerHeight);
                        if (this._curStyle !== newStyle) {
                            this._curStyle = newStyle;
                            this._styleEl.textContent = newStyle;
                        }
                    },

                    _removeInlineStyle: function _removeInlineStyle() {
                        if (this._styleEl) document.head.removeChild(this._styleEl);
                        this._styleEl = null;
                        this._curStyle = null;
                    },

                    _redrawLayoutMenu: function _redrawLayoutMenu() {
                        var layoutMenu = this.getLayoutMenu();
                        if (layoutMenu && layoutMenu.querySelector('.menu')) {
                            var inner = layoutMenu.querySelector('.menu-inner');
                            var scrollTop = inner.scrollTop;
                            var pageScrollTop = document.documentElement.scrollTop;
                            layoutMenu.style.display = 'none';
                            layoutMenu.style.display = '';
                            inner.scrollTop = scrollTop;
                            document.documentElement.scrollTop = pageScrollTop;
                            return true;
                        }
                        return false;
                    },

                    _supportsTransitionEnd: function _supportsTransitionEnd() {
                        if (window.QUnit) return false;
                        var el = document.body || document.documentElement;
                        if (!el) return false;
                        var result = false;
                        TRANS_PROPERTIES.forEach(function (evnt) {
                            if (typeof el.style[evnt] !== 'undefined') result = true;
                        });
                        return result;
                    },

                    _getNavbarHeight: function _getNavbarHeight() {
                        var _this = this;
                        var layoutNavbar = this.getLayoutNavbar();
                        if (!layoutNavbar) return 0;
                        if (!this.isSmallScreen()) return layoutNavbar.getBoundingClientRect().height;

                        var clonedEl = layoutNavbar.cloneNode(true);
                        clonedEl.id = null;
                        clonedEl.style.visibility = 'hidden';
                        clonedEl.style.position = 'absolute';
                        Array.prototype.slice.call(clonedEl.querySelectorAll('.collapse.show')).forEach(function (el) {
                            return _this._removeClass('show', el);
                        });
                        layoutNavbar.parentNode.insertBefore(clonedEl, layoutNavbar);
                        var navbarHeight = clonedEl.getBoundingClientRect().height;
                        clonedEl.parentNode.removeChild(clonedEl);
                        return navbarHeight;
                    },

                    _getFooterHeight: function _getFooterHeight() {
                        var layoutFooter = this.getLayoutFooter();
                        if (!layoutFooter) return 0;
                        return layoutFooter.getBoundingClientRect().height;
                    },

                    _getAnimationDuration: function _getAnimationDuration(el) {
                        var duration = window.getComputedStyle(el).transitionDuration;
                        return parseFloat(duration) * (duration.indexOf('ms') !== -1 ? 1 : 1000);
                    },

                    _setMenuHoverState: function _setMenuHoverState(hovered) {
                        this[hovered ? '_addClass' : '_removeClass']('layout-menu-hover');
                    },

                    _setCollapsed: function _setCollapsed(collapsed) {
                        // BU FONKSÝYON SADECE KÜÇÜK EKRANLARDA layout-menu-expanded SINIFINI YÖNETÝYOR GÝBÝ.
                        // BÜYÜK EKRANLAR ÝÇÝN document.documentElement ÜZERÝNDE .layout-menu-collapsed EKLEME/KALDIRMA ÝÞLEMÝ EKSÝK.
                        var _this2 = this;
                        if (this.isSmallScreen()) {
                            if (collapsed) {
                                this._removeClass('layout-menu-expanded');
                            } else {
                                setTimeout(function () {
                                    _this2._addClass('layout-menu-expanded');
                                }, this._redrawLayoutMenu() ? 5 : 0);
                            }
                        }
                        // BÜYÜK EKRAN MANTIÐI BURAYA EKLENMELÝ:
                        // else {
                        //     if (collapsed) {
                        //         this._addClass('layout-menu-collapsed');
                        //     } else {
                        //         this._removeClass('layout-menu-collapsed');
                        //     }
                        // }
                    },

                    _bindLayoutAnimationEndEvent: function _bindLayoutAnimationEndEvent(modifier, cb) {
                        var _this3 = this;
                        var menu = this.getMenu();
                        var duration = menu ? this._getAnimationDuration(menu) + 50 : 0;
                        if (!duration) {
                            modifier.call(this);
                            cb.call(this);
                            return;
                        }
                        this._transitionCallback = function (e) {
                            if (e.target !== menu) return;
                            _this3._unbindLayoutAnimationEndEvent();
                            cb.call(_this3);
                        };
                        TRANS_EVENTS.forEach(function (e) {
                            menu.addEventListener(e, _this3._transitionCallback, false);
                        });
                        modifier.call(this); // Modifier hemen çalýþýr
                        this._transitionCallbackTimeout = setTimeout(function () {
                            _this3._transitionCallback.call(_this3, {
                                target: menu
                            });
                        }, duration);
                    },

                    _unbindLayoutAnimationEndEvent: function _unbindLayoutAnimationEndEvent() {
                        var _this4 = this;
                        var menu = this.getMenu();
                        if (this._transitionCallbackTimeout) {
                            clearTimeout(this._transitionCallbackTimeout);
                            this._transitionCallbackTimeout = null;
                        }
                        if (menu && this._transitionCallback) {
                            TRANS_EVENTS.forEach(function (e) {
                                menu.removeEventListener(e, _this4._transitionCallback, false);
                            });
                        }
                        if (this._transitionCallback) {
                            this._transitionCallback = null;
                        }
                    },

                    _bindWindowResizeEvent: function _bindWindowResizeEvent() {
                        var _this5 = this;
                        this._unbindWindowResizeEvent();
                        var cb = function () {
                            if (_this5._resizeTimeout) {
                                clearTimeout(_this5._resizeTimeout);
                                _this5._resizeTimeout = null;
                            }
                            _this5._triggerEvent('resize');
                        };
                        this._resizeCallback = function () {
                            if (_this5._resizeTimeout) clearTimeout(_this5._resizeTimeout);
                            _this5._resizeTimeout = setTimeout(cb, _this5.RESIZE_DELAY);
                        };
                        window.addEventListener('resize', this._resizeCallback, false);
                    },

                    _unbindWindowResizeEvent: function _unbindWindowResizeEvent() {
                        if (this._resizeTimeout) {
                            clearTimeout(this._resizeTimeout);
                            this._resizeTimeout = null;
                        }
                        if (this._resizeCallback) {
                            window.removeEventListener('resize', this._resizeCallback, false);
                            this._resizeCallback = null;
                        }
                    },

                    _bindMenuMouseEvents: function _bindMenuMouseEvents() {
                        var _this6 = this;
                        if (this._menuMouseEnter && this._menuMouseLeave && this._windowTouchStart) return;
                        var layoutMenu = this.getLayoutMenu();
                        if (!layoutMenu) return this._unbindMenuMouseEvents();

                        if (!this._menuMouseEnter) {
                            this._menuMouseEnter = function () {
                                if (_this6.isSmallScreen() || _this6._hasClass('layout-transitioning')) {
                                    // return _this6._setMenuHoverState(false); // Orijinalinde true olabilir, test etmek lazým
                                }
                                // return _this6._setMenuHoverState(false); // Orijinalinde true olabilir
                                // Hover state yönetimi menü daraltma/geniþletmeyle ilgili deðilse geçici olarak devre dýþý býrakýlabilir.
                            };
                            layoutMenu.addEventListener('mouseenter', this._menuMouseEnter, false);
                            layoutMenu.addEventListener('touchstart', this._menuMouseEnter, false);
                        }
                        if (!this._menuMouseLeave) {
                            this._menuMouseLeave = function () {
                                _this6._setMenuHoverState(false);
                            };
                            layoutMenu.addEventListener('mouseleave', this._menuMouseLeave, false);
                        }
                        if (!this._windowTouchStart) {
                            this._windowTouchStart = function (e) {
                                if (!e || !e.target || !_this6._findParent(e.target, '.layout-menu')) {
                                    _this6._setMenuHoverState(false);
                                }
                            };
                            window.addEventListener('touchstart', this._windowTouchStart, true);
                        }
                    },

                    _unbindMenuMouseEvents: function _unbindMenuMouseEvents() {
                        if (!this._menuMouseEnter && !this._menuMouseLeave && !this._windowTouchStart) return;
                        var layoutMenu = this.getLayoutMenu();
                        if (this._menuMouseEnter) {
                            if (layoutMenu) {
                                layoutMenu.removeEventListener('mouseenter', this._menuMouseEnter, false);
                                layoutMenu.removeEventListener('touchstart', this._menuMouseEnter, false);
                            }
                            this._menuMouseEnter = null;
                        }
                        if (this._menuMouseLeave) {
                            if (layoutMenu) {
                                layoutMenu.removeEventListener('mouseleave', this._menuMouseLeave, false);
                            }
                            this._menuMouseLeave = null;
                        }
                        if (this._windowTouchStart) {
                            // window.removeEventListener yerine window.addEventListener vardý, düzeltildi.
                            if (layoutMenu) { // Bu kontrol gereksiz olabilir, window globaldir.
                                window.removeEventListener('touchstart', this._windowTouchStart, true);
                            }
                            this._windowTouchStart = null;
                        }
                        this._setMenuHoverState(false);
                    },

                    scrollToActive: function scrollToActive() {
                        var animate = arguments.length > 0 && arguments[0] !== undefined ? arguments[0] : false;
                        this._scrollToActive(animate);
                    },

                    setCollapsed: function setCollapsed() {
                        var _this7 = this;
                        var collapsed = arguments.length > 0 && arguments[0] !== undefined ? arguments[0] : requiredParam('collapsed');
                        var animate = arguments.length > 1 && arguments[1] !== undefined ? arguments[1] : true;
                        var layoutMenu = this.getLayoutMenu();
                        if (!layoutMenu) return;

                        this._unbindLayoutAnimationEndEvent();

                        if (animate && this._supportsTransitionEnd()) {
                            this._addClass('layout-transitioning'); // <html>'e eklenir
                            if (collapsed) this._setMenuHoverState(false); // Menü hover'ý kapatýr

                            this._bindLayoutAnimationEndEvent(
                                function () { // Modifier
                                    if (_this7.isSmallScreen()) {
                                        _this7._setCollapsed(collapsed);
                                    } else {
                                        // BÜYÜK EKRANLAR ÝÇÝN DÜZELTME:
                                        if (collapsed) {
                                            _this7._addClass('layout-menu-collapsed', _this7.ROOT_EL); // Hedef: document.documentElement
                                        } else {
                                            _this7._removeClass('layout-menu-collapsed', _this7.ROOT_EL);
                                        }
                                    }
                                },
                                function () { // Callback - ANÝMASYON BÝTÝNCE ÇALIÞIR 
                                    _this7._removeClass('layout-transitioning');
                                    _this7._triggerWindowEvent('resize');
                                    _this7._triggerEvent('toggle');
                                    _this7._setMenuHoverState(false);
                                }
                            );
                        } else { // Animasyonsuz ise
                            this._addClass('layout-no-transition');
                            if (collapsed) this._setMenuHoverState(false);

                            // Collapse / Expand
                            if (this.isSmallScreen()) {
                                this._setCollapsed(collapsed);
                            } else {
                                // BÜYÜK EKRANLAR ÝÇÝN DÜZELTME:
                                if (collapsed) {
                                    this._addClass('layout-menu-collapsed', this.ROOT_EL);
                                } else {
                                    this._removeClass('layout-menu-collapsed', this.ROOT_EL);
                                }
                            }

                            setTimeout(function () {
                                _this7._removeClass('layout-no-transition');
                                _this7._triggerWindowEvent('resize');
                                _this7._triggerEvent('toggle');
                                _this7._setMenuHoverState(false);
                            }, 1);
                        }
                    },

                    toggleCollapsed: function toggleCollapsed() {
                        var animate = arguments.length > 0 && arguments[0] !== undefined ? arguments[0] : true;
                        this.setCollapsed(!this.isCollapsed(), animate);
                    },

                    setPosition: function setPosition() {
                        var fixed = arguments.length > 0 && arguments[0] !== undefined ? arguments[0] : requiredParam('fixed');
                        var offcanvas = arguments.length > 1 && arguments[1] !== undefined ? arguments[1] : requiredParam('offcanvas');
                        this._removeClass('layout-menu-offcanvas layout-menu-fixed layout-menu-fixed-offcanvas');
                        if (!fixed && offcanvas) {
                            this._addClass('layout-menu-offcanvas');
                        } else if (fixed && !offcanvas) {
                            this._addClass('layout-menu-fixed');
                            this._redrawLayoutMenu();
                        } else if (fixed && offcanvas) {
                            this._addClass('layout-menu-fixed-offcanvas');
                            this._redrawLayoutMenu();
                        }
                        this.update();
                    },

                    getLayoutMenu: function getLayoutMenu() {
                        return document.querySelector('.layout-menu');
                    },

                    getMenu: function getMenu() {
                        var layoutMenu = this.getLayoutMenu();
                        if (!layoutMenu) return null;
                        return !this._hasClass('menu', layoutMenu) ? layoutMenu.querySelector('.menu') : layoutMenu;
                    },

                    getLayoutNavbar: function getLayoutNavbar() {
                        return document.querySelector('.layout-navbar');
                    },

                    getLayoutFooter: function getLayoutFooter() {
                        return document.querySelector('.content-footer');
                    },

                    update: function update() {
                        if (this.getLayoutNavbar() && (!this.isSmallScreen() && this.isLayoutNavbarFull() && this.isFixed() || this.isNavbarFixed()) || this.getLayoutFooter() && this.isFooterFixed()) {
                            this._updateInlineStyle(this._getNavbarHeight(), this._getFooterHeight());
                        }
                        this._bindMenuMouseEvents();
                    },

                    setAutoUpdate: function setAutoUpdate() {
                        var _this8 = this;
                        var enable = arguments.length > 0 && arguments[0] !== undefined ? arguments[0] : requiredParam('enable');
                        if (enable && !this._autoUpdate) {
                            this.on('resize.Helpers:autoUpdate', function () {
                                return _this8.update();
                            });
                            this._autoUpdate = true;
                        } else if (!enable && this._autoUpdate) {
                            this.off('resize.Helpers:autoUpdate');
                            this._autoUpdate = false;
                        }
                    },

                    isMobileDevice: function isMobileDevice() {
                        return typeof window.orientation !== 'undefined' || navigator.userAgent.indexOf('IEMobile') !== -1;
                    },

                    isSmallScreen: function isSmallScreen() {
                        return (window.innerWidth || document.documentElement.clientWidth || document.body.clientWidth) < this.LAYOUT_BREAKPOINT;
                    },

                    isLayoutNavbarFull: function isLayoutNavbarFull() {
                        return !!document.querySelector('.layout-wrapper.layout-navbar-full');
                    },

                    isCollapsed: function isCollapsed() {
                        if (this.isSmallScreen()) {
                            return !this._hasClass('layout-menu-expanded');
                        }
                        // BÜYÜK EKRANLARDA document.documentElement ÜZERÝNDEN KONTROL EDÝLMELÝ
                        return this._hasClass('layout-menu-collapsed' /*, this.ROOT_EL VEYA HEDEFLENEN ELEMENT */);
                    },

                    isFixed: function isFixed() {
                        return this._hasClass('layout-menu-fixed layout-menu-fixed-offcanvas');
                    },

                    isNavbarFixed: function isNavbarFixed() {
                        return this._hasClass('layout-navbar-fixed') || !this.isSmallScreen() && this.isFixed() && this.isLayoutNavbarFull();
                    },

                    isFooterFixed: function isFooterFixed() {
                        return this._hasClass('layout-footer-fixed');
                    },

                    isLightStyle: function isLightStyle() {
                        return document.documentElement.classList.contains('light-style'); // Temanýzda 'light-style' yerine data-bs-theme="light" kullanýlýyor.
                    },

                    on: function on() {
                        var event = arguments.length > 0 && arguments[0] !== undefined ? arguments[0] : requiredParam('event');
                        var callback = arguments.length > 1 && arguments[1] !== undefined ? arguments[1] : requiredParam('callback');
                        var _event$split = event.split('.'),
                            _event$split2 = _slicedToArray(_event$split, 1),
                            _event = _event$split2[0];
                        var _event$split3 = event.split('.'),
                            _event$split4 = _toArray(_event$split3),
                            namespace = _event$split4.slice(1);
                        namespace = namespace.join('.') || null;
                        this._listeners.push({
                            event: _event,
                            namespace: namespace,
                            callback: callback
                        });
                    },

                    off: function off() {
                        var _this9 = this;
                        var event = arguments.length > 0 && arguments[0] !== undefined ? arguments[0] : requiredParam('event');
                        var _event$split5 = event.split('.'),
                            _event$split6 = _slicedToArray(_event$split5, 1),
                            _event = _event$split6[0];
                        var _event$split7 = event.split('.'),
                            _event$split8 = _toArray(_event$split7),
                            namespace = _event$split8.slice(1);
                        namespace = namespace.join('.') || null;
                        this._listeners.filter(function (listener) {
                            return listener.event === _event && listener.namespace === namespace;
                        }).forEach(function (listener) {
                            return _this9._listeners.splice(_this9._listeners.indexOf(listener), 1);
                        });
                    },

                    init: function init() {
                        var _this10 = this;
                        if (this._initialized) return;
                        this._initialized = true;
                        this._updateInlineStyle(0);
                        this._bindWindowResizeEvent();
                        this.off('init._Helpers');
                        this.on('init._Helpers', function () {
                            _this10.off('resize._Helpers:redrawMenu');
                            _this10.on('resize._Helpers:redrawMenu', function () {
                                _this10.isSmallScreen() && !_this10.isCollapsed() && _this10._redrawLayoutMenu();
                            });
                            if (typeof document.documentMode === 'number' && document.documentMode < 11) {
                                _this10.off('resize._Helpers:ie10RepaintBody');
                                _this10.on('resize._Helpers:ie10RepaintBody', function () {
                                    if (_this10.isFixed()) return;
                                    var scrollTop = document.documentElement.scrollTop;
                                    document.body.style.display = 'none';
                                    document.body.style.display = 'block';
                                    document.documentElement.scrollTop = scrollTop;
                                });
                            }
                        });
                        this._triggerEvent('init');
                    },

                    destroy: function destroy() {
                        var _this11 = this;
                        if (!this._initialized) return;
                        this._initialized = false;
                        this._removeClass('layout-transitioning');
                        this._removeInlineStyle();
                        this._unbindLayoutAnimationEndEvent();
                        this._unbindWindowResizeEvent();
                        this._unbindMenuMouseEvents();
                        this.setAutoUpdate(false);
                        this.off('init._Helpers');
                        this._listeners.filter(function (listener) {
                            return listener.event !== 'init';
                        }).forEach(function (listener) {
                            return _this11._listeners.splice(_this11._listeners.indexOf(listener), 1);
                        });
                    },

                    initPasswordToggle: function initPasswordToggle() {
                        var toggler = document.querySelectorAll('.form-password-toggle i');
                        if (typeof toggler !== 'undefined' && toggler !== null) {
                            toggler.forEach(function (el) {
                                el.addEventListener('click', function (e) {
                                    e.preventDefault();
                                    var formPasswordToggle = el.closest('.form-password-toggle');
                                    var formPasswordToggleIcon = formPasswordToggle.querySelector('i');
                                    var formPasswordToggleInput = formPasswordToggle.querySelector('input');
                                    if (formPasswordToggleInput.getAttribute('type') === 'text') {
                                        formPasswordToggleInput.setAttribute('type', 'password');
                                        formPasswordToggleIcon.classList.replace('bx-show', 'bx-hide');
                                    } else if (formPasswordToggleInput.getAttribute('type') === 'password') {
                                        formPasswordToggleInput.setAttribute('type', 'text');
                                        formPasswordToggleIcon.classList.replace('bx-hide', 'bx-show');
                                    }
                                });
                            });
                        }
                    },

                    initSpeechToText: function initSpeechToText() {
                        var SpeechRecognition = window.SpeechRecognition || window.webkitSpeechRecognition;
                        var speechToText = document.querySelectorAll('.speech-to-text');
                        if (SpeechRecognition !== undefined && SpeechRecognition !== null) {
                            if (typeof speechToText !== 'undefined' && speechToText !== null) {
                                var recognition = new SpeechRecognition();
                                var toggler = document.querySelectorAll('.speech-to-text i');
                                toggler.forEach(function (el) {
                                    var listening = false;
                                    el.addEventListener('click', function () {
                                        el.closest('.input-group').querySelector('.form-control').focus();
                                        recognition.onspeechstart = function () {
                                            listening = true;
                                        };
                                        if (listening === false) {
                                            recognition.start();
                                        }
                                        recognition.onerror = function () {
                                            listening = false;
                                        };
                                        recognition.onresult = function (event) {
                                            el.closest('.input-group').querySelector('.form-control').value = event.results[0][0].transcript;
                                        };
                                        recognition.onspeechend = function () {
                                            listening = false;
                                            recognition.stop();
                                        };
                                    });
                                });
                            }
                        }
                    },

                    ajaxCall: function ajaxCall(url) {
                        return new Promise(function (resolve, reject) {
                            var req = new XMLHttpRequest();
                            req.open('GET', url);
                            req.onload = function () {
                                return req.status === 200 ? resolve(req.response) : reject(Error(req.statusText));
                            };
                            req.onerror = function (e) {
                                return reject(Error("Network Error: ".concat(e)));
                            };
                            req.send();
                        });
                    },

                    initSidebarToggle: function initSidebarToggle() {
                        var sidebarToggler = document.querySelectorAll('[data-bs-toggle=\"sidebar\"]');
                        sidebarToggler.forEach(function (el) {
                            el.addEventListener('click', function () {
                                var target = el.getAttribute('data-target');
                                var overlay = el.getAttribute('data-overlay');
                                var appOverlay = document.querySelectorAll('.app-overlay');
                                var targetEl = document.querySelectorAll(target);
                                targetEl.forEach(function (tel) {
                                    tel.classList.toggle('show');
                                    if (typeof overlay !== 'undefined' && overlay !== null && overlay !== false && typeof appOverlay !== 'undefined') {
                                        if (tel.classList.contains('show')) {
                                            appOverlay[0].classList.add('show');
                                        } else {
                                            appOverlay[0].classList.remove('show');
                                        }
                                        appOverlay[0].addEventListener('click', function (e) {
                                            e.currentTarget.classList.remove('show');
                                            tel.classList.remove('show');
                                        });
                                    }
                                });
                            });
                        });
                    },

                    getCssVar: function getCssVar(color) {
                        var isChartJs = arguments.length > 1 && arguments[1] !== undefined ? arguments[1] : false;
                        if (isChartJs === true) {
                            return getComputedStyle(document.documentElement).getPropertyValue("--".concat(window.Helpers.prefix).concat(color)).trim();
                        }
                        return "var(--".concat(window.Helpers.prefix).concat(color, ")");
                    }
                };

                if (typeof window !== 'undefined') {
                    Helpers.init();
                    if (Helpers.isMobileDevice() && window.chrome) {
                        document.documentElement.classList.add('layout-menu-100vh');
                    }
                    if (document.readyState === 'complete') {
                        Helpers.update();
                    } else {
                        document.addEventListener('DOMContentLoaded', function onContentLoaded() {
                            Helpers.update();
                            document.removeEventListener('DOMContentLoaded', onContentLoaded);
                        });
                    }
                }

                window.Helpers = Helpers; // Helpers objesini global window'a ata
            }
        };
        var __webpack_require__ = {
            d: function (e, n) {
                for (var t in n) {
                    if (__webpack_require__.o(n, t) && !__webpack_require__.o(e, t)) {
                        Object.defineProperty(e, t, {
                            enumerable: true,
                            get: n[t]
                        });
                    }
                }
            },
            o: function (e, n) {
                return Object.prototype.hasOwnProperty.call(e, n);
            },
            r: function (e) {
                if (typeof Symbol !== 'undefined' && Symbol.toStringTag) {
                    Object.defineProperty(e, Symbol.toStringTag, {
                        value: 'Module'
                    });
                }
                Object.defineProperty(e, '__esModule', {
                    value: true
                });
            }
        };
        var __webpack_exports__ = {};
        __webpack_modules__["./js/helpers.js"](0, __webpack_exports__, __webpack_require__);
        return __webpack_exports__;
    })();
});