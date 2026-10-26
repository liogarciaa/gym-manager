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

## Licenciamiento Criptográfico & Distribución

El sistema implementa un modelo de **licencia permanente y ejecutable offline**, diseñado bajo arquitectura *Zero-Trust* para evitar dependencias con servidores externos de validación y garantizar operatividad continua en el mostrador del gimnasio.

### Esquema de Protección & Firma Criptográfica
* **Enlazado a Hardware (Hardware-Bound):** La licencia se genera a partir del identificador único del equipo (`MachineGuid` de Windows).
* **Firma Asimétrica (RSA / ECDSA):** 
  * Las licencias (`.gymlicense`) se firman digitalmente con una **clave privada** conservada exclusivamente en el perfil del emisor.
  * La aplicación compilada contiene únicamente la **clave pública** del proveedor, utilizada para verificar matemáticamente la integridad del archivo y la pertenencia a la PC cliente.
* **Módulos Granulares de Permisos:** El archivo de licencia define las capacidades habilitadas para el cliente (ej. activación del módulo de hardware NFC/RFID). Un cliente puede actualizar sus permisos importando una nueva licencia firmada sin necesidad de reinstalar la aplicación ni alterar la base de datos.
* **Persistencia Aislada:** La licencia validada se almacena en `%LOCALAPPDATA%\GymManager\license.gymlicense`, manteniéndose intacta ante actualizaciones del sistema.

---

## Empaquetado & Despliegue (Inno Setup)

Para el despliegue comercial en entornos de cliente final, el software se distribuye mediante un **instalador ejecutable autocontenido** generado con **Inno Setup 6**:

* **Aislamiento de Permisos en Windows:** El ejecutable y las dependencias compiladas en modo *Release* (`win-x64 Self-Contained`) se alojan en `C:\Program Files\GymManager\`.
* **Persistencia de Datos Protegida:** La base de datos activa (`GymManager.db`) y la licencia conviven en `%LOCALAPPDATA%\GymManager\`. De esta forma, el sistema no requiere elevación de privilegios de administrador para operaciones diarias de escritura en SQLite.
* **Ciclo de Actualización Seguro:** Las tareas de instalación y desinstalación preservan la carpeta de datos del usuario, garantizando que el historial de socios, cobros y la licencia de uso no sufran pérdida de información al actualizar la versión del software.

---

## Página Web-Demo
* https://liogarciaa.github.io/gym-manager/

---
