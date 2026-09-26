# VitaTestSuite

Test suite for the PS Vita: **step-by-step execution and exploration of dumped Vita code**
in a sandbox, with a full log of every hardware (MMIO) register access.

Load almost any binary the console contains - ARM modules (`*.self`, `*.suprx`, `*.skprx`, ELF),
CMeP / "F00D" first-loader and secure-kernel images, Venezia MPE payloads and Ernie (RL78 syscon)
dumps - single-step it, watch the registers and the disassembly, and read the resulting device
access log.

---

## Components

| Component | File |
|---|---|
| **ARM Cortex-A9 MPCore** interpreter (ARMv7-A: A32, Thumb-16, Thumb-2, CP15 + short-descriptor MMU, VFPv3) | `Core/ArmCore.cs`, `Core/ArmDisasm.cs`, `Core/ArmMmu.cs`, `Core/ArmVfp.cs` |
| **Toshiba MeP-c5** interpreter (CMeP / "F00D"; Venezia MPE profile) | `Core/MePCore.cs`, `Core/MePIsa.cs` |
| **Renesas RL78** interpreter (Ernie / syscon) | `Core/Rl78Core.cs`, `Core/Rl78Decode.cs`, `Core/Rl78Isa.cs`, `Core/Rl78Disasm.cs`, `Core/Rl78IsaTable.g.cs` |
| Memory model + MMIO device bus + access log | `MemoryHub.cs`, `Core/AccessLog.cs` |
| MMIO device models (keyring, mailbox, Bigmac, Bignum, eMMC crypto, GPIO, secure controller, Ernie SFRs) | `Core/Devices/VitaDevices.cs` |
| SELF/SRVK/SPKG container, metadata decryption, key store (ported from `pup_fiction`) | `Core/SceSelf.cs`, `Core/SceKeys.cs`, `Core/CoreAes.cs` |
| ELF32/ELF64 loader, Vita module (SceModuleInfo import/export NID) parsing | `Core/ElfImage.cs` |
| PUP package (PSP2UPDAT) parsing and extraction | `Core/PupPackage.cs` |
| NID to symbol-name database (`db.yml`, 8534 entries) | `Core/NidDatabase.cs`, `Docs/nid_db.yml` |
| Architecture sniffing, unified load, sandbox orchestration | `Core/ImageLoader.cs`, `Core/Sandbox.cs` |
| GUI (register / disassembly / memory / log panes) | `Form1.cs` |
| Command processor + headless script mode | `CommandProcessor.cs`, `Program.cs` |

---

## Building

The project targets .NET Framework 4.8 (WinForms) and builds with the .NET SDK:

```powershell
& "C:\Program Files\dotnet\dotnet.exe" build VitaTestSuite\VitaTestSuite.csproj
```

Output: `Build\VitaTestSuite.exe`.

> The `.resx` files contain binary resources, so the SDK build needs
> `System.Resources.Extensions` (plus `System.Memory`, `System.Buffers`,
> `System.Numerics.Vectors`, `System.Runtime.CompilerServices.Unsafe`).
> Those assemblies are vendored under `VitaTestSuite\libs\` and copied next to the
> executable. The project is otherwise unchanged and still opens and builds in Visual Studio.

---

## Using the GUI

Start `Build\VitaTestSuite.exe` (no arguments). The window has three central panes plus a bottom
log pane:

| Pane | Content |
|---|---|
| Left (tabs) | register views - one tab per core: **ARM Regs**, **MeP Regs**, **RL78 Regs** |
| Middle (tabs) | disassemblers - one tab per core: **ARM Core**, **MeP Core**, **RL78 Core** |
| Right (tabs) | **Memory** (hex dump; clicking a disassembly line follows it) |
| Bottom (tabs) | **Report** (main) and **MMIO log** |

The tab of the active core is selected automatically when the architecture changes.
Toolbar: **Step**, **Step x10**, **Run**, **Reset**.

The **File** menu loads images:

* **Open Image...** - sniff the file (ELF / SELF / PUP / raw) and load it, switching the
  sandbox to the right core automatically.
* **Load CMeP first_loader...** - load a 16 KiB `vita_prototype_bootrom.bin`-style image at `0x5C000`.
* **Load Ernie (RL78) dump...** - load `USS-1001.bin` / `USS-1002.bin`, reset to the vector at `0x0000`.
* **Load SELF module...** - decrypt the SELF, rebuild the ELF, load the segments, parse the module tables.
* **Show MMIO access log** - dump the last 200 hardware accesses into the log pane.

---

## Headless / scripting mode

Everything the GUI does is a command, so runs are reproducible:

```powershell
cd Build
.\VitaTestSuite.exe -script ..\Scripts\verify_all.cmd -log verify_all.log
.\VitaTestSuite.exe -cmd "arch mep" "loadfirstloader ..\..\dumps\vita_prototype_bootrom.bin" "run 200000" "mmiolog stats"
.\VitaTestSuite.exe -uicheck      # print the pane/tab layout and exit
```

The exit code is 1 when any command reported a failure.
Ready-made scripts live in `Scripts\`: `verify_all.cmd`, `mep_first_loader.cmd`,
`rl78_ernie.cmd`, `arm_self.cmd`, `self_module.cmd`, `smoke.cmd`.

### Command reference

| Command | Description |
|---|---|
| `arch <arm\|mep\|venezia\|rl78\|status>` | Choose the emulated core and install its memory map + devices |
| `open <file> [addr]` | Sniff and load any image (ELF/SELF/PUP/raw) |
| `loadelf`, `loadself` | Explicit loaders (same as `open`) |
| `loadfirstloader <file>` | CMeP boot ROM / first loader at `0x5C000` (+ stack seeding) |
| `loadernie <file>` | Ernie (RL78) dump, reset to the vector at `0x000000` |
| `load <addr> <file>` | Raw bytes at an address |
| `info`, `mem`, `devices`, `modinfo` | Image / regions / device registers / module imports+exports |
| `pup <file.pup>`, `pupget <file.pup> <i> <out>` | List / extract PUP segments |
| `reset [addr]`, `step [n]`, `run [n]`, `cont [limit]` | Execution control |
| `regs`, `setreg <name> <value>` | Registers (`setreg pc <addr>` jumps) |
| `dis [addr] [count] [arm\|thumb]` | Disassemble, with an instruction-set override for ARM |
| `coverage <start> <end>` | Linear disassembly coverage + mnemonic histogram |
| `bp <addr> \| bp list \| bp clear [addr]` | Breakpoints |
| `dump <addr> [size]`, `peek`, `poke`, `addmem`, `mapmem` | Memory |
| `mmiolog <on\|off\|tail [n]\|unmapped [n]\|clear\|stats\|ram on\|ram off>` | Hardware access log |
| `devset <addr> <value>`, `devget <addr>` | Poke an MMIO register directly |
| `keyring` | Key material captured by the CMeP keyring window |
| `trace <on\|off>` | Echo every executed instruction |
| `nids <db.yml>` | Load the NID to name database |

Command names are case-insensitive.

---

## Sandboxes

### CMeP ("F00D", Toshiba MeP-c5)

```
0x00040000  cmep_ram   128 KiB  vector base / next-stage staging
0x0005C000  cmep_rom    16 KiB  first-loader window (writable: the ROM wipes itself)
0x00800000  cmep_priv  128 KiB  private RAM (secure kernel / secure modules)
```

Devices (every access logged): mailbox `0xE0000000`, flags `0xE0020000`, **keyring** `0xE0030000`
(captured key material + clear/query history), Bignum `0xE0040800`, Bigmac `0xE0050000`,
strap `0xE0062000`, eMMC crypto `0xE0070000`, GPIO/handshake `0xE20A0000`, secure controller `0xE3100000`.

`arch venezia` adds the `0x80000000` SPRAM window and turns on IVC2 VLIW packet recognition
(Venezia = 4 MPE cores, each an MeP-c5 + IVC2 coprocessor).

Verified: `vita_prototype_bootrom.bin` and `pch-5c-cold_first_loader.bin` execute 200 000 instructions
from reset with **0 undefined instructions and 0 unmapped accesses**, clearing `.bss` and running
down to the ARM mailbox poll loop (`R32 0xE0000010`), with every MMIO access matching the static
analysis in `..\dumps\bootrom_analysis\*.mmio.csv`.

### Ernie (Renesas RL78 syscon)

```
0x00000000  ernie_flash  1 MiB   the glitch dump (USS-1001.bin / USS-1002.bin / ernie_tv_*)
0x000FFF00  Ernie.SFR    256 B   SFR window (logged through the MMIO bus)
```

Verified: the whole 0x00000-0x3E000 code region disassembles with **100 % known instructions**,
and `USS-1001.bin` executes 300 000 instructions from the reset vector `0xE000` with 0 unmapped
accesses, finishing in a stable hardware handshake loop:

```
0x3005A: cmp !0xFFFA2, #0xC0
0x3005E: bnz $0x3005A        ; waits for the syscon handshake byte, which no model provides
```

### ARM Cortex-A9 MPCore (Kermit)

```
0x00000000  arm_rom     64 KiB   vectors / boot stub
0x1F000000  arm_sram   256 KiB
0x40000000  arm_priv    64 MiB
0x80000000  arm_main    64 MiB   main DRAM; the sandbox seeds SP to 0x83FFFEF0
```

MMIO seen by ARM (CMeP mailbox mirror, pervasive/secure-controller windows, UART) goes through the
same access log. Vita module ELFs use `e_entry` as the **SceModuleInfo offset**, so the loader says
so explicitly and points you at `modinfo` for the exported functions. ARM NEON is decoded but not
executed (it never desynchronises the stream); VFPv3 is implemented.

---

## Hardware access log

* Every access that lands on an `MmioDevice` is recorded (`AccessLog`, 200 000-entry ring buffer)
  with kind (R/W/FETCH), size, address, value, PC and the resolved device + register name.
* Unmapped accesses are recorded too - they are a first-class reversing signal
  (`mmiolog unmapped`).
* Plain RAM accesses are counted; `mmiolog ram on` records them as well.
* The GUI shows them live in the **MMIO log** tab and as `[mmio]` report lines.

```
W32 E0030024 = 0000020F  CMeP.Keyring.KeyringClearFlags        @0005C0DC
R32 E0050024 = 00000000  CMeP.Bigmac.Bigmac status (bit0 busy) @0005CDB6
W32 E0000000 = 00000001  CMeP.Mailbox.MailboxCmepToArm.Status  @0005C5F0
```

---

## Keys

`Core/SceKeys.cs` embeds the key tables ported from `pup_fiction` (`keys.py`, `keys_internal.py`,
`keys_proto.py`, `keys_external.py`, 49 entries with their source line). Verified against
`pup_fiction`'s own output: `psp2swu.self`, `cui_setupper.self` and `kprx_auth_sm.self` decrypt
byte-for-byte identically; 23/23 SDK SELFs and 60/60 firmware `fs\os0` modules extract. Development
(platform `0xC0`) SELF files parse without keys because their metadata is plaintext. Metadata keys
are AES-256, NPDRM/vault keys AES-128 - only the IV is normalised.

---

## Credits / references

* `pup_fiction` - SELF/SRVK/SPKG structures, key tables, PUP extraction (ported to C#).
* Vita Development Wiki - CMeP/F00D, Venezia, Ernie, Kermit memory maps and protocols.
* GNU binutils / CGEN `cpu/mep-c5.cpu` - MeP instruction encodings (validated against a
  `mep-elf` objdump listing: 4154/4162 lines exact, 100 % of the code region).
* GNU binutils `opcodes/rl78-decode.opc` / `rl78-decode.c` and `rl78-dis.c` - RL78 encodings.
* ARM Architecture Reference Manual ARMv7-A (`Docs/armv7-a-r-manual.pdf`), cross-checked against
  capstone 5.0.7 (87 % agreement over 1 048 576 A32 encodings; the remainder are capstone-refused
  or cosmetic differences).
* Boot-ROM static analysis in `..\dumps\bootrom_analysis` - used as the MeP disassembler oracle.
