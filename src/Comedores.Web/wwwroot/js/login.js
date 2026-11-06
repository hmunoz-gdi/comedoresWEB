(()=>{
 const dialog=document.getElementById('account-locked-dialog')||document.getElementById('terminal-rejected-dialog');
 if(!dialog)return;
 dialog.addEventListener('close',()=>{const username=document.getElementById('Username');username.focus();username.select();});
 dialog.showModal();dialog.querySelector('button[autofocus]').focus();
})();
