// Service worker de notificaciones push. Se registra solo cuando alguien
// activa las notificaciones desde el panel; un visitante del sitio público
// nunca dispara esto.

self.addEventListener('push', function (event) {
    var datos = {};

    try {
        datos = event.data ? event.data.json() : {};
    } catch (e) {
        datos = { title: 'Gato Negro', body: event.data ? event.data.text() : '' };
    }

    var titulo = datos.title || 'Gato Negro';

    var opciones = {
        body: datos.body || '',
        icon: '/img/gato-negro.png',
        badge: '/img/gato-negro.png',
        data: { url: datos.url || '/' },
        // Mismo tag: una notificación nueva reemplaza a la anterior en vez
        // de apilarse si llegan varias seguidas.
        tag: 'gatonegro-aviso',
        renotify: true
    };

    event.waitUntil(self.registration.showNotification(titulo, opciones));
});

// Al tocar la notificación, se enfoca una pestaña del panel ya abierta si
// existe; si no, se abre una nueva.
self.addEventListener('notificationclick', function (event) {
    event.notification.close();
    var url = (event.notification.data && event.notification.data.url) || '/';

    event.waitUntil(
        clients.matchAll({ type: 'window', includeUncontrolled: true }).then(function (lista) {
            for (var i = 0; i < lista.length; i++) {
                if (lista[i].url.indexOf(url) !== -1 && 'focus' in lista[i]) {
                    return lista[i].focus();
                }
            }
            if (clients.openWindow) {
                return clients.openWindow(url);
            }
        })
    );
});
