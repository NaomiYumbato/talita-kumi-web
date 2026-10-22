# Plan de Implementación: Portal de Administración de Agenda y Reservas

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Construir el Portal de Administración privado para Naomi (dueña y única estilista de Talita Kumi), con inicio de sesión seguro, calendario semanal interactivo por franjas, gestión operativa de reservas en tabla con selector directo de estado y notas editables in situ, agendamiento de citas manuales y estética clara idéntica al sitio de la clienta.

**Architecture:** Módulo MVC en `/Admin` protegido por autenticación nativa de cookies de ASP.NET Core contra la tabla `UsuariosAdmin` en SQL Server. Se extiende `ReservaService` para soportar consultas de agenda semanal, filtrado dinámico, endpoints AJAX ligeros para cambio de estado y guardado de notas en línea, y creación de citas manuales. La interfaz utiliza una plantilla propia `_AdminLayout.cshtml` con la paleta clara oficial (`#FAF6F0`, blanco, naranja koi `#E85D3F` y azul agua `#437694`).

**Tech Stack:** ASP.NET Core 10 MVC, Entity Framework Core 10, SQL Server (`TalitaKumiDb`), ASP.NET Core Cookie Authentication, `PasswordHasher<UsuarioAdmin>`, Bootstrap 5 + CSS de marca, xUnit + InMemoryDatabase para pruebas unitarias.

**Spec:** [`docs/superpowers/specs/2026-10-22-portal-administrador-design.md`](file:///Volumes/Mac_Env/TalitaKumi/TalitaKumiWebApp/docs/superpowers/specs/2026-10-22-portal-administrador-design.md)

## Global Constraints

- **Estética Visual:** Diseño claro, luminoso y elegante idéntico a la vista pública de la clienta (fondo crema `--color-cream` `#FAF6F0`, tarjetas blancas, cabecera blanca, acentos en naranja koi `#E85D3F` y azul agua `#437694`, sin fondos oscuros).
- **Modelo de Personal:** 1 sola trabajadora (la dueña Naomi es la única administradora y estilista).
- **Autenticación:** Uso de tabla `UsuariosAdmin` en SQL Server con hash seguro y cookies nativas de ASP.NET Core (sin Identity pesado).
- **Notas Técnicas:** Campo `Notas` editable in situ directamente en la tabla y en el popup del calendario.
- **Acciones y Estados:** Selector desplegable directo en cada fila de la tabla (`🟡 Pendiente de Abono`, `🟢 Reserva Confirmada`, `🔵 Servicio Finalizado`, `🔴 Cita Cancelada`) con actualización asíncrona sin popups redundantes.
- **Calendario Semanal:** Organizado en las 3 franjas horarias principales (*Mañana*, *Tarde 1*, *Tarde 2*) con popup al clic en cada cita.
- **Abono:** `MontoAbonado` (decimal, default 0.00; al confirmar abono pasa a 20.00 o el monto recibido).
- **Agendamiento:** Botón "➕ Agendar Cita Manual" para registrar citas directas por llamada o presenciales.

## Review Focus

1. **Intento de acceso a `/Admin` sin sesión activa:** Debe redirigir inmediatamente a `/Admin/Login` con `ReturnUrl` sin revelar datos del dashboard.
2. **Cambio de estado a "Reserva Confirmada" desde el selector:** Debe actualizar `Estado = "Confirmada"` y fijar `MontoAbonado = 20.00m` si estaba en 0.
3. **Edición de notas con caracteres especiales o espacios vacíos:** Debe sanitizar y permitir notas vacías o con tildes/emojis sin corromper la reserva.
4. **Mapeo de reservas del bot con franjas horarias de texto amplio:** Debe mapearse limpiamente a una de las 3 franjas (*Mañana*, *Tarde 1*, *Tarde 2*) en el calendario semanal sin romperse.
5. **Registro de cita manual con clienta no existente:** Debe crear o reutilizar el registro en la tabla `Clientes` vinculado por teléfono sin duplicados.

---

### Task 1: Modelo de Datos, Entidad `UsuarioAdmin` y Campo `MontoAbonado` en `Reserva`

**Files:**
- Create: `Models/UsuarioAdmin.cs`
- Modify: `Models/Reserva.cs`
- Modify: `Data/AppDbContext.cs`
- Test: `tests/TalitaKumiWebApp.Tests/AppDbContextAdminTests.cs`

**Interfaces:**
- Produces:
  - `UsuarioAdmin` (`Id`, `Username`, `PasswordHash`, `NombreCompleto`, `Activo`, `UltimoAcceso`, `FechaCreacion`).
  - `Reserva.MontoAbonado` (`decimal`, default `0.00m`).
  - `DbSet<UsuarioAdmin> UsuariosAdmin` en `AppDbContext`.

- [ ] **Step 1: Write the failing test**

```csharp
// tests/TalitaKumiWebApp.Tests/AppDbContextAdminTests.cs
[Fact]
public async Task AppDbContext_PuedeGuardarYRecuperarUsuarioAdmin_YReservaConMontoAbonado()
{
    var options = new DbContextOptionsBuilder<AppDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .Options;

    using var context = new AppDbContext(options);
    var admin = new UsuarioAdmin
    {
        Username = "naomi",
        PasswordHash = "AQAAAAEAACcQAAAAE...",
        NombreCompleto = "Naomi Yumbato"
    };
    context.UsuariosAdmin.Add(admin);

    var reserva = new Reserva
    {
        Nombre = "Camila Ramos",
        Telefono = "998877665",
        Servicio = "Balayage",
        Fecha = new DateOnly(2026, 10, 24),
        Horario = "Mañana (10:00 a 13:00)",
        MontoAbonado = 20.00m
    };
    context.Reservas.Add(reserva);
    await context.SaveChangesAsync();

    var adminDb = await context.UsuariosAdmin.FirstOrDefaultAsync(u => u.Username == "naomi");
    Assert.NotNull(adminDb);
    Assert.Equal("Naomi Yumbato", adminDb.NombreCompleto);

    var reservaDb = await context.Reservas.FirstOrDefaultAsync(r => r.Telefono == "998877665");
    Assert.NotNull(reservaDb);
    Assert.Equal(20.00m, reservaDb.MontoAbonado);
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/TalitaKumiWebApp.Tests/TalitaKumiWebApp.Tests.csproj --filter "FullyQualifiedName~AppDbContextAdminTests"`
Expected: FAIL (types `UsuarioAdmin` or `MontoAbonado` do not exist).

- [ ] **Step 3: Implement `UsuarioAdmin` and update `Reserva` and `AppDbContext`**

- Create `Models/UsuarioAdmin.cs` con propiedades `Id`, `Username`, `PasswordHash`, `NombreCompleto`, `Activo`, `UltimoAcceso`, `FechaCreacion`.
- Add `public decimal MontoAbonado { get; set; } = 0.00m;` en `Models/Reserva.cs`.
- Add `public DbSet<UsuarioAdmin> UsuariosAdmin => Set<UsuarioAdmin>();` y mapeo de columnas en `Data/AppDbContext.cs`.

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test tests/TalitaKumiWebApp.Tests/TalitaKumiWebApp.Tests.csproj --filter "FullyQualifiedName~AppDbContextAdminTests"`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add Models/UsuarioAdmin.cs Models/Reserva.cs Data/AppDbContext.cs tests/TalitaKumiWebApp.Tests/AppDbContextAdminTests.cs
git commit -m "feat(admin): agregar entidad UsuarioAdmin y campo MontoAbonado a Reserva"
```

---

### Task 2: Servicio de Autenticación de Administrador (`AdminAuthService`) y Seeding Inicial

**Files:**
- Create: `Services/IAdminAuthService.cs`
- Create: `Services/AdminAuthService.cs`
- Test: `tests/TalitaKumiWebApp.Tests/AdminAuthServiceTests.cs`
- Modify: `Program.cs`

**Interfaces:**
- Consumes: `AppDbContext`, `UsuarioAdmin`.
- Produces:
  - `Task<bool> ValidateCredentialsAsync(string username, string password)`
  - `Task<UsuarioAdmin?> GetByUsernameAsync(string username)`
  - `Task EnsureDefaultAdminUserAsync(string defaultUsername, string defaultPassword, string defaultNombre)`

- [ ] **Step 1: Write the failing test**

```csharp
// tests/TalitaKumiWebApp.Tests/AdminAuthServiceTests.cs
[Fact]
public async Task AdminAuthService_ValidarCredenciales_RetornaTrueParaPasswordCorrecto_YFalseParaIncorrecto()
{
    var options = new DbContextOptionsBuilder<AppDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .Options;
    using var context = new AppDbContext(options);
    var service = new AdminAuthService(context);

    await service.EnsureDefaultAdminUserAsync("naomi", "TalitaKumi2026*", "Naomi Yumbato");

    var ok = await service.ValidateCredentialsAsync("naomi", "TalitaKumi2026*");
    var errorClave = await service.ValidateCredentialsAsync("naomi", "ClaveErronea");
    var errorUser = await service.ValidateCredentialsAsync("desconocido", "TalitaKumi2026*");

    Assert.True(ok);
    Assert.False(errorClave);
    Assert.False(errorUser);
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/TalitaKumiWebApp.Tests/TalitaKumiWebApp.Tests.csproj --filter "FullyQualifiedName~AdminAuthServiceTests"`
Expected: FAIL (`AdminAuthService` does not exist).

- [ ] **Step 3: Implement `IAdminAuthService` and `AdminAuthService`**

Implementar usando `Microsoft.AspNetCore.Identity.PasswordHasher<UsuarioAdmin>`.
- `EnsureDefaultAdminUserAsync`: si `context.UsuariosAdmin` está vacío, hashea la contraseña y crea el usuario por defecto.
- `ValidateCredentialsAsync`: busca por `Username` y `Activo == true`, verifica con `PasswordVerificationResult.Success`. Si coincide, actualiza `UltimoAcceso = DateTime.UtcNow`.

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test tests/TalitaKumiWebApp.Tests/TalitaKumiWebApp.Tests.csproj --filter "FullyQualifiedName~AdminAuthServiceTests"`
Expected: PASS.

- [ ] **Step 5: Register service in `Program.cs`**

Registrar `builder.Services.AddScoped<IAdminAuthService, AdminAuthService>();` y llamar a `EnsureDefaultAdminUserAsync` en el arranque.

- [ ] **Step 6: Commit**

```bash
git add Services/IAdminAuthService.cs Services/AdminAuthService.cs tests/TalitaKumiWebApp.Tests/AdminAuthServiceTests.cs Program.cs
git commit -m "feat(admin): implementar AdminAuthService con hash seguro y seeding inicial"
```

---

### Task 3: Controlador y Vista de Inicio de Sesión (`/Admin/Login` y `/Admin/Logout`)

**Files:**
- Create: `ViewModels/Admin/LoginViewModel.cs`
- Create: `Controllers/AdminController.cs` (métodos `Login` GET/POST, `Logout` POST)
- Create: `Views/Admin/Login.cshtml`
- Modify: `Program.cs` (configuración de Cookie Authentication)
- Test: `tests/TalitaKumiWebApp.Tests/AdminControllerAuthTests.cs`

**Interfaces:**
- Consumes: `IAdminAuthService`, `LoginViewModel`.
- Produces: Rutas `/Admin/Login` y `/Admin/Logout` protegidas con Cookie Authentication (`CookieAuthenticationDefaults.AuthenticationScheme`).

- [ ] **Step 1: Write the failing test**

```csharp
// tests/TalitaKumiWebApp.Tests/AdminControllerAuthTests.cs
[Fact]
public async Task Login_POST_ConCredencialesInvalidas_RetornaVistaConError()
{
    var mockAuth = new Mock<IAdminAuthService>();
    mockAuth.Setup(a => a.ValidateCredentialsAsync("usuario", "malo")).ReturnsAsync(false);
    var controller = new AdminController(mockAuth.Object, null!);

    var model = new LoginViewModel { Username = "usuario", Password = "malo" };
    var result = await controller.Login(model, returnUrl: null);

    var viewResult = Assert.IsType<ViewResult>(result);
    Assert.False(controller.ModelState.IsValid);
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/TalitaKumiWebApp.Tests/TalitaKumiWebApp.Tests.csproj --filter "FullyQualifiedName~AdminControllerAuthTests"`
Expected: FAIL (`LoginViewModel` / `AdminController` not defined).

- [ ] **Step 3: Implement `LoginViewModel`, `AdminController` (Auth actions), `Login.cshtml` and `Program.cs` auth**

- `LoginViewModel`: `Username` (Required), `Password` (Required, DataType Password), `RememberMe` (bool).
- `AdminController`:
  - `[AllowAnonymous] [HttpGet("Admin/Login")]`
  - `[AllowAnonymous] [HttpPost("Admin/Login")]` -> si valida, genera `ClaimsIdentity` con roles `Admin`, llama `HttpContext.SignInAsync`, redirige a `returnUrl` o `/Admin`.
  - `[HttpPost("Admin/Logout")]` -> `HttpContext.SignOutAsync`, redirige a `/Admin/Login`.
- `Views/Admin/Login.cshtml`: pantalla con estética clara Talita Kumi (`#FAF6F0`, logo koi, tarjeta blanca, inputs estilizados).
- `Program.cs`: `builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(options => { options.LoginPath = "/Admin/Login"; options.AccessDeniedPath = "/Admin/Login"; });`

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test tests/TalitaKumiWebApp.Tests/TalitaKumiWebApp.Tests.csproj --filter "FullyQualifiedName~AdminControllerAuthTests"`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add ViewModels/Admin/LoginViewModel.cs Controllers/AdminController.cs Views/Admin/Login.cshtml Program.cs tests/TalitaKumiWebApp.Tests/AdminControllerAuthTests.cs
git commit -m "feat(admin): implementar pantalla de login y autenticacion por cookies"
```

---

### Task 4: Extensión de `ReservaService` para Agenda y Gestión de Reservas

**Files:**
- Modify: `Services/IReservaService.cs`
- Modify: `Services/ReservaService.cs`
- Create: `ViewModels/Admin/CrearReservaManualViewModel.cs`
- Test: `tests/TalitaKumiWebApp.Tests/AdminReservaServiceTests.cs`

**Interfaces:**
- Produces en `IReservaService`:
  - `Task<List<Reserva>> ObtenerReservasSemanaAsync(DateOnly inicioSemana, DateOnly finSemana)`
  - `Task<List<Reserva>> ObtenerReservasFiltradasAsync(DateOnly? fecha, string? busqueda, string? estado)`
  - `Task<bool> ActualizarEstadoAsync(int id, string nuevoEstado)`
  - `Task<bool> ActualizarNotasAsync(int id, string notas)`
  - `Task<Reserva> CrearReservaManualAsync(CrearReservaManualViewModel model)`

- [ ] **Step 1: Write the failing test**

```csharp
// tests/TalitaKumiWebApp.Tests/AdminReservaServiceTests.cs
[Fact]
public async Task ActualizarEstado_ACampoConfirmada_ActualizaEstadoYMontoAbonado()
{
    var options = new DbContextOptionsBuilder<AppDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .Options;
    using var context = new AppDbContext(options);
    var service = new ReservaService(context);

    var r = new Reserva { Nombre = "Ana", Telefono = "999", Servicio = "Corte", Fecha = DateOnly.FromDateTime(DateTime.Today), Horario = "Mañana", Estado = "Pendiente", MontoAbonado = 0m };
    context.Reservas.Add(r);
    await context.SaveChangesAsync();

    var ok = await service.ActualizarEstadoAsync(r.Id, "Reserva Confirmada");
    Assert.True(ok);

    var actualizada = await context.Reservas.FindAsync(r.Id);
    Assert.Equal("Confirmada", actualizada!.Estado);
    Assert.Equal(20.00m, actualizada.MontoAbonado);
}

[Fact]
public async Task ActualizarNotas_GuardaTextoCorrectamente()
{
    var options = new DbContextOptionsBuilder<AppDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .Options;
    using var context = new AppDbContext(options);
    var service = new ReservaService(context);

    var r = new Reserva { Nombre = "Beatriz", Telefono = "988", Servicio = "Balayage", Fecha = DateOnly.FromDateTime(DateTime.Today), Horario = "Tarde 1", Notas = "Nota previa" };
    context.Reservas.Add(r);
    await context.SaveChangesAsync();

    var ok = await service.ActualizarNotasAsync(r.Id, "Tinte 7.1 + agua 20 vol");
    Assert.True(ok);

    var actualizada = await context.Reservas.FindAsync(r.Id);
    Assert.Equal("Tinte 7.1 + agua 20 vol", actualizada!.Notas);
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/TalitaKumiWebApp.Tests/TalitaKumiWebApp.Tests.csproj --filter "FullyQualifiedName~AdminReservaServiceTests"`
Expected: FAIL (`ActualizarEstadoAsync` / `ActualizarNotasAsync` not declared).

- [ ] **Step 3: Implement methods in `IReservaService` and `ReservaService`**

- `ObtenerReservasSemanaAsync`: filtra por `Fecha >= inicioSemana && Fecha <= finSemana` ordenado por Fecha y Horario.
- `ObtenerReservasFiltradasAsync`: aplica filtros opcionales de Fecha, búsqueda insensible a mayúsculas sobre Nombre/Teléfono y filtro de Estado.
- `ActualizarEstadoAsync`: normaliza cadenas ("Reserva Confirmada" -> "Confirmada", "Servicio Finalizado" -> "Completada", "Cita Cancelada" -> "Cancelada"). Si pasa a "Confirmada" y `MontoAbonado == 0`, asigna `MontoAbonado = 20.00m`.
- `ActualizarNotasAsync`: asigna `Notas` (hasta 500 caracteres).
- `CrearReservaManualAsync`: busca o crea el `Cliente`, calcula duración/precio base y guarda la `Reserva`.

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test tests/TalitaKumiWebApp.Tests/TalitaKumiWebApp.Tests.csproj --filter "FullyQualifiedName~AdminReservaServiceTests"`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add Services/IReservaService.cs Services/ReservaService.cs ViewModels/Admin/CrearReservaManualViewModel.cs tests/TalitaKumiWebApp.Tests/AdminReservaServiceTests.cs
git commit -m "feat(admin): implementar consultas de agenda, actualizacion de estado/notas y creacion manual en ReservaService"
```

---

### Task 5: Endpoints y Acciones de Dashboard en `AdminController`

**Files:**
- Create: `ViewModels/Admin/AdminDashboardViewModel.cs`
- Create: `ViewModels/Admin/ActualizarEstadoRequest.cs`
- Create: `ViewModels/Admin/ActualizarNotasRequest.cs`
- Modify: `Controllers/AdminController.cs`
- Test: `tests/TalitaKumiWebApp.Tests/AdminDashboardControllerTests.cs`

**Interfaces:**
- Consumes: `IReservaService`.
- Produces:
  - `[Authorize] [HttpGet("Admin")]` y `[HttpGet("Admin/Index")]` -> `AdminDashboardViewModel`.
  - `[Authorize] [HttpPost("Admin/ActualizarEstado")]` -> `JsonResult` `{ success = true, estado, montoAbonado }`.
  - `[Authorize] [HttpPost("Admin/ActualizarNotas")]` -> `JsonResult` `{ success = true, notas }`.
  - `[Authorize] [HttpPost("Admin/CrearReservaManual")]` -> redirect a `/Admin` con mensaje de éxito en TempData.

- [ ] **Step 1: Write the failing test**

```csharp
// tests/TalitaKumiWebApp.Tests/AdminDashboardControllerTests.cs
[Fact]
public async Task ActualizarEstado_EndpointPost_RetornaJsonConExito()
{
    var mockReserva = new Mock<IReservaService>();
    mockReserva.Setup(r => r.ActualizarEstadoAsync(5, "Reserva Confirmada")).ReturnsAsync(true);
    var controller = new AdminController(null!, mockReserva.Object);

    var req = new ActualizarEstadoRequest { Id = 5, Estado = "Reserva Confirmada" };
    var result = await controller.ActualizarEstado(req);

    var jsonResult = Assert.IsType<JsonResult>(result);
    dynamic data = jsonResult.Value!;
    Assert.True(data.success);
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/TalitaKumiWebApp.Tests/TalitaKumiWebApp.Tests.csproj --filter "FullyQualifiedName~AdminDashboardControllerTests"`
Expected: FAIL.

- [ ] **Step 3: Implement ViewModels and actions in `AdminController`**

- `AdminDashboardViewModel`: fecha seleccionada, inicio/fin de semana actual, lista de reservas semanales mapeadas por día/turno, lista de reservas filtradas para la tabla, lista de servicios disponibles para el modal manual.
- Implementar `Index`, `ActualizarEstado`, `ActualizarNotas` y `CrearReservaManual` decorados con `[Authorize]`.

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test tests/TalitaKumiWebApp.Tests/TalitaKumiWebApp.Tests.csproj --filter "FullyQualifiedName~AdminDashboardControllerTests"`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add ViewModels/Admin/AdminDashboardViewModel.cs ViewModels/Admin/ActualizarEstadoRequest.cs ViewModels/Admin/ActualizarNotasRequest.cs Controllers/AdminController.cs tests/TalitaKumiWebApp.Tests/AdminDashboardControllerTests.cs
git commit -m "feat(admin): implementar endpoints de dashboard, actualizacion asincrona y creacion manual"
```

---

### Task 6: Vistas Razor con Estética Clara y Cliente JavaScript

**Files:**
- Create: `Views/Shared/_AdminLayout.cshtml`
- Create: `Views/Admin/Index.cshtml`
- Create: `wwwroot/js/admin-dashboard.js`
- Modify: `Views/Shared/_Layout.cshtml` (enlace sutil en el footer hacia `/Admin`)

**Interfaces:**
- Consumes: `AdminDashboardViewModel`, paleta clara (`--color-cream` `#FAF6F0`, blanco `#FFFFFF`, koi `#E85D3F`, agua `#437694`).

- [ ] **Step 1: Create `_AdminLayout.cshtml` con Estética Clara**

Estructura luminosa:
- Barra superior blanca con logo koi, saludo "👋 Hola, Naomi", botón naranja koi **"➕ Agendar Cita Manual"** y botón discreto de cerrar sesión.
- Enlaces CSS a `site.css` para compartir la identidad tipográfica y paleta de la clienta.

- [ ] **Step 2: Create `Views/Admin/Index.cshtml`**

- **Sección Superior:** Calendario semanal interactivo con 6 columnas (Lun-Sáb) y 3 filas de turno (*Mañana 10:00-13:00*, *Tarde 1 13:00-16:30*, *Tarde 2 16:30-19:30*). Tarjetas con título de clienta, servicio, precio y distintivo de notas. Modal emergente al clic con enlace a WhatsApp.
- **Sección Inferior:** Tabla operativa de reservas:
  - Barra de búsqueda por texto y fecha.
  - Columna de **Notas / Observaciones** con `input` en línea y botón `💾 Guardar`.
  - Columna de **Estado** con `<select class="status-select">` directo (*🟡 Pendiente de Abono*, *🟢 Reserva Confirmada*, *🔵 Servicio Finalizado*, *🔴 Cita Cancelada*).
  - Botón de WhatsApp directo (`wa.me/51...`).
- **Modal de Agendamiento Manual:** Formulario para registrar nombre, teléfono, servicio, fecha, turno/hora, abono y notas.

- [ ] **Step 3: Create `wwwroot/js/admin-dashboard.js`**

- Manejador de evento `change` en los selectores de estado para invocar `fetch('/Admin/ActualizarEstado', { method: 'POST', ... })` con animación o toast de confirmación.
- Manejador de clic en `Guardar Nota` para invocar `fetch('/Admin/ActualizarNotas', ...)`.
- Lógica de apertura/cierre de modales (popup de calendario y modal de nueva cita manual).

- [ ] **Step 4: Verify build and compile**

Run: `dotnet build`
Expected: 0 errors.

- [ ] **Step 5: Commit**

```bash
git add Views/Shared/_AdminLayout.cshtml Views/Admin/Index.cshtml wwwroot/js/admin-dashboard.js Views/Shared/_Layout.cshtml
git commit -m "feat(admin): crear vistas del dashboard administrativo con estetica clara y JS interactivo"
```

---

### Task 7: Verificación Integral del Flujo Administrativo

**Files:**
- Create: `tests/TalitaKumiWebApp.Tests/AdminFlowIntegrationTests.cs`

**Interfaces:**
- Valida la integración completa del circuito de administración: autenticación -> visualización de calendario -> actualización de notas en línea -> cambio de estado vía selector -> creación de cita manual.

- [ ] **Step 1: Write integration tests**

```csharp
// tests/TalitaKumiWebApp.Tests/AdminFlowIntegrationTests.cs
[Fact]
public async Task FlujoCompleto_CrearCitaManual_CambiarEstadoAConfirmada_YActualizarNotas()
{
    var options = new DbContextOptionsBuilder<AppDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .Options;
    using var context = new AppDbContext(options);
    var servicio = new Servicio { Nombre = "Balayage Caramelo", Categoria = "Color", PrecioBase = 180m, DuracionMinutos = 180 };
    context.Servicios.Add(servicio);
    await context.SaveChangesAsync();

    var reservaService = new ReservaService(context);

    // 1. Crear cita manual
    var nuevaCita = await reservaService.CrearReservaManualAsync(new CrearReservaManualViewModel
    {
        Nombre = "Valeria Ramos",
        Telefono = "988776655",
        ServicioId = servicio.Id,
        Fecha = new DateOnly(2026, 10, 24),
        Horario = "Tarde 1 (13:00 a 16:30)",
        Precio = 180m,
        MontoAbonado = 0m,
        Notas = "Prueba de mecha requerida"
    });

    Assert.NotNull(nuevaCita);
    Assert.Equal("Pendiente", nuevaCita.Estado);

    // 2. Cambiar estado a Confirmada (debe asignar abono de S/. 20)
    await reservaService.ActualizarEstadoAsync(nuevaCita.Id, "Reserva Confirmada");
    var citaConfirmada = await context.Reservas.FindAsync(nuevaCita.Id);
    Assert.Equal("Confirmada", citaConfirmada!.Estado);
    Assert.Equal(20.00m, citaConfirmada.MontoAbonado);

    // 3. Actualizar notas in situ
    await reservaService.ActualizarNotasAsync(nuevaCita.Id, "Abonó S/. 20 Yape op 48291. Tono cenizo.");
    var citaConNota = await context.Reservas.FindAsync(nuevaCita.Id);
    Assert.Contains("op 48291", citaConNota!.Notas);

    // 4. Verificar que aparece en la semana
    var reservasSemana = await reservaService.ObtenerReservasSemanaAsync(new DateOnly(2026, 10, 19), new DateOnly(2026, 10, 25));
    Assert.Contains(reservasSemana, r => r.Id == nuevaCita.Id);
}
```

- [ ] **Step 2: Run all tests in project**

Run: `dotnet test tests/TalitaKumiWebApp.Tests/TalitaKumiWebApp.Tests.csproj`
Expected: ALL PASS.

- [ ] **Step 3: Commit**

```bash
git add tests/TalitaKumiWebApp.Tests/AdminFlowIntegrationTests.cs
git commit -m "test(admin): agregar pruebas de integracion del flujo administrativo completo"
```
