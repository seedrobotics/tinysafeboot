This folder contains compiled versions of the TSB Advanced Loader tool.

TSBLoader is a PC software to interact with the TSB bootloader on the slave device to manage flash, manage eeprom, bootloader configuration and emergency erase.

Use tsbloader_adv.exe -? for usage options and instructions.

Versions 1.0.1 and 1.0.2 are is compiled with the open source Mono .Net compiler meaning the <b>same binaries are compatible cross platform for Windows, Linux and MacOS, provided Mono is installed</b> on your system (in case it is Linux or MacOS; if you are on Windows either have Mono or .Net installed (.Net is probably already installed in Windows))
Version 1.0.5 and 1.0.8 is compiled with VS, but provided MONO is installed on Linux or MacOS, it should also run.

Refer to Mono project page (http://www.mono-project.com/) for information on installing Mono.
MONO is REQUIRED if you wish to run the binaries under Linux and MacOS. There is no need to recompile any file for your OS if you have Mono installed.

--

From Jan 2019 a GUI Tool is available to assist in using TSB Loader.
This is essentially a GUI wrapper for the Command line tool. You MUST have TSBloader_adv.exe version 1.0.5 to use this tool.
The tool is written in VB.Net and has been tested under Windows 10 only. Linux and MacOS have not been tested and are unlikelly to be able to run this tool. In this case use the command line tool instead.

From Apr 2019 (version 1.0.8 of the Advanced Loader tool), we've added the "Magic Bytes" capability that lets users store 2 bytes at their choosing (for model setting, batch identification, ...)
So far, only the command line tool supports this, using the -xop=m option. 
The GUI tool will be updated at a later stage to include this as well.

From August 2020 (version 1.0.9) we introduce changes to better with the new "fixedbaud" variants of the TSB bootloader.


FAQs:
- The binaries don't run on Linux/Mac: say permission denied
  You need to give +x permission to the file.
  At the Console window, type chmod +x tsbloader_adv.exe
  
- I get errors about accessing the Serial port on Linux if I'm not root.
  This is a common issue under Linux. The /dev/ttyUSBx devices often need to be added to dialout group. 
  If you are running as non-root, at the command prompt type:
    sudo usermod -a -G dialout $USER
  Next, you need to log out and log back on.

  Some older Linux distributions included a “modemmanager” module that occupies Serial ports as well. 
  While this is quite uncommon today, if the above still won't resolve the issue, you can try uninstalling “modemmanager” by typing sudo apt-get remove modemmanager (or the equivalent for your distribution)