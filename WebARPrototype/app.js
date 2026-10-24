import * as THREE from 'three';
import { OrbitControls } from 'three/addons/controls/OrbitControls.js';

const DATA = [
  ['mercury','Mercury',4879,57900000,88,1407.6,'#a9a39a','A small, heavily cratered world with a year lasting 88 Earth days.'],
  ['venus','Venus',12104,108200000,224.7,-5832.5,'#d99b57','A dense carbon dioxide atmosphere traps heat, making Venus the hottest planet.'],
  ['earth','Earth',12756,149600000,365.2,23.9,'#4d94dc','Earth has stable surface liquid water and is the only world currently known to support life.'],
  ['mars','Mars',6792,228000000,687,24.6,'#c76548','A cold desert world with ancient river valleys, polar ice, and the tallest volcano known.'],
  ['jupiter','Jupiter',142984,778500000,4331,9.9,'#c99e73','The largest planet is a hydrogen-rich gas giant with powerful storms.'],
  ['saturn','Saturn',120536,1432000000,10747,10.7,'#d1b77a','A gas giant whose broad rings are made mostly of water ice particles.'],
  ['uranus','Uranus',51118,2867000000,30589,-17.2,'#7dc8cd','An ice giant that rotates on a strongly tilted axis, giving it extreme seasons.'],
  ['neptune','Neptune',49528,4515000000,59800,16.1,'#456ad1','A distant ice giant with supersonic winds and a large moon, Triton.']
].map(([id,name,diameter,distance,period,rotation,color,description])=>({id,name,diameter,distance,period,rotation,color,description}));
const SUN_KM=1392700;
// JPL approximate Keplerian elements and century rates; J2000 ecliptic, valid 1800–2050.
const ORBITS=[
 [0.38709927,0.20563593,7.00497902,252.25032350,77.45779628,48.33076593,0.00000037,0.00001906,-0.00594749,149472.67411175,0.16047689,-0.12534081],
 [0.72333566,0.00677672,3.39467605,181.97909950,131.60246718,76.67984255,0.00000390,-0.00004107,-0.00078890,58517.81538729,0.00268329,-0.27769418],
 [1.00000261,0.01671123,-0.00001531,100.46457166,102.93768193,0,0.00000562,-0.00004392,-0.01294668,35999.37244981,0.32327364,0],
 [1.52371034,0.09339410,1.84969142,-4.55343205,-23.94362959,49.55953891,0.00001847,0.00007882,-0.00813131,19140.30268499,0.44441088,-0.29257343],
 [5.20288700,0.04838624,1.30439695,34.39644051,14.72847983,100.47390909,-0.00011607,-0.00013253,-0.00183714,3034.74612775,0.21252668,0.20469106],
 [9.53667594,0.05386179,2.48599187,49.95424423,92.59887831,113.66242448,-0.00125060,-0.00050991,0.00193609,1222.49362201,-0.41897216,-0.28867794],
 [19.18916464,0.04725744,0.77263783,313.23810451,170.95427630,74.01692503,-0.00196176,-0.00004397,-0.00242939,428.48202785,0.40805281,0.04240589],
 [30.06992276,0.00859048,1.77004347,-55.12002969,44.96476227,131.78422574,0.00026291,0.00005105,0.00035372,218.45945325,-0.32241464,-0.00508664]
];
const DEG=Math.PI/180;
const $=id=>document.getElementById(id);
const renderer=new THREE.WebGLRenderer({canvas:$('scene'),antialias:true,alpha:true,powerPreference:'high-performance'});
renderer.setPixelRatio(Math.min(devicePixelRatio,2)); renderer.setSize(innerWidth,innerHeight);
renderer.setClearColor(0x000000,0);
renderer.outputColorSpace=THREE.SRGBColorSpace; renderer.xr.enabled=true; renderer.xr.setReferenceSpaceType('local');
const scene=new THREE.Scene();
const camera=new THREE.PerspectiveCamera(48,innerWidth/innerHeight,.001,200);
camera.position.set(0,3.7,8.5);
const controls=new OrbitControls(camera,renderer.domElement); controls.enableDamping=true; controls.target.set(0,0,0);
scene.add(new THREE.HemisphereLight(0xb8ceff,0x202637,1.2));
const sunLight=new THREE.PointLight(0xffdda0,150,0,1.4); scene.add(sunLight);
const root=new THREE.Group(); scene.add(root); root.visible=false;
const system=new THREE.Group(); root.add(system);system.add(sunLight);
const loader=new THREE.TextureLoader();
const textures={};
const textureIds=['sun',...DATA.map(p=>p.id)];
for(const id of textureIds){const texture=loader.load(`assets/textures/${id}.jpg`,undefined,undefined,()=>{});texture.colorSpace=THREE.SRGBColorSpace;textures[id]=texture;}
const sphereGeometry=new THREE.SphereGeometry(1,32,24);
const sunMaterial=new THREE.MeshBasicMaterial({map:textures.sun,color:0xffffff});
const sun=new THREE.Mesh(sphereGeometry,sunMaterial); sun.name='sun'; system.add(sun);
const planetMeshes=[]; const orbitLines=[]; const clock=new THREE.Clock();
for(const p of DATA){
  const mat=new THREE.MeshStandardMaterial({map:textures[p.id],color:textures[p.id]?0xffffff:p.color,roughness:.92,metalness:0});
  const mesh=new THREE.Mesh(sphereGeometry,mat);mesh.name=p.id;mesh.userData.planet=p;system.add(mesh);planetMeshes.push(mesh);
  if(p.id==='saturn'){const rings=new THREE.Mesh(new THREE.RingGeometry(1.25,2.3,64),new THREE.MeshStandardMaterial({color:0xcbb889,transparent:true,opacity:.78,side:THREE.DoubleSide,roughness:1,depthWrite:false}));rings.rotation.x=Math.PI/2;rings.rotation.z=26.7*DEG;mesh.add(rings);}
  const points=[];for(let i=0;i<=128;i++){const a=i/128*Math.PI*2;points.push(new THREE.Vector3(Math.cos(a),0,Math.sin(a)));}
  const orbit=new THREE.Line(new THREE.BufferGeometry().setFromPoints(points),new THREE.LineBasicMaterial({color:0xa8b8d4,transparent:true,opacity:.23}));system.add(orbit);orbitLines.push(orbit);
}
const reticle=new THREE.Group();
const ring=new THREE.Mesh(new THREE.RingGeometry(.08,.095,48),new THREE.MeshBasicMaterial({color:0xe6d292,side:THREE.DoubleSide}));ring.rotation.x=-Math.PI/2;reticle.add(ring);
const cross=new THREE.Mesh(new THREE.RingGeometry(.012,.018,24),new THREE.MeshBasicMaterial({color:0xffffff,side:THREE.DoubleSide}));cross.rotation.x=-Math.PI/2;reticle.add(cross);scene.add(reticle);reticle.visible=false;
const raycaster=new THREE.Raycaster(); const pointer=new THREE.Vector2();
let hitSource=null,placed=false,paused=false,elapsed=0,selected=null,isAR=false,timeScale=1;
const epoch=new Date();let meanAnomalies=[];
let sunDiameter=.6,metersPerAU=.12;
function elementsAt(i){const T=(epoch-Date.UTC(2000,0,1,12))/(36525*86400000),o=ORBITS[i];return{a:o[0]+o[6]*T,e:o[1]+o[7]*T,I:(o[2]+o[8]*T)*DEG,L:o[3]+o[9]*T,peri:o[4]+o[10]*T,node:o[5]+o[11]*T};}
function solveKepler(M,e){let E=M;for(let j=0;j<12;j++){const d=(E-e*Math.sin(E)-M)/(1-e*Math.cos(E));E-=d;if(Math.abs(d)<1e-12)break;}return E;}
function orbitalPosition(i,M){const el=elementsAt(i),w=(el.peri-el.node)*DEG,E=solveKepler(M,el.e),xp=el.a*(Math.cos(E)-el.e),yp=el.a*Math.sqrt(1-el.e*el.e)*Math.sin(E),cw=Math.cos(w),sw=Math.sin(w),co=Math.cos(el.node*DEG),so=Math.sin(el.node*DEG),ci=Math.cos(el.I),si=Math.sin(el.I);return new THREE.Vector3((cw*co-sw*so*ci)*xp+(-sw*co-cw*so*ci)*yp,(sw*si)*xp+(cw*si)*yp,(cw*so+sw*co*ci)*xp+(-sw*so+cw*co*ci)*yp);}
meanAnomalies=DATA.map((_,i)=>{const el=elementsAt(i);return((el.L-el.peri)%360+360)%360*DEG;});
for(let i=0;i<DATA.length;i++){
  const points=[];for(let j=0;j<=160;j++)points.push(orbitalPosition(i,j/160*Math.PI*2));
  orbitLines[i].geometry.dispose();orbitLines[i].geometry=new THREE.BufferGeometry().setFromPoints(points);
}

function rebuildScale(){
  sun.scale.setScalar(sunDiameter/2);sunLight.position.set(0,0,0);sunLight.intensity=Math.max(30,sunDiameter*250);
  DATA.forEach((p,i)=>{
    const mesh=planetMeshes[i],orbit=orbitLines[i];
    mesh.scale.setScalar(Math.max(.0015,sunDiameter*(p.diameter/SUN_KM)/2));
    orbit.scale.setScalar(metersPerAU);
  });
}
function animateBodies(dt){
  if(!paused)elapsed+=dt*timeScale;
  DATA.forEach((p,i)=>{if(!paused)meanAnomalies[i]+=dt*timeScale/(p.period*86400)*Math.PI*2;const m=planetMeshes[i];m.position.copy(orbitalPosition(i,meanAnomalies[i])).multiplyScalar(metersPerAU);m.rotation.y=elapsed/(p.rotation*3600)*Math.PI*2;});
  sun.rotation.y=elapsed/(25.38*86400)*Math.PI*2;
}
function showPlaced(){placed=true;root.visible=true;$('intro').hidden=true;$('toolbar').hidden=false;$('arHint').hidden=true;$('status').textContent='';$('legend').hidden=isAR;$('modeBadge').textContent=isAR?'AR SESSION':'3D PREVIEW';}
function resetPlacement(){if(isAR){placed=false;root.visible=false;$('arHint').hidden=false;$('status').textContent='Aim at a surface, then tap to place.';}else{root.visible=true;placed=true;root.position.set(0,0,0);root.quaternion.identity();}}
function setStatus(message){$('status').textContent=message;}
function fillProfile(p){selected=p;$('profile').hidden=false;$('profileType').textContent=p.id==='sun'?'G2V MAIN-SEQUENCE STAR':(p.diameter>50000?'GIANT PLANET':'TERRESTRIAL PLANET');$('profileName').textContent=p.id==='sun'?'The Sun':p.name;$('profileDescription').textContent=p.description;$('profileDiameter').textContent=`${p.diameter.toLocaleString()} km`;$('profileDistance').textContent=p.distance?`${(p.distance/1e6).toFixed(1)} million km`:'—';$('profilePeriod').textContent=p.period?`${p.period>=365?(p.period/365.25).toFixed(2)+' years':p.period+' days'}`:'—';$('profileRotation').textContent=`${Math.abs(p.rotation)} hours${p.rotation<0?' (retrograde)':''}`;}
function onSelect(event){if(!isAR||!placed){if(isAR&&reticle.visible){root.position.setFromMatrixPosition(reticle.matrix);root.quaternion.setFromRotationMatrix(reticle.matrix);showPlaced();}return;}const source=event.inputSource;const frame=renderer.xr.getFrame();if(!source||!frame)return;const pose=frame.getPose(source.targetRaySpace,renderer.xr.getReferenceSpace());if(!pose)return;raycaster.setFromXRController(controller);const hits=raycaster.intersectObjects([sun,...planetMeshes],false);if(hits.length)fillProfile(hits[0].object.name==='sun'?{id:'sun',diameter:SUN_KM,rotation:609.12,description:'The star at the center of our Solar System. Its gravity holds the planets in orbit.'}:hits[0].object.userData.planet);}
const controller=renderer.xr.getController(0);controller.addEventListener('select',onSelect);scene.add(controller);

renderer.xr.addEventListener('sessionstart',async()=>{
  isAR=true;document.documentElement.classList.add('xr-active');placed=false;root.visible=false;controls.enabled=false;$('intro').hidden=true;$('toolbar').hidden=true;$('arHint').hidden=false;$('modeBadge').textContent='AR SESSION';setStatus('Move your phone slowly to find a horizontal surface.');
  try{const session=renderer.xr.getSession();hitSource=await session.requestHitTestSource({space:await session.requestReferenceSpace('viewer')});session.addEventListener('end',()=>{hitSource=null;isAR=false;document.documentElement.classList.remove('xr-active');reticle.visible=false;controls.enabled=true;root.visible=true;$('intro').hidden=true;$('toolbar').hidden=false;$('arHint').hidden=true;$('legend').hidden=false;$('modeBadge').textContent='3D PREVIEW';setStatus('AR session ended. Continuing in 3D preview.');},{once:true});}catch(error){setStatus(`Surface detection unavailable: ${error.message}`);}
});
renderer.xr.addEventListener('sessionend',()=>{isAR=false;});

async function init(){
  rebuildScale();
  let arSupported=false;
  if(navigator.xr){try{arSupported=await navigator.xr.isSessionSupported('immersive-ar');}catch{}}
  $('startButton').disabled=false;
  if(arSupported){$('startButton').textContent='Start AR experience';$('previewButton').hidden=false;$('supportMessage').textContent='AR needs a supported phone and HTTPS. You can also open the 3D preview.';}
  else{$('startButton').textContent='Open 3D preview';$('supportMessage').textContent='This browser or device does not report immersive AR support. The 3D preview works here; AR needs a supported phone and HTTPS.';}
  $('status').textContent='';
  const openPreview=()=>{isAR=false;controls.enabled=true;showPlaced();camera.position.set(0,3.7,8.5);controls.target.set(0,0,0);};
  $('startButton').addEventListener('click',()=>{if(arSupported){navigator.xr.requestSession('immersive-ar',{requiredFeatures:['hit-test'],optionalFeatures:['dom-overlay'],domOverlay:{root:document.body}}).then(session=>renderer.xr.setSession(session)).catch(error=>setStatus(`Could not start AR: ${error.message}`));}else openPreview();});
  $('previewButton').addEventListener('click',openPreview);
  $('sizeRange').addEventListener('input',e=>{sunDiameter=Number(e.target.value);$('sizeValue').value=`${sunDiameter.toFixed(2)} m`;rebuildScale();});
  $('distanceRange').addEventListener('input',e=>{metersPerAU=Number(e.target.value);$('distanceValue').value=`${metersPerAU.toFixed(2)} m/AU`;rebuildScale();});
  $('speedSelect').addEventListener('change',e=>{timeScale=Number(e.target.value);});
  $('pauseButton').addEventListener('click',()=>{paused=!paused;$('pauseButton').innerHTML=paused?'▶ <span>Resume</span>':'Ⅱ <span>Pause</span>';});
  $('resetButton').addEventListener('click',resetPlacement);$('closeProfile').addEventListener('click',()=>{$('profile').hidden=true;selected=null;});
  $('bodySelect').addEventListener('change',e=>{const id=e.target.value;if(!id)return;if(id==='sun')fillProfile({id:'sun',diameter:SUN_KM,rotation:609.12,description:'The star at the center of our Solar System. Its gravity holds the planets in orbit.'});else fillProfile(DATA.find(p=>p.id===id));});
  $('scene').addEventListener('pointerup',e=>{if(isAR||!placed)return;const rect=renderer.domElement.getBoundingClientRect();pointer.x=((e.clientX-rect.left)/rect.width)*2-1;pointer.y=-((e.clientY-rect.top)/rect.height)*2+1;raycaster.setFromCamera(pointer,camera);const hits=raycaster.intersectObjects([sun,...planetMeshes],false);if(hits.length){const obj=hits[0].object;if(obj.name==='sun')fillProfile({id:'sun',diameter:SUN_KM,rotation:609.12,description:'The star at the center of our Solar System. Its gravity holds the planets in orbit.'});else fillProfile(obj.userData.planet);}});
  $('scene').addEventListener('pointerdown',e=>{if(!isAR&&$('intro').hidden&&e.pointerType==='touch'){const rect=renderer.domElement.getBoundingClientRect();pointer.x=((e.clientX-rect.left)/rect.width)*2-1;pointer.y=-((e.clientY-rect.top)/rect.height)*2+1;raycaster.setFromCamera(pointer,camera);const hits=raycaster.intersectObjects([sun,...planetMeshes],false);if(hits.length)e.preventDefault();}});
}

renderer.setAnimationLoop((_,frame)=>{
  const dt=Math.min(clock.getDelta(),.05);animateBodies(dt);
  if(isAR&&frame&&hitSource){const results=frame.getHitTestResults(hitSource);let horizontalPose=null;for(const result of results){const pose=result.getPose(renderer.xr.getReferenceSpace());if(pose&&Math.abs(pose.transform.matrix[5])>.85){horizontalPose=pose;break;}}if(horizontalPose){reticle.visible=!placed;reticle.matrix.fromArray(horizontalPose.transform.matrix);reticle.matrixWorldNeedsUpdate=true;if(!placed)setStatus(`Surface found · ${renderer.xr.getEnvironmentBlendMode()} blend. Tap to place.`);}else if(!placed){reticle.visible=false;setStatus(`Move your phone slowly to find a horizontal surface · ${renderer.xr.getEnvironmentBlendMode()} blend.`);}}
  controls.update();renderer.render(scene,camera);
});
addEventListener('resize',()=>{camera.aspect=innerWidth/innerHeight;camera.updateProjectionMatrix();renderer.setSize(innerWidth,innerHeight);});
init().catch(error=>setStatus(`Unable to initialize the 3D scene: ${error.message}`));
