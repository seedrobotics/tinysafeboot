This folder contains compiled versions of the TSBAdvanced Loader tool.

TSBLoader is a PC software to interact with the TSB bootloader on the slave device to manage flash, manage eeprom, bootloader configuration and emergency erase.

Use tsbloader_adv.exe -? for usage options and instructions.

This tool is compiled with the open source Mono .Net compiler meaning the <b>same binaries are compatible cross platform for Windows, Linux and MacOS, provided Mono is installed</b> on your system (in case it is Linux or MacOS; if you are on Windows either have Mono or .Net installed (.Net is probably already installed in Windows))

Refer to Mono project page (http://www.mono-project.com/) for information on installing Mono.
MONO is REQUIRED if you wish to run the binaries under Linux and MacOS. There is no need to recompile any file for your OS if you have Mono installed.


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