# Ernie (Renesas RL78 syscon) dump sandbox walk.
# Run from the Build folder, e.g.:
#   VitaTestSuite.exe -script ..\Scripts\rl78_Ernie.cmd -log Ernie.log

arch rl78
loadErnie ..\..\Ernie-master\USS-1001.bin
dis 0x000000 24
step 200
regs
run 200000
info
regs
mmiolog stats
mmiolog tail 60
