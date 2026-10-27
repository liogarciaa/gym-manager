#include <string.h>

const byte RELAY_PIN = 7;
const byte RELAY_ACTIVE_LEVEL = LOW;
const unsigned long RELAY_PULSE_MS = 3000;
const byte COMMAND_BUFFER_SIZE = 24;

char commandBuffer[COMMAND_BUFFER_SIZE];
byte commandLength = 0;
bool relayIsActive = false;
unsigned long relayActivatedAt = 0;

void setRelay(bool active) {
  digitalWrite(RELAY_PIN, active ? RELAY_ACTIVE_LEVEL : !RELAY_ACTIVE_LEVEL);
  relayIsActive = active;
  if (active) {
    relayActivatedAt = millis();
  }
}

void processCommand() {
  commandBuffer[commandLength] = '\0';

  if (strcmp(commandBuffer, "IDENTIFY?") == 0) {
    Serial.println("GYM-DOOR/1");
  } else if (strcmp(commandBuffer, "OPEN") == 0) {
    if (relayIsActive) {
      Serial.println("BUSY");
    } else {
      setRelay(true);
      Serial.println("OPENING");
    }
  } else if (commandLength > 0) {
    Serial.println("ERR");
  }

  commandLength = 0;
}

void setup() {
  digitalWrite(RELAY_PIN, !RELAY_ACTIVE_LEVEL);
  pinMode(RELAY_PIN, OUTPUT);
  Serial.begin(9600);
}

void loop() {
  while (Serial.available() > 0) {
    char incoming = static_cast<char>(Serial.read());

    if (incoming == '\n' || incoming == '\r') {
      processCommand();
    } else if (commandLength < COMMAND_BUFFER_SIZE - 1) {
      commandBuffer[commandLength++] = incoming;
    } else {
      commandLength = 0;
    }
  }

  if (relayIsActive && millis() - relayActivatedAt >= RELAY_PULSE_MS) {
    setRelay(false);
  }
}
