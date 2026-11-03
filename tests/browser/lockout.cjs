// Utilizar una base aislada: esta suite crea y bloquea cuentas ficticias.
const {chromium}=require('playwright'),assert=require('node:assert/strict'),fs=require('node:fs'),path=require('node:path'),crypto=require('node:crypto');
(async()=>{
 const base=process.env.TEST_BASE_URL||'http://127.0.0.1:5087',adminPassword=process.env.TEST_DEMO_PASSWORD||fs.readFileSync(path.join(__dirname,'../../.local/demo-password'),'utf8').trim();
 const password='Aa1!'+crypto.randomBytes(16).toString('hex'),name='lock'+crypto.randomBytes(4).toString('hex');
 const browser=await chromium.launch({headless:true,executablePath:process.env.CHROMIUM_PATH||undefined,args:['--no-sandbox']});let phase='inicio';
 async function page(){return(await browser.newContext()).newPage();}
 async function attempt(p,user,pass){await p.goto(base+'/Login');await p.locator('#Username').fill(user);await p.locator('#Password').fill(pass);await Promise.all([p.waitForNavigation(),p.getByRole('button',{name:'Iniciar sesión'}).click()]);}
 try{
  const admin=await page();await attempt(admin,'demo',adminPassword);await admin.waitForURL(base+'/');await admin.goto(base+'/Users');
  await admin.locator('#Username').fill(name);await admin.locator('#Password').fill(password);await admin.locator('#Profile').selectOption('Catalogo');await Promise.all([admin.waitForNavigation(),admin.locator('button[value=create]').click()]);
  const p=await page();await p.goto(base+'/Login');assert.equal(await p.locator('.login .login-logos img').count(),2);assert.equal(await p.locator('header').count(),0);for(const logo of await p.locator('.login-logos img').all())assert(await logo.evaluate(i=>i.complete&&i.naturalWidth>0));
  phase='contador reiniciado al acertar';for(let i=0;i<2;i++){await attempt(p,name,'Wrong-123456789!');assert(!(await p.locator('.notice').textContent()).includes('Cuenta bloqueada'));}await attempt(p,name,password);await p.waitForURL(base+'/');
  // Conserva una sesión abierta para comprobar su revocación al bloquear la cuenta.
  const active=await page();await attempt(active,name,password);await active.waitForURL(base+'/');
  phase='tercer intento bloquea';for(let i=0;i<3;i++){await attempt(p,name,'Wrong-123456789!');const text=await p.locator('.notice').textContent();assert.equal(text.includes('Cuenta bloqueada'),i===2);assert(text.includes('2442'));}
  await attempt(p,name,password);assert((await p.locator('.notice').textContent()).includes('Cuenta bloqueada'));const expired=await active.request.get(base+'/Employees');assert(expired.url().includes('/Login'));
  phase='desbloqueo protegido';await admin.goto(base+'/Users');let row=admin.locator('tr').filter({has:admin.locator('td').filter({hasText:new RegExp('^'+name+'$')})}).first();assert((await row.textContent()).includes('Bloqueada'));
  const token=await row.locator('input[name="__RequestVerificationToken"]').inputValue();const empty=await admin.request.post(base+'/Users?handler=Unlock',{form:{accountName:name,reason:'',__RequestVerificationToken:token}});assert.equal(empty.status(),200);await attempt(p,name,password);assert((await p.locator('.notice').textContent()).includes('Cuenta bloqueada'));
  await row.locator('input[name=reason]').fill('Identidad verificada, folio PRUEBA-2442');await Promise.all([admin.waitForNavigation(),row.getByRole('button',{name:'Desbloquear cuenta'}).click()]);assert((await admin.locator('.notice').textContent()).includes('Cuenta desbloqueada'));assert((await admin.locator('main').textContent()).includes('PRUEBA-2442'));
  phase='acceso recuperado';await attempt(p,name,password);await p.waitForURL(base+'/');const denied=await p.request.post(base+'/Users?handler=Unlock',{form:{accountName:'demo',reason:'Manipulación',__RequestVerificationToken:await(await p.request.get(base+'/Employees')).text().then(html=>html.match(/name="__RequestVerificationToken" type="hidden" value="([^"]+)"/)[1])}});assert.equal(denied.status(),403);
  phase='contador vuelve a cero';for(let i=0;i<2;i++){await attempt(p,name,'Wrong-123456789!');assert(!(await p.locator('.notice').textContent()).includes('Cuenta bloqueada'));}
  // El tercer fallo se envía en paralelo con otro intento: debe conservar el bloqueo.
  const other=await page();await Promise.all([attempt(p,name,'Wrong-123456789!'),attempt(other,name,'Wrong-123456789!')]);assert((await p.locator('.notice').textContent()).includes('Cuenta bloqueada'));await attempt(other,name,password);assert((await other.locator('.notice').textContent()).includes('Cuenta bloqueada'));
  phase='visualización';await p.goto(base+'/Login');await p.setViewportSize({width:390,height:844});assert(await p.evaluate(()=>document.documentElement.scrollWidth<=innerWidth+1));await p.setViewportSize({width:1366,height:768});await p.screenshot({path:'/tmp/login-logos.png',fullPage:true});
  console.log('PASS: logos interiores, tres intentos consecutivos, acierto reinicia contador, bloqueo impide acceso correcto y revoca sesiones, desbloqueo sólo administrador con motivo, bitácora persistente y fallos simultáneos.');
 }catch(e){console.error(phase,e);throw e;}finally{await browser.close();}
})().catch(()=>process.exitCode=1);
