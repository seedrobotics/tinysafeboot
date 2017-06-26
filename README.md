# tinysafeboot
Public repository to expand on Julien Thomas excellent Tinysafeboot bootloader for ATMEGA and ATTINY

The original Tinysafeboot, including detailed documentation if available from Julien's website at http://jtxp.org/tech/tinysafeboot_en.htm

The aim of this repository is to integrate changes and improvements made to TSB and share them in compliance with the original license.
Everyone is welcome to expand on this excellent work.

<p>Git repository structure:</p>
<ul>
<li>firmware_ASM: includes the firmware part to be installed on the ATMEGA and ATTINY devices using ICSP</li>
<li>software/tsb_loader_original: the original TSB Loader written by Julien Thomas in FreeBasic.<br/>
You can use this program to produce TSB binaries for your processor without needing to use an ATMEL assembler.<br/>
If using this tool to read code from your processor, beware that it has a bug when saving to BIN files 
(it won't save the last byte); save to Intel HEX instead which does not have this issue.</li>
<li>software/tsb_loader2: a completely re written TSB loader in Mono (C#) for cross platform compatibility.<br/>
This new loader has several advantages:
<ul><li>You can specify the device password on the command line, thus eliminating the timeout in the original version</li>
<li>Communication relies on the OS buffers to detect arrival of device replies, which makes this code significantly faster
to run (for example at 19 200 bps) it can be up to 5x faster than tsb_original.
<li>Extremely evrbose output: in case of error you are told exactly what is wrong
<li>Ability to perform multiple operations in one single TSB session (for example, a firmware erase/write/verify in one go).<br/>
In the original TSB Loader, you need to reset the processor and start a new TSB session for each individual operation.</li>
<li>Configurable pre_wait times and reply_timeout times makes it extremely versatile when dealing with self resetting boards (like Arduino)</li>
<li>Automaticaly repeatable operations for multiple devices: if you have devices on a daisay chain (connected in paralel) with unique passwords,
you can enter a list of device passwords and all operations are performed on all of them at once (your board needs to have an auto reset capability
based on DTR transitions, similar to the Arduino implementation)</li>
<li>It includes a clever work around to overcome the bug on Daisy chain operation where silent devices might escape the "wrong password" lock
and go into Emergency Erase confirm mode and subsequently boot.<br/>
While the fix does not prevent them from booting, it forces the devices out of the "wrong password lock" before initiating any communication
in order to prevent interferences from the Emergency erase confirmation.</li>
</ul>
</ul>
