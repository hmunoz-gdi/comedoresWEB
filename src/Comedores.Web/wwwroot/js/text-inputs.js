// Selecciona el contenido al entrar al campo; permite colocar el cursor en clics posteriores.
(() => {
    const types = new Set(['text', 'search', 'tel', 'email', 'url', 'password']);
    const eligible = element => (element instanceof HTMLTextAreaElement ||
        element instanceof HTMLInputElement && types.has(element.type)) && !element.disabled;
    let pointerFocus = null;
    const select = element => { if (eligible(element)) element.select(); };
    document.addEventListener('pointerdown', event => {
        pointerFocus = eligible(event.target) && document.activeElement !== event.target ? event.target : null;
    }, true);
    document.addEventListener('focusin', event => select(event.target));
    document.addEventListener('mouseup', event => {
        if (event.target === pointerFocus && document.activeElement === pointerFocus) {
            event.preventDefault(); select(pointerFocus);
        }
        pointerFocus = null;
    }, true);
    window.addEventListener('blur', () => { pointerFocus = null; });
    select(document.activeElement);
})();
