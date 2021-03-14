package com.alpaca;

import java.net.SocketException;

public class Main {

    public static void main(String[] args)  {
        Finder finder = null;
        try {
            finder = new Finder();
            finder.run();
        } catch (SocketException e) {
            e.printStackTrace();
        }
    }
}
