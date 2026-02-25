# Helper Scripts and Files

The files contained in this folder are additions to the main package that will allow for tighter integration with your system.  Most of these are written specifically for Windows as Linux already has good support for URI functionality built in to the system.

### SSH

These two files are specifically to launch SSH sessions to a host directly from the Chimply program.  It takes a URI of the following format and will parse it out and ask for the username if none was specified in the URI.  Windows by default will include your username (domain or not) in the session, which is more often than not, undesired.

**Security Warning** : Please review the registry file and batch script as these will be running with system privileges.

1. Merge the registry element to your registry.
2. Copy the batch file to: ```c:\Windows\System32\OpenSSH\```