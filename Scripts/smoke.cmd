# Smoke test: check that every core is present and reacts to commands.
# Usage: VitaTestSuite.exe -script Scripts\smoke.cmd

arch status
arch mep
mem
arch venezia
arch rl78
mem
arch arm
mem
devices
mmiolog stats
