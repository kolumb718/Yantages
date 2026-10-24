import * as THREE from 'three';
const scene = new THREE.Scene();
scene.background = new THREE.Color('#111c22');
scene.fog = new THREE.FogExp2('#111c22', .022);
const renderer = new THREE.WebGLRenderer({antialias:true});
renderer.setPixelRatio(Math.min(devicePixelRatio,2));
renderer.shadowMap.enabled=true;renderer.shadowMap.type=THREE.PCFSoftShadowMap;
renderer.toneMapping=THREE.ACESFilmicToneMapping;renderer.toneMappingExposure=1.35;
document.querySelector('#scene').append(renderer.domElement);
const camera = new THREE.PerspectiveCamera(35,1,.1,100);
camera.position.set(13,10,22);camera.lookAt(0,1.5,0);
scene.add(new THREE.HemisphereLight('#81989f','#28251f',1.3));
const mat=(c,r= .9)=>new THREE.MeshStandardMaterial({color:c,roughness:r});
const metal=mat('#334846'),dark=mat('#182c2c'),rust=mat('#705647'),cream=mat('#a69d7a'),cloth=mat('#6b7563');
function box(w,h,d,x,y,z,m=metal,parent=scene){const o=new THREE.Mesh(new THREE.BoxGeometry(w,h,d),m);o.position.set(x,y,z);o.castShadow=true;o.receiveShadow=true;parent.add(o);return o;}
function cylinder(r,h,x,y,z,m=metal,parent=scene){const o=new THREE.Mesh(new THREE.CylinderGeometry(r,r,h,14),m);o.position.set(x,y,z);o.castShadow=true;o.receiveShadow=true;parent.add(o);return o;}
function sphere(r,x,y,z,m,parent=scene){const o=new THREE.Mesh(new THREE.SphereGeometry(r,16,12),m);o.position.set(x,y,z);o.castShadow=true;parent.add(o);return o;}
function light(color,power,x,y,z){const l=new THREE.PointLight(color,power,12,2);l.position.set(x,y,z);l.castShadow=true;l.shadow.mapSize.set(1024,1024);scene.add(l);return l;}
// Open-sided ship, built as a theatrical cutaway.
box(17,.45,7,0,-.3,0,dark);box(17,5,.3,0,2.3,-3.4,metal);
box(.3,4,7,-8.5,1.8,0,metal);box(.3,2,7,8.5,.8,0,metal);
for(let x=-8;x<8;x+=1.2){box(1.16,.05,6.8,x,.0,0,mat(x%2?'#38433d':'#343d38'));box(.09,4.8,.35,x,2.2,-3.13,dark)}
for(let x=-8;x<=8;x+=4){box(.23,.25,7,x,4.65,0,dark);box(.23,4.8,.3,x,2.25,-3,dark)}
for(let y=3.7;y<4.5;y+=.3){const p=cylinder(.08,16,0,y,-2.8,rust);p.rotation.z=Math.PI/2;}
for(let x=-7;x<7;x+=2){box(.1,.9,.25,x,4,-2.75,dark)}
// Earthside window and distant city lights.
box(4.3,2.5,.18,5.3,2.65,-3.15,dark);
box(3.8,2.1,.12,5.3,2.65,-3.03,new THREE.MeshStandardMaterial({color:'#345f6b',emissive:'#32586b',emissiveIntensity:.8}));
for(let i=0;i<12;i++){const x=3.5+i*.3,h=.3+(Math.sin(i*5)+1)*.35;box(.24,h,.05,x,1.7+h/2,-2.94,mat('#203e4c'));for(let j=0;j<3;j++)box(.035,.04,.02,x,1.85+j*.16,-2.9,new THREE.MeshBasicMaterial({color:i%3?'#6c9d9b':'#d2aa70'}));}
box(.09,2.2,.16,5.3,2.65,-2.86,dark);box(4,.09,.16,5.3,2.6,-2.86,dark);
light('#8abac9',35,5,3,-1.7);
// Bed, bedding, lamp and possessions.
box(3.3,.48,1.6,-5.6,.55,-1.9,dark);box(3.1,.25,1.5,-5.6,.9,-1.9,cream);box(2.15,.2,1.52,-5.2,1.08,-1.9,cloth);box(.65,.24,1.15,-6.7,1.1,-1.9,mat('#b4ad90'));
for(let x=-6.1;x<-4.2;x+=.24)box(.045,.025,1.52,x,1.2,-1.9,mat('#596655'));
box(.7,.8,.8,-7.5,.4,-2, rust);cylinder(.06,.6,-7.5,1.1,-2,dark);cylinder(.28,.3,-7.5,1.5,-2,cream);light('#ffc481',45,-7.5,1.6,-1.6);
box(.38,.1,.65,-5.6,.12,-.65,rust);box(.38,.1,.65,-5.05,.12,-.65,rust);
// Galley.
box(2,1.35,1.2,-2.2,.68,-2.4,dark);box(2.15,.12,1.35,-2.2,1.42,-2.4,cream);box(.9,.04,.7,-2.5,1.5,-2.3,metal);cylinder(.22,.3,-1.55,1.64,-2.3,rust);
box(1.9,.7,.7,-2.2,3,-2.8,cloth);box(.04,.65,.1,-2.2,3,-2.38,dark);
// Order terminal.
box(1.35,1.3,.9,.4,.65,-1.8,dark);const consoleBody=box(1.65,1.2,.4,.4,1.8,-2,metal);consoleBody.rotation.x=-.12;
const screenMat=new THREE.MeshStandardMaterial({color:'#7ea992',emissive:'#8ed7af',emissiveIntensity:1.5});box(1.35,.82,.05,.4,1.88,-1.74,screenMat);
for(let i=0;i<4;i++)box(.9-i*.13,.035,.02,.27,2.12-i*.14,-1.7,new THREE.MeshBasicMaterial({color:'#2c6557'}));
box(1.65,.12,.7,.4,1.14,-1.43,metal);for(let i=0;i<7;i++)box(.12,.03,.25,-.2+i*.19,1.22,-1.35,cream);
const terminalLight=light('#8be7be',23,.4,2,-1.1);
// Cockpit dashboard, chair and cargo.
box(2.6,1,.95,5.6,.65,-2.2,dark);const dash=box(2.7,.15,1.2,5.6,1.25,-2.1,metal);dash.rotation.x=.18;
for(let i=0;i<3;i++)box(.5,.05,.45,4.85+i*.7,1.36,-2.1,screenMat);
cylinder(.12,.6,5.5,.4,-.4,dark);box(1,.22,1,5.5,.78,-.4,rust);box(1,1.2,.2,5.5,1.25,.05,rust);
box(1.7,1.25,1.5,7,.65,1.8,cloth);box(1.8,.15,1.6,7,1.3,1.8,cream);box(.55,.36,.02,7,.8,2.56,mat('#d6ba76'));
box(1.1,.7,1.1,3.2,.38,2,rust);box(.15,.75,1.12,3.2,.39,2,dark);
// Door and hatch hardware.
box(1.5,3,.22,2.35,1.5,-3.12,dark);box(1.15,2.65,.12,2.35,1.5,-2.95,mat('#4a5b54'));const wheel=new THREE.Mesh(new THREE.TorusGeometry(.24,.035,8,20),rust);wheel.position.set(2.35,1.6,-2.83);scene.add(wheel);
// Small courier in a weathered ochre flight jacket.
const player=new THREE.Group();player.position.set(-4,0,.65);scene.add(player);
const jacket=mat('#bd9352'),boots=mat('#252a29');
const body=box(.48,.66,.35,0,.85,0,jacket,player);sphere(.26,0,1.43,0,mat('#a79476'),player);sphere(.28,0,1.56,-.03,jacket,player);box(.35,.18,.06,0,1.45,.23,dark,player);
const legs=[box(.16,.4,.2,-.13,.3,0,boots,player),box(.16,.4,.2,.13,.3,0,boots,player)];
box(.12,.55,.18,-.32,.85,0,jacket,player);box(.12,.55,.18,.32,.85,0,jacket,player);box(.38,.44,.2,0,.9,-.26,rust,player);
const key=new Set(),dialog=document.querySelector('#terminal'),interact=document.querySelector('#interact');let accepted=false,near=false;
function openTerminal(){if(near&&!dialog.open){key.clear();dialog.showModal()}}
window.addEventListener('keydown',e=>{if(['KeyW','KeyA','KeyS','KeyD','ArrowUp','ArrowDown','ArrowLeft','ArrowRight','KeyE'].includes(e.code))e.preventDefault();key.add(e.code);if(e.code==='KeyE'&&!e.repeat)openTerminal()});
window.addEventListener('keyup',e=>key.delete(e.code));window.addEventListener('blur',()=>key.clear());
interact.onclick=openTerminal;document.querySelector('#close').onclick=()=>dialog.close();
document.querySelector('#accept').onclick=()=>{accepted=true;document.querySelector('#accept').disabled=true;document.querySelector('#accept').textContent='Заказ принят ✓';document.querySelector('#status').textContent='Следующий шаг: забрать суп в кафе на Земле. На этом первый прототип заканчивается — корабль можно исследовать дальше.';document.querySelector('#notice').innerHTML='<span class="pulse">✓</span><div>Заказ принят<small>Забрать еду · Земля, кафе «Спутник»</small></div>';};
for(const b of document.querySelectorAll('[data-move]')){const code='Key'+b.dataset.move.toUpperCase();b.onpointerdown=e=>{b.setPointerCapture(e.pointerId);key.add(code)};b.onpointerup=b.onpointercancel=()=>key.delete(code)}
let audio;document.querySelector('#sound').onclick=async()=>{if(!audio){audio=new AudioContext();const o=audio.createOscillator(),g=audio.createGain();o.type='sine';o.frequency.value=55;g.gain.value=.035;o.connect(g).connect(audio.destination);o.start();await audio.suspend()}if(audio.state==='running'){await audio.suspend()}else await audio.resume();document.querySelector('#sound').textContent='Звук: '+(audio.state==='running'?'вкл.':'выкл.')};
function resize(){renderer.setSize(innerWidth,innerHeight);camera.aspect=innerWidth/innerHeight;camera.position.set(13,10,innerWidth<700?38:22);camera.lookAt(0,1.5,0);camera.updateProjectionMatrix()}window.addEventListener('resize',resize);resize();
const clock=new THREE.Clock();let time=0;
function frame(){requestAnimationFrame(frame);const dt=Math.min(clock.getDelta(),.05);time+=dt;let x=0,z=0;if(!dialog.open){x=Number(key.has('KeyD')||key.has('ArrowRight'))-Number(key.has('KeyA')||key.has('ArrowLeft'));z=Number(key.has('KeyS')||key.has('ArrowDown'))-Number(key.has('KeyW')||key.has('ArrowUp'));}const length=Math.hypot(x,z)||1;player.position.x=THREE.MathUtils.clamp(player.position.x+x/length*dt*2.6,-7.5,7.7);player.position.z=THREE.MathUtils.clamp(player.position.z+z/length*dt*2.6,-.45,2.7);if(x||z){player.rotation.y=Math.atan2(x,z);legs[0].rotation.x=Math.sin(time*10)*.4;legs[1].rotation.x=-legs[0].rotation.x;body.position.y=.85+Math.sin(time*20)*.02}else{legs.forEach(l=>l.rotation.x=0)}near=Math.hypot(player.position.x-.4,player.position.z+.65)<1.8;interact.hidden=!near||dialog.open;interact.textContent=accepted?'E · Посмотреть заказ':'E · Открыть терминал';terminalLight.intensity=23+Math.sin(time*2)*2;renderer.render(scene,camera)}frame();
