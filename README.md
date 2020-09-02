# tinysafeboot
Public repository to expand on Julien Thomas excellent Tinysafeboot bootloader for ATMEGA and ATTINY

The original Tinysafeboot, including detailed documentation if available from Julien's website at http://jtxp.org/tech/tinysafeboot_en.htm

In this repository, we publish our changes and improvements made to TSB. We have written a brief introduction to TSB and the new features we've added here http://kb.seedrobotics.com/doku.php?id=tsb:home

<p>Git repository structure:</p>
<ul>
<li><b>firmware_ASM/original_FW</b>: includes the firmware part to be installed on the ATMEGA and ATTINY devices using ICSP, originally written by Julien Thomas.<br/>
This implementation has a bug when operating in Daisy chain (multiple devices connected in parallel). See issue #1</li>

<li><b>firmware_ASM/latest_stable_release/20170626-autobaud</b>: bugfix for issue #1 in the tracker (inherited from Julian's code).</li>

<li><b>firmware_ASM/latest_stable_release/20200727-fixedbaud</b>: Communication routines are rewritten to use the processor's native UART. It is the prefered version where using the native UART is a possibility as it includes may improvements. Open the folder <b>latest_stable_release</b> and see the README for more information.</li>

<li><b>software/tsb_loader_original</b>: the original TSB Loader (PC software) written by Julien Thomas in FreeBasic.<br/>
You can use this program to produce TSB binaries for your processor without needing to use an ATMEL assembler.<br/>
If using this tool to read code from your processor, beware that it has a bug when saving to BIN files 
(it won't save the last byte); save to Intel HEX instead which does not have this issue.<br>
Binary versions produced by the tool also have the Daisy chain/Password escape bug and are of the Autobauding variant. Beware!</li>

<li><b>software/tsbloader_advanced</b>: our completely re written TSB loader in Mono (C#) for cross platform compatibility.<br/>
This new loader has several advantages:
<ul>
<li><b>NEW for the 20170626-autobaud version of the firmware</b>: due to the communication timeout implemented in this version, it is recommended to use tsbloader_adv version 1.0.9 or higher<br/>
Using earlier versions may result in an error when setting Password, TImeout or Magic bytes (this is because this information needs ot be passed quickly to prevent bootloader session timeout).<br/>
Version 1.0.9 fixes this by asking this information to the user beforehand, and only after having it on hand, will it initiate the bootloader session.<br/>
( if using the GUI versionc you also need version  or above to work correctly with tsbloader_adv 1.0.9 or newer)</li>
<li>You can specify the device Activation password on the command line (actually several individual passwords, if you are on a Daisy chain), thus eliminating the timeout you had in the original version before you could enter the password</li>
<li>Communication relies on the OS buffers to detect arrival of device replies, which makes this code significantly faster
to run (for example at 19 200 bps it can be up to 5x faster than tsb_original, which uses hard coded wait times).
<li>Extremely verbose output: in case of error you are told exactly what is wrong
<li>Ability to perform multiple operations in one single TSB session (for example, a firmware erase/write/verify in one go).<br/>
In the original TSB Loader, you need to reset the processor and start a new TSB session for each individual operation.</li>
<li>Configurable pre_wait times and reply_timeout times makes it extremely versatile when dealing with self resetting boards (Arduino-style)</li>
<li>Automatically repeatable operations for multiple devices: if you have devices on a daisy chain with unique passwords,
you can provide a list of device passwords and all operations are performed on each of them, one at a time (your board needs to have an auto reset capability
based on DTR transitions, similar to the Arduino implementation)</li>
<li>It implements a clever work around to overcome the bug on Daisy chain operation where silent devices might escape the "wrong password" lock,
 go into Emergency Erase confirm mode and subsequently boot. This loader can be used to attempt a bugfix on older, affected bootloaders.<br/>
While this fix does not prevent them from booting, it forces the devices out of the "wrong password lock" before initiating any communication
in order to prevent interferences from the Emergency erase confirmation.</li>
<li>Adds the ability to store 2 user defined "Magic Bytes" (useful for model numbers, batch identification, etc)
<li><b>What it does NOT DO</b>: it will not produce the TSB binaries to load on your ATMEGA/ATTINY device. If you need that feature, please use TSB original.</li>
</ul>
</ul>

For further information see our introduction to TSB here http://kb.seedrobotics.com/doku.php?id=tsb:home
