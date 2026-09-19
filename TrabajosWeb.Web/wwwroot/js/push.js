// Activa o desactiva las notificaciones push del panel y mantiene
// sincronizadas todas las campanitas que haya en la página (la de
// escritorio en el navbar y la de la barra inferior en mobile).
(function () {
    var botones = Array.prototype.slice.call(document.querySelectorAll('.navbar .boton-notificaciones'));
    if (!botones.length) return;

    if (!('serviceWorker' in navigator) || !('PushManager' in window)) {
        botones.forEach(function (boton) { boton.hidden = true; });
        return;
    }

    function esBotonEscritorio(boton) {
        return boton.classList.contains('btn');
    }

    function base64UrlABytes(base64Url) {
        var relleno = '='.repeat((4 - (base64Url.length % 4)) % 4);
        var base64 = (base64Url + relleno).replace(/-/g, '+').replace(/_/g, '/');
        var cruda = window.atob(base64);
        var bytes = new Uint8Array(cruda.length);
        for (var i = 0; i < cruda.length; i++) bytes[i] = cruda.charCodeAt(i);
        return bytes;
    }

    function pintar(activo) {
        botones.forEach(function (boton) {
            boton.setAttribute('aria-pressed', activo ? 'true' : 'false');
            boton.title = activo
                ? 'Notificaciones activadas (tocá para desactivar)'
                : 'Activar notificaciones de consultas nuevas';

            var icono = boton.querySelector('i');
            if (icono) {
                icono.className = activo ? 'fa-solid fa-bell' : 'fa-solid fa-bell-slash';
            }

            if (esBotonEscritorio(boton)) {
                boton.classList.toggle('btn-outline-primary', !activo);
                boton.classList.toggle('btn-primary', activo);
            } else {
                boton.classList.toggle('active', activo);
            }
        });
    }

    function estaActivo() {
        return botones[0].getAttribute('aria-pressed') === 'true';
    }

    function deshabilitar(deshabilitado) {
        botones.forEach(function (boton) { boton.disabled = deshabilitado; });
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
            .catch(function () { });
    }

    function estadoInicial() {
        navigator.serviceWorker.register('/sw.js')
            .then(function () { return navigator.serviceWorker.ready; })
            .then(function (registro) { return registro.pushManager.getSubscription(); })
            .then(function (suscripcion) { pintar(!!suscripcion); })
            .catch(function () { });

        actualizarContador();
        setInterval(actualizarContador, 60000);
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

    botones.forEach(function (boton) {
        boton.addEventListener('click', function () {
            deshabilitar(true);

            var yaActivo = estaActivo();
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
                    deshabilitar(false);
                });
        });
    });

    // ---------- Redirección de la campanita móvil al hacer clic con avisos pendientes ----------
    var botonMobileNoti = document.querySelector('.mobile-bottom-nav .boton-notificaciones');
    if (botonMobileNoti) {
        botonMobileNoti.addEventListener('click', function (e) {
            var badge = botonMobileNoti.querySelector('.badge-notificaciones');
            if (badge) {
                var texto = badge.textContent.trim();
                var cantidad = texto === '99+' ? 100 : (parseInt(texto, 10) || 0);

                if (cantidad > 0) {
                    window.location.href = '/Admin/Consultas';
                } else {
                    e.preventDefault();
                }
            }
        });
    }

    // ---------- Escuchar avisos en tiempo real del Service Worker ----------
    if ('serviceWorker' in navigator) {
        navigator.serviceWorker.addEventListener('message', function (event) {
            if (event.data && event.data.tipo === 'actualizar-contador') {
                actualizarContador();
            }
        });
    }

    estadoInicial();
})();