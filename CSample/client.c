// (c) 2019 Daniel Van Noord
// This code is licensed under MIT license (see License.txt for details)

#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <sys/types.h>

#ifdef _WIN32
#pragma comment(lib, "Ws2_32.lib")
#pragma comment(lib, "IPHLPAPI.lib")
#include <io.h>
#include <winsock2.h>
#include <ws2tcpip.h>
#include <iptypes.h>
#include <iphlpapi.h>
#ifndef socklen_t
#define socklen_t int
#endif
#else
#include <arpa/inet.h>
#include <ifaddrs.h>
#include <netinet/in.h>
#include <sys/socket.h>
#include <unistd.h>
#endif

#define PORT 32227

int main(int argc, char *argv[])
{
	char *mess = "alpacadiscovery1";

	struct sockaddr_in servaddr, cliaddr, from;
	int broadcastSock, n;
	socklen_t fromlen;
	char buf[1024];

#ifdef _WIN32
	WSADATA wsa;

	if (WSAStartup(MAKEWORD(2, 2), &wsa) != 0)
	{
		perror("WSAStartup Failed");
		exit(EXIT_FAILURE);
	}
#endif

	if ((broadcastSock = socket(AF_INET, SOCK_DGRAM, 0)) < 0)
	{
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

	if (setsockopt(broadcastSock, SOL_SOCKET, (SO_BROADCAST), &(int){1}, sizeof(int)) < 0)
		perror("setsockopt(SO_BROADCAST) failed");

	if (bind(broadcastSock, (const struct sockaddr *)&cliaddr,
			 sizeof(cliaddr)) < 0)
	{
		perror("bind failed");
		exit(EXIT_FAILURE);
	}

	//Use Windows and Linux APIs to read all addresses and send out a broadcast on each network
#ifdef _WIN32
	IP_ADAPTER_INFO *pAdapterInfo;
	ULONG ulOutBufLen;
	DWORD dwRetVal;

	pAdapterInfo = (IP_ADAPTER_INFO *)malloc(sizeof(IP_ADAPTER_INFO));
	ulOutBufLen = sizeof(IP_ADAPTER_INFO);

	if (GetAdaptersInfo(pAdapterInfo, &ulOutBufLen) != ERROR_SUCCESS)
	{
		free(pAdapterInfo);
		pAdapterInfo = (IP_ADAPTER_INFO *)malloc(ulOutBufLen);
	}

	if ((dwRetVal = GetAdaptersInfo(pAdapterInfo, &ulOutBufLen)) != ERROR_SUCCESS)
	{
		printf("GetAdaptersInfo call failed with %d\n", dwRetVal);
	}

	PIP_ADAPTER_INFO pAdapter = pAdapterInfo;
	IN_ADDR ip_addr, mask_addr;
	while (pAdapter)
	{
		inet_pton(AF_INET, pAdapter->IpAddressList.IpAddress.String, &ip_addr);
		inet_pton(AF_INET, pAdapter->IpAddressList.IpMask.String, &mask_addr);

		if (ip_addr.s_addr != 0)
		{
			servaddr.sin_addr.s_addr = ip_addr.s_addr | ~mask_addr.s_addr;

			if (sendto(broadcastSock, mess, strlen(mess), 0, (struct sockaddr *)&servaddr, sizeof(struct sockaddr_in)) < 0)
				perror("sendto");
		}

		pAdapter = pAdapter->Next;
	}

#else

	struct ifaddrs *ifaddrp, *ifa;
	struct sockaddr_in *ip_addr, *mask_addr;

	getifaddrs(&ifaddrp);
	for (ifa = ifaddrp; ifa; ifa = ifa->ifa_next)
	{
		if (ifa->ifa_addr && ifa->ifa_addr->sa_family == AF_INET)
		{
			ip_addr = (struct sockaddr_in *)ifa->ifa_addr;
			mask_addr = (struct sockaddr_in *)ifa->ifa_netmask;

			servaddr.sin_addr.s_addr = ip_addr->sin_addr.s_addr | ~mask_addr->sin_addr.s_addr;

			if (sendto(broadcastSock, mess, strlen(mess), 0, (struct sockaddr *)&servaddr, sizeof(struct sockaddr_in)) < 0)
				perror("sendto");
		}
	}

	freeifaddrs(ifaddrp);

#endif

	//Now send to Local Host
	servaddr.sin_addr.s_addr = htonl(INADDR_LOOPBACK);
	servaddr.sin_port = htons(PORT);

	if (sendto(broadcastSock, mess, strlen(mess), 0, (struct sockaddr *)&servaddr, sizeof(struct sockaddr_in)) < 0)
		perror("sendto");

	fromlen = sizeof(struct sockaddr_in);
	while (1)
	{
		n = recvfrom(broadcastSock, buf, 1024, 0, (struct sockaddr *)&from, &fromlen);
		if (n < 0)
		{
			#ifdef _WIN32
				//If nothing is bound on local host Windows will return a WSAECONNRESET
				//This should be ignored
				if(WSAGetLastError() != WSAECONNRESET){
					perror("recvfrom");
					continue;
				}
			#else
				perror("recvfrom");
				continue;
			#endif
		}

		char str[INET_ADDRSTRLEN];

		inet_ntop(AF_INET, &(from.sin_addr), str, INET_ADDRSTRLEN);

		write(1, buf, n);
		write(1, " ", 1);
		write(1, str, strlen(str));
		write(1, "\n", 1);
	}
}