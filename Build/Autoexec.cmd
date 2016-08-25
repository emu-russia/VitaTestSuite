addmem mainmem 0x100000
mapmem 0x80000000 mainmem
mem
load 0x80000000 sample_libatomic.self
dump 0x80000000
