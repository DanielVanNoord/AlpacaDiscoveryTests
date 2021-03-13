import 'package:alpaca_discovery/alpaca_discovery.dart';
import 'package:test/test.dart';

void main() {
  test('message', () {
    expect(is_discovery_message('alpacadiscovery1'), true);
    expect(is_discovery_message('alpacadiscovery2'), false);
  });
}
