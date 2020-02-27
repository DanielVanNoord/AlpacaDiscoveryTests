import signal
import pyuv
import netifaces
import os
import socket
import struct
import json

from threading import Lock

__lock = Lock()

def print_data(data, addr):
    __lock.acquire()
    if 'alpacaport' in str(data):
        jsondata = json.loads(data)
        print(addr, ":",  jsondata['alpacaport'])
    __lock.release()

class __async_ipv6_search:
    def __init__(self, loop, ip_iface, iface_index, discoport=32227,  mcgroup="ff12::414c:5041:4341"):
        self.server = pyuv.UDP(loop)
        self.server.bind((ip_iface, 0))

        self.server.start_recv(self.__on_read)

        self.signal_h = pyuv.Signal(loop)
        self.signal_h.start(self.__signal_cb, signal.SIGINT)

        self.server.try_send((mcgroup, discoport), "alpacadiscovery".encode())

    def __on_read(self, handle, ip_port, flags, data, error):
        if data is not None:
            print_data(data, ip_port[0])

    def __signal_cb(self, handle, signum):
        self.signal_h.close()
        self.server.close()

class __async_ipv4:
    def __init__(self, discoport, loop):
        self.server = pyuv.UDP(loop)
        self.server.bind(('0.0.0.0', 0))

        self.server.start_recv(self.__on_read)

        self.signal_h = pyuv.Signal(loop)
        self.signal_h.start(self.__signal_cb, signal.SIGINT)

        for interface in netifaces.interfaces():
            for interfacedata in netifaces.ifaddresses(interface):
                if netifaces.AF_INET == interfacedata:
                    for ip in netifaces.ifaddresses(interface)[netifaces.AF_INET]:
                        if('broadcast' in ip):
                            self.server.try_send((ip['broadcast'], discoport), "alpacadiscovery".encode())

    def __on_read(self, handle, ip_port, flags, data, error):
        if data is not None:
            print_data(data, ip_port[0])


    def __signal_cb(self, handle, signum):
        self.signal_h.close()
        self.server.close()

def search(loop, discoport=32227, mcgroup="ff12::414c:5041:4341"):
    __initipv4(loop, discoport)
    __initipv6(loop, discoport, mcgroup)

def __initipv4(loop, discoport=32227):
    try:
        __async_ipv4(discoport, loop)
    except Exception as e: print(e)        

def __initipv6(loop, discoport=32227, mcgroup="ff12::414c:5041:4341"):
    resonders = []
    try:
        for interface in netifaces.interfaces():
            for interfacedata in netifaces.ifaddresses(interface):
                if netifaces.AF_INET6 == interfacedata:
                    for ip in netifaces.ifaddresses(interface)[netifaces.AF_INET6]:
                        ip_index = ip['addr'].split('%')
                        
                        if len(ip_index) > 1:
                            try:
                                resonders.append(__async_ipv6_search(loop, ip_index[0], int(ip_index[1]), discoport, mcgroup))
                            except Exception as e: print(e)
    except Exception as e: print(e)