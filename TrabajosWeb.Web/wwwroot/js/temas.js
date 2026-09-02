/**
 * Selector de estilo visual.
 * El tema se guarda en localStorage y se aplica sobre <html data-tema="...">.
 * El CSS hace el resto: cada tema redefine las mismas variables.
 */
(function () {
    'use strict';

    var CLAVE = 'tema';
    var VALIDOS = ['claro', 'oscuro', 'bloques'];
    var raiz = document.documentElement;

    function leerGuardado() {
        try {
            var valor = localStorage.getItem(CLAVE);
            return VALIDOS.indexOf(valor) !== -1 ? valor : 'claro';
        } catch (e) {
            return 'claro';
        }
    }

    function marcarActivo(tema) {
        document.querySelectorAll('[data-tema-valor]').forEach(function (boton) {
            boton.setAttribute('aria-pressed', boton.dataset.temaValor === tema);
        });
    }

    function aplicar(tema, persistir) {
        raiz.setAttribute('data-tema', tema);
        marcarActivo(tema);

        if (persistir) {
            try {
                localStorage.setItem(CLAVE, tema);
            } catch (e) { }
        }
    }

    document.addEventListener('DOMContentLoaded', function () {
        aplicar(leerGuardado(), false);

        document.querySelectorAll('[data-tema-valor]').forEach(function (boton) {
            boton.addEventListener('click', function () {
                aplicar(boton.dataset.temaValor, true);
            });
        });
    });
})();