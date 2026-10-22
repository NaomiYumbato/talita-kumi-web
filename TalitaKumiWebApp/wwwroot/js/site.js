// ========================================================
// TALITA KUMI · CLIENT LOGIC & RESERVATION SYNC
// ========================================================

let cotizacionActiva = null;

// Preseleccionar servicio desde las tarjetas y enfocar formulario
function preselectService(serviceName) {
    const select = document.getElementById('serviceSelect');
    if (select) {
        for (let i = 0; i < select.options.length; i++) {
            if (select.options[i].value === serviceName) {
                select.selectedIndex = i;
                actualizarAlertaNailHelper();
                break;
            }
        }
    }
}

// Aplicar cotización proveniente del Chatbot al formulario web
window.aplicarCotizacionAFormulario = function(cotizacion) {
    cotizacionActiva = cotizacion;

    const select = document.getElementById('serviceSelect');
    if (select) {
        // Encontrar la técnica que coincida
        let valorDestino = 'Soft Gel Tips';
        if (cotizacion.resumenServicio.includes('Semipermanente')) valorDestino = 'Manicura Semipermanente';
        else if (cotizacion.resumenServicio.includes('Kapping')) valorDestino = 'Kapping Gel';
        else if (cotizacion.resumenServicio.includes('Esculpidas')) valorDestino = 'Uñas Esculpidas';
        else if (cotizacion.resumenServicio.includes('Soft Gel')) valorDestino = 'Soft Gel Tips';

        for (let i = 0; i < select.options.length; i++) {
            if (select.options[i].value === valorDestino) {
                select.selectedIndex = i;
                break;
            }
        }
    }

    // Actualizar y mostrar el banner de cotización
    const banner = document.getElementById('cotizacionBanner');
    const titulo = document.getElementById('cotizacionTitulo');
    const precio = document.getElementById('cotizacionPrecio');
    const duracion = document.getElementById('cotizacionDuracion');
    const detalles = document.getElementById('cotizacionDetallesTexto');

    if (banner && titulo && precio && duracion) {
        titulo.textContent = cotizacion.resumenServicio;
        precio.textContent = cotizacion.precioFormateado;
        duracion.innerHTML = `<i class="bi bi-clock me-1"></i>${cotizacion.duracionFormateada}`;
        if (detalles && cotizacion.detalles) {
            detalles.textContent = `Personalización: ${cotizacion.detalles.join(' · ')}`;
        }
        banner.classList.remove('d-none');
    }

    // Ocultar alerta de sugerencia ya que ya cotizó
    const alertHelper = document.getElementById('nailHelperAlert');
    if (alertHelper) alertHelper.classList.add('d-none');

    // Desplazar suavemente hacia el formulario
    const seccionReserva = document.getElementById('reserva');
    if (seccionReserva) {
        seccionReserva.scrollIntoView({ behavior: 'smooth', block: 'start' });
    }
};

// Quitar presupuesto activo y restaurar selector normal
function quitarCotizacionActiva() {
    cotizacionActiva = null;
    const banner = document.getElementById('cotizacionBanner');
    if (banner) banner.classList.add('d-none');
    actualizarAlertaNailHelper();
}

// Mostrar u ocultar sugerencia de cotizador en base al servicio seleccionado
function actualizarAlertaNailHelper() {
    const select = document.getElementById('serviceSelect');
    const helper = document.getElementById('nailHelperAlert');
    if (!select || !helper) return;

    if (cotizacionActiva) {
        helper.classList.add('d-none');
        return;
    }

    const selectedOption = select.options[select.selectedIndex];
    if (selectedOption && selectedOption.getAttribute('data-is-nail') === 'true') {
        helper.classList.remove('d-none');
    } else {
        helper.classList.add('d-none');
    }
}

// Manejar el envío del formulario de reserva con guardado en API y redirección a WhatsApp
async function handleBookingSubmit(event) {
    event.preventDefault();

    const name = document.getElementById('clientName').value.trim();
    const phone = document.getElementById('clientPhone').value.trim();
    const select = document.getElementById('serviceSelect');
    const selectedOption = select.options[select.selectedIndex];
    const date = document.getElementById('bookingDate').value;
    const time = document.getElementById('bookingTime').value;
    const message = document.getElementById('clientMessage').value.trim();

    if (!name || !phone || !select.value || !date || !time) {
        alert('Por favor, completa todos los campos obligatorios.');
        return;
    }

    let servicioFinal = selectedOption.value;
    let precioFinal = parseFloat(selectedOption.getAttribute('data-base-price')) || 45.0;
    let duracionFinal = parseInt(selectedOption.getAttribute('data-duration')) || 60;
    let notasFinales = message;

    if (cotizacionActiva) {
        servicioFinal = cotizacionActiva.resumenServicio;
        precioFinal = cotizacionActiva.precioTotal;
        duracionFinal = cotizacionActiva.duracionMinutos;
        const resumenCotizacion = cotizacionActiva.detalles ? `[Detalle Bot: ${cotizacionActiva.detalles.join(', ')}]` : '';
        notasFinales = message ? `${message} ${resumenCotizacion}` : resumenCotizacion;
    }

    const submitBtn = event.target.querySelector('button[type="submit"]');
    const originalBtnHtml = submitBtn ? submitBtn.innerHTML : '';
    if (submitBtn) {
        submitBtn.disabled = true;
        submitBtn.innerHTML = '<span class="spinner-border spinner-border-sm me-2" role="status" aria-hidden="true"></span>Guardando reserva...';
    }

    // 1. Guardar en Base de Datos (SQL Server) vía API REST
    try {
        await fetch('/api/reservas', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                nombre: name,
                telefono: phone,
                servicio: servicioFinal,
                precioEstimado: precioFinal,
                duracionMinutos: duracionFinal,
                fecha: date,
                horario: time,
                notas: notasFinales
            })
        });
    } catch (err) {
        console.warn('No se pudo sincronizar con la base de datos en segundo plano:', err);
    } finally {
        if (submitBtn) {
            submitBtn.disabled = false;
            submitBtn.innerHTML = originalBtnHtml;
        }
    }

    // 2. Construir mensaje estructurado para WhatsApp (Perú +51)
    const studioWhatsApp = '51911345678';
    let waText = `¡Hola Talita Kumi! Deseo solicitar una reserva:%0A%0A` +
        `🌸 *Nombre:* ${encodeURIComponent(name)}%0A` +
        `📱 *WhatsApp:* ${encodeURIComponent(phone)}%0A` +
        `💅 *Servicio:* ${encodeURIComponent(servicioFinal)}%0A` +
        `💰 *Precio Estimado:* S/. ${precioFinal.toFixed(2)}%0A` +
        `⏱️ *Duración aprox.:* ${duracionFinal} min%0A` +
        `📅 *Fecha:* ${encodeURIComponent(date)}%0A` +
        `⏰ *Horario:* ${encodeURIComponent(time)}`;

    if (message) {
        waText += `%0A📝 *Nota adicional:* ${encodeURIComponent(message)}`;
    }

    if (cotizacionActiva && cotizacionActiva.detalles) {
        waText += `%0A✨ *Personalización:* ${encodeURIComponent(cotizacionActiva.detalles.join(', '))}`;
    }

    const waUrl = `https://wa.me/${studioWhatsApp}?text=${waText}`;

    // 3. Mostrar feedback visual en la interfaz
    const successCard = document.getElementById('bookingSuccess');
    const bookingForm = document.getElementById('bookingForm');
    const btnSendWhatsApp = document.getElementById('btnSendWhatsApp');

    if (btnSendWhatsApp) {
        btnSendWhatsApp.href = waUrl;
    }

    if (bookingForm && successCard) {
        bookingForm.classList.add('d-none');
        successCard.classList.remove('d-none');
        successCard.scrollIntoView({ behavior: 'smooth', block: 'center' });
    }

    // Abrir WhatsApp automáticamente en una nueva pestaña después de 1 segundo
    setTimeout(() => {
        window.open(waUrl, '_blank');
    }, 1000);
}

// Inicialización de escuchadores de eventos
document.addEventListener('DOMContentLoaded', () => {
    // Fecha mínima al día actual
    const dateInput = document.getElementById('bookingDate');
    if (dateInput) {
        const today = new Date().toISOString().split('T')[0];
        dateInput.min = today;
    }

    // Escuchador para selector de servicios (mostrar alerta de cotizador interactivo)
    const select = document.getElementById('serviceSelect');
    if (select) {
        select.addEventListener('change', actualizarAlertaNailHelper);
    }

    // Botón para descartar presupuesto del chatbot
    const btnQuitar = document.getElementById('btnQuitarCotizacion');
    if (btnQuitar) {
        btnQuitar.addEventListener('click', quitarCotizacionActiva);
    }
});
