document.addEventListener('DOMContentLoaded',()=>{
    const element=document.querySelector('.hero-swiper');
    if(!element || !window.Swiper)return;
    const en=document.documentElement.lang==='en-US';
    const reduced=window.matchMedia('(prefers-reduced-motion: reduce)').matches;
    const pause=document.querySelector('.luxe-pause');
    let paused=reduced;
    const slider=new Swiper(element,{
        loop:true,effect:'fade',fadeEffect:{crossFade:true},speed:reduced?0:1000,
        autoplay:reduced?false:{delay:6500,disableOnInteraction:false,pauseOnMouseEnter:true},
        pagination:{el:'.luxe-hero-controls .swiper-pagination',clickable:true},
        navigation:{nextEl:'.luxe-slide-next',prevEl:'.luxe-slide-prev'},
        a11y:{enabled:true,prevSlideMessage:en?'Previous slide':'Önceki görsel',nextSlideMessage:en?'Next slide':'Sonraki görsel',paginationBulletMessage:en?'Go to slide {{index}}':'{{index}}. görsele git'}
    });
    const update=()=>{pause?.setAttribute('aria-pressed',String(paused));pause?.setAttribute('aria-label',paused?(en?'Play slideshow':'Slaytları oynat'):(en?'Pause slideshow':'Slaytları duraklat'));const icon=pause?.querySelector('i');if(icon)icon.className='fa-solid fa-'+(paused?'play':'pause');};
    update();pause?.addEventListener('click',()=>{paused=!paused;if(paused)slider.autoplay?.stop();else slider.autoplay?.start();update();});
    element.addEventListener('focusin',()=>slider.autoplay?.pause());
    element.addEventListener('focusout',event=>{if(!paused&&!element.contains(event.relatedTarget))slider.autoplay?.resume();});
    document.addEventListener('visibilitychange',()=>{if(document.hidden)slider.autoplay?.pause();else if(!paused)slider.autoplay?.resume();});
});
