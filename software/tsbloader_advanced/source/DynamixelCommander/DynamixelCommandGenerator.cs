using System;
using System.Collections.Generic;
using System.Text;

namespace DynamixelCommander
{
    public class DynamixelCommandGenerator
    {

        public const byte DYN1_INSTR_PING = 0x1;
        public const byte DYN1_INSTR_READ = 0x2;
        public const byte DYN1_INSTR_WRITE = 0x3;
        public const byte DYN1_INSTR_SYNC_WRITE = 0x83;
        public const byte DYN1_INSTR_BULK_READ = 0x92;
        public const byte DYN1_INSTR_WRITE_NO_REPLY = 0x33; // extension to DYN1 implemented in Seed Robotics fw
        public const byte DYN1_INSTR_REBOOT = 0x8; // extension in DYN1; not part of DYN1 standard
        public const byte DYN1_INSTR_JUMP_TO_BOOTLDR = 0x9; // extension in DYN1; not part of DYN1 standard

        public const byte ID_BROADCAST = 0xfe;

        public enum en_DynProtocol_Version { Dynamixel_1 = 1, Dynamixel_2 = 2 }

        private en_DynProtocol_Version dyn_protocol_version;

        public DynamixelCommandGenerator(en_DynProtocol_Version protocol_version)
        {
            dyn_protocol_version = protocol_version;
        }


        public byte[] generate_dyn_packet(byte ID, byte INSTRUCTION, List<byte> PARAMS)
        {
            byte checksum = 0;

            List<byte> command_bytes = new List<byte>();

            command_bytes.Add(0xff);
            command_bytes.Add(0xff);
            command_bytes.Add(ID);
            command_bytes.Add((byte)(PARAMS.Count + 2));
            command_bytes.Add(INSTRUCTION);            
            command_bytes.AddRange(PARAMS);

            for (byte b = 2; b < command_bytes.Count; b++) {
                checksum = (byte) (checksum + command_bytes[b]);
            }
            checksum = (byte) (~checksum);

            command_bytes.Add(checksum);

            return command_bytes.ToArray();
        }

        public byte[] generate_write_packet(byte ID, byte wr_start_address, List<byte> data_to_write)
        {
            data_to_write.Insert(0, wr_start_address);

            return generate_dyn_packet(ID, DYN1_INSTR_WRITE, data_to_write);
        }

        public byte[] generate_read_packet(byte ID, byte start_addr, byte read_length)
        {
            List<byte> cmd_params = new List<byte>();
            cmd_params.Add(start_addr);
            cmd_params.Add(read_length);

            return generate_dyn_packet(ID, DYN1_INSTR_READ, cmd_params);
        }

        public byte[] generate_reboot_packet(byte ID)
        {
            return generate_dyn_packet(ID, DYN1_INSTR_REBOOT, new List<byte>());
        }

        public byte[] generate_ping_packet(byte ID)
        {
            return generate_dyn_packet(ID, DYN1_INSTR_PING, new List<byte>());
        }

        public byte[] generate_jumpt_to_bootldr_packet(byte ID, int delay_ms, byte baud_rate_register_low, byte baud_rate_register_high, int jump_address)
        {
            List<byte> cmd_params = new List<byte>();
            cmd_params.Add((byte)delay_ms);
            cmd_params.Add((byte)(delay_ms >> 8));
            cmd_params.Add(baud_rate_register_low);
            cmd_params.Add(baud_rate_register_high);
            cmd_params.Add((byte)jump_address);
            cmd_params.Add((byte)(jump_address >> 8));

            return generate_dyn_packet(ID, DYN1_INSTR_JUMP_TO_BOOTLDR, cmd_params);
        }

        public int get_value_from_reply(byte[] reply)
        {
            if (reply.Length >= 6)
            {
                int len = (int)reply[3] - 2;

                int value = 0;
                for (byte b=0; b < len; b++)
                {
                    value = value + (int)reply[b + 5] + b * 256;
                }

                return value;
            } else
            {
                return -1;
            }
        }

    }
}
