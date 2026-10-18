(() => {
'use strict';
const ready=()=>{
 const modal=document.getElementById('ai-blog-modal'), form=document.getElementById('ai-blog-form');if(!modal||!form)return;
 const status=document.getElementById('ai-blog-status'), button=form.querySelector('button[type=submit]'), spinner=form.querySelector('.ai-spinner'),label=form.querySelector('[data-ai-label]');
 const title=document.querySelector('input[name=Title]'),subtitle=document.querySelector('input[name=SubTitle]'),content=document.querySelector('textarea[name=Content]'),category=document.querySelector('select[name=CatagoryId]');
 let busy=false;
 const cleanHtml=html=>{
  const doc=new DOMParser().parseFromString(html,'text/html');const allowed=new Set(['H2','H3','P','UL','OL','LI','STRONG','EM','BR','BLOCKQUOTE']);
  for(const node of [...doc.body.querySelectorAll('*')].reverse()){
   if(!allowed.has(node.tagName)){if(['SCRIPT','STYLE','IFRAME','OBJECT','FORM','SVG','MATH','TEMPLATE'].includes(node.tagName))node.remove();else node.replaceWith(...node.childNodes);}
   else for(const attribute of [...node.attributes])node.removeAttribute(attribute.name);
  }
  return doc.body.innerHTML;
 };
 const valid=draft=>draft&&typeof draft.title==='string'&&draft.title.length>=5&&draft.title.length<=150&&typeof draft.subtitle==='string'&&draft.subtitle.length>=10&&typeof draft.html==='string'&&draft.html.length<=100000&&typeof draft.summary==='string';
 const apply=(draft,categoryId)=>{
  if(!valid(draft)||!title||!subtitle||!content)throw new Error('AI taslağı forma aktarılamadı.');
  title.value=draft.title;subtitle.value=draft.subtitle;content.value=cleanHtml(draft.html);
  if(category&&[...category.options].some(x=>x.value===String(categoryId)))category.value=String(categoryId);
  const editor=window.tinymce?.get(content.id);
  const sync=()=>{editor.setContent(content.value);editor.save();};
  if(editor){if(editor.initialized)sync();else editor.once('init',sync);}
  else if(window.CKEDITOR?.instances?.[content.id])window.CKEDITOR.instances[content.id].setData(content.value);
  for(const input of [title,subtitle,content,category]){input?.dispatchEvent(new Event('input',{bubbles:true}));input?.dispatchEvent(new Event('change',{bubbles:true}));}
 };
 const query=new URLSearchParams(location.search);
 if(query.get('aiDraft')==='1'&&title){
  try{const raw=sessionStorage.getItem('xspa-ai-draft');sessionStorage.removeItem('xspa-ai-draft');const pending=raw?JSON.parse(raw):null;if(pending&&Date.now()-pending.created<300000){apply(pending.draft,pending.categoryId);status.textContent='AI taslağı forma aktarıldı. Kaydetmeden önce doğrulayın.';}}
  catch{status.textContent='Taslak aktarımı tamamlanamadı. Yeniden üretin.';}
 }
 if(query.get('ai')==='1'&&window.bootstrap)bootstrap.Modal.getOrCreateInstance(modal).show();
 form.addEventListener('submit',async event=>{
  event.preventDefault();if(busy||!form.reportValidity())return;
  if(title&&(title.value.trim()||subtitle.value.trim()||content.value.trim())&&!document.getElementById('ai-overwrite').checked){status.textContent='Mevcut içeriği değiştirmek için izin kutusunu işaretleyin.';return;}
  busy=true;button.disabled=true;spinner.hidden=false;label.textContent='Özenle hazırlanıyor…';form.setAttribute('aria-busy','true');status.textContent='AI taslağı hazırlanıyor. Bu işlem bir süre alabilir.';
  const abort=new AbortController(),timeout=setTimeout(()=>abort.abort(),100000);
  try{
   const response=await fetch(form.action,{method:'POST',body:new FormData(form),credentials:'same-origin',signal:abort.signal,headers:{Accept:'application/json','X-Requested-With':'XMLHttpRequest'}});
   if(response.status===429)throw new Error('Üretim sınırına ulaşıldı. Bir dakika sonra yeniden deneyin.');
   const result=response.headers.get('content-type')?.includes('application/json')?await response.json():null;
   if(!response.ok)throw new Error(result?.message||'Üretim tamamlanamadı. Oturumunuzu ve bağlantınızı kontrol edin.');
   if(!valid(result))throw new Error('AI yanıtı geçersiz. Formunuz değiştirilmedi.');
   const categoryId=form.querySelector('[name=CategoryId]').value;
   if(!title){sessionStorage.setItem('xspa-ai-draft',JSON.stringify({draft:result,categoryId,created:Date.now()}));location.assign('/AdminBlog/Create?aiDraft=1');return;}
   apply(result,categoryId);status.textContent='Taslak forma aktarıldı. Özet: '+result.summary;bootstrap.Modal.getOrCreateInstance(modal).hide();
   let notice=document.getElementById('ai-draft-notice');if(!notice){notice=document.createElement('p');notice.id='ai-draft-notice';notice.className='alert alert-warning';title.closest('form').before(notice);}notice.textContent='AI taslağı hazır. Bilgileri doğrulayıp düzenleyin; henüz kaydedilmedi veya yayımlanmadı. Özet: '+result.summary;
  }catch(error){status.textContent=error.name==='AbortError'?'Üretim zaman aşımına uğradı. Tekrar deneyin.':error.message||'Üretim tamamlanamadı.';}
  finally{clearTimeout(timeout);busy=false;button.disabled=false;spinner.hidden=true;label.textContent='Makaleyi Oluştur';form.removeAttribute('aria-busy');}
 });
};
if(document.readyState==='loading')document.addEventListener('DOMContentLoaded',ready);else ready();
})();
