// ========================================================
// CHATBOT COTIZADOR DE UÑAS · TALITA KUMI
// ========================================================

const Chatbot = (function() {
    let isOpen = false;
    let currentStep = 0;
    let selectedData = {
        tecnica: '',
        largo: '',
        nailArt: '',
        retiro: ''
    };

    const steps = [
        {
            title: "Paso 1 de 4",
            message: "¡Hola! 🌸 Soy la asistente de **Talita Kumi**. Te ayudo a cotizar tu diseño de uñas en 30 segundos.<br><br>¿Qué técnica base deseas realizarte?",
            options: [
                { label: "Manicura Semipermanente", value: "Semipermanente", badge: "Desde S/. 45" },
                { label: "Kapping Gel (Refuerzo)", value: "Kapping", badge: "Desde S/. 65" },
                { label: "Soft Gel Tips (Extensiones)", value: "Soft Gel", badge: "Desde S/. 90" },
                { label: "Uñas Esculpidas (Acrílico)", value: "Esculpidas", badge: "Desde S/. 120" }
            ],
            key: "tecnica"
        },
        {
            title: "Paso 2 de 4",
            message: "¡Gran elección! ✨ ¿Qué largo prefieres para tus uñas?",
            options: [
                { label: "Natural / Corto (N° 0)", value: "Natural", badge: "+S/. 0" },
                { label: "Largo Medio (N° 1 a 2)", value: "Medio", badge: "+S/. 15" },
                { label: "Largo Extremo / XL (N° 3 a 5)", value: "XL", badge: "+S/. 30" }
            ],
            key: "largo"
        },
        {
            title: "Paso 3 de 4",
            message: "¡Perfecto! 🎨 Ahora cuéntame qué nivel de diseño o arte deseas:",
            options: [
                { label: "Color Liso / Básico (1 o 2 tonos)", value: "Liso", badge: "+S/. 0" },
                { label: "Francesita / Cat Eye / 2 Uñas Decoradas", value: "Francesita", badge: "+S/. 15" },
                { label: "Full Nail Art / 3D / Cristales / Mano Alzada", value: "Full Arte", badge: "+S/. 35" }
            ],
            key: "nailArt"
        },
        {
            title: "Paso 4 de 4",
            message: "Última pregunta: ¿Tienes algún producto anterior en tus uñas que debamos retirar?",
            options: [
                { label: "No, mis uñas están limpias", value: "Ninguno", badge: "Sin retiro" },
                { label: "Sí, esmalte semipermanente anterior", value: "Semipermanente", badge: "+S/. 10" },
                { label: "Sí, acrílico o soft gel de otro salón", value: "Esculpidas", badge: "+S/. 20" }
            ],
            key: "retiro"
        }
    ];

    function init() {
        createChatbotDOM();
        bindEvents();
    }

    function createChatbotDOM() {
        if (document.getElementById('chatbotContainer')) return;

        const container = document.createElement('div');
        container.id = 'chatbotContainer';
        container.innerHTML = `
            <div class="chatbot-badge-hint" id="chatbotHint">
                ✨ ¿Cuánto saldría tu diseño? Cotízalo aquí
            </div>
            <button class="chatbot-toggle-btn" id="chatbotToggle" aria-label="Abrir cotizador">
                <i class="bi bi-chat-heart-fill"></i>
            </button>

            <div class="chatbot-window" id="chatbotWindow">
                <div class="chatbot-header">
                    <div class="d-flex align-items-center">
                        <img src="/images/logo-header.jpeg" alt="Talita Kumi Asistente" class="chatbot-avatar" />
                        <div class="chatbot-title-box">
                            <div class="chatbot-name">Talita Kumi Bot</div>
                            <div class="chatbot-status">
                                <span class="status-dot"></span> Cotizadora en línea
                            </div>
                        </div>
                    </div>
                    <button class="chatbot-close-btn" id="chatbotClose" aria-label="Cerrar">
                        <i class="bi bi-x-lg"></i>
                    </button>
                </div>

                <div class="chatbot-messages" id="chatbotMessages">
                    <!-- Mensajes dinámicos -->
                </div>
            </div>
        `;
        document.body.appendChild(container);
    }

    function bindEvents() {
        const toggleBtn = document.getElementById('chatbotToggle');
        const closeBtn = document.getElementById('chatbotClose');
        const hint = document.getElementById('chatbotHint');

        toggleBtn.addEventListener('click', () => {
            toggleChat();
        });

        closeBtn.addEventListener('click', () => {
            closeChat();
        });

        if (hint) {
            hint.addEventListener('click', () => {
                openChat();
            });
        }
    }

    function toggleChat() {
        if (isOpen) {
            closeChat();
        } else {
            openChat();
        }
    }

    function openChat() {
        isOpen = true;
        const win = document.getElementById('chatbotWindow');
        const hint = document.getElementById('chatbotHint');
        win.classList.add('open');
        if (hint) hint.style.display = 'none';

        if (currentStep === 0 && document.getElementById('chatbotMessages').children.length === 0) {
            renderCurrentStep();
        }
    }

    function closeChat() {
        isOpen = false;
        const win = document.getElementById('chatbotWindow');
        win.classList.remove('open');
    }

    function addBotMessage(html) {
        const messages = document.getElementById('chatbotMessages');
        const msgDiv = document.createElement('div');
        msgDiv.className = 'bot-msg';
        msgDiv.innerHTML = `
            <div class="bot-bubble">${html}</div>
        `;
        messages.appendChild(msgDiv);
        scrollToBottom();
    }

    function addUserMessage(text) {
        const messages = document.getElementById('chatbotMessages');
        const msgDiv = document.createElement('div');
        msgDiv.className = 'user-msg';
        msgDiv.innerHTML = `
            <div class="user-bubble">${text}</div>
        `;
        messages.appendChild(msgDiv);
        scrollToBottom();
    }

    function scrollToBottom() {
        const messages = document.getElementById('chatbotMessages');
        messages.scrollTop = messages.scrollHeight;
    }

    function renderCurrentStep() {
        if (currentStep < steps.length) {
            const step = steps[currentStep];
            addBotMessage(step.message);

            const messages = document.getElementById('chatbotMessages');
            const optionsDiv = document.createElement('div');
            optionsDiv.className = 'chatbot-options';
            optionsDiv.id = `optionsStep${currentStep}`;

            step.options.forEach(opt => {
                const btn = document.createElement('button');
                btn.className = 'chatbot-btn-option';
                btn.innerHTML = `
                    <span>${opt.label}</span>
                    <span class="badge-price">${opt.badge}</span>
                `;
                btn.onclick = () => handleOptionSelect(step.key, opt.value, opt.label);
                optionsDiv.appendChild(btn);
            });

            messages.appendChild(optionsDiv);
            scrollToBottom();
        } else {
            finalizarCotizacion();
        }
    }

    function handleOptionSelect(key, value, label) {
        selectedData[key] = value;

        // Deshabilitar botones anteriores
        const prevOptions = document.getElementById(`optionsStep${currentStep}`);
        if (prevOptions) {
            prevOptions.remove();
        }

        addUserMessage(label);
        currentStep++;
        setTimeout(renderCurrentStep, 400);
    }

    async function finalizarCotizacion() {
        addBotMessage("Calculando tu presupuesto en tiempo real... ⏳");

        try {
            const response = await fetch('/api/cotizaciones/unas', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(selectedData)
            });

            if (!response.ok) throw new Error('Error en el cálculo');
            const cotizacion = await response.json();
            mostrarResultado(cotizacion);
        } catch (error) {
            console.warn('Fallback a cálculo local:', error);
            // Cálculo local de respaldo
            const cotizacionFallback = calcularLocal(selectedData);
            mostrarResultado(cotizacionFallback);
        }
    }

    function calcularLocal(data) {
        let precio = 45;
        let duracion = 60;
        if (data.tecnica === 'Kapping') { precio = 65; duracion = 75; }
        if (data.tecnica === 'Soft Gel') { precio = 90; duracion = 90; }
        if (data.tecnica === 'Esculpidas') { precio = 120; duracion = 120; }

        if (data.largo === 'Medio') { precio += 15; duracion += 15; }
        if (data.largo === 'XL') { precio += 30; duracion += 30; }

        if (data.nailArt === 'Francesita') { precio += 15; duracion += 20; }
        if (data.nailArt === 'Full Arte') { precio += 35; duracion += 40; }

        if (data.retiro === 'Semipermanente') { precio += 10; duracion += 20; }
        if (data.retiro === 'Esculpidas') { precio += 20; duracion += 30; }

        const horas = Math.floor(duracion / 60);
        const mins = duracion % 60;
        const duracionFormateada = horas > 0 ? `${horas}h ${mins.toString().padStart(2, '0')}m` : `${mins}m`;

        return {
            precioTotal: precio,
            duracionMinutos: duracion,
            precioFormateado: `S/. ${precio.toFixed(2)}`,
            duracionFormateada: duracionFormateada,
            resumenServicio: `${data.tecnica} (Largo ${data.largo}, Arte ${data.nailArt})`,
            detalles: [
                `Técnica: ${data.tecnica}`,
                `Largo: ${data.largo}`,
                `Nail Art: ${data.nailArt}`,
                `Retiro: ${data.retiro}`
            ]
        };
    }

    function mostrarResultado(cotizacion) {
        const messages = document.getElementById('chatbotMessages');
        const cardDiv = document.createElement('div');
        cardDiv.className = 'cotizacion-card-result';
        cardDiv.innerHTML = `
            <div class="result-header">✨ Presupuesto Calculado</div>
            <div class="result-service-name">${cotizacion.resumenServicio}</div>
            
            <div class="result-total-box">
                <div>
                    <small class="text-muted d-block">Total Estimado</small>
                    <span class="result-total-price">${cotizacion.precioFormateado}</span>
                </div>
                <div class="text-end">
                    <small class="text-muted d-block">Duración aproximada</small>
                    <span class="result-time"><i class="bi bi-clock me-1"></i>${cotizacion.duracionFormateada}</span>
                </div>
            </div>

            <button class="btn-book-from-chat" id="btnAgendarDesdeChat">
                <i class="bi bi-calendar2-check"></i> Agendar con este presupuesto
            </button>

            <button class="btn-restart-chat" id="btnReiniciarChat">
                🔄 Recalcular otro diseño
            </button>
        `;
        messages.appendChild(cardDiv);
        scrollToBottom();

        document.getElementById('btnAgendarDesdeChat').onclick = () => {
            closeChat();
            if (window.aplicarCotizacionAFormulario) {
                window.aplicarCotizacionAFormulario(cotizacion);
            }
        };

        document.getElementById('btnReiniciarChat').onclick = () => {
            currentStep = 0;
            selectedData = { tecnica: '', largo: '', nailArt: '', retiro: '' };
            messages.innerHTML = '';
            renderCurrentStep();
        };
    }

    // Exponer método público para abrir el bot desde cualquier botón de la web
    return {
        init: init,
        open: openChat,
        close: closeChat
    };
})();

document.addEventListener('DOMContentLoaded', () => {
    Chatbot.init();
});
