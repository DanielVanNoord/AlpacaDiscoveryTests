package com.alpaca;

import java.io.IOException;
import java.net.*;
import java.nio.charset.StandardCharsets;
import java.util.Enumeration;

public class Finder extends Thread {
    private DatagramSocket socket;
    private boolean running;

    public Finder() throws SocketException {
        socket = new DatagramSocket(0);
        socket.setBroadcast(true);
    }

    public void run() {
        running = true;

        String alpaca_discovery_string = "alpacadiscovery1";
        byte[] alpaca_discovery_bytes = alpaca_discovery_string.getBytes(StandardCharsets.UTF_8);

        byte[] response_buffer = new byte[255];

        InetAddress localhost = null;

        try {
            for (Enumeration<NetworkInterface> en = NetworkInterface.getNetworkInterfaces(); en.hasMoreElements();) {
                NetworkInterface intf = en.nextElement();
                for (InterfaceAddress address : intf.getInterfaceAddresses()) {
                    if (address.getAddress() instanceof Inet4Address) {

                        DatagramPacket message = new DatagramPacket(alpaca_discovery_bytes, alpaca_discovery_bytes.length, address.getBroadcast(), 32227);
                        try {
                            socket.send(message);
                        } catch (IOException e) {
                            e.printStackTrace();
                        }
                    }
                }
            }
        } catch (SocketException e) {
            e.printStackTrace();
        }

        try {
            DatagramPacket message = new DatagramPacket(alpaca_discovery_bytes, alpaca_discovery_bytes.length, InetAddress.getByName("ff12::00a1:9aca"), 32227);
            socket.send(message);
        } catch (IOException e) {
            e.printStackTrace();
        }

        while (running) {
            DatagramPacket packet
                    = new DatagramPacket(response_buffer, response_buffer.length);
            try {
                socket.receive(packet);
            } catch (IOException e) {
                e.printStackTrace();
            }

            InetAddress address = packet.getAddress();
            int port = packet.getPort();

            System.out.println(address);
            System.out.println(new String(packet.getData(), 0, packet.getLength()));
        }
        socket.close();
    }
}
