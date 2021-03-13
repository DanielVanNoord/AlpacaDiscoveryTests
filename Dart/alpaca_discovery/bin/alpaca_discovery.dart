import 'package:alpaca_discovery/alpaca_discovery.dart' as alpaca_discovery;

void main(List<String> arguments) async {
  final ipv4_responder = await alpaca_discovery.Responder.create_ipv4_responder();
  final ipv6_responder = await alpaca_discovery.Responder.create_ipv6_responder();
}
