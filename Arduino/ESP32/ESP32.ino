//This is an example that uses the Espressif arduino-esp32 libraries from https://github.com/espressif/arduino-esp32/
//It was tested on an Adafruit HUZZAH32 ESP32 feather and a Lolin D32 Pro
//Partially based on an Arduino UDP sample from the Espressif Repository.
//My changes / contributions are under the MIT License. The Arduino libraries are LGPL and available from the https://github.com/espressif/arduino-esp32/.  -DVN

#include "WiFi.h"
#include "AsyncUDP.h"

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
const char *ssid = _SSID;           // your network SSID (name)
const char *password = _PASSWORD;   // your network password (use for WPA, or use as key for WEP)

unsigned const int localPort = 32227;  //The Alpaca Discovery test port
unsigned const int alpacaPort = 4567;  //The (fake) port that the Alpaca API would be available on

AsyncUDP udp;

void setup()
{
    Serial.begin(115200);
    WiFi.mode(WIFI_STA);
    WiFi.begin(ssid, password);
    if (WiFi.waitForConnectResult() != WL_CONNECTED)
    {
        Serial.println("WiFi Failed");
        while (1)
        {
            delay(1000);
        }
    }
    Serial.print("Connect with IP Address: ");
    Serial.println(WiFi.localIP());
    
    if (udp.listen(32227))
    {
        Serial.println("Listening for discovery requests...");
        udp.onPacket([](AsyncUDPPacket packet) {
            Serial.print("Received UDP Packet of Type: ");
            Serial.print(packet.isBroadcast() ? "Broadcast" : packet.isMulticast() ? "Multicast" : "Unicast");
            Serial.print(", From: ");
            Serial.print(packet.remoteIP());
            Serial.print(":");
            Serial.print(packet.remotePort());
            Serial.print(", To: ");
            Serial.print(packet.localIP());
            Serial.print(":");
            Serial.print(packet.localPort());
            Serial.print(", Length: ");
            Serial.print(packet.length());
            Serial.print(", Data: ");
            Serial.write(packet.data(), packet.length());
            Serial.println();

            // No undersized packets allowed
            if (packet.length() < 16)
            {
                return;
            }

            //Compare packet to Alpaca Discovery string
            if (strncmp("alpacadiscovery1", (char *)packet.data(), 16) != 0)
            {
                return;
            }

            // send a reply, to the IP address and port that sent us the packet we received
            // on a real system this would be the port the Alpaca API was on
            packet.printf("{\"alpacaport\": %d}", alpacaPort);
        });
    }
}

void loop()
{
    //Run the program
}
