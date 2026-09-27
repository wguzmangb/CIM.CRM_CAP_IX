(function () {
    var cuerpo = document.getElementById("cuerpo-renglones");
    if (!cuerpo) return;

    var plantilla = document.getElementById("plantilla-renglon");
    var botonAgregar = document.getElementById("agregar-renglon");

    
    var siguiente = cuerpo.querySelectorAll("tr.renglon").length;

    var moneda = new Intl.NumberFormat("es-MX", {
        minimumFractionDigits: 2,
        maximumFractionDigits: 2
    });

    function numero(campo) {
        var valor = parseFloat(campo && campo.value);
        return isNaN(valor) ? 0 : valor;
    }

    function recalcular() {
        var subtotal = 0;

        cuerpo.querySelectorAll("tr.renglon").forEach(function (fila) {
            var importe = numero(fila.querySelector(".cantidad")) *
                numero(fila.querySelector(".precio"));

            subtotal += importe;
            fila.querySelector(".importe-renglon").textContent = moneda.format(importe);
        });

        var impuesto = Math.round(subtotal * 0.16 * 100) / 100;

        document.getElementById("vista-subtotal").textContent = moneda.format(subtotal);
        document.getElementById("vista-impuesto").textContent = moneda.format(impuesto);
        document.getElementById("vista-total").textContent = moneda.format(subtotal + impuesto);
    }

    function agregar() {
        var fila = plantilla.content.cloneNode(true);

        fila.querySelectorAll("input").forEach(function (campo) {
            campo.name = campo.name.replace("__i__", siguiente);
            if (campo.type === "hidden") campo.value = siguiente;
        });

        cuerpo.appendChild(fila);
        siguiente++;

        var nueva = cuerpo.querySelector("tr.renglon:last-child .descripcion");
        if (nueva) nueva.focus();
    }

    if (botonAgregar) botonAgregar.addEventListener("click", agregar);

    cuerpo.addEventListener("input", function (evento) {
        if (evento.target.classList.contains("cantidad") ||
            evento.target.classList.contains("precio")) {
            recalcular();
        }
    });

    cuerpo.addEventListener("click", function (evento) {
        var boton = evento.target.closest(".btn-quitar");
        if (!boton) return;

        var filas = cuerpo.querySelectorAll("tr.renglon");
        boton.closest("tr.renglon").remove();

       
        if (filas.length === 1) agregar();

        recalcular();
    });

    recalcular();
})();