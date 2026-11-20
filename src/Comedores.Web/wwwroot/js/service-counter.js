(() => {
 document.querySelectorAll('[data-service-counter-url]').forEach(root=>{
  const output=document.getElementById(root.dataset.counterOutput),status=document.getElementById(root.dataset.counterOutput+'-status');
  const room=document.getElementById(root.dataset.counterRoomSelect),type=document.getElementById(root.dataset.counterTypeSelect);
  let controller;
  async function refresh(){
   controller?.abort();controller=new AbortController();const current=controller;
   if(document.hidden)return;
   const url=new URL(root.dataset.serviceCounterUrl,location.origin);
   if(root.dataset.counterManual==='true'){
    if(!room?.value||!type?.value){output.textContent='—';status.textContent='Selecciona comedor y servicio';return;}
    url.searchParams.set('roomId',room.value);url.searchParams.set('serviceType',type.value);
   }
   try{
    const response=await fetch(url,{credentials:'same-origin',cache:'no-store',signal:AbortSignal.any([current.signal,AbortSignal.timeout(8000)])});
    if(!response.ok||response.redirected||!response.headers.get('content-type')?.includes('application/json'))throw new Error();
    const data=await response.json();if(current!==controller)return;
    if(data.count!==null&&(!Number.isInteger(data.count)||data.count<0))throw new Error();
    output.textContent=data.count===null?'—':String(data.count);
    status.textContent=data.message||'Incluye encargos · corte por servicio y fecha';
    // El contexto se obtiene del servidor, también al cambiar de horario.
    if(root.dataset.counterManual!=='true'&&root.classList.contains('pickup-module-banner')){
     document.getElementById('pickup-service-label').textContent=({DYN:'Desayuno',COM:'Comida',CEN:'Cena'})[data.type]||'Sin servicio vigente';
     document.getElementById('pickup-room-label').textContent=data.room||'Sin asignar';
    }
   }catch(error){if(current!==controller||error.name==='AbortError')return;output.textContent='—';status.textContent='Sin sincronización · reintentando';}
  }
  room?.addEventListener('change',refresh);type?.addEventListener('change',refresh);
  document.addEventListener('sicom:service-saved',refresh);document.addEventListener('sicom:service-context-changed',refresh);
  document.addEventListener('visibilitychange',refresh);window.addEventListener('focus',refresh);window.addEventListener('online',refresh);
  refresh();setInterval(refresh,15000);
 });
})();
