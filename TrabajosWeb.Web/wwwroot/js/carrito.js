// Carrito de Gato Negro. Vive en localStorage: sin cuenta, sin sesión.
// Al confirmar, el pedido se guarda en la base (con los precios recalculados
// del lado del servidor) y avisa al panel por notificación push. No se usa
// WhatsApp en este flujo.
document.addEventListener("DOMContentLoaded", function () {
    actualizarContadorCarrito();
    inicializarEventosGlobales();

    var btnAbrirCarrito = document.getElementById('abrirModalCarrito');
    if (btnAbrirCarrito) {
        btnAbrirCarrito.addEventListener('click', function (e) {
            e.preventDefault();
            abrirModalCarritoPrincipal();
        });
    }

    var btnVaciar = document.getElementById('btnVaciarCarrito');
    if (btnVaciar) {
        btnVaciar.addEventListener('click', function () {
            Swal.fire({
                title: '¿Vaciar carrito?',
                text: 'Se eliminarán todos los productos seleccionados.',
                icon: 'warning',
                showCancelButton: true,
                confirmButtonColor: '#d33',
                cancelButtonColor: '#3085d6',
                confirmButtonText: 'Sí, vaciar',
                cancelButtonText: 'Cancelar',
                allowOutsideClick: false
            }).then(function (result) {
                if (result.isConfirmed) {
                    localStorage.removeItem('carrito');
                    renderizarCarritoModal();
                    actualizarContadorCarrito();
                }
            });
        });
    }

    var btnFinalizar = document.getElementById('btnFinalizarPedido');
    if (btnFinalizar) {
        btnFinalizar.addEventListener('click', function () {
            confirmarPedido();
        });
    }

    var modalEl = document.getElementById('modalCarrito');
    if (modalEl) {
        modalEl.addEventListener('hide.bs.modal', function () {
            var elementoActivo = document.activeElement;
            if (elementoActivo && modalEl.contains(elementoActivo)) {
                elementoActivo.blur();
            }
        });
    }
});

// ---------- Inicialización global ----------

function abrirModalCarritoPrincipal() {
    renderizarCarritoModal();

    // focus:false es necesario: Bootstrap retiene el foco dentro del
    // modal y después no se puede escribir en el cuadro de SweetAlert.
    var modalEl = document.getElementById('modalCarrito');
    if (modalEl) {
        var modal = bootstrap.Modal.getInstance(modalEl) || new bootstrap.Modal(modalEl, {
            focus: false
        });
        modal.show();
    }
}

function inicializarEventosGlobales() {
    // Botón móvil que abre el mismo modal
    var btnMobile = document.getElementById("abrirModalCarritoMobile");
    var modalElement = document.getElementById("modalCarrito");

    if (btnMobile && modalElement) {
        btnMobile.addEventListener("click", function (e) {
            e.preventDefault();
            abrirModalCarritoPrincipal();
        });
    }
}

// ---------- Confirmación ----------

function confirmarPedido() {
    var carrito = JSON.parse(localStorage.getItem('carrito')) || [];
    if (carrito.length === 0) return;

    Swal.fire({
        title: 'Últimos datos',
        html:
            '<input id="pedidoNombre" class="swal2-input" placeholder="Tu nombre" autocomplete="name">' +
            '<input id="pedidoTelefono" class="swal2-input" placeholder="Tu teléfono" autocomplete="tel">' +
            '<textarea id="pedidoNota" class="swal2-textarea" placeholder="Algo que quieras aclarar (opcional)"></textarea>',
        confirmButtonText: 'Confirmar pedido',
        cancelButtonText: 'Volver',
        showCancelButton: true,
        reverseButtons: true,
        allowOutsideClick: false,
        focusConfirm: false,
        didOpen: function () {
            var primero = document.getElementById('pedidoNombre');
            if (primero) primero.focus();
        },
        preConfirm: function () {
            var nombre = document.getElementById('pedidoNombre').value.trim();
            var telefono = document.getElementById('pedidoTelefono').value.trim();

            if (!nombre) {
                Swal.showValidationMessage('Necesitamos tu nombre');
                return false;
            }

            if (!telefono) {
                Swal.showValidationMessage('Necesitamos un teléfono para responderte');
                return false;
            }

            return {
                nombre: nombre,
                telefono: telefono,
                nota: document.getElementById('pedidoNota').value.trim()
            };
        }
    }).then(function (r) {
        if (!r.isConfirmed) return;
        enviarPedido(r.value, carrito);
    });
}

function enviarPedido(datos, carrito) {
    var cuerpo = {
        nombre: datos.nombre,
        telefono: datos.telefono,
        nota: datos.nota || null,
        items: carrito.map(function (i) {
            return {
                trabajoId: i.id,
                cantidad: parseInt(i.cantidad, 10) || 1
            };
        })
    };

    Swal.fire({
        title: 'Registrando tu pedido…',
        allowOutsideClick: false,
        allowEscapeKey: false,
        didOpen: function () { Swal.showLoading(); }
    });

    fetch('/pedidos/confirmar', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'X-Requested-With': 'XMLHttpRequest'
        },
        body: JSON.stringify(cuerpo)
    })
        .then(function (r) { return r.json(); })
        .then(function (r) {
            if (!r.ok) {
                Swal.fire({
                    icon: 'error',
                    title: 'No se pudo registrar',
                    text: r.error || 'Probá de nuevo en un momento.',
                    allowOutsideClick: false
                });
                return;
            }

            limpiarDespuesDeEnviar();

            Swal.fire({
                icon: 'success',
                title: 'Pedido N° ' + r.numero,
                text: 'Ya avisamos al salón. Te van a contactar para coordinar pago y entrega.',
                allowOutsideClick: false
            });
        })
        .catch(function () {
            Swal.fire({
                icon: 'error',
                title: 'Falló la conexión',
                text: 'No pudimos registrar el pedido. Probá de nuevo.',
                allowOutsideClick: false
            });
        });
}

function limpiarDespuesDeEnviar() {
    localStorage.removeItem('carrito');
    actualizarContadorCarrito();
    renderizarCarritoModal();

    var modalEl = document.getElementById('modalCarrito');
    if (modalEl) {
        // Quitamos el foco de cualquier elemento interno antes de que se oculte
        var elementoActivo = document.activeElement;
        if (elementoActivo && modalEl.contains(elementoActivo)) {
            elementoActivo.blur();
        }

        var modalInstance = bootstrap.Modal.getInstance(modalEl);
        if (modalInstance) {
            modalInstance.hide();
        }
    }
}

function formatear(n) {
    return (Number(n) || 0).toLocaleString('es-AR');
}

// ---------- Pantalla y renderizado ----------

function actualizarContadorCarrito() {
    try {
        var carrito = JSON.parse(localStorage.getItem('carrito')) || [];
        var totalItems = carrito.reduce(function (sum, item) {
            return sum + (parseInt(item.cantidad, 10) || 1);
        }, 0);

        // Escritorio y barra inferior de mobile se actualizan juntos acá:
        // nada de esperar a un MutationObserver que puede arrancar tarde.
        ['contadorCarrito', 'contadorCarritoMobile'].forEach(function (id) {
            var contadorEl = document.getElementById(id);
            if (!contadorEl) return;

            contadorEl.textContent = totalItems;
            contadorEl.style.display = totalItems > 0 ? 'inline-block' : 'none';
        });
    } catch (e) {
        console.error("Error al actualizar el contador del carrito", e);
    }
}

function renderizarCarritoModal() {
    var carrito = JSON.parse(localStorage.getItem('carrito')) || [];
    var tbody = document.getElementById('tablaCarritoCuerpo');
    var divVacio = document.getElementById('carritoContenidoVacio');
    var divLleno = document.getElementById('carritoContenidoLleno');
    var contenedorTotal = document.getElementById('contenedorTotal');
    var montoTotalEl = document.getElementById('carritoTotalMonto');

    if (!tbody) return;

    tbody.innerHTML = '';

    if (carrito.length === 0) {
        if (divVacio) divVacio.style.display = 'block';
        if (divLleno) divLleno.style.display = 'none';
        if (contenedorTotal) contenedorTotal.style.display = 'none';
        if (montoTotalEl) montoTotalEl.textContent = '$0';
        return;
    }

    if (divVacio) divVacio.style.display = 'none';
    if (divLleno) divLleno.style.display = 'block';
    if (contenedorTotal) contenedorTotal.style.display = 'flex';

    var totalGeneral = 0;

    carrito.forEach(function (item, index) {
        var subtotal = item.precio * item.cantidad;
        totalGeneral += subtotal;

        var tr = document.createElement('tr');
        tr.innerHTML =
            '<td>' +
            '<div class="d-flex align-items-center">' +
            (item.imagen ? '<img src="' + item.imagen + '" alt="" style="width: 45px; height: 45px; object-fit: cover;" class="rounded me-2">' : '') +
            '<span class="fw-bold">' + item.titulo + '</span>' +
            '</div>' +
            '</td>' +
            '<td>$' + formatear(item.precio) + '</td>' +
            '<td>' +
            '<input type="number" min="1" max="99" value="' + item.cantidad + '" class="form-control form-control-sm text-center input-cantidad-item" style="width: 60px;" data-index="' + index + '">' +
            '</td>' +
            '<td class="fw-bold">$' + formatear(subtotal) + '</td>' +
            '<td>' +
            '<button type="button" class="btn btn-sm btn-outline-danger btn-eliminar-item" data-index="' + index + '">' +
            '<i class="fa-solid fa-xmark"></i>' +
            '</button>' +
            '</td>';

        tbody.appendChild(tr);
    });

    if (montoTotalEl) {
        montoTotalEl.textContent = '$' + formatear(totalGeneral);
    }

    // Eventos locales que se reconstruyen cada vez que la tabla se redibuja
    document.querySelectorAll('.input-cantidad-item').forEach(function (input) {
        input.addEventListener('change', function () {
            var idx = parseInt(this.dataset.index, 10);
            var nuevaCant = parseInt(this.value, 10) || 1;
            if (nuevaCant < 1) nuevaCant = 1;
            if (nuevaCant > 99) nuevaCant = 99;

            carrito[idx].cantidad = nuevaCant;
            localStorage.setItem('carrito', JSON.stringify(carrito));
            renderizarCarritoModal();
            actualizarContadorCarrito();
        });
    });

    document.querySelectorAll('.btn-eliminar-item').forEach(function (btn) {
        btn.addEventListener('click', function () {
            var idx = parseInt(this.dataset.index, 10);
            carrito.splice(idx, 1);
            localStorage.setItem('carrito', JSON.stringify(carrito));
            renderizarCarritoModal();
            actualizarContadorCarrito();
        });
    });
}
