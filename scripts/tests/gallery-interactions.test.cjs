const test=require('node:test'),assert=require('node:assert/strict'),vm=require('node:vm'),fs=require('node:fs'),path=require('node:path');
const source=fs.readFileSync(path.join(__dirname,'../../WebUI/wwwroot/js/site.js'),'utf8');
const block=source.slice(source.indexOf('        const galleryFilters ='),source.indexOf('        const businessStatuses ='));
function boot(reduce=false){
 let plays=0,cancels=0;
 const items=['mekan','ekip','mekan'].map((category,i)=>({hidden:false,dataset:{category},getBoundingClientRect(){return{left:i*100,top:0};},animate(){plays++;return{cancel(){cancels++;}};}}));
 const buttons=['all','mekan','ekip','urunler'].map(category=>({dataset:{galleryFilter:category},badge:{textContent:''},attrs:{},querySelector(){return this.badge;},setAttribute(k,v){this.attrs[k]=v;},addEventListener(event,fn){this.click=fn;}}));
 const filters={hidden:true,querySelectorAll(){return buttons;}},status={textContent:''},empty={hidden:true};
 const document={querySelector(s){return s==='[data-gallery-filters]'?filters:s==='[data-gallery-count]'?status:empty;},querySelectorAll(){return items;}};
 vm.runInNewContext(block,{document,reduced:{matches:reduce},text:(tr)=>tr});
 return{items,buttons,filters,status,empty,plays:()=>plays,cancels:()=>cancels};
}
test('gallery badges show category totals and all photos stay visible initially',()=>{const f=boot();assert.deepEqual(f.buttons.map(x=>x.badge.textContent),['3','2','1','0']);assert.equal(f.filters.hidden,false);assert.ok(f.items.every(x=>!x.hidden));});
test('category selection updates accessibility and limits lightbox to visible photos',()=>{const f=boot();f.buttons[2].click();assert.equal(f.buttons[2].attrs['aria-pressed'],'true');assert.equal(f.buttons[0].attrs['aria-pressed'],'false');assert.deepEqual(f.items.map(x=>x.hidden),[true,false,true]);assert.equal(f.items[0].dataset.fancybox,'gallery-filtered-out');assert.equal(f.items[1].dataset.fancybox,'spa-gallery');assert.equal(f.status.textContent,'1 fotoğraf');});
test('empty category and restoring all photos remain functional',()=>{const f=boot();f.buttons[3].click();assert.equal(f.empty.hidden,false);f.buttons[0].click();assert.ok(f.items.every(x=>!x.hidden));assert.equal(f.empty.hidden,true);});
test('reduced motion never starts a gallery animation',()=>{const f=boot(true);f.buttons[1].click();assert.equal(f.plays(),0);});
test('rapid changes cancel earlier animations instead of stacking them',()=>{const f=boot();f.buttons[1].click();f.buttons[2].click();assert.ok(f.cancels()>0);assert.ok(f.plays()>0);});
