(() => {
'use strict';
const selector='button:not(.btn-close):not(.sidebar-group-title),a.btn,a.luxe-button,a.auth-google';
const reduced=window.matchMedia('(prefers-reduced-motion: reduce)');
document.addEventListener('pointerdown',event=>{
 if(reduced.matches || event.button!==0) return;
 const control=event.target.closest?.(selector);
 if(!control || control.disabled || control.getAttribute('aria-disabled')==='true' || control.getAttribute('aria-busy')==='true' || control.closest('.tox,.fancybox__container'))return;
 const bounds=control.getBoundingClientRect();if(!bounds.width||!bounds.height)return;
 control.querySelector('.xspa-ripple')?.remove();
 const ripple=document.createElement('span'),size=Math.max(bounds.width,bounds.height);
 ripple.className='xspa-ripple';ripple.setAttribute('aria-hidden','true');
 ripple.style.setProperty('--ripple-size',size+'px');ripple.style.setProperty('--ripple-left',(event.clientX-bounds.left-size/2)+'px');ripple.style.setProperty('--ripple-top',(event.clientY-bounds.top-size/2)+'px');
 control.classList.add('xspa-ripple-host');control.append(ripple);setTimeout(()=>ripple.remove(),650);
});
})();
