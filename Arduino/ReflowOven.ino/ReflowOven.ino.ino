#include <SoftwareSerial.h>
//#include <ArduinoJson.h>

SoftwareSerial Bluetooth(9, 10);

void setup() 
{
  Serial.begin(9600);

  Bluetooth.begin(19200);
}

void loop() 
{
  
}
