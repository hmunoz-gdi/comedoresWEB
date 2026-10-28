(() => {
    const banner = document.getElementById('server-banner');
    if (!banner) return;
    const clock = document.getElementById('server-clock');
    const date = document.getElementById('server-date');
    const service = document.getElementById('current-service');
    const status = document.getElementById('clock-status');
    const motion = document.getElementById('clock-motion');
    const zone = 'America/Mexico_City';
    const timeFormat = new Intl.DateTimeFormat('es-MX', { timeZone: zone, hour: '2-digit', minute: '2-digit', second: '2-digit', hourCycle: 'h23' });
    const dateFormat = new Intl.DateTimeFormat('es-MX', { timeZone: zone, weekday: 'long', day: 'numeric', month: 'long', year: 'numeric' });
    let serverEpoch = Number(banner.dataset.epoch), syncedAt = performance.now();
    let simulated = banner.dataset.simulated === 'true', periods = null, roomConfigured = true, pending = false;
    const update = (element, value) => { if (element.textContent !== value) element.textContent = value; };
    function render() {
        // Avanza desde la última hora recibida; no depende del reloj configurado en el navegador.
        const utc = new Date(serverEpoch + (simulated ? 0 : performance.now() - syncedAt));
        update(clock, timeFormat.format(utc));
        update(date, dateFormat.format(utc));
        clock.dateTime = date.dateTime = utc.toISOString();
        if (periods !== null) {
            const parts = Object.fromEntries(timeFormat.formatToParts(utc).map(p => [p.type, p.value]));
            const seconds = Number(parts.hour) * 3600 + Number(parts.minute) * 60 + Number(parts.second) + utc.getUTCMilliseconds() / 1000;
            const matches = periods.filter(p => p.startSeconds <= seconds && p.endSeconds >= seconds);
            const label = !roomConfigured ? 'Equipo sin comedor autorizado' : matches.length === 0 ? 'Fuera de horario' : matches.length > 1 ? 'Horarios superpuestos' : !matches[0].known ? 'Tipo de servicio sin configurar' : matches[0].name;
            update(service, label);
        }
    }
    async function synchronize() {
        if (pending || document.hidden) return;
        pending = true;
        try {
            const start = performance.now();
            const response = await fetch(banner.dataset.clockUrl, { credentials: 'same-origin', cache: 'no-store', signal: AbortSignal.timeout(8000) });
            if (response.redirected || !response.headers.get('content-type')?.includes('application/json')) {
                update(status, 'Sesión vencida: actualiza la página para entrar nuevamente.');
                banner.dataset.connection = 'warning';
                return;
            }
            if (!response.ok) throw new Error('No disponible');
            const value = await response.json();
            if (!Number.isFinite(value.utcMilliseconds) || !Array.isArray(value.windows)) throw new Error('Respuesta inválida');
            simulated = value.simulated;
            // Aproxima el transporte usando la mitad del viaje; el registro siempre usa la hora del servidor.
            serverEpoch = value.utcMilliseconds + (simulated ? 0 : (performance.now() - start) / 2);
            syncedAt = performance.now(); periods = value.windows; roomConfigured = value.roomConfigured;
            banner.dataset.connection = 'online';
            update(status, simulated ? 'PRUEBA · Hora simulada fija' : 'EN LÍNEA · Hora del servidor');
            render();
        } catch {
            banner.dataset.connection = 'warning';
            update(status, 'Sin sincronización: se muestra la hora estimada desde la última conexión.');
        } finally { pending = false; }
    }
    motion.addEventListener('click', () => {
        const paused = banner.classList.toggle('marquee-paused');
        motion.setAttribute('aria-pressed', String(paused));
        motion.textContent = paused ? 'Reanudar marquesina' : 'Pausar marquesina';
    });
    document.addEventListener('visibilitychange', () => { if (!document.hidden) synchronize(); });
    window.addEventListener('focus', synchronize);
    window.addEventListener('online', synchronize);
    window.addEventListener('offline', () => {
        banner.dataset.connection = 'warning';
        update(status, 'Sin conexión: hora estimada desde la última sincronización.');
    });
    render(); synchronize();
    setInterval(render, 1000);
    setInterval(synchronize, 30000);
})();
