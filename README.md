# httpHandler
**Last rewrite: 8 of june**
This is a C# project made for running simple static web apps.

Run it in a directory you want to serve as: executableFile
To serve a directory you are not in run: executableFile [PATH]
To serve it on a specific port use: executableFile [PATH] [PORT]

If the directory you are serving contains a file called "serve" then that file will be used as a config file.
Use "port XXXX" to set a defualt port. Example "port 5000" but do note that passing a port in the statment to run it will take priority.
Use 'redirect "[PATH]" "[TARGETPATH]"' to make a redirect.
Use "badPathFile [PATH]" to set a file as the reply for a 404 Not Found


