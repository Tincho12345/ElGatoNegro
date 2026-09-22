document.addEventListener("DOMContentLoaded", function () {
    // 1. Lógica del Visor de Imágenes
    var principal = document.getElementById('visorPrincipal');
    if (principal) {
        var capas = [
            document.getElementById('visorCapaA'),
            document.getElementById('visorCapaB')
        ];

        var video = document.getElementById('visorVideo');
        var expandir = document.getElementById('visorExpandir');
        var minis = document.querySelectorAll('.visor-mini');
        var anclas = document.querySelectorAll('a[data-lightbox][data-url]');

        var actual = 0;
        var indiceActual = 0;
        var enCurso = false;
        var urlActual = capas[0] && !capas[0].hidden ? capas[0].getAttribute('src') : '';

        function precargar(url) {
            return new Promise(function (listo) {
                var previa = new Image();
                previa.onload = listo;
                previa.onerror = listo;
                previa.src = url;
                setTimeout(listo, 1200);
            });
        }

        function cruzar(url, alt, haciaAbajo) {
            var sale = capas[actual];
            var entra = capas[1 - actual];

            entra.src = url;
            entra.alt = alt || '';
            entra.hidden = false;

            entra.classList.remove('visible', 'saliendo');
            entra.classList.toggle('desde-arriba', !haciaAbajo);

            requestAnimationFrame(function () {
                requestAnimationFrame(function () {
                    entra.classList.add('visible');
                    entra.classList.remove('desde-arriba');

                    sale.classList.remove('visible');
                    sale.classList.add('saliendo');
                });
            });

            actual = 1 - actual;

            setTimeout(function () {
                sale.classList.remove('saliendo');
                sale.hidden = true;
                enCurso = false;
            }, 560);
        }

        function mostrar(mini) {
            if (enCurso || mini.classList.contains('activa')) return;

            var indice = parseInt(mini.dataset.indice, 10) || 0;
            var haciaAbajo = indice > indiceActual;
            indiceActual = indice;

            minis.forEach(function (otra) {
                otra.classList.toggle('activa', otra === mini);
                otra.setAttribute('aria-selected', otra === mini);
            });

            video.pause();

            var url = mini.dataset.url;

            if (mini.dataset.tipo !== 'imagen') {
                capas.forEach(function (c) {
                    c.classList.remove('visible', 'saliendo', 'desde-arriba');
                    c.hidden = true;
                });

                video.src = url;
                video.hidden = false;
                if (expandir) expandir.hidden = true;
                return;
            }

            video.hidden = true;
            urlActual = url;
            if (expandir) expandir.hidden = false;

            enCurso = true;

            precargar(url).then(function () {
                cruzar(url, mini.dataset.alt, haciaAbajo);
            });
        }

        minis.forEach(function (mini) {
            mini.addEventListener('click', function () { mostrar(mini); });
        });

        function abrirLupa() {
            for (var i = 0; i < anclas.length; i++) {
                if (anclas[i].dataset.url === urlActual) {
                    anclas[i].click();
                    return;
                }
            }
            if (anclas.length) anclas[0].click();
        }

        if (expandir) {
            expandir.addEventListener('click', abrirLupa);
        }

        capas.forEach(function (capa) {
            capa.addEventListener('click', function () {
                if (!capa.hidden) abrirLupa();
            });
        });
    }

    // 2. Agregar al carrito. Gato Negro no maneja talle ni color: un
    // trabajo es una sola línea con cantidad.
    var panel = document.getElementById('panelDetalleProducto');
    var btnAgregar = document.getElementById('btnAgregarCarrito');

    if (panel && btnAgregar) {
        btnAgregar.addEventListener('click', function () {
            var productoId = panel.dataset.productoId;
            var titulo = panel.dataset.productoTitulo;
            var precio = parseFloat(panel.dataset.productoPrecio) || 0;
            var imagen = panel.dataset.productoImagen;

            var inputCantidad = document.getElementById('inputCantidad');
            var cantidad = parseInt(inputCantidad ? inputCantidad.value : '1', 10) || 1;

            var itemCar = {
                id: productoId,
                titulo: titulo,
                precio: precio,
                cantidad: cantidad,
                imagen: imagen
            };

            var carrito = JSON.parse(localStorage.getItem('carrito')) || [];

            var index = carrito.findIndex(function (i) { return i.id === productoId; });
            if (index > -1) {
                carrito[index].cantidad += cantidad;
            } else {
                carrito.push(itemCar);
            }

            localStorage.setItem('carrito', JSON.stringify(carrito));

            if (typeof actualizarContadorCarrito === 'function') {
                actualizarContadorCarrito();
            }

            Swal.fire({
                icon: 'success',
                title: '¡Agregado al carrito!',
                text: titulo + ' se añadió correctamente.',
                showConfirmButton: false,
                timer: 2000
            });
        });
    }
});
