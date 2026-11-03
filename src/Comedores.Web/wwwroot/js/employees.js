(()=>{
 const $=id=>document.getElementById(id),root=document.querySelector('.employee-admin');if(!root)return;
 const exists=root.dataset.exists==='true';let stream=null,blob=null,previewUrl=null,busy=false,starting=false;
 const fields=$('employee-fields'),photoStatus=$('camera-status');
 function edit(create){fields.disabled=false;$('save-employee').disabled=false;$('edit-employee').disabled=true;$('deactivate-employee').disabled=true;$('new-employee').disabled=true;$('employee-mode').textContent=create?'Alta: completa los datos y pulsa Guardar datos.':'Modificación: realiza los cambios y pulsa Guardar datos.';(create?$('Data_Number'):$('Data_Name')).focus();}
 $('new-employee').onclick=()=>{if(exists){location.href='/Employees?New=1';return;}edit(true);$('Data_Number').value=$('Number').value;};
 // Nuevo siempre abre un formulario vacío; el número se puede capturar nuevamente.
 if(new URLSearchParams(location.search).get('New')==='1'&&exists){location.replace('/Employees?New=1');return;}
 if(new URLSearchParams(location.search).get('New')==='1'||root.dataset.editing==='true')edit(!exists);
 $('Data_Number').addEventListener('input',e=>{e.target.value=e.target.value.replace(/[^0-9]/g,'').slice(0,5);});
 $('edit-employee').onclick=()=>edit(false);
 $('employee-form').addEventListener('submit',e=>{if(busy){e.preventDefault();return;}busy=true;$('save-employee').disabled=true;});
 const dialog=$('deactivate-dialog');$('deactivate-employee').onclick=()=>dialog.showModal();dialog.addEventListener('close',()=>{if(dialog.returnValue==='confirm'){$('deactivate-employee').disabled=true;$('deactivate-form').requestSubmit();}});
 function stop(){stream?.getTracks().forEach(t=>t.stop());stream=null;$('camera-preview').srcObject=null;$('camera-preview').hidden=true;$('capture-photo').disabled=true;$('stop-camera').disabled=true;$('start-camera').disabled=!exists||busy;}
 function pending(file){if(previewUrl)URL.revokeObjectURL(previewUrl);blob=file;previewUrl=URL.createObjectURL(file);$('captured-photo').src=previewUrl;$('captured-photo').hidden=false;$('saved-photo').hidden=true;$('camera-placeholder').hidden=true;$('save-photo').disabled=false;photoStatus.textContent='Fotografía capturada o seleccionada. Pulsa Guardar fotografía para enviarla al servidor.';}
 async function start(){if(starting||busy)return;blob=null;if(previewUrl){URL.revokeObjectURL(previewUrl);previewUrl=null;}$('save-photo').disabled=true;starting=true;$('start-camera').disabled=true;stop();$('start-camera').disabled=true;
  try{if(!window.isSecureContext||!navigator.mediaDevices?.getUserMedia)throw new Error('La cámara necesita HTTPS o abrir el sistema en localhost.');
   const device=$('camera-device').value;stream=await navigator.mediaDevices.getUserMedia({audio:false,video:{width:{ideal:1280},height:{ideal:720},...(device?{deviceId:{exact:device}}:{})}});
   const video=$('camera-preview');video.srcObject=stream;video.hidden=false;$('captured-photo').hidden=true;$('saved-photo').hidden=true;$('camera-placeholder').hidden=true;await video.play();
   const devices=(await navigator.mediaDevices.enumerateDevices()).filter(d=>d.kind==='videoinput'),current=stream.getVideoTracks()[0].getSettings().deviceId;
   $('camera-device').replaceChildren(...devices.map((d,i)=>new Option(d.label||`Cámara ${i+1}`,d.deviceId,false,d.deviceId===current)));$('camera-device').disabled=false;$('capture-photo').disabled=false;$('stop-camera').disabled=false;photoStatus.textContent='Cámara encendida. Captura la foto cuando el empleado esté listo.';
  }catch(error){stop();photoStatus.textContent=error.name==='NotAllowedError'?'Permiso de cámara denegado. Autorízalo en el navegador o selecciona un archivo JPEG.':error.name==='NotFoundError'?'No se encontró una cámara conectada.':error.name==='NotReadableError'?'La cámara está ocupada. Cierra otros programas que la estén usando e inténtalo nuevamente.':error.message;}
  finally{starting=false;$('start-camera').disabled=!exists||busy;}
 }
 $('start-camera').onclick=start;$('camera-device').onchange=start;$('stop-camera').onclick=()=>{stop();if(blob)$('captured-photo').hidden=false;else if(exists)$('saved-photo').hidden=false;photoStatus.textContent='Cámara apagada.';};
 $('capture-photo').onclick=()=>{const v=$('camera-preview');if(!v.videoWidth)return;const canvas=document.createElement('canvas');const scale=Math.min(1,1280/v.videoWidth);canvas.width=Math.round(v.videoWidth*scale);canvas.height=Math.round(v.videoHeight*scale);canvas.getContext('2d').drawImage(v,0,0,canvas.width,canvas.height);canvas.toBlob(file=>{if(file){pending(file);stop();}},'image/jpeg',.9);};
 $('photo-file').onchange=e=>{const file=e.target.files[0];if(!file)return;if(file.type!=='image/jpeg'||file.size>5*1024*1024){photoStatus.textContent='Selecciona un JPEG de máximo 5 MB.';e.target.value='';return;}stop();pending(file);};
 $('photo-form').onsubmit=async e=>{e.preventDefault();if(!blob||busy)return;busy=true;$('save-photo').disabled=true;$('photo-file').disabled=true;$('start-camera').disabled=true;photoStatus.textContent='Guardando fotografía…';try{const data=new FormData(e.target);data.set('photo',blob,'fotografia.jpg');const response=await fetch(e.target.action,{method:'POST',body:data});if(response.redirected)throw new Error('Tu sesión terminó. Inicia sesión nuevamente.');const result=await response.json();photoStatus.textContent=result.message;if(result.ok){blob=null;$('photo-file').value='';}else $('save-photo').disabled=false;}catch{photoStatus.textContent='No se confirmó el guardado. Conservamos la captura: revisa tu conexión y vuelve a guardar.';$('save-photo').disabled=false;}finally{busy=false;$('photo-file').disabled=!exists;$('start-camera').disabled=!exists;}};
 if(exists){$('saved-photo').src=root.dataset.photoUrl+'&v='+Date.now();$('saved-photo').onload=()=>{$('camera-placeholder').hidden=true;};$('saved-photo').onerror=()=>{$('saved-photo').hidden=true;$('camera-placeholder').textContent='Este empleado aún no tiene fotografía.';};}
 window.addEventListener('pagehide',()=>{stop();if(previewUrl)URL.revokeObjectURL(previewUrl);});
})();
