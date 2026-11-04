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
window.prellenarCitaManual = function (fechaIso, turno) {
    const modalEl = document.getElementById('modalCrearReserva');
    if (!modalEl) return;

    const inputFecha = document.getElementById('crearFecha');
    if (inputFecha) inputFecha.value = fechaIso;

    const selectHorario = document.getElementById('crearHorario');
    if (selectHorario) {
        for (let i = 0; i < selectHorario.options.length; i++) {
            if (selectHorario.options[i].value.includes(turno)) {
                selectHorario.selectedIndex = i;
                break;
            }
        }
    }

    const modal = bootstrap.Modal.getOrCreateInstance(modalEl);
    modal.show();
};

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
