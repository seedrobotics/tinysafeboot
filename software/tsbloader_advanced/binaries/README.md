This folder contains compiled versions of the TSBAdvanced Loader tool.

These tools are compiled with open source Mono .Net compiler meaning the binaries are cross platform for Windows, Linux and MacOS, provided Mono is installed on your system (in case iti is Linux os MacOS)

Refer to Mono project page (http://www.mono-project.com/) for information on installing Mono.
MONO is REQUIRED if you wish to run the binaries under Linux and MacOS. There is no need to recompile any file if you have Mono installed.


FAQs:
- The binaries don't run on Linux/Mac: say permission denied
  You need to give +x permission to the file.
  At the Console window, type chmod +x tsbloader_adv.exe
  
- I get errors about accessing the Serial port on Linux if I'm not root.
  This is a common issue under Linux. The /dev/ttyUSBx devices are often added to dial-out group. 
  If you are running as non-root, at the command prompt type:
    sudo usermod -a -G dialout $USER
  Next you need to log out and log back on.

  Some older Linux distributions included a “modemmanager” module that occupied the port. 
  While this is quite uncommon today, if the above still won't resolve the issue, you can try uninstalling “modemmanager” by typing sudo apt-get remove modemmanager (or the equivalent for your distribution)