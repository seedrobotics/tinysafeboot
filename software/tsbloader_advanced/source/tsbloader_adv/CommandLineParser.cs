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
using System.Reflection;

namespace Tsbloader_adv
{
     class CommandLineParser
    {

    /* parse command line. Options are:
        * -port=[port name]
        * -baud=[baud rate bps. Default is 9600]
        * -prewait=[millisecs to wait after opening the port and before sending the activation char. Default is 200]
        * -replytimeout=[milliseconds to wait for the bootloader to respond before considering a timeout. Default is 2000]
        * -fop=[fimware operation(s) to perform. e=erase, w=write, v=verify, r=read. Operations are performed in the order they are written]
        * -eop=[eeprom operation(s) to perform. e=erase, w=write, v=verify, r=read. Operations are performed in the order they are written]
        * -xop=[extended operations to perform. T=set timeout, P=set password.]
        * -ffile=[file name for flash data. For flash related options]
        * -efile=[file name for eeprom data. For EEPROM related options]
        * -seederos=bren  
        * -pwd=[comma separated list of bootloader passwords if addressing one or several password protected devices]
        * -XXX [performs an emergency erase. If XXX is specified, no -fop, -eop, -xop options are allowed]
        * -?, -h Displays help screen 
        * -acmode=cold/live  -- bootloader activation mode. Cold is activation from reset; live is by
        *                       activating the bootloader with the system running
        * -dynid Dynamixel ID of the device to activate in live mode.
        */

         private const int default_prewait_ms = 300;
         private const int default_replytimeout_ms = 1500;

        public enum en_bootloader_activation_mode
        { COLD_BOOT, LIVE_VIA_DYNAMIXEL }

        public enum en_bootloader_operations
        {
            EEP_ERASE, EEP_WRITE, EEP_VERIFY, EEP_READ, FW_ERASE, FW_WRITE, FW_VERIFY, FW_READ,
            TIMEOUT_CHANGE, PASSWORD_CHANGE, WRITE_MAGIC_BYTES, EMERGENCY_ERASE, SEEDEROS_BRIDGEENABLE, SEEDEROS_BRIDGEDISABLE, DISPLAY_DEVICE_INFO,
            PATCH_DAISY_CHAIN_BUG
        }

        public List<en_bootloader_operations> bootloader_operations;
        public List<String> bootloader_passwords;

        public string port_name, flash_file_name, eeprom_file_name;
        public int baudrate_bps;
        public int prewait_ms, replytimeout_ms;
        public int dynid;
        public en_bootloader_activation_mode activation_mode;
        public bool tag_eepromfilename_withdatetimepwd;
        public bool tag_flashfilename_withdatetimepwd;
        

        private bool port_found_, baud_found_, prewait_found_, timeout_found_, ffile_found_, efile_found_, pwd_found_, xxx_found_, fop_found_,
            eop_found_, xop_found_, tagefile_found_, tagffile_found_, seederos_found_, patch_daisychain_found_, display_device_info_found_,
            acmode_found_, dynid_found_;

        public CommandLineParser()
        {
            bootloader_operations = new List<en_bootloader_operations>();
            bootloader_passwords = new List<string>();

            /* set defaults */
            baudrate_bps = 9600;
            prewait_ms = default_prewait_ms;
            replytimeout_ms = default_replytimeout_ms;
            activation_mode = en_bootloader_activation_mode.COLD_BOOT; // default is cold boot

            dynid = -1;
        }

         public bool parse_command_line(String[] args)
         {
             if (args.Count() == 0)
             {
                 display_usage();
                 return false;
             }

             foreach (string s in args)
             {
                 string[] split = s.Split('=');
                 if (split.Count() > 2)
                 {
                     throw_error_invalid_param(s);
                     return false;
                 }
                 else if (split.Count() == 1)
                 {
                     switch (split[0].ToLower())
                     {
                         case "-?":
                         case "-h":
                             display_usage();
                             return false;

                         case "-xxx":
                             bootloader_operations.Add(en_bootloader_operations.EMERGENCY_ERASE);
                             xxx_found_ = true;
                             break;

                         case "-tagefile":
                             if (tagefile_found_) { throw_error_duplicate_param(split[0]); return false; }
                             tag_eepromfilename_withdatetimepwd = true;
                             tagefile_found_ = true;
                             break;

                         case "-tagffile":
                             if (tagffile_found_) { throw_error_duplicate_param(split[0]); return false; }
                             tag_flashfilename_withdatetimepwd = true;
                             tagffile_found_ = true;
                             break;  

                         case "-patchdaisychain":
                             if (patch_daisychain_found_) { throw_error_duplicate_param(split[0]); return false; }
                             bootloader_operations.Add(en_bootloader_operations.PATCH_DAISY_CHAIN_BUG);
                             patch_daisychain_found_ = true;
                             break; 

                         case "-i":
                            if (display_device_info_found_) { throw_error_duplicate_param(split[0]); return false; }
                            display_device_info_found_ = true;
                            bootloader_operations.Add(en_bootloader_operations.DISPLAY_DEVICE_INFO);
                            break;
                       
                         default:
                             throw_error_invalid_param(s);
                             return false;
                     }
                 }
                 else  /* we have precisely 2 arguments */
                 {
                     switch (split[0].ToLower())
                     {
                         case "-port":
                             if (port_found_) { throw_error_duplicate_param(split[0]); return false; }
                             port_name = split[1];

                             if (port_name.Length == 0)
                             {
                                 throw_error("Port name is not correctly specified.");
                                 return false;
                             }

                             port_found_ = true;
                             break;
                             
                         case "-baud":
                             if (baud_found_) { throw_error_duplicate_param(split[0]); return false; }
                             if (!int.TryParse(split[1], out baudrate_bps))
                             {
                                 throw_error(string.Format("Invalid argument '{0}' passed to '{1}'.", split[1], split[0]));
                                 return false;
                             }
                             else
                             {
                                 /* if it success, the baud rate is already stored in baudrate_bps */
                                 baud_found_ = true;
                             }

                             baudrate_bps = Math.Abs(baudrate_bps); /* ensure we use a positive number */

                             break;

                        case "-acmode":
                            if (acmode_found_) { throw_error_duplicate_param(split[0]); return false; }
                            split[1] = split[1].ToLower();

                            if (split[1] == "cold")
                            {
                                activation_mode = en_bootloader_activation_mode.COLD_BOOT;
                                acmode_found_ = true;
                            }
                            else if (split[1] == "live")
                            {
                                activation_mode = en_bootloader_activation_mode.LIVE_VIA_DYNAMIXEL;
                                acmode_found_ = true;
                            } else
                            {
                                throw_error(string.Format("Invalid argument '{0}' passed to '{1}'. Must be 'cold' or 'live'.", split[1], split[0]));
                                return false;
                            }
                            break;

                        case "-dynid":
                            if (dynid_found_) { throw_error_duplicate_param(split[0]); return false; }
                            if (!int.TryParse(split[1], out dynid))
                            {
                                throw_error(string.Format("Invalid argument '{0}' passed to '{1}'.", split[1], split[0]));
                                return false;
                            }
                            else
                            {
                                /* if it success, the baud rate is already stored in baudrate_bps */
                                dynid_found_ = true;
                            }
                            break;

                        case "-prewait":
                             if (prewait_found_) { throw_error_duplicate_param(split[0]); return false; }
                             if (!int.TryParse(split[1], out prewait_ms))
                             {
                                 throw_error(string.Format("Invalid argument '{0}' passed to '{1}'.", split[1], split[0]));
                                 return false;
                             }
                             else
                             {
                                 /* if it success, the baud rate is already stored in baudrate_bps */
                                 prewait_found_ = true;
                             }

                             prewait_ms = Math.Abs(prewait_ms); /* ensure we use a positive number */
                             break;

                         case "-replytimeout":
                             if (timeout_found_) { throw_error_duplicate_param(split[0]); return false; }
                             if (!int.TryParse(split[1], out replytimeout_ms))
                             {
                                 throw_error(string.Format("Invalid argument '{0}' passed to '{1}'.", split[1], split[0]));
                                 return false;
                             }
                             else
                             {
                                 /* if it success, the baud rate is already stored in baudrate_bps */
                                 timeout_found_ = true;
                             }

                             replytimeout_ms = Math.Abs(replytimeout_ms); /* ensure we use a positive number */
                             break;

                         case "-fop":
                             if (fop_found_) { throw_error_duplicate_param(split[0]); return false; }

                             foreach (char c in split[1].ToLower())
                             {
                                 switch(c) {
                                     case 'e':
                                        bootloader_operations.Add(en_bootloader_operations.FW_ERASE);
                                        break;
                                     case 'w':
                                        bootloader_operations.Add(en_bootloader_operations.FW_WRITE);
                                        break;

                                     case 'v':
                                        bootloader_operations.Add(en_bootloader_operations.FW_VERIFY);
                                        break;

                                     case 'r':
                                        bootloader_operations.Add(en_bootloader_operations.FW_READ);
                                        break;

                                     default:
                                        throw_error(string.Format("Invalid option '{0}' specified for '{1}'", c, split[0]));
                                        return false;
                                 }
                             }
                             fop_found_ = true;
                             break;

                         case "-eop":
                             if (eop_found_) { throw_error_duplicate_param(split[0]); return false; }

                             foreach (char c in split[1].ToLower())
                             {
                                 switch(c) {
                                     case 'e':
                                        bootloader_operations.Add(en_bootloader_operations.EEP_ERASE);
                                        break;
                                     case 'w':
                                        bootloader_operations.Add(en_bootloader_operations.EEP_WRITE);
                                        break;

                                     case 'v':
                                        bootloader_operations.Add(en_bootloader_operations.EEP_VERIFY);
                                        break;

                                     case 'r':
                                        bootloader_operations.Add(en_bootloader_operations.EEP_READ);
                                        break;

                                     default:
                                        throw_error(string.Format("Invalid option '{0}' specified for '{1}'", c, split[0]));
                                        return false;
                                 }
                             }
                             eop_found_ = true;
                             break;

                         case "-xop":
                             if (xop_found_) { throw_error_duplicate_param(split[0]); return false; }

                             foreach (char c in split[1].ToLower())
                             {
                                 switch(c) {
                                     case 't':
                                        bootloader_operations.Add(en_bootloader_operations.TIMEOUT_CHANGE  );
                                        break;
                                     case 'p':
                                        bootloader_operations.Add(en_bootloader_operations.PASSWORD_CHANGE);
                                        break;
                                    case 'm':
                                        bootloader_operations.Add(en_bootloader_operations.WRITE_MAGIC_BYTES);
                                        break;


                                    default:
                                        throw_error(string.Format("Invalid option '{0}' specified for '{1}'", c, split[0]));
                                        return false;
                                 }
                             }
                             xop_found_ = true;
                             break;

                         case "-pwd":
                             if (pwd_found_) { throw_error_duplicate_param(split[0]); return false; }
                             string[] pwd_split = split[1].Split(',');

                             foreach(string pwd in pwd_split) {

                                 if (bootloader_passwords.Contains(pwd)) {
                                     throw_error(string.Format("Password '{0}' is specified at least twice. Please specify each password only once.", pwd));
                                     return false;
                                 }
                                 bootloader_passwords.Add(pwd);

                             }
                             pwd_found_ = true;
                             break;

                         case "-ffile":
                             if (ffile_found_) { throw_error_duplicate_param(split[0]); return false; }
                             flash_file_name = split[1];
                             ffile_found_ = true;
                             break;

                         case "-efile":
                             if (efile_found_) { throw_error_duplicate_param(split[0]); return false; }
                             eeprom_file_name = split[1];
                             efile_found_ = true;
                             break;

                         case "-seederos":
                             if (seederos_found_)  { throw_error_duplicate_param(split[0]); return false; }

                             if (split[1].ToLower().Equals("bron") || split[1].ToLower().Equals("bren")) /* we'll keep 'bren' for legacy purposes but hanging for 'bron' for future */
                             {
                                 bootloader_operations.Add(en_bootloader_operations.SEEDEROS_BRIDGEENABLE);
                             }
                             else if (split[1].ToLower().Equals("broff"))
                             {
                                bootloader_operations.Add(en_bootloader_operations.SEEDEROS_BRIDGEDISABLE);
                             }
                            else
                             {
                                 throw_error(string.Format("Invalid option '{0}' specified for '{1}'", split[1], split[0]));
                                 return false;
                             }
                             seederos_found_ = true;
                             break;

                         default:
                             throw_error_invalid_param(split[0]);
                             return false;
                     }
                 }
             }

            if (!port_found_)
            {
                throw_error(string.Format("Missing command line parameter '-port'{0}The serial port name must be specified for all operations.", Environment.NewLine));
                return false;
            }

            if (xxx_found_ && bootloader_operations.Count() > 1) 
             {
                 /* xxx was specified but other operations were specified too.
                  * This is an incorrect behaviour. XXX must be used alone
                  */
                 throw_error("The '-XXX' command option can only be used alone. It can't be combined with -fop, -eop, -xop or -seederos.");
                 return false;
             }

             if (seederos_found_ && bootloader_operations.Count() > 1)
             {
                 /* xxx was specified but other operations were specified too.
                  * This is an incorrect behaviour. XXX must be used alone
                  */
                 throw_error("The '-seederos' command option can only be used alone. It can't be combined with -fop, -eop, -xop or -XXX.");
                 return false;
             }


             return true;

        }

         private void throw_error(string error)
         {
             Console.WriteLine();
             Console.WriteLine(error);
             Console.WriteLine("");
             Console.WriteLine("For usage information run the program with the '-?' option.");
         }

         private void throw_error_duplicate_param(string param_name)
         {
             throw_error(string.Format("Invalid syntax: Parameter '{0}' is specified more than once.", param_name));
         }

         private void throw_error_invalid_param(string param)
         {
             throw_error(string.Format("Invalid parameter specified: {0}", param));
         }

         private void display_usage()
         {
             var version = Assembly.GetExecutingAssembly().GetName().Version;
             /*This trick required setting the last 2 parts of the version to '*'
              * However that is not supported by MONO, so it won't work.
              * DateTime buildDate = new DateTime(2000, 1, 1)
                .AddDays(version.Build)
                .AddSeconds(version.Revision*2);*/

             Console.WriteLine("");
            Console.WriteLine("=======================================================================");
            Console.WriteLine(" Tinysafeboot Advanced Loader");
            Console.WriteLine(" Version " + version.ToString());
            Console.WriteLine("=======================================================================");

            Console.WriteLine("\n-port=    [port name]");
            Console.WriteLine("-baud=    [baud rate bps. Default is 9600]");
            Console.WriteLine("-acmode=  [bootloader activation mode: 'cold' (default), 'live']");
            Console.WriteLine("          'live' is currently only supported on Seed Robotics devices");
            Console.WriteLine("-prewait= [millisecs to wait after opening the port and before sending");
            Console.WriteLine("          the activation char. Default is {0}]", default_prewait_ms);
            Console.WriteLine("-replytimeout=[millisecs to wait for reply after sending the");
            Console.WriteLine("              bootloader activation sequence. Default is {0}]", default_replytimeout_ms);
            Console.WriteLine("-pwd=      [specify if acessing password protected devices, in 'cold' mode");
            Console.WriteLine("           specify the password (if only one device) or a list of");
            Console.WriteLine("           comma-separated passwords if acessing several devices.]");
            Console.WriteLine("-dynid=    [specify if acessing a device in 'live' mode]");
            Console.WriteLine("-i         Displays device and bootloader information.");
            Console.WriteLine("-fop=      [flash operation(s) to perform. Multiple options can be specified.");
            Console.WriteLine("           e=erase, w=write, v=verify, r=read.");
            Console.WriteLine("           Operations are performed in the order they are written]");
            Console.WriteLine("-eop=      [eeprom operation(s) to perform. Multiple options can be specified.");
            Console.WriteLine("           e=erase, w=write, v=verify, r=read.");
            Console.WriteLine("           Operations are performed in the order they are written]");
            Console.WriteLine("-xop=      [extended options. Multiple options can be specified.");
            Console.WriteLine("             t=set bootloader timeout,");
            Console.WriteLine("             p=set bootloader password,");
            Console.WriteLine("             m=set magic bytes]");
            Console.WriteLine("-ffile=    [file name for flash data. Used by flash related options]");
            Console.WriteLine("-efile=    [file name for eeprom data. Used by EEProm related options]");
            Console.WriteLine("-tagefile  [append the current date, time and bootloader password");
            Console.WriteLine("           to the EEProm -efile name.]");
            Console.WriteLine("-tagffile  [append the current date, time and bootloader password");
            Console.WriteLine("           to the Flash -ffile name.]");
            Console.WriteLine("-XXX       [performs an emergency erase. If XXX is specified, no");
            Console.WriteLine("           -fop, -eop, -xop options are allowed]");
            Console.WriteLine("-patchdaisychain [patches the UserData page in order to cope with");
            Console.WriteLine("                  a known issue operating in one-wire daisy chain");
            Console.WriteLine("                  (TSB bootloader versions earlier than 2017)]");
            Console.WriteLine("-?, -h     Displays help screen (this screen)");
            Console.WriteLine("Commands specific to Seed Robotics products:");
            Console.WriteLine("           -seederos=bron  [enables  bridge mode on an EROS main board]");
            Console.WriteLine("           -seederos=broff [disabled bridge mode on an EROS main board]");
            Console.WriteLine("");
            Console.WriteLine("*** Apply RESET or POWER-UP on target device right before TSB session!  ***");
            Console.WriteLine(" Some boards may autoreset the devices on Serial connection (Arduino style) ");
            Console.WriteLine("");
            Console.WriteLine("Examples:");
            Console.WriteLine("'tsbloader_adv -port=COM2 -eop=rve -fop=ewv -efile=eeprom_backup.bin -ffile=firmware.hex'");
            Console.WriteLine("This command will perform the operations in the order specified:");
            Console.WriteLine("\t-EEProm Read and save to the -efile (eeprom_backup.bin)");
            Console.WriteLine("\t-EEProm verify against the -efile (eeprom_backup.bin)");
            Console.WriteLine("\t followed by an EEProm erase");
            Console.WriteLine("\tFlash Erase, followed by Write and Verify of the -ffile (firmware.hex)");
            Console.WriteLine("");
            Console.WriteLine("If an error occurs during any of these operations, the program terminates with");
            Console.WriteLine("an error and will not perform the next operations.");
            Console.WriteLine("");
            Console.WriteLine("");
            Console.WriteLine("This program was developed by Seed Robotics Ltd (http://seedrobotics.com) and");
            Console.WriteLine("expands on the excellent work of Julien Thomas (http://jtxp.org/).");
            Console.WriteLine("Source code is available from https://github.com/seedrobotics/tinysafeboot");
            Console.WriteLine("This program is Licensed under the GNU GPL V3 version.");
            Console.WriteLine("See <http://www.gnu.org/licenses/>");
         }
     }
}