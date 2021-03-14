// (c) 2019 Daniel Van Noord
// This code is licensed under MIT license (see License.txt for details)

#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <sys/types.h>

#ifdef _WIN32
#pragma comment(lib, "Ws2_32.lib")
#include <io.h>
#include <winsock2.h>
#include <ws2tcpip.h>
#ifndef socklen_t
#define socklen_t int
#endif
#else
#include <unistd.h>
#include <sys/socket.h>
#include <arpa/inet.h>
#include <netinet/in.h>
#endif

#define DiscoveryPort 32227
#define AlpacaPort 8910

//https://stackoverflow.com/questions/14388706/socket-options-so-reuseaddr-and-so-reuseport-how-do-they-differ-do-they-mean-t
int main(int argc, char *argv[])
{
	int sock, n;
	socklen_t fromlen;
	struct sockaddr_in servaddr, from;
	char buf[1024];

#ifdef _WIN32
	WSADATA wsa;

	if (WSAStartup(MAKEWORD(2, 2), &wsa) != 0)
	{
		perror("WSAStartup Failed");
		exit(EXIT_FAILURE);
	}
#endif

	sock = socket(AF_INET, SOCK_DGRAM, 0);
	if (sock < 0)
		perror("Opening socket");

	memset(&servaddr, '\0', sizeof(struct sockaddr_in));
	servaddr.sin_family = AF_INET;
	servaddr.sin_family = AF_INET;
	servaddr.sin_addr.s_addr = INADDR_ANY;
	servaddr.sin_port = htons(DiscoveryPort);

		if (setsockopt(sock, SOL_SOCKET, (SO_REUSEADDR), &(int){1}, sizeof(int)) < 0)
		perror("setsockopt(SO_REUSEPORT) failed");

#ifndef _WIN32
	if (setsockopt(sock, SOL_SOCKET, (SO_REUSEPORT), &(int){1}, sizeof(int)) < 0)
		perror("setsockopt(SO_REUSEPORT) failed");
#endif

	if (bind(sock, (struct sockaddr *)&servaddr, sizeof(struct sockaddr_in)) < 0)
		perror("binding");

	fromlen = sizeof(struct sockaddr_in);
	while (1)
	{
		n = recvfrom(sock, buf, 1024, 0, (struct sockaddr *)&from, &fromlen);
		if (n < 0)
			perror("recvfrom");
		write(1, buf, n);
		write(1, "\n", 1);

		if (n < 16)
		{
			continue;
		}
		//I am comparing 0 for clarity
		if (strncmp("alpacadiscovery1", buf, 16) != 0)
		{
			continue;
		}

		char response[36] = {0};

		sprintf(response, "{\"alpacaport\": %d}", AlpacaPort);

		n = sendto(sock, response, strlen(response),
				   0, (struct sockaddr *)&from, fromlen);
		if (n < 0)
			perror("sendto");
	}
}
