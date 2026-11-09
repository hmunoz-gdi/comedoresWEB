// Ejecutar exclusivamente contra la base aislada comedores_pickup_test.
const {chromium}=require('playwright'),assert=require('node:assert/strict'),fs=require('node:fs'),{execFileSync}=require('node:child_process');
(async()=>{
 const base=process.env.TEST_BASE_URL||'http://127.0.0.1:5104';const target=new URL(base);assert(['127.0.0.1','localhost'].includes(target.hostname)&&target.port==='5104','Usa únicamente el servidor local aislado en 5104');
 const sql=s=>execFileSync('docker',['--host=unix:///var/run/docker.sock','exec','-i','comedores-postgres','psql','-U','comedores','-d','comedores_pickup_test','-At','-v','ON_ERROR_STOP=1'],{input:s,encoding:'utf8'}).trim();
 sql(`DELETE FROM "Consumptions";DELETE FROM "PickupBatches";INSERT INTO "Employees" ("Number","Name","Area","Position","WorkSchedule","SpecialShift","Active") VALUES ('40002','Compañero ficticio dos','Pruebas','Pruebas','Pruebas',false,true),('40003','Compañero ficticio tres','Pruebas','Pruebas','Pruebas',false,true) ON CONFLICT ("Number") DO NOTHING;`);
 const browser=await chromium.launch({executablePath:process.env.CHROMIUM_PATH||'/usr/bin/chromium',args:['--no-sandbox']});
 const p=await browser.newPage({viewport:{width:1366,height:768}});const errors=[];p.on('pageerror',e=>errors.push(e.message));
 async function add(id,value,wait){await p.locator(id).fill(value);await p.locator(id).press('Enter');await wait();}
 try{
  await p.goto(base+'/Login');await p.locator('#Username').fill('demo');await p.locator('#Password').fill(fs.readFileSync('.local/demo-password','utf8').trim());await Promise.all([p.waitForURL(base+'/'),p.getByRole('button',{name:'Iniciar sesión'}).click()]);
  await p.goto(base+'/Register');await p.getByRole('link',{name:'Recoger para compañeros'}).click();
  await p.locator('#pickup-room').selectOption('1');await p.locator('#pickup-type').selectOption('COM');
  await add('#collector-number','40001',()=>p.locator('#member-number').waitFor({state:'visible'}));await p.waitForFunction(()=>!document.querySelector('#member-number').disabled);
  for(const [i,number]of ['40002','40003','49001'].entries())await add('#member-number',number,()=>p.waitForFunction(n=>document.querySelectorAll('#pickup-list li').length===n,i+2));
  assert.equal(sql('SELECT count(*) FROM "Consumptions"'),'0');await p.screenshot({path:'/tmp/pickup-ready.png',fullPage:true});
  await p.locator('#member-number').press('Control+Enter');await p.locator('#pickup-dialog[open]').waitFor();assert((await p.locator('#pickup-dialog-title').innerText()).includes('Servicio aplicado'));assert.equal(sql('SELECT count(*) FROM "Consumptions" WHERE "CollectedByNumber"=\'40001\''),'4');assert.equal(sql('SELECT count(DISTINCT "PickupBatchId") FROM "Consumptions"'),'1');
  await p.locator('#pickup-accept').click();await p.waitForFunction(()=>document.activeElement.id==='collector-number');assert.equal(await p.locator('#pickup-room').inputValue(),'1');assert.equal(await p.locator('#pickup-type').inputValue(),'COM');assert.equal(await p.locator('#pickup-list li').count(),0);
  await add('#collector-number','40001',()=>p.waitForFunction(()=>!document.querySelector('#member-number').disabled));await p.locator('#include-own').uncheck();await add('#member-number','40002',()=>p.waitForFunction(()=>document.querySelectorAll('#pickup-list li').length===1));
  await p.locator('#pickup-save').click();await p.locator('#pickup-dialog[open]').waitFor();assert((await p.locator('#pickup-dialog-title').innerText()).includes('repetidos'));await p.locator('#pickup-cancel').click();await p.waitForFunction(()=>!document.querySelector('#pickup-save').disabled);assert.equal(sql('SELECT count(*) FROM "Consumptions"'),'4');
  await p.locator('#pickup-save').click();await p.locator('#pickup-dialog[open]').waitFor();await p.locator('#pickup-accept').click();await p.waitForFunction(()=>document.querySelector('#pickup-dialog-title').textContent==='Servicio aplicado');assert.equal(sql('SELECT count(*) FROM "Consumptions"'),'5');assert.equal(sql('SELECT count(*) FROM "Consumptions" c JOIN "Employees" e ON e."Id"=c."EmployeeId" WHERE e."Number"=\'40001\''),'1');await p.locator('#pickup-accept').click();
  // Reenvíos concurrentes del mismo folio conservan exactamente un grupo.
  const payload={requestId:require('node:crypto').randomUUID(),collectorNumber:'40001',numbers:['40003'],roomId:1,serviceType:'CEN'};
  const token=await p.locator('#pickup-token input').first().inputValue();const post=data=>p.request.post(base+'/Pickup?handler=Save',{headers:{RequestVerificationToken:token},data}).then(r=>r.json());
  const replay=await Promise.all([post(payload),post(payload)]);assert(replay.every(r=>r.ok));assert.equal(sql(`SELECT count(*) FROM "Consumptions" WHERE "PickupBatchId"='${payload.requestId}'`),'1');
  assert(!(await post({...payload,numbers:['40002']})).ok);assert(!(await post({...payload,requestId:require('node:crypto').randomUUID(),numbers:['40003','99999']})).ok);
  const csv=await p.request.get(base+'/Reports?handler=Csv&format=detalle&From=2026-11-06&To=2026-11-06');assert((await csv.text()).includes(payload.requestId));
  await p.setViewportSize({width:1024,height:650});await p.screenshot({path:'/tmp/pickup-compact.png',fullPage:true});assert(await p.evaluate(()=>document.documentElement.scrollWidth<=innerWidth));assert.deepEqual(errors,[]);
  console.log('PASS: recogida propia y encargos, sólo encargos, Enter/Ctrl+Enter, cancelación y confirmación conjunta, limpieza y combos conservados, reintentos concurrentes, rechazo atómico, CSV y vista compacta sin errores JS.');
 }finally{await browser.close();}
})().catch(e=>{console.error(e);process.exitCode=1});
