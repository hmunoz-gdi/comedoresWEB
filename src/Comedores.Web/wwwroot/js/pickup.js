(()=>{
 const $=id=>document.getElementById(id),app=$('pickup-app');if(!app)return;
 let collector=null,members=[],request=app.dataset.request,busy=false,uncertain=false;
 const status=text=>{$('pickup-status').textContent=text;$('pickup-status').hidden=!text;};
 const numbers=()=>[...($('include-own').checked&&collector?[collector.number]:[]),...members.map(m=>m.number)];
 function controls(){
  const locked=busy||uncertain;
  for(const id of ['collector-number','include-own','member-number','member-add','pickup-clear','pickup-room','pickup-type']){const e=$(id);if(e)e.disabled=locked||(['member-number','member-add'].includes(id)&&!collector);}
  $('collector-number').readOnly=!!collector;
  for(const b of $('collector-form').querySelectorAll('button'))b.disabled=locked||!!collector;
  $('pickup-save').disabled=busy||!collector||numbers().length===0;
  $('pickup-save').textContent=uncertain?'Reintentar el mismo grupo':'Registrar '+numbers().length+' servicios';
 }
 function render(){
  const rows=[...($('include-own').checked&&collector?[{...collector,own:true}]:[]),...members];
  $('pickup-count').textContent=rows.length;
  $('pickup-list').replaceChildren(...rows.map(person=>{
   const li=document.createElement('li'),span=document.createElement('span');span.textContent=person.number+' · '+person.name+(person.own?' · Propio del responsable':' · Encargo');li.append(span);
   if(!person.own){const b=document.createElement('button');b.type='button';b.className='secondary';b.textContent='Quitar';b.disabled=busy||uncertain;b.onclick=()=>{members=members.filter(m=>m.number!==person.number);render();};li.append(b);}return li;
  }));controls();
 }
 async function json(response){
  if(response.redirected||!response.headers.get('content-type')?.includes('application/json'))throw new Error('La sesión terminó o el equipo ya no está autorizado. Inicia sesión nuevamente.');
  if(!response.ok)throw new Error('No fue posible completar la solicitud.');return response.json();
 }
 async function lookup(number){
  const url=new URL(app.dataset.lookup,location.origin);url.searchParams.set('number',number);
  const data=await json(await fetch(url,{cache:'no-store',signal:AbortSignal.timeout(15000)}));
  if(!data.ok||!data.active)throw new Error(data.message||'Empleado inexistente o de baja.');
  $('pickup-person').textContent=data.number+' · '+data.name;const photo=$('pickup-photo');photo.hidden=true;photo.onerror=()=>{photo.hidden=true;};photo.onload=()=>{photo.hidden=false;};photo.src=data.photoUrl;return data;
 }
 for(const id of ['collector-number','member-number'])$(id).addEventListener('input',e=>{e.target.value=e.target.value.replace(/[^0-9]/g,'').slice(0,5);});
 $('collector-number').addEventListener('input',()=>{collector=null;members=[];$('collector-name').textContent='';render();});
 $('collector-form').onsubmit=async e=>{
  e.preventDefault();if(busy||uncertain||collector)return;busy=true;controls();
  try{collector=await lookup($('collector-number').value);members=[];$('collector-name').textContent='Responsable: '+collector.number+' · '+collector.name;status('Agrega los números de sus compañeros.');}
  catch(error){collector=null;status(error.message);}
  finally{busy=false;render();if(collector)$('member-number').focus();}
 };
 $('member-form').onsubmit=async e=>{
  e.preventDefault();if(busy||uncertain||!collector)return;const number=$('member-number').value;
  if(number===collector.number||members.some(m=>m.number===number)){status('Este empleado ya está incluido. Para el responsable utiliza Incluir también su comida.');$('member-number').select();return;}
  busy=true;controls();try{members.push(await lookup(number));$('member-number').value='';status('Agregado. Puedes leer el siguiente gafete o registrar el grupo.');}
  catch(error){status(error.message);}finally{busy=false;render();$('member-number').focus();$('member-number').select();}
 };
 $('include-own').onchange=render;
 async function dialog(title,text,warnings,confirm){
  $('pickup-dialog-title').textContent=title;$('pickup-dialog-message').textContent=text;
  $('pickup-warnings').replaceChildren(...(warnings||[]).map(w=>{const p=document.createElement('p');p.textContent=w.number+' · '+w.name+': '+w.count+' servicio(s) previos.';return p;}));
  $('pickup-cancel').hidden=!confirm;$('pickup-accept').textContent=confirm?'Continuar':'Aceptar';const d=$('pickup-dialog');d.returnValue='cancel';
  return new Promise(resolve=>{d.addEventListener('close',()=>resolve(d.returnValue==='continue'),{once:true});d.showModal();(confirm?$('pickup-cancel'):$('pickup-accept')).focus();});
 }
 function clear(){
  collector=null;members=[];$('include-own').checked=true;$('collector-number').value='';$('collector-name').textContent='';$('member-number').value='';
  $('pickup-photo').onload=null;$('pickup-photo').removeAttribute('src');$('pickup-photo').hidden=true;
  $('pickup-person').textContent='Verifica el nombre y fotografía al agregar cada gafete.';status('');render();$('collector-number').focus();
 }
 $('pickup-clear').onclick=clear;
 $('pickup-save').onclick=async()=>{
  if(busy||!collector||numbers().length===0)return;
  if($('member-number').value){status('Pulsa Enter para agregar el último número antes de registrar el grupo.');$('member-number').focus();return;}
  if($('pickup-room')&&(!$('pickup-room').value||!$('pickup-type').value)){status('Selecciona comedor y tipo de servicio.');return;}
  busy=true;render();const payload={requestId:request,collectorNumber:collector.number,numbers:numbers(),roomId:$('pickup-room')?Number($('pickup-room').value):null,serviceType:$('pickup-type')?.value||null};
  try{
   const send=()=>fetch(app.dataset.save,{method:'POST',headers:{'Content-Type':'application/json','RequestVerificationToken':$('pickup-token').querySelector('input[name="__RequestVerificationToken"]').value},body:JSON.stringify(payload),signal:AbortSignal.timeout(30000)}).then(json);
   let data=await send();uncertain=false;
   while(data.needsConfirmation){
    if(!await dialog('Servicios repetidos',data.message,data.warnings,true)){status('Registro cancelado: no se guardó ningún servicio del grupo.');return;}
    payload.confirmedType=data.serviceType;payload.confirmedCounts=Object.fromEntries(data.warnings.map(w=>[w.number,w.count]));data=await send();uncertain=false;
   }
   if(data.ok){uncertain=false;request=data.nextRequestId;await dialog('Servicio aplicado',data.message,null,false);clear();}else status(data.message);
  }catch(error){uncertain=true;status(error.message+' Conservamos el grupo. Reintenta esta misma solicitud para confirmar el resultado sin duplicar servicios.');}
  finally{busy=false;render();if(!collector)$('collector-number').focus();}
 };
 document.addEventListener('keydown',e=>{if(e.key==='Enter'&&e.ctrlKey&&!busy){e.preventDefault();$('pickup-save').click();}});
 render();$('collector-number').focus();
})();
