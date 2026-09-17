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
        data: { url: datos.url || '/Admin/Consultas' },
        // Mismo tag: una notificación nueva reemplaza a la anterior en vez
        // de apilarse si llegan varias seguidas.
        tag: 'gatonegro-aviso',
        renotify: true,
        // Propiedades para forzar mayor visibilidad y banner en pantalla
        requireInteraction: true,
        vibrate: [200, 100, 200]
    };

    event.waitUntil(self.registration.showNotification(titulo, opciones));
});

// Al tocar la notificación, se enfoca una pestaña del panel ya abierta si
// existe; si no, se abre una nueva orientada a la URL de destino.
self.addEventListener('notificationclick', function (event) {
    event.notification.close();
    var urlDestino = (event.notification.data && event.notification.data.url) || '/Admin/Consultas';

    event.waitUntil(
        clients.matchAll({ type: 'window', includeUncontrolled: true }).then(function (clientList) {
            // Si ya hay una pestaña abierta del sitio, la trae al frente y la redirige si es necesario
            for (var i = 0; i < clientList.length; i++) {
                var client = clientList[i];
                if ('focus' in client) {
                    client.focus();
                    if ('navigate' in client) {
                        return client.navigate(urlDestino);
                    }
                    return;
                }
            }
            // Si no hay ninguna pestaña abierta, abre una nueva directamente en la sección
            if (clients.openWindow) {
                return clients.openWindow(urlDestino);
            }
        })
    );
});