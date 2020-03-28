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

#define PORT     32227

int main(int argc, char *argv[])
{
	char* mess = "alpacadiscovery1";

	struct sockaddr_in servaddr, cliaddr, from;
	int broadcastSock, n, fromlen;
	char buf[1024];

	if ((broadcastSock = socket(AF_INET, SOCK_DGRAM, 0)) < 0) {
		perror("socket creation failed");
		exit(EXIT_FAILURE);
	}

	memset(&servaddr, '\0', sizeof(struct sockaddr_in));
	servaddr.sin_family = AF_INET;
	servaddr.sin_port = htons(PORT);
	servaddr.sin_addr.s_addr = htonl(INADDR_BROADCAST);

	cliaddr.sin_family = AF_INET;
	cliaddr.sin_addr.s_addr = htonl(INADDR_ANY);
	cliaddr.sin_port = htons(0);

	if (setsockopt(broadcastSock, SOL_SOCKET, (SO_BROADCAST), &(int) { 1 }, sizeof(int)) < 0)
		perror("setsockopt(SO_BROADCAST) failed");

	if (bind(broadcastSock, (const struct sockaddr *)&cliaddr,
		sizeof(cliaddr)) < 0)
	{
		perror("bind failed");
		exit(EXIT_FAILURE);
	}

	if (sendto(broadcastSock, mess, strlen(mess), 0, (struct sockaddr *)&servaddr, sizeof(struct sockaddr_in)) < 0)
		perror("sendto");
	fromlen = sizeof(struct sockaddr_in);
	while (1) {
		n = recvfrom(broadcastSock, buf, 1024, 0, (struct sockaddr *)&from, &fromlen);
		if (n < 0) perror("recvfrom");

		char str[INET_ADDRSTRLEN];

		inet_ntop(AF_INET, &(from.sin_addr), str, INET_ADDRSTRLEN);

		write(1, buf, n);
		write(1, " ", 1);
		write(1, str, strlen(str));
		write(1, "\n", 1);
	}
}