// Transiciones suaves al cambiar de vista.
// Al salir de una página el contenido se desvanece; al entrar, aparece.
// Los enlaces que ya se manejan por fetch (filtros de la galería) o que
// llevan a otro sitio quedan afuera.
(function () {
    var cuerpo = document.body;

    // Entrada: se quita la clase apenas el navegador pinta la página.
    requestAnimationFrame(function () {
        cuerpo.classList.remove('pagina-entrando');
    });

    function esInterno(a) {
        if (!a.href) return false;
        if (a.origin !== window.location.origin) return false;
        if (a.hasAttribute('download')) return false;
        if (a.target && a.target !== '_self') return false;

        var protocolo = a.protocol;
        if (protocolo !== 'http:' && protocolo !== 'https:') return false;

        // Anclas dentro de la misma página: no hay cambio de vista.
        if (a.getAttribute('href').charAt(0) === '#') return false;
        if (a.pathname === window.location.pathname && a.hash) return false;

        // Los filtros de la galería se resuelven por fetch.
        if (a.classList.contains('filtro') || a.classList.contains('filtro-externo')) return false;

        // Escape manual: data-sin-transicion
        if (a.hasAttribute('data-sin-transicion')) return false;

        return true;
    }

    document.addEventListener('click', function (e) {
        if (e.defaultPrevented) return;
        if (e.button !== 0 || e.metaKey || e.ctrlKey || e.shiftKey || e.altKey) return;

        var a = e.target.closest('a');
        if (!a || !esInterno(a)) return;

        e.preventDefault();

        var destino = a.href;
        cuerpo.classList.add('pagina-saliendo');

        // Si algo demora, navegamos igual.
        var listo = false;
        function ir() {
            if (listo) return;
            listo = true;
            window.location.href = destino;
        }

        setTimeout(ir, 260);
    });

    // Al volver con el botón "atrás", el navegador puede restaurar la página
    // desde caché con la clase de salida puesta: la sacamos.
    window.addEventListener('pageshow', function (e) {
        if (e.persisted) cuerpo.classList.remove('pagina-saliendo');
    });
})();