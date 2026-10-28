// Ejecutar contra una base de prueba con empleados ficticios 40001 y 49001.
const {chromium}=require('playwright');
const assert=require('node:assert/strict');
const fs=require('node:fs');
const path=require('node:path');
(async()=>{
 const browser=await chromium.launch({headless:true,executablePath:process.env.CHROMIUM_PATH||undefined,args:['--no-sandbox']});
 const page=await browser.newPage();
 const base=process.env.TEST_BASE_URL||'http://127.0.0.1:5082';
 const password=process.env.TEST_DEMO_PASSWORD||fs.readFileSync(path.join(__dirname,'../../.local/demo-password'),'utf8').trim();
 let posts=0,phase="start";
 page.on('request',req=>{if(req.method()==='POST'&&req.url().includes('handler=Quick'))posts++;});
 const waitText=async text=>{await page.waitForFunction(text=>document.getElementById('registration-message').textContent.includes(text),text);};
 const lookupCount=async number=>(await (await page.request.get(`${base}/Register?handler=Lookup&number=${number}`)).json()).todayCount;
 const consult=async number=>{await page.locator('#Number').fill(number);await page.keyboard.press('Enter');await page.waitForFunction(()=>!document.getElementById('register-button').disabled);};
 try{
  const anon=await page.request.get(base+'/Register?handler=Lookup&number=40001');
  assert(!(anon.headers()['content-type']||'').includes('application/json'));
  await page.goto(base+'/Login');await page.locator('#Username').fill('demo');await page.locator('#Password').fill(password);
  await Promise.all([page.waitForURL(base+'/'),page.getByRole('button',{name:'Iniciar sesión'}).click()]);
  phase="open-register";
  await page.goto(base+'/Register');
  assert(await page.locator('#employee-card').isHidden());
  const before=await lookupCount('40001');
  await page.locator('#Number').fill('40001');
  phase="first-lookup";
  await page.keyboard.down('Enter');
  await page.waitForFunction(()=>!document.getElementById('register-button').disabled);
  await page.keyboard.down('Enter'); // Tecla mantenida: evento repeat.
  await page.waitForTimeout(80);
  assert.equal(posts,0);
  await page.keyboard.up('Enter');
  phase="photo";
  await page.waitForFunction(()=>document.getElementById('employee-photo').naturalWidth>0);
  await page.keyboard.press('Enter');await waitText('Servicio registrado.');
  assert.equal(await page.locator('#Number').inputValue(),'');
  assert.equal(await page.evaluate(()=>document.activeElement.id),'Number');
  assert.equal(await page.locator('#employee-stage').textContent(),'ÚLTIMO SERVICIO REGISTRADO');
  assert.equal(await lookupCount('40001'),before+1);
  phase="second-lookup";
  await consult('40001');
  const countPosts=posts;
  await page.locator('#registration-form').evaluate(form=>{form.requestSubmit();form.requestSubmit();});
  await waitText('Servicio registrado.');
  assert.equal(posts,countPosts+1);assert.equal(await lookupCount('40001'),before+2);
  phase="missing-photo";
  await consult('49001');
  await page.waitForFunction(()=>document.getElementById('photo-label').textContent==='Sin fotografía disponible');
  await page.locator('#Number').fill('40001');
  assert(await page.locator('#register-button').isDisabled());
  assert(await page.locator('#employee-card').isHidden());
  await page.keyboard.press('Enter');await page.waitForFunction(()=>!document.getElementById('register-button').disabled);
  phase="lost-response";
  // Simula pérdida de respuesta después de que el servidor guardó el consumo.
  const pattern='**/Register?handler=Quick';
  await page.route(pattern,async route=>{await route.fetch();await route.abort('failed');});
  await page.keyboard.press('Enter');await waitText('Puedes reintentar');
  assert(await page.locator('#register-button').isEnabled());
  await page.unroute(pattern);
  await page.keyboard.press('Enter');await waitText('Esta solicitud ya fue registrada;');
  assert.equal(await lookupCount('40001'),before+3);
  await page.locator('#Number').fill('49001');await page.keyboard.press('Enter');await page.waitForFunction(()=>!document.getElementById('register-button').disabled);
  phase="escape";
  await page.keyboard.press('Escape');assert.equal(await page.locator('#Number').inputValue(),'');assert(await page.locator('#employee-card').isHidden());
  await page.screenshot({path:process.env.TEST_SCREENSHOT||'/tmp/comedores-quick.png',fullPage:true});
  console.log('PASS: Enter consulta/confirma; tecla mantenida y doble envío no duplican; campo listo; fotos configuradas; cambio de empleado; Escape; reintento tras respuesta perdida sin duplicado.');
 }catch(error){
  console.error('Fase:',phase,'URL:',page.url());
  console.error('Mensaje:',await page.locator('#registration-message').textContent().catch(()=>''));
  await page.screenshot({path:'/tmp/quick-error.png',fullPage:true});throw error;
 }finally{await browser.close();}
})().catch(error=>{console.error(error.message);process.exitCode=1;});
