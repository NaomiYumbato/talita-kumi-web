// ========================================================
// TALITA KUMI · CLIENT LOGIC & RESERVATION SYNC
// ========================================================

let cotizacionActiva = null;
let fechasOcupadasSet = new Set();
const MAPA_LABELS_TURNO = {
    'Mañana (10:00 a 13:00)': 'Mañana (10:00 a 13:00)',
    'Tarde 1 (13:00 a 16:30)': 'Tarde 1 - Temprano (13:00 a 16:30)',
    'Tarde 2 (16:30 a 19:30)': 'Tarde 2 - Tarde (16:30 a 19:30)'
};

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

// Manejo de alertas y disponibilidad del selector de fechas/turnos
function mostrarAlertaFecha(mensaje) {
    const alertBox = document.getElementById('bookingDateAlert');
    const alertText = document.getElementById('bookingDateAlertText');
    if (alertBox && alertText) {
        alertText.textContent = mensaje;
        alertBox.classList.remove('d-none');
    } else {
        alert(mensaje);
    }
}

function ocultarAlertaFecha() {
    const alertBox = document.getElementById('bookingDateAlert');
    if (alertBox) alertBox.classList.add('d-none');
}

function resetBookingTime(placeholderText = 'Selecciona una fecha primero...') {
    const bookingTime = document.getElementById('bookingTime');
    if (!bookingTime) return;
    bookingTime.innerHTML = `<option value="" disabled selected>${placeholderText}</option>`;
    bookingTime.disabled = true;
}

async function cargarFechasOcupadas() {
    try {
        const resp = await fetch('/api/reservas/fechas-ocupadas');
        if (resp.ok) {
            const fechas = await resp.json();
            fechasOcupadasSet = new Set(fechas);
        }
    } catch (err) {
        console.warn('No se pudo precargar fechas ocupadas:', err);
    }
}

async function refrescarDisponibilidadFecha(fecha) {
    const dateInput = document.getElementById('bookingDate');
    const bookingTime = document.getElementById('bookingTime');
    if (!fecha || !bookingTime) return;

    try {
        const resp = await fetch(`/api/reservas/disponibilidad?fecha=${encodeURIComponent(fecha)}`);
        if (!resp.ok) return;

        const disp = await resp.json();
        if (disp.estaCompletamenteOcupado || !disp.turnosDisponibles || disp.turnosDisponibles.length === 0) {
            fechasOcupadasSet.add(fecha);
            mostrarAlertaFecha('Esta fecha ya tiene sus 3 turnos ocupados. Por favor selecciona otra fecha disponible.');
            if (dateInput) dateInput.value = '';
            resetBookingTime('Fecha sin turnos disponibles...');
            return;
        }

        ocultarAlertaFecha();
        bookingTime.innerHTML = `<option value="" disabled selected>Elige un turno disponible (${disp.turnosDisponibles.length} disponible${disp.turnosDisponibles.length > 1 ? 's' : ''})...</option>`;
        
        disp.turnosDisponibles.forEach(t => {
            const opt = document.createElement('option');
            opt.value = t;
            opt.textContent = MAPA_LABELS_TURNO[t] || t;
            bookingTime.appendChild(opt);
        });

        bookingTime.disabled = false;
    } catch (err) {
        console.error('Error al consultar disponibilidad:', err);
    }
}

function initBookingAvailability() {
    const dateInput = document.getElementById('bookingDate');
    const bookingTime = document.getElementById('bookingTime');
    if (!dateInput) return;

    cargarFechasOcupadas();

    if (bookingTime && !dateInput.value) {
        resetBookingTime('Selecciona una fecha primero...');
    }

    const onDateChanged = async () => {
        const val = dateInput.value;
        if (!val) {
            ocultarAlertaFecha();
            resetBookingTime('Selecciona una fecha primero...');
            return;
        }

        // 1. Validar fecha pasada
        const todayIso = new Date().toISOString().split('T')[0];
        if (val < todayIso) {
            mostrarAlertaFecha('No puedes seleccionar una fecha anterior al día de hoy.');
            dateInput.value = '';
            resetBookingTime('Selecciona una fecha válida...');
            return;
        }

        // 2. Validar Domingo (cerrado)
        const parts = val.split('-').map(Number);
        const dayOfWeek = new Date(parts[0], parts[1] - 1, parts[2]).getDay();
        if (dayOfWeek === 0) {
            mostrarAlertaFecha('El estudio no atiende los domingos. Por favor selecciona una fecha de lunes a sábado.');
            dateInput.value = '';
            resetBookingTime('Selecciona una fecha de atención...');
            return;
        }

        // 3. Validar si ya está en fechas completamente ocupadas
        if (fechasOcupadasSet.has(val)) {
            mostrarAlertaFecha('Esta fecha ya tiene sus 3 turnos ocupados. Por favor selecciona otro día.');
            dateInput.value = '';
            resetBookingTime('Fecha sin turnos disponibles...');
            return;
        }

        // 4. Consultar disponibilidad en tiempo real
        ocultarAlertaFecha();
        if (bookingTime) {
            bookingTime.innerHTML = '<option value="" disabled selected>Consultando disponibilidad de turnos...</option>';
            bookingTime.disabled = true;
        }

        await refrescarDisponibilidadFecha(val);
    };

    dateInput.addEventListener('change', onDateChanged);
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
    let serviceIdFinal = parseInt(selectedOption.getAttribute('data-service-id')) || null;
    let precioFinal = parseFloat(selectedOption.getAttribute('data-base-price')) || 45.0;
    let duracionFinal = parseInt(selectedOption.getAttribute('data-duration')) || 60;
    let notasFinales = message;

    if (cotizacionActiva) {
        servicioFinal = selectedOption.value;
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

    // 1. Guardar en Base de Datos vía API REST con validación estricta de turno único
    try {
        const response = await fetch('/api/reservas', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                nombre: name,
                telefono: phone,
                servicioId: serviceIdFinal,
                servicio: servicioFinal,
                precioEstimado: precioFinal,
                duracionMinutos: duracionFinal,
                fecha: date,
                horario: time,
                notas: notasFinales
            })
        });

        if (!response.ok) {
            const errData = await response.json().catch(() => ({}));
            const errMessage = errData.error || 'No se pudo agendar la cita. Es posible que el turno ya haya sido reservado.';
            mostrarAlertaFecha(errMessage);
            // Refrescar turnos disponibles de la fecha
            await refrescarDisponibilidadFecha(date);
            return;
        }
    } catch (err) {
        console.error('Error de red al conectar con el servidor:', err);
        alert('Hubo un error de conexión al enviar la solicitud. Por favor intenta de nuevo.');
        return;
    } finally {
        if (submitBtn) {
            submitBtn.disabled = false;
            submitBtn.innerHTML = originalBtnHtml;
        }
    }

    // 2. Construir mensaje estructurado para WhatsApp (Perú +51)
    const studioWhatsApp = (window.StudioConfig && window.StudioConfig.whatsAppNumber) 
        ? window.StudioConfig.whatsAppNumber 
        : '51986073098';
    let waText = `¡Hola Talita Kumi! Deseo solicitar una reserva:%0A%0A` +
        ` *Nombre:* ${encodeURIComponent(name)}%0A` +
        ` *Servicio:* ${encodeURIComponent(servicioFinal)}%0A` +
        ` *Precio Estimado:* S/. ${precioFinal.toFixed(2)}%0A` +
        ` *Duración aprox.:* ${duracionFinal} min%0A` +
        ` *Fecha:* ${encodeURIComponent(date)}%0A` +
        ` *Horario:* ${encodeURIComponent(time)}`;

    if (message) {
        waText += `%0A *Nota adicional:* ${encodeURIComponent(message)}`;
    }

    if (cotizacionActiva && cotizacionActiva.detalles) {
        waText += `%0A *Cotización:* ${encodeURIComponent(cotizacionActiva.detalles.join(', '))}`;
    }

    const waUrl = `https://wa.me/${studioWhatsApp}?text=${waText}`;

    // 3. Mostrar feedback visual en la interfaz (sin redirección automática)
    const successCard = document.getElementById('bookingSuccess');
    const bookingForm = document.getElementById('bookingForm');
    const btnSendWhatsApp = document.getElementById('btnSendWhatsApp');

    const summaryServicio = document.getElementById('summaryServicio');
    const summaryFechaHora = document.getElementById('summaryFechaHora');
    const summaryPrecio = document.getElementById('summaryPrecio');

    if (summaryServicio) summaryServicio.textContent = servicioFinal;
    if (summaryFechaHora) summaryFechaHora.textContent = `${date} (${time})`;
    if (summaryPrecio) summaryPrecio.textContent = `S/. ${precioFinal.toFixed(2)}`;

    if (btnSendWhatsApp) {
        btnSendWhatsApp.href = waUrl;
    }

    if (bookingForm && successCard) {
        bookingForm.classList.add('d-none');
        const cotizacionBanner = document.getElementById('cotizacionBanner');
        if (cotizacionBanner) {
            cotizacionBanner.classList.add('d-none');
        }
        successCard.classList.remove('d-none');
        successCard.scrollIntoView({ behavior: 'smooth', block: 'center' });
    }
}

// Reiniciar formulario para solicitar otra cita
function reiniciarFormularioReserva() {
    const successCard = document.getElementById('bookingSuccess');
    const bookingForm = document.getElementById('bookingForm');
    
    if (bookingForm) {
        bookingForm.reset();
        bookingForm.classList.remove('d-none');
    }
    if (successCard) {
        successCard.classList.add('d-none');
    }
    ocultarAlertaFecha();
    resetBookingTime('Selecciona una fecha primero...');
    quitarCotizacionActiva();
    cargarFechasOcupadas();
    
    const seccionReserva = document.getElementById('reserva');
    if (seccionReserva) {
        seccionReserva.scrollIntoView({ behavior: 'smooth', block: 'start' });
    }
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

    // Inicializar disponibilidad y bloqueo de turnos/días ocupados
    initBookingAvailability();
});
