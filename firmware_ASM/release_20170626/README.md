<p>This code is essentially a bugfix attempt at resolving #Issue 1 identified in the Issues section. (https://github.com/seedrobotics/tinysafeboot/issues/1)
</p><p>
This issue is apparently a bug and the only modification has been the addition of 2 lines to ensure that the bootloader does not go into Emergency Erase confirmation mode
in case we have already received a wrong password. (in that case, the correct behaviour would be to stay in an infinite loop, pulling characters
to smoothen the power profile and avoid attacks based on power profile analysis).
</p>
<p>
<u>This code has not been extensively tested</u>. From the numbers crunched, it seems the two instructions that were added still fit in the calculations
that are made and the code will still align properly but please test before real-world deployment!
<p>
<b>To Compile this code</p> you need ATMEL Studio or gavrasm. Both are free.</p>
<ul><li>ATMEL Studio is quite a large download but it's fairly user friendly.<br/>The files in this folder are for an ATMEL Studio 7 solution.</li>
<li>gavrasm is much lighter and seems straightforward as well (http://www.avr-asm-tutorial.net/gavrasm/index_en.html)<br/>If you'll be using gavrasm, you probably only need the <i>main.asm</i> file from this folder.</li>
</p><p>
In either case you will need to modify the header and set the appropriate PORTx, DDRx and PINx for RX and TX (set them to the same pin if you
want Half Duplex, One wire operation).</p>
<p><b>If you are not comfortable with modifying these PRTx, DDRx, ... or are unfamiliar with this</b>, it is best that you use Julien's original
TSB loader tool (in FreeBasic). This tool has the ability to generate Bootloader binaries for multiple processors without the need for any
external compiler. It's main disadvantage is that it produces Bootloaders without this bugfix.<br/>
However if you won't be using your device in a Daisy chain and just stand alone, you can probably get your work done using the code built by Julien's TSB Loader tool.

