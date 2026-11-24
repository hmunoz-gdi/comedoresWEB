// Crea y elimina su propia base temporal. Nunca usa comedores ni comedores_pruebas.
const {chromium}=require('playwright'),fs=require('node:fs'),path=require('node:path'),assert=require('node:assert/strict'),{execFileSync,spawn}=require('node:child_process'),{randomBytes}=require('node:crypto');
(async()=>{
 const database='comedores_rename_'+randomBytes(6).toString('hex'),base='http://127.0.0.1:5108',password=fs.readFileSync('.local/demo-password','utf8').trim(),web=path.resolve('src/Comedores.Web');
 const docker=['--host=unix:///var/run/docker.sock','exec','-i','comedores-postgres'];const run=(exe,args=[],input)=>execFileSync('docker',[...docker,exe,...args],{input,encoding:'utf8',stdio:['pipe','pipe','pipe']});
 const sql=q=>run('psql',['-U','comedores','-d',database,'-At','-v','ON_ERROR_STOP=1','-c',q]).trim();
 const migrate=(expected=database)=>run('psql',['-U','comedores','-d',database,'-v','base_objetivo='+expected,'-f','-'],fs.readFileSync('scripts/sql/renombrar-demo-sagdi.sql','utf8'));
 let server,browser;
 async function stop(){if(server&&server.exitCode===null){await new Promise(resolve=>{server.once('exit',resolve);server.kill('SIGTERM');});}server=null;}
 async function start(){server=spawn(process.env.TEST_DOTNET||'dotnet',[path.join(web,'bin/Debug/net10.0/Comedores.Web.dll'),'--urls',base],{cwd:web,stdio:'ignore',env:{...process.env,ASPNETCORE_ENVIRONMENT:'Development',ConnectionStrings__Comedores:`Host=127.0.0.1;Username=comedores;Database=${database}`,Demo__Password:password,Demo__TerminalIp:'172.16.70.68',EmployeePhotos__Directory:'/tmp/sicom-rename-photos'}});for(let i=0;i<100;i++){if(server.exitCode!==null)throw new Error('Falló el arranque de la instancia temporal.');try{if((await fetch(base+'/Login')).status===200)return;}catch{}await new Promise(r=>setTimeout(r,100));}throw new Error('La instancia temporal no respondió.');}
 async function login(user,pw=password){const p=await browser.newPage();await p.goto(base+'/Login');await p.locator('#Username').fill(user);await p.locator('#Password').fill(pw);await Promise.all([p.waitForNavigation(),p.getByRole('button',{name:'Iniciar sesión'}).click()]);return p;}
 function snapshot(){return JSON.parse(sql(`SELECT json_build_object('account',to_jsonb(u),'roles',(SELECT json_agg(x ORDER BY "RoleId") FROM "AspNetUserRoles" x WHERE "UserId"=u."Id"),'claims',(SELECT json_agg(x ORDER BY "Id") FROM "AspNetUserClaims" x WHERE "UserId"=u."Id"),'tokens',(SELECT json_agg(x ORDER BY "Name") FROM "AspNetUserTokens" x WHERE "UserId"=u."Id"),'logins',(SELECT json_agg(x ORDER BY "ProviderKey") FROM "AspNetUserLogins" x WHERE "UserId"=u."Id")) FROM "AspNetUsers" u WHERE lower("UserName") IN('demo','sagdi') ORDER BY "UserName" LIMIT 1`));}
 run('createdb',['-U','comedores',database]);
 try{
  await start();browser=await chromium.launch({executablePath:'/usr/bin/chromium',args:['--no-sandbox']});
  assert.equal(sql(`SELECT string_agg("UserName",',') FROM "AspNetUsers"`),'sagdi');
  sql(`UPDATE "AspNetUsers" SET "UserName"='demo',"NormalizedUserName"='DEMO' WHERE "UserName"='sagdi';INSERT INTO "AspNetUserClaims"("UserId","ClaimType","ClaimValue") SELECT "Id",'ComedorId','3' FROM "AspNetUsers" WHERE "UserName"='demo';INSERT INTO "AspNetUserTokens"("UserId","LoginProvider","Name","Value") SELECT "Id",'ficticio','token','valor ficticio' FROM "AspNetUsers" WHERE "UserName"='demo';INSERT INTO "AspNetUserLogins"("UserId","LoginProvider","ProviderKey","ProviderDisplayName") SELECT "Id",'ficticio','clave','Ficticio' FROM "AspNetUsers" WHERE "UserName"='demo';INSERT INTO "AuditLogs"("OccurredAt","Actor","Ip","Action","Target","Detail","Origin") VALUES(now(),'demo','local','Prueba histórica','Registro anterior','Conservar sin reescribir','Aplicación');`);
  await stop();await start();assert.equal(sql(`SELECT count(*) FROM "AspNetUsers" WHERE "UserName"='sagdi'`),'0');
  const old=await login('demo');assert.equal(old.url(),base+'/');const before=snapshot();
  assert.throws(()=>migrate('base_equivocada'));assert.equal(sql(`SELECT count(*) FROM "AspNetUsers" WHERE "UserName"='demo'`),'1');
  sql(`UPDATE "AspNetUsers" SET "NormalizedUserName"='INCONSISTENTE' WHERE "UserName"='demo'`);assert.throws(()=>migrate());sql(`UPDATE "AspNetUsers" SET "NormalizedUserName"='DEMO' WHERE "UserName"='demo'`);
  // No fusionar ni sobrescribir si el destino ya existe.
  sql(`INSERT INTO "AspNetUsers"("Id","UserName","NormalizedUserName","EmailConfirmed","PhoneNumberConfirmed","TwoFactorEnabled","AccessFailedCount","LockoutEnabled") VALUES('cuenta-conflicto','sagdi','SAGDI',false,false,false,0,false);`);assert.throws(migrate);assert.equal(sql(`SELECT count(*) FROM "AspNetUsers" WHERE "UserName"='demo'`),'1');sql(`DELETE FROM "AspNetUsers" WHERE "Id"='cuenta-conflicto'`);
  migrate();const after=snapshot();
  for(const field of Object.keys(before.account)){if(!['UserName','NormalizedUserName','SecurityStamp','ConcurrencyStamp'].includes(field))assert(JSON.stringify(before.account[field])===JSON.stringify(after.account[field]),`Se conserva el campo ${field}`);}
  for(const field of ['roles','claims','tokens','logins'])assert(JSON.stringify(before[field])===JSON.stringify(after[field]),`Se conserva ${field}`);
  assert(before.account.SecurityStamp!==after.account.SecurityStamp);assert(before.account.ConcurrencyStamp!==after.account.ConcurrencyStamp);assert.equal(after.account.UserName,'sagdi');assert.equal(after.account.NormalizedUserName,'SAGDI');
  assert.equal(sql(`SELECT count(*) FROM "AuditLogs" WHERE "Actor"='demo' AND "Action"='Prueba histórica'`),'1');assert.equal(sql(`SELECT count(*) FROM "AuditLogs" WHERE "Action"='Renombre de usuario'`),'1');migrate();assert.equal(sql(`SELECT count(*) FROM "AuditLogs" WHERE "Action"='Renombre de usuario'`),'1');
  await old.goto(base+'/Users');assert(old.url().includes('/Login'));
  const renamed=await login('sagdi');assert.equal(renamed.url(),base+'/');await renamed.goto(base+'/Users?SelectedUser=sagdi');assert(await renamed.locator('#reset-password-form').isVisible());
  const denied=await login('demo');assert(denied.url().includes('/Login'));
  await stop();await start();assert.equal(sql(`SELECT count(*) FROM "AspNetUsers"`),'1');assert.equal(sql(`SELECT count(*) FROM "AspNetUsers" WHERE "UserName"='demo'`),'0');const again=await login('sagdi');assert.equal(again.url(),base+'/');
  console.log('PASS: cuenta nueva sagdi, compatibilidad con demo sin crear duplicados, conflicto rechazado, mismo Id/hash/roles/claims/tokens/logins/bloqueo, normalización, sesiones antiguas invalidadas, contraseña actual funciona, demo deja de entrar, conserva permiso de restablecimiento, historial intacto y renombre idempotente incluso tras reiniciar.');
 }finally{if(browser)await browser.close();await stop();run('dropdb',['-U','comedores','--force',database]);}
})().catch(e=>{console.error(e);process.exitCode=1;});
