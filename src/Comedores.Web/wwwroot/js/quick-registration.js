(() => {
    const number = document.getElementById('Number');
    const lookup = document.getElementById('lookup-form');
    const registration = document.getElementById('registration-form');
    if (!number || !lookup || !registration) return;
    const el = id => document.getElementById(id);
    const card = el('employee-card'), button = el('register-button'), message = el('registration-message');
    let busy = false, enterHeld = false;
    let ready = !card.hidden && !button.disabled;
    let loadedNumber = card.dataset.number || '';
    let photoVersion = 0;
    const notify = text => { message.textContent = text; message.hidden = false; };
    const focus = () => number.focus({ preventScroll: true });
    const valid = () => /^[0-9]{1,5}$/.test(number.value);
    const setBusy = value => {
        busy = value;
        number.readOnly = value;
        el('lookup-button').disabled = value;
        button.disabled = value || !ready;
        registration.setAttribute('aria-busy', String(value));
    };
    function showPhoto(url, name) {
        const version = ++photoVersion;
        const photo = el('employee-photo'), placeholder = el('photo-placeholder');
        photo.hidden = true; placeholder.hidden = false;
        el('photo-label').textContent = 'Cargando fotografía…';
        photo.onload = () => { if (version !== photoVersion) return; photo.hidden = false; placeholder.hidden = true; };
        photo.onerror = () => { if (version !== photoVersion) return; photo.hidden = true; placeholder.hidden = false; el('photo-label').textContent = 'Sin fotografía disponible'; };
        photo.alt = `Fotografía de ${name || 'empleado'}`;
        photo.src = url;
    }
    const explain = error => error.name === 'TimeoutError' ? 'Se agotó el tiempo de espera.' : error.name === 'TypeError' ? 'No se pudo contactar al servidor.' : error.message;
    async function json(response) {
        if (response.redirected || !response.headers.get('content-type')?.includes('application/json')) {
            throw new Error('La sesión pudo terminar. Actualiza la página e inicia sesión antes de continuar.');
        }
        if (!response.ok) throw new Error('No se pudo completar la solicitud. Revisa la conexión y vuelve a intentar.');
        return response.json();
    }
    lookup.addEventListener('submit', async event => {
        event.preventDefault();
        if (busy) return;
        ready = false; button.disabled = true;
        if (!valid()) { notify('Escribe de 1 a 5 dígitos.'); focus(); return; }
        setBusy(true); card.hidden = true; notify('Consultando empleado…');
        try {
            const url = new URL(lookup.dataset.lookupUrl, window.location.origin);
            url.searchParams.set('number', number.value);
            const data = await json(await fetch(url, { cache: 'no-store', credentials: 'same-origin', signal: AbortSignal.timeout(15000) }));
            if (!data.ok) { notify(data.message); return; }
            loadedNumber = data.number;
            el('current-service').textContent = data.currentService;
            el('registration-number').value = data.number;
            el('RequestId').value = data.requestId;
            for (const [id, text] of Object.entries({
                'employee-name': data.name || 'Nombre no disponible en el catálogo',
                'employee-number': data.number, 'employee-area': data.area,
                'employee-position': data.position, 'employee-schedule': data.workSchedule,
                'employee-special': data.specialShift ? 'Sí' : 'No', 'employee-count': data.todayCount
            })) el(id).textContent = text;
            el('employee-stage').textContent = 'COMENSAL CONSULTADO';
            card.hidden = false; ready = data.canRegister; notify(data.message);
            showPhoto(data.photoUrl, data.name);
        } catch (error) { notify(explain(error)); }
        finally { setBusy(false); focus(); }
    });
    function confirmRepeat(data) {
        const dialog = el('repeat-dialog');
        el('repeat-message').textContent = data.message;
        dialog.returnValue = 'cancel';
        return new Promise(resolve => {
            dialog.addEventListener('close', () => resolve(dialog.returnValue === 'continue'), { once: true });
            dialog.showModal();
            el('cancel-repeat').focus();
        });
    }
    registration.addEventListener('submit', async event => {
        event.preventDefault();
        if (busy || !ready || number.value !== loadedNumber) return;
        const payload = new FormData(registration); // Mismo RequestId al reintentar tras error de red.
        setBusy(true); notify('Registrando servicio…');
        try {
            const send = () => fetch(registration.dataset.quickUrl, {
                method: 'POST', body: payload, credentials: 'same-origin', signal: AbortSignal.timeout(15000), headers: { Accept: 'application/json' }
            }).then(json);
            let data = await send();
            while (data.needsConfirmation) {
                if (!await confirmRepeat(data)) { notify('Registro cancelado. No se guardó otro servicio.'); return; }
                payload.set('ConfirmedSegmentCount', String(data.segmentCount));
                payload.set('ConfirmedServiceType', data.serviceType);
                notify('Registrando servicio confirmado…');
                data = await send();
            }
            notify(data.message);
            if (data.ok) {
                ready = false; loadedNumber = ''; number.value = '';
                el('registration-number').value = ''; el('RequestId').value = '';
                el('employee-count').textContent = data.todayCount;
                el('employee-stage').textContent = 'ÚLTIMO SERVICIO REGISTRADO';
            }
        } catch (error) { notify(explain(error) + ' Puedes reintentar esta misma solicitud sin duplicarla.'); }
        finally { setBusy(false); focus(); }
    });
    number.addEventListener('input', () => {
        ready = false; button.disabled = true;
        if (!busy) card.hidden = true;
    });
    document.addEventListener('keydown', event => {
        if (event.key === 'Escape' && !busy && document.activeElement === number) {
            event.preventDefault(); ready = false; loadedNumber = ''; number.value = ''; card.hidden = true; button.disabled = true; focus();
        }
        if (event.key !== 'Enter') return;
        const target = event.target;
        if (target !== number && target !== button && target !== el('lookup-button')) return;
        event.preventDefault();
        if (event.repeat || enterHeld || busy || event.isComposing) return;
        enterHeld = true;
        if (target === el('lookup-button')) lookup.requestSubmit();
        else if (ready && number.value === loadedNumber) registration.requestSubmit();
        else if (target === number) lookup.requestSubmit();
    });
    document.addEventListener('keyup', event => { if (event.key === 'Enter') enterHeld = false; });
    window.addEventListener('blur', () => { enterHeld = false; });
    focus();
})();
