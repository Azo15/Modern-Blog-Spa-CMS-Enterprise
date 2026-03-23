document.addEventListener('DOMContentLoaded', function () {
    const heroSwiperElement = document.querySelector('.hero-swiper');

    if (heroSwiperElement) {

        const slideCount = heroSwiperElement.querySelectorAll('.swiper-slide').length;

        if (slideCount > 1) {
            const swiper = new Swiper('.hero-swiper', {
                loop: true,

                effect: 'fade',
                fadeEffect: {
                    crossFade: true
                },

                autoplay: {
                    delay: 5000,
                    disableOnInteraction: false,
                },

                pagination: {
                    el: '.swiper-pagination',
                    clickable: true,
                },

                navigation: {
                    nextEl: '.swiper-button-next',
                    prevEl: '.swiper-button-prev',
                },

                speed: 1000,
            });
        }
    }
});