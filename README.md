# Alpaca Discovery Tests

This is an experimental discovery protocol for the new ASCOM Alpaca platform. It is based on UDP and is designed to be as light weight and easy to implement as possible. It uses a known broadcast port and a known message / response. This protocol is only meant to enable clients to find Alpaca Servers. All information about the Server can then be retrieved via the management API.

For the following document Server will refer to something (a driver or device) that exposes the Alpaca interface and Client will refer to client applications that want to locate and use the Server's API(s).  

In order to implement this protocol all a Client needs is the ability to send a UDP broadcast and be able to receive a direct response. All the Server needs to be able to do is to receive broadcast UDP messages and to respond directly to the message. See below for a more formal specification.

 Once the IP Address and Alpaca port of the Server is known the Client can use the management API to query the Server and discover its supported endpoints and functions.

 For testing this uses port 32227 for the broadcasts.

# Examples

This repository contains examples for several platforms and languages of both the Client and Server protocol. Both the Client and the Server protocol are designed to run on microcotrollers, any OS, or any language that offers UDP features. 

All of these examples have been tested on a variety of networks and operating systems and have consistently worked. If anyone finds differently please let me know. Note, before claiming that these don't work please make sure that you have a valid network route between your devices and that no firewall can interfere.

Note that this is a UDP protocol, packets can be lost or dropped. If you can't find a Server try sending the request again.

## Alpaca8266

This is an example of the Server protocol that runs on several ESP8266 boards. I have tested this on the Adafruit Feather Huzzah 8266 (https://www.adafruit.com/product/2821) and on a generic NodeMCU 1.0 ESP12-E (mine was made by LoLin and was version 3.0). It uses the Arduino Libraries and IDE (https://www.arduino.cc/en/main/software). You can follow the normal method of adding support for the ESP8266 (Adafruit has a good tutorial). Make sure to set your SSID and Password in the arduino_secrets file.

After this is flashed on a board it will print any received requests via the board serial port and respond to the Client with the response message. You can view the serial data through the Arduino Serial Monitor.

## AlpacaNina

This is an example of the Server protocol that runs on the Arduino MKR 1010, which uses the ESP32 for its WiFi support (https://store.arduino.cc/usa/mkr-wifi-1010). It also uses the Arduino Libraries and IDE.

After this is flashed on a board it will print any received requests via the board serial port and respond to the Client with the response message. You can view the serial data through the Arduino Serial Monitor.

## CServer

This is a simple example C Server and Client that runs on Linux. It was tested on Ubuntu 18.04, Manjaro (Arch) and Raspbian. Both require gcc and binutils to build. To build simply run "gcc -o client client.c" and "gcc -o server server.c". To start simply run the output program in a terminal. The Server will listen for any discovery packets and print what it receives to the terminal. It will then respond. The client will send out the discovery request and print any response. 

Note that the C Client currently sends the discovery broadcast to the 255.255.255.255 address. This does not work as well as sending broadcasts to each network adaptor's broadcast address as some adaptors and networking gear may not forward generic packets.

## Net 

This is an example .Net Server and Client. This includes a .Net Standard 2.0 and .Net Framework 3.5 library that implements the protocols as well as several runtimes. The Net 3.5 was tested on Windows 7 and 10 and the Net Standard was tested via Net Core 2.0 on Windows, Linux (Ubuntu, Manjaro and Raspbian) and OSX. It was also tested on Android and IOS (example apps coming soon) via Xamarin. 

This Client example iterates over all network adapters on the system and sends the request to each broadcast address that it finds. This works much better then sending a single generic broadcast. 

These can be built with Visual Studio 2017 Community or via the dotnet commandline tools. To publish a dotnet core example for a different platform use the dotnet publish command (https://docs.microsoft.com/en-us/dotnet/core/tools/dotnet-publish?tabs=netcore21). For example running the following command in the core Client or Server folder will create a Self Contained Deployment in a folder named linux for Linux x64: `dotnet publish --self-contained --
runtime linux-x64 -o linux`. See https://docs.microsoft.com/en-us/dotnet/core/rid-catalog for target platforms.

## Python
This is an example Python 3 Client and Server. This requires the netifaces package for the Client. This can be installed with `pip (or pip3) install netifaces`. This was tested on  Windows, Linux (Ubuntu, Manjaro and Raspbian) and OSX. They can be run with the normal Python command.

There is one line in the server that must be removed on Windows and included for Linux or OSX. This handles a difference in the socket API across the different platforms. See the file for details.

This Client iterates over all network adapters on the system and sends the request to each broadcast address that it finds. This works much better then sending a single generic broadcast. 

# Specification

### Notes

This is a (rough!) draft specification. The final protocol and strings may be different. Or we may go with a different approach entirely.

There are several working samples included in this repository. These should help to reduce any ambiguity with this standard. All there servers were tested at the same time to ensure that the broadcast port could be shared.

There are several open questions about this specification. First we need to choose a final port number. Second we need to choose the final discovery and response messages. Additionally we need to decide if the messages should be versioned.

There are likely other issues not yet included.

### Basic flow

A Client broadcasts a UDP packet containing the discovery message on the discovery port. Servers listening for broadcasts on this port respond directly to the Client with the response message. Clients pull the Alpaca API port from the message and then can query the management API for details about the server.

### Definitions

For the following document Server will refer to something (a driver or device) that exposes the Alpaca interface and Client will refer to client applications that want to locate and use the Server's API(s). 

PORT (Alpaca Discovery Port): this is the port that the Client Broadcasts the discovery message on and the Server listens on. The Client should not listen on this port. Rather it should listen on a system assigned port. Likewise the Server should not respond using port. It should respond on a system assigned port. For the test I have chosen port 32227. This falls outside of the IANA Ephemeral Port range and was not use by any registered protocols that I could find. The final port may change and may be registered by us with IANA.

DISCOVERY: this is the message that is sent by the client via broadcast on the PORT. Currently this message is simply *alpaca discovery*. For example in C the message could be defined as `char* DISCOVERY = "alpaca discovery";`

RESPONSE: this is the message that the server sends back via unicast to the client. This message include the port that the Alpaca API is available on the server. The current message is *alpaca here:port* where port is the port number of the Alpaca API. For example in c this could be set to a char* with `sprintf(response, "alpaca here:%d", AlpacaPortNumber);`

### Specification (work in progress)

Servers and Clients MUST come configured to use the PORT for broadcasts by default. Servers and Clients MUST provide a mechanism for the end user to change the used PORT if this is required on their network.

Servers must be configured so that they can share the PORT with other Servers in a shared context. This means that Servers MUST open the socket with the language equivalent of SO_REUSEADDR on Windows and the language equivalents of SO_REUSEADDR and SO_REUSEPORT on Linux / OSX. Servers in a shared context MUST NOT require or use root access so that other Servers can also use the port.

Clients MUST NOT bind their socket to the PORT. Clients SHOULD bind their socket to a system assigned port. Clients SHOULD broadcast the DISCOVERY message to the unique broadcast address of each network interface rather then to the generic broadcast address. This maximizes compatibility with various networking gear and helps to ensure that the packets are routed correctly. 

Clients MAY broadcast the DISCOVERY message multiple times as this is a UDP based protocol and packets may be lost. Clients SHOULD combine the responses to remove duplicate responses. Servers SHOULD respond to each request, although they SHOULD rate limit responses to prevent UDP amplification attacks. In addition Servers SHOULD NOT be open to the Internet and SHOULD ONLY respond to trusted IP addresses.

Servers MUST respond to the Client DISCOVERY request with the RESPONSE message containing the Servers Alpaca Port. This response MUST occur via unicast and be directed to the port and IP address that the client sent the DISCOVERY message from. Clients then may use the port specified in the RESPONSE message to query the Alpaca API on the Server for details about the features it provides.

# How to help?
You can help by testing these examples on your networks to look for problems. Second you can leave comments, here or on the ASCOM forum. Also pull requests to make improvements to the examples / specification are welcome. Finally more examples are very welcome. I would like to add a Windows C example as well as Java, Node.js and more microcontrollers / microcontroller platforms. Also any other languages or frameworks are welcome.

Once the protocol is finalized we will need reference implementations and libraries for many languages and platforms. We will also need test code to ensure that both Server and Client libraries perform correctly and will work in shared contexts.


Note that all the code in this repo is under the MIT license. Anything you submit must be under the same license or not be under copyright. 



