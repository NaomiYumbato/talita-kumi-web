/**
 * Talita Kumi - Admin Dashboard Script
 * Maneja interacción de calendario, actualización asíncrona de notas y estados,
 * y modales de citas.
 */

document.addEventListener('DOMContentLoaded', () => {
    initCalendarModal();
    initServiceSelector();
    initStatusDropdowns();
    initNotesInlineEditor();
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

// 5. Edición de notas in situ en la tabla
function initNotesInlineEditor() {
    const buttons = document.querySelectorAll('.btn-guardar-nota');

    buttons.forEach(btn => {
        btn.addEventListener('click', async () => {
            const id = parseInt(btn.getAttribute('data-id'), 10);
            const row = btn.closest('tr');
            const input = row ? row.querySelector('.input-nota') : null;
            const feedback = row ? row.querySelector('.nota-feedback') : null;

            if (!input) return;

            const notasTexto = input.value.trim();
            const originalHtml = btn.innerHTML;
            btn.innerHTML = `<span class="spinner-border spinner-border-sm" role="status"></span>`;
            btn.disabled = true;

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
                    throw new Error('Error al guardar notas.');
                }

                const data = await response.json();
                if (data.success) {
                    btn.innerHTML = `<i class="bi bi-check2"></i>`;
                    btn.classList.replace('btn-outline-secondary', 'btn-outline-success');

                    if (feedback) {
                        feedback.classList.remove('d-none');
                        setTimeout(() => feedback.classList.add('d-none'), 2500);
                    }

                    showAdminToast('Notas guardadas correctamente.');

                    setTimeout(() => {
                        btn.innerHTML = originalHtml;
                        btn.classList.replace('btn-outline-success', 'btn-outline-secondary');
                        btn.disabled = false;
                    }, 1800);
                } else {
                    throw new Error(data.message || 'No se pudo guardar.');
                }
            } catch (err) {
                btn.innerHTML = originalHtml;
                btn.disabled = false;
                showAdminToast(err.message || 'Error al conectar con el servidor.', true);
            }
        });
    });

    // Guardar con Enter en el input
    document.querySelectorAll('.input-nota').forEach(input => {
        input.addEventListener('keydown', (e) => {
            if (e.key === 'Enter') {
                e.preventDefault();
                const row = input.closest('tr');
                const btn = row ? row.querySelector('.btn-guardar-nota') : null;
                if (btn) btn.click();
            }
        });
    });
}

function getAntiForgeryToken() {
    const tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
    return tokenInput ? tokenInput.value : '';
}
