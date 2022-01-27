/* This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Tsbloader_adv
{
    partial class Program
    {
        private const byte RETURN_INVALID_CMDLINE = 1;
        private const byte RETURN_ERRORS_ENCOUNTERED = 2;
        private const byte RETURN_UNKOWN_OPTION_IN_CASE = 3;
        private const byte RETURN_SEEDEROS_COMMAND_FAILED = 4;
        private const byte RETURN_UNEXPECTED_ERROR = 5;
        private const byte MINIMUM_TIMEOUT_SETTING = 16;

        static int Main(string[] args)
        {
            /* install global Exception handler
             * Won't do much more than pretify'ing the error messages but better
             * fail gracefully that terribly
             */
            System.AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionTrapper;

            bool errors_encountered = false;

            CommandLineParser cmd_parser = new CommandLineParser();

            if (cmd_parser.parse_command_line(args) == false)
            {
                return RETURN_INVALID_CMDLINE;
            }

            /****** SEED EROS Bdrige mode enable/disable requests */
            if (cmd_parser.bootloader_operations.Contains(CommandLineParser.en_bootloader_operations.SEEDEROS_BRIDGEENABLE))
            {
                return EROS_EnableBridgeMode(ref cmd_parser);
            }

            if (cmd_parser.bootloader_operations.Contains(CommandLineParser.en_bootloader_operations.SEEDEROS_BRIDGEDISABLE))
            {
                return EROS_DisableBridgeMode(ref cmd_parser);
            }

            /****** Emergency Erase Request
                Emergency Erase restores bootloader access in case of lost password, by wiping
                the full FLASH and EEPROM memory areas
             */
            if (cmd_parser.bootloader_operations.Contains( CommandLineParser.en_bootloader_operations.EMERGENCY_ERASE)) {
                enEmergencyEraseConfirmResult eresult = ConfirmEmergencyErase(ref cmd_parser);

                if (eresult == enEmergencyEraseConfirmResult.ER_CONFIRMED)
                {
                    TSBInterfacing tsb = new TSBInterfacing(UpdateStatusOnScreen, AskQuestion_ToUser);
                    if (!tsb.EmergencyErase(cmd_parser.port_name, cmd_parser.baudrate_bps, cmd_parser.prewait_ms, cmd_parser.replytimeout_ms))
                    {
                        Console.WriteLine();
                        Console.WriteLine("> Error while performing Emergency Erase operation.");
                        return RETURN_ERRORS_ENCOUNTERED;
                    }
                    else
                    {
                        return 0;
                    }
                }
                else if (eresult == enEmergencyEraseConfirmResult.ER_ERROR)
                {
                    return RETURN_ERRORS_ENCOUNTERED;
                }
                else
                {
                    return 0;
                }
            }


            /********** Main cycle for multi-device bootloader operations 
                We will cycle through the list of passwords supplied
                (or use empty password if none supplied)

                On every iteration we will run the same operations but we will collect user
                options on each iteration (bc user may want different options for each
                device)
             */
            
            if (cmd_parser.bootloader_passwords.Count() == 0)
            {
                /* add a bogus entry with an empty password. Assuming user intends to start the session
                 * with no password. */
                cmd_parser.bootloader_passwords.Add("");
            }

            /* only advance to TSB if we have more commands lined up and requested at the command line */
            if (cmd_parser.bootloader_operations.Count == 0 )
            {
                Console.WriteLine();
                Console.WriteLine("> No bootloader actions specified. Ending session");
                return 0;
            }
            

            foreach (string pwd in cmd_parser.bootloader_passwords)
            {
                TSBInterfacing tsb = new TSBInterfacing(UpdateStatusOnScreen, AskQuestion_ToUser);
                bool activation_result = false;

                /* from TSB versions of 2020 onwards, we now have an operation timeout
                       which means the commands must get their complete input in a limited timespan

                      This means that, for setting password, timeout and/or magic bytes
                      we should ask the user for them beforehand so that they'r ready to go
                      If we wait for the user input while the sesison is active, the session
                      will likely timeout.   
                    */
                TSBInterfacing.str_tsb_session_data new_user_data = default(TSBInterfacing.str_tsb_session_data);
                new_user_data.magic_bytes = new byte[2]; // we need to explicit allocate space for this

                if (pwd.Length > 0)
                {
                    if (cmd_parser.activation_mode == CommandLineParser.en_bootloader_activation_mode.COLD_BOOT)
                    {
                        Console.WriteLine();
                        Console.WriteLine("> Preparing to activate bootloader for device with password: {0}", pwd);
                        CollectUserOptions_AndUpdateCachedSessionData(ref cmd_parser, ref new_user_data);

                        activation_result = tsb.ActivateBootloaderFromColdStart(cmd_parser.port_name, cmd_parser.baudrate_bps, cmd_parser.prewait_ms, cmd_parser.replytimeout_ms, pwd, cmd_parser.verbose_output);
                    }
                    else
                    {
                        Console.WriteLine("ERROR: Bootloader passwords can only be specified when activating in 'cold' [boot] mode.");
                        return RETURN_ERRORS_ENCOUNTERED;
                    }
                } 
                else if (cmd_parser.activation_mode == CommandLineParser.en_bootloader_activation_mode.COLD_BOOT) {
                    Console.WriteLine();
                    Console.WriteLine("> Preparing to activate bootloader (no device password specified)");
                    CollectUserOptions_AndUpdateCachedSessionData(ref cmd_parser, ref new_user_data);

                    activation_result = tsb.ActivateBootloaderFromColdStart(cmd_parser.port_name, cmd_parser.baudrate_bps, cmd_parser.prewait_ms, cmd_parser.replytimeout_ms, pwd, cmd_parser.verbose_output);
                }
                else if (cmd_parser.activation_mode == CommandLineParser.en_bootloader_activation_mode.LIVE_VIA_DYNAMIXEL)
                {
                    if (cmd_parser.dynid < 0 || cmd_parser.dynid > 253)
                    {
                        Console.WriteLine("ERROR: 'live' bootloader activation mode was selected but the '-dynid' parameter was not given or is invalid. Please set a valid '-dynid' for 'live' activation mode.");
                        return RETURN_ERRORS_ENCOUNTERED;
                    }

                    Console.WriteLine();
                    Console.WriteLine("> Preparing to activate bootloader 'live', on device with DynID {0}", cmd_parser.dynid);
                    CollectUserOptions_AndUpdateCachedSessionData(ref cmd_parser, ref new_user_data);

                    // baud after activation should not be hard coded as we may need to speak at higher bauds if going through the EROS board.
                    // timeout for bootloader activation must exceed the time set in the servo firmware.
                    activation_result = tsb.ActivateBootloaderFromDynamixel(cmd_parser.port_name, cmd_parser.baudrate_bps, 9600, (byte) cmd_parser.dynid, 3500, 6000, TSBInterfacing.en_dyn_protocol_version.DYNAMIXEL_1, cmd_parser.verbose_output);
                }

                if (activation_result == true)
                {
                    /* Bootloader is active. Print all bootloader information */
                    PrintDeviceInfo(tsb);

                    /*  check if the user asked to tag the file names.
                        *  This MUST be done here for the cases where we use multiple
                        *  passwords; in that case, we have sessins starting at different times
                        *  (initiated here) and also different passwords
                        */
                    string tag = string.Format("_{0:yyMMdd}_{0:HHmmss}", DateTime.Now);
                    if (pwd.Length > 0) tag += string.Format("_{0}", pwd);

                    string eep_filename = cmd_parser.eeprom_file_name;
                    string flash_filename = cmd_parser.flash_file_name;

                    if (cmd_parser.tag_eepromfilename_withdatetimepwd && !string.IsNullOrEmpty(cmd_parser.eeprom_file_name))
                    {
                        eep_filename = AddTagToFilename(cmd_parser.eeprom_file_name, tag);
                    }

                    if (cmd_parser.tag_flashfilename_withdatetimepwd && !string.IsNullOrEmpty(cmd_parser.flash_file_name))
                    {
                        flash_filename = AddTagToFilename(cmd_parser.flash_file_name, tag);
                    }


                    bool request_last_page_write = false;
                    /* loop through the various operations requested */
                    foreach (CommandLineParser.en_bootloader_operations bootloader_op in cmd_parser.bootloader_operations)
                    {
                        switch (bootloader_op)
                        {
                            case CommandLineParser.en_bootloader_operations.DISPLAY_DEVICE_INFO:
                                /* do nothing; when we activated TSB it already displayed the device info */
                                break;

                            case CommandLineParser.en_bootloader_operations.EEP_ERASE:
                                if (!tsb.EEProm_Erase())
                                {
                                    Console.WriteLine();
                                    Console.WriteLine("> Error Erasing EEProm.");
                                    errors_encountered = true;
                                }
                                break;

                            case CommandLineParser.en_bootloader_operations.EEP_WRITE:
                                if (!tsb.EEProm_Write(eep_filename))
                                {
                                    Console.WriteLine();
                                    Console.WriteLine("> Error Writing EEPROM.");
                                    errors_encountered = true;
                                }
                                break;

                            case CommandLineParser.en_bootloader_operations.EEP_VERIFY:
                                if (!tsb.EEProm_Verify(eep_filename))
                                {
                                    Console.WriteLine();
                                    Console.WriteLine("> Error Verifying EEPROM.");
                                    errors_encountered = true;
                                }
                                break;

                            case CommandLineParser.en_bootloader_operations.EEP_READ:
                                if (!tsb.EEProm_Read(eep_filename))
                                {
                                    Console.WriteLine();
                                    Console.WriteLine("> Error Reading EEPROM.");
                                    errors_encountered = true;
                                }
                                break;

                            case CommandLineParser.en_bootloader_operations.FW_ERASE:
                                if (!tsb.Flash_Erase())
                                {
                                    Console.WriteLine();
                                    Console.WriteLine("> Error Erasing Flash.");
                                    errors_encountered = true;
                                }
                                break;

                            case CommandLineParser.en_bootloader_operations.FW_WRITE:
                                if (!tsb.Flash_Write(flash_filename))
                                {
                                    Console.WriteLine();
                                    Console.WriteLine("> Error Writing Flash.");
                                    errors_encountered = true;
                                }
                                break;

                            case CommandLineParser.en_bootloader_operations.FW_VERIFY:
                                if (!tsb.Flash_Verify(flash_filename))
                                {
                                    Console.WriteLine();
                                    Console.WriteLine("> Error Verifying Flash.");
                                    errors_encountered = true;
                                }
                                break;

                            case CommandLineParser.en_bootloader_operations.FW_READ:
                                if (!tsb.Flash_Read(flash_filename))
                                {
                                    Console.WriteLine();
                                    Console.WriteLine("> Error Reading Flash.");
                                    errors_encountered = true;
                                }
                                break;

                            case CommandLineParser.en_bootloader_operations.PASSWORD_CHANGE:
                                tsb.session_data_.password = new_user_data.password;
                                request_last_page_write = true;
                                break;

                            case CommandLineParser.en_bootloader_operations.TIMEOUT_CHANGE:
                                tsb.session_data_.timeout = new_user_data.timeout;
                                request_last_page_write = true;
                                break;


                            case CommandLineParser.en_bootloader_operations.WRITE_MAGIC_BYTES:
                                tsb.session_data_.magic_bytes[0] = new_user_data.magic_bytes[0];
                                tsb.session_data_.magic_bytes[1] = new_user_data.magic_bytes[1];
                                request_last_page_write = true;
                                break;

                            case CommandLineParser.en_bootloader_operations.PATCH_DAISY_CHAIN_BUG:
                                request_last_page_write = true; // a simple last page write, applies the patch
                                break;

                            default:
                                Console.WriteLine("ERROR: Unknown Bootloader operation in function main()");
                                return RETURN_UNKOWN_OPTION_IN_CASE;
                        }
                    }

                    // check if there are updated settings in last page, and commit them
                    if (request_last_page_write)
                    {
                        if (!tsb.LastPage_Write())
                        {
                            Console.WriteLine();
                            Console.WriteLine("> Error updating bootloader configuration data.");
                            Console.WriteLine("> (Daisy chain patch, Timeout, password and/or Magic bytes might not have been updated)");
                            errors_encountered = true;
                        }
                        request_last_page_write = false;
                    }

                    tsb.DeactivateBootloader();
                }
                else
                {
                    Console.WriteLine();
                    Console.WriteLine("> Could not activate bootloader for the selected device.");
                    Console.WriteLine("> Hint: Is password correct? Is BAUD rate correct?");
                    tsb.DeactivateBootloader();
                    errors_encountered = true;
                }
            }
            
            if (errors_encountered)
            {
                Console.WriteLine();
                Console.WriteLine("> Requested operations completed with errors. Please review the error messages above.");
                return RETURN_ERRORS_ENCOUNTERED;
            }
            else
            {
                Console.WriteLine();
                Console.WriteLine("> All operations complete.");
                return 0;
            }
        }

        static void PrintDeviceInfo(TSBInterfacing tsb)
        {
            Console.WriteLine();
            Console.WriteLine("==================================================================");
            Console.WriteLine("|   Bootloader information   |     Device Information            |");
            Console.WriteLine("==================================================================");
            Console.WriteLine(" {0,-28}  {1, -35}", string.Format("Version  : {0}({1:X})", tsb.session_data_.builddate, tsb.session_data_.status_buildver), string.Format("Name     : {0}", tsb.session_data_.device_name));
            Console.WriteLine(" {0,-28}  {1, -35}", string.Format("Password : {0}", tsb.session_data_.password),  string.Format("Signature: {0}", tsb.session_data_.device_signature));
            Console.WriteLine(" {0,-28}  {1, -35}", string.Format("Timeout  : {0}", tsb.session_data_.timeout), string.Format("Flash    : {0}b", tsb.session_data_.flash_size));
            Console.WriteLine(" {0,-28}  {1, -35}", string.Format("MagicByte: 0x{0:X02} 0x{1:X02}", tsb.session_data_.magic_bytes[0], tsb.session_data_.magic_bytes[1]),                                       string.Format("Appflash : {0}b", tsb.session_data_.appflash));
            Console.WriteLine(" {0,-28}  {1, -35}", "Patch for Daisy Chain", string.Format("EEProm   : {0}b", tsb.session_data_.eeprom_size));
            Console.WriteLine(" {0,-28}  {1, -35}", string.Format("operation: {0}", tsb.session_data_.daisychain_patch_in_lastpage ? "Applied" : "Not Applied"), string.Format("Pagesize : {0}b", tsb.session_data_.pagesize));
            Console.WriteLine(" {0,-28}  {1, -35}", "", string.Format("AppJump  : {0:X4}", tsb.session_data_.appjump_address));
            Console.WriteLine("==================================================================");
        }

        static string AddTagToFilename(string filename, string tag)
        {
            string new_name;
            int dot_pos = filename.LastIndexOf('.');
            if (dot_pos > 0)
            {
                new_name = filename.Substring(0, dot_pos) + tag + filename.Substring(dot_pos);
            }
            else
            {
                new_name = filename + tag;
            }

            return new_name;
        }

        static bool request_confirm()
        {
            string c = "";

            while (c.ToLower() != "y" && c.ToLower() != "n")
            {
                Console.Write("Confirm (y/n)? ");
                c = Console.ReadLine();
            }

            if (c.ToString().ToLower() == "y")
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        static void wait_for_reply(System.IO.Ports.SerialPort serial_port, int timeout)
        {
            long millis = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
            long end_millis = millis + timeout;
            do
            {
                millis = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
            } while (serial_port.BytesToRead < 1 && millis < end_millis);

        }

        static bool get_YesNo_reply()
        {
            string reply;
            do
            {
                reply = Console.ReadLine();
            } while (string.IsNullOrEmpty(reply));

            if (reply.ToUpper() == "Y")
            {
                return true;
            } else
            {
                return false;
            }
        }


        static string AskQuestion_ToUser(string question)
        {
            // must use WriteLine, to force a newline, so that the question is sent to the buffered
            // app in case we're redirecting the output
            Console.WriteLine(question);
            return Console.ReadLine();
        }

        static void UpdateStatusOnScreen(string msg, int progess_percent, bool msg_contains_error, TSBInterfacing.en_cb_status_update_lineending line_ending_behaviour)
        {
            // build the message all in one go so that we only call console.write once
            // this should speed up performance significantly if output is redirected
            string s = "";

            if (line_ending_behaviour == TSBInterfacing.en_cb_status_update_lineending.CBSU_CARRIAGE_RETURN_TO_BOL ||
                line_ending_behaviour == TSBInterfacing.en_cb_status_update_lineending.CBSU_CARRIAGE_RETURN_TO_BOL_AND_NEWLINE)
            {
                s += "\r";
            }

            s += msg;

            if (progess_percent>=0)
            {
                s += string.Format(" ({0}%)", progess_percent);
            }

            if (line_ending_behaviour == TSBInterfacing.en_cb_status_update_lineending.CBSU_NEWLINE || 
                line_ending_behaviour == TSBInterfacing.en_cb_status_update_lineending.CBSU_CARRIAGE_RETURN_TO_BOL_AND_NEWLINE)
            {
                s += Environment.NewLine;
            }

            Console.Write(s);
        }

        static void UnhandledExceptionTrapper(object sender, UnhandledExceptionEventArgs e)
        {
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine("=======================================");
            Console.WriteLine(" An UNEXPECTED ERROR occured. Details: ");
            Console.WriteLine("=======================================");
            Console.WriteLine(e.ExceptionObject.ToString());
            Console.WriteLine();
            Environment.Exit(RETURN_UNEXPECTED_ERROR);
        }

    }
}
