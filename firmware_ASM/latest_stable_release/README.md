<p>This code forks from Julien's release 20150826 which is the one that is paired with his original TSB loader tool.
<br/>
(There seems to have been a subsequent release in 2016 with improvements to autobauding which changes the Emergency Erase procedure
and breaks compatibility with the PC tool; we chose to fork off the 20150826 version).
</p>

<h1>Available versions</h1>

<p>
At present, there are two variants of the bootloader.<br>
Both variants use the same protocol, so the loader tool will work with either version.

<ul>
<li><b>20170626-autobaud</b> This is the closest to the original TSB loader tool. It uses bit banging to communicate via UART. Therefore it works with any processor with a digital pin. (also has the autobauding)
</li>
<li><b>20200727-fixedbaud</b> In this new version, the communication routines are fully re-writtem. It requires a processor with a Physical UART and uses the first UART (UART0). It is easily changeable to another UART with some minor code changes.
<ul>
<li>Requires a physical UART</li>
<li>Uses a fixed baud. By default it is set to 33.3kbps (which loads much faster compared to the usual 9.6k or 19.6k achieavble on the autobauding variant). Higher bauds may not work as expected to maintain cmpatility with one-wire mode (see next topic)</li>
<li>Supports One-wire (i.e. Rx and Tx shorted) or Full Duplex (RX and TX with independent lines) natively without needing any code change.</li>
<li>Observes a guard time when switching from Rx to Tx (to better accomodate One-wire mode)</li>
<li>Implements communication timeout: if the host stops comunicating for a few seconds, it exists the bootloader and attempts to boot application code.</li>
<li>A minium activation timeout is now observed. This ensures the bootloader remains accessible even if the Last Page (where bootloader configuraiton is stored) becomes corrupted. This way it is always possible to, at least, run the Emergency Erase option, instead of bricking the processor.</li>
<li>This also fixes a condition (that happens with all autobauding versiosn) where, if you connect the processor to a device that is already sending data, the autobauding routine might lock up the processor, especially if you're operating at medium to high bauds (>19.8kbps typically).<br/>
With this fixed baud version it won't happen; if booting into an active communications line, the bootloader will recognize data that is not the activation sequence and boot to application code pretty fast.</li>
<li> DRAWBACKS: requires the use of the native UART of the processor.
</ul>
</li>
</ul>


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
</p>

<h3>For the <b>20170626-autobaud</b> version:</h3>
<p>
<ul><li>Find section "PORTS" and specifiy the ports to use. <br/>Note that TSB can work on any digital pin (don't have to be the processor's UART pins)
     <br/>If you want to use TSB in Half Duplex, One Wire mode (Daisy chain network or also called Multi-drop network) set the RX and TX to the SAME pin.
</li>
<li>If you are using ATMEL Studio, select your processor model (target device). To do this, use the option in the toolbar or go to Project Properties, Select the Device section and change the target processor to the processor you are compiling for.
</li>  
<li>Finally, compile. Pay attention to the messages generated as they provide valuable verbose feedback.</li>
</ul>
</p>


<h3>For the 20200727-fixedbaud version:</h3>
<p>
This version is pretty much all set and ready to go. Just tweak the baud (if needed) and set the processor model.
<ul><li>Set the BAUD RATE. We recommend staying within 33.3k or slower (we tested 33.3k at 16Mhz successfully)</li>
<ul><li>By the default the code uses the registers for the first UART (UART0) and communicates throught that UART. IF you need a different UART you will need to edit the code where UART-related registers are used and change them to your UART. (again only IF YOU need it).</li>
<li>If you are using ATMEL Studio, select your processor model (target device). To do this, use the option in the toolbar or go to Project Properties, Select the Device section and change the target processor to the processor you are compiling for.
</li>  
<li>Finally, compile. Pay attention to the messages generated as they provide valuable verbose feedback.</li>
</ul>
</p>



<p><b>If you are not comfortable with modifying these PRTx, DDRx, ..., compiling or are unfamiliar with this</b>, you can try and use Julien's original
TSB loader tool (in FreeBasic for the PC). This tool is available from this repository as well (it is tsbloader_original). <br/>
It has the ability to generate Bootloader binaries  automatically for most processor models (the Autobauding variant) without the need for any
external compiler. It's only drawback is that it produces Bootloaders of version 20150826 which don't have the bugfix for #Issue 1 identified in the Issues section. (https://github.com/seedrobotics/tinysafeboot/issues/1)<br/>
If you won't be connecting your devices in a Daisy Chain or Multi Drop network, that version should be fine; if you do, then we strongly recommend using a newer version with this fixed.
</p>


<p>
<b>Changelog:</b></p>
<p>

- <b>20200727 - fixed baud</b></p>
<p>
This is a new version of TSB which uses a fixed baud.<br/>
This is the first release. The advantages and improvements of this version are listed in the version description above.
</p>


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

