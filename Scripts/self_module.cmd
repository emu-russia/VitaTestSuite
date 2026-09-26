# SELF module loading (decrypt + load + inspect imports/exports).
# Run from the Build folder, e.g.:
#   VitaTestSuite.exe -script ..\Scripts\self_module.cmd -log self.log

arch arm
nids ..\Docs\nid_db.yml
open "..\..\Sony_Vita_Sdk_0945-YLoD\InstallFiles\[26]\sdk\target\module\libdeflt.suprx"
info
devices
