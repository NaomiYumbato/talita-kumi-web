# Documento de Diseño: Portal de Administración de Agenda y Reservas

**Fecha:** 2026-10-22  
**Proyecto:** Talita Kumi Web App  
**Autor:** Naomi Yumbato / Antigravity Superpowers  
**Estado:** Propuesta para Aprobación  

---

## 1. Contexto e Intención

Talita Kumi es un salón de belleza y estética capilar operado de manera personalizada por su dueña y única estilista (Naomi). 

Actualmente, el sitio web cuenta con un chatbot y catálogo que permite a los clientes solicitar citas y generar reservas en la base de datos (`TalitaKumiDb`). Sin embargo, para que Naomi pueda gestionar el negocio en su día a día, requiere un **Portal de Administración privado y protegido por contraseña** que centralice su agenda semanal, el control de abonos (anticipo de S/. 20 por Yape/Plin/efectivo), el seguimiento de clientes por WhatsApp, el registro de citas presenciales/telefónicas y las notas técnicas de cada trabajo.

---

## 2. Objetivos, No-Objetivos y Anti-Objetivos

### Objetivos (Goals)
1. **Autenticación Simple y Segura:**
   - Inicio de sesión con usuario y contraseña en `/Admin/Login`.
   - Autenticación basada en cookies nativas de ASP.NET Core (`CookieAuthenticationDefaults`).
   - Tabla `UsuariosAdmin` en SQL Server para persistir las credenciales con hash seguro (`IPasswordHasher` de ASP.NET Core).
   - Cierre de sesión seguro en `/Admin/Logout`.
2. **Calendario Semanal Interactivo (Sección Superior):**
   - Vista de agenda semanal de Lunes a Sábado organizada por las 3 franjas horarias principales del salón:
     - ☀️ **Mañana:** 10:00 AM - 1:00 PM
     - 🌤️ **Tarde 1 (Temprano):** 1:00 PM - 4:30 PM
     - 🌙 **Tarde 2 (Tarde):** 4:30 PM - 7:30 PM
   - Las citas se muestran en fichas coloreadas por estado (🟡 Pendiente, 🟢 Confirmada, 🔵 Atendida/Completada, 🔴 Cancelada).
   - Al hacer clic en cualquier cita del calendario, se abre el **popup modal** con la información de la clienta, notas, enlace a WhatsApp y acciones rápidas.
3. **Gestión Operativa de Reservas / CRUD Directo en Tabla (Sección Inferior):**
   - Sin popups redundantes: toda la información está directamente visible en las columnas de la tabla.
   - **Edición in situ de Notas:** Cada fila cuenta con su campo de notas editable y botón de guardado rápido sin tener que abrir ventanas adicionales.
   - **Cambio de Estado Directo (Propuesta 2 Aprobada):** Cada fila cuenta con un selector desplegable (`<select>`) intuitivo con los estados:
     - `🟡 Pendiente de Abono`
     - `🟢 Reserva Confirmada`
     - `🔵 Servicio Finalizado`
     - `🔴 Cita Cancelada`
     Al cambiar el estado en el menú desplegable, el sistema actualiza la base de datos de inmediato vía Fetch/AJAX y ajusta el saldo y abono correspondiente.
   - **Acceso rápido a WhatsApp:** Botón con enlace directo al chat de la clienta (`https://wa.me/51...`).
   - Filtros de fecha y buscador en tiempo real por nombre o teléfono.
4. **Registro de Citas Manuales ("➕ Agendar Cita Manual"):**
   - Botón en la cabecera que abre un formulario modal para registrar clientas que agendan directamente por llamada o presencialmente.

### No-Objetivos (Non-Goals)
- No se requiere soporte multi-empleado ni asignación de turnos entre múltiples estilistas (el salón opera bajo el modelo de trabajadora única/dueña).
- No se requiere integración automática con APIs bancarias de Yape o Plin; la confirmación del abono se realiza de forma manual por Naomi mediante un botón.
- No se requiere una pasarela de pago con tarjeta de crédito en esta etapa.

### Anti-Objetivos (Anti-Goals)
- **Evitar Identity Bloat:** No implementar ASP.NET Core Identity completo con decenas de tablas (`AspNetRoles`, `AspNetUserClaims`, etc.) que añadirían sobrecarga innecesaria para un portal de un único administrador.
- **No romper las reservas existentes:** Las reservas generadas por el bot web deben visualizarse de inmediato en la agenda sin requerir migraciones destructivas.

---

## 3. Modelo de Datos y Arquitectura

### 3.1. Entidad `UsuarioAdmin`
Nueva entidad mapeada en `TalitaKumiDbContext`:
```csharp
public class UsuarioAdmin
{
    public int Id { get; set; }
    [Required, MaxLength(50)]
    public string Username { get; set; } = string.Empty;
    [Required, MaxLength(255)]
    public string PasswordHash { get; set; } = string.Empty;
    [Required, MaxLength(100)]
    public string NombreCompleto { get; set; } = string.Empty;
    public bool Activo { get; set; } = true;
    public DateTime? UltimoAcceso { get; set; }
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
}
```

### 3.2. Entidad `Reserva`
Aprovecha y complementa los campos del modelo `Models/Reserva.cs`:
- `Id`
- `ClienteId` / `Cliente`
- `ServicioId` / `ServicioRelacionado`
- `Nombre`
- `Telefono`
- `Servicio`
- `PrecioEstimado`
- `MontoAbonado` (decimal, default 0.00; al confirmar abono pasa a 20.00 o el monto recibido)
- `DuracionMinutos`
- `Fecha` (DateOnly)
- `Horario` (string, almacena tanto turnos del bot como horas puntuales manuales)
- `Notas` (string? hasta 500 caracteres, editable)
- `Estado` (string: "Pendiente", "Confirmada", "Completada", "Cancelada")
- `FechaCreacion`

*(Decisión de diseño para solapamiento de horarios)*: No se implementará un bloqueo rígido de agenda que impida guardar citas superpuestas; el salón opera con 1 estilista pero Naomi suele atender un corte o brushing mientras procesa una decoloración o tiempo de pose de otra clienta. Se mostrarán avisos visuales sin bloquear su gestión.

### 3.3. Configuración y Seeding Inicial
- Durante el arranque o migración inicial, si no existe ningún usuario en `UsuariosAdmin`, el sistema creará el usuario administrador por defecto para Naomi con contraseña inicial configurable en `appsettings.json` o variables de entorno.

---

## 4. Diseño de la Interfaz de Usuario (UI/UX)

La interfaz del administrador **debe mantener una estética clara, luminosa y elegante**, idéntica a la paleta de la vista del cliente (sin fondos oscuros):
- **Paleta de Colores Oficial:**
  - Fondo general: Crema cálido (`#FAF6F0` / `--color-cream`).
  - Tarjetas y contenedores: Blanco puro (`#FFFFFF`) con bordes sutiles cálidos (`#E5DAD0` / `--color-border`).
  - Cabecera: Blanca luminosa con logotipo circular naranja koi (`#E85D3F`).
  - Acentos de marca: Naranja Koi (`#E85D3F`), Azul Agua (`#437694`) y Dorado sutil (`#F5A623`).
  - Tipografía: *Montserrat* para textos y botones, y *Cormorant Garamond* para títulos elegantes.
1. **Encabezado del Panel (`/Admin`):**
   - Logotipo y bienvenida a Naomi.
   - Botón destacado: **"➕ Nueva Cita Manual"** en naranja koi.
   - Botón discreto de salida (**"Cerrar Sesión"**).
2. **Calendario Semanal (Sección Superior):**
   - 6 columnas (Lunes a Sábado) y 3 filas correspondientes a las franjas horarias (*Mañana*, *Tarde 1*, *Tarde 2*).
   - Fichas de cita claras con bordes de color según estado (amarillo suave para pendientes, verde menta para confirmadas, azul suave para finalizadas).
   - Al hacer clic en una cita, abre un popup modal limpio para inspeccionar datos y abrir WhatsApp.
3. **Tabla Operativa de Reservas (Sección Inferior):**
   - Fondo blanco con encabezados crema.
   - **Notas técnicas editables directamente en la celda** con botón "Guardar Nota".
   - **Selector de estado directo (`<select>`):** Permite cambiar al instante entre *Pendiente de Abono*, *Reserva Confirmada*, *Servicio Finalizado*, o *Cita Cancelada*.
   - Botón verde oficial de WhatsApp (`wa.me/51...`) para contacto directo.

---

## 5. Decisiones Tomadas con el Usuario
- **Estética Visual:** Diseño claro, luminoso y elegante idéntico a la vista pública de la clienta (fondo crema `--color-cream`, tarjetas blancas, cabecera blanca, acentos en naranja koi y azul agua, sin fondos oscuros).
- **Autenticación:** Uso de tabla `UsuariosAdmin` en SQL Server (Opción B acordada).
- **Notas Técnicas:** Campo `Notas` editable in situ directamente en la tabla y en el popup del calendario.
- **Acciones y Estados:** Selector desplegable directo en cada fila de la tabla (Propuesta 2 acordada: *Pendiente de Abono*, *Reserva Confirmada*, *Servicio Finalizado*, *Cita Cancelada*), evitando botones cambiantes o ambiguos.
- **Calendario Semanal:** Organizado en las 3 franjas horarias principales (*Mañana*, *Tarde 1*, *Tarde 2*) con popup al clic en cada cita.
- **Agendamiento:** Botón "➕ Agendar Cita Manual" para registrar citas directas por llamada o presenciales.
- **Modelo de Personal:** 1 sola trabajadora (la dueña es la única estilista).

---

## 6. Lo que se deja a Criterio del Desarrollador (Builder)
- Nombres de los ViewModels (`LoginViewModel`, `AdminDashboardViewModel`, `EditarReservaViewModel`).
- Detalles de la implementación del controlador (`AdminController`) y servicios auxiliares (`AdminAuthService`).
- Cobertura con pruebas unitarias para el flujo de autenticación y actualización de estados/notas.
