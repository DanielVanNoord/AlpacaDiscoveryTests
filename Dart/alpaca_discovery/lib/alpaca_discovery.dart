import 'dart:convert';
import 'dart:io';
import 'dart:typed_data';

final String discoveryMessage = 'alpacadiscovery1';
final String response = '{"AlpacaPort": 9823}';
final String multicast_group = 'ff12::00a1:9aca';

bool is_discovery_message(String message) {
  return message.contains(discoveryMessage);
}

class Responder {
  static void _handle_response(RawDatagramSocket udpSocket) {
    udpSocket.forEach((RawSocketEvent event) {
      if (event == RawSocketEvent.read) {
        final dg = udpSocket.receive();
        print('responder, ' + utf8.decode(dg.data));
        print('responder, ' + dg.address.toString());
        print('responder, ' + dg.port.toString());

        if (is_discovery_message(utf8.decode(dg.data))) {
          udpSocket.send(utf8.encode(response), dg.address, dg.port);
        }
      }
    });
  }

  static Future<RawDatagramSocket> create_ipv4_responder() {
    if (Platform.isWindows) {
      final responder = RawDatagramSocket.bind(InternetAddress.anyIPv4, 32227,
              reuseAddress: true)
          .then((RawDatagramSocket udpSocket) {
        _handle_response(udpSocket);
      });
      return responder;
    } else {
      final responder = RawDatagramSocket.bind(InternetAddress.anyIPv4, 32227,
              reuseAddress: true, reusePort: true)
          .then((RawDatagramSocket udpSocket) {
        _handle_response(udpSocket);
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
        _handle_response(udpSocket);
      });
      return responder;
    } else {
      final responder = RawDatagramSocket.bind(InternetAddress.anyIPv6, 32227,
              reuseAddress: true, reusePort: true)
          .then((RawDatagramSocket udpSocket) {
        udpSocket.joinMulticast(InternetAddress(multicast_group));
        _handle_response(udpSocket);
      });
      return responder;
    }
  }
}

class DeviceEndPoint {
  InternetAddress endpoint_address;
  int endpoint_port;

  InternetAddress get address {
    return endpoint_address;
  }

  set address(InternetAddress value) {
    endpoint_address = value;
  }

  int get port {
    return endpoint_port;
  }

  set port(int value) {
    endpoint_port = value;
  }

  DeviceEndPoint(InternetAddress address, int port) {
    endpoint_address = address;
    endpoint_port = port;
  }
}

class Finder {
  List<DeviceEndPoint> Found_Devices = [];

  void search_ipv4() {
    RawDatagramSocket.bind(InternetAddress.anyIPv4, 0)
        .then((RawDatagramSocket udpSocket) {
      udpSocket.broadcastEnabled = true;

      _handle_finder_response(udpSocket);

      if (NetworkInterface.listSupported) {
        NetworkInterface.list(type: InternetAddressType.IPv4).then((value) => {
              value.forEach((element) {
                element.addresses.forEach((address) {
                  udpSocket.send(utf8.encode(discoveryMessage),
                      _get_broadcast_address(address), 32227);
                });
              })
            });
      } else {
        udpSocket.send(utf8.encode(discoveryMessage),
            InternetAddress('255.255.255.255'), 32227);
      }
    });
  }

  void search_ipv6() {
    RawDatagramSocket.bind(InternetAddress.anyIPv6, 0)
        .then((RawDatagramSocket udpSocket) {
      _handle_finder_response(udpSocket);

      udpSocket.send(utf8.encode(discoveryMessage),
          InternetAddress('ff12::00a1:9aca'), 32227);
    });
  }

  void _handle_finder_response(RawDatagramSocket udpSocket) {
    udpSocket.forEach((RawSocketEvent event) {
      if (event == RawSocketEvent.read) {
        final dg = udpSocket.receive();
        final data = utf8.decode(dg.data);
        print('Finder, ' + data);
        print('Finder, ' + dg.address.toString());

        if (data.contains('AlpacaPort')) {
          var decoded = json.decode(data);

          int port = (decoded as Map)['AlpacaPort'];
          Found_Devices.add(DeviceEndPoint(dg.address, port));
        }
      }
    });
  }

  InternetAddress _get_broadcast_address(InternetAddress address) {
    //Really basic, assumes a mask of 255.255.255.0
    return InternetAddress.fromRawAddress(Uint8List.fromList([
      address.rawAddress[0],
      address.rawAddress[1],
      address.rawAddress[2],
      255
    ]));
  }
}
