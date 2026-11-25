(() => {
 const dialog=document.getElementById('feedback-received'),number=document.getElementById('Number'),form=document.getElementById('feedback-form');
 if(dialog){dialog.addEventListener('close',()=>{number?.focus();number?.select();});dialog.showModal();}
 function photo(img){const fallback=img.nextElementSibling;function show(){const ok=img.complete&&img.naturalWidth>0;img.hidden=!ok;if(fallback)fallback.hidden=ok;}img.addEventListener('load',show);img.addEventListener('error',show);show();}
 document.querySelectorAll('[data-employee-photo]').forEach(photo);
 if(!form||!number)return;
 const preview=document.getElementById('feedback-employee'),name=document.getElementById('feedback-employee-name'),img=document.getElementById('feedback-employee-photo');
 photo(img);let timer,version=0,request;
 function clear(){version++;request?.abort();preview.hidden=true;name.textContent='';img.removeAttribute('src');img.hidden=true;img.nextElementSibling.hidden=false;}
 async function lookup(){clearTimeout(timer);clear();const value=number.value,stamp=version;if(!/^[0-9]{1,5}$/.test(value))return;request=new AbortController();
  try{const url=new URL(form.action||location.href,location.href);url.search='';url.searchParams.set('handler','Lookup');url.searchParams.set('number',value);const response=await fetch(url,{signal:request.signal,cache:'no-store'});if(!response.ok)throw new Error();const data=await response.json();if(stamp!==version||number.value!==value)return;preview.hidden=false;name.textContent=data.ok?data.name:data.message;if(data.ok)img.src=data.photoUrl;else img.parentElement.hidden=true;if(data.ok)img.parentElement.hidden=false;}
  catch(error){if(error.name!=='AbortError'&&stamp===version){preview.hidden=false;img.parentElement.hidden=true;name.textContent='No se pudo consultar al empleado. Intenta nuevamente.';}}
 }
 number.addEventListener('input',()=>{clearTimeout(timer);clear();timer=setTimeout(lookup,350);});
 number.addEventListener('blur',lookup);
 number.addEventListener('keydown',event=>{if(event.key==='Enter'){event.preventDefault();lookup();}});
 if(number.value)lookup();
})();
