const {chromium}=require('playwright');
const assert=require('node:assert/strict'),fs=require('node:fs'),path=require('node:path');
(async()=>{
 const browser=await chromium.launch({headless:true,executablePath:process.env.CHROMIUM_PATH||undefined,args:['--no-sandbox']});
 const context=await browser.newContext(),page=await context.newPage();
 const inside=process.env.TEST_BASE_URL||'http://127.0.0.1:5082',outside=process.env.TEST_OUTSIDE_URL||'http://127.0.0.1:5083';
 const password=process.env.TEST_DEMO_PASSWORD||fs.readFileSync(path.join(__dirname,'../../.local/demo-password'),'utf8').trim();
 async function login(url,target=page){await target.goto(url+'/Login');await target.locator('#Username').fill('demo');await target.locator('#Password').fill(password);await Promise.all([target.waitForURL(url+'/'),target.getByRole('button',{name:'Iniciar sesión'}).click()]);}
 try {
  await login(inside);await page.goto(inside+'/Register');await page.locator('#Number').fill('40001');await page.keyboard.press('Enter');
  await page.waitForFunction(()=>!document.getElementById('register-button').disabled);
  for(const [width,height] of [[1920,1080],[1366,768],[1366,650],[1280,720],[1024,768],[1024,650]]) {
   await page.setViewportSize({width,height});await page.waitForTimeout(80);
   const metrics=await page.evaluate(()=>({width:innerWidth,height:innerHeight,scrollWidth:document.documentElement.scrollWidth,scrollHeight:document.documentElement.scrollHeight,buttonBottom:document.getElementById('register-button').getBoundingClientRect().bottom}));
   console.log(`${width}x${height}`,JSON.stringify(metrics));
   assert(metrics.scrollWidth<=width+1,'Desbordamiento horizontal');assert(metrics.scrollHeight<=height+1,'Pantalla necesita desplazamiento vertical');assert(metrics.buttonBottom<=height,'Botón fuera de vista');
  }
  await page.screenshot({path:'/tmp/comedores-horizontal.png',fullPage:true});
  await page.setViewportSize({width:390,height:844});assert(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth+1),'Móvil con desbordamiento horizontal');
  await page.setViewportSize({width:1366,height:650});
  const before=(await(await page.request.get(inside+'/Register?handler=Lookup&number=40001')).json()).todayCount;
  // Emula que cerró el horario entre consulta y confirmación.
  const outsideContext=await browser.newContext(),away=await outsideContext.newPage();
  await login(outside,away);await away.goto(outside+'/Register');
  const outsideToken=await away.locator('#registration-form input[name="__RequestVerificationToken"]').inputValue();
  const pattern='**/Register?handler=Quick';await page.route(pattern,async route=>{
   const requestId=await page.locator('#RequestId').inputValue();
   const response=await away.request.post(outside+'/Register?handler=Quick',{form:{Number:'40001',RequestId:requestId,__RequestVerificationToken:outsideToken}});
   await route.fulfill({response});
  });
  await page.locator('#Number').fill('40001');await page.keyboard.press('Enter');await page.waitForFunction(()=>!document.getElementById('register-button').disabled);
  await page.keyboard.press('Enter');await page.locator('#hours-dialog').waitFor({state:'visible'});
  assert((await page.locator('#hours-message').textContent()).includes('Fuera del horario'));
  await page.locator('#hours-accept').click();await page.waitForFunction(()=>document.activeElement.id==='Number'&&!document.getElementById('Number').readOnly);
  assert.equal((await(await page.request.get(inside+'/Register?handler=Lookup&number=40001')).json()).todayCount,before);await page.unroute(pattern);
  await away.goto(outside+'/Register');assert(await away.locator('#hours-dialog').isHidden());
  await away.locator('#Number').fill('40001');await away.keyboard.press('Enter');await away.locator('#hours-dialog').waitFor({state:'visible'});
  assert(await away.locator('#registration-message').isHidden());await away.keyboard.press('Enter');
  await away.waitForFunction(()=>document.activeElement.id==='Number'&&!document.getElementById('Number').readOnly);assert(await away.locator('#register-button').isDisabled());
  await away.goto(outside+'/Register?Number=40001');await away.locator('#hours-dialog').waitFor({state:'visible'});await away.keyboard.press('Escape');await away.waitForFunction(()=>document.activeElement.id==='Number');
  await outsideContext.close();
  console.log('PASS: vista horizontal completa, móvil sin desbordamiento, fuera de horario por consulta/confirmación/URL directa, cierre con Enter/Escape y ningún consumo guardado.');
 }catch(error){console.error(error.message);await page.screenshot({path:'/tmp/layout-error.png',fullPage:true});throw error;}
 finally{await browser.close();}
})().catch(()=>{process.exitCode=1;});
