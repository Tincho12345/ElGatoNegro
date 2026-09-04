// Confirmaciones y avisos con SweetAlert, compartidos por los dos layouts.
// Uso: data-confirmar="¿Texto de la pregunta?" en un form, un botón o un enlace.
// Si además lleva data-ajax, el envío va por fetch y no recarga la página:
//   data-quitar   selector del contenedor que se elimina de la pantalla
//   data-exito    mensaje del aviso al terminar
// El contenedor padre puede llevar data-vacio con el texto a mostrar
// cuando ya no queda ningún elemento.
(function () {
    if (typeof Swal === 'undefined') return;

    var Aviso = Swal.mixin({
        toast: true,
        position: 'top-end',
        showConfirmButton: false,
        timer: 3500,
        timerProgressBar: true
    });

    // Actualiza los contadores del panel si el servidor los devolvió.
    function refrescarContadores(r) {
        var total = document.querySelector('[data-contador="total"]');
        if (total && r.total !== undefined) total.textContent = r.total;

        var publicados = document.querySelector('[data-contador="publicados"]');
        if (publicados && r.publicados !== undefined) publicados.textContent = r.publicados;
    }

    function quitarDeLaPantalla(contenedor, mensajeVacio) {
        contenedor.style.transition = 'opacity .3s, transform .3s';
        contenedor.style.opacity = '0';
        contenedor.style.transform = 'scale(.9)';

        setTimeout(function () {
            var padre = contenedor.parentElement;
            contenedor.remove();

            if (padre && padre.children.length === 0 && mensajeVacio) {
                var p = document.createElement('p');
                p.className = 'tenue chico sin-margen';
                p.textContent = mensajeVacio;
                padre.replaceWith(p);
            }
        }, 300);
    }

    function enviarPorAjax(form) {
        var contenedor = form.dataset.quitar
            ? form.closest(form.dataset.quitar)
            : null;

        var padre = contenedor ? contenedor.parentElement : null;
        var mensajeVacio = padre ? padre.dataset.vacio : null;

        fetch(form.action, {
            method: 'POST',
            body: new FormData(form),
            headers: { 'X-Requested-With': 'XMLHttpRequest' }
        })
            .then(function (r) { return r.json(); })
            .then(function (r) {
                if (!r.ok) {
                    Swal.fire({ icon: 'error', title: r.error || 'No se pudo eliminar.' });
                    return;
                }

                if (contenedor) quitarDeLaPantalla(contenedor, mensajeVacio);

                refrescarContadores(r);

                Aviso.fire({
                    icon: 'success',
                    title: form.dataset.exito || 'Listo.'
                });
            })
            .catch(function () {
                Swal.fire({ icon: 'error', title: 'Falló la conexión. Probá de nuevo.' });
            });
    }

    document.querySelectorAll('[data-confirmar]').forEach(function (el) {
        var evento = el.tagName === 'FORM' ? 'submit' : 'click';

        el.addEventListener(evento, function (e) {
            if (el.dataset.confirmado === 'si') return;

            e.preventDefault();

            Swal.fire({
                title: el.dataset.confirmar,
                icon: 'warning',
                showCancelButton: true,
                confirmButtonText: 'Sí, eliminar',
                cancelButtonText: 'Cancelar',
                confirmButtonColor: '#c0392b',
                reverseButtons: true,
                focusCancel: true
            }).then(function (r) {
                if (!r.isConfirmed) return;

                if (el.tagName === 'FORM' && el.dataset.ajax !== undefined) {
                    enviarPorAjax(el);
                    return;
                }

                el.dataset.confirmado = 'si';

                if (el.tagName === 'FORM') el.submit();
                else if (el.form) el.form.submit();
                else if (el.tagName === 'A') window.location.href = el.href;
                else el.click();
            });
        });
    });
})();