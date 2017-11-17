This is the original TSB Loader tool, written by the original author.

TSBLoader is a PC software to interact with the bootloader on the slave device to flash, manage eeprom, etc.
This original version compiles with FreeBasic.

- This original version is also capable of producing FIRMWARE binaries to install directly on your processor (without having to compile from ASM).
  This feature is not present in the tsbloader_advanced and this may be a use case for it, despite its development being deprecated in this repository.
  Note, however, the produced bootloader files are based on a precompiled 2015 version of the bootloader that contains a bug if you connect your devices in a Daisy chain. This is detailed in the issue tracker.

- This version also contains a bug when dumping the content of one of the memories in a specific file format (it trims the last byte).
  See the issue tracker for more information.

We are no longer maintaining this tool and have since developed tsbloader_advanced tool, written in C#/Mono and host of new capabilities.
The new tool also has a "patch" mode to fix the bug present in the bootloader binaries that have the daisy chain bug.
Through the use of tsbloader_advanced you can work  the issue and be able to operate the devices even if they have that buggy version.



