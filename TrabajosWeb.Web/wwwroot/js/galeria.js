// Filtro de la galería sin recargar la página.
// Intercepta los clics en los filtros, pide el HTML de la grilla al servidor
// y lo reemplaza con una transición. La URL se actualiza igual, así que el
// botón "atrás" y compartir el enlace siguen funcionando.
(function () {
    var contenedor = document.getElementById('galeria-contenido');
    if (!contenedor) return;

    var cargando = false;

    function pedir(url, empujarHistorial) {
        if (cargando) return;
        cargando = true;

        contenedor.classList.add('cargando');

        fetch(url, { headers: { 'X-Requested-With': 'XMLHttpRequest' } })
            .then(function (r) {
                if (!r.ok) throw new Error('respuesta ' + r.status);
                return r.text();
            })
            .then(function (html) {
                contenedor.innerHTML = html;
                contenedor.classList.remove('cargando');
                conectarFiltros();

                if (empujarHistorial) history.pushState({ galeria: true }, '', url);

                // Dejamos el encabezado de la galería a la vista, sin saltos bruscos.
                var seccion = document.getElementById('galeria');
                if (seccion) {
                    var y = seccion.getBoundingClientRect().top + window.pageYOffset - 90;
                    if (window.pageYOffset > y) window.scrollTo({ top: y, behavior: 'smooth' });
                }

                cargando = false;
            })
            .catch(function () {
                // Si algo falla, dejamos que el navegador haga la navegación normal.
                contenedor.classList.remove('cargando');
                cargando = false;
                window.location.href = url;
            });
    }

    function conectarFiltros() {
        contenedor.querySelectorAll('.filtro').forEach(function (a) {
            a.addEventListener('click', function (e) {
                if (a.classList.contains('activo')) {
                    e.preventDefault();
                    return;
                }

                e.preventDefault();
                pedir(a.href, true);
            });
        });
    }

    window.addEventListener('popstate', function () {
        pedir(window.location.href, false);
    });

    conectarFiltros();
})();