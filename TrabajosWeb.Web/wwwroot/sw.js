// Service worker de notificación push con aviso instantáneo a las pestañas
self.addEventListener('push', function (event) {
    var datos = {};

    try {
        datos = event.data ? event.data.json() : {};
    } catch (e) {
        datos = { title: 'Gato Negro', body: event.data ? event.data.text() : '' };
    }

    var titulo = datos.title || 'Gato Negro';
    // Fallback de texto para asegurar que siempre haya contenido visual en el Toast
    var cuerpo = datos.body || '¡Tenés una nueva consulta en la peluquería!';

    var opciones = {
        body: cuerpo,
        icon: '/img/gato-negro.png',
        badge: '/img/gato-negro.png',
        data: {
            url: datos.url || '/Admin/Consultas',
            consultaId: datos.consultaId || null
        },
        tag: 'gatonegro-aviso',
        renotify: true,
        requireInteraction: true,
        vibrate: [200, 100, 200],
        actions: [
            { action: 'ver', title: 'Ver consulta' },
            { action: 'marcar-leida', title: 'Marcar como leída' }
        ]
    };

    // Muestra la notificación y además le avisa a todas las pestañas abiertas que actualicen el contador al instante
    event.waitUntil(
        self.registration.showNotification(titulo, opciones).then(function () {
            return avisarPestanas();
        })
    );
});

// Al tocar la notificación (o uno de sus botones)
self.addEventListener('notificationclick', function (event) {
    var datos = event.notification.data || {};
    var urlDestino = datos.url || '/Admin/Consultas';
    var consultaId = datos.consultaId;

    event.notification.close();

    if (event.action === 'marcar-leida') {
        if (!consultaId) return;

        // Se marca en el servidor sin abrir ni enfocar ninguna pestaña.
        event.waitUntil(
            fetch('/Notificaciones/marcar-leida/' + consultaId, { method: 'POST' })
                .then(function () { return avisarPestanas(); })
        );
        return;
    }

    // Acción "ver" o clic en el cuerpo: se enfoca una pestaña del panel ya abierta o se abre una nueva
    event.waitUntil(
        clients.matchAll({ type: 'window', includeUncontrolled: true }).then(function (clientList) {
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
            if (clients.openWindow) {
                return clients.openWindow(urlDestino);
            }
        })
    );
});

function avisarPestanas() {
    return clients.matchAll({ type: 'window', includeUncontrolled: true }).then(function (clientList) {
        clientList.forEach(function (client) {
            client.postMessage({ tipo: 'actualizar-contador' });
        });
    });
}
