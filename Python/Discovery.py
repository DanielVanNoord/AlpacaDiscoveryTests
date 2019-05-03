# (c) 2019 Daniel Van Noord
# This code is licensed under MIT license (see License.txt for details)

import socket
port = 32227

server_address = ('255.255.255.255', port) #broadcast to discovery port

# Create listening port
# ---------------------
sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
sock.setsockopt(socket.SOL_SOCKET, socket.SO_BROADCAST, 1) 

try:
    sock.bind(('0.0.0.0', 0)) #listen to any on a temporary port
except:
    print('failure to bind')
    sock.close()
    raise

AlpacaDiscovery = "alpaca discovery"
AlpacaResponse = "alpaca here"

sock.sendto(AlpacaDiscovery.encode(), server_address)

while True:
    data, addr = sock.recvfrom(1024) # buffer size is 1024 bytes
    
    if AlpacaResponse in str(data, "ascii"):    
        print(addr[0], ":",  str.split(data.decode("ascii"),':')[1])