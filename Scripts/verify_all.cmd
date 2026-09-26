# End-to-end verification of every sandbox.
# Run from the Build folder:
#   VitaTestSuite.exe -script ..\Scripts\verify_all.cmd -log verify_all.log

# ---------------------------------------------------------------- CMeP (F00D)
arch mep
loadfirstloader ..\..\dumps\vita_prototype_bootrom.bin
dis 0x5C000 8
run 200000
info
mmiolog stats
keyring

# ------------------------------------------------- retail CMeP first loader
arch mep
loadfirstloader ..\..\dumps\pch-5c-cold_first_loader.bin
run 200000
info

# ------------------------------------------------- CMeP secure kernel (RAM)
arch mep
load 0x800000 ..\..\Vita_104_Firmware\Out\SLB2_dec\secure_kernel.bin
reset 0x800000
dis 0x800000 6
run 200000
info

# ------------------------------------------------------- Venezia MPE profile
arch venezia
info

# --------------------------------------------------------------- ELF modules
arch arm
nids ..\Docs\nid_db.yml
open ..\..\Vita_104_Firmware\Out\PUP_dec\cui_setupper.elf
info
run 5000
info

# --------------------------------------------------------------- SELF module
arch arm
open "..\..\Sony_Vita_Sdk_0945-YLoD\InstallFiles\[26]\sdk\target\module\libdeflt.suprx"
info
modinfo

# ------------------------------------------------------------ Ernie (RL78)
arch rl78
loadErnie ..\..\Ernie-master\USS-1001.bin
dis 0xE000 12
run 300000
info
mmiolog stats
mmiolog tail 10
