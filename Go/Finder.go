package main

import (
	"encoding/binary"
	"errors"
	"fmt"
	"net"
)

func main() {
	pc, err := net.ListenPacket("udp", ":0")
	if err != nil {
		fmt.Print(fmt.Errorf("error listening for responses: %+v", err.Error()))
		return
	}
	defer pc.Close()

	message := []byte("alpacadiscovery1")

	ifaces, err := net.Interfaces()
	if err != nil {
		fmt.Print(fmt.Errorf("could not get ifaces: %+v", err.Error()))
		return
	}
	for _, i := range ifaces {

		if i.Flags&net.FlagUp == 0 {
			// If the interface is not up skip it
			continue
		}
		addrs, err := i.Addrs()
		if err != nil {
			fmt.Print(fmt.Errorf("could not get address: %+v", err.Error()))
			continue
		}
		for _, a := range addrs {
			switch v := a.(type) {

			case *net.IPNet:
				// Check if IPv6
				if v.IP.To4() == nil {

					addrMCast, err := net.ResolveUDPAddr("udp6", "[ff12::00a1:9aca]:32227")
					if err != nil {
						fmt.Print(fmt.Errorf("IPv6 resolution error: %+v", err.Error()))
						continue
					}
					go sendmessage(pc, addrMCast, message)

				} else { // Is IPv4
					addr, err := bcastAddr(v)

					if err != nil {
						fmt.Print(fmt.Errorf("IPv4 to broadcast address error: %+v", err.Error()))
						continue
					}
					addrBCast, err := net.ResolveUDPAddr("udp4", addr.String()+":32227")
					if err != nil {
						fmt.Print(fmt.Errorf("IPv4 resolution error: %+v", err.Error()))
						continue
					}

					go sendmessage(pc, addrBCast, message)
				}
			}
		}
	}

	for {
		buf := make([]byte, 1024)
		n, addr, err := pc.ReadFrom(buf)
		if err != nil {
			continue
		}

		if n > 0 {

			fmt.Print("Addr: ", addr)
			fmt.Println(" - buf: ", string(buf))
		}
	}
}

func bcastAddr(n *net.IPNet) (net.IP, error) {
	if n.IP.To4() == nil {
		return net.IP{}, errors.New("IPv6 addresses use multicast, not broadcast")
	}
	ip := make(net.IP, len(n.IP.To4()))
	binary.BigEndian.PutUint32(ip, binary.BigEndian.Uint32(n.IP.To4())|^binary.BigEndian.Uint32(net.IP(n.Mask).To4()))
	return ip, nil
}

func sendmessage(pc net.PacketConn, addr net.Addr, buf []byte) {
	_, err := pc.WriteTo(buf, addr)
	if err != nil {
		fmt.Print(fmt.Errorf("failed to send message: %+v", err.Error()))
	}
}
