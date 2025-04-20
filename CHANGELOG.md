
# IPK Project 2: Client for a chat server using the `IPK25-CHAT` protocol

## Table of contents
1. [Summary](#introduction)
2. [Theory](#theory)
	1. [TCP](#tcp)
	2. [UDP](#udp)
3. [How to run](#build-project)
4. [Implementation details](#implementation)
5. [Testing](#testing)
6. [Bibliography](#bibliography)

## Summary  <a name="summary"></a>


- This is a program written in C# *(.NET9.0)* implementing the client for a chat server using the `ipk25-chat` protocol. There are two variants of this protocol: `UDP` and  `TCP`. Messages/confirmations etc. are formatted and printed to the console.

## Theory <a name="theory"></a>
Necessary theory to understand how each protocol works.
### TCP <a name="tcp"></a>
- Transmission Control Protocol [1] is a core internet protocol. Server and Client establish a connection by three-way handshake, they agree on terms before doing the communication. It is more reliable than UDP.

### UDP <a name="udp"></a>
  - User Datagram Protocol [2] is connectionless and also a core internet protocol. It doesn't have strict rules like TCP does,  so the apps need to do most of the job. Actively listens on a specified port for any messages. It skips handshake unlike TCP, its faster, ideal for streaming something where dropping few frames is ok. 
  

## How to run  <a name="build-project"></a>
*Tested in the provided Virtual Machine and the provided development environment:*
`nix develop "git+https://git.fit.vutbr.cz/NESFIT/dev-envs.git?dir=ipk#csharp"`
- Compile the client by running `make`, this will output `ipk25chat-client` executable
- Options: ipk25chat-client -t [tcp|udp] -p [port] -h [hostname] -n [nickname] -r [retries] -d [timeout]

- example: **` ./ipk25chat-client -t tcp -s 127.0.0.1 -p 6969`**

  
 ## Implementation details <a name="implementation"></a>
  Everything starts in the `Main` function of the program where we call method from class `Chat` called `BeginChatting`(this method accepts the input arguments). From here we call the method `ParseArguments` ( from class `ArgumentParsing`) to process input arguments.
  Depending on the chosen transport protocol, we set the `chat` variable (from interface `IChatProtocol`). This interface defines methods used by both protocols, classes `Udp` and `Tcp`  inherit from this Interface. 
  After this point we move to method `Fsm` located in the `Chat` class. States switch based on the user input/server messages. In various states of the FSM, `chat.` methods are called to parse user input and incoming server messages.
  FSM states:
  - Start ------> waiting for /auth {username} {secret} {displayname} from user input
  - Auth ------> parse the REPLY message, if  OK -> go to Open
  - Open ------> waiting for user input + receiving server messages
  - Join ------> waiting for any REPLY message + receiving server messages
  - EndSuccess ------> ending with exit code error (0)
  - EndFailure ------> ending with exit code error (1)

## Testing <a name="testing"></a>

- Testing was done on the provided *virtual machine*. It was done with **Wireshark** [3] running to verify and check the packets. 

  

- Provided server `anton5.fit.vutbr.cz` also used to verify that both protocols respond well and show messages etc. properly. 

  

- Tests were done manually, or by python servers/tests. Also used improved tests from last year changed by fellow classmates to confirm my outputs and correct handling of scenarios i didn't think about testing: https://github.com/Vlad6422/VUT_IPK_CLIENT_TESTS. 56/58 passed. 

  
  
  

### Test Results

- **TCP**
 most of the test cases were executed by `./ipk25chat-client -s 127.0.0.1 -p 6969 -t tcp`, unless specified otherwise, client referred to as C, netcat as S.
  - (netcat [4] used to listen to my messages and send back responses)
`nc -C -l 127.0.0.1 6969`
	- **authentification SUCCESS test**

	```
	C(input): /auth uuu sss dddd
	S: AUTH uuu AS dddd USING sss
	S(send back): REPLY OK IS success.
	C: Action Success: success.
	```
	- **authentification FAIL test**

	```
	C(input): /auth uuu sss dddd
	S: AUTH uuu AS dddd USING sss
	S(send back): REPLY NOK IS fail.
	C: Action Failure: fail.
	```
	- **receiving a single message and sending a single message**

	```
	-auth already happened (same way as in the authentification test)
	S(send): MSG FROM netcat IS hello
	C: netcat: hello
	C(input): hello mr netcat
	S: MSG FROM dddd IS hello mr netcat
	```
	- **receiving and sending multiple messages**

	```
	-auth already happened (same way as in the authentification test)
	S(send): MSG FROM netcat IS today is a beautiful day
	C: netcat: today is a beautiful day
	S(send): MSG FROM netdog IS yes i agree
	C: netdog: yes i agree
	C(input): im silly dddd
	S: MSG FROM dddd IS im silly dddd
	C(input): i disagree
	S: MSG FROM dddd IS i disagree
	```
	- **send message and rename and send another**

	```
	-auth already happened (same way as in the authentification test)
	C(input): hi im dddd
	C(input): /rename cooldog
	C(input): im cool dog now
	S: MSG FROM dddd IS hi im dddd
	S: MSG FROM cooldog IS im cool dog now
	```
	- **BYE from user test**

	```
	-auth already happened (same way as in the authentification test)
	C(input): im leaving
	S: MSG FROM dddd IS im leaving
	C(input): ctrl+c
	S: BYE FROM dddd
	c: Process finished with exit code 0.
	```
	- **ERROR from server test**

	```
	-auth already happened (same way as in the authentification test)
	S(send): ERR FROM netcat IS begone
	C: ERROR FROM netcat: begone
	C: Process finished with exit code 1.
	```
	- **JOIN to a different channel test**

	```
	-auth already happened (same way as in the authentification test)
	C(input): /join channel4
	S: JOIN channel4 AS dddd
	S(send): REPLY OK IS success
	C: Action Success: success
	```

	- *join the provided reference server anton5.fit.vutbr.cz*
		 joining and sending few messages/receiving messages and leaving
	```
	C(input): /auth xhavli66 725ef352-a42f-4757-b28b-cac090bf55a6 tcpcat
	C: Action Success: Authentication successful. 
	C: Server: tcpcat has joined `discord.general` via TCP.
	C(input): hi its me the tcp cat 
	C: Server: miau has joined `discord.general` via UDP.
	C: pomoc: aaaaaa 
	C(input): help pomoc aaaaaa
	C(input): ctrl+c
	Process finished with exit code 0.
	```
- **UDP**
 Most of the test cases were executed by `./ipk25chat-client -s anton5.fit.vutbr.cz -p 4567 -t udp`, unless specified otherwise.
 client referred to as C, server as S.
  - Netcat can't really simulate invidual byte sendings so mainly the reference server was used. 
  - Things harder to replicate tested by running the public tests mentioned at the beginning of Testing part. 
	- *authentification SUCCESS test*

	```
	C(input):/auth xhavli66 725ef352-a42f-4757-b28b-cac090bf55a6 ahojjjjjjjj 
	C:Action Success: Authentication successful. 
	C:Server: ahojjjjjjjj has joined `discord.general` via UDP. 
	C:Server: meow has joined `discord.general` via TCP. 
	C(input): ahoj vsichni
	C:Server: testing44 has joined `discord.general` via UDP. 
	C:hulahej: ugrofinsko
	```
	![discord server auth](/images/first_udp.png)
	*screenshot from discord server for reference:*
	
	- *join the server and send and receive messages*
	```
	C(input):/auth xhavli66 725ef352-a42f-4757-b28b-cac090bf55a6 steve 
	C:Action Success: Authentication successful. 
	C:Server: steve has joined `discord.general` via UDP. 
	C:Captain_Bober: ajo vlastne a to blokuje ten nat 
	C(input):lava 
	C:Server: lili has joined `discord.general` via UDP. 
	C:terename: rename 
	C(input):/rename pes 
	C:Server: petocmorik has joined `discord.general` via TCP.
	C(input):pes skace
	C:Server: man has joined `discord.general` via TCP.
	```
	![discord server communication](/images/second_udp.png)
	*screenshot from discord server and wireshark for reference:*
	![wireshark communication](/images/wireshark.png)
	- *join the server and leave correctly after sending messages*
	```
	C(input):/auth xhavli66 725ef352-a42f-4757-b28b-cac090bf55a6 steve
	C:Action Success: Authentication successful.
	C:Server: steve has joined `discord.general` via UDP.
	C(input):ahoj zdravim
	C:Server: aragorn has switched from `discord.general` to `dis`.
	C(input):tak ja zas mizim
	C:ctrl+c
	```
	the BYE send (`ctrl+c`) is shown on the wireshark screenshot and steve left on the discord communication screenshot
	![discord server communication](/images/third_udp.png)
	*screenshot from discord server and wireshark for reference:*
	![wireshark communication](/images/wireshark2.png)

## Biblography <a name="bibliography"></a>
[1] Wikipedia. **Transmission Control Protocol**. [online]. April 2025. [cited 2025-04-18]. Available at https://en.wikipedia.org/wiki/Transmission_Control_Protocol 
[2] Wikipedia. **User Datagram Protocol**. [online]. April 2025. [cited 2025-04-18]. Available at https://en.wikipedia.org/wiki/User_Datagram_Protocol
[3] The Wireshark Team. **Wireshark - Go Deep**. [online]. [cited 2025-04-19]. Available at: https://www.wireshark.org/
[4] Nmap Project. **Ncat - Netcat for the 21st Century**. [online]. [cited 2025-04-19]. Available at: https://nmap.org/ncat/ 

