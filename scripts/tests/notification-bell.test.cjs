const test=require('node:test'),assert=require('node:assert/strict'),fs=require('node:fs'),vm=require('node:vm');
const source=fs.readFileSync('WebUI/wwwroot/js/notification-bell.js','utf8');
class Element {
 constructor(){this.dataset={};this.children=[];this.textContent='';this.listeners={};this.classList={add(){},remove(){}};}
 setAttribute(name,value){this[name]=value;} addEventListener(name,callback){this.listeners[name]=callback;}
 append(...nodes){this.children.push(...nodes);} replaceChildren(...nodes){this.children=nodes;}
 set innerHTML(value){throw Error('Unsafe HTML rendering');}
}
function fixture(){
 const panel=new Element(),list=new Element(),status=new Element(),badge=new Element(),bell=new Element(),events={},navigations=[],calls=[],timers=[];let requests=0;
 let data={items:[{id:7,title:'<script>unsafe()</script>',content:'<img src=x onerror=unsafe()>',url:'javascript:unsafe()',isRead:false,createdAtUtc:'2026-10-24T10:00:00'}],unreadCount:1,latestId:7};
 panel.dataset.listUrl='/admin/notifications/list';panel.querySelector=s=>s==='[data-notification-list]'?list:status;
 const document={hidden:false,querySelectorAll:s=>s==='[data-notifications]'?[panel]:[badge],querySelector:()=>panel,getElementById:()=>bell,createElement:()=>new Element(),addEventListener(){}};
 const context={document,URL,FormData,Intl,Date,setInterval(){},setTimeout:(callback,delay)=>timers.push({callback,delay}),fetch:async()=>{requests++;return {ok:true,json:async()=>structuredClone(data)};},window:{location:{origin:'https://xspa.test',assign:value=>navigations.push(value)},addEventListener:(name,callback)=>events[name]=callback,XspaAjax:{postForm:async(form,body)=>{calls.push({action:form.action,id:body.get('id'),throughId:body.get('throughId')});data.items[0].isRead=true;data.unreadCount=0;return {success:true,unreadCount:0};}}}};
 vm.runInNewContext(source,context);return {panel,list,status,badge,bell,calls,navigations,events,timers,requests:()=>requests};
}
const flush=()=>new Promise(resolve=>setImmediate(resolve));
test('bell renders text safely, blocks external URLs and shows unread badge',async()=>{
 const f=fixture();await flush();assert.equal(f.badge.textContent,'1');assert.equal(f.badge.hidden,false);
 const link=f.list.children[0].children[0];assert.equal(link.href,'/AdminDashboard');assert.equal(link.children[0].textContent,'<script>unsafe()</script>');assert.equal(link.children[1].textContent,'<img src=x onerror=unsafe()>');
});
test('AJAX read updates badge without page navigation or reloading',async()=>{
 const f=fixture();await flush();const button=f.list.children[0].children[1];
 await f.panel.listeners.click({target:{closest:s=>s==='[data-notification-read]'?button:null},preventDefault(){}});
 assert.equal(f.calls[0].action,'/admin/notifications/read');assert.equal(f.calls[0].id,'7');assert.equal(f.badge.hidden,true);assert.equal(f.navigations.length,0);
});
test('bulk read sends the snapshot boundary through the shared AJAX helper',async()=>{
 const f=fixture();await flush();const button=new Element();
 await f.panel.listeners.click({target:{closest:s=>s==='[data-notification-all]'?button:null},preventDefault(){}});
 assert.equal(f.calls[0].action,'/admin/notifications/read-all');assert.equal(f.calls[0].throughId,'7');assert.equal(f.navigations.length,0);
});

test('live counts update immediately and a burst triggers one list refresh',async()=>{
 const f=fixture();await flush();
 f.events['xspa:notifications-changed']({detail:{unreadCount:5}});
 f.events['xspa:notifications-changed']({detail:{unreadCount:6}});
 assert.equal(f.badge.textContent,'6');assert.equal(f.requests(),1);
 const pending=f.timers.filter(timer=>timer.delay===120);assert.equal(pending.length,1);
 pending[0].callback();await flush();assert.equal(f.requests(),2);
});
