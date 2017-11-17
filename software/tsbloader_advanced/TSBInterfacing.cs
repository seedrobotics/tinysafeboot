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
using System.Threading;
using System.IO.Ports;
using System.IO;

namespace Tsbloader_adv
{
    class TSBInterfacing
    {
        public enum en_processor_type { ATTINY=0, ATMEGA=1 };
        public enum en_appjump_mode { RELATIVE_JUMP = 0, ABSOLUTE_JUMP=1 };

        public enum en_intelhex_validationresult
        {
            IHEX_VALID_LINE, IHEX_NO_START_CHAR, IHEX_INVALID_HEX_STRING, IHEX_INVALID_CHECKSUM,
            IHEX_INCOMPLETE_LINE, IHEX_LINE_TOO_LONG
        };

        public struct str_tsb_session_data {
            public int buildver;
            public byte status;
            public string device_signature;
            public string device_name;
            public int pagesize;
            public int appflash;
            public int flash_size;
            public int eeprom_size;
            public int appjump_address;
            public byte timeout;
            public string password;
            public en_processor_type processor_type;
            public en_appjump_mode jump_mode;
            public bool daisychain_patch_in_lastpage;
        }

        struct str_split_intelhex_line
        {
            public en_intelhex_validationresult validation_result;
            public uint first_address;
            public byte type_of_data;
            public byte size_of_data;
            public byte[] data;
        }
        public const int max_pwd_length = 16; /* maximum bytes for password to allow space for 0s to overcome daisy chain bug */


        const byte bootloader_activation_messagesize = 17;
        const byte CONFIRM_CHAR = (byte)'!';
        const byte REQUEST_CHAR = (byte)'?';
        private const int command_reply_timeout_ms = 1000;
        
        private bool bootloader_active_;
        private SerialPort serial_port_;

        private Dictionary<string, string> dic_device_names_;

        public str_tsb_session_data session_data_;         /* session specific data */

        private const int large_buff_size_ = 1048576; /* 1048576 = 1 MB */
        private byte[] large_buff_; 

        public TSBInterfacing() {
            serial_port_ = new SerialPort();

            serial_port_.Parity = Parity.None;
            serial_port_.DataBits = 8;
            serial_port_.StopBits = StopBits.One;
     
            dic_device_names_ = new Dictionary<string,string>();
            fill_devicenames(ref dic_device_names_);

            large_buff_ = new byte[large_buff_size_]; 
        }

        public bool ActivateBootloader(string port_name, int baud_bps, int pre_wait_ms, int reply_timeout_ms, string bootloader_pwd) {
            if (bootloader_active_ ) {
                if ( bootloader_pwd == session_data_.password)
                {
                    return true;
                }
                else
                {
                    Console.WriteLine("Attempting to Activate Bootloader but another Bootloader session is already active.{0}End the previous sessio before starting a new one. (in code, use function DeactivateBootloader())", Environment.NewLine);
                    return false;
                }
            }

            if (serial_port_.IsOpen) { 
                serial_port_.Close();
            }

            serial_port_.BaudRate = baud_bps;
            serial_port_.PortName = port_name;
            serial_port_.Encoding = Encoding.ASCII;

            try
            {
                serial_port_.Open();

                /* Apparently on .Net/Mono we need to manually set the DTR enable flag,
                 * which is commonly asserted whenever a device connects to a terminal.
                 * The internal Reset of the Seed Eros boards is dependent on this behaviour
                 * of DTR so, for the sake of honouring the "typical" behaviour
                 * we will assert it on connect and de-assert it on disconnect
                 */
                serial_port_.DtrEnable = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(string.Format("Error opening Serial port '{0}':", serial_port_.PortName));
                Console.WriteLine(ex.Message);
                return false;
            }

            Thread.Sleep(pre_wait_ms);

            /* print and discard any data that may have appeared in the meanime. This data is not relevant for the bootloader
             * activation process. 
             */
            if (serial_port_.BytesToRead > 0)
            {
                Console.WriteLine("< " + serial_port_.ReadExisting().ToString().Replace("\n","\n< "));
                Console.WriteLine();
            }


            /* Send bootloader activation chars
             */
            serial_port_.Write(string.Format("@@@{0}", bootloader_pwd));

            while (true) {
                /* wait for reply or timeout */
                wait_for_reply(reply_timeout_ms, bootloader_activation_messagesize);

                if (serial_port_.BytesToRead == 0) /* if no reply */
                {
                    if (bootloader_pwd.Length > 0) /* check if we used a pwd already */
                    {
                        Console.WriteLine(string.Format("No reply received from the bootloader.{0}Check if the password is correct, if the device has power and if it has been reset before initiating this command (some may boards auto reset the device).", Environment.NewLine));
                        return false;

                    } else {
                        /* attempt asking for a password */
                        Console.WriteLine("No reply from bootloader. Would you like to enter a bootloader password?");
                        Console.WriteLine("(press Ctrl+C to cancel or enter password and press enter)");
                        Console.Write("Password: ");
                        bootloader_pwd = Console.ReadLine();

                        if (bootloader_pwd.Length == 0) {
                            Console.WriteLine();
                            Console.WriteLine("No password entered. Ending program.");
                            return false;
                        }

                        /* send the password and wait for reply again */
                        serial_port_.Write(bootloader_pwd);

                        /* the While Cycle will run again */
                    }
                } else {
                    /* we have a reply; break the while cycle */
                    break;
                }
            }

            /* pull reply in */
            byte[] reply_buffer = new byte[256];

            serial_port_.Read(reply_buffer, 0, serial_port_.BytesToRead);

            if (reply_buffer[16] != CONFIRM_CHAR) {
                Console.WriteLine();
                Console.WriteLine(string.Format("ERROR: Invalid reply from Bootloader. Confirmation character is invalid ('{0}')", reply_buffer[16]));
                Console.WriteLine(string.Format("Full reply was: '{0}'", System.Text.Encoding.ASCII.GetString(reply_buffer)));
                return false;
            }
            else if (System.Text.Encoding.ASCII.GetString(reply_buffer, 0, 3) != "TSB") {
                Console.WriteLine();
                Console.WriteLine(string.Format("ERROR: Invalid Reply Header ('{0}')", System.Text.Encoding.ASCII.GetString(reply_buffer, 0, 3) ));
                Console.WriteLine(string.Format("Full reply was: '{0}'", System.Text.Encoding.ASCII.GetString(reply_buffer)));
                return false;
            }
            else
            {
                //Console.WriteLine("Bootloader responded.");
            }

 
            /* NOTE: While AVR/Assembler is accounting flash addresses space in WORDS
                * (except from EEPROM locations), at PC side we are using BYTES 
                */
            session_data_.buildver = (int)reply_buffer[3] + (int) reply_buffer[4] * 256;
            session_data_.status = reply_buffer[5];
            session_data_.device_signature = string.Format("{0:X2}{1:X2}{2:X2}", reply_buffer[6], reply_buffer[7], reply_buffer[8]);
            session_data_.pagesize = (int)reply_buffer[9] * 2; // WORDS * 2 = BYTES
            session_data_.appflash = ((int) reply_buffer[10] + ((int)reply_buffer[11]) * 256) * 2; // WORDS * 2 = BYTES
            session_data_.flash_size = (session_data_.appflash / 1024 + 1) * 1024;
            session_data_.eeprom_size = ((int) reply_buffer[12] + ((int)reply_buffer[13]) * 256) + 1;

            if (dic_device_names_.ContainsKey(session_data_.device_signature))
            {
                session_data_.device_name = dic_device_names_[session_data_.device_signature];
            }
            else
            {
                session_data_.device_name = "Unknown";
            }

            if (session_data_.pagesize % 16 > 0) {
                /* invalid page size; something is wrong */
                Console.WriteLine();
                Console.WriteLine(string.Format("ERROR: Bootloader replied with an invalid pagesize ('{0}'). Pagesize is not multiple of 16.", session_data_.pagesize));
                return false;
            }

            /* check if it's the TSB firmware with new date identifier and status byte */
            if (session_data_.buildver < 32768) {
                session_data_.buildver = ( session_data_.buildver & 31 ) + ((session_data_.buildver & 480) / 32) * 100 + 
                                            ((session_data_.buildver & 65024 ) / 512) * 10000 + 20000000;

            } else { /* old date encoding in tsb-fw (three bytes) */
                session_data_.buildver = session_data_.buildver + 65536 + 20000000;
            }

            /* detect device type and appjump mode */
            switch(reply_buffer[15]) {
                case 0x00:
                    session_data_.jump_mode = en_appjump_mode.RELATIVE_JUMP;
                    session_data_.processor_type = en_processor_type.ATTINY;
                    break;

                case 0x0c:
                    session_data_.jump_mode = en_appjump_mode.ABSOLUTE_JUMP;
                    session_data_.processor_type = en_processor_type.ATTINY;
                    break;

                case 0xaa:
                    session_data_.jump_mode = en_appjump_mode.RELATIVE_JUMP;
                    session_data_.processor_type = en_processor_type.ATMEGA;
                    break;

                default:
                    Console.WriteLine(string.Format("ERROR: Unknown processor code received ('{0}'). Can't determine APPJump and processor type.",  reply_buffer[15]));
                    return false;
            }

            bootloader_active_ = true;
            /* request the last page, where we have the remaining information stored, including
                * appjump address, timeout and password */
            if (!LastPage_Read())
            {
                DeactivateBootloader();
            }
  
            return bootloader_active_;
        }

        public void DeactivateBootloader()
        {
            if (serial_port_.IsOpen) {
                serial_port_.Write("q");
                serial_port_.DtrEnable = false;
                serial_port_.Close();
            }

            bootloader_active_ = false;
        }


        /******************************
         * LAST PAGE functions 
         *******************************/

        public bool LastPage_Read()
        {
            byte[] reply_buff = new byte[256];

            Console.Write("Reading boootloader configuration data...");
            if (!bootloader_active()) return false;

            /* repeat reading the last page AT LEAST 2 times. The second time, if the daisy chain patch
             * is applied, will be the correct one, with the right information.
             * this happens because, if we are on a daisy chain, the first read will be
             * poluted by the other devices speaking on the bus */
            byte b; bool tsb_replied = false;
            for (b = 0; b < 2; b++)
            {
                serial_port_.Write("c");

                /* pull page from device */
                if (!read_page_from_device(ref reply_buff))
                {
                    return false;
                }

                tsb_replied = return_to_tsbmainparser();
            }

            /* get the data from the byte array */
            session_data_.appjump_address = (int)reply_buff[0] + ((int)reply_buff[1] * 256);
            if (session_data_.processor_type == en_processor_type.ATMEGA) session_data_.appjump_address = 0x0;

            session_data_.timeout = reply_buff[2];

            session_data_.password = "";
            for (b = 3; b <= session_data_.pagesize; b++)
            {
                if (reply_buff[b] == 0xFF)
                {
                    break;
                }
                else
                {
                    session_data_.password = session_data_.password + (char)reply_buff[b];
                }
            }

            session_data_.daisychain_patch_in_lastpage = true;
            for (++b; b <= session_data_.pagesize; b++)
            {
                if (reply_buff[b] != 0)
                {
                    session_data_.daisychain_patch_in_lastpage = false;
                }
            }

            Console.WriteLine("Done");
            return tsb_replied;
        }

        public bool LastPage_Write()
        {
            byte[] page_buff = new byte[256];

            Console.Write("Writing bootloader configuration data...");
            if (!bootloader_active()) return false;

            if (session_data_.password.Length >= max_pwd_length)
            {
                Console.Write("ERROR");
                Console.WriteLine("Password is too long. Maximum pasword length that can be accepted in this tool is {0}", max_pwd_length);
                Console.WriteLine("If you need to set a longer password, try using the original TSB Loader written in FreeBasic");
                return false;
            }

            /***** Prepare the page to write */
            page_buff[0] = (byte) (session_data_.appjump_address % 256);
            page_buff[1] = (byte) (session_data_.appjump_address / 256);

            page_buff[2] = session_data_.timeout;

            byte[] pwd_array = Encoding.ASCII.GetBytes(session_data_.password);
            pwd_array.CopyTo(page_buff, 3);

            byte next_wr_ix = (byte) (pwd_array.Length + 3);

            if (next_wr_ix >= session_data_.pagesize - 7)
            {
                Console.WriteLine("ERROR");
                Console.WriteLine("Password is too long for the pagesize in this device. Maximum password length in this device is {0} characters, due to the device pagesize.", session_data_.pagesize - 7);
                return false;
            } else {
                page_buff[next_wr_ix++] = 0xFF; /* password termination character */

                /* Make the rest of the page zeros.
                 * In old versions, in a daisy chain, when a wrong password is given it goes to an infinite loop
                 * but the loop can be broken by sending a 0 (code for exmegency erase) which then sends waits for the
                 * Conformation char; if no confirmation, the other devices boot.
                 * We want to explore this by making a sequence of 3 zeros pass on the bus, thus forcing the bug to occur
                 * and making the other devices boot; hopefully they won't disturb comms again and we can operate normally
                 */
                while (next_wr_ix < session_data_.pagesize)
                {
                    page_buff[next_wr_ix++] = 0x00;
                }
            }

            /* for Last page write, the device replies by re sending the page that was just sent */

            /* in Daisy chain operation, if it's the first time we're writing the last page with the
             * patch, the other devices might interfere in the communication before quitting,
             * so we may have to write the LastPage twice to overcome this 
             */
            byte[] reply_buff = new byte[256];
            for (byte b = 0; b < 2; b++)
            {
                /**** Start the write procedure **/
                serial_port_.Write("C");

                if (!write_page_to_device(ref page_buff))
                {
                    return false;
                }
                                
                if (read_page_from_device(ref reply_buff) == false && b > 0)
                {
                    return false;
                }
                
                /* compare the data received */
                if (page_buff.SequenceEqual(reply_buff))
                {
                    break;

                } else if (b > 0) {
                    Console.Write("ERROR");
                    Console.Write("Verification of written data failed.");
                    return false;
                }

                return_to_tsbmainparser(); /* go back to main parser before repeating the loop */
            }

            Console.WriteLine("Done");
            return return_to_tsbmainparser();
        }


        /**************************************
         * EEPROM Functions
         *************************************/

        public bool EEProm_Read(string eep_filename)
        {
            Console.WriteLine();
            Console.Write("EEProm read... ");
            if (!bootloader_active()) return false;

            if (string.IsNullOrEmpty(eep_filename))
            {
                Console.WriteLine("ERROR");
                Console.WriteLine("No valid filename specified. Please specify the filename to save the EEPROM data.");
                return false;
            }

            int nr_bytes_returned = 0;
            serial_port_.Write("e");
            bool result = false;

            if (read_all_pages_to_largebuff(session_data_.eeprom_size, ref nr_bytes_returned) == true)
            {
                Console.WriteLine();
                Console.Write("EEProm read complete. Saving {0} bytes to file {1}... ", nr_bytes_returned, eep_filename);

                result = write_largebuffer_to_file(eep_filename, nr_bytes_returned);

                result = result & return_to_tsbmainparser();
            }

            return result;
        }


        public bool EEProm_Write(string eep_filename)
        {
            Console.WriteLine();
            Console.Write("EEProm write... ");
            if (!bootloader_active()) return false;

            if (string.IsNullOrEmpty(eep_filename))
            {
                Console.WriteLine("ERROR");
                Console.WriteLine("No valid filename specified. Please specify the filename to load the EEPROM data.");
                return false;
            }

            int nr_bytes_infile = 0;
            if (!read_file_to_largebuffer(eep_filename, ref nr_bytes_infile) == true)
            {
                return false;
            }
            else if (nr_bytes_infile > session_data_.eeprom_size)
            {
                Console.WriteLine("ERROR");
                Console.WriteLine("File is larger than EEPROM size. (file size={0}, eeprom size={1})", nr_bytes_infile, session_data_.eeprom_size);
                return false;
            }

            serial_port_.Write("E");

            if (!write_largebuffer_to_device(nr_bytes_infile))
            {
                return false;
            }

            Console.WriteLine("");
            Console.WriteLine("EEPROM write complete.");
            return return_to_tsbmainparser();
        }


        public bool EEProm_Verify(string eep_filename)
        {
            Console.WriteLine();
            Console.Write("EEProm verify... ");
            if (!bootloader_active()) return false;

            if (string.IsNullOrEmpty(eep_filename))
            {
                Console.WriteLine("ERROR");
                Console.WriteLine("No valid filename specified. Please specify the filename to load the EEPROM data.");
                return false;
            }

            int nr_bytes_infile = 0;
            if (!read_file_to_largebuffer(eep_filename, ref nr_bytes_infile) == true)
            {
                return false;
            }
            else if (nr_bytes_infile > session_data_.eeprom_size)
            {
                Console.WriteLine("ERROR");
                Console.WriteLine("File is larger than EEPROM size. (file size={0}, eeprom size={1})", nr_bytes_infile, session_data_.eeprom_size);
                return false;
            }

            /* read page by page a compare to our large buff */
            byte[] in_buff = new byte[256];
            int curr_addr = 0;

            serial_port_.Write("e");

            int con_col = Console.CursorLeft, con_row = Console.CursorTop;
            do
            {
                serial_port_.Write(((char)CONFIRM_CHAR).ToString()); /* confirm to pull page
                                                                      * the cofnirm signal is used on multi page reads as 
                                                                      * a way to sync the communication.
                                                                      */
                if (read_page_from_device(ref in_buff) == false)
                {
                    Console.WriteLine("ERROR");
                    Console.WriteLine("Error while trying to read a page from the device. Starting address 0x{0}", curr_addr);
                    return false;
                }
                else
                {
                    /* compare manually byte by byte */
                    for (byte b=0; b < session_data_.pagesize; b++) {
                        Console.SetCursorPosition(con_col, con_row);
                        Console.Write("0x{0:X} ({1}%)", curr_addr, (curr_addr * 100) / (nr_bytes_infile - 1));
                        
                        if (large_buff_[curr_addr] != in_buff[b]) {
                            Console.WriteLine(" ERROR");
                            Console.WriteLine("Verification error at position 0x{0:X}", curr_addr);
                            return false;
                        }

                        /* this is for cases where the loaded file size is not a multiple of page size;
                         * in that case, we should stop comparing when the file size contents ends
                         */
                        if (++curr_addr >= nr_bytes_infile) break;
                    }                    
                }
            } while (curr_addr < nr_bytes_infile);
            /* Cosmetic improvement: round the address displayed to the next page.
                This is because, if you do a sequence of Write->Verifiy, write will stop counting
                at an address multiple of pagezise whereas verify above, stopped at the actual end of the file.
                Bump the address up to the next multiple fo pagesize so that the Write and Verify addresses are the same
                Otherwise it might trick the user into thinking the file was not completely verified.
                The later message "x bytes verified" will display the actual number of bytes verified */
            int last_addr = nr_bytes_infile;
            if (last_addr % session_data_.pagesize != 0)
            {
                last_addr = (last_addr / session_data_.pagesize + 1) * session_data_.pagesize;
            }
            Console.SetCursorPosition(con_col, con_row);
            Console.WriteLine("0x{0:X} ({1}%)", last_addr, 100);

            Console.WriteLine("EEProm Verification sucessful. Verified all {0} bytes in file {1}", nr_bytes_infile, eep_filename);

            return return_to_tsbmainparser();
        }

        public bool EEProm_Erase()
        {
            /* the erase function activelly writes the EEPROM with
             * 0xFFs
             */

            Console.WriteLine();
            Console.Write("EEProm Erase... ");
            if (!bootloader_active()) return false;

            byte[] out_buff = Enumerable.Repeat((byte)0xFF, session_data_.eeprom_size).ToArray();
            Array.Copy(out_buff, 0, large_buff_, 0, session_data_.eeprom_size);

            serial_port_.Write("E");

            if (!write_largebuffer_to_device(session_data_.eeprom_size))
            {
                return false;
            }

            Console.WriteLine("");
            Console.WriteLine("EEPROM Erase complete.");
            return return_to_tsbmainparser();
        }

        /*********************************
         * Flash functions
         *********************************/

        public bool Flash_Read(string flash_filename)
        {
            Console.WriteLine();
            Console.Write("Flash read... ");
            if (!bootloader_active()) return false;

            if (string.IsNullOrEmpty(flash_filename))
            {
                Console.WriteLine("ERROR");
                Console.WriteLine("No valid filename specified. Please specify the filename to save the Flash data.");
                return false;
            }

            int nr_bytes_returned = 0;
            serial_port_.Write("f");
            bool result = false;

            if (read_all_pages_to_largebuff(session_data_.appflash, ref nr_bytes_returned) == true)
            {
                Console.WriteLine();
                Console.Write("Flash read complete. Processing read data...");

                /* remove empty/erased pages */
                int curr_ix = nr_bytes_returned - 1;
                while (curr_ix >= 0)
                {
                    if (large_buff_[curr_ix] == 0xFF)
                    {
                        nr_bytes_returned--;
                        curr_ix--;
                    } else {
                        break;
                    }
                }


                /* In ATTINYs the reset vector is also modified to point to TSB; we need to undo
                 * that modification, in order to have a original, correct flash file 
                 * (original reset vector calculated from from appjump in LASTPAGE) */
                if (nr_bytes_returned > 0)
                {
                    if (session_data_.processor_type == en_processor_type.ATTINY)
                    {
                        if (session_data_.jump_mode == en_appjump_mode.RELATIVE_JUMP)
                        {
                            int i;
                            if (session_data_.appjump_address - 0xC000 > (4096 - session_data_.flash_size / 2))
                            {
                                // newer TSB with backward rjmp
                                i = session_data_.appjump_address - session_data_.pagesize / 2 - (4096 - session_data_.flash_size / 2);
                            } else {
                                // older TSB with forward rjmp
                                i = session_data_.appjump_address - session_data_.pagesize / 2;
                            }
                            large_buff_[1] = (byte)(i / 256);
                            large_buff_[0] = (byte)(i - large_buff_[1] * 256);
                        }                        
                    }

                    Console.WriteLine("Done");
                    Console.Write("Saving {0} bytes to file {1}... ", nr_bytes_returned, flash_filename);

                    result = write_largebuffer_to_file(flash_filename, nr_bytes_returned);
                    result = result & return_to_tsbmainparser();
                    return result;
                }
                else
                {
                    Console.WriteLine("Done");
                    Console.Write("Flash is empty. Nothing to save to file.");
                    return return_to_tsbmainparser();
                }
  
            }
            else
            { /* flash read failed while reading from device */
                return false;
            }
        }

        public bool Flash_Write(string flash_filename)
        {
            Console.WriteLine();
            Console.Write("Flash write... ");
            if (!bootloader_active()) return false;

            if (string.IsNullOrEmpty(flash_filename))
            {
                Console.WriteLine("ERROR");
                Console.WriteLine("No valid filename specified. Please specify the filename to load the Flash data.");
                return false;
            }

            int nr_bytes_infile = 0;
            if (!read_file_to_largebuffer(flash_filename, ref nr_bytes_infile) == true)
            {
                return false;
            }
            else if (nr_bytes_infile > session_data_.appflash)
            {
                Console.WriteLine("ERROR");
                Console.WriteLine("File is larger than Flash size. (file size={0}, flash size={1})", nr_bytes_infile, session_data_.appflash);
                return false;
            }

            if (SPM_instructions_present_in_largebuffer(nr_bytes_infile) && 
                session_data_.processor_type == en_processor_type.ATTINY)
            {
                Console.WriteLine("WARNING: The firmware you are about to upload");
                Console.WriteLine("contains the SPM opcode that performs direct flash writes.");
                Console.WriteLine("If used incorrectly they may overwrite and damage the bootloader.");
                Console.WriteLine();
                
                string answer;
                while(true) {
                    Console.Write("Do you understand this and want to continue anyway? (y/n) ");
                    answer = Console.ReadLine().ToLower();

                    if (answer == "n") {
                        return false;
                    } else if (answer == "y") {
                        break;
                    }
                }               
            }

            serial_port_.Write("F");

            if (!write_largebuffer_to_device(nr_bytes_infile))
            {
                return false;
            }

            Console.WriteLine("");
            Console.WriteLine("Flash write complete.");
            return return_to_tsbmainparser();
        }

        public bool Flash_Verify(string flash_filename)
        {
            Console.WriteLine();
            Console.Write("Flash verify... ");
            if (!bootloader_active()) return false;

            if (string.IsNullOrEmpty(flash_filename))
            {
                Console.WriteLine("ERROR");
                Console.WriteLine("No valid filename specified. Please specify the filename to load the Flash data.");
                return false;
            }

            int nr_bytes_infile = 0;
            if (!read_file_to_largebuffer(flash_filename, ref nr_bytes_infile) == true)
            {
                return false;
            }
            else if (nr_bytes_infile > session_data_.appflash)
            {
                Console.WriteLine("ERROR");
                Console.WriteLine("File is larger than Flash size. (file size={0}, appflash size={1})", nr_bytes_infile, session_data_.appflash);
                return false;
            }

            /* read page by page a compare to our large buff */
            byte[] in_buff = new byte[256];
            int curr_addr = 0;
            bool first_page = true;

            serial_port_.Write("f");

            int con_col = Console.CursorLeft, con_row = Console.CursorTop;
            do
            {
                serial_port_.Write(((char)CONFIRM_CHAR).ToString()); /* confirm to pull page
                                                                      * the cofnirm signal is used on multi page reads as 
                                                                      * a way to sync the communication.
                                                                      */
                if (read_page_from_device(ref in_buff) == false)
                {
                    Console.WriteLine("ERROR");
                    Console.WriteLine("Error while trying to read a page from the device. Starting address 0x{0}", curr_addr);
                    return false;
                }
                else
                {
                    /* compare manually byte by byte */
                    byte b;
                    if (first_page && session_data_.processor_type == en_processor_type.ATTINY)
                    {
                        /* on ATTINY ignore the first 4 bytes, which are modified for APPJump */
                        b = 4; first_page = false;
                    }
                    else
                    {
                        b = 0;
                    }

                    for (; b < session_data_.pagesize; b++)
                    {
                        Console.SetCursorPosition(con_col, con_row);
                        Console.Write("0x{0:X} ({1}%)", curr_addr, (curr_addr * 100) / (nr_bytes_infile - 1));

                        if (large_buff_[curr_addr] != in_buff[b])
                        {
                            Console.WriteLine(" ERROR");
                            Console.WriteLine("Verification error at position 0x{0:X}", curr_addr);
                            return false;
                        }

                        /* this is for cases where the loaded file size is not a multiple of page size;
                         * in that case, we should stop comparing when the file size contents ends
                         */
                        if (++curr_addr >= nr_bytes_infile) break;
                    }
                }
            } while (curr_addr < nr_bytes_infile);
            /* Cosmetic improvement: round the address displayed to the next page.
               This is because, if you do a sequence of Write->Verifiy, write will stop counting
               at an address multiple of pagezise whereas verify above, stopped at the actual end of the file.
               Bump the address up to the next multiple fo pagesize so that the Write and Verify addresses are the same
               Otherwise it might trick the user into thinking the file was not completely verified.
               The later message "x bytes verified" will display the actual number of bytes verified */
            int last_addr = nr_bytes_infile;            
            if (last_addr % session_data_.pagesize != 0)
            {
                last_addr = (last_addr / session_data_.pagesize + 1) * session_data_.pagesize;
            }
            Console.SetCursorPosition(con_col, con_row);
            Console.WriteLine("0x{0:X} ({1}%)", last_addr, 100);

            Console.WriteLine("Flash Verification sucessful. Verified all {0} bytes in file {1}", nr_bytes_infile, flash_filename);

            return return_to_tsbmainparser();
        }


        public bool Flash_Erase()
        {
            /* Flash Erase works differently from EEProm erase.
             * Because we want to leave the TSB bootloader intact in ATTiny devices, we CAN'T do a
             * full-on brute force Write of 0xFFs as that would overwrite/delete the bootloader on an ATTINY
             * Instead, for flash erase, TSB has a built in functionality that ensures the safety of
             * the bootloader:
             * Send the Flash Write command and once the bootloader sesion starts it will first do a flash erase
             * top to bottom (this is the default behaviour built into the bootloader).
             * Next, when it sends the REQUEST char to initiate sending a page, reply with something other than
             * CONFIRM; this will cause the bootloader to abort/end the Flash write routine. By now, the bootloader will have
             * erased the flash, so we have got the result we wanted.
             */

            Console.WriteLine();
            Console.Write("Flash Erase... ");
            if (!bootloader_active()) return false;

            serial_port_.Write("F");

            wait_for_reply(command_reply_timeout_ms * 2, 1); /* wait for the REQUEST char; allow for some extra timeout room since it will
                                                              * do a flash erase top to bottom right after receiving the "F" command */

            if (serial_port_.BytesToRead != 1)
            {
                Console.WriteLine("ERROR");
                Console.WriteLine("No Request signal from bootloader after sending the 'F' char.");
                return false;
            }
            else
            {
                byte conf = (byte) serial_port_.ReadByte(); /* read the REQUEST byte g */
                if (conf != REQUEST_CHAR)
                {
                    Console.WriteLine("ERROR");
                    Console.WriteLine("Protocol out of Sync: did not receive the expected REQUEST char. Instead got '{0}'", ((char)conf).ToString());
                    return false;
                }

                serial_port_.Write(((char)REQUEST_CHAR).ToString()); /* when we invoike the "F" command the bootloader will already do a flash erase
                                                                      * and move to waiting for new data.
                                                                      * If we don't send the expected CONFIRM command to send a page, it will simply
                                                                      * exit the routine and return to main parser */

                if (!return_to_tsbmainparser())
                {
                    Console.WriteLine("ERROR");
                    Console.WriteLine("Did not receive a Flash Erase confirmation from bootloader.");
                    return false;
                }
            }

            Console.WriteLine("Done");
            Console.WriteLine("Flash Erase complete.");
            return true;
        }

        public bool EmergencyErase(string port_name, int baud_bps, int pre_wait_ms, int reply_timeout_ms)
        {
            /* The process of issuing an emergency erase involves sending the initialization sequence @@@
             * and then send a \0.
             * The bootloader responds with REQUEST and we send a CONFIRM
             */

            Console.Write("Performing Emergency Erase... ");

            /* Because in an Emergency Erase can't Activate the Bootloader, we must go through the
               process of opening the port and sending the activating chars manually */
            if (bootloader_active_)
            {
                Console.WriteLine("ERROR");
                Console.WriteLine("Attempting to Initiate an Emergency Erase but another Bootloader session is already active.{0}End the previous sessio before starting a new one. (in code, use function DeactivateBootloader())", Environment.NewLine);
                return false;
            }

            if (serial_port_.IsOpen)
            {
                serial_port_.Close();
            }

            serial_port_.BaudRate = baud_bps;
            serial_port_.PortName = port_name;
            serial_port_.Encoding = Encoding.ASCII;

            try
            {
                serial_port_.Open();

                /* Apparently on .Net/Mono we need to manually set the DTR enable flag,
                 * which is commonly asserted whenever a device connects to a terminal.
                 * The internal Reset of the Seed Eros boards is dependent on this behaviour
                 * of DTR so, for the sake of honouring the "typical" behaviour
                 * we will assert it on connect and de-assert it on disconnect
                 */
                serial_port_.DtrEnable = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR");
                Console.WriteLine(string.Format("Error opening Serial port '{0}':", serial_port_.PortName));
                Console.WriteLine(ex.Message);
                return false;
            }

            Thread.Sleep(pre_wait_ms);

            /* print and discard any data that may have appeared in the meanime. This data is not relevant for the bootloader
             * activation process. 
             */
            if (serial_port_.BytesToRead > 0)
            {
                Console.WriteLine("< " + serial_port_.ReadExisting().ToString().Replace("\n", "\n< "));
                Console.WriteLine();
            }


            /* Send bootloader activation chars
             *  (no password)
             */
            serial_port_.Write("@@@");

            /* wait for reply or timeout */
            wait_for_reply(reply_timeout_ms, bootloader_activation_messagesize);

            if (serial_port_.BytesToRead != 0) /* if no reply */
            {
                Console.WriteLine("HALT");
                Console.WriteLine();
                Console.WriteLine("It seems the Bootloader is still accessible without password.");
                Console.WriteLine("No Emergency Erase will be performed. If you wish to erase flash or eeprom");
                Console.WriteLine("memories on a device without password, use '-fop=e' and '-eop=e' instead.");
                return false;
            }


            /* now the sequence is the following:
                   - Send 0x0
                   - Receive REQ char
                   - Send CONF
                   - Receive REQ char second time
                   - Send CONF a third time.
             */
            for (byte b=0; b <=1; b++) { 
                if (b==0)
                {
                    /* Send a 0x0 to indicate Emergency erase */
                    serial_port_.Write(((char)'\0').ToString());
                } else
                {
                    serial_port_.Write(((char)CONFIRM_CHAR).ToString());
                }

                wait_for_reply(command_reply_timeout_ms, 1);
                if (serial_port_.BytesToRead != 1)
                {
                    Console.WriteLine("ERROR");

                    if (b == 0)
                    {
                        Console.WriteLine("No reply from bootloader. (Is the device connected? Did you power cycle the device?)");
                    }
                    else {
                        Console.WriteLine("No Request signal from bootloader after sending CONFIRMATION of Erase.");
                    }
                    return false;
                }
                else
                {
                    byte conf = (byte)serial_port_.ReadByte(); /* read the REQUEST byte g */
                    if (conf != REQUEST_CHAR)
                    {
                        Console.WriteLine("ERROR");
                        Console.WriteLine("Protocol out of Sync: did not receive the expected REQUEST char in the {1} cycle. Instead got '{0}'", ((char)conf).ToString(), b+1);
                        return false;
                    }
                }
            }

            serial_port_.Write(((char)CONFIRM_CHAR).ToString()); /* send the final confirmation */

            /* do some animation to show some progress */
            int con_col = Console.CursorLeft, con_row = Console.CursorTop;

            string progress_signals = "|/-\\|/-\\|/-\\";

            for (byte b=0; b<=10; b++)
            {
                Console.SetCursorPosition(con_col, con_row);
                Console.Write("{0}", progress_signals.Substring(b, 1));
                wait_for_reply(command_reply_timeout_ms, 1); /* wait for the CONFIRMATION char; to inform the Emergency Erase is done
                                                                wait longer than usual as mutiple memories must be erased */
            }
            Console.SetCursorPosition(con_col, con_row);

            if (serial_port_.BytesToRead != 1)
            {
                Console.WriteLine("ERROR");
                Console.WriteLine("The bootloader has not sent confirmation indicating Emergency Erase is complete.");
                return false;
            }
            else
            {
                byte conf = (byte)serial_port_.ReadByte(); /* read the REQUEST byte g */
                if (conf != CONFIRM_CHAR)
                {
                    Console.WriteLine("ERROR");
                    Console.WriteLine("Protocol out of Sync: Did not received the Confirmation character indicating Emergency Erase is complete. Instead got '{0}'", ((char)conf).ToString());
                    return false;
                }
            }

            Console.WriteLine("Done");
            Console.WriteLine("Emergency Erase complete.");
            return true;
        }


        /*************************************
         * Auxiliary functions
         *************************************/

        private void wait_for_reply(long timeout_millisecs, int min_num_chars)
        {
            long millis = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
            long end_millis = millis + timeout_millisecs;
            do
            {
                millis = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
            } while (serial_port_.BytesToRead < min_num_chars && millis < end_millis);
        }
        
        private bool read_page_from_device(ref byte[] buff)
        {
            /* we can't be sending the CONFIRM signal to pull page here because
             * the ReadLast page doesn't use it; we just send a "c" and pull the whole page
             * The CONFIRM signal is only used in multipage reads; so it should be sent
             * only from the routine that does the multipage reads */
            wait_for_reply(command_reply_timeout_ms, session_data_.pagesize);

            if (serial_port_.BytesToRead < session_data_.pagesize)
            {
                Console.WriteLine("ERROR");
                Console.WriteLine("Received an incomplete page ({0} bytes)", serial_port_.BytesToRead);
                return false;
            }
            else
            {
                try
                {
                    serial_port_.Read(buff, 0, session_data_.pagesize);
                    return true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("PROGRAM ERROR");
                    Console.WriteLine("While pulling Data page from device: " + ex.Message);
                    return false;
                }
            }
        }

        private bool read_all_pages_to_largebuff(int nr_bytes_to_read, ref int nr_bytes_returned)
        {
            /* Reads all pages to the large_buff */
            byte[] in_buff = new byte[256];
            int large_buff_ix = 0;

            int con_col = Console.CursorLeft, con_row = Console.CursorTop;
            do
            {
                serial_port_.Write(((char)CONFIRM_CHAR).ToString()); /* confirm to pull page
                                                                      * the cofnirm signal is used on multi page reads as 
                                                                      * a way to sync the communication.
                                                                      */
                if (read_page_from_device(ref in_buff) == false)
                {
                    return false;
                }
                else
                {
                    Array.Copy(in_buff, 0, large_buff_, large_buff_ix, session_data_.pagesize); // in_buff.CopyTo(large_buff_, large_buff_ix);
                    large_buff_ix += session_data_.pagesize;

                    Console.SetCursorPosition(con_col, con_row);
                    Console.Write("0x{0:X} ({1}%)", large_buff_ix - 1, (large_buff_ix * 100) / nr_bytes_to_read);
                }
            } while (large_buff_ix < nr_bytes_to_read);

            nr_bytes_returned = large_buff_ix; /* don't add +1 here! large_buff_ix always points to the next address to write
                                                * the variable is zeor based, but bc it walways pointing at the next position
                                                * to write, the value it holds is actually the count so far, because that value
                                                * is a pointer to a position not yet written */

            return (nr_bytes_returned == nr_bytes_to_read);
        }

       

        private bool write_page_to_device(ref byte[] buff)
        {
            /* wait for the REQUEST char from the device to sngal it's ready to receive data */
            wait_for_reply(command_reply_timeout_ms, 1);

            if (serial_port_.BytesToRead != 1)
            {
                Console.WriteLine("ERROR");
                Console.WriteLine("No request from bootloader to initiate page writing.");
                return false;
            }

            byte c = (byte) serial_port_.ReadByte();

            if (c == REQUEST_CHAR)
            {
                serial_port_.Write(((char)CONFIRM_CHAR).ToString()); /* confirm to pull page
                                                                    * the cofnirm signal is used on multi page reads as 
                                                                    * a way to sync the communication.
                                                                    */
                serial_port_.Write(buff, 0, session_data_.pagesize);
                return true;
            }
            else
            {
                Console.WriteLine("ERROR");
                Console.WriteLine("Bootloader request character is invalid ('{0}').", (char) c);
                return false;
            }
        }

        private bool write_largebuffer_to_device(int nr_bytes_to_write) {

            /* Writes all pages in the large_buff to the device */
            byte[] out_buff = new byte[256];
            int large_buff_ix = 0;

            int con_col = Console.CursorLeft, con_row = Console.CursorTop;
            do
            {
                /* pull page from large buffer to be written */
                Array.Copy(large_buff_, large_buff_ix, out_buff, 0, session_data_.pagesize);

                if (write_page_to_device(ref out_buff) == false)
                {
                    return false;
                }
                else
                {
                    large_buff_ix += session_data_.pagesize;

                    Console.SetCursorPosition(con_col, con_row);
                    Console.Write("0x{0:X} ({1}%)", large_buff_ix, (large_buff_ix * 100) / nr_bytes_to_write);
                }
            } while (large_buff_ix < nr_bytes_to_write);

            return true;
        }

        private bool return_to_tsbmainparser()
        {
            /* TSB sends a confirmation char when it returns to the main parser.
             * Some commands such as Flash read are self aware and return when they know they've sent
             * the full flash.
             * Other like EEProm read don't check the nr of chars read and need to be sent a character other
             * than confirm (for example REQUEST) to exit the routine and return to the main parser.
             * It's unclear why it is like this, but let's cope with it here
             */
            
            
            byte b = 2;
            while(b > 0) {

                /* Let's see if we already have the CONFIRM meaning we're back at the main parser */
                wait_for_reply(command_reply_timeout_ms, 1);

                if (serial_port_.BytesToRead >= 1)
                {
                    byte c = (byte) serial_port_.ReadByte();
                    if (c == CONFIRM_CHAR)
                    {
                        return true;
                    }
                    else if (c == REQUEST_CHAR && b > 1)
                    {
                       /* Certain operations such as EEPROM write are not aware of eeprom size and may send the
                        * REQUEST CHAR to get a new page; in this case, send a REQUEST as well (<> CONFIRM)
                        * to abort the page sending sequence
                        */
                        serial_port_.Write(((char)REQUEST_CHAR).ToString()); /* revert to main command parser */

                    }
                    else
                    {
                        return false;
                    }
                }
                else if (b > 1)
                {
                    /* if we don't yet have the confirm char, try sneding a wrong char to try force returning
                        * to the main parser and repeat the loop */
                    serial_port_.Write(((char)REQUEST_CHAR).ToString()); /* revert to main command parser */
                } 
                else
                {
                    return false;
                }
                b--;
            }

            return false;
        }


        private bool bootloader_active()
        {
            if (!bootloader_active_)
            {
                Console.WriteLine("ERROR");
                Console.WriteLine("Bootloader not active.");
                return false;
            }
            else
            {
                return true;
            }
        }



        /***********************************************
         * File manipulation functions
         **********************************************/

        private bool write_largebuffer_to_file(string filename, int nr_bytes)
        {
            /* check if the format is Intel HEX or plain binary */

            /* get file extension */
            int dot_pos = filename.LastIndexOf(".");
            string file_extension = "";
            if (dot_pos != 0)
            {
                file_extension = filename.Substring(dot_pos + 1).ToLower();
            }

            if (file_extension == "hex" || file_extension == "eep")
            {
                /* INTEL HEX */
                try
                {
                    StreamWriter sw = new StreamWriter(filename, false, System.Text.Encoding.ASCII);

                    sw.Write(":020000020000FC" + Environment.NewLine); /* header: for files <64Kb */

                    string line;
                    int i = 0, t, accum_checksum;
                    while (i < nr_bytes)
                    {
                        line = string.Format(":10{0:X4}00", i);

                        /* if the address it's larger than a byte, split it in bytes and add the
                         * bytes individually (as opposed to adding the whole number, which would produce
                         * a wrong checksum);
                         * Note: it needs to divide by 256 (0~255) and not 0xFF or 255; the range has actually 256 values (0~255). */
                        accum_checksum = 0x10;
                        for (t = i; t > 0xFF; t /= 256)
                        {
                            accum_checksum += t % 256;
                        }
                        accum_checksum += t;

                        /* add data bytes */
                        for (byte b = 0; b < 16; b++)
                        {
                            line = line + string.Format("{0:X2}", large_buff_[i]);

                            accum_checksum += large_buff_[i];
                            i++;
                        }

                        /* calculate the final checksum */
                        byte checksum_8bit = (byte)~accum_checksum;
                        checksum_8bit += 1; /* we need the 2's complement which is doen by negating and then adding 1;
                                             * ensure this is done in a byte variable so that it overflows to 0 in case you're doing FF+1 */

                        line = line + string.Format("{0:X2}", checksum_8bit) + Environment.NewLine;
                        sw.Write(line);
                    }

                    sw.Write(":00000001FF" + Environment.NewLine); /* EOF indicator */
                    sw.Flush();
                    sw.Close();

                    Console.WriteLine("Done");

                    return true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("FILE ERROR");
                    Console.WriteLine("While attempting to write to IHEX format file '{0}': {1}", filename, ex.Message);
                    return false;
                }
            }
            else
            {
                /* plain binary */
                try
                {
                    BinaryWriter bw = new BinaryWriter(File.Open(filename, System.IO.FileMode.Create));

                    bw.Write(large_buff_, 0, nr_bytes);
                    bw.Flush();
                    bw.Close();

                    Console.WriteLine("Done");

                    return true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("FILE ERROR");
                    Console.WriteLine("While attempting to write to BIN format file '{0}': {1}", filename, ex.Message);
                    return false;
                }
            }
        }


        private bool read_file_to_largebuffer(string filename, ref int bytes_count)
        {
            /* check if the format is Intel HEX or plain binary */

            /* get file extension */
            int dot_pos = filename.LastIndexOf(".");
            string file_extension = "";
            int numBytes;

            if (dot_pos != 0)
            {
                file_extension = filename.Substring(dot_pos + 1).ToLower();
            }

            if (file_extension == "hex" || file_extension == "eep")
            {
                /* INTEL HEX */
                try
                {
                    StreamReader sr = new StreamReader(filename, System.Text.Encoding.ASCII, false);
                    string line;
                    str_split_intelhex_line split_line = new str_split_intelhex_line();
                    split_line.data = new byte[256];


                    /* we will only look at the DATA type lines (00)
                     * We will ignore all other lines.
                     * While this is not really correct it's a simplification, bc we only really
                     * case about the DATA entries for our purpose. The headers don't mean anything
                     * to the bootloader.
                     * We will, however, check to make sure the address sequence is
                     * correct
                     */

                    int curr_addr = 0, curr_line = 1; /* line counting starts at 1 bc is it used to display user errors (users start counting from 1) */
                    while (!sr.EndOfStream)
                    {
                        line = sr.ReadLine().Trim();
                        if (line.Length == 0) continue;

                        if (decode_ihex_line(line, ref split_line))
                        {
                            if (split_line.type_of_data == 0x0) /* 0x0 = DATA section */
                            {
                                if (split_line.first_address != curr_addr)
                                {
                                    Console.WriteLine("ERROR");
                                    Console.WriteLine("DATA in IHEX file is not written in sequential memory addresses. Offending address at line {0}", curr_line);
                                    sr.Close();
                                    return false;
                                }
                            
                                Array.Copy(split_line.data, 0, large_buff_, curr_addr, split_line.size_of_data); //split_line.data.CopyTo(large_buff_, curr_addr);
                                curr_addr += split_line.size_of_data;
                            }
                            curr_line++;
                        }
                        else
                        {
                            print_ihex_error(split_line, curr_line);
                            sr.Close();
                            return false;
                        }
                    }
                    sr.Close();

                    numBytes = curr_addr;

                }
                catch (Exception ex)
                {
                    Console.WriteLine("FILE ERROR");
                    Console.WriteLine("While attempting to read IHEX format file '{0}': {1}", filename, ex.Message);
                    return false;
                }
            }
            else
            {
                /* plain binary */
                try
                {
                    BinaryReader br = new BinaryReader(File.Open(filename, System.IO.FileMode.Open));

                    numBytes = (int)new FileInfo(filename).Length;
                    byte[] read_bytes = br.ReadBytes((int)numBytes);
                    read_bytes.CopyTo(large_buff_, 0);

                    br.Close();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("FILE ERROR");
                    Console.WriteLine("While attempting to read BIN format file '{0}': {1}", filename, ex.Message);
                    return false;
                }
            }



            if (numBytes % session_data_.pagesize != 0)   /* if we did not receive a multiple of pagesize */
            {
                /* round the byte count up tot he nearest pagesize and fill it with 0xFF */
                /* this formula only works when numBytes % session_data_.pagesize != 0; otherwise it will give you
                 * a size with one extra page
                 */
                long adjusted_nr_bytes = (numBytes / session_data_.pagesize) * session_data_.pagesize + (session_data_.pagesize - numBytes % session_data_.pagesize);

                do
                {
                    large_buff_[numBytes] = 0xFF;
                    numBytes++;
                } while (numBytes < adjusted_nr_bytes);
            }

            bytes_count = (int)numBytes;

            return true;
        }

        private bool decode_ihex_line(string line, ref str_split_intelhex_line ihex_split_line)
        {
            /* decode the first line
            * Format is:
            * :
            * size of data (1 byte, 2 chars)
            * Address (2 bytes, 4 chars)
            * Type of data (1 byte; 00= data; 02=extended segment address)
            * Data (nr of bytes described by "size of data")
            * Checksum (1 byte, 2 chars)
            */

            int charat = 0;
            int checksum = 0;

            if (line.Length < 11)
            {
                ihex_split_line.validation_result = en_intelhex_validationresult.IHEX_INCOMPLETE_LINE;
                return false;
            }

            if (line.Substring(charat, 1) != ":")
            {
                ihex_split_line.validation_result = en_intelhex_validationresult.IHEX_NO_START_CHAR;
                return false;
            }
            charat++;

            /* size of data section */
            if (!byte.TryParse(line.Substring(charat, 2), System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.CurrentCulture, out ihex_split_line.size_of_data))
            {
                ihex_split_line.validation_result = en_intelhex_validationresult.IHEX_INVALID_HEX_STRING;
                return false;
            }
            else
            {
                checksum += ihex_split_line.size_of_data;
                charat += 2;
            }

            /* first address */
            if (!uint.TryParse(line.Substring(charat, 4), System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.CurrentCulture, out ihex_split_line.first_address))
            {
                ihex_split_line.validation_result = en_intelhex_validationresult.IHEX_INVALID_HEX_STRING;
                return false;
            }
            else
            {
                /* bc first address is composed of two bytes, we need to add the bytes individually */
                int value;
                for (value = (int)ihex_split_line.first_address; value > 0xFF; value /= 256)
                {
                    checksum += value % 256;
                }
                checksum += value;
                charat += 4;
            }

            /* type of data */
            if (!byte.TryParse(line.Substring(charat, 2), System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.CurrentCulture, out ihex_split_line.type_of_data))
            {
                ihex_split_line.validation_result = en_intelhex_validationresult.IHEX_INVALID_HEX_STRING;
                return false;
            }
            else
            {
                checksum += ihex_split_line.type_of_data;
                charat += 2;
            }

            /* now get data */

            /* check that the line is actually the correct length */
            if (line.Length < ihex_split_line.size_of_data * 2 + 11)
            {
                ihex_split_line.validation_result = en_intelhex_validationresult.IHEX_INCOMPLETE_LINE;
                return false;
            }
            else if (line.Length > ihex_split_line.size_of_data * 2 + 11)
            {
                ihex_split_line.validation_result = en_intelhex_validationresult.IHEX_LINE_TOO_LONG;
                return false;
            }

            for (int i = 0; i < ihex_split_line.size_of_data; i++)
            {
                if (!byte.TryParse(line.Substring(charat, 2), System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.CurrentCulture, out ihex_split_line.data[i]))
                {
                    ihex_split_line.validation_result = en_intelhex_validationresult.IHEX_INVALID_HEX_STRING;
                    return false;
                }
                else
                {
                    checksum += ihex_split_line.data[i];
                    charat += 2;
                }
            }

            /* calculate the final checksum */
            byte checksum_8bit = (byte)~checksum;
            checksum_8bit += 1; /* we need the 2's complement which is doen by negating and then adding 1;
                                    * ensure this is done in a byte variable so that it overflows to 0 in case you're doing FF+1 */

            byte checksum_from_file;
            if (!byte.TryParse(line.Substring(charat, 2), System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.CurrentCulture, out checksum_from_file))
            {
                ihex_split_line.validation_result = en_intelhex_validationresult.IHEX_INVALID_HEX_STRING;
                return false;
            }
            else
            {
                if ((byte)checksum_from_file != checksum_8bit)
                {
                    ihex_split_line.validation_result = en_intelhex_validationresult.IHEX_INVALID_CHECKSUM;
                    return false;
                }
            }

            return true;

        }

        private void print_ihex_error(str_split_intelhex_line split_line, int line_nr)
        {
            Console.WriteLine("IHEX PARSING ERROR");

            switch (split_line.validation_result)
            {
                case en_intelhex_validationresult.IHEX_INCOMPLETE_LINE:
                    Console.WriteLine("IHEX line is incomplete/too short (at line nr {0})", line_nr);
                    break;

                case en_intelhex_validationresult.IHEX_INVALID_CHECKSUM:
                    Console.WriteLine("Invalid Checksum at line nr {0}", line_nr);
                    break;

                case en_intelhex_validationresult.IHEX_INVALID_HEX_STRING:
                    Console.WriteLine("Invalid HEX value representation (at line nr {0})", line_nr);
                    break;

                case en_intelhex_validationresult.IHEX_LINE_TOO_LONG:
                    Console.WriteLine("IHEX line is too long (at line nr {0})", line_nr);
                    break;

                case en_intelhex_validationresult.IHEX_NO_START_CHAR:
                    Console.WriteLine("Invalid line in IHEX file: No start char. (at line nr {0})", line_nr);
                    break;

                default:
                    Console.WriteLine("Unspecified error parsing file (at line nr {0})", line_nr);
                    break;
            }
        }

        private bool SPM_instructions_present_in_largebuffer(int buff_size) {

            for(int i=0; i < buff_size - 1; i+=2) {
                if (large_buff_[i] == 0xE8 && large_buff_[i+1] == 0x95) {
                    return true;
                }
            }

            return false;
        }


        private static void fill_devicenames(ref Dictionary<string,string> device_names) {
            /* manually built a dictonary with the mapping fromt he device ID to device name */
            device_names.Add("1E9001", "1200");
            device_names.Add("1E9101", "2313");
            device_names.Add("1E9102", "2323");
            device_names.Add("1E9103", "2343");
            device_names.Add("1E9201", "4414");
            device_names.Add("1E9203", "4433");
            device_names.Add("1E9303", "4434/8535");
            device_names.Add("1E9301", "8515");
            device_names.Add("1E9703", "ATmega1280");
            device_names.Add("1E9704", "ATmega1281");
            device_names.Add("1E9706", "ATmega1284");
            device_names.Add("1E9705", "ATmega1284P");
            device_names.Add("1EA703", "ATmega1284RFR2");
            device_names.Add("1E9702", "ATmega128[A]");
            device_names.Add("1EA701", "ATmega128RFA1");
            device_names.Add("1EA702", "ATmega128RFR2");
            device_names.Add("1E9404", "ATmega162");
            device_names.Add("1E940F", "ATmega164A");
            device_names.Add("1E940A", "ATmega164P[A]");
            device_names.Add("1E9410", "ATmega165A");
            device_names.Add("1E9407", "ATmega165P[A]");
            device_names.Add("1E9406", "ATmega168[A]");
            device_names.Add("1E940B", "ATmega168P[A]");
            device_names.Add("1E9411", "ATmega169A");
            device_names.Add("1E9405", "ATmega169P[A]");
            device_names.Add("1E9403", "ATmega16[A]");
            device_names.Add("1E940C", "ATmega16HVA");
            device_names.Add("1E940D", "ATmega16HVB");
            device_names.Add("1E9484", "ATmega16M1");
            device_names.Add("1E9489", "ATmega16U2");
            device_names.Add("1E9488", "ATmega16U4");
            device_names.Add("1E9801", "ATmega2560");
            device_names.Add("1E9802", "ATmega2561");
            device_names.Add("1EA803", "ATmega2564RFR2");
            device_names.Add("1EA802", "ATmega256RFR2");
            device_names.Add("1E9515", "ATmega324A");
            device_names.Add("1E9511", "ATmega324PA");
            device_names.Add("1E9508", "ATmega324P");
            device_names.Add("1E950E", "ATmega3250A/P/PA");
            device_names.Add("1E9506", "ATmega3250");
            device_names.Add("1E9505", "ATmega325/A/PA");
            device_names.Add("1E950D", "ATmega325P");
            device_names.Add("1E9514", "ATmega328");
            device_names.Add("1E950F", "ATmega328P");
            device_names.Add("1E950C", "ATmega3290A/P/PA");
            device_names.Add("1E9504", "ATmega3290");
            device_names.Add("1E9503", "ATmega329[A]");
            device_names.Add("1E950B", "ATmega329P[A]");
            device_names.Add("1E9502", "ATmega32[A]");
            device_names.Add("1E9586", "ATmega32C1");
            device_names.Add("1E9510", "ATmega32HVB[revB]");
            device_names.Add("1E9584", "ATmega32M1");
            device_names.Add("1E958A", "ATmega32U2");
            device_names.Add("1E9587", "ATmega32U4");
            device_names.Add("1E9507", "ATmega406");
            device_names.Add("1E9205", "ATmega48[A]");
            device_names.Add("1E920A", "ATmega48P[A]");
            device_names.Add("1E9608", "ATmega640");
            device_names.Add("1E9609", "ATmega644[A]");
            device_names.Add("1E960A", "ATmega644P[A]");
            device_names.Add("1EA603", "ATmega644RFR2");
            device_names.Add("1E9606", "ATmega6450/P/A");
            device_names.Add("1E9605", "ATmega645/P/A");
            device_names.Add("1E9604", "ATmega6490/P/A");
            device_names.Add("1E9603", "ATmega649[A]");
            device_names.Add("1E960B", "ATmega649P");
            device_names.Add("1E9602", "ATmega64[A]");
            device_names.Add("1E9686", "ATmega64C1");
            device_names.Add("1E9610", "ATmega64HVE2");
            device_names.Add("1E9684", "ATmega64M1");
            device_names.Add("1EA602", "ATmega64RFR2");
            device_names.Add("1E9306", "ATmega8515");
            device_names.Add("1E9308", "ATmega8535");
            device_names.Add("1E930A", "ATmega88[A]");
            device_names.Add("1E930F", "ATmega88P[A]");
            device_names.Add("1E9307", "ATmega8[A]");
            device_names.Add("1E9310", "ATmega8HVA");
            device_names.Add("1E9389", "ATmega8U2");
            device_names.Add("1E9003", "ATtiny10");
            device_names.Add("1E9007", "ATtiny13[A]");
            device_names.Add("1E9412", "ATtiny1634");
            device_names.Add("1E9487", "ATtiny167");
            device_names.Add("1E910F", "ATtiny20");
            device_names.Add("1E910A", "ATtiny2313[A]");
            device_names.Add("1E910B", "ATtiny24[A]");
            device_names.Add("1E9108", "ATtiny25");
            device_names.Add("1E910C", "ATtiny261[A]");
            device_names.Add("1E9109", "ATtiny26");
            device_names.Add("1E9107", "ATtiny28");
            device_names.Add("1E920E", "ATtiny40");
            device_names.Add("1E920D", "ATtiny4313");
            device_names.Add("1E920C", "ATtiny43U");
            device_names.Add("1E9215", "ATtiny441");
            device_names.Add("1E9207", "ATtiny44[A]");
            device_names.Add("1E9206", "ATtiny45");
            device_names.Add("1E9208", "ATtiny461[A]");
            device_names.Add("1E9209", "ATtiny48");
            device_names.Add("1E8F0A", "ATtiny4");
            device_names.Add("1E8F09", "ATtiny5");
            device_names.Add("1E9314", "ATtiny828");
            device_names.Add("1E9315", "ATtiny841");
            device_names.Add("1E930C", "ATtiny84[A]");
            device_names.Add("1E930B", "ATtiny85");
            device_names.Add("1E930D", "ATtiny861[A]");
            device_names.Add("1E9387", "ATtiny87");
            device_names.Add("1E9311", "ATtiny88");
            device_names.Add("1E9008", "ATtiny9");

        }

    }
}
