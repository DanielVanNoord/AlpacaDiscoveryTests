package com.alpaca;

import java.io.IOException;
import java.net.SocketException;

public class Main {

    public static void main(String[] args)  {

        try {
            Responder responder = new Responder();
            responder.start();
        } catch (IOException e) {
            e.printStackTrace();
        }

        try {
            ResponderIPv6 responder = new ResponderIPv6();
            responder.start();
        } catch (IOException e) {
            e.printStackTrace();
        }

        try {
            Finder finder = new Finder();
            finder.run();
        } catch (SocketException e) {
            e.printStackTrace();
        }
    }
}
