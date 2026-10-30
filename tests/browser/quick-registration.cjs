// Usar una base de prueba con 40001/49001 y hora simulada dentro de desayuno.
const {chromium}=require('playwright');
const assert=require('node:assert/strict');
const fs=require('node:fs'),path=require('node:path');
(async()=>{
 const browser=await chromium.launch({headless:true,executablePath:process.env.CHROMIUM_PATH||undefined,args:['--no-sandbox']});
 const context=await browser.newContext();
 const page=await context.newPage(),base=process.env.TEST_BASE_URL||'http://127.0.0.1:5082';
 const password=process.env.TEST_DEMO_PASSWORD||fs.readFileSync(path.join(__dirname,'../../.local/demo-password'),'utf8').trim();
 let phase='login';
 const message=page.locator('#registration-message');
 const count=async()=> (await (await page.request.get(base+'/Register?handler=Lookup&number=40001')).json()).todayCount;
 const consult=async(target,number)=>{
  await target.locator('#Number').fill(number);await target.keyboard.press('Enter');
  await target.waitForFunction(()=>!document.getElementById('register-button').disabled);
 };
 const waitMessage=async(text,target=page)=>target.waitForFunction(text=>document.getElementById('registration-message').textContent.includes(text),text);
 const finish=async(target=page)=>{
  await target.waitForFunction(()=>document.getElementById('repeat-dialog').open||document.getElementById('registration-message').textContent.includes('Servicio registrado.'));
  if(await target.locator('#repeat-dialog').isVisible())await target.locator('#continue-repeat').click();
  await waitMessage('Servicio registrado.',target);
 };
 try {
  const anon=await page.request.get(base+'/Register?handler=Lookup&number=40001');assert(!(anon.headers()['content-type']||'').includes('application/json'));
  await page.goto(base+'/Login');await page.locator('#Username').fill('demo');await page.locator('#Password').fill(password);
  await Promise.all([page.waitForURL(base+'/'),page.getByRole('button',{name:'Iniciar sesión'}).click()]);
  await page.goto(base+'/Register');assert(await page.locator('#employee-card').isHidden());
  assert((await page.locator('aside').allTextContents()).some(t=>t.includes('HORA SIMULADA')));
  const before=await count();
  phase='tecla mantenida';
  await page.locator('#Number').fill('40001');await page.keyboard.down('Enter');
  await page.waitForFunction(()=>!document.getElementById('register-button').disabled);
  await page.keyboard.down('Enter');await page.waitForTimeout(80);
  assert.equal(await count(),before);await page.keyboard.up('Enter');
  await page.waitForFunction(()=>document.getElementById('employee-photo').naturalWidth>0);
  await page.keyboard.press('Enter');await finish();
  assert.equal(await page.locator('#Number').inputValue(),'');assert.equal(await page.evaluate(()=>document.activeElement.id),'Number');
  assert((await message.textContent()).includes('Desayuno')&&(await message.textContent()).includes(process.env.TEST_EXPECTED_TIME||'08:30:00'));
  assert.equal(await count(),before+1);
  phase='cancelar repetición';
  await consult(page,'40001');await page.keyboard.press('Enter');await page.locator('#repeat-dialog').waitFor({state:'visible'});
  assert((await page.locator('#repeat-message').textContent()).includes(String(before+1)));
  await page.locator('#cancel-repeat').click();await waitMessage('Registro cancelado');assert.equal(await count(),before+1);
  phase='continuar y doble envío';
  await page.locator('#registration-form').evaluate(form=>{form.requestSubmit();form.requestSubmit();});await finish();assert.equal(await count(),before+2);
  phase='otro operador aumenta el conteo';
  const other=await page.context().newPage();await other.goto(base+'/Register');
  await consult(page,'40001');await page.keyboard.press('Enter');await page.locator('#repeat-dialog').waitFor({state:'visible'});
  await consult(other,'40001');await other.keyboard.press('Enter');await finish(other);assert.equal(await count(),before+3);
  await page.locator('#continue-repeat').click();
  await page.waitForFunction(n=>document.getElementById('repeat-dialog').open&&document.getElementById('repeat-message').textContent.includes(String(n)),before+3);
  await page.locator('#cancel-repeat').click();await waitMessage('Registro cancelado');assert.equal(await count(),before+3);await other.close();
  phase='foto inexistente y cambio de empleado';
  await consult(page,'49001');await page.waitForFunction(()=>document.getElementById('photo-label').textContent==='Sin fotografía disponible');
  await page.locator('#Number').fill('40001');assert(await page.locator('#register-button').isDisabled());assert(await page.locator('#employee-card').isHidden());
  await page.keyboard.press('Enter');await page.waitForFunction(()=>!document.getElementById('register-button').disabled);
  phase='respuesta perdida';
  const pattern='**/Register?handler=Quick';
  await page.route(pattern,async route=>{
   const response=await route.fetch();const data=await response.json();
   if(data.ok)await route.abort('failed');else await route.fulfill({response});
  });
  await page.keyboard.press('Enter');await page.locator('#repeat-dialog').waitFor({state:'visible'});await page.locator('#continue-repeat').click();
  await waitMessage('Puedes reintentar');await page.unroute(pattern);
  await page.keyboard.press('Enter');await waitMessage('Esta solicitud ya fue registrada;');assert.equal(await count(),before+4);
  phase='Escape';await consult(page,'49001');await page.keyboard.press('Escape');assert.equal(await page.locator('#Number').inputValue(),'');assert(await page.locator('#employee-card').isHidden());
  if(process.env.TEST_REPORT_FIXTURE==='1') {
   phase='reporte y PDF';
   await page.goto(base+'/Reports?From=2026-10-20&To=2026-10-20&RoomId=1&Category=Operativo');
   const row=page.locator('.table-wrap').first().locator('tbody tr');assert.equal(await row.count(),1);
   const cells=await row.locator('td').allTextContents();
   assert(cells[5].includes('08:05:11'));assert(cells[6].includes('14:10:12')&&cells[6].includes('14:20:13'));assert(cells[7].includes('20:35:14'));
   const pdf='/tmp/comedores-horarios.pdf';await page.pdf({path:pdf,preferCSSPageSize:true,printBackground:true});
   const text=require('node:child_process').execFileSync('pdftotext',[pdf,'-'],{encoding:'utf8'});
   for(const value of ['Desayuno','Comida','Cena','08:05:11','14:10:12','14:20:13','20:35:14'])assert(text.includes(value),value+' ausente en PDF');
  }
  console.log('PASS: registro con hora/tipo, aviso de repetición, cancelar/continuar, conteo actualizado por otro operador, Enter mantenido/doble envío, reintento sin duplicar y Escape.'+(process.env.TEST_REPORT_FIXTURE==='1'?' Reporte y PDF conservan cada hora en su segmento.':''));
 }catch(error){console.error('Fase:',phase,error.message);await page.screenshot({path:'/tmp/quick-error.png',fullPage:true});throw error;}
 finally{await browser.close();}
})().catch(()=>{process.exitCode=1;});
