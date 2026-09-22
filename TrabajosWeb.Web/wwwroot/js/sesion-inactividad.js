/* ====================================================
    ⏱️ Cierre por inactividad

    A los 60 minutos sin actividad la sesión caduca. Un
    minuto antes aparece un cartel con cuenta regresiva
    y la opción de seguir conectado.

    El reloj de acá es solo el aviso: quien caduca de
    verdad es la cookie del servidor, emitida con ese
    mismo plazo. Sin eso, cerrar el cartel dejaría al
    usuario trabajando con una sesión que él cree viva.
==================================================== */
(function () {

    const MINUTO = 60 * 1000;

    const DURACION = 15 * MINUTO;
    const AVISO_ANTES = 1 * MINUTO;

    /**
     * Cada cuánto, como mucho, se le avisa al servidor que hay actividad.
     * Sin este freno, mover el mouse dispararía una petición por evento.
     */
    const ESPACIADO_RENOVACION = 4 * MINUTO;

    let ultimaActividad = Date.now();
    let ultimaRenovacion = 0;
    let caducada = false;
    let modal = null;

    // ----------------------------------------------------
    // Cartel
    // ----------------------------------------------------
    function armarCartel() {

        if (document.getElementById('modalSesion')) return;

        const div = document.createElement('div');

        div.innerHTML = `
            <div class="modal fade" id="modalSesion" tabindex="-1"
                 data-bs-backdrop="static" data-bs-keyboard="false" aria-hidden="true">
                <div class="modal-dialog modal-dialog-centered modal-sm">
                    <div class="modal-content">

                        <div class="modal-body text-center py-4">
                            <i class="fa-solid fa-clock-rotate-left fs-1 text-warning"></i>

                            <h5 class="mt-3 mb-2" id="tituloSesion">
                                Tu sesión está por cerrarse
                            </h5>

                            <p class="text-secondary small mb-3" id="textoSesion">
                                Por seguridad se cierra sola cuando no la usás.
                            </p>

                            <div class="display-6 fw-light mb-3" id="cuentaSesion"></div>

                            <div class="d-grid gap-2">
                                <button type="button" class="btn btn-primary" id="btnSeguirSesion">
                                    Seguir conectado
                                </button>
                                <button type="button" class="btn btn-outline-secondary btn-sm"
                                        id="btnSalirSesion">
                                    Cerrar sesión ahora
                                </button>
                            </div>
                        </div>
                    </div>
                </div>
            </div>`;

        document.body.appendChild(div.firstElementChild);

        document.getElementById('btnSeguirSesion')
            .addEventListener('click', seguirConectado);

        document.getElementById('btnSalirSesion')
            .addEventListener('click', cerrarSesion);

        // Red de seguridad: si el cartel llegara a cerrarse —por el teclado,
        // por una extensión, por lo que sea— no puede quedar el sistema
        // accesible detrás con una sesión que ya no existe.
        document.getElementById('modalSesion')
            .addEventListener('hidden.bs.modal', () => {
                if (caducada) window.location.href = '/Cuenta/Login';
            });
    }

    function mostrarCartel() {
        armarCartel();

        // getOrCreateInstance recién existe desde Bootstrap 5.2; el sitio
        // carga la 5.0, así que se arma a mano con getInstance.
        var elModal = document.getElementById('modalSesion');
        modal = modal || bootstrap.Modal.getInstance(elModal) || new bootstrap.Modal(elModal);

        modal.show();
    }

    function ocultarCartel() {
        modal?.hide();
    }

    // ----------------------------------------------------
    // Acciones
    // ----------------------------------------------------
    async function renovar() {

        ultimaRenovacion = Date.now();

        try {
            const resp = await fetch('/Cuenta/Renovar', { method: 'POST' });
            if (!resp.ok) return false;

            const data = await resp.json();
            return data.activa === true;

        } catch {
            return false;
        }
    }

    async function seguirConectado() {

        const ok = await renovar();

        if (!ok) { caducar(); return; }

        ultimaActividad = Date.now();
        ocultarCartel();
    }

    function cerrarSesion() {

        // Se manda por POST igual que el botón del menú, para que el
        // servidor revoque la sesión además de borrar la cookie.
        const form = document.createElement('form');

        form.method = 'post';
        form.action = '/Cuenta/Logout';

        document.body.appendChild(form);
        form.submit();
    }

    /**
     * Se acabó.
     *
     * Lo primero es cerrar la sesión de verdad, no mostrar el cartel: si
     * solo se avisara, la cookie seguiría viva hasta vencer por su cuenta y
     * quien cerrara el cartel seguiría trabajando. El cartel es el aviso de
     * algo que ya pasó, no una advertencia de algo que va a pasar.
     */
    function caducar() {

        if (caducada) return;
        caducada = true;

        // Sin navegar: se borra la cookie mientras el cartel queda a la
        // vista. El usuario que vuelve entiende qué pasó en vez de
        // encontrarse en la pantalla de ingreso sin explicación.
        fetch('/Cuenta/Logout', { method: 'POST' }).catch(() => { });

        armarCartel();
        mostrarCartel();

        document.getElementById('tituloSesion').textContent = 'Tu sesión caducó';

        document.getElementById('textoSesion').textContent =
            'Pasó una hora sin actividad. Volvé a ingresar para continuar.';

        document.getElementById('cuentaSesion').textContent = '';
        document.getElementById('btnSalirSesion').classList.add('d-none');

        const boton = document.getElementById('btnSeguirSesion');

        boton.textContent = 'Volver a ingresar';
        boton.onclick = () => { window.location.href = '/Cuenta/Login'; };
    }

    // ----------------------------------------------------
    // Reloj
    // ----------------------------------------------------
    function tic() {

        if (caducada) return;

        const inactivo = Date.now() - ultimaActividad;

        if (inactivo >= DURACION) { caducar(); return; }

        if (inactivo >= DURACION - AVISO_ANTES) {

            mostrarCartel();

            const restan = Math.ceil((DURACION - inactivo) / 1000);
            const cuenta = document.getElementById('cuentaSesion');

            if (cuenta) {
                const m = Math.floor(restan / 60);
                const s = restan % 60;

                cuenta.textContent = `${m}:${String(s).padStart(2, '0')}`;
            }
        }
    }

    function huboActividad() {

        if (caducada) return;

        // Con el cartel arriba, mover el mouse no cuenta: seguir conectado
        // tiene que ser una decisión, no un accidente.
        if (document.getElementById('modalSesion')?.classList.contains('show')) return;

        ultimaActividad = Date.now();

        if (Date.now() - ultimaRenovacion > ESPACIADO_RENOVACION) renovar();
    }

    ['click', 'keydown', 'scroll', 'touchstart'].forEach(evento =>
        document.addEventListener(evento, huboActividad, { passive: true }));

    // El intervalo es corto para que la cuenta regresiva se vea correr
    setInterval(tic, 1000);

    // Volver a una pestaña dormida: el navegador frena los intervalos y el
    // reloj puede haberse quedado atrás.
    document.addEventListener('visibilitychange', () => {
        if (document.visibilityState === 'visible') tic();
    });

})();
