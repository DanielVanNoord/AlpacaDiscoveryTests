# (c) 2019 Daniel Van Noord
# This code is licensed under MIT license (see License.txt for details)
import os
import ipaddress
import socket
import struct
from threading import Thread

port = 32227

AlpacaDiscovery = "alpacadiscovery1"
AlpacaResponse = "{\"alpacaport\": 4227}"

def respond_ipv4():
    # Create listening port
    # ---------------------
    sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
    sock.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)  #share address
    if os.name != "nt":
        sock.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEPORT, 1)  #needed on Linux and OSX to share port with net core. Remove on windows.
    
    device_address = ('0.0.0.0', port) #listen for any IP

    try:
        sock.bind(device_address)
    except:
        print('failure to bind')
        sock.close()
        raise

    while True:
        data, addr = sock.recvfrom(1024)
        
        if AlpacaDiscovery in str(data, "ascii"):         
            sock.sendto(AlpacaResponse.encode(), addr)        

def respond_ipv6():
    # Create a socket
    sockv6 = socket.socket(socket.AF_INET6, socket.SOCK_DGRAM)
    sockv6.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
    if os.name != "nt":
        sockv6.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEPORT, 1)  #needed on Linux and OSX to share port with net core. Remove on windows.
        
    try:
        sockv6.bind(('', port))
    except:
        print('failure to bind tp IPv6')
        sockv6.close()
        raise

    addrinfo = socket.getaddrinfo("ff12::00a1:9aca", None)[0]
    group_bin = socket.inet_pton(addrinfo[0], addrinfo[4][0])
    
    # Join group
    mreq = group_bin + struct.pack('@I', 0)
    if os.name == "nt":
        sockv6.setsockopt(41, socket.IPV6_JOIN_GROUP, mreq) #some versions of python on Windows do not have this option
    else:
        sockv6.setsockopt(socket.IPPROTO_IPV6, socket.IPV6_JOIN_GROUP, mreq) 

    while True:
        data, addr = sockv6.recvfrom(1024)
    
        if AlpacaDiscovery in str(data, "ascii"):         
            sockv6.sendto(AlpacaResponse.encode(), addr)


if __name__ == "__main__":
    # really basic threading just to get the testing started 
    threadv6 = Thread(target = respond_ipv6)
    threadv6.start()     
    thread = Thread(target = respond_ipv4)
    thread.start()               