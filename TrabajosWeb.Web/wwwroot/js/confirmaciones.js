
// Confirmaciones y avisos con SweetAlert, compartidos por los dos layouts.
// Uso: data-confirmar="¿Texto de la pregunta?" en un form, un botón o un enlace.
(function () {
    if (typeof Swal === 'undefined') return;

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

                el.dataset.confirmado = 'si';

                if (el.tagName === 'FORM') el.submit();
                else if (el.form) el.form.submit();
                else if (el.tagName === 'A') window.location.href = el.href;
                else el.click();
            });
        });
    });
})();