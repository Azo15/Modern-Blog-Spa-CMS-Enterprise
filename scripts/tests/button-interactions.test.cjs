const test=require('node:test'),assert=require('node:assert/strict'),vm=require('node:vm'),fs=require('node:fs'),path=require('node:path');
const source=fs.readFileSync(path.join(__dirname,'../../WebUI/wwwroot/js/interactions.js'),'utf8');
function boot({reduced=false,disabled=false,busy=false}={}){
 const timers=[],children=[];let handler;
 const control={disabled,attrs:{'aria-busy':busy?'true':'false'},getAttribute(k){return this.attrs[k];},closest(){return null;},querySelector(){return null;},getBoundingClientRect(){return{width:120,height:48,left:10,top:20};},classList:{add(){}},append(node){children.push(node);}};
 const document={addEventListener(name,fn){assert.equal(name,'pointerdown');handler=fn;},createElement(){return{style:{values:{},setProperty(k,v){this.values[k]=v;}},setAttribute(k,v){this[k]=v;},remove(){this.removed=true;}};}};
 vm.runInNewContext(source,{document,window:{matchMedia(){return{matches:reduced};}},setTimeout(fn){timers.push(fn);}});
 return{children,timers,press(button=0){handler({button,clientX:30,clientY:40,target:{closest(){return control;}},preventDefault(){throw Error('native event must remain untouched');}});}};
}
test('ripple supplies decorative feedback and cleans up without cancelling native actions',()=>{const f=boot();f.press();assert.equal(f.children.length,1);assert.equal(f.children[0]['aria-hidden'],'true');f.timers[0]();assert.equal(f.children[0].removed,true);});
test('disabled and busy controls receive no decorative mutations',()=>{for(const options of [{disabled:true},{busy:true}]){const f=boot(options);f.press();assert.equal(f.children.length,0);}});
test('reduced motion preference prevents ripple creation',()=>{const f=boot({reduced:true});f.press();assert.equal(f.children.length,0);});
test('secondary pointer button does not start a ripple',()=>{const f=boot();f.press(2);assert.equal(f.children.length,0);});
