// Usar una base aislada con empleados de demostración 40001/49001.
const {chromium}=require('playwright'),assert=require('node:assert/strict'),fs=require('node:fs'),path=require('node:path');
(async()=>{
 const base=process.env.TEST_BASE_URL||'http://127.0.0.1:5090',password=process.env.TEST_DEMO_PASSWORD||fs.readFileSync(path.join(__dirname,'../../.local/demo-password'),'utf8').trim();
 const browser=await chromium.launch({executablePath:process.env.CHROMIUM_PATH||undefined,args:['--no-sandbox']});const p=await browser.newPage();
 async function recovered(){await p.waitForFunction(()=>document.activeElement.id==='Number'&&!document.getElementById('Number').readOnly);assert(await p.locator('#register-button').isDisabled());assert(await p.locator('#employee-card').isHidden());assert.deepEqual(await p.locator('#Number').evaluate(e=>[e.selectionStart,e.selectionEnd]),[0,5]);assert(await p.locator('#registration-message').isHidden());}
 try{
  await p.goto(base+'/Login');await p.locator('#Username').fill('demo');await p.locator('#Password').fill(password);await Promise.all([p.waitForURL(base+'/'),p.getByRole('button',{name:'Iniciar sesión'}).click()]);await p.goto(base+'/Register');
  await p.locator('#Number').fill('40001');await p.keyboard.press('Enter');await p.waitForFunction(()=>!document.getElementById('employee-card').hidden);
  await p.locator('#Number').fill('99999');await p.keyboard.press('Enter');await p.locator('#not-found-dialog').waitFor({state:'visible'});assert.equal(await p.locator('#not-found-title').textContent(),'Empleado no encontrado');assert(await p.locator('#success-dialog').isHidden());assert(await p.locator('#hours-dialog').isHidden());assert.equal(await p.locator('#registration-number').inputValue(),'');await p.keyboard.press('Enter');await recovered();
  await p.keyboard.type('49001');assert.equal(await p.locator('#Number').inputValue(),'49001');await p.keyboard.press('Enter');await p.waitForFunction(()=>!document.getElementById('employee-card').hidden);
  await p.locator('#Number').fill('99999');await p.locator('#lookup-button').click();await p.locator('#not-found-dialog').waitFor({state:'visible'});await p.locator('#not-found-accept').click();await recovered();
  await p.goto(base+'/Register?Number=99999');await p.locator('#not-found-dialog').waitFor({state:'visible'});await p.keyboard.press('Escape');await recovered();
  const token=await p.locator('#registration-form input[name="__RequestVerificationToken"]').inputValue();const result=await p.request.post(base+'/Register?handler=Quick',{form:{Number:'99999',RequestId:require('node:crypto').randomUUID(),SelectedServiceType:'DYN',CoordinatorRoomId:'1',__RequestVerificationToken:token}});assert.equal((await result.json()).ok,false);
  console.log('PASS: empleado inexistente por Enter, clic y URL directa; diálogo sin confundir fuera de horario, ficha oculta, registro deshabilitado, retorno con Enter/Aceptar/Escape y número seleccionado para corregir.');
 }finally{await browser.close();}
})().catch(e=>{console.error(e);process.exitCode=1});
