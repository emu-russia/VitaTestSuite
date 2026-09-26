# ARM Cortex-A9 MPCore sandbox walk over a decrypted firmware ELF.
# Run from the Build folder, e.g.:
#   VitaTestSuite.exe -script ..\Scripts\arm_self.cmd -log arm.log

arch arm
open ..\..\Vita_104_Firmware\Out\PUP_dec\cui_setupper.elf
info
regs
dis 0x0 16
step 200
regs
mmiolog stats
mmiolog tail 40
