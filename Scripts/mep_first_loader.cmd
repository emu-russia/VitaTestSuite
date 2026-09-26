# CMeP ("F00D") first_loader / boot ROM sandbox walk.
# Run from the Build folder, e.g.:
#   VitaTestSuite.exe -script ..\Scripts\mep_first_loader.cmd -log mep.log

arch mep
loadfirstloader ..\..\dumps\vita_prototype_bootrom.bin
dis 0x5C000 24
step 200
regs
mmiolog stats
mmiolog tail 40
run 200000
info
regs
keyring
mmiolog stats
mmiolog tail 60
