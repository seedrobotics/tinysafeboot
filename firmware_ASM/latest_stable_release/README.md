<p>This code forks from Julien's release 20150826 which is the one that is paired with his original TSB loader tool.
<br/>
(There seems to have been a subsequent release in 2016 with improvements to autobauding which changes the Emergency Erase procedure
and breaks compatibility with the PC tool; we chose to fork off the 20150826 version).
</p>

<p>
<b>Compiling</b>
</p><p>
To Compile this code you need ATMEL Studio or gavrasm. Both are free.</p>
<ul><li>ATMEL Studio is quite a large download but it's fairly user friendly. It's also the easiest way to compile the bootloader for your device.<br/>The files in this folder are for an ATMEL Studio 7 solution.</li>
<li>gavrasm is lighter and seems straightforward as well (http://www.avr-asm-tutorial.net/gavrasm/index_en.html)<br/>However gavrasm seems to have some troubling compiling certain parts of the code; we would recommened using ATMEL Studio 7 instead.</li>
</ul>
</p>

<p>
You need to tailor the ASM file to your process before compiling:
<ul><li>Find section "PORTS" and specifiy the ports to use. <br/>Note that TSB can work on any digital pin (don't have to be the processor's UART pins)
     <br/>If you want to use TSB in Half Duplex, One Wire mode (Daisy chain network or also called Multi-drop network) set the RX and TX to the SAME pin.
</li>
<li>If you are using ATMEL Studio, select your processor model (target device). To do this, use the option in the toolbar or go to Project Properties, Select the Device section and change the target processor to the processor you are compiling for.
</li>  
<li>Finally, compile. Pay attention to the messages generated as they provide valuable verbose feedback.</li>
</ul>
</p>
<p><b>If you are not comfortable with modifying these PRTx, DDRx, ... and compiling or are unfamiliar with this</b>, you can try and use Julien's original
TSB loader tool (in FreeBasic for the PC). This tool is available from this repository as well (it is tsbloader_original). It has the ability to generate Bootloader binaries for multiple processors without the need for any
external compiler. It's only drawback is that it produces Bootloaders of version 20150826 which don't have the bugfix for #Issue 1 identified in the Issues section. (https://github.com/seedrobotics/tinysafeboot/issues/1)<br/>
If you won't be connecting your devices in a Daisy Chain or Multi Drop etwork, that version should be fine; if you do, then we strongly recommend using a newer version with this fixed.
</p>


<p>
<b>Changelog:</b>

- <b>20170626</b>
 Bugfix for #Issue 1 identified in the Issues section. (https://github.com/seedrobotics/tinysafeboot/issues/1)
<p>
This bug occurs in situations where a wrong password was sent to the bootloader. In this case the bootloader should go into an infitine loop
but if you send a \0 it will escape the loop and enter Emergency Erase Mode. If the actual Emergency Erase confirmation sequence is sento to the serial port, it WILL erase the device.
<br/>
This is not the expected behaviour and is potentially damaging in case you use passwords.

The only modification has been the addition of 2 lines to ensure that the bootloader does not go into Emergency Erase confirmation mode
and stays in an infitinite loop in case we have already received a wrong password. 
(the system stays an infinite loop but still pulling characters from the UART and discading them, to smoothen the power profile and avoid attacks based on power profile analysis).
</p>
<p>
<u>This code has not been tested on all MCUs</u>. From the numbers crunched, it seems the two instructions that were added still fit in the calculations
that are made and the code will still align properly but please test before real-world deployment!

