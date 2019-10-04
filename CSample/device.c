// (c) 2019 Daniel Van Noord
// This code is licensed under MIT license (see License.txt for details)

#include <stdio.h>
#include <stdlib.h>
#include <unistd.h>
#include <string.h>
#include <sys/types.h>
#include <sys/socket.h>
#include <arpa/inet.h>
#include <netinet/in.h>

#define DiscoveryPort 32227
#define AlpacaPort 8910

//https://stackoverflow.com/questions/14388706/socket-options-so-reuseaddr-and-so-reuseport-how-do-they-differ-do-they-mean-t
int main(int argc, char *argv[])
{
	int sock, fromlen, n;
	struct sockaddr_in servaddr, from;
	char buf[1024];

	sock = socket(AF_INET, SOCK_DGRAM, 0);
	if (sock < 0) perror("Opening socket");

	memset(&servaddr, '\0', sizeof(struct sockaddr_in));
	servaddr.sin_family = AF_INET;
	servaddr.sin_family = AF_INET;
	servaddr.sin_addr.s_addr = INADDR_ANY;
	servaddr.sin_port = htons(DiscoveryPort);

	if (setsockopt(sock, SOL_SOCKET, (SO_REUSEPORT), &(int) { 1 }, sizeof(int)) < 0)
		perror("setsockopt(SO_REUSEPORT) failed");

	if (bind(sock, (struct sockaddr *)&servaddr, sizeof(struct sockaddr_in)) < 0)
		perror("binding");

	fromlen = sizeof(struct sockaddr_in);
	while (1) {
		n = recvfrom(sock, buf, 1024, 0, (struct sockaddr *)&from, &fromlen);
		if (n < 0) perror("recvfrom");
		write(1, buf, n);
		write(1, "\n", 1);

		if (n < 16)
		{
			continue;
		}
		//I am comparing 0 for clarity
		if (strncmp("alpaca discovery", buf, 16) != 0)
		{
			continue;
		}

		char response[36] = { 0 };

		sprintf(response, "{\"alpacaport\": %d}", AlpacaPort);

		n = sendto(sock, response, strlen(response),
			0, (struct sockaddr *)&from, fromlen);
		if (n < 0) perror("sendto");
	}
}
