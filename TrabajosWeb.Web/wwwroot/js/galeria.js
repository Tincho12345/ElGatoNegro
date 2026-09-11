// Filtro de la galería con Isotope, al estilo de la plantilla Baker.
// Todas las piezas están en el HTML desde el principio: al tocar un filtro
// las tarjetas se reacomodan deslizándose, sin pedir nada al servidor.
//
// No usa imagesLoaded: las tarjetas tienen altura fija por CSS, así que el
// acomodo se puede calcular enseguida. Cada foto que llega dispara un
// reacomodo por las dudas, que es más barato que bloquear el arranque.
(function () {
    var bloque = document.querySelector('.isotope-layout');
    if (!bloque || typeof Isotope === 'undefined') return;

    var contenedor = bloque.querySelector('.isotope-container');
    if (!contenedor) return;

    var filtros = bloque.querySelectorAll('.isotope-filters li');
    var vacio = bloque.querySelector('.galeria-sin-resultados');
    var panelProducto = bloque.querySelector('#filtrosProducto');
    var columna = bloque.querySelector('#galeriaColumna');
    var slugProductos = bloque.dataset.slugProductos || '';
    var grilla = null;

    // Estado de todos los filtros a la vez. Isotope decide con esto,
    // así se pueden combinar categoría + marca + precio sin pelearse.
    var estado = {
        categoria: '',
        subcat: '',
        marca: '',
        precioMin: null,
        precioMax: null,
        soloOferta: false
    };

    function coincide(item) {
        // Isotope pasa el elemento; la guarda cubre el caso de que llegue
        // por "this" en vez de por argumento.
        if (!item || !item.classList) item = this;
        if (!item || !item.classList) return true;

        if (estado.categoria && !item.classList.contains(estado.categoria)) return false;
        if (estado.subcat && item.dataset.subcat !== estado.subcat) return false;
        if (estado.marca && item.dataset.marca !== estado.marca) return false;
        if (estado.soloOferta && item.dataset.oferta !== '1') return false;

        if (estado.precioMin !== null || estado.precioMax !== null) {
            var precio = parseFloat(item.dataset.precio);

            // Sin precio cargado no pertenece a ningún rango.
            if (isNaN(precio)) return false;
            if (estado.precioMin !== null && precio < estado.precioMin) return false;
            if (estado.precioMax !== null && precio > estado.precioMax) return false;
        }

        return true;
    }

    // Muestra el aviso solo cuando el filtro no dejó ninguna pieza a la vista.
    function revisarVacio() {
        if (!vacio || !grilla) return;
        vacio.classList.toggle('visible', grilla.filteredItems.length === 0);
    }

    // Cambia la dirección del navegador sin recargar, para que el enlace
    // se pueda compartir y el botón "atrás" funcione.
    function actualizarUrl(slug) {
        var url = slug ? '?categoria=' + encodeURIComponent(slug) : window.location.pathname;
        history.pushState({ categoria: slug }, '', url);
    }

    // Cuántos filtros de producto hay puestos. Se muestra en el botón
    // de pantallas chicas, donde el panel está cerrado.
    function contarFiltros() {
        var n = 0;
        if (estado.subcat) n++;
        if (estado.marca) n++;
        if (estado.precioMin !== null || estado.precioMax !== null) n++;
        if (estado.soloOferta) n++;
        return n;
    }

    function revisarCuenta() {
        if (!panelProducto) return;

        var globo = panelProducto.querySelector('#filtrosCuenta');
        if (!globo) return;

        var n = contarFiltros();
        globo.textContent = n;
        globo.hidden = n === 0;
    }

    // El panel lateral solo aparece dentro de Productos. Al mostrarlo la
    // grilla pierde un cuarto del ancho, así que Isotope tiene que recalcular.
    function revisarPanel() {
        if (!panelProducto || !columna) return;

        var mostrar = !!slugProductos && estado.categoria === slugProductos;

        if (panelProducto.hidden === !mostrar) return;

        panelProducto.hidden = !mostrar;
        columna.classList.toggle('col-lg-9', mostrar);

        // El ancho cambia recién después de que el navegador repinta.
        requestAnimationFrame(function () {
            if (grilla) grilla.layout();
        });
    }

    function refrescar() {
        if (!grilla) return;

        grilla.arrange({ filter: coincide });
        revisarCuenta();
        setTimeout(revisarVacio, 420);
    }

    function limpiarFiltrosProducto() {
        estado.subcat = '';
        estado.marca = '';
        estado.precioMin = null;
        estado.precioMax = null;
        estado.soloOferta = false;

        if (!panelProducto) return;

        panelProducto.querySelectorAll('.filtro-lista[data-grupo]').forEach(function (grupo) {
            grupo.querySelectorAll('.filtro-opcion').forEach(function (opcion) {
                opcion.classList.toggle('activo', opcion.dataset.valor === '');
            });
        });

        var min = panelProducto.querySelector('#precioMin');
        var max = panelProducto.querySelector('#precioMax');
        var oferta = panelProducto.querySelector('#soloOferta');

        if (min) min.value = '';
        if (max) max.value = '';
        if (oferta) oferta.checked = false;

        revisarCuenta();
    }

    function aplicar(filtro, slug, empujar) {
        if (!grilla) return;

        // Cambiar de categoría descarta los filtros de producto: quedarían
        // aplicados sobre algo donde no significan nada.
        if (slug !== estado.categoria) limpiarFiltrosProducto();

        estado.categoria = slug || '';

        filtros.forEach(function (li) {
            li.classList.toggle('filter-active', li.dataset.filter === filtro);
        });

        revisarPanel();
        refrescar();

        if (empujar) actualizarUrl(slug);
    }

    var filtroInicial = bloque.dataset.defaultFilter || '*';
    estado.categoria = filtroInicial === '*' ? '' : filtroInicial.replace(/^\./, '');

    grilla = new Isotope(contenedor, {
        itemSelector: '.isotope-item',
        layoutMode: bloque.dataset.layout || 'masonry',
        filter: coincide,
        // Con jQuery presente, Isotope pasaría la función a .is() y la
        // llamaría con (indice, elemento). Filtramos en vanilla.
        isJQueryFiltering: false,
        sortBy: bloque.dataset.sort || 'original-order',
        transitionDuration: '0.65s',
        stagger: 40,
        percentPosition: true,
        masonry: { columnWidth: '.isotope-item' },
        hiddenStyle: { opacity: 0, transform: 'scale(0.86) translateY(18px)' },
        visibleStyle: { opacity: 1, transform: 'scale(1) translateY(0)' }
    });

    contenedor.classList.add('lista');
    revisarPanel();
    revisarVacio();

    // Cada foto que termina de bajar puede cambiar la altura real de su
    // tarjeta: pedimos un reacomodo, agrupando los pedidos seguidos.
    var pendiente = null;

    function reacomodar() {
        clearTimeout(pendiente);
        pendiente = setTimeout(function () {
            if (grilla) grilla.layout();
        }, 120);
    }

    contenedor.querySelectorAll('img').forEach(function (img) {
        if (img.complete) return;
        img.addEventListener('load', reacomodar, { once: true });
        img.addEventListener('error', reacomodar, { once: true });
    });

    window.addEventListener('load', reacomodar);

    filtros.forEach(function (li) {
        li.addEventListener('click', function () {
            if (li.classList.contains('filter-active')) return;
            aplicar(li.dataset.filter, li.dataset.slug, true);
        });
    });

    // ---------- Panel lateral de productos ----------

    if (panelProducto) {
        // Elegir una opción dentro de un grupo
        panelProducto.querySelectorAll('.filtro-lista[data-grupo]').forEach(function (grupo) {
            var clave = grupo.dataset.grupo; // "subcat" o "marca"

            grupo.querySelectorAll('.filtro-opcion').forEach(function (opcion) {
                opcion.addEventListener('click', function () {
                    grupo.querySelectorAll('.filtro-opcion').forEach(function (otra) {
                        otra.classList.remove('activo');
                    });

                    opcion.classList.add('activo');
                    estado[clave] = opcion.dataset.valor;
                    refrescar();
                });
            });
        });

        // Plegar y desplegar cada grupo
        panelProducto.querySelectorAll('.filtro-cabecera').forEach(function (cabecera) {
            cabecera.addEventListener('click', function () {
                var abierto = cabecera.getAttribute('aria-expanded') === 'true';
                cabecera.setAttribute('aria-expanded', !abierto);
                reacomodar();
            });
        });

        // "Ver más" dentro de un grupo con muchas opciones
        panelProducto.querySelectorAll('.filtro-vermas').forEach(function (boton) {
            var lista = boton.parentElement.querySelector('.filtro-lista');
            if (!lista) return;

            var textoOriginal = boton.textContent.trim();

            boton.addEventListener('click', function () {
                var expandida = lista.classList.toggle('expandida');
                boton.setAttribute('aria-expanded', expandida);
                boton.textContent = expandida ? 'Ver menos' : textoOriginal;
            });
        });

        // Abrir y cerrar el panel en pantallas chicas
        var abrir = panelProducto.querySelector('#abrirFiltros');
        var cuerpo = panelProducto.querySelector('#filtrosCuerpo');

        if (abrir && cuerpo) {
            abrir.addEventListener('click', function () {
                var abierto = cuerpo.classList.toggle('abierto');
                abrir.setAttribute('aria-expanded', abierto);
            });
        }

        var min = panelProducto.querySelector('#precioMin');
        var max = panelProducto.querySelector('#precioMax');
        var oferta = panelProducto.querySelector('#soloOferta');

        // Se espera a que deje de tipear para no reacomodar en cada tecla.
        var esperaPrecio = null;

        function leerPrecios() {
            clearTimeout(esperaPrecio);
            esperaPrecio = setTimeout(function () {
                var desde = min && min.value !== '' ? parseFloat(min.value) : NaN;
                var hasta = max && max.value !== '' ? parseFloat(max.value) : NaN;

                estado.precioMin = isNaN(desde) ? null : desde;
                estado.precioMax = isNaN(hasta) ? null : hasta;
                refrescar();
            }, 350);
        }

        if (min) min.addEventListener('input', leerPrecios);
        if (max) max.addEventListener('input', leerPrecios);

        if (oferta) {
            oferta.addEventListener('change', function () {
                estado.soloOferta = oferta.checked;
                refrescar();
            });
        }

        panelProducto.querySelectorAll('#limpiarFiltros, #limpiarFiltrosMovil').forEach(function (boton) {
            boton.addEventListener('click', function () {
                limpiarFiltrosProducto();
                refrescar();
            });
        });
    }

    // Baja hasta la galería dejando lugar para la barra fija de arriba.
    function irAGaleria(suave) {
        var seccion = document.getElementById('galeria');
        if (!seccion) return;

        var y = seccion.getBoundingClientRect().top + window.pageYOffset - 90;
        window.scrollTo({ top: y, behavior: suave ? 'smooth' : 'auto' });
    }

    // En pantallas chicas el menú queda abierto al no navegar de verdad.
    function cerrarMenu() {
        var menu = document.querySelector('.navbar-collapse.show');
        if (menu) menu.classList.remove('show');
    }

    // Los botones "Ver trabajos" de Servicios y el menú de arriba filtran acá mismo.
    document.querySelectorAll('.filtro-externo').forEach(function (a) {
        a.addEventListener('click', function (e) {
            var slug = a.dataset.slug;
            if (!slug) return;

            e.preventDefault();
            cerrarMenu();
            aplicar('.' + slug, slug, true);
            irAGaleria(true);
        });
    });

    // Al llegar desde otra página con la categoría en la dirección, el
    // servidor ya filtró: solo falta bajar hasta la grilla.
    if (new URLSearchParams(window.location.search).get('categoria')) {
        setTimeout(function () { irAGaleria(false); }, 150);
    }

    // Botón "atrás" del navegador.
    window.addEventListener('popstate', function () {
        var slug = new URLSearchParams(window.location.search).get('categoria');
        aplicar(slug ? '.' + slug : '*', slug || '', false);
    });
})();
