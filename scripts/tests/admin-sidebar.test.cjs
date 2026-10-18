const test=require('node:test');const assert=require('node:assert/strict');const vm=require('node:vm');const fs=require('node:fs');const path=require('node:path');
const source=fs.readFileSync(path.join(__dirname,'../../WebUI/wwwroot/js/admin-sidebar.js'),'utf8');
function fixture(current='/AdminBlog/Edit/1'){
 const groups=['overview','editorial','system'].map(id=>({id,button:{attrs:{},setAttribute(k,v){this.attrs[k]=v;},getAttribute(k){return this.attrs[k];},addEventListener(k,v){this.click=v;}},panel:{style:{},setAttribute(k,v){this[k]=v;}},querySelector(s){return s==='.sidebar-group-title'?this.button:this.panel;}}));
 const links=['/AdminDashboard','/AdminBlog','/AdminSettings/Smtp'].map((href,i)=>({href:'https://localhost'+href,attrs:{},setAttribute(k,v){this.attrs[k]=v;},closest(){return groups[i];}}));
 const root={querySelectorAll(s){return s==='.sidebar-group'?groups:links;}};
 vm.runInNewContext(source,{URL,location:{pathname:current,origin:'https://localhost',search:''},document:{readyState:'complete',querySelector(){return root;}}});return{groups,links};
}
test('sidebar highlights parent link for edit routes and keeps its group open',()=>{const{groups,links}=fixture();assert.equal(links[1].attrs['aria-current'],'page');assert.equal(groups[1].button.attrs['aria-expanded'],'true');assert.equal(groups[0].panel.inert,true);});
test('opening another group closes the previous one',()=>{const{groups}=fixture();groups[2].button.click();assert.equal(groups[2].button.attrs['aria-expanded'],'true');assert.equal(groups[1].button.attrs['aria-expanded'],'false');assert.equal(groups[1].panel.inert,true);});
test('open group can collapse without losing active page marker',()=>{const{groups,links}=fixture();groups[1].button.click();assert.equal(groups[1].panel.style.gridTemplateRows,'0fr');assert.equal(links[1].attrs['aria-current'],'page');});
test('sidebar initialization is harmless when absent',()=>{assert.doesNotThrow(()=>vm.runInNewContext(source,{document:{readyState:'complete',querySelector(){return null;}}}));});
