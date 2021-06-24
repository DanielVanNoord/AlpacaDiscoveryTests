//Partially based on an Arduino UDP sample from the Arduino website. My changes / contributions are under the MIT License. The Arduino libraries are LGPL and available from the Arduino Github.  -DVN


#include <SPI.h>         
#include <Ethernet.h>
#include <EthernetUdp.h>  // UDP library from: bjoern@cs.stanford.edu 12/30/2008


// Enter a MAC address, some models of the shield do not have one
byte mac[] = {  
  0xDE, 0xAD, 0xBE, 0xEF, 0xFE, 0xED };

unsigned const int localPort = 32227;      //The Alpaca Discovery test port
unsigned const int alpacaPort = 4567;      //The (fake) port that the Alpaca API would be available on

char packetBuffer[255]; //buffer to hold incoming packet

// An EthernetUDP instance to let us send and receive packets over UDP
EthernetUDP Udp;

void setup() {
  // start the Ethernet, use dhcp
  Ethernet.begin(mac);

  Udp.begin(localPort);

  Serial.begin(115200);

  Serial.println(Ethernet.localIP());
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

    sprintf(response, "{\"AlpacaPort\": %d}", alpacaPort);

    // send a reply, to the IP address and port that sent us the packet we received
    Udp.beginPacket(Udp.remoteIP(), Udp.remotePort());
    Udp.write(response);
    Udp.endPacket();
  }
}
