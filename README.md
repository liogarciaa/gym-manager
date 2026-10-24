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
