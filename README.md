# Alpaca Discovery Tests

This is an experimental discovery protocol for the new ASCOM Alpaca platform. It is based on UDP and is designed to be as light weight and easy to implement as possible. It uses a known broadcast port and a known message / response.

For the following document Server will refer to something (a driver or device) that exposes the Alpaca interface and Client will refer to client applications that want to locate and use the Server's device(s).  

In order to implement this protocol all a Client needs is the ability to send a UDP broadcast and be able to receive a direct response. All the Server needs to be able to do is to receive broadcast UDP messages and to respond directly to the message. See below (coming soon) for a more formal specification.

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

These can be built with Visual Studio 2017 Community or via the dotnet commandline tools. To publish a dotnet core example for a different platform use the dotnet publish command (https://docs.microsoft.com/en-us/dotnet/core/tools/dotnet-publish?tabs=netcore21). For example running the following command in the core Client or Server folder will create a Self Contained Deployment in a folder named linux for Linux x64: "dotnet publish --self-contained --
runtime linux-x64 -o linux". See https://docs.microsoft.com/en-us/dotnet/core/rid-catalog for target platforms.

## Python
This is an example Python 3 Client and Server. This requires the netifaces package for the Client. This can be installed with pip (or pip3) install netifaces. This was tested on  Windows, Linux (Ubuntu, Manjaro and Raspbian) and OSX. They can be run with the normal Python command.

There is one line in the server that must be removed on Windows and included for Linux or OSX. See the file for details.

This Client iterates over all network adapters on the system and sends the request to each broadcast address that it finds. This works much better then sending a single generic broadcast. 

# Specification
Work in progress, coming soon

Servers and Clients MUST come configured to use the Aplaca Discovery Port for broadcasts by default. Servers and Clients MUST provide a mechanism for the end user to change the used Aplaca Discovery Port.

Servers MUST open the socket with SO_REUSEADDR on Windows and SO_REUSEADDR and SO_REUSEPORT on Linux / OSX. Servers in a shared context MUST NOT require or use root access so that other Servers can also use the port.


# How to help?
First you can help by testing these examples on your networks to look for problems. Second you can leave comments, here or on the ASCOM forum. Also pull requests to make improvements to the examples / specification are welcome. Finally more examples are very welcome. I would like to add a Windows C example as well as Java, Node.js and more microcontrollers / microcontroller platforms. Also any other languages or frameworks are welcome.

Note that all the code in this repo is under the MIT license. Anything you submit must be under the same license or not be under copyright. 



