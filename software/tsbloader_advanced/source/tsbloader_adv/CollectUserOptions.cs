using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tsbloader_adv
{
    enum enEmergencyEraseConfirmResult { ER_CONFIRMED, ER_CANCELLED, ER_ERROR };
    partial class Program
    {


        static enEmergencyEraseConfirmResult ConfirmEmergencyErase(ref CommandLineParser cmd_parser)
        {
            if (cmd_parser.activation_mode != CommandLineParser.en_bootloader_activation_mode.COLD_BOOT)
            {
                Console.WriteLine("ERROR: Emergency Erase can only be done in Cold [boot] activation mode.");
                return enEmergencyEraseConfirmResult.ER_ERROR;
            }

            Console.WriteLine();
            Console.WriteLine("WARNING: Emergency Erase deletes all Application Flash");
            Console.WriteLine("and EEPROM data, as well as:");
            Console.WriteLine("  Timeout, Password and Magic Byte!");
            Console.WriteLine("No Firmware or EEPROM data will be left on the device after");
            Console.WriteLine("this operation.");
            Console.WriteLine("This creates a clean TSB configuration with default values.");
            Console.WriteLine();
            Console.WriteLine("IMPORTANT! An Emergency Erase DOESN'T target an Individual");
            Console.WriteLine("device; if the device is connected on a BUS (Daisy chain),");
            Console.WriteLine("ALL devices on the BUS will perform the Emergency Erase.");
            Console.WriteLine();
            Console.WriteLine("This function should ONLY be used by experienced users.");
            Console.WriteLine();
            Console.Write("Do you fully understand the information above? (Y/n) ");
            if (!get_YesNo_reply())
            {
                Console.WriteLine();
                Console.WriteLine("Operation cancelled. Nothing was done.");
                return enEmergencyEraseConfirmResult.ER_CANCELLED;
            }
            Console.Write("Do you wish to continue? (Y/n) ");
            if (!get_YesNo_reply())
            {
                Console.WriteLine();
                Console.WriteLine("Operation cancelled. Nothing was done.");
                return enEmergencyEraseConfirmResult.ER_CANCELLED;
            }

            return enEmergencyEraseConfirmResult.ER_CONFIRMED;
        }


        static void CollectUserOptions_AndUpdateCachedSessionData(ref CommandLineParser cmd_parser, ref TSBInterfacing.str_tsb_session_data new_user_data)
        {

            Console.WriteLine("> Collecting user options");
            if (cmd_parser.bootloader_operations.Contains(CommandLineParser.en_bootloader_operations.PASSWORD_CHANGE))
            {
                string new_pwd;
                while (true)
                {
                    Console.WriteLine("Please enter the _new_ Password (max. {0} chars): ", TSBInterfacing.max_pwd_length);
                    new_pwd = Console.ReadLine();

                    if (string.IsNullOrEmpty(new_pwd))
                    {
                        new_pwd = "";
                        Console.WriteLine("No password specified. The bootloader will be accessible without password.");
                        break;
                    }
                    else if (new_pwd.Length > Tsbloader_adv.TSBInterfacing.max_pwd_length)
                    {
                        Console.WriteLine("ERROR: Password is too long. Maximum password length supported by this tool is {0} characters.", Tsbloader_adv.TSBInterfacing.max_pwd_length);
                    }
                    else
                    {
                        Console.WriteLine("New password will be set to: {0}", new_pwd);
                        Console.WriteLine("(if you lose your password, you may use the Emergency Erase option '-XXX')");
                        break;
                    }
                }
                new_user_data.password = new_pwd;
                Console.WriteLine();
            }

            if (cmd_parser.bootloader_operations.Contains(CommandLineParser.en_bootloader_operations.TIMEOUT_CHANGE))
            {
                int new_timeout_setting;

                while (true)
                {
                    Console.WriteLine("Please enter new Timeout setting: ");
                    string new_timeout = Console.ReadLine();

                    if (string.IsNullOrEmpty(new_timeout))
                    {
                        Console.WriteLine("Invalid timeout specified. Timeout must be number between {0} and 255.", MINIMUM_TIMEOUT_SETTING);
                    }
                    else
                    {
                        if (!int.TryParse(new_timeout, out new_timeout_setting))
                        {
                            Console.WriteLine("Invalid timeout specified. Timeout must be a number, between {0} and 255.", MINIMUM_TIMEOUT_SETTING);
                        }
                        else if (new_timeout_setting < MINIMUM_TIMEOUT_SETTING || new_timeout_setting > 255)
                        {
                            Console.WriteLine("Invalid timeout value specified. Timeout must be between {0} and 255.", MINIMUM_TIMEOUT_SETTING);
                        }
                        else
                        {
                            new_user_data.timeout = (byte)new_timeout_setting;
                            break;
                        }
                    }
                }
                Console.WriteLine();

            }

            if (cmd_parser.bootloader_operations.Contains(CommandLineParser.en_bootloader_operations.WRITE_MAGIC_BYTES))
            {
                /* ask the user for the new magic bytes */
                string[] s_magic_bytes_names = { "First", "Second" };

                //Console.WriteLine("Magic Bytes currently set to [0x{0:X02}] [0x{1:X02}]", tsb.session_data_.magic_bytes[0], tsb.session_data_.magic_bytes[1]);

                for (byte b = 0; b < 2; b++)
                {
                    while (true)
                    {                        
                        Console.WriteLine("Enter the {0} Magic Byte, in HEX format (ie. for '0xFE', just enter 'FE'): 0x", s_magic_bytes_names[b]);
                        string magic_byte = Console.ReadLine();

                        if (string.IsNullOrWhiteSpace(magic_byte))
                        {
                            Console.WriteLine("Empty Magic Byte specified; will be set to 0xFF");
                            magic_byte = "FF";
                        }

                        int mb;
                        try
                        {
                            // based on tips for parsing HEX here https://theburningmonk.com/2010/02/converting-hex-to-int-in-csharp/
                            mb = int.Parse(magic_byte, System.Globalization.NumberStyles.HexNumber);

                            if (mb > 255)
                            {
                                Console.WriteLine("ERROR: HEX number too big. Magic bytes are of BYTE type in range 0x0 to 0xFF.");
                            }
                            else
                            {
                                new_user_data.magic_bytes[b] = (byte)mb;
                                break;
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("ERROR: Invalid HEX value. Magic bytes are set in HEX. Example: to set to 0xA7, type 'A7' and ommit the '0x' part.");
                        }
                    }
                    Console.WriteLine();
                }

                Console.WriteLine("Magic Bytes will be set to: [0x{0:X02}] [0x{1:X02}]", new_user_data.magic_bytes[0], new_user_data.magic_bytes[1]);
                Console.WriteLine();
            }

            Console.WriteLine("> Proceeding to Activate Bootloader.");
        }
    }
}
