(() => {
'use strict';
const ready=()=>{
 const root=document.querySelector('[data-sidebar-groups]'); if(!root) return;
 const groups=[...root.querySelectorAll('.sidebar-group')];
 const normalize=path=>path.replace(/\/Index\/?$/i,'').replace(/\/$/,'').toLowerCase();
 const current=normalize(location.pathname); let active=null,score=-1;
 for(const link of root.querySelectorAll('a[href]')){
  const url=new URL(link.href,location.origin);if(url.origin!==location.origin)continue;
  const path=normalize(url.pathname), queryMatches=!url.search || url.search===location.search;
  const match=current===path || current.startsWith(path+'/');
  if(match && queryMatches && path.length>score){active=link;score=path.length;}
 }
 active?.setAttribute('aria-current','page');
 const setOpen=(group,open)=>{
  const button=group.querySelector('.sidebar-group-title'),panel=group.querySelector('.sidebar-group-panel');
  button.setAttribute('aria-expanded',String(open)); panel.style.gridTemplateRows=open?'1fr':'0fr';panel.inert=!open;panel.setAttribute('aria-hidden',String(!open));
 };
 const initial=active?.closest('.sidebar-group') || groups[0];
 groups.forEach(group=>{
  setOpen(group,group===initial);
  group.querySelector('.sidebar-group-title').addEventListener('click',()=>{
   const open=group.querySelector('.sidebar-group-title').getAttribute('aria-expanded')!=='true';
   groups.forEach(other=>setOpen(other,other===group && open));
  });
 });
};
if(document.readyState==='loading')document.addEventListener('DOMContentLoaded',ready);else ready();
})();
