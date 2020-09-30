//Partially based on an Arduino UDP sample from the Arduino website. My changes / contributions are under the MIT License. The Arduino libraries are LGPL and available from the Arduino Github.  -DVN

#include <SPI.h>
#include <WiFiNINA.h>
#include <WiFiUdp.h>

/*
 * If the arduino_secrets.h file does not exist create it with the following contents
#ifndef ARDUINO_SECRETS_H
#define ARDUINO_SECRETS_H
#define _SSID "Goes Here"
#define _PASSWORD "Goes Here"
#endif
 * Just make sure to keep the SSID and Password out of source control
 */
#include "arduino_secrets.h"

///////enter your sensitive data in the Secret tab/arduino_secrets.h
char ssid[] = _SSID;        // your network SSID (name)
char pass[] = _PASSWORD;    // your network password (use for WPA, or use as key for WEP)
int keyIndex = 0;           // your network key Index number (needed only for WEP)

unsigned const int localPort = 32227;      //The Alpaca Discovery test port
unsigned const int alpacaPort = 4567;      //The (fake) port that the Alpaca API would be available on

char packetBuffer[255]; //buffer to hold incoming packet

WiFiUDP Udp;

void setup() {
  //Initialize serial and wait for port to open:
  Serial.begin(115200);
  /*while (!Serial) {
    ; // Can be useful for debug
  }*/

  // check for the WiFi module:
  if (WiFi.status() == WL_NO_MODULE) {
    Serial.println("Communication with WiFi module failed!");
    // don't continue
    while (true);
  }

  String fv = WiFi.firmwareVersion();
  if (fv < "1.0.0") {
    Serial.println("Please upgrade the firmware");
  }

  // attempt to connect to the Wifi network defined in arduino_secrets.h
  WiFi.begin(ssid, pass);
  
  while (WiFi.status() != WL_CONNECTED) {
    delay(500);
    Serial.print(".");
  }
  
  Serial.println("Connected to wifi");
  printWifiStatus();

  Serial.println("Listening for discovery requests...");
  
  Udp.begin(localPort);
}

void loop() {
  CheckForDiscovery();
}


void CheckForDiscovery() {
  // if there's data available, read a packet
  int packetSize = Udp.parsePacket();
  if (packetSize) {
    Serial.print("Received packet of size: ");
    Serial.println(packetSize);
    Serial.print("From ");
    IPAddress remoteIp = Udp.remoteIP();
    Serial.print(remoteIp);
    Serial.print(", on port ");
    Serial.println(Udp.remotePort());

    // read the packet into packetBufffer
    int len = Udp.read(packetBuffer, 255);
    if (len > 0) {
      //Ensure that it is null terminated
      packetBuffer[len] = 0;
    }
    Serial.print("Contents: ");
    Serial.println(packetBuffer);

    // No undersized packets allowed
    if (len < 16)
    {
      return;
    }

    if (strncmp("alpacadiscovery1", packetBuffer, 16) != 0)
    {
      return;
    }

    char response[36] = {0};

    sprintf(response, "{\"alpacaport\": %d}", alpacaPort);

    // send a reply, to the IP address and port that sent us the packet we received
    Udp.beginPacket(Udp.remoteIP(), Udp.remotePort());
    Udp.write(response);
    Udp.endPacket();
  }
}


void printWifiStatus() {
  // print the SSID of the network you're attached to:
  Serial.print("SSID: ");
  Serial.println(WiFi.SSID());

  // print your board's IP address:
  IPAddress ip = WiFi.localIP();
  Serial.print("IP Address: ");
  Serial.println(ip);

  // print the received signal strength:
  long rssi = WiFi.RSSI();
  Serial.print("signal strength (RSSI):");
  Serial.print(rssi);
  Serial.println(" dBm");
}
