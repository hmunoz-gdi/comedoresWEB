(()=>{
 const dialog=document.getElementById('account-locked-dialog');
 if(!dialog)return;
 dialog.addEventListener('close',()=>{const username=document.getElementById('Username');username.focus();username.select();});
 dialog.showModal();document.getElementById('account-locked-accept').focus();
})();
