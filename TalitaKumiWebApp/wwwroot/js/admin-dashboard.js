/**
 * Talita Kumi - Admin Dashboard Script
 * Maneja interacción de calendario, actualización asíncrona de notas y estados,
 * y modales de citas.
 */

document.addEventListener('DOMContentLoaded', () => {
    initCalendarModal();
    initServiceSelector();
    initStatusDropdowns();
    initNotesModalEditor();
    initAdminAvailability();
    initProfileModal();
});

// Toast auxiliar para retroalimentación instantánea
function showAdminToast(message, isError = false) {
    const toastEl = document.getElementById('toastAdmin');
    const toastBody = document.getElementById('toastAdminBody');
    if (!toastEl || !toastBody) return;

    toastBody.innerHTML = `
        <i class="bi ${isError ? 'bi-exclamation-octagon-fill text-danger' : 'bi-check-circle-fill text-success'} fs-5"></i>
        <span>${message}</span>
    `;

    toastEl.className = `toast align-items-center text-white ${isError ? 'bg-danger' : 'bg-dark'} border-0 shadow-lg`;
    const toast = bootstrap.Toast.getOrCreateInstance(toastEl, { delay: 3500 });
    toast.show();
}

// 1. Popup de Detalle de Cita desde el Calendario
function initCalendarModal() {
    const detailModal = document.getElementById('modalDetalleCita');
    if (!detailModal) return;

    detailModal.addEventListener('show.bs.modal', (event) => {
        const button = event.relatedTarget;
        if (!button) return;

        const nombre = button.getAttribute('data-nombre') || '-';
        const telefono = button.getAttribute('data-telefono') || '-';
        const servicio = button.getAttribute('data-servicio') || '-';
        const precio = button.getAttribute('data-precio') || '0.00';
        const abono = button.getAttribute('data-abono') || '0.00';
        const fecha = button.getAttribute('data-fecha') || '-';
        const horario = button.getAttribute('data-horario') || '-';
        const estado = button.getAttribute('data-estado') || 'Pendiente';
        const notas = button.getAttribute('data-notas') || '';

        document.getElementById('modalDetalleNombre').textContent = nombre;
        document.getElementById('modalDetalleTelefono').textContent = telefono;
        document.getElementById('modalDetalleFecha').textContent = fecha;
        document.getElementById('modalDetalleHorario').textContent = horario;
        document.getElementById('modalDetalleServicio').textContent = servicio;
        document.getElementById('modalDetallePrecio').textContent = `S/. ${parseFloat(precio).toFixed(2)}`;
        document.getElementById('modalDetalleAbono').textContent = `S/. ${parseFloat(abono).toFixed(2)}`;

        const notasEl = document.getElementById('modalDetalleNotas');
        notasEl.textContent = (notas && notas.trim().length > 0) ? notas : 'Sin observaciones registradas.';

        // Badge de estado
        const badgeEl = document.getElementById('modalDetalleBadgeEstado');
        let badgeClass = 'bg-warning-subtle text-warning-emphasis border border-warning-subtle';
        let badgeText = '🟡 Pendiente de Abono';
        if (estado === 'Confirmada') {
            badgeClass = 'bg-success-subtle text-success-emphasis border border-success-subtle';
            badgeText = '🟢 Reserva Confirmada';
        } else if (estado === 'Completada') {
            badgeClass = 'bg-primary-subtle text-primary-emphasis border border-primary-subtle';
            badgeText = '🔵 Servicio Finalizado';
        } else if (estado === 'Cancelada') {
            badgeClass = 'bg-danger-subtle text-danger-emphasis border border-danger-subtle';
            badgeText = '🔴 Cita Cancelada';
        }
        badgeEl.className = `badge px-3 py-2 fs-6 rounded-pill ${badgeClass}`;
        badgeEl.textContent = badgeText;

        // WhatsApp Link
        const cleanTel = telefono.replace(/\D/g, '');
        const msg = encodeURIComponent(`Hola ${nombre}, te saludamos de Talita Kumi sobre tu cita de ${servicio} agendada para el ${fecha} (${horario}).`);
        const btnWa = document.getElementById('modalDetalleBtnWa');
        btnWa.href = `https://wa.me/51${cleanTel}?text=${msg}`;
    });
}

// 2. Prellenar Modal de Cita Manual al dar clic en casilla libre del calendario
const TURNOS_CATALOGO = [
    { key: 'Mañana', value: 'Mañana (10:00 a 13:00)', label: 'Mañana (10:00 - 13:00)' },
    { key: 'Tarde 1', value: 'Tarde 1 (13:00 a 16:30)', label: 'Tarde 1 (13:00 - 16:30)' },
    { key: 'Tarde 2', value: 'Tarde 2 (16:30 a 19:30)', label: 'Tarde 2 (16:30 - 19:30)' }
];

window.prellenarCitaManual = function (fechaIso, turno) {
    const modalEl = document.getElementById('modalCrearReserva');
    if (!modalEl) return;

    const inputFecha = document.getElementById('crearFecha');
    if (inputFecha) inputFecha.value = fechaIso;

    actualizarDisponibilidadAdmin(fechaIso, turno);

    const modal = bootstrap.Modal.getOrCreateInstance(modalEl);
    modal.show();
};

async function actualizarDisponibilidadAdmin(fechaIso, turnoSeleccionar = null) {
    const selectHorario = document.getElementById('crearHorario');
    const alertBox = document.getElementById('crearAlertaDisponibilidad');
    const alertText = document.getElementById('crearAlertaDisponibilidadTexto');
    const submitBtn = document.getElementById('btnSubmitCrearReserva');

    if (!selectHorario || !fechaIso) return;

    // Verificar si es domingo
    const parts = fechaIso.split('-').map(Number);
    const dayOfWeek = new Date(parts[0], parts[1] - 1, parts[2]).getDay();

    if (dayOfWeek === 0) {
        if (alertBox && alertText) {
            alertText.textContent = 'Atención: Los domingos el estudio se encuentra cerrado.';
            alertBox.classList.remove('d-none');
        }
    } else if (alertBox) {
        alertBox.classList.add('d-none');
    }

    try {
        const resp = await fetch(`/api/reservas/disponibilidad?fecha=${encodeURIComponent(fechaIso)}`);
        if (!resp.ok) return;

        const data = await resp.json();
        const ocupados = data.turnosOcupados || [];
        const disponibles = data.turnosDisponibles || [];

        selectHorario.innerHTML = '';

        if (disponibles.length === 0) {
            const opt = document.createElement('option');
            opt.value = '';
            opt.textContent = '⚠️ Todos los turnos están ocupados';
            opt.disabled = true;
            opt.selected = true;
            selectHorario.appendChild(opt);

            if (alertBox && alertText) {
                alertText.textContent = `No hay turnos disponibles para el ${fechaIso}. Los 3 turnos ya están reservados.`;
                alertBox.classList.remove('d-none');
            }
            if (submitBtn) submitBtn.disabled = true;
            return;
        }

        if (submitBtn) submitBtn.disabled = false;
        if (dayOfWeek !== 0 && alertBox) {
            alertBox.classList.add('d-none');
        }

        let preseleccionHecha = false;

        TURNOS_CATALOGO.forEach(t => {
            const estaOcupado = ocupados.some(o => o.includes(t.key) || o === t.value);
            if (!estaOcupado) {
                const opt = document.createElement('option');
                opt.value = t.value;
                opt.textContent = t.label;

                if (turnoSeleccionar && (turnoSeleccionar.includes(t.key) || t.value.includes(turnoSeleccionar))) {
                    opt.selected = true;
                    preseleccionHecha = true;
                }

                selectHorario.appendChild(opt);
            }
        });

        if (!preseleccionHecha && selectHorario.options.length > 0) {
            selectHorario.selectedIndex = 0;
        }
    } catch (err) {
        console.error('Error al consultar disponibilidad en admin:', err);
    }
}

function initAdminAvailability() {
    const inputFecha = document.getElementById('crearFecha');
    const modalEl = document.getElementById('modalCrearReserva');

    if (inputFecha) {
        inputFecha.addEventListener('change', () => {
            actualizarDisponibilidadAdmin(inputFecha.value);
        });
    }

    if (modalEl) {
        modalEl.addEventListener('show.bs.modal', (event) => {
            if (!event.relatedTarget || !event.relatedTarget.classList.contains('slot-empty')) {
                if (inputFecha && inputFecha.value) {
                    actualizarDisponibilidadAdmin(inputFecha.value);
                }
            }
        });
    }
}

// 3. Selección asistida de servicio en creación manual
function initServiceSelector() {
    const select = document.getElementById('crearServicioSelect');
    const inputPrecio = document.getElementById('crearPrecio');
    const inputNombre = document.getElementById('crearServicioNombre');

    if (!select || !inputPrecio) return;

    select.addEventListener('change', () => {
        const option = select.options[select.selectedIndex];
        if (!option || !option.value) return;

        const precio = option.getAttribute('data-precio');
        const nombre = option.getAttribute('data-nombre');

        if (precio && inputPrecio) {
            inputPrecio.value = parseFloat(precio).toFixed(2);
        }
        if (nombre && inputNombre && !inputNombre.value) {
            inputNombre.value = nombre;
        }
    });
}

// 4. Selector directo de estado en la tabla (AJAX)
function initStatusDropdowns() {
    const dropdowns = document.querySelectorAll('.status-dropdown');

    dropdowns.forEach(dropdown => {
        let previousValue = dropdown.value;

        dropdown.addEventListener('focus', () => {
            previousValue = dropdown.value;
        });

        dropdown.addEventListener('change', async () => {
            const id = parseInt(dropdown.getAttribute('data-id'), 10);
            const nuevoEstado = dropdown.value;
            const row = dropdown.closest('tr');
            const spinner = row ? row.querySelector('.status-spinner') : null;

            if (spinner) spinner.classList.remove('d-none');
            dropdown.disabled = true;

            try {
                const response = await fetch('/Admin/ActualizarEstado', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                        'RequestVerificationToken': getAntiForgeryToken()
                    },
                    body: JSON.stringify({ id, estado: nuevoEstado })
                });

                if (!response.ok) {
                    throw new Error('Error al actualizar el estado.');
                }

                const data = await response.json();
                if (data.success) {
                    previousValue = nuevoEstado;
                    showAdminToast(`Estado actualizado: <strong>${nuevoEstado}</strong>`);

                    // Si se confirmó y se abonó automáticamente, reflejar en la celda
                    if (data.montoAbonado > 0 && row) {
                        const abonoCell = row.querySelector('.celda-abono');
                        if (abonoCell) {
                            abonoCell.textContent = `Abono: S/. ${parseFloat(data.montoAbonado).toFixed(2)}`;
                            abonoCell.classList.add('text-success', 'fw-bold');
                            abonoCell.classList.remove('text-muted');
                        }
                    }

                    // Sincronizar automáticamente el calendario semanal superior sin recargar página
                    actualizarCalendarioPorCambioEstado(row, nuevoEstado, data.montoAbonado);
                } else {
                    throw new Error(data.message || 'No se pudo actualizar.');
                }
            } catch (err) {
                dropdown.value = previousValue;
                showAdminToast(err.message || 'Error de conexión al actualizar.', true);
            } finally {
                if (spinner) spinner.classList.add('d-none');
                dropdown.disabled = false;
            }
        });
    });
}

// Sincronización en vivo del calendario semanal superior
function actualizarCalendarioPorCambioEstado(row, nuevoEstado, montoAbonado) {
    if (!row) return;

    const id = row.getAttribute('data-reserva-id');
    const fechaIso = row.getAttribute('data-fecha-iso');
    const turnoClave = row.getAttribute('data-turno-clave');
    const fechaDisplay = row.getAttribute('data-fecha-display');
    const horario = row.getAttribute('data-horario');
    const nombre = row.getAttribute('data-nombre');
    const telefono = row.getAttribute('data-telefono');
    const servicio = row.getAttribute('data-servicio');
    const precio = row.getAttribute('data-precio') || '0.00';
    const notas = row.getAttribute('data-notas') || '';

    // Actualizar atributos en la fila de la tabla
    row.setAttribute('data-estado', nuevoEstado);
    if (montoAbonado !== undefined && montoAbonado !== null) {
        row.setAttribute('data-abono', montoAbonado);
    }
    const abonoFinal = montoAbonado !== undefined && montoAbonado !== null 
        ? montoAbonado 
        : (row.getAttribute('data-abono') || '0.00');

    // Determinar estilo y texto del badge
    let badgeColor = 'border-warning bg-warning-subtle text-warning-emphasis';
    let estadoIcon = '🟡 Pendiente';

    if (nuevoEstado === 'Confirmada') {
        badgeColor = 'border-success bg-success-subtle text-success-emphasis';
        estadoIcon = '🟢 Confirmada';
    } else if (nuevoEstado === 'Completada') {
        badgeColor = 'border-primary bg-primary-subtle text-primary-emphasis';
        estadoIcon = '🔵 Finalizada';
    } else if (nuevoEstado === 'Cancelada') {
        badgeColor = 'border-danger bg-danger-subtle text-danger-emphasis';
        estadoIcon = '🔴 Cancelada';
    }

    // 1. Buscar si la tarjeta ya existe en el calendario semanal
    let card = document.querySelector(`.appointment-card[data-id="${id}"]`);

    if (card) {
        card.setAttribute('data-estado', nuevoEstado);
        card.setAttribute('data-abono', abonoFinal);

        const badge = card.querySelector('.badge');
        if (badge) {
            badge.className = `badge border ${badgeColor}`;
            badge.style.fontSize = '0.68rem';
            badge.textContent = estadoIcon;
        }

        const priceRow = card.querySelector('.d-flex.justify-content-between.align-items-center.mt-1');
        if (priceRow) {
            let abonoSpan = priceRow.querySelector('.text-success');
            if (parseFloat(abonoFinal) > 0) {
                if (!abonoSpan) {
                    abonoSpan = document.createElement('span');
                    abonoSpan.className = 'text-success fw-bold';
                    abonoSpan.title = 'Abonó';
                    priceRow.appendChild(abonoSpan);
                }
                abonoSpan.textContent = `✓ S/. ${parseFloat(abonoFinal).toFixed(2)}`;
            } else if (abonoSpan) {
                abonoSpan.remove();
            }
        }

        const cell = card.closest('.calendar-slot-cell');
        if (cell) {
            const activeCards = cell.querySelectorAll('.appointment-card:not([data-estado="Cancelada"])');
            let freedBtn = cell.querySelector('.slot-freed-action');
            if (activeCards.length === 0) {
                if (!freedBtn) {
                    freedBtn = document.createElement('div');
                    freedBtn.className = 'slot-freed-action mt-1 text-center';
                    freedBtn.innerHTML = `
                        <button type="button" class="btn btn-sm btn-outline-success py-0 px-2 small w-100" style="font-size: 0.7rem;" onclick="prellenarCitaManual('${fechaIso}', '${turnoClave}')">
                            <i class="bi bi-plus-lg"></i> Turno libre
                        </button>
                    `;
                    cell.appendChild(freedBtn);
                }
            } else if (freedBtn) {
                freedBtn.remove();
            }
        }

        // Animación suave de confirmación
        card.classList.add('border-primary', 'shadow');
        setTimeout(() => {
            card.classList.remove('border-primary', 'shadow');
        }, 1200);
    } else {
        // Si no está en el calendario semanal pero su fecha y turno corresponden a la semana mostrada
        const cell = document.querySelector(`.calendar-slot-cell[data-fecha="${fechaIso}"][data-turno="${turnoClave}"]`);
        if (cell) {
            const slotEmpty = cell.querySelector('.slot-empty');
            if (slotEmpty) slotEmpty.remove();

            const tieneNotas = notas && notas.trim().length > 0;
            const tieneAbono = parseFloat(abonoFinal) > 0;

            const newCard = document.createElement('div');
            newCard.className = 'appointment-card border rounded-3 p-2 shadow-sm mb-1 bg-white cursor-pointer';
            newCard.setAttribute('role', 'button');
            newCard.setAttribute('tabindex', '0');
            newCard.setAttribute('data-bs-toggle', 'modal');
            newCard.setAttribute('data-bs-target', '#modalDetalleCita');
            newCard.setAttribute('data-id', id);
            newCard.setAttribute('data-nombre', nombre);
            newCard.setAttribute('data-telefono', telefono);
            newCard.setAttribute('data-servicio', servicio);
            newCard.setAttribute('data-precio', precio);
            newCard.setAttribute('data-abono', abonoFinal);
            newCard.setAttribute('data-fecha', fechaDisplay || fechaIso);
            newCard.setAttribute('data-horario', horario);
            newCard.setAttribute('data-estado', nuevoEstado);
            newCard.setAttribute('data-notas', notas);

            newCard.innerHTML = `
                <div class="d-flex justify-content-between align-items-center mb-1">
                    <span class="badge border ${badgeColor}" style="font-size: 0.68rem;">
                        ${estadoIcon}
                    </span>
                    ${tieneNotas ? '<span title="Tiene notas u observaciones" class="text-danger small"><i class="bi bi-sticky-fill"></i></span>' : ''}
                </div>
                <div class="fw-bold text-truncate text-dark small" title="${escapeHtml(nombre)}">
                    ${escapeHtml(nombre)}
                </div>
                <div class="text-muted text-truncate" style="font-size: 0.75rem;" title="${escapeHtml(servicio)}">
                    ${escapeHtml(servicio)}
                </div>
                <div class="d-flex justify-content-between align-items-center mt-1" style="font-size: 0.75rem;">
                    <span class="fw-semibold text-secondary">S/. ${parseFloat(precio).toFixed(2)}</span>
                    ${tieneAbono ? `<span class="text-success fw-bold" title="Abonó">✓ S/. ${parseFloat(abonoFinal).toFixed(2)}</span>` : ''}
                </div>
            `;

            cell.appendChild(newCard);

            if (nuevoEstado === 'Cancelada') {
                const freedBtn = document.createElement('div');
                freedBtn.className = 'slot-freed-action mt-1 text-center';
                freedBtn.innerHTML = `
                    <button type="button" class="btn btn-sm btn-outline-success py-0 px-2 small w-100" style="font-size: 0.7rem;" onclick="prellenarCitaManual('${fechaIso}', '${turnoClave}')">
                        <i class="bi bi-plus-lg"></i> Turno libre
                    </button>
                `;
                cell.appendChild(freedBtn);
            }
        }
    }

    // Actualizar contadores semanales
    actualizarContadoresSemana();
}

function actualizarContadoresSemana() {
    const cards = document.querySelectorAll('.calendar-grid-table .appointment-card');
    let total = cards.length;
    let confirmadas = 0;
    let pendientes = 0;

    cards.forEach(c => {
        const est = c.getAttribute('data-estado');
        if (est === 'Confirmada') confirmadas++;
        else if (est === 'Pendiente') pendientes++;
    });

    const elTotal = document.getElementById('contadorTotalSemana');
    const elConfirmadas = document.getElementById('contadorConfirmadasSemana');
    const elPendientes = document.getElementById('contadorPendientesSemana');

    if (elTotal) elTotal.textContent = total;
    if (elConfirmadas) elConfirmadas.textContent = confirmadas;
    if (elPendientes) elPendientes.textContent = pendientes;
}

// 5. Edición de notas y observaciones mediante Modal Pop-up Simple
function initNotesModalEditor() {
    const modalEl = document.getElementById('modalEditarNota');
    if (!modalEl) return;

    const inputId = document.getElementById('modalNotaReservaId');
    const textarea = document.getElementById('modalNotaTexto');
    const btnGuardar = document.getElementById('modalNotaBtnGuardar');

    // Al abrir el modal, cargar la nota de la fila
    modalEl.addEventListener('show.bs.modal', (event) => {
        const btn = event.relatedTarget;
        if (!btn) return;

        const id = btn.getAttribute('data-id') || '';
        const notas = btn.getAttribute('data-notas') || '';

        if (inputId) inputId.value = id;
        if (textarea) textarea.value = notas;
    });

    // Autoenfoque en el área de texto al abrirse
    modalEl.addEventListener('shown.bs.modal', () => {
        if (textarea) {
            textarea.focus();
            const len = textarea.value.length;
            textarea.setSelectionRange(len, len);
        }
    });

    // Guardar cambios vía AJAX
    if (btnGuardar) {
        btnGuardar.addEventListener('click', async () => {
            const idVal = inputId ? inputId.value : '';
            const id = parseInt(idVal, 10);
            const notasTexto = textarea ? textarea.value.trim() : '';

            if (isNaN(id) || id <= 0) return;

            const originalHtml = btnGuardar.innerHTML;
            btnGuardar.innerHTML = `<span class="spinner-border spinner-border-sm me-1" role="status"></span> Guardando...`;
            btnGuardar.disabled = true;

            try {
                const response = await fetch('/Admin/ActualizarNotas', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                        'RequestVerificationToken': getAntiForgeryToken()
                    },
                    body: JSON.stringify({ id, notas: notasTexto })
                });

                if (!response.ok) {
                    throw new Error('Error al guardar las notas.');
                }

                const data = await response.json();
                if (data.success) {
                    const notasFinales = data.notas || '';
                    const tieneNotas = notasFinales.trim().length > 0;

                    // 1. Actualizar el botón y el snippet en la fila de la tabla
                    const triggerBtn = document.querySelector(`.btn-abrir-nota[data-id="${id}"]`);
                    if (triggerBtn) {
                        triggerBtn.setAttribute('data-notas', notasFinales);
                        triggerBtn.innerHTML = `<i class="bi bi-journal-text me-1"></i>${tieneNotas ? 'Ver / Editar' : 'Agregar'}`;

                        const cellContainer = triggerBtn.closest('td');
                        if (cellContainer) {
                            const snippet = cellContainer.querySelector('.celda-nota-texto');
                            if (snippet) {
                                snippet.setAttribute('title', notasFinales);
                                if (tieneNotas) {
                                    snippet.innerHTML = `<i class="bi bi-sticky-fill text-warning me-1"></i><span class="texto-nota-display">${escapeHtml(notasFinales)}</span>`;
                                } else {
                                    snippet.innerHTML = `<span class="text-muted fst-italic texto-nota-display">Sin observaciones</span>`;
                                }
                            }
                        }
                    }

                    // 2. Si existe la tarjeta en el calendario superior, actualizar data-notas e ícono
                    const card = document.querySelector(`.appointment-card[data-id="${id}"]`);
                    if (card) {
                        card.setAttribute('data-notas', notasFinales);
                        const topHeader = card.querySelector('.d-flex.justify-content-between');
                        const noteIcon = card.querySelector('.bi-sticky-fill')?.closest('span');
                        if (tieneNotas) {
                            if (!noteIcon && topHeader) {
                                const iconSpan = document.createElement('span');
                                iconSpan.className = 'text-danger small';
                                iconSpan.title = 'Tiene notas u observaciones';
                                iconSpan.innerHTML = '<i class="bi bi-sticky-fill"></i>';
                                topHeader.appendChild(iconSpan);
                            }
                        } else if (noteIcon) {
                            noteIcon.remove();
                        }
                    }

                    // 3. Cerrar el modal
                    const modalInstance = bootstrap.Modal.getInstance(modalEl);
                    if (modalInstance) {
                        modalInstance.hide();
                    }

                    showAdminToast('Notas guardadas.');
                } else {
                    throw new Error(data.message || 'No se pudo guardar la nota.');
                }
            } catch (err) {
                showAdminToast(err.message || 'Error al conectar con el servidor.', true);
            } finally {
                btnGuardar.innerHTML = originalHtml;
                btnGuardar.disabled = false;
            }
        });
    }
}

function escapeHtml(text) {
    if (!text) return '';
    return text
        .replace(/&/g, "&amp;")
        .replace(/</g, "&lt;")
        .replace(/>/g, "&gt;")
        .replace(/"/g, "&quot;")
        .replace(/'/g, "&#039;");
}

function getAntiForgeryToken() {
    const tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
    return tokenInput ? tokenInput.value : '';
}

// 7. Modal Mi Perfil (Cargar datos y alternar visibilidad de contraseñas)
function initProfileModal() {
    const modalEl = document.getElementById('modalMiPerfil');
    if (!modalEl) return;

    modalEl.addEventListener('show.bs.modal', async () => {
        const inputUsername = document.getElementById('perfilUsername');
        const inputNombre = document.getElementById('perfilNombreCompleto');
        const alertBox = document.getElementById('perfilAlerta');

        // Limpiar campos de contraseña
        const passActual = document.getElementById('perfilPasswordActual');
        const passNuevo = document.getElementById('perfilNuevoPassword');
        const passConf = document.getElementById('perfilConfirmarPassword');
        if (passActual) passActual.value = '';
        if (passNuevo) passNuevo.value = '';
        if (passConf) passConf.value = '';

        if (alertBox) alertBox.classList.add('d-none');

        try {
            const resp = await fetch('/Admin/ObtenerPerfil');
            if (resp.ok) {
                const data = await resp.json();
                if (inputUsername) inputUsername.value = data.username || '';
                if (inputNombre) inputNombre.value = data.nombreCompleto || '';
            }
        } catch (err) {
            console.error('Error al cargar datos del perfil:', err);
        }
    });

    const toggleBtns = modalEl.querySelectorAll('.btn-toggle-password');
    toggleBtns.forEach(btn => {
        btn.addEventListener('click', () => {
            const targetId = btn.getAttribute('data-target');
            const targetInput = document.getElementById(targetId);
            const icon = btn.querySelector('i');
            if (!targetInput || !icon) return;

            if (targetInput.type === 'password') {
                targetInput.type = 'text';
                icon.className = 'bi bi-eye-slash';
            } else {
                targetInput.type = 'password';
                icon.className = 'bi bi-eye';
            }
        });
    });
}

// Manejador del formulario de edición de perfil
window.handleGuardarPerfil = async function (event) {
    event.preventDefault();

    const inputUsername = document.getElementById('perfilUsername');
    const inputNombre = document.getElementById('perfilNombreCompleto');
    const inputPassActual = document.getElementById('perfilPasswordActual');
    const inputPassNuevo = document.getElementById('perfilNuevoPassword');
    const inputPassConf = document.getElementById('perfilConfirmarPassword');

    const nuevoUsername = inputUsername ? inputUsername.value.trim() : '';
    const nombreCompleto = inputNombre ? inputNombre.value.trim() : '';
    const passwordActual = inputPassActual ? inputPassActual.value : '';
    const nuevoPassword = inputPassNuevo ? inputPassNuevo.value : '';
    const confirmarPassword = inputPassConf ? inputPassConf.value : '';

    const alertBox = document.getElementById('perfilAlerta');
    const alertText = document.getElementById('perfilAlertaTexto');
    const submitBtn = document.getElementById('btnGuardarPerfil');

    const mostrarError = (msg) => {
        if (alertBox && alertText) {
            alertText.textContent = msg;
            alertBox.classList.remove('d-none');
        } else {
            showAdminToast(msg, true);
        }
    };

    if (alertBox) alertBox.classList.add('d-none');

    if (!nuevoUsername) {
        mostrarError('El usuario de acceso es obligatorio.');
        return;
    }

    if (!nombreCompleto) {
        mostrarError('El nombre completo es obligatorio.');
        return;
    }

    if (nuevoPassword || confirmarPassword) {
        if (!passwordActual) {
            mostrarError('Debes ingresar tu contraseña actual para autorizar el cambio de contraseña.');
            return;
        }
        if (nuevoPassword.length < 6) {
            mostrarError('La nueva contraseña debe tener al menos 6 caracteres.');
            return;
        }
        if (nuevoPassword !== confirmarPassword) {
            mostrarError('La nueva contraseña y su confirmación no coinciden.');
            return;
        }
    }

    const originalBtnHtml = submitBtn ? submitBtn.innerHTML : '';
    if (submitBtn) {
        submitBtn.disabled = true;
        submitBtn.innerHTML = '<span class="spinner-border spinner-border-sm me-1" role="status"></span> Guardando...';
    }

    try {
        const resp = await fetch('/Admin/ActualizarPerfil', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': getAntiForgeryToken()
            },
            body: JSON.stringify({
                nuevoUsername,
                nombreCompleto,
                passwordActual: passwordActual || null,
                nuevoPassword: nuevoPassword || null,
                confirmarPassword: confirmarPassword || null
            })
        });

        const data = await resp.json();
        if (!resp.ok || !data.success) {
            throw new Error(data.message || 'Error al actualizar el perfil.');
        }

        // Actualizar el nombre visible en la barra superior en vivo
        const navbarNombre = document.getElementById('adminNavbarNombre');
        if (navbarNombre && data.nombre) {
            navbarNombre.textContent = data.nombre;
        }

        if (inputUsername && data.username) {
            inputUsername.value = data.username;
        }

        // Cerrar modal
        const modalEl = document.getElementById('modalMiPerfil');
        const modal = bootstrap.Modal.getInstance(modalEl);
        if (modal) modal.hide();

        if (data.usernameCambiado) {
            showAdminToast('Usuario de acceso actualizado. Actualizando sesión...');
            setTimeout(() => {
                window.location.reload();
            }, 1000);
            return;
        }

        showAdminToast(data.message || 'Perfil actualizado con éxito.');
    } catch (err) {
        mostrarError(err.message || 'Error de conexión con el servidor.');
    } finally {
        if (submitBtn) {
            submitBtn.disabled = false;
            submitBtn.innerHTML = originalBtnHtml;
        }
    }
};

