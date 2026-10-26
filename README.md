# GymManager — Sistema de Gestión Comercial & Control de Acceso Hardware

> Aplicación de escritorio desarrollada en **C# / WPF** orientada a la gestión integral de gimnasios, automatización de cobros y control de accesos por hardware mediante arquitectura **Offline-First**.

---

## Problemática de Negocio
Muchas PyMEs y centros deportivos sufren interrupciones operativas debido a caídas de conectividad a Internet o complejidad en la supervisión manual de accesos. **GymManager** resuelve esto permitiendo:
* **Autonomía Operativa (Offline-First):** Operación 100% local sin costo de servidor ni dependencia de conexión externa.
* **Control Físico & Auditoría:** Integración con lectores RFID y módulos de relé USB para destrabar puertas/electroimanes únicamente a socios con cuota al día.
* **Adaptabilidad de UI:** Renderizado vectorial adaptable a monitores antiguos o de baja resolución (1024x768).

---

## Stack Tecnológico
* **Lenguaje:** C# (.NET Core / .NET 8)
* **Interfaz Gráfica:** WPF (Windows Presentation Foundation) con XAML y patrón **MVVM**
* **Base de Datos:** SQLite local con Entity Framework Core
* **Integración de Hardware:** `System.IO.Ports` para comunicación por puerto serie (COM) con placas relé USB

---

## Integración con Hardware
```text
[ Socio aproxima Llavero RFID 125kHz ]
                 │
                 ▼ (Modo Emulador de Teclado / USB)
[ Aplicación WPF (GymManager) ] ──► [ Consulta en SQLite local ]
                 │
                 ├─► Cuota al día / En tolerancia ──► [ Envía comando HEX a Puerto COM ] ──► [ Relé USB se activa ] ──► [ Electroimán abre 3s ]
                 └─► Cuota vencida ───────────────► [ Emite alerta sonora/visual ] ──► [ Puerta permanece trabada ]
```

---

## Página Web-Demo
* https://liogarciaa.github.io/gym-manager/

## Licencias por instalación

Cada licencia es permanente, queda ligada a una PC y puede habilitar NFC. La aplicación lleva solo la clave pública; la clave privada para emitir licencias se conserva cifrada en el perfil local del proveedor y nunca debe subirse al repositorio.

La primera vez, desde la raíz del repositorio, inicializá el par de claves:

```powershell
dotnet run --project GymManager.LicenseIssuer/GymManager.LicenseIssuer.csproj -- init-keys
```

La herramienta pide una frase secreta y guarda la clave privada cifrada en `%LOCALAPPDATA%\GymManager\LicenseIssuer\private-key.pem`. Guardá una copia segura de ese archivo y de la frase secreta por separado. La clave pública queda en `GymManager.Licensing/TrustedIssuerPublicKey.pem`; compilá y distribuí la aplicación después de inicializarla.

Para emitir una licencia, el cliente debe enviarte el identificador que aparece en la pantalla de activación. Ejecutá:

```powershell
dotnet run --project GymManager.LicenseIssuer/GymManager.LicenseIssuer.csproj -- issue
```

La herramienta solicita el gimnasio, el identificador de instalación, si incluye NFC y la frase secreta. Guarda el archivo firmado en `Documentos\GymManagerLicenses`. El cliente lo importa desde la pantalla de activación o desde Configuración.

Las licencias no vencen. Si el cliente cambia de PC, hay que emitirle otra con el nuevo identificador. Para agregar NFC después de la compra, se emite otra licencia para el mismo identificador con NFC habilitado. Perder la clave privada impide emitir o renovar licencias con la clave pública ya distribuida.

## Datos locales e instalador

La base de datos activa se guarda en `%LOCALAPPDATA%\GymManager\GymManager.db`, no junto al ejecutable. Esto permite instalar la aplicación en `Program Files` sin necesitar permisos de escritura en esa carpeta. La licencia también vive en `%LOCALAPPDATA%\GymManager`.

Si esa base aún no existe, el primer inicio busca una `GymManager.db` anterior junto al ejecutable o en el directorio de trabajo y la copia con SQLite a la carpeta nueva. El original no se elimina. Los backups automáticos, la copia manual y la restauración usan la base de `%LOCALAPPDATA%`.

Después de copiarla, GymManager comprueba las tablas y columnas esperadas y agrega las que falten dentro de una transacción, con valores compatibles para registros existentes. No se usa `Database.Migrate()` porque las versiones antiguas fueron creadas con `EnsureCreated()` y no tienen un historial EF confiable.

Cada perfil de Windows tiene su propia carpeta `LocalAppData` y, por lo tanto, su propia base y licencia. Al actualizar con Inno Setup, no agregues una acción que borre `%LOCALAPPDATA%\GymManager`; ahí quedan los datos y la licencia del cliente.

## Generar instalador con Inno Setup

Instalá Inno Setup 6 en la PC de desarrollo. Desde la raíz del repositorio, publicá GymManager en Release y en modo autocontenido:

```powershell
dotnet publish .\GymManager.csproj -c Release -r win-x64 --self-contained true -p:UseAppHost=true -o .\artifacts\publish\win-x64
```

Después compilá el instalador:

```powershell
& "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe" .\GymManager.iss
```

El instalador queda en `artifacts\installer`. El script instala los archivos publicados bajo `Program Files` y crea accesos directos. No incluyas la base de desarrollo, archivos `.gymlicense`, la herramienta emisora ni la clave privada. La licencia se entrega por separado a cada cliente. Al actualizar la versión, incrementá `AppVersion` en `GymManager.iss`; no agregues acciones de Inno que borren `%LOCALAPPDATA%\GymManager` al actualizar o desinstalar.
