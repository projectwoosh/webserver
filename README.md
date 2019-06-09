# WebServer
This project is made by Tristan and was written in C# on Linux. This project isn't meant for widespread use, because it's just a barebone HTTP/1.1 server, used on thewoosh.net. Please let me know if you have any suggestions or have found any bugs in the 'Issues' section.

## Configuration
You have to make a configuration.ini file to use this project, and I highly advise you, when you actually want to use this solution, to change the Program.cs to fit your needs.
The configuration.ini options are:
<br>
* [string] certlocation - The location of the PFX file containing the encrypted certificate.
* [string] certpassword - The password for the certificate.
* [string] hostname - The hostname for this server, for example: github.com or thewoosh.net
* [bool] keepalive - Should we reuse the connection?
* [int32] keepalivelevel - What is the maximum request count we can have until we close the connection?
* [int32] listenertimeout - How many milliseconds should we wait between TcpListener.Pending() ?
* [string] servername - The name of the server, used in the Server header.

Only *certlocation*, *certpassword* and *hostname* are required with the default configuration.

## License
This project is licensed under the MIT License.
