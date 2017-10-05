using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chip8Assembly
{
    public class Instruction
    {
        public string AssemblyInstr;
        public string[] AssemblyArgs;
        public ushort Opcode;
        public string OriginalCode;

        public override string ToString()
        {
            string ret = "(0x" + Convert.ToString(Opcode, 16).PadLeft(4, '0') + ") " + AssemblyInstr;
            if (AssemblyArgs.Length > 0)
            {
                ret += " ";
                for (int i = 0; i < AssemblyArgs.Length - 1; i++)
                {
                    ret += AssemblyArgs[i] + ", ";
                }
                ret += AssemblyArgs[AssemblyArgs.Length - 1];
            }
            return ret;
        }
    }
}
