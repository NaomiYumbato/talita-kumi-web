# Plan de Implementación: Cotizador de Uñas y Sistema de Reservas en Soles

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implementar un motor de cotización de uñas por capas en Soles (S/.), un chatbot conversacional interactivo guiado por botones, persistencia de reservas con Entity Framework Core (SQLite) y su sincronización bidireccional con el formulario web y WhatsApp.

**Architecture:** Se utilizará un motor de cálculo puro en C# (Dominio) probado mediante pruebas unitarias xUnit (TDD). Para la persistencia se configurará EF Core con SQLite local (cero configuración y portable para portafolio). El frontend incorporará un widget flotante de chatbot ligero en JavaScript/CSS conectado al formulario existente en la landing page mediante precarga de estado y eventos.

**Tech Stack:** ASP.NET Core 10.0 MVC, Entity Framework Core SQLite, xUnit, Vanilla JS, CSS3, Bootstrap 5.

**Spec:** Diseñado a partir del brainstorming de Talita Kumi: Catálogo de servicios fijos en Perú (Soles) + Matriz de Uñas acumulativa (Técnica Base + Largo + Nail Art + Retiro) con duración estimada en minutos.

---

## Global Constraints

- Todos los precios deben expresarse en Soles peruanos (`S/.`) con formato decimal estándar.
- La persistencia debe funcionar sin requerir servidores externos instalados (usar SQLite local `talitakumi.db`).
- El chatbot debe ser 100% determinista (árbol de estados guiado con botones rápidos, sin costo de API keys).
- Toda reserva debe guardar: Nombre, WhatsApp, Servicio, Precio Total Estimado, Duración en minutos, Fecha, Franja Horaria y Notas.
- El formulario web existente no se elimina: se conecta con el chatbot para recibir presupuestos precargados o permitir reservas directas.

## Review Focus

1. **Cálculo de Cotización:** Combinación acumulativa correcta de Técnica Base + Largo + Arte + Retiro sumando tanto el monto en S/. como la duración en minutos.
2. **Validación de Datos en Reserva:** Rechazo de reservas con fecha anterior al día actual o campos obligatorios faltantes.
3. **Persistencia en SQLite:** Confirmación de que las citas guardadas persisten tras reiniciar la aplicación.
4. **Precarga en el Formulario:** Al hacer clic en "Agendar con este presupuesto" en el chat, el formulario debe hacer scroll suave y mostrar el desglose precargado sin sobreescribir datos ya escritos por la clienta.
5. **Generación del Enlace de WhatsApp:** Formateo correcto del mensaje de WhatsApp con saltos de línea codificados, emojis y detalle en Soles.

---

### Task 1: Motor de Cálculo y Dominio de Cotizaciones (TDD)

**Files:**
- Create: `Models/Cotizaciones/CotizacionUñaRequest.cs`
- Create: `Models/Cotizaciones/CotizacionUñaResult.cs`
- Create: `Services/NailPricingEngine.cs`
- Create: `tests/TalitaKumiWebApp.Tests/NailPricingEngineTests.cs`

- [x] **Step 1.1:** Configurar proyecto de pruebas xUnit (`tests/TalitaKumiWebApp.Tests`) y referenciar el proyecto web principal.
- [x] **Step 1.2:** Escribir pruebas unitarias que verifiquen las reglas de cálculo en Soles y minutos:
  - Semipermanente básico (S/. 45, 60m).
  - Soft Gel + Largo Medio + Francesita (S/. 90 + S/. 15 + S/. 15 = S/. 120, 125m).
  - Esculpidas + Largo XL + Full Arte + Retiro (S/. 120 + S/. 30 + S/. 35 + S/. 20 = S/. 205, 210m).
- [x] **Step 1.3:** Ejecutar pruebas para confirmar que fallen (Red).
- [x] **Step 1.4:** Implementar `NailPricingEngine.cs` con las tarifas exactas acordadas.
- [x] **Step 1.5:** Ejecutar pruebas para confirmar que pasen al 100% (Green).

---

### Task 2: Persistencia con Entity Framework Core y SQLite

**Files:**
- Modify: `TalitaKumiWebApp.csproj` (agregar paquete `Microsoft.EntityFrameworkCore.Sqlite`)
- Create: `Data/AppDbContext.cs`
- Create: `Models/Reserva.cs`
- Create: `Services/IReservaService.cs`
- Create: `Services/ReservaService.cs`
- Modify: `Program.cs` (registrar DbContext y servicio)
- Create: `tests/TalitaKumiWebApp.Tests/ReservaServiceTests.cs`

- [x] **Step 2.1:** Agregar el paquete NuGet `Microsoft.EntityFrameworkCore.Sqlite` a `TalitaKumiWebApp.csproj`.
- [x] **Step 2.2:** Definir la entidad `Reserva` con campos para cliente, servicio, precio, duración, fecha, horario, notas y estado (Pendiente, Confirmada).
- [x] **Step 2.3:** Crear `AppDbContext` y configurar la base de datos SQLite `talitakumi.db`.
- [x] **Step 2.4:** Escribir prueba unitaria para `ReservaService` validando creación exitosa de reservas.
- [x] **Step 2.5:** Implementar `ReservaService` y registrarlo en la inyección de dependencias en `Program.cs`.
- [x] **Step 2.6:** Asegurar creación automática de la base de datos al inicio (`app.Services.CreateScope() -> dbContext.Database.EnsureCreated()`).

---

### Task 3: Controladores y Endpoints de API

**Files:**
- Create: `Controllers/Api/CotizacionesApiController.cs`
- Create: `Controllers/Api/ReservasApiController.cs`
- Create: `tests/TalitaKumiWebApp.Tests/ApiControllerTests.cs`

- [x] **Step 3.1:** Escribir pruebas para el endpoint `POST /api/cotizaciones/unas`.
- [x] **Step 3.2:** Implementar `CotizacionesApiController` que reciba el JSON de opciones y devuelva el resultado calculado.
- [x] **Step 3.3:** Escribir pruebas para el endpoint `POST /api/reservas`.
- [x] **Step 3.4:** Implementar `ReservasApiController` para guardar la solicitud en la base de datos y devolver respuesta estructurada.

---

### Task 4: Widget del Chatbot Cotizador en el Frontend

**Files:**
- Create: `wwwroot/js/chatbot.js`
- Create: `wwwroot/css/chatbot.css`
- Modify: `Views/Shared/_Layout.cshtml` (incluir `chatbot.css` y `chatbot.js`)

- [x] **Step 4.1:** Crear la estructura visual del botón flotante y ventana de chat en la esquina inferior derecha con colores del logo (Azul Olas `#437694` y Coral Koi `#E85D3F`).
- [x] **Step 4.2:** Implementar la máquina de estados en `chatbot.js` con los 4 pasos:
  1. Selección de Técnica Base (Semipermanente, Kapping, Soft Gel, Esculpidas).
  2. Selección de Largo (Natural, Medio, Largo XL).
  3. Selección de Nail Art (Liso, Francés/CatEye, Full Arte).
  4. Extras (Sin retiro, Retiro semi, Retiro esculpidas).
- [x] **Step 4.3:** Mostrar la tarjeta de resumen final con el precio en `S/.`, tiempo aproximado y botón de acción: *"Agendar con este presupuesto"*.

---

### Task 5: Sincronización con el Formulario Web y WhatsApp

**Files:**
- Modify: `Views/Home/Index.cshtml` (actualizar selector de servicios a Soles y agregar contenedor de cotización)
- Modify: `wwwroot/js/site.js` (conectar eventos entre el chatbot y el formulario)

- [x] **Step 5.1:** Actualizar los precios mostrados en la sección de servicios y en el formulario de `Index.cshtml` a moneda nacional (Soles `S/.`).
- [x] **Step 5.2:** Implementar función `aplicarCotizacionAFormulario(cotizacion)` que:
  - Seleccione automáticamente el servicio correspondiente en el `<select>`.
  - Muestre una caja destacada en el formulario con el detalle ("Presupuesto del Asistente: S/. 120 · Duración: 2h 05m").
  - Haga scroll suave hasta la sección `#reserva`.
- [x] **Step 5.3:** Agregar alerta interactiva en el selector de servicios: si eligen un servicio de uñas complejo de forma manual, mostrar sugerencia para abrir el bot cotizador.
- [x] **Step 5.4:** Conectar el envío del formulario para que guarde en `/api/reservas` y genere el enlace directo a WhatsApp con el desglose en Soles.

---

### Task 6: Verificación de Extremo a Extremo

- [x] **Step 6.1:** Ejecutar `dotnet test` y comprobar que todas las pruebas unitarias y de integración pasen en verde.
- [x] **Step 6.2:** Probar el flujo completo en el navegador:
  - Abrir chatbot ➔ Cotizar Soft Gel con Nail Art ➔ Pulsar "Agendar" ➔ Verificar precarga en formulario.
  - Completar nombre y fecha ➔ Enviar reserva ➔ Verificar guardado en SQLite y apertura de WhatsApp.
