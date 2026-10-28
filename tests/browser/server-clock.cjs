const {chromium}=require('playwright');
const assert=require('node:assert/strict');
const fs=require('node:fs'),path=require('node:path');
(async()=>{
 const browser=await chromium.launch({headless:true,executablePath:process.env.CHROMIUM_PATH||undefined,args:['--no-sandbox']});
 const context=await browser.newContext({timezoneId:'Pacific/Auckland'});
 await context.addInitScript(()=>{Date.now=()=>0;}); // El reloj no debe depender de la fecha local del cliente.
 const page=await context.newPage(),base=process.env.TEST_BASE_URL||'http://127.0.0.1:5082';
 const password=process.env.TEST_DEMO_PASSWORD||fs.readFileSync(path.join(__dirname,'../../.local/demo-password'),'utf8').trim();
 const status=page.locator('#clock-status'),clock=page.locator('#server-clock');
 const waitStatus=text=>page.waitForFunction(text=>document.getElementById('clock-status').textContent.includes(text),text);
 const refocus=()=>page.evaluate(()=>window.dispatchEvent(new Event('focus')));
 async function login(target,url){
  await target.goto(url+'/Login');await target.locator('#Username').fill('demo');await target.locator('#Password').fill(password);
  await Promise.all([target.waitForURL(url+'/'),target.getByRole('button',{name:'Iniciar sesión'}).click()]);
  await target.goto(url+'/Register');
 }
 try {
  const anon=await page.request.get(base+'/Register?handler=Clock');assert(!(anon.headers()['content-type']||'').includes('application/json'));
  await login(page,base);await waitStatus('EN LÍNEA');
  const server=await (await page.request.get(base+'/Register?handler=Clock')).json();
  assert.equal(server.simulated,false);assert(Math.abs(server.utcMilliseconds-Date.now())<5000);
  const expected=new Intl.DateTimeFormat('es-MX',{timeZone:'America/Mexico_City',hour:'2-digit',minute:'2-digit',second:'2-digit',hourCycle:'h23'}).format(new Date(server.utcMilliseconds));
  const seconds=s=>s.split(':').reduce((total,value)=>total*60+Number(value),0);
  const delta=Math.abs(seconds(await clock.textContent())-seconds(expected));assert(Math.min(delta,86400-delta)<3);
  const first=await clock.textContent();await page.waitForTimeout(2200);assert.notEqual(await clock.textContent(),first);
  await page.locator('#clock-motion').click();assert.equal(await page.locator('#clock-motion').getAttribute('aria-pressed'),'true');
  assert.equal(await page.locator('.server-marquee span').evaluate(el=>getComputedStyle(el).animationPlayState),'paused');
  const paused=await clock.textContent();await page.waitForTimeout(1200);assert.notEqual(await clock.textContent(),paused);
  await page.emulateMedia({reducedMotion:'reduce'});assert.equal(await page.locator('.server-marquee span').evaluate(el=>getComputedStyle(el).animationName),'none');
  const pattern='**/Register?handler=Clock';
  let fixture={...server,utcMilliseconds:Date.parse('2026-10-29T02:29:58.500Z')};
  await page.route(pattern,route=>route.fulfill({json:fixture}));await refocus();
  await page.waitForFunction(()=>document.getElementById('server-clock').textContent.startsWith('20:29:'));
  await page.waitForFunction(()=>document.getElementById('current-service').textContent==='Cena',{},{timeout:6000});
  fixture={...server,utcMilliseconds:Date.parse('2026-10-29T05:59:58.500Z')};await refocus();
  await page.waitForFunction(()=>document.getElementById('server-clock').textContent.startsWith('23:59:'));
  await page.waitForFunction(()=>document.getElementById('server-date').textContent.includes('29 de octubre'),{},{timeout:6000});
  await page.unroute(pattern);await page.route(pattern,route=>route.abort('failed'));await refocus();await waitStatus('Sin sincronización');
  const disconnected=await clock.textContent();await page.waitForTimeout(1200);assert.notEqual(await clock.textContent(),disconnected);
  await page.unroute(pattern);await refocus();await waitStatus('EN LÍNEA');
  await page.screenshot({path:'/tmp/comedores-reloj.png',fullPage:true});
  if(process.env.TEST_SIM_BASE_URL){
   const simContext=await browser.newContext(),sim=await simContext.newPage();await login(sim,process.env.TEST_SIM_BASE_URL);
   await sim.waitForFunction(()=>document.getElementById('clock-status').textContent.includes('Hora simulada fija'));
   const fixed=await sim.locator('#server-clock').textContent();await sim.waitForTimeout(1500);assert.equal(await sim.locator('#server-clock').textContent(),fixed);
   await simContext.close();
  }
  console.log('PASS: fecha/hora avanza, fuente del servidor con cliente en otra zona/reloj alterado, pausa y movimiento reducido, cambio a Cena, medianoche, desconexión/recuperación y hora simulada claramente identificada.');
 }catch(error){console.error(error.message);await page.screenshot({path:'/tmp/clock-error.png',fullPage:true});throw error;}
 finally{await browser.close();}
})().catch(()=>{process.exitCode=1;});
