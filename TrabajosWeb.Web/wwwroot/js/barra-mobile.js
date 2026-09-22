/* ====================================================
    📱 Barra inferior mobile — ocultar al bajar

    Al bajar por la página la barra se va, al subir
    vuelve. En una pantalla de celular esos 56px son
    mucho, y mientras se lee no hacen falta.
==================================================== */
(function () {

    const barra = document.querySelector('.mobile-bottom-nav');
    if (!barra) return;

    const CLASE_OCULTA = 'mobile-bottom-nav--oculta';

    /**
     * Movimiento mínimo para reaccionar. Sin esto la barra tiembla:
     * el scroll llega en fracciones de píxel y cualquier temblor del
     * dedo la haría entrar y salir sola.
     */
    const UMBRAL = 8;

    /** Arriba de todo siempre visible: ahí nadie está leyendo todavía. */
    const ZONA_SEGURA = 80;

    let ultimaY = window.scrollY;
    let oculta = false;
    let pendiente = false;

    function aplicar(debeOcultarse) {

        if (debeOcultarse === oculta) return;

        oculta = debeOcultarse;
        barra.classList.toggle(CLASE_OCULTA, oculta);
    }

    function evaluar() {

        pendiente = false;

        // En iOS el scroll pasa de largo los extremos y devuelve
        // valores negativos o mayores al alto real
        const y = Math.max(0, window.scrollY);
        const delta = y - ultimaY;

        if (Math.abs(delta) < UMBRAL) return;

        ultimaY = y;

        const doc = document.documentElement;
        const finDePagina = y + window.innerHeight >= doc.scrollHeight - 2;

        // Al final de la página también se muestra: es donde suele
        // estar el pie y donde la persona busca a dónde ir ahora
        if (y <= ZONA_SEGURA || finDePagina) {
            aplicar(false);
            return;
        }

        aplicar(delta > 0);
    }

    /**
     * El cálculo se hace una vez por cuadro.
     *
     * El evento de scroll se dispara muchas más veces que las que el
     * navegador dibuja: sin esto se estarían haciendo cuentas que
     * nadie llega a ver.
     */
    function alScrollear() {

        if (pendiente) return;

        pendiente = true;
        requestAnimationFrame(evaluar);
    }

    window.addEventListener('scroll', alScrollear, { passive: true });

    // Si alguien llega a la barra con el tabulador tiene que verla
    barra.addEventListener('focusin', () => aplicar(false));

    // Volver de otra pestaña con la barra escondida deja una pantalla
    // sin navegación hasta que la persona adivine que debe subir
    document.addEventListener('visibilitychange', () => {
        if (!document.hidden) aplicar(false);
    });

})();
