// Alta y edición de trabajos en un modal, sin salir del panel.
// El formulario se pide por fetch y se envía por XHR, que es lo único que
// da progreso real de subida. Las fotos se ven en miniatura y se pueden
// descartar con la cruz o arrastrándolas al tacho.
(function () {
    var CLAVE_ARCHIVOS = 'archivos';

    var capa = null;      // fondo oscuro
    var cuerpo = null;    // donde va el formulario
    var tacho = null;     // zona de descarte
    var elegidos = [];    // archivos que se van a subir
    var subiendo = false;
    var arrastrando = null;

    // ---------- Armado del modal ----------

    function crear() {
        capa = document.createElement('div');
        capa.className = 'modal-capa';
        capa.innerHTML =
            '<div class="modal-caja" role="dialog" aria-modal="true">' +
            '  <div class="modal-cabecera">' +
            '    <h2 class="modal-titulo"></h2>' +
            '    <button type="button" class="modal-cerrar" aria-label="Cerrar">' +
            '      <i class="fa-solid fa-xmark"></i></button>' +
            '  </div>' +
            '  <div class="modal-cuerpo"></div>' +
            '  <div class="modal-progreso"><div class="modal-progreso-barra"></div></div>' +
            '  <div class="modal-pie">' +
            '    <span class="modal-estado"></span>' +
            '    <button type="button" class="boton boton-claro modal-cancelar">Cancelar</button>' +
            '    <button type="button" class="boton boton-primario modal-guardar">' +
            '      <i class="fa-solid fa-check"></i>Guardar</button>' +
            '  </div>' +
            '  <div class="tacho" aria-hidden="true">' +
            '    <i class="fa-solid fa-trash-can tacho-cerrado"></i>' +
            '    <i class="fa-solid fa-trash-can-arrow-up tacho-abierto"></i>' +
            '    <span class="tacho-texto">Soltá para eliminar</span>' +
            '  </div>' +
            '</div>';

        document.body.appendChild(capa);

        cuerpo = capa.querySelector('.modal-cuerpo');
        tacho = capa.querySelector('.tacho');

        capa.querySelector('.modal-cerrar').addEventListener('click', cerrar);
        capa.querySelector('.modal-cancelar').addEventListener('click', cerrar);
        capa.querySelector('.modal-guardar').addEventListener('click', guardar);

        // A propósito no se cierra al hacer clic afuera: con un formulario
        // largo, un clic al costado borraría todo lo cargado sin aviso.
        // Se sale por la cruz, por Cancelar o con Escape.
        document.addEventListener('keydown', function (e) {
            if (e.key === 'Escape' && capa.classList.contains('abierta')) cerrar();
        });

        conectarTacho();
    }

    function abrir(titulo) {
        if (!capa) crear();

        elegidos = [];
        capa.querySelector('.modal-titulo').textContent = titulo;
        capa.querySelector('.modal-estado').textContent = '';
        progreso(null);

        cuerpo.innerHTML = '<div class="modal-cargando">' +
            '<i class="fa-solid fa-circle-notch fa-spin"></i></div>';

        document.body.classList.add('con-modal');
        capa.classList.add('abierta');
    }

    function cerrar() {
        if (subiendo) {
            Swal.fire(encima({
                icon: 'warning',
                title: 'Hay una subida en curso',
                text: 'Esperá a que termine para no perder los archivos.'
            }));
            return;
        }

        capa.classList.remove('abierta');
        document.body.classList.remove('con-modal');
        elegidos = [];
    }

    function progreso(porcentaje) {
        var barra = capa.querySelector('.modal-progreso');
        var relleno = capa.querySelector('.modal-progreso-barra');

        if (porcentaje === null) {
            barra.classList.remove('visible');
            relleno.style.width = '0%';
            return;
        }

        barra.classList.add('visible');
        relleno.style.width = porcentaje + '%';
    }

    function aviso(icono, titulo) {
        Swal.mixin({
            toast: true, position: 'top-end', showConfirmButton: false,
            timer: 2500, timerProgressBar: true,
            // El modal de edición tiene z-index propio: sin esto el aviso
            // asoma por detrás.
            didOpen: function (popup) {
                popup.parentElement.style.zIndex = '20000';
            }
        }).fire({ icon: icono, title: titulo });
    }

    // ---------- Carga del formulario ----------

    function cargar(url, titulo) {
        abrir(titulo);

        fetch(url, { headers: { 'X-Requested-With': 'XMLHttpRequest' } })
            .then(function (r) {
                if (!r.ok) throw new Error('respuesta ' + r.status);
                return r.text();
            })
            .then(function (html) {
                cuerpo.innerHTML = html;
                conectarArchivos();
                conectarMediosGuardados();
            })
            .catch(function () {
                cuerpo.innerHTML = '<p class="tenue">No se pudo abrir el formulario. ' +
                    'Probá de nuevo.</p>';
            });
    }

    // ---------- Tacho ----------

    // El tacho puede tener pointer-events desactivados por CSS, así que los
    // eventos se escuchan en toda la capa y se compara la posición del cursor
    // contra el recuadro del tacho.
    function sobreTacho(e) {
        if (!tacho.classList.contains('visible')) return false;

        var r = tacho.getBoundingClientRect();
        return e.clientX >= r.left && e.clientX <= r.right
            && e.clientY >= r.top && e.clientY <= r.bottom;
    }

    function conectarTacho() {
        capa.addEventListener('dragover', function (e) {
            if (!arrastrando) return;

            // Sin esto el navegador no permite soltar.
            e.preventDefault();
            tacho.classList.toggle('encima', sobreTacho(e));
        });

        capa.addEventListener('drop', function (e) {
            if (!arrastrando) return;

            e.preventDefault();
            var borrar = sobreTacho(e);
            tacho.classList.remove('encima');

            if (borrar) descartar(arrastrando);
        });
    }

    function mostrarTacho(si) {
        tacho.classList.toggle('visible', si);
        if (!si) tacho.classList.remove('encima');
    }

    // Prepara un elemento para poder arrastrarlo hasta el tacho.
    function hacerArrastrable(elemento) {
        elemento.setAttribute('draggable', 'true');

        elemento.addEventListener('dragstart', function (e) {
            arrastrando = elemento;
            elemento.classList.add('arrastrando');
            mostrarTacho(true);
            // Firefox necesita que se transfiera algo para iniciar el arrastre.
            if (e.dataTransfer) e.dataTransfer.setData('text/plain', 'x');
        });

        elemento.addEventListener('dragend', function () {
            elemento.classList.remove('arrastrando');
            mostrarTacho(false);
            arrastrando = null;
        });
    }

    // Saca un elemento: si todavía no se subió, alcanza con quitarlo de la
    // lista; si ya está guardado, hay que avisarle al servidor.
    function descartar(elemento) {
        if (elemento.classList.contains('previa')) {
            var i = parseInt(elemento.dataset.indice, 10);
            if (!isNaN(i)) {
                elegidos.splice(i, 1);
                dibujarPrevias();
                aviso('success', 'Archivo descartado.');
            }
            return;
        }

        // Un archivo ya guardado se borra del disco del servidor: no hay
        // vuelta atrás, así que siempre se pregunta antes.
        var form = elemento.querySelector('form[data-confirmar]');
        if (form) confirmarBorrado(form, elemento);
    }

    // ---------- Archivos elegidos ----------

    function pesoLegible(bytes) {
        if (bytes < 1024 * 1024) return Math.round(bytes / 1024) + ' KB';
        return (bytes / 1024 / 1024).toFixed(1) + ' MB';
    }

    function conectarArchivos() {
        var campo = cuerpo.querySelector('#archivos');
        var zona = cuerpo.querySelector('#zonaArchivos');
        if (!campo) return;

        campo.addEventListener('change', function () {
            Array.prototype.forEach.call(campo.files, function (f) {
                elegidos.push(f);
            });
            campo.value = '';   // permite volver a elegir el mismo archivo
            dibujarPrevias();
        });

        if (!zona) return;

        ['dragenter', 'dragover'].forEach(function (ev) {
            zona.addEventListener(ev, function (e) {
                // Si lo que se arrastra es una miniatura, la zona no reacciona.
                if (arrastrando) return;
                e.preventDefault();
                zona.classList.add('encima');
            });
        });

        ['dragleave', 'drop'].forEach(function (ev) {
            zona.addEventListener(ev, function (e) {
                e.preventDefault();
                zona.classList.remove('encima');
            });
        });

        zona.addEventListener('drop', function (e) {
            if (arrastrando) return;
            Array.prototype.forEach.call(e.dataTransfer.files, function (f) {
                elegidos.push(f);
            });
            dibujarPrevias();
        });
    }

    function dibujarPrevias() {
        var caja = cuerpo.querySelector('#previas');
        if (!caja) return;

        caja.innerHTML = '';

        elegidos.forEach(function (archivo, indice) {
            var item = document.createElement('div');
            item.className = 'previa';
            item.dataset.indice = indice;
            item.title = archivo.name;

            if (archivo.type.indexOf('image/') === 0) {
                var img = document.createElement('img');
                img.src = URL.createObjectURL(archivo);
                img.onload = function () { URL.revokeObjectURL(img.src); };
                item.appendChild(img);
            } else {
                var vid = document.createElement('video');
                vid.src = URL.createObjectURL(archivo);
                vid.muted = true;
                vid.preload = 'metadata';
                item.appendChild(vid);

                var marca = document.createElement('span');
                marca.className = 'previa-video';
                marca.innerHTML = '<i class="fa-solid fa-play"></i>';
                item.appendChild(marca);
            }

            var pie = document.createElement('span');
            pie.className = 'previa-peso';
            pie.textContent = pesoLegible(archivo.size);
            item.appendChild(pie);

            var quitar = document.createElement('button');
            quitar.type = 'button';
            quitar.className = 'quitar';
            quitar.title = 'Descartar';
            quitar.innerHTML = '<i class="fa-solid fa-xmark"></i>';
            quitar.addEventListener('click', function () {
                elegidos.splice(indice, 1);
                dibujarPrevias();
            });
            item.appendChild(quitar);

            hacerArrastrable(item);
            caja.appendChild(item);
        });

        if (elegidos.length) {
            var total = elegidos.reduce(function (s, a) { return s + a.size; }, 0);
            var resumen = document.createElement('p');
            resumen.className = 'previas-resumen ayuda chico';
            resumen.textContent = elegidos.length +
                (elegidos.length === 1 ? ' archivo' : ' archivos') +
                ' · ' + pesoLegible(total) + ' en total · arrastrá al tacho para descartar';
            caja.appendChild(resumen);
        }
    }

    // ---------- Archivos ya guardados ----------

    function conectarMediosGuardados() {
        cuerpo.querySelectorAll('.medio').forEach(function (medio) {
            hacerArrastrable(medio);

            var form = medio.querySelector('form[data-confirmar]');
            if (!form) return;

            form.addEventListener('submit', function (e) {
                e.preventDefault();
                confirmarBorrado(form, medio);
            });
        });
    }

    // Todo diálogo que salga estando el modal abierto necesita dos cosas:
    // quedar por encima de él, y no cerrarse por un clic al costado.
    function encima(opciones) {
        opciones.allowOutsideClick = false;

        opciones.didOpen = function (popup) {
            popup.parentElement.style.zIndex = '20000';
        };

        return opciones;
    }

    function confirmarBorrado(form, medio) {
        Swal.fire({
            title: form.dataset.confirmar || '¿Eliminar este archivo?',
            text: 'Se borra del servidor y no se puede recuperar.',
            icon: 'warning',
            showCancelButton: true,
            confirmButtonText: 'Sí, eliminar',
            cancelButtonText: 'Cancelar',
            confirmButtonColor: '#b5453a',
            reverseButtons: true,
            focusCancel: true,
            allowOutsideClick: false,
            // El modal de edición tiene z-index propio: sin esto la pregunta
            // quedaría tapada detrás.
            didOpen: function (popup) {
                popup.parentElement.style.zIndex = '20000';
            }
        }).then(function (r) {
            if (r.isConfirmed) enviarBorrado(form, medio);
        });
    }

    function enviarBorrado(form, medio) {
        fetch(form.action, {
            method: 'POST',
            body: new FormData(form),
            headers: { 'X-Requested-With': 'XMLHttpRequest' }
        })
            .then(function (x) { return x.json(); })
            .then(function (x) {
                if (!x.ok) {
                    Swal.fire(encima({ icon: 'error', title: x.error || 'No se pudo eliminar.' }));
                    return;
                }

                medio.style.transition = 'opacity .3s, transform .3s';
                medio.style.opacity = '0';
                medio.style.transform = 'scale(.8)';

                setTimeout(function () {
                    var padre = medio.parentElement;
                    medio.remove();

                    if (padre && padre.children.length === 0 && padre.dataset.vacio) {
                        var p = document.createElement('p');
                        p.className = 'tenue chico sin-margen';
                        p.textContent = padre.dataset.vacio;
                        padre.replaceWith(p);
                    }
                }, 300);

                aviso('success', 'Archivo eliminado.');
            })
            .catch(function () {
                Swal.fire(encima({ icon: 'error', title: 'Falló la conexión.' }));
            });
    }

    // ---------- Guardado ----------

    function guardar() {
        var form = cuerpo.querySelector('#formTrabajo');
        if (!form || subiendo) return;

        var datos = new FormData(form);
        datos.delete(CLAVE_ARCHIVOS);
        elegidos.forEach(function (a) { datos.append(CLAVE_ARCHIVOS, a); });

        subiendo = true;
        capa.querySelector('.modal-guardar').disabled = true;
        capa.querySelector('.modal-cancelar').disabled = true;
        capa.querySelector('.modal-estado').textContent = elegidos.length
            ? 'Subiendo archivos...' : 'Guardando...';
        progreso(0);

        var xhr = new XMLHttpRequest();
        xhr.open('POST', form.action);
        xhr.setRequestHeader('X-Requested-With', 'XMLHttpRequest');

        xhr.upload.addEventListener('progress', function (e) {
            if (!e.lengthComputable) return;
            var pct = Math.round(e.loaded / e.total * 100);
            progreso(pct);
            if (pct >= 100) {
                capa.querySelector('.modal-estado').textContent = 'Procesando...';
            }
        });

        xhr.addEventListener('load', function () {
            liberar();

            var r;
            try {
                r = JSON.parse(xhr.responseText);
            } catch (err) {
                terminarConError('El servidor respondió algo inesperado.');
                return;
            }

            if (!r.ok) {
                terminarConError(r.error || 'No se pudo guardar.');
                return;
            }

            capa.classList.remove('abierta');
            document.body.classList.remove('con-modal');
            elegidos = [];

            if (r.aviso) {
                Swal.fire(encima({ icon: 'warning', title: 'Guardado con avisos', text: r.aviso }));
            } else {
                aviso('success', 'Trabajo guardado.');
            }

            refrescarPanel();
        });

        xhr.addEventListener('error', function () {
            liberar();
            terminarConError('Falló la conexión. Los archivos no se subieron.');
        });

        xhr.send(datos);
    }

    function liberar() {
        subiendo = false;
        capa.querySelector('.modal-guardar').disabled = false;
        capa.querySelector('.modal-cancelar').disabled = false;
    }

    function terminarConError(mensaje) {
        progreso(null);
        capa.querySelector('.modal-estado').textContent = '';
        Swal.fire(encima({ icon: 'error', title: 'No se guardó', text: mensaje }));
    }

    // Trae el panel de nuevo y reemplaza solo la parte que cambió.
    function refrescarPanel() {
        fetch(window.location.href, { headers: { 'X-Requested-With': 'XMLHttpRequest' } })
            .then(function (r) { return r.text(); })
            .then(function (html) {
                var doc = new DOMParser().parseFromString(html, 'text/html');
                var reemplazado = false;

                ['#panelResumen', '#panelContenido'].forEach(function (sel) {
                    var nuevo = doc.querySelector(sel);
                    var viejo = document.querySelector(sel);

                    if (nuevo && viejo) {
                        viejo.replaceWith(nuevo);
                        reemplazado = true;
                    }
                });

                // Si el panel no tiene esas zonas, recargar es preferible a
                // dejar la tabla mostrando datos viejos.
                if (!reemplazado) {
                    window.location.reload();
                    return;
                }

                conectarBotones();
            })
            .catch(function () {
                window.location.reload();
            });
    }

    // ---------- Enganche con el panel ----------

    function conectarBotones() {
        document.querySelectorAll('a[href*="/Admin/NuevoTrabajo"]').forEach(function (a) {
            if (a.dataset.modal) return;
            a.dataset.modal = '1';
            a.addEventListener('click', function (e) {
                e.preventDefault();
                cargar(a.href, 'Nueva publicación');
            });
        });

        document.querySelectorAll('a[href*="/Admin/EditarTrabajo"]').forEach(function (a) {
            if (a.dataset.modal) return;
            a.dataset.modal = '1';
            a.addEventListener('click', function (e) {
                e.preventDefault();
                cargar(a.href, 'Editar publicación');
            });
        });
    }

    conectarBotones();
})();