// Buscador, filtros y orden de la tabla del panel de administración.
// Trabaja sobre las filas que ya están en la página: no consulta al servidor.
//
// La barra de filtros vive fuera de #panelContenido a propósito. Cuando se
// guarda o se borra un trabajo, trabajo-modal.js reemplaza ese bloque entero;
// al quedar afuera, lo que el usuario haya tipeado o elegido sobrevive, y un
// MutationObserver vuelve a aplicarlo sobre las filas nuevas.
(function () {
    var barra = document.getElementById('filtrosPanel');
    var contenido = document.getElementById('panelContenido');
    if (!barra || !contenido) return;

    var campo = barra.querySelector('#buscarFila');
    var limpiar = barra.querySelector('#limpiarFila');
    var botonVoz = barra.querySelector('#dictarFila');
    var selCategoria = barra.querySelector('#filtroCategoria');
    var selEstado = barra.querySelector('#filtroEstado');
    var selOrden = barra.querySelector('#ordenTabla');
    var cuenta = barra.querySelector('#cuentaFilas');

    var palabras = [];
    var observador = null;

    // Interruptor del observador, para poder apagarlo mientras se reordena.
    function observar(activo) {
        if (!observador) return;

        if (activo) {
            observador.observe(contenido, { childList: true, subtree: true });
        } else {
            observador.disconnect();
        }
    }

    // Misma limpieza que hace la vista al armar data-buscar.
    function normalizar(texto) {
        return (texto || '')
            .normalize('NFD')
            .replace(/[\u0300-\u036f]/g, '')
            .toLowerCase();
    }

    function cuerpo() {
        return contenido.querySelector('tbody');
    }

    function filas() {
        var tbody = cuerpo();
        if (!tbody) return [];

        return Array.prototype.slice
            .call(tbody.querySelectorAll('tr'))
            .filter(function (tr) { return !tr.classList.contains('sin-coincidencias'); });
    }

    function pasa(tr) {
        if (selCategoria && selCategoria.value && tr.dataset.categoria !== selCategoria.value) {
            return false;
        }

        if (selEstado && selEstado.value && tr.dataset.publicado !== selEstado.value) {
            return false;
        }

        // Todas las palabras tienen que estar; el orden no importa.
        var donde = tr.dataset.buscar || '';

        for (var i = 0; i < palabras.length; i++) {
            if (donde.indexOf(palabras[i]) === -1) return false;
        }

        return true;
    }

    function ordenar(lista) {
        if (!selOrden) return lista;

        var modo = selOrden.value;

        return lista.sort(function (a, b) {
            switch (modo) {
                case 'fecha-asc':
                    return (a.dataset.fecha || '').localeCompare(b.dataset.fecha || '');
                case 'titulo-asc':
                    return (a.dataset.titulo || '').localeCompare(b.dataset.titulo || '');
                case 'titulo-desc':
                    return (b.dataset.titulo || '').localeCompare(a.dataset.titulo || '');
                default: // fecha-desc
                    return (b.dataset.fecha || '').localeCompare(a.dataset.fecha || '');
            }
        });
    }

    // Fila de aviso cuando ningún trabajo coincide.
    function avisoVacio(tbody, mostrar, columnas) {
        var aviso = tbody.querySelector('.sin-coincidencias');

        if (!mostrar) {
            if (aviso) aviso.remove();
            return;
        }

        if (aviso) return;

        aviso = document.createElement('tr');
        aviso.className = 'sin-coincidencias';
        aviso.innerHTML = '<td colspan="' + columnas + '">' +
            '<i class="fa-regular fa-face-frown d-block fs-3 mb-2"></i>' +
            'Ningún trabajo coincide con esa búsqueda.</td>';

        tbody.appendChild(aviso);
    }

    function aplicar() {
        var tbody = cuerpo();
        if (!tbody) return;

        var todas = filas();
        if (!todas.length) return;

        var columnas = todas[0].children.length;
        var visibles = 0;

        // Reordenar mueve nodos, y mover nodos dispara al observador de más
        // abajo. Se lo apaga mientras dura el trabajo para no entrar en bucle.
        observar(false);

        try {
            var ordenadas = ordenar(todas.slice());

            // Solo se tocan los nodos si el orden cambió de verdad.
            var distinto = ordenadas.some(function (tr, i) {
                return tr !== todas[i];
            });

            if (distinto) {
                ordenadas.forEach(function (tr) {
                    tbody.appendChild(tr);
                });
            }

            ordenadas.forEach(function (tr) {
                var mostrar = pasa(tr);
                tr.hidden = !mostrar;
                if (mostrar) visibles++;
            });

            avisoVacio(tbody, visibles === 0, columnas);
        } finally {
            observar(true);
        }

        if (cuenta) {
            cuenta.textContent = visibles === todas.length
                ? todas.length + (todas.length === 1 ? ' trabajo' : ' trabajos')
                : visibles + ' de ' + todas.length;
        }
    }

    // ---------- Búsqueda ----------

    function leerBusqueda() {
        if (!campo) return;

        if (limpiar) limpiar.hidden = campo.value === '';

        palabras = normalizar(campo.value)
            .split(/\s+/)
            .filter(function (p) { return p.length > 0; });

        aplicar();
    }

    if (campo) {
        var espera = null;

        campo.addEventListener('input', function () {
            if (limpiar) limpiar.hidden = campo.value === '';

            clearTimeout(espera);
            espera = setTimeout(leerBusqueda, 250);
        });

        campo.addEventListener('keydown', function (e) {
            if (e.key === 'Enter') e.preventDefault();
        });
    }

    if (limpiar) {
        limpiar.addEventListener('click', function () {
            campo.value = '';
            limpiar.hidden = true;
            palabras = [];
            aplicar();
            campo.focus();
        });
    }

    [selCategoria, selEstado, selOrden].forEach(function (sel) {
        if (sel) sel.addEventListener('change', aplicar);
    });

    // ---------- Dictado por voz ----------

    (function () {
        if (!botonVoz || !campo) return;

        // Chrome y Edge la traen con prefijo; Firefox todavía no la tiene.
        var Reconocimiento = window.SpeechRecognition || window.webkitSpeechRecognition;
        if (!Reconocimiento) return;

        botonVoz.hidden = false;

        var oyente = new Reconocimiento();
        oyente.lang = 'es-AR';
        oyente.continuous = false;
        oyente.interimResults = true;
        oyente.maxAlternatives = 1;

        var escuchando = false;
        var textoAyuda = campo.placeholder;

        function marcar(activo) {
            escuchando = activo;
            botonVoz.classList.toggle('escuchando', activo);
            botonVoz.setAttribute('aria-label', activo ? 'Detener dictado' : 'Buscar por voz');
        }

        botonVoz.addEventListener('click', function () {
            if (escuchando) {
                oyente.stop();
                return;
            }

            campo.value = '';
            try {
                oyente.start();
            } catch (e) {
                // Un start() seguido de otro tira error; se ignora.
            }
        });

        oyente.addEventListener('start', function () { marcar(true); });
        oyente.addEventListener('end', function () { marcar(false); });

        oyente.addEventListener('result', function (e) {
            var texto = '';

            for (var i = 0; i < e.results.length; i++) {
                texto += e.results[i][0].transcript;
            }

            campo.value = texto.trim();
            leerBusqueda();
        });

        oyente.addEventListener('error', function (e) {
            marcar(false);

            if (e.error === 'not-allowed' || e.error === 'service-not-allowed') {
                campo.placeholder = 'Permití el micrófono para dictar';
                setTimeout(function () { campo.placeholder = textoAyuda; }, 4000);
            }
        });
    })();

    // ---------- Filas nuevas ----------

    // Al guardar o borrar, el panel reemplaza la tabla entera. Se vuelve a
    // aplicar lo que el usuario tenía puesto, sin que tenga que tocar nada.
    var pendiente = null;

    observador = new MutationObserver(function () {
        clearTimeout(pendiente);
        pendiente = setTimeout(aplicar, 60);
    });

    observar(true);
    aplicar();
})();
