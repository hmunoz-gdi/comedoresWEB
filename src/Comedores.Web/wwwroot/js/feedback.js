(() => {
 const dialog=document.getElementById('feedback-received'),number=document.getElementById('Number');
 if(dialog){dialog.addEventListener('close',()=>{number?.focus();number?.select();});dialog.showModal();}
})();
