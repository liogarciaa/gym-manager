# Controlador de puerta GymManager

Este programa es para un Arduino Nano V3 con adaptador USB-serie CH340 (VID `1A86`, PID `7523`) y un módulo de relé de 5 V. GymManager busca la placa por su identificador USB y verifica el firmware con `IDENTIFY?`. Cuando autoriza un ingreso, envía `OPEN`.

## Cargar el firmware

1. Abre `GymManagerDoorController.ino` con Arduino IDE.
2. Selecciona la placa Arduino Nano y el procesador/bootloader que corresponda a tu clon. Si falla la carga, prueba `ATmega328P (Old Bootloader)`.
3. Pulsa **Subir**. El programa queda guardado en el Nano; no hace falta dejar Arduino IDE abierto.
4. Antes de conectar la cerradura, verifica si la entrada del módulo de relé es activa en bajo o en alto. El sketch viene configurado para activa en bajo. Si tu módulo es activo en alto, cambia `RELAY_ACTIVE_LEVEL` a `HIGH` y vuelve a cargar el sketch.

El relé recibe un pulso máximo de 3 segundos. Si llega otra orden mientras está activado, responde `BUSY` y no extiende el pulso.

## Orden de prueba

Haz estas pruebas **después de cargar el firmware y antes de conectar la cerradura o su fuente de 12 V**:

1. Conecta el Nano por USB y conecta solo el módulo de relé de 5 V, respetando sus pines `IN`, `GND` y `VCC`.
2. Acerca un llavero de prueba asignado a un socio activo que no tenga un ingreso registrado en las últimas 2 horas. Comprueba que GymManager indique el controlador conectado y, al autorizar, que el relé haga clic y su LED cambie de estado durante unos 3 segundos. El anti-passback actual bloqueará una segunda prueba inmediata con el mismo socio.
3. Comprueba con un multímetro en continuidad, sin conectarlo a ninguna fuente, que `COM` y `NO` se conecten durante el pulso y vuelvan a abrirse después. El relé puede hacer clic aunque no haya cerradura conectada.
4. Prueba también una credencial no asignada o un socio vencido: no debe activarse el relé.

Si la prueba sale bien, el instalador puede conectar la cerradura con su fuente independiente, una vez confirmados el tipo de cerradura, su corriente y el contacto `NO`/`NC` correcto. **No conectes los 12 V al Nano, al USB ni al lado de control del módulo.** La salida desde adentro debe funcionar independientemente de la PC y de este firmware.

## Conexiones de control

- Pin digital 7 del Nano -> `IN` del módulo de relé.
- `GND` del Nano -> `GND` de control del módulo.
- `VCC` del módulo -> fuente regulada de 5 V adecuada para el módulo.
- La fuente de 12 V de la cerradura se conmuta por los contactos `COM` y `NO` o `NC`; la selección depende del tipo de cerradura y debe definirla el instalador.

Confirma la polaridad de entrada, el aislamiento y la capacidad del contacto para la carga inductiva en corriente continua de la cerradura; no te guíes solo por la cifra de amperes impresa en el relé.

## Protocolo serial

El Nano se comunica a 9600 baudios, 8-N-1. Cada comando termina con una nueva línea:

- `IDENTIFY?` -> `GYM-DOOR/1`
- `OPEN` -> `OPENING`, o `BUSY` si ya hay un pulso activo

El pulso termina automáticamente a los 3000 ms, aunque GymManager se cierre.
