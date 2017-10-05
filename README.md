# Chip8-Assembler
Chip8 Assembler made in C#

Converts Assembly file with Chip8 instructions into Chip8 binary rom.

### C urrently supports:

- All Instructions
- Usage of Labels instead of addresses

### Planned features:

- Direct data storage (easy access using label)
- Organization

## IMPORTANT RULES

- Labels can't share line with code (in current version)
- No whitespace allowed in front of instruction (in current version)
- Instruction and arguments need to be divided by space
- Arguments need to be separated by commas (whitespace allowed)
- Try to avoid empty lines
