package com.alpaca;

import java.io.IOException;
import java.net.*;
import java.nio.charset.StandardCharsets;

public class ResponderIPv6 extends Thread {
    private final MulticastSocket socket;
    private boolean running;

    public ResponderIPv6() throws IOException {
        socket = new MulticastSocket(null);
        socket.setReuseAddress(true);
        try {
            if(!System.getProperty("os.name").contains("Windows")) {
                socket.setOption(StandardSocketOptions.SO_REUSEPORT, true);
            }
        } catch (IOException e) {
            e.printStackTrace();
        }
        socket.joinGroup(InetAddress.getByName("ff12::00a1:9aca"));
        socket.bind(new InetSocketAddress(InetAddress.getByName("0.0.0.0"), 32227));
    }

    public void run() {
        running = true;

        String alpaca_discovery_string = "alpacadiscovery1";
        byte[] alpaca_discovery_response = "{\"alpacaport\": 10321}".getBytes(StandardCharsets.UTF_8);
        byte[] response_buffer = new byte[255];

        while (running) {
            DatagramPacket packet = new DatagramPacket(response_buffer, response_buffer.length);
            try {
                socket.receive(packet);
            } catch (IOException e) {
                e.printStackTrace();
            }

            InetAddress address = packet.getAddress();
            int port = packet.getPort();

            System.out.println(address);
            System.out.println(new String(packet.getData(), 0, packet.getLength()));

            if(new String(packet.getData()).contains(alpaca_discovery_string)){
                try {
                    DatagramPacket message = new DatagramPacket(alpaca_discovery_response, alpaca_discovery_response.length, address, port);
                    socket.send(message);
                } catch (IOException e) {
                    e.printStackTrace();
                }
            }
        }
        socket.close();
    }

    public void end(){
        running = false;
    }
}
