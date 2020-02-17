# (c) 2019 Daniel Van Noord
# This code is licensed under MIT license (see License.txt for details)

import socket
port = 32227

device_address = ('0.0.0.0', port) #listen for any IP

# Create listening port
# ---------------------
sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
sock.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)  #share address
# sock.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEPORT, 1)  #needed on Linux and OSX to share port with net core. Remove on windows.

try:
    sock.bind(device_address)
except:
    print('failure to bind')
    sock.close()
    raise

AlpacaDiscovery = "alpacadiscovery1"
AlpacaResponse = "{\"alpacaport\": 4227}"

while True:
    data, addr = sock.recvfrom(1024)
    
    if AlpacaDiscovery in str(data, "ascii"):    
        sock.sendto(AlpacaResponse.encode(), addr)
                
