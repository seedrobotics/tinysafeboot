# TSB bootloader without the AutoBauding feature

The original TSB bootloader includes an Autobauding feature that attempts detecting your baud and matching it automatically.

There are a number of advantages to this. One of the most significant is that it is independent of the clock rate.
This means you can install the same bootloader version onto processors at different clock rates (although the processor model must still be the same)

Another very important advantage is that it does not rely on the UART pins and instead simulates a UART on ANY digital pin of the processor. For ATtiny’s this is still the only (and best) option available.

This project forks off version 20170626, the latest stable release with autobauding.


## Why no Autobauding if it's all that great

The problem is bootloader activation and possible bootloader lock up under certain conditions.

In reality the bootloader activation sequence "@@@" is not really verified by the bootloader.

Instead it uses the knowledge of the binary representation of @@@ to detect 24 transitions to 0.
This way, timing these transitions is the foundation of its autobauding feature.

### How can it go wrong?

If you attempt communication with the device (i.e. if you send UART data) that is not "@@@" before the bootloader times out, it will not verify that it's not an "@@@".
Instead it will time the transitions to 0, thus coming to a baud that's either unachievable or just plain wrong and unknown to the user because the characters sent were not the expected "@@@".

As such the bootloader activates at a wrong, unknown or unachievable baud rate.
This is especially problematic if you use high baud rates.

So, as you can see, this is not the typical bootloader activation where, when it sees the wrong activation sequence it boots; instead it won't validate the characters and instead deduct a baud rate from the transitions to 0.

Once the bootloader activates, it will stay active until it receives an invalid command or a command to exit.

Once again, because the baud was wrongly determined, you can't communicate with it and thus you can send any exit command so it will stay active until a power cycle occurs.

In words, your device locks up in the bootloader.

### Oh then autobauding is bad

No autobauding is not bad. 

It's just not ideal on scenarios where you can't guarantee there will be no communication during the bootloader timeout or if you want to accelerate boot by forcing the bootloader to boot the app prematurely by sending a wrong activation character.

There are still great points towards autobauding:

* The independence from the processor clock and the ability to achieve different bauds
* The ability to use ANY digital pin for communication (even only 1 pin, in one wire mode supported by TSB), whereas if you rely on UART you are limited to the UART pins for communication.
* In the case of processors without a native UART (such as the ATtiny's) autobauding is still the only option for now.


### Other advantages of a fixed baud

The following are advantages of a fixed baud:
* Higher baud rates can be achieved (for example on an 8Mhz ATMEGA the maximum we could achieve on autobaud were 19200, whereas on UART you can go all the way up to 500kbps or 1Mbps although staying at a more friendly 57600 or 155200 is likely a better idea)
* Higher bauds mean faster flashing and verification (but limited by the time it takes to write and read flash or eeprom)
* You can force the bootloader to boot prematurely (before the timeout ends) by sending a wrong activation character
* You'll never get locked into unknown baud, active bootloader until next reboot.


## Approach for a fixed baud

The idea is to rewrite ONLY the part that deals with communication.
The other logic, including all bootloader functionalities and capabilities will be untouched.

You need to bear in mind that using a fixed baud means relying on the UBRR register. This register works in relation to the system clock.
Therefore for every clock rate you need different UBRR values (and thus a different complied HEX) to achieve the same baud rate.

Another important goal is that we want to maintain one-wire capability (meaning RX and TX would be shorted).

Having one-wire capability means we have RX normally active and only when we Transmit we disable RX and enable TX.
This practice also enables connection of devices in a Daisy chain by not having TX active pulling HIGH at all times.

Another important aspect for one-wire is that we can only turn RX back on once the TXC flag is asserted and NOT when the UDRE flag asserts.

* On UDRE assertion one typically loads a new character to be sent; however UDRE asserts while the UART shift register is still shifting out the last byte but it's ready to take in a new one, which allows maximum performance. The processor can do this because the UDR is a double buffered register.
* TXC asserts later, when the shift register completes shifting out the byte. We can only transition from disabling TX to enabling RX on TXC; if we do it on UDRE, RX will be enabled while bits are still being shifted out and thus, if RX and TX are shorted for One wire operation, we'll "see" the tail of the last character being sent, received back in RX (as a whole strange character but looking at the bits in a logic analyzer you can see it's the tail).

Relying on TXC reduces performance but ensures correct operation in one-wire mode (where TX and RX are shorted) and ensures proper operation of daisy-chain connections.


## License

This project is licensed under the GPL V3 License

## Acknowledgments

* Julien Thomas excellent TSB work http://jtxp.org/tech/tinysafeboot_en.htm

This repository is merely a public posting of his work and intends to merely improve on his already excellent work.
