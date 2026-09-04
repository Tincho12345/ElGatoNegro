// Filtro de la galería sin recargar la página.
// Intercepta los clics en los filtros y en los botones "Ver trabajos" de la
// sección Servicios, pide el HTML de la grilla al servidor y lo reemplaza con
// una transición. La URL se actualiza igual, así que el botón "atrás" y
// compartir el enlace siguen funcionando.
(function () {
    var contenedor = document.getElementById('galeria-contenido');
    if (!contenedor) return;

    var cargando = false;

    function irALaGaleria(siempre) {
        var seccion = document.getElementById('galeria');
        if (!seccion) return;

        var y = seccion.getBoundingClientRect().top + window.pageYOffset - 90;

        // Al filtrar desde arriba bajamos siempre; desde la grilla, solo si
        // el encabezado quedó fuera de la vista.
        if (siempre || window.pageYOffset > y) {
            window.scrollTo({ top: y, behavior: 'smooth' });
        }
    }

    function pedir(url, empujarHistorial, bajarSiempre) {
        if (cargando) return;
        cargando = true;

        contenedor.classList.add('cargando');

        if (bajarSiempre) irALaGaleria(true);

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

                if (!bajarSiempre) irALaGaleria(false);

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
                e.preventDefault();
                if (a.classList.contains('activo')) return;
                pedir(a.href, true, false);
            });
        });
    }

    // Los botones de la sección Servicios están fuera del contenedor,
    // así que se conectan una sola vez.
    document.querySelectorAll('.filtro-externo').forEach(function (a) {
        a.addEventListener('click', function (e) {
            e.preventDefault();
            pedir(a.href, true, true);
        });
    });

    window.addEventListener('popstate', function () {
        pedir(window.location.href, false, false);
    });

    conectarFiltros();
})();