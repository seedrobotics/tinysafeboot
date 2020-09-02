using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tsbloader_adv
{
    partial class Program
    {
        static byte EROS_EnableBridgeMode(ref CommandLineParser cmd_parser)
        {
            System.IO.Ports.SerialPort serial_port = new System.IO.Ports.SerialPort();

            serial_port.BaudRate = cmd_parser.baudrate_bps;
            serial_port.PortName = cmd_parser.port_name;
            serial_port.Encoding = Encoding.ASCII;

            try
            {
                Console.WriteLine();
                Console.WriteLine("> Seed Robotics Eros: Enabling bridge mode...");

                serial_port.Open();
                System.Threading.Thread.Sleep(cmd_parser.prewait_ms);

                /* clear out any wrong/previous commands that might exist on the board buffer
                 * This ensures we're at a clean state of the command parser when we send the bridge enable command */
                serial_port.Write("\r\n");
                wait_for_reply(serial_port, cmd_parser.replytimeout_ms);
                //System.Threading.Thread.Sleep(500); /* wait another 500ms to fully receive all comms */
                serial_port.ReadExisting(); /* read and discard */

                /* now send the actual command */
                /* 20 = 4 + 16 (Int bus to USB with DTR reset) */
                serial_port.Write("commbridge 20\r\n"); /* this command is specific to the EROS firmware CONSOLE; if you
                                                         * don't have a console on the port you are trying to open, this is
                                                         * not necessary;
                                                         * It bridges the USB interface to the Internal bus, using the
                                                         * a special bridge mode built for flashing the servos
                                                         * (it will auto reset the bus on Serial connect, uses a softserial library to
                                                         * cope with the device side of TSB weird timing, etc.) */

                wait_for_reply(serial_port, cmd_parser.replytimeout_ms);

                if (serial_port.BytesToRead < 1)
                {
                    Console.WriteLine("ERROR");
                    Console.WriteLine("No reply from the Seed Eros device.");
                    Console.WriteLine("(Is this the Serial Port of the Seed Eros device? Is the device already in bridge mode?)");
                    return RETURN_SEEDEROS_COMMAND_FAILED;
                }
                else
                {
                    Console.Write("  < Reply: ");
                    while (serial_port.BytesToRead > 0)
                    {
                        Console.WriteLine(serial_port.ReadLine().ToString());
                    }
                }

                serial_port.Close();

                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR");
                Console.WriteLine(ex.Message);
                Console.WriteLine();
                Console.WriteLine("> Pre requisite command (SeedEros) failed. Ending session.");
                return RETURN_SEEDEROS_COMMAND_FAILED;
            }

        }


        static byte EROS_DisableBridgeMode(ref CommandLineParser cmd_parser)
        {
            System.IO.Ports.SerialPort serial_port = new System.IO.Ports.SerialPort();

            /* to exit bridge mode, we open and close the port at 1200 bps */
            serial_port.BaudRate = 1200;
            serial_port.PortName = cmd_parser.port_name;
            serial_port.Encoding = Encoding.ASCII;

            try
            {
                Console.WriteLine();
                Console.Write("> Seed Robotics Eros: Sending Host request to disable bridge mode...");

                serial_port.Open();
                serial_port.DtrEnable = true; /* on Mono / .Net we apparently need to explicitly set DTR */

                System.Threading.Thread.Sleep(800); /* wait a moment */

                serial_port.Close();
                System.Threading.Thread.Sleep(200); /* wait some other moment; just give Serial driver some spare time */

                Console.WriteLine(" Done.\n");
                Console.WriteLine("  ( If your firmware supports host request to exit bridge mode, the unit's LEDs should be back to white/three colours.");
                Console.WriteLine("    If the LED colour hasn't changed, you need to manually power cycle your unit (including disconnecting the USB cable) to resume regular operation.)");

                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR");
                Console.WriteLine(ex.Message);
                Console.WriteLine();
                Console.WriteLine("> Post process command (SeedEros) failed.");
                return RETURN_SEEDEROS_COMMAND_FAILED;
            }
        }
    }
}
