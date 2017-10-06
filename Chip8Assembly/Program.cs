using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chip8Assembly
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Tachi Chip8 Assembler");
            Console.WriteLine("(C)2017 Tachi");
            Console.WriteLine("");
            string input = "";
            string output = "";
            for (int i = 0; i < args.Length; i++)
            {
                if ((args[i] == "/i") || (args[i] == "-i") || (args[i] == "--Input"))
                {
                    i++;
                    try
                    {
                        input = args[i];
                    }
                    catch (Exception ex)
                    {
                        LogError(ex);
                        return;
                    }
                }
                else if ((args[i] == "/o") || (args[i] == "-o") || (args[i] == "--Output"))
                {
                    i++;
                    try
                    {
                        output = args[i];
                    }
                    catch (Exception ex)
                    {
                        LogError(ex);
                        return;
                    }
                }
                else if ((args[i] == "/h") || (args[i] == "-h") || (args[i] == "--Help"))
                {
                    Console.WriteLine("Command line arguments");
                    Console.WriteLine("--Input  [-i,/i]        Select Input File");
                    Console.WriteLine("--Output [-o,/o]        Select Output File");
                    Console.WriteLine("--Help   [-h,/h]        Show this information");
                    Console.WriteLine();
                    Console.WriteLine("Use : " + System.AppDomain.CurrentDomain.FriendlyName + " --Input \"C:\\Chip8\\Program.asm\" --Output \"C:\\Chip8\\Program.ch8\"");
                    return;
                }
                else
                {
                    Console.WriteLine("Unknown argument: " + args[i]);
                    Console.WriteLine("--Help   [-h,/h]        Show this information");
                    return;
                }
            }
            if (string.IsNullOrWhiteSpace(input))
            {
                Console.WriteLine("Input file not specified");
                Console.WriteLine("--Input  [-i,/i]        Select Input File");
                Console.WriteLine("--Help   [-h,/h]        Show this information");
                return;
            }
            if (string.IsNullOrWhiteSpace(output))
            {
                try
                {
                    if (input.LastIndexOf('.') >= 0)
                    {
                        output = input.Substring(0, input.LastIndexOf('.')) + ".ch8";
                    }
                    else
                    {
                        output = input + ".ch8";
                    }
                }
                catch
                {
                    Console.WriteLine("Output file not specified");
                    Console.WriteLine("--Output [-o,/o]        Select Output File");
                    Console.WriteLine("--Help   [-h,/h]        Show this information");
                    return;
                }
            }
            try
            {
                string[] original = System.IO.File.ReadAllLines(input).Where(t => !String.IsNullOrWhiteSpace(t)).ToArray();
                new Program().Start(original, output);
                Console.WriteLine("All done!");
            }
            catch (Exception ex)
            {
                LogError(ex);
            }
        }

        List<Instruction> instructions = new List<Instruction>();
        Dictionary<string, ushort> Labels = new Dictionary<string, ushort>();


        public byte ParseRegister(string input)
        {
            if (!input.StartsWith("V")) throw new Exception("Regsiter not valid, have to start with V: " + input);
            int reg = 255;
            try { reg = Convert.ToInt32("0x" + input.Substring(1), 16); }
            catch { throw new Exception("Register not valid, have to be value 0-F: " + input); }
            if (reg > 0xF) throw new Exception("Register not valid, have to be value 0-F: " + input);
            return (Byte)reg;
        }

        public ushort ParseAddress(string input)
        {
            int parse;
            int addr;
            if (int.TryParse(input, out parse)) addr = parse;
            else
            {
                if (!Labels.ContainsKey(input)) throw new Exception("Incorrect address/label: " + input);
                addr = (Labels[input]*2) + 0x200;
            }
            if (addr > 0xFFF) throw new Exception("Address higher then 12 bit: " + addr);
            return (ushort)addr;
        }

        public byte ParseNibble(string input)
        {
            int parse;
            if (!int.TryParse(input, out parse)) throw new Exception("Nibble not number: " + input);
            if (parse > 0xF) throw new Exception("Nibble have to be 4 bits: " + input);
            return (byte)parse;
        }

        public byte ParseByte(string input)
        {
            int parse;
            if (!int.TryParse(input, out parse)) throw new Exception("Byte not number: " + input);
            if (parse > 0xFF) throw new Exception("Byte have to be 8 bits: " + input);
            return (Byte)parse;
        }

        public Instruction DecodeInstruction(string instruction)
        {
            string instr = instruction;
            string[] args = new string[0];

            if (instruction.IndexOf(' ') >= 0)
            {
                if (instruction.Contains(":"))
                {
                    instr = instruction.Split(':')[0] + ":";
                    args = new string[] { instruction.Split(':')[1].TrimStart(' ') };
                }
                else
                {
                    instr = instruction.Substring(0, instruction.IndexOf(' '));
                    string arg = instruction.Substring(instruction.IndexOf(' ') + 1).Replace(" ", "");
                    args = arg.Split(',');
                    for (int j = 0; j < args.Length; j++)
                    {
                        if (args[j].StartsWith("0x"))
                        {
                            args[j] = Convert.ToUInt16(args[j], 16).ToString();
                        }
                        else if (args[j].StartsWith("0b"))
                        {
                            args[j] = Convert.ToUInt16(args[j], 2).ToString();
                        }
                        args[j] = args[j].ToUpper();
                    }
                }
            }
            return new Instruction() { AssemblyInstr = instr.ToUpper(), AssemblyArgs = args, OriginalCode = instruction };
        }

        public void Start(string[] data, string output)
        {
            try
            {
                for (var i = 0; i < data.Length; i++)
                {
                    instructions.Add(DecodeInstruction(data[i].Trim(' ','\t')));
                }
                FirstPass();
                SecondPass();
                ThirdPass(output);
            }
            catch (Exception ex)
            {
                LogError(ex);
                return;
            }
        }

        public static void LogError(Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(ex.Message);
            if (ex.InnerException != null)
            {
                LogError(ex.InnerException);
            }
            Console.ForegroundColor = ConsoleColor.Gray;
        }

        public void FirstPass()
        {
            for (var i = 0; i < instructions.Count; i++)
            {
                try
                {
                    if (instructions[i].AssemblyInstr.EndsWith(":"))
                    {
                        Console.WriteLine("Label: " + instructions[i].AssemblyInstr + "@" + i);
                        Labels.Add(instructions[i].AssemblyInstr.Substring(0, instructions[i].AssemblyInstr.Length - 1), (ushort)i);

                        if (instructions[i].AssemblyArgs.Length > 0)
                        {
                            instructions[i] = DecodeInstruction(instructions[i].AssemblyArgs[0]);
                            i--;
                        }
                        else
                        {
                            instructions.RemoveAt(i);
                            i--;
                        }
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception($"Error at line {i + 1} : {instructions[i].OriginalCode}", ex);
                }
            }
        }

        public void SecondPass()
        {
            for (var i = 0; i < instructions.Count; i++)
            {
                try
                {
                    switch (instructions[i].AssemblyInstr)
                    {
                        case "CLS": instructions[i].Opcode = CLS(instructions[i]); break;
                        case "RET": instructions[i].Opcode = RET(instructions[i]); break;
                        case "SYS": instructions[i].Opcode = SYS(instructions[i]); break;
                        case "JP": instructions[i].Opcode = JP(instructions[i]); break;
                        case "CALL": instructions[i].Opcode = CALL(instructions[i]); break;
                        case "SE": instructions[i].Opcode = SE(instructions[i]); break;
                        case "SNE": instructions[i].Opcode = SNE(instructions[i]); break;
                        case "SKP": instructions[i].Opcode = SKP(instructions[i]); break;
                        case "SKNP": instructions[i].Opcode = SKNP(instructions[i]); break;
                        case "LD": instructions[i].Opcode = LD(instructions[i]); break;
                        case "AND": instructions[i].Opcode = AND(instructions[i]); break;
                        case "OR": instructions[i].Opcode = OR(instructions[i]); break;
                        case "XOR": instructions[i].Opcode = XOR(instructions[i]); break;
                        case "ADD": instructions[i].Opcode = ADD(instructions[i]); break;
                        case "SUB": instructions[i].Opcode = SUB(instructions[i]); break;
                        case "SUBN": instructions[i].Opcode = SUBN(instructions[i]); break;
                        case "SHR": instructions[i].Opcode = SHR(instructions[i]); break;
                        case "SHL": instructions[i].Opcode = SHL(instructions[i]); break;
                        case "RND": instructions[i].Opcode = RND(instructions[i]); break;
                        case "DRW": instructions[i].Opcode = DRW(instructions[i]); break;
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception($"Error at line {i} : {instructions[i].OriginalCode}", ex);
                }
            }
        }

        public void ThirdPass(string output)
        {
            List<byte> toWriteList = new List<byte>();
            for (int i = 0; i < instructions.Count; i++)
            {
                byte Lower = (byte)((instructions[i].Opcode >> 8) & 0xFF);
                byte Upper = (byte)(instructions[i].Opcode & 0xFF);
                toWriteList.Add(Lower);
                toWriteList.Add(Upper);
            }
            System.IO.File.WriteAllBytes(output, toWriteList.ToArray());
        }

        public ushort CLS(Instruction instr)
        {
            return 0x00E0;
        }

        public ushort RET(Instruction instr)
        {
            return 0x00EE;
        }

        public ushort SYS(Instruction instr)
        {
            ushort arg = ParseAddress(instr.AssemblyArgs[0]);
            return arg;
        }

        public ushort JP(Instruction instr)
        {
            if (instr.AssemblyArgs.Length == 2)
            {
                //BNNN
                if (instr.AssemblyArgs[0] != "V0") throw new Exception("Incorrect register: " + instr.AssemblyArgs[0]);
                ushort addr = ParseAddress(instr.AssemblyArgs[1]);
                if (addr > 0xFFF) throw new Exception("Address higher then 12 bit: " + addr);
                return (ushort)(0xB000 + addr);
            }
            else
            {
                //1NNN
                ushort addr = ParseAddress(instr.AssemblyArgs[0]);
                return (ushort)(0x1000 + addr);
            }
            throw new NotImplementedException();
        }

        public ushort CALL(Instruction instr)
        {
            //2NNN
            ushort addr = ParseAddress(instr.AssemblyArgs[0]);
            return (ushort)(0x2000 + addr);
        }

        public ushort SE(Instruction instr)
        {
            byte reg1 = ParseRegister(instr.AssemblyArgs[0]);
            try
            {
                byte reg2 = ParseRegister(instr.AssemblyArgs[1]);
                return (ushort)(0x5000 + (reg1 << 8) + (reg2 << 4));
            }
            catch
            {
                try
                {
                    byte val = ParseByte(instr.AssemblyArgs[1]);
                    return (ushort)(0x3000 + (reg1 << 8) + val);
                }
                catch
                {
                    throw new Exception("Second parameter have to be Register or Byte value: " + instr.AssemblyArgs[1]);
                }
            }
        }

        public ushort SNE(Instruction instr)
        {
            byte reg1 = ParseRegister(instr.AssemblyArgs[0]);
            try
            {
                byte reg2 = ParseRegister(instr.AssemblyArgs[1]);
                return (ushort)(0x4000 + (reg1 << 8) + (reg2 << 4));
            }
            catch
            {
                try
                {
                    byte val = ParseByte(instr.AssemblyArgs[1]);
                    return (ushort)(0x9000 + (reg1 << 8) + val);
                }
                catch
                {
                    throw new Exception("Second parameter have to be Register or Byte value: " + instr.AssemblyArgs[1]);
                }
            }
        }

        public ushort SKP(Instruction instr)
        {
            //Ex9E
            byte reg = ParseRegister(instr.AssemblyArgs[0]);
            return (ushort)(0xE09E + (reg << 8));
        }

        public ushort SKNP(Instruction instr)
        {
            //ExA1
            byte reg = ParseRegister(instr.AssemblyArgs[0]);
            return (ushort)(0xE0A1 + (reg << 8));
        }

        public ushort LD(Instruction instr)
        {
            if (instr.AssemblyArgs[0] == "F")
            {
                //LD F, Vx - Fx29
                byte reg2 = ParseRegister(instr.AssemblyArgs[1]);
                return (ushort)(0xF029 + (reg2 << 8));
            }
            else if (instr.AssemblyArgs[0] == "B")
            {
                //LD B, Vx - Fx33
                byte reg2 = ParseRegister(instr.AssemblyArgs[1]);
                return (ushort)(0xF033 + (reg2 << 8));
            }
            else if (instr.AssemblyArgs[0] == "DT")
            {
                //LD DT, Vx - Fx15 
                byte reg2 = ParseRegister(instr.AssemblyArgs[1]);
                return (ushort)(0xF015 + (reg2 << 8));
            }
            else if (instr.AssemblyArgs[0] == "ST")
            {
                //LD ST, Vx - Fx18
                byte reg2 = ParseRegister(instr.AssemblyArgs[1]);
                return (ushort)(0xF018 + (reg2 << 8));
            }
            else if (instr.AssemblyArgs[0] == "I")
            {
                //LD I, addr -  Annn
                ushort addr = ParseAddress(instr.AssemblyArgs[1]);
                return (ushort)(0xA000 + addr);
            }
            else if (instr.AssemblyArgs[0] == "[I]")
            {
                //LD [I], Vx - Fx55
                byte reg2 = ParseRegister(instr.AssemblyArgs[1]);
                return (ushort)(0xF055 + (reg2 << 8));
            }
            else
            {
                byte reg1 = ParseRegister(instr.AssemblyArgs[0]);
                if (instr.AssemblyArgs[1] == "DT")
                {
                    //LD Vx, DT - Fx07
                    return (ushort)(0xF007 + (reg1 << 8));
                }
                if (instr.AssemblyArgs[1] == "K")
                {
                    //LD Vx, K - Fx0A
                    return (ushort)(0xF00A + (reg1 << 8));
                }
                if (instr.AssemblyArgs[1] == "[I]")
                {
                    //LD Vx, [I] - Fx65
                    return (ushort)(0xF065 + (reg1 << 8));
                }
                try
                {
                    //LD Vx, Vy - 8xy0
                    byte reg2 = ParseRegister(instr.AssemblyArgs[1]);
                    return (ushort)(0x8000 + (reg1 << 8) + (reg2 << 4));
                }
                catch
                {
                    try
                    {
                        //LD Vx, byte - 6xkk
                        byte value = ParseByte(instr.AssemblyArgs[1]);
                        return (ushort)(0x6000 + (reg1 << 8) + value);
                    }
                    catch
                    {
                        throw new Exception("Second argument needs to be: DT, K, [I], Register or Byte :" + instr.AssemblyArgs[1]);
                    }
                }
            }

        }

        public ushort AND(Instruction instr)
        {
            //8xy2
            byte reg1 = ParseRegister(instr.AssemblyArgs[0]);
            byte reg2 = ParseRegister(instr.AssemblyArgs[1]);
            return (ushort)(0x8002 + (reg1 << 8) + (reg2 << 4));
        }


        public ushort OR(Instruction instr)
        {
            //8xy1
            byte reg1 = ParseRegister(instr.AssemblyArgs[0]);
            byte reg2 = ParseRegister(instr.AssemblyArgs[1]);
            return (ushort)(0x8001 + (reg1 << 8) + (reg2 << 4));
        }

        public ushort XOR(Instruction instr)
        {
            //8xy3
            byte reg1 = ParseRegister(instr.AssemblyArgs[0]);
            byte reg2 = ParseRegister(instr.AssemblyArgs[1]);
            return (ushort)(0x8003 + (reg1 << 8) + (reg2 << 4));
        }

        public ushort ADD(Instruction instr)
        {
            if (instr.AssemblyArgs[0] == "I")
            {
                //I.Vx - Fx1E
                byte reg = ParseRegister(instr.AssemblyArgs[1]);
                return (ushort)(0xF01E + (reg << 8));
            }
            else
            {
                byte reg1 = ParseRegister(instr.AssemblyArgs[0]);
                try
                {
                    //Vx,Vy - 8xy4
                    byte reg2 = ParseRegister(instr.AssemblyArgs[1]);
                    return (ushort)(0x8004 + (reg1 << 8) + (reg2 << 4));
                }
                catch
                {
                    try
                    {
                        //Vx,Byte - 7xkk
                        byte reg2 = ParseByte(instr.AssemblyArgs[1]);
                        return (ushort)(0x7000 + (reg1 << 8) + reg2);
                    }
                    catch
                    {
                        throw new Exception("Second argument have to be register or number: " + instr.AssemblyArgs[1]);
                    }
                }
            }
        }

        public ushort SUB(Instruction instr)
        {
            //8xy5
            byte reg1 = ParseRegister(instr.AssemblyArgs[0]);
            byte reg2 = ParseRegister(instr.AssemblyArgs[1]);
            return (ushort)(0x8005 + (reg1 << 8) + (reg2 << 4));
        }

        public ushort SUBN(Instruction instr)
        {
            //8xy7
            byte reg1 = ParseRegister(instr.AssemblyArgs[0]);
            byte reg2 = ParseRegister(instr.AssemblyArgs[1]);
            return (ushort)(0x8007 + (reg1 << 8) + (reg2 << 4));
        }

        public ushort SHR(Instruction instr)
        {
            //8xy6
            byte reg1 = ParseRegister(instr.AssemblyArgs[0]);
            byte reg2 = 0;
            if (instr.AssemblyArgs.Length > 1)
            {
                reg2 = ParseRegister(instr.AssemblyArgs[1]);
            }
            return (ushort)(0x8006 + (reg1 << 8) + (reg2 << 4));
        }

        public ushort SHL(Instruction instr)
        {
            //8xyE
            byte reg1 = ParseRegister(instr.AssemblyArgs[0]);
            byte reg2 = 0;
            if (instr.AssemblyArgs.Length > 1)
            {
                reg2 = ParseRegister(instr.AssemblyArgs[1]);
            }
            return (ushort)(0x800E + (reg1 << 8) + (reg2 << 4));
        }

        public ushort RND(Instruction instr)
        {
            //Cxkk
            byte reg = ParseRegister(instr.AssemblyArgs[0]);
            byte val = ParseByte(instr.AssemblyArgs[1]);
            return (ushort)(0xC000 + (reg << 8) + val);
        }

        public ushort DRW(Instruction instr)
        {
            //Dxyn
            byte reg1 = ParseRegister(instr.AssemblyArgs[0]);
            byte reg2 = ParseRegister(instr.AssemblyArgs[1]);
            byte val = ParseNibble(instr.AssemblyArgs[2]);
            return (ushort)(0xD000 + (reg1 << 8) + (reg2 << 4) + val);
        }
    }
}
