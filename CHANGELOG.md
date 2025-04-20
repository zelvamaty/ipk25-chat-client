### Basic functionality:
Client joins the server, user uses /auth and after successful reply they start communicating.

### How to run and input arguments
*Tested in the provided Virtual Machine and the provided development environment:*
`nix develop "git+https://git.fit.vutbr.cz/NESFIT/dev-envs.git?dir=ipk#csharp"`
- Compile the client by running `make`, this will output `ipk25chat-client` executable
- Options: ipk25chat-client -t [tcp|udp] -p [port] -h [hostname] -n [nickname] -r [retries] -d [timeout]

- example: **` ./ipk25chat-client -t tcp -s 127.0.0.1 -p 6969`**

### Known limitations:
- few scenarios in UDP weren't tested properly