import signal
import pyuv
import netifaces
import os
import socket
import struct

class __async_ipv6:
    def __init__(self, loop, ip_iface, iface_index, alpacaport, discoport=32227, mcgroup="ff12::414c:5041:4341"):
        self.server = pyuv.UDP(loop)
        self.__alpacaport = alpacaport
        self.__sockv6 = socket.socket(socket.AF_INET6, socket.SOCK_DGRAM)
        self.__sockv6.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
        if os.name != "nt":
            self.__sockv6.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEPORT, 1)  #needed on Linux and OSX to share port with net core. Remove on windows.

        self.server.open(self.__sockv6.fileno())
        self.server.bind((ip_iface, discoport))
        addrinfo = socket.getaddrinfo(mcgroup, None)[0]
        group_bin = socket.inet_pton(addrinfo[0], addrinfo[4][0])
            
        # Join group
        mreq = group_bin + struct.pack('@I', iface_index)
        if os.name == "nt":
            self.__sockv6.setsockopt(41, socket.IPV6_JOIN_GROUP, mreq) #some versions of python on Windows do not have this option
        else:
            self.__sockv6.setsockopt(socket.IPPROTO_IPV6, socket.IPV6_JOIN_GROUP, mreq)

        self.server.start_recv(self.__on_read)

        self.signal_h = pyuv.Signal(loop)
        self.signal_h.start(self.__signal_cb, signal.SIGINT)

    def __on_read(self, handle, ip_port, flags, data, error):
        if data is not None:
            resp = "{{\"alpacaport\": {port}}}".format(port = self.__alpacaport).encode()
            handle.send(ip_port, resp)

    def __signal_cb(self, handle, signum):
        self.signal_h.close()
        self.server.close()

class __async_ipv4:
    def __init__(self, alpacaport, discport, loop):
        self.server = pyuv.UDP(loop)
        self.__alpacaport = alpacaport
        self.__sockv6 = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
        self.__sockv6.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
        if os.name != "nt":
            self.__sockv6.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEPORT, 1)  #needed on Linux and OSX to share port with net core. Remove on windows.

        self.server.open(self.__sockv6.fileno())
        self.server.bind(('0.0.0.0', discport))

        self.server.start_recv(self.__on_read)

        self.signal_h = pyuv.Signal(loop)
        self.signal_h.start(self.__signal_cb, signal.SIGINT)

    def __on_read(self, handle, ip_port, flags, data, error):
        if data is not None:
            resp = "{{\"alpacaport\": {port}}}".format(port = self.__alpacaport).encode()
            handle.send(ip_port, resp)

    def __signal_cb(self, handle, signum):
        self.signal_h.close()
        self.server.close()

def server(loop, alpacaport, discoport=32227, mcgroup="ff12::414c:5041:4341"):
    __initipv4(loop, alpacaport, discoport)
    __initipv6(loop, alpacaport, discoport, mcgroup)

def __initipv4(loop, alpacaport, discoport=32227):
    try:
        __async_ipv4(alpacaport, discoport, loop)
    except Exception as e: print(e)        

def __initipv6(loop, alpacaport,  discoport=32227, mcgroup="ff12::414c:5041:4341"):
    if os.name != "nt":
        __async_ipv6(loop, '::', 0, alpacaport, discoport, mcgroup)
    else:
        resonders = []
        try:
            for interface in netifaces.interfaces():
                for interfacedata in netifaces.ifaddresses(interface):
                    if netifaces.AF_INET6 == interfacedata:
                        for ip in netifaces.ifaddresses(interface)[netifaces.AF_INET6]:
                            ip_index = ip['addr'].split('%')
                            
                            if len(ip_index) > 1:
                                try:
                                    resonders.append(__async_ipv6(loop, ip_index[0], int(ip_index[1]), alpacaport, discoport, mcgroup))
                                except Exception as e: print(e)
        except Exception as e: print(e)
