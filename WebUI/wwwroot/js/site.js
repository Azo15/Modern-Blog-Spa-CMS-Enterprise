(() => {
    'use strict';
    const ready = () => {
        const reduced = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
        document.body.classList.add('js-ready');
        if (window.AOS && !reduced) AOS.init({duration:800, once:true, offset:45, easing:'ease-out-cubic'});
        if (window.Fancybox) Fancybox.bind('[data-fancybox]', {hideScrollbar:false});
        const header=document.getElementById('site-header');
        const nav=document.getElementById('navbarNav');
        const toggle=document.querySelector('.luxe-menu-toggle');
        const scroll=()=>header?.classList.toggle('is-scrolled',window.scrollY>35);
        scroll();window.addEventListener('scroll',scroll,{passive:true});
        nav?.addEventListener('show.bs.collapse',()=>header?.classList.add('menu-open'));
        nav?.addEventListener('hidden.bs.collapse',()=>header?.classList.remove('menu-open'));
        document.addEventListener('keydown',event=>{if(event.key==='Escape' && nav?.classList.contains('show') && window.bootstrap){bootstrap.Collapse.getOrCreateInstance(nav).hide();toggle?.focus();}});
        window.addEventListener('resize',()=>{if(window.innerWidth>=1200 && header?.classList.contains('menu-open')){bootstrap.Collapse.getOrCreateInstance(nav).hide();}},{passive:true});
        document.querySelectorAll('img').forEach(image=>image.addEventListener('error',()=>{if(!image.dataset.recovered){image.dataset.recovered='true';image.src='/img/about-retreat.webp';}},{once:true}));
    };
    if(document.readyState==='loading') document.addEventListener('DOMContentLoaded',ready); else ready();
})();
