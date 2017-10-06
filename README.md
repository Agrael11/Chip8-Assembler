# Chip8-Assembler
Chip8 Assembler made in C#

Converts Assembly file with Chip8 instructions into Chip8 binary rom.

### Currently supports:

- All Instructions
- Usage of Labels instead of addresses
- Direct data storage (easy access using label) usage : DATA 16bit value
- Data organization. Usage: ORG address (0x200 - 0xFFF, have to be divisible by 2)

## IMPORTANT RULES

- Instruction and arguments need to be divided by space
- Arguments need to be separated by commas (whitespace allowed)
- .ORG needs to be divisible by 2