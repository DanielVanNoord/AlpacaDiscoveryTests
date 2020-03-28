import json
import os
import socket
import struct
# requires netifaces, you can install it with pip install netifaces
# this is used to iterate over all interfaces on the computer and to use the correct broadcast address
import netifaces

from threading import Lock, Thread

port = 32227  # a temporary port that I choose for testing
AlpacaDiscovery = "alpacadiscovery1"
AlpacaResponse = "alpacaport"

lock = Lock()


def search_ipv4():
    # Create listening port
    # ---------------------
    sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
    sock.setsockopt(socket.SOL_SOCKET, socket.SO_BROADCAST, 1)

    try:
        sock.bind(('0.0.0.0', 0))  # listen to any on a temporary port
    except:
        print('failure to bind')
        sock.close()
        raise

    for interface in netifaces.interfaces():
        for interfacedata in netifaces.ifaddresses(interface):
            if netifaces.AF_INET == interfacedata:
                for ip in netifaces.ifaddresses(interface)[netifaces.AF_INET]:
                    if('broadcast' in ip):
                        sock.sendto(AlpacaDiscovery.encode(),
                                    (ip['broadcast'], port))

    while True:
        data, addr = sock.recvfrom(1024)  # buffer size is 1024 bytes

        print_data(str(data, "ascii"), addr)


def search_ipv6():
    # Create listening port
    # ---------------------
    sock = socket.socket(socket.AF_INET6, socket.SOCK_DGRAM)

    try:
        sock.bind(('', 0))  # listen to any on a temporary port
    except:
        print('failure to bind')
        sock.close()
        raise

    sock.sendto(AlpacaDiscovery.encode(), ("ff12::00a1:9aca", port))

    while True:
        data, addr = sock.recvfrom(1024)  # buffer size is 1024 bytes

        print_data(str(data, "ascii"), addr)


def print_data(data, addr):
    lock.acquire()
    if AlpacaResponse in data:
        jsondata = json.loads(data)
        print(addr[0], ":",  jsondata[AlpacaResponse])
    lock.release()


if __name__ == "__main__":
    # really basic threading just to get the testing started
    threadv6 = Thread(target=search_ipv6)
    threadv6.start()
    thread = Thread(target=search_ipv4)
    thread.start()
