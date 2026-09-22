// Activa o desactiva las notificaciones push del panel. Solo hace algo si
// existe el botón de la campanita (menú hamburguesa), que solo se renderiza
// dentro del panel de admin, así que en el sitio público este script no
// hace nada. La campanita de la barra inferior es solo un acceso directo a
// Consultas y no pasa por acá.
(function () {
    var boton = document.querySelector('.boton-notificaciones');

    function base64UrlABytes(base64Url) {
        var relleno = '='.repeat((4 - (base64Url.length % 4)) % 4);
        var base64 = (base64Url + relleno).replace(/-/g, '+').replace(/_/g, '/');
        var cruda = window.atob(base64);
        var bytes = new Uint8Array(cruda.length);
        for (var i = 0; i < cruda.length; i++) bytes[i] = cruda.charCodeAt(i);
        return bytes;
    }

    function pintar(activo) {
        if (!boton) return;
        boton.classList.toggle('btn-outline-primary', !activo);
        boton.classList.toggle('btn-primary', activo);
        boton.setAttribute('aria-pressed', activo ? 'true' : 'false');
        boton.title = activo
            ? 'Notificaciones activadas (tocá para desactivar)'
            : 'Notificaciones desactivadas (tocá para activar)';

        var icono = boton.querySelector('i');
        if (icono) {
            icono.classList.toggle('fa-bell', activo);
            icono.classList.toggle('fa-bell-slash', !activo);
        }
    }

    function navegadorLegible() {
        var ua = navigator.userAgent;
        var nav = /Edg/.test(ua) ? 'Edge'
            : /Chrome/.test(ua) ? 'Chrome'
            : /Firefox/.test(ua) ? 'Firefox'
            : /Safari/.test(ua) ? 'Safari'
            : 'Navegador';
        var so = /Windows/.test(ua) ? 'Windows'
            : /Android/.test(ua) ? 'Android'
            : /iPhone|iPad/.test(ua) ? 'iOS'
            : /Mac/.test(ua) ? 'Mac'
            : '';
        return so ? nav + ' en ' + so : nav;
    }

    function avisar(icono, titulo, texto) {
        if (window.Swal) {
            Swal.fire({
                icon: icono,
                title: titulo,
                text: texto,
                toast: icono === 'success',
                position: icono === 'success' ? 'top-end' : undefined,
                showConfirmButton: icono !== 'success',
                timer: icono === 'success' ? 3000 : undefined
            });
        } else if (texto) {
            alert(titulo + ': ' + texto);
        }
    }

    // ---------- Contador de consultas sin leer ----------
    // Una sola función, un solo endpoint: actualiza todos los badges que
    // haya en la página (escritorio + acceso directo de la barra inferior),
    // sin duplicar la lógica en cada uno.
    function actualizarContador() {
        fetch('/Notificaciones/no-leidas')
            .then(function (r) { return r.ok ? r.json() : null; })
            .then(function (datos) {
                if (!datos) return;
                var cantidad = datos.cantidad || 0;

                document.querySelectorAll('.badge-notificaciones').forEach(function (badge) {
                    if (cantidad > 0) {
                        badge.textContent = cantidad > 99 ? '99+' : String(cantidad);
                        badge.style.display = '';
                    } else {
                        badge.style.display = 'none';
                    }
                });
            })
            .catch(function () {
                // Sin poder consultar el contador, se deja como estaba.
            });
    }

    actualizarContador();
    setInterval(actualizarContador, 60000);

    // El service worker avisa al instante cuando llega una consulta nueva
    // o cuando se marca una como leída desde la propia notificación.
    if ('serviceWorker' in navigator) {
        navigator.serviceWorker.addEventListener('message', function (event) {
            if (event.data && event.data.tipo === 'actualizar-contador') {
                actualizarContador();
            }
        });
    }

    if (!boton) return;

    if (!('serviceWorker' in navigator) || !('PushManager' in window)) {
        // Navegador viejo o sin soporte: mejor ocultar el botón que
        // mostrar algo que nunca va a funcionar.
        boton.hidden = true;
        return;
    }

    function estadoInicial() {
        navigator.serviceWorker.register('/sw.js')
            .then(function () { return navigator.serviceWorker.ready; })
            .then(function (registro) { return registro.pushManager.getSubscription(); })
            .then(function (suscripcion) { pintar(!!suscripcion); })
            .catch(function () {
                // Sin poder consultar el estado, el botón queda como estaba
                // (apagado) para que el usuario pueda reintentar.
            });
    }

    function activar() {
        return fetch('/Notificaciones/clave-publica')
            .then(function (r) {
                if (!r.ok) throw new Error('sin-clave');
                return r.json();
            })
            .then(function (clave) {
                return navigator.serviceWorker.ready.then(function (registro) {
                    return registro.pushManager.subscribe({
                        userVisibleOnly: true,
                        applicationServerKey: base64UrlABytes(clave.publicKey)
                    });
                });
            })
            .then(function (suscripcion) {
                var json = suscripcion.toJSON();

                return fetch('/Notificaciones/suscribir', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({
                        endpoint: json.endpoint,
                        p256dh: json.keys.p256dh,
                        auth: json.keys.auth,
                        dispositivo: navegadorLegible()
                    })
                });
            })
            .then(function (r) {
                if (!r.ok) throw new Error('no-guardo');
                pintar(true);
                avisar('success', 'Notificaciones activadas');
            });
    }

    function desactivar() {
        return navigator.serviceWorker.ready
            .then(function (registro) { return registro.pushManager.getSubscription(); })
            .then(function (suscripcion) {
                if (!suscripcion) return;

                var endpoint = suscripcion.endpoint;

                return suscripcion.unsubscribe().then(function () {
                    return fetch('/Notificaciones/desuscribir', {
                        method: 'POST',
                        headers: { 'Content-Type': 'application/json' },
                        body: JSON.stringify(endpoint)
                    });
                });
            })
            .then(function () {
                pintar(false);
                avisar('success', 'Notificaciones desactivadas');
            });
    }

    boton.addEventListener('click', function () {
        boton.disabled = true;

        var yaActivo = boton.classList.contains('btn-primary');
        var accion;

        if (yaActivo) {
            accion = desactivar();
        } else if (Notification.permission === 'denied') {
            accion = Promise.reject('bloqueado');
        } else {
            accion = activar();
        }

        accion
            .catch(function (motivo) {
                pintar(false);

                if (motivo === 'bloqueado') {
                    avisar('warning', 'Notificaciones bloqueadas',
                        'Las bloqueaste antes en este navegador. Para activarlas, habilitalas ' +
                        'desde el candado o el ícono de sitio en la barra de direcciones.');
                } else {
                    avisar('error', 'No se pudo activar',
                        'Probá de nuevo en un momento.');
                }
            })
            .finally(function () {
                boton.disabled = false;
            });
    });

    estadoInicial();
})();
