document.getElementById('print-report')?.addEventListener('click', () => window.print());

(() => {
 const dialog=document.getElementById('delete-consumption-dialog');if(!dialog)return;
 let trigger;
 document.querySelectorAll('.delete-consumption').forEach(button=>button.addEventListener('click',()=>{
  trigger=button;document.getElementById('delete-consumption-id').value=button.dataset.id;
  document.getElementById('delete-consumption-description').textContent=button.dataset.description;
  document.getElementById('delete-reason').value='';dialog.showModal();document.getElementById('cancel-deletion').focus();
 }));
 document.getElementById('cancel-deletion').addEventListener('click',()=>dialog.close());
 dialog.addEventListener('close',()=>trigger?.focus());
 document.getElementById('delete-consumption-form').addEventListener('submit',event=>{
  if(!dialog.open){event.preventDefault();return;}
  const button=event.submitter;if(button)button.disabled=true;
 });
})();
