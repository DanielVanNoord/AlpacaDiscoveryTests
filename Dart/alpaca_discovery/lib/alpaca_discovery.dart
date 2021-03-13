import 'dart:convert';
import 'dart:io';

final String discoveryMessage = 'alpacadiscovery1';
final String response = '{"alpacaport": 9823}';
final String multicast_group = 'ff12::00a1:9aca';

bool is_discovery_message(String message) {
  return message.contains(discoveryMessage);
}

class Responder {
  static Future<RawDatagramSocket> create_ipv4_responder() {
    if (Platform.isWindows) {
      final responder = RawDatagramSocket.bind(InternetAddress.anyIPv4, 32227,
              reuseAddress: true)
          .then((RawDatagramSocket udpSocket) {
        udpSocket.forEach((RawSocketEvent event) {
          if (event == RawSocketEvent.read) {
            final dg = udpSocket.receive();
            print(utf8.decode(dg.data));
            print(dg.address.toString());
            print(dg.port.toString());

            if (is_discovery_message(utf8.decode(dg.data))) {
              udpSocket.send(utf8.encode(response), dg.address, dg.port);
            }
          }
        });
      });
      return responder;
    } else {
      final responder = RawDatagramSocket.bind(InternetAddress.anyIPv4, 32227,
              reuseAddress: true, reusePort: true)
          .then((RawDatagramSocket udpSocket) {
        udpSocket.forEach((RawSocketEvent event) {
          if (event == RawSocketEvent.read) {
            final dg = udpSocket.receive();
            print(utf8.decode(dg.data));
            print(dg.address.toString());
            print(dg.port.toString());

            if (is_discovery_message(utf8.decode(dg.data))) {
              udpSocket.send(utf8.encode(response), dg.address, dg.port);
            }
          }
        });
      });
      return responder;
    }
  }

  static Future<RawDatagramSocket> create_ipv6_responder() {
    if (Platform.isWindows) {
      final responder = RawDatagramSocket.bind(InternetAddress.anyIPv6, 32227,
              reuseAddress: true)
          .then((RawDatagramSocket udpSocket) {
        udpSocket.joinMulticast(InternetAddress(multicast_group));
        udpSocket.forEach((RawSocketEvent event) {
          if (event == RawSocketEvent.read) {
            final dg = udpSocket.receive();
            print(utf8.decode(dg.data));
            print(dg.address.toString());
            print(dg.port.toString());

            if (is_discovery_message(utf8.decode(dg.data))) {
              udpSocket.send(utf8.encode(response), dg.address, dg.port);
            }
          }
        });
      });
      return responder;
    } else {
      final responder = RawDatagramSocket.bind(InternetAddress.anyIPv6, 32227,
              reuseAddress: true, reusePort: true)
          .then((RawDatagramSocket udpSocket) {
        udpSocket.joinMulticast(InternetAddress(multicast_group));
        udpSocket.forEach((RawSocketEvent event) {
          if (event == RawSocketEvent.read) {
            final dg = udpSocket.receive();
            print(utf8.decode(dg.data));
            print(dg.address.toString());
            print(dg.port.toString());

            if (is_discovery_message(utf8.decode(dg.data))) {
              udpSocket.send(utf8.encode(response), dg.address, dg.port);
            }
          }
        });
      });
      return responder;
    }
  }
}

