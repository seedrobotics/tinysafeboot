<p>This is Julien Thomas' release 20150826 which is paired with the original tsb loader release also available from this git repository.</p>

<p>There seems to have been a subsequent release in 2016 with improvements to autobauding which changes the Emergency Erase procedure
and breaks compatibility with the PC tool; we chose to fork off the 20150826 version and therefore this is the version we have published on our Git.
</p>
<p>
Beware this version has a bug: in case a wrong password was sent to the bootloader it should go into an infitine loop
but if you send a \0 it will escape the loop and enter Emergency Erase Mode. If the actual Emergency Erase confirmation appears it WILL erase the device.
</p>
<p>
Check the latest_version folder where we have published the new version with this bugfix.