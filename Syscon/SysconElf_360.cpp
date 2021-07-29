// Decompiling the Syson driver from the 3.60 firmware.

// Additional source: https://wiki.henkaku.xyz/vita/SceSyscon
// IDA Loader: https://github.com/TeamMolecule/sceutils

void SecureMonitorCall()
{
    // Secure Monitor Call
  __asm { SMC.W           #0 }
}

int __fastcall SysconSetCallback(int a1, int a2, int a3)
{
  int v3; // r7
  __int64 v4; // r5
  int v5; // r0

  v3 = a3;
  LODWORD(v4) = a1;
  HIDWORD(v4) = a2;
  v5 = imp_SceCpuForDriver_ksceKernelCpuSuspendIntr(&unk_8100C260);
  *(_QWORD *)((char *)&unk_8100C260 + 8 * v3 - 380) = v4;
  imp_SceCpuForDriver_ksceKernelCpuResumeIntr(&unk_8100C260, v5);
  return 0;
}

int __fastcall sub_8100003C(int a1)
{
  int v1; // r4
  int *v2; // r5
  int v3; // r0
  int v4; // r3
  unsigned __int8 *v5; // r6
  int v6; // t1
  int v7; // r2
  int v8; // r1
  int v9; // r2
  int v10; // r3
  void (__fastcall *v11)(_DWORD, signed int, int); // r5
  signed int v13; // r3
  int v14; // r7
  int v15; // r12
  unsigned __int8 *v16; // r3
  int v17; // lr
  int v18; // r0
  int v19; // r1
  int v20; // r3
  int v21; // r0
  bool v22; // nf
  unsigned __int8 v23; // vf
  int v24; // ST08_4
  int v25; // ST0C_4
  int v26; // ST10_4
  int v27; // ST14_4
  int v28; // ST18_4
  int v29; // ST20_4
  int v30; // ST1C_4
  int v31; // ST24_4
  int v32; // ST34_4
  int v33; // ST38_4
  int v34; // ST3C_4
  int v35; // ST40_4
  int v36; // ST28_4
  int v37; // ST2C_4
  int v38; // ST30_4
  int v39; // ST8C_4
  int v40; // ST84_4
  int v41; // ST44_4
  int v42; // ST48_4
  int v43; // ST4C_4
  int v44; // ST88_4
  int v45; // ST80_4
  int v46; // ST54_4
  int v47; // ST58_4
  int v48; // ST5C_4
  int v49; // ST60_4
  int v50; // ST70_4
  int v51; // ST7C_4
  int v52; // ST6C_4
  int v53; // ST78_4

  v1 = a1;
  v2 = (int *)dword_8100C098;
  v3 = imp_SceThreadmgrForDriver_ksceKernelGetSystemTimeLow();
  v4 = dword_8100C094;
  *(_DWORD *)(v1 + 104) = v3;
  if ( v4 )
    (*(void (__fastcall **)(int))(v4 + 4))(v1);
  if ( *(_DWORD *)(v1 + 4) << 20 >= 0 )
    v5 = (unsigned __int8 *)(v1 + 16);
  else
    v5 = *(unsigned __int8 **)(v1 + 80);
  imp_SceGpioForDriver_ksceGpioPortClear(0, 3);
  for ( ; v2[10]; v6 = *v2 )
    ;
  v7 = *(_DWORD *)(v1 + 4);
  v8 = v2[11];
  v2[9] = 1536;
  v9 = v7 & 0x40000;
  if ( v9 )
  {
    v2[1] = *v5 | (v5[1] << 8);
    v2[1] = (unsigned __int16)(~(v5[1] + *v5 + 1) << 8) | 1;
  }
  else
  {
    v13 = v5[2];
    v14 = v13 + 2;
    v15 = v13 - 27;
    if ( v13 <= 29 )
    {
      v19 = 2;
    }
    else
    {
      v16 = v5;
      do
      {
        v17 = v16[1];
        v16 += 32;
        v18 = v5[v9];
        __pld(&v5[v9 + 39]);
        v9 += 32;
        v19 = v9 + 2;
        v2[1] = v18 | (v17 << 8);
        v2[1] = *(v16 - 30) | (*(v16 - 29) << 8);
        v2[1] = *(v16 - 28) | (*(v16 - 27) << 8);
        v2[1] = *(v16 - 26) | (*(v16 - 25) << 8);
        v2[1] = *(v16 - 24) | (*(v16 - 23) << 8);
        v2[1] = *(v16 - 22) | (*(v16 - 21) << 8);
        v2[1] = *(v16 - 20) | (*(v16 - 19) << 8);
        v2[1] = *(v16 - 18) | (*(v16 - 17) << 8);
        v2[1] = *(v16 - 16) | (*(v16 - 15) << 8);
        v2[1] = *(v16 - 14) | (*(v16 - 13) << 8);
        v2[1] = *(v16 - 12) | (*(v16 - 11) << 8);
        v2[1] = *(v16 - 10) | (*(v16 - 9) << 8);
        v2[1] = *(v16 - 8) | (*(v16 - 7) << 8);
        v2[1] = *(v16 - 6) | (*(v16 - 5) << 8);
        v2[1] = *(v16 - 4) | (*(v16 - 3) << 8);
        v2[1] = *(v16 - 2) | (*(v16 - 1) << 8);
      }
      while ( v15 > v9 + 2 );
    }
    do
    {
      v20 = v5[v9];
      v21 = v5[v9 + 1];
      v23 = __OFSUB__(v14, v19);
      v22 = v14 - v19 < 0;
      v9 = v19;
      v19 += 2;
      v2[1] = v20 | (v21 << 8);
    }
    while ( !(v22 ^ v23) );
  }
  v2[2] = 0;
  v2[4] = 1;
  v10 = v2[4];
  __dsb(0xFu);
  imp_SceGpioForDriver_ksceGpioPortSet(0, 3);
  v11 = (void (__fastcall *)(_DWORD, signed int, int))dword_8100C278;
  *(_DWORD *)(v1 + 4) |= 0x40020000u;
  if ( v11 )
  {
    v24 = *(unsigned __int8 *)(v1 + 18);
    v25 = *(unsigned __int8 *)(v1 + 19);
    v26 = *(unsigned __int8 *)(v1 + 20);
    v27 = *(unsigned __int8 *)(v1 + 21);
    v28 = *(unsigned __int8 *)(v1 + 22);
    v29 = *(unsigned __int8 *)(v1 + 24);
    v30 = *(unsigned __int8 *)(v1 + 23);
    v31 = *(unsigned __int8 *)(v1 + 25);
    v32 = *(unsigned __int8 *)(v1 + 29);
    v33 = *(unsigned __int8 *)(v1 + 30);
    v34 = *(unsigned __int8 *)(v1 + 31);
    v35 = *(unsigned __int8 *)(v1 + 32);
    v36 = *(unsigned __int8 *)(v1 + 26);
    v37 = *(unsigned __int8 *)(v1 + 27);
    v38 = *(unsigned __int8 *)(v1 + 28);
    v39 = *(unsigned __int8 *)(v1 + 36);
    v40 = *(unsigned __int8 *)(v1 + 41);
    v41 = *(unsigned __int8 *)(v1 + 33);
    v42 = *(unsigned __int8 *)(v1 + 34);
    v43 = *(unsigned __int8 *)(v1 + 35);
    v44 = *(unsigned __int8 *)(v1 + 42);
    v45 = *(unsigned __int8 *)(v1 + 45);
    v46 = *(unsigned __int8 *)(v1 + 37);
    v47 = *(unsigned __int8 *)(v1 + 38);
    v48 = *(unsigned __int8 *)(v1 + 39);
    v49 = *(unsigned __int8 *)(v1 + 40);
    v50 = *(unsigned __int8 *)(v1 + 44);
    v51 = *(unsigned __int8 *)(v1 + 47);
    v52 = *(unsigned __int8 *)(v1 + 43);
    v53 = *(unsigned __int8 *)(v1 + 46);
    v11(0, 34, *(unsigned __int8 *)(v1 + 16) | (*(unsigned __int8 *)(v1 + 17) << 8));
  }
  return 0;
}

signed int sub_810002FC()
{
  int v0; // r4
  int v1; // r0
  signed __int64 v2; // r2
  _BYTE *v3; // r7
  char v4; // lr
  int v5; // r7
  int v6; // lr
  signed int v7; // r7
  unsigned int v8; // r0
  int v9; // r3
  unsigned int v10; // r0
  char v11; // lr
  char *v12; // r2
  int v13; // r7
  char v14; // r6
  _BYTE *v15; // r2
  char v16; // r0
  int v17; // r6
  int v18; // r1
  signed int result; // r0
  int v20; // [sp+0h] [bp-20h]
  int v21; // [sp+4h] [bp-1Ch]

  v21 = MEMORY[0];
  while ( sub_810036B4(&dword_8100C224) < 0 )
    ;
  while ( sub_810036C0(&byte_8100C23C) < 0 )
    ;
  LOWORD(v0) = -16384;
  HIWORD(v0) = (unsigned int)dword_8100C000 >> 16;
  v20 = 0;
  if ( *(_BYTE *)(v0 + 0x23C) )
  {
    v1 = *(char *)(v0 + 0x23C) - 48;
    v2 = v1;
    if ( *(_BYTE *)(v0 + 0x23D) )
    {
      v2 = 10LL * v1 + *(char *)(v0 + 0x23D) - 48;
      if ( *(_BYTE *)(v0 + 0x23E) )
      {
        v2 = 10 * v2 + *(char *)(v0 + 0x23E) - 48;
        if ( *(_BYTE *)(v0 + 0x23F) )
        {
          v2 = 10 * v2 + *(char *)(v0 + 0x23F) - 48;
          if ( *(_BYTE *)(v0 + 0x240) )
          {
            v2 = 10 * v2 + *(char *)(v0 + 0x240) - 48;
            if ( *(_BYTE *)(v0 + 0x241) )
            {
              v2 = 10 * v2 + *(char *)(v0 + 0x241) - 48;
              if ( *(_BYTE *)(v0 + 0x242) )
              {
                v2 = 10 * v2 + *(char *)(v0 + 0x242) - 48;
                if ( *(_BYTE *)(v0 + 0x243) )
                {
                  v2 = 10 * v2 + *(char *)(v0 + 0x243) - 48;
                  if ( *(_BYTE *)(v0 + 0x244) )
                  {
                    LOWORD(v3) = -16384;
                    HIWORD(v3) = (unsigned int)dword_8100C000 >> 16;
                    v2 = 10 * v2 + *(char *)(v0 + 0x244) - 48;
                    if ( *(_BYTE *)(v0 + 0x245) )
                    {
                      v2 = 10 * v2 + *(char *)(v0 + 0x245) - 48;
                      if ( v3[582] )
                      {
                        v2 = 10 * v2 + (char)v3[582] - 48;
                        if ( v3[583] )
                        {
                          v2 = 10 * v2 + (char)v3[583] - 48;
                          if ( v3[584] )
                          {
                            v4 = v3[585];
                            v2 = 10 * v2 + (char)v3[584] - 48;
                            if ( v3[585] )
                            {
                              v5 = (unsigned __int8)v3[586];
                              v2 = 10 * v2 + v4 - 48;
                              if ( v5 )
                              {
                                v6 = (char)v5;
                                v7 = 15;
                                v2 = 10 * v2 + v6 - 48;
                              }
                              else
                              {
                                v7 = 14;
                              }
                            }
                            else
                            {
                              v7 = 13;
                            }
                          }
                          else
                          {
                            v7 = 12;
                          }
                        }
                        else
                        {
                          v7 = 11;
                        }
                      }
                      else
                      {
                        v7 = 10;
                      }
                    }
                    else
                    {
                      v7 = 9;
                    }
                  }
                  else
                  {
                    v7 = 8;
                  }
                }
                else
                {
                  v7 = 7;
                }
              }
              else
              {
                v7 = 6;
              }
            }
            else
            {
              v7 = 5;
            }
          }
          else
          {
            v7 = 4;
          }
        }
        else
        {
          v7 = 3;
        }
      }
      else
      {
        v7 = 2;
      }
    }
    else
    {
      v7 = 1;
    }
    v20 = v7;
  }
  else
  {
    v2 = 0LL;
  }
  v8 = *(_DWORD *)(v0 + 0x224);
  *(_QWORD *)(v0 + 592) = v2;
  if ( v8 > 0x1000003 )
  {
    while ( sub_8100371C(18) < 0 && sub_8100371C(18) < 0 )
      ;
  }
  else if ( v8 > 0x70501 )
  {
    while ( sub_8100371C(2) < 0 && sub_8100371C(2) < 0 )
      ;
  }
  if ( !(*(_BYTE *)(v0 + 0xBC) & 0x40) )
  {
    while ( sub_81003C7C(&v20) < 0 )
      ;
    if ( *(_DWORD *)(v0 + 0x228) & 8 )
    {
      while ( sub_81003C5C(&v20) < 0 )
        ;
    }
  }
  *(_BYTE *)(v0 + 0xC7) = -1;
  *(_BYTE *)(v0 + 0xC8) = -1;
  while ( sub_810036A8(&v20) < 0 )
    ;
  v9 = 0;
  v10 = v20;
  for ( *(_DWORD *)(v0 + 0xA8) = v20; ; v10 = *(_DWORD *)(v0 + 0xA8) )
  {
    v11 = byte_81009584[v9];
    v12 = &byte_81009584[v9];
    v13 = (unsigned __int8)byte_81009584[v9 + 1];
    v9 += 8;
    v14 = v12[2];
    v15 = (_BYTE *)*((_DWORD *)v12 + 1);
    v16 = (v10 >> 8 * v11) & v13 ? v14 ^ 1 : v14;
    *v15 = v16;
    if ( v9 == 200 )
      break;
  }
  v17 = v21;
  v18 = MEMORY[0];
  *(_BYTE *)(v0 + 0xE0) = 1;
  result = 1;
  *(_DWORD *)(v0 + 0x270) = 1;
  *(_BYTE *)(v0 + 0xC7) = -1;
  *(_BYTE *)(v0 + 0xC8) = -1;
  if ( v17 != v18 )
    imp_SceSysclibForDriver___stack_chk_fail(1);
  return result;
}

signed int sub_8100064C()
{
  int v0; // r0
  int v1; // r4
  int v2; // r0
  __int64 v3; // r10
  int v4; // r0
  int v5; // r0
  int v6; // r0
  unsigned int v7; // r8
  int v8; // r2
  int v9; // r3
  int *v10; // r0
  unsigned __int8 *v11; // r5
  unsigned int v12; // r6
  unsigned int v13; // r2
  signed int v14; // r7
  int v15; // r3
  int v16; // r1
  int v17; // r3
  void (__fastcall **v18)(signed int, signed int, signed int); // r7
  void (__fastcall *v19)(signed int, signed int, signed int); // r7
  int v20; // r3
  unsigned int v21; // r1
  unsigned int v22; // r2
  int v23; // r3
  unsigned int v24; // r12
  signed int v25; // r1
  unsigned int v26; // lr
  uint8x8_t *v27; // r12
  int32x4_t v28; // q8
  int v29; // r6
  unsigned int v30; // r7
  uint16x8_t v31; // q12
  uint16x8_t v32; // q11
  uint16x8_t v33; // q10
  uint16x8_t v34; // q9
  uint8x8_t v35; // d18
  uint8x8_t v36; // d19
  uint16x8_t v37; // q10
  uint16x8_t v38; // q9
  int32x2_t v39; // d16
  int v40; // r6
  unsigned int v41; // r1
  unsigned __int8 v42; // r6
  bool v43; // cf
  bool v44; // zf
  int v45; // r7
  int v46; // r6
  int v47; // r1
  int v48; // r2
  int v49; // r5
  char v50; // r7
  void (__fastcall *v51)(int, _DWORD); // r6
  int v52; // r3
  int v53; // r8
  int v54; // r2
  unsigned __int8 *v55; // r1
  int v56; // r3
  int v57; // r7
  int v58; // r6
  int v59; // lr
  _BYTE *v60; // r2
  _BYTE *v61; // r5
  int v62; // r12
  unsigned __int8 v63; // r9
  int v64; // lr
  BOOL v65; // r4
  int v66; // r4
  int *v67; // r9
  int v68; // r11
  signed int v69; // r4
  int v70; // r3
  int v71; // r12
  int v72; // r1
  unsigned int v73; // r3
  int v74; // r7
  int v75; // r6
  unsigned int v76; // r2
  int v77; // r2
  int v78; // r3
  int v79; // r1
  int v80; // r2
  int v81; // r3
  int v82; // r3
  signed int v83; // r6
  signed int v84; // r7
  int v85; // r0
  int v86; // r3
  _DWORD *v87; // r2
  int v88; // r1
  int *v89; // r3
  int *v90; // r2
  int v91; // r1
  int *v92; // r2
  int *v93; // r8
  unsigned int v94; // r5
  int v95; // r0
  int v97; // r3
  int v98; // r0
  void (__fastcall *v99)(int, _DWORD); // r6
  _DWORD *v100; // r7
  int v101; // r8
  void (__fastcall *v102)(signed int, signed int, signed int); // r3
  int v103; // r5
  int v104; // r1
  int v105; // r2
  signed int v106; // lr
  unsigned int v107; // r12
  signed int v108; // r1
  int v109; // r3
  int v110; // r2
  int v111; // r5
  int v112; // r3
  int v113; // r0
  void (__fastcall *v114)(int, _DWORD); // r6
  _DWORD *v115; // r7
  int v116; // r8
  unsigned int v117; // r7
  int v118; // r12
  int v119; // r7
  int v120; // r3
  int v121; // r2
  int v122; // r1
  unsigned int v123; // lr
  int v124; // r2
  int v125; // r1
  signed int v126; // r2
  int v127; // r3
  int v128; // r7
  int v129; // r2
  int v130; // r6
  unsigned int v131; // r1
  BOOL v132; // lr
  int v133; // r7
  unsigned int v134; // r1
  BOOL v135; // r1
  int v136; // lr
  int v137; // r1
  unsigned int v138; // r6
  BOOL v139; // r6
  int v140; // lr
  unsigned int v141; // r3
  BOOL v142; // r1
  int v143; // r6
  void (__fastcall *v144)(int, _DWORD); // r6
  int v145; // r8
  void (__fastcall *v146)(signed int, _DWORD); // r5
  void (__fastcall *v147)(unsigned int, _DWORD); // r6
  unsigned int v148; // r5
  char v149; // r3
  void (__fastcall *v150)(int, _DWORD); // r9
  int v151; // r8
  void (__fastcall *v152)(int, _DWORD); // r9
  int v153; // r8
  void (__fastcall *v154)(int, _DWORD); // r9
  int v155; // r8
  int v156; // ST88_4
  void (__fastcall **v157)(signed int, signed int, signed int); // [sp+8Ch] [bp-44h]
  int v158; // [sp+90h] [bp-40h]
  bool v159; // [sp+90h] [bp-40h]
  int v160; // [sp+94h] [bp-3Ch]
  int v161; // [sp+98h] [bp-38h]
  int v162; // [sp+9Ch] [bp-34h]
  __int64 v163; // [sp+9Ch] [bp-34h]
  void (__fastcall *v164)(int, _DWORD); // [sp+A4h] [bp-2Ch]

  LOWORD(v0) = -15776;
  LOWORD(v1) = -16384;
  HIWORD(v1) = (unsigned int)dword_8100C000 >> 16;
  HIWORD(v0) = (unsigned int)&unk_8100C260 >> 16;
  v2 = imp_SceCpuForDriver_ksceKernelCpuSuspendIntr(v0);
  LODWORD(v3) = *(_DWORD *)v1;
  *(_BYTE *)(v1 + 0xBE) = 1;
  v161 = v2;
  v4 = *(_DWORD *)(v3 + 4);
  *(_DWORD *)v1 = 0;
  if ( !(v4 & 0x200) )
    imp_SceGpioForDriver_ksceGpioPortClear(0, 3);
  v5 = imp_SceThreadmgrForDriver_ksceKernelGetSystemTimeLow();
  HIDWORD(v3) = v5;
  *(_DWORD *)(v1 + 0x9C) = v5;
  imp_SceGpioForDriver_ksceGpioAcquireIntr(0, 4);
  LOWORD(v6) = -15776;
  v7 = 0;
  HIWORD(v6) = (unsigned int)&unk_8100C260 >> 16;
  imp_SceCpuForDriver_ksceKernelCpuUnlockStoreLR(v6);
  v9 = *(_DWORD *)(v3 + 4);
  v10 = *(int **)(v1 + 0x98);
  if ( v9 << 21 >= 0 )
  {
    v11 = (unsigned __int8 *)(v3 + 48);
  }
  else
  {
    v8 = *(_QWORD *)(v3 + 84) >> 32;
    v11 = (unsigned __int8 *)*(_QWORD *)(v3 + 84);
  }
  if ( *(_DWORD *)(v3 + 4) << 21 >= 0 )
    v12 = 32;
  else
    v12 = *(_DWORD *)(v3 + 92);
  if ( *(_DWORD *)(v3 + 4) << 21 < 0 )
  {
    v11 += v12;
    v12 = v8 - v12;
  }
  if ( !v10[10] )
  {
    v11[2] = 0;
    *v11 = -1;
    v7 = -1;
    v11[1] = -1;
    v9 = *(_DWORD *)(v3 + 4) | 0x100000;
    *(_DWORD *)(v3 + 4) = v9;
  }
  if ( v10[11] )
  {
    v7 = -1;
    v9 |= 0x200000u;
    *(_DWORD *)(v3 + 4) = v9;
  }
  if ( v10[9] << 22 < 0 )
  {
    v10[9] = 512;
    v9 |= 0x400000u;
    *(_DWORD *)(v3 + 4) = v9;
  }
  if ( v12 > 1 && v10[10] )
  {
    v13 = 3;
    v14 = 1;
    v15 = 0;
    do
    {
      v16 = *v10;
      if ( !v15 )
        v7 = (unsigned __int8)v16;
      v11[v15] = v16;
      v43 = v12 >= v13;
      v44 = v12 == v13;
      v15 += 2;
      v11[v14] = BYTE1(v16);
      v14 = v13;
      v13 += 2;
    }
    while ( !v44 && v43 && v10[10] );
    v9 = *(_DWORD *)(v3 + 4);
  }
  if ( v9 << 21 < 0 )
    *(_DWORD *)(v3 + 48) = *(_DWORD *)v11;
  v10[4] = 0;
  v17 = v10[4];
  __dsb(0xFu);
  LOWORD(v18) = -15752;
  HIWORD(v18) = (unsigned int)&dword_8100C278 >> 16;
  v157 = v18;
  imp_SceGpioForDriver_ksceGpioPortClear(0, 3);
  v19 = *v18;
  if ( v19 )
    ((void (__fastcall *)(signed int, signed int, int, _DWORD, _DWORD, _DWORD, _DWORD, _DWORD, _DWORD, _DWORD, _DWORD, _DWORD, _DWORD, _DWORD, _DWORD, _DWORD, _DWORD, _DWORD, _DWORD, _DWORD, _DWORD, _DWORD, _DWORD, _DWORD, _DWORD, _DWORD, _DWORD, _DWORD, _DWORD, _DWORD, _DWORD, _DWORD, _DWORD, _DWORD, _DWORD, _DWORD))v19)(
      1,
      34,
      *(unsigned __int8 *)(v3 + 16) | (*(unsigned __int8 *)(v3 + 17) << 8),
      *(_DWORD *)(v3 + 4),
      *v11,
      v11[1],
      v11[2],
      v11[3],
      v11[4],
      v11[5],
      v11[6],
      v11[7],
      v11[8],
      v11[9],
      v11[10],
      v11[11],
      v11[12],
      v11[13],
      v11[14],
      v11[15],
      v11[16],
      v11[17],
      v11[18],
      v11[19],
      v11[20],
      v11[21],
      v11[22],
      v11[23],
      v11[24],
      v11[25],
      v11[26],
      v11[27],
      v11[28],
      v11[29],
      v11[30],
      v11[31]);
  v20 = *(_DWORD *)(v1 + 0x94);
  if ( v20 )
    (*(void (__fastcall **)(_DWORD))(v20 + 8))(v3);
  if ( v7 == -1 )
    goto LABEL_134;
  v21 = v11[2];
  if ( v21 <= 1 || v12 < v21 + 3 )
    goto LABEL_134;
  v22 = v21 + 2;
  if ( v21 + 2 >= (-((unsigned __int8)v11 & 7) & 0xFu) )
    v23 = -((unsigned __int8)v11 & 7) & 0xF;
  else
    v23 = v21 + 2;
  if ( v22 > 0x11 )
  {
    v24 = v23;
    if ( !v23 )
    {
      v25 = v23;
      goto LABEL_58;
    }
  }
  else
  {
    v24 = v21 + 2;
  }
  v23 = *v11;
  if ( v24 <= 1 )
  {
    v25 = 1;
  }
  else
  {
    v23 += v11[1];
    if ( v24 <= 2 )
    {
      v25 = 2;
    }
    else
    {
      v23 += v21;
      if ( v24 <= 3 )
      {
        v25 = 3;
      }
      else
      {
        v23 += v11[3];
        if ( v24 <= 4 )
        {
          v25 = 4;
        }
        else
        {
          v23 += v11[4];
          if ( v24 <= 5 )
          {
            v25 = 5;
          }
          else
          {
            v23 += v11[5];
            if ( v24 <= 6 )
            {
              v25 = 6;
            }
            else
            {
              v23 += v11[6];
              if ( v24 <= 7 )
              {
                v25 = 7;
              }
              else
              {
                v23 += v11[7];
                if ( v24 <= 8 )
                {
                  v25 = 8;
                }
                else
                {
                  v23 += v11[8];
                  if ( v24 <= 9 )
                  {
                    v25 = 9;
                  }
                  else
                  {
                    v23 += v11[9];
                    if ( v24 <= 0xA )
                    {
                      v25 = 10;
                    }
                    else
                    {
                      v23 += v11[10];
                      if ( v24 <= 0xB )
                      {
                        v25 = 11;
                      }
                      else
                      {
                        v23 += v11[11];
                        if ( v24 <= 0xC )
                        {
                          v25 = 12;
                        }
                        else
                        {
                          v23 += v11[12];
                          if ( v24 <= 0xD )
                          {
                            v25 = 13;
                          }
                          else
                          {
                            v23 += v11[13];
                            if ( v24 <= 0xE )
                            {
                              v25 = 14;
                            }
                            else
                            {
                              v23 += v11[14];
                              if ( v24 <= 0xF )
                              {
                                v25 = 15;
                              }
                              else
                              {
                                v23 += v11[15];
                                if ( v24 <= 0x10 )
                                {
                                  v25 = 16;
                                }
                                else
                                {
                                  v25 = 17;
                                  v23 += v11[16];
                                }
                              }
                            }
                          }
                        }
                      }
                    }
                  }
                }
              }
            }
          }
        }
      }
    }
    if ( v22 == v24 )
      goto LABEL_81;
  }
LABEL_58:
  v26 = (v22 - v24) >> 4;
  v158 = v22 - v24;
  v160 = 16 * v26;
  if ( !v26 )
    goto LABEL_348;
  v27 = (uint8x8_t *)&v11[v24];
  if ( v26 <= 2 )
  {
    v28.n128_u64[0] = 0LL;
    v28.n128_u64[1] = 0LL;
    v30 = 0;
  }
  else
  {
    v28.n128_u64[0] = 0LL;
    v28.n128_u64[1] = 0LL;
    v29 = (int)&v27[4];
    v30 = 0;
    do
    {
      v31 = vmovl_u8(*(uint8x8_t *)(v29 - 32));
      v32 = vmovl_u8(*(uint8x8_t *)(v29 - 24));
      v33 = vmovl_u8(*(uint8x8_t *)(v29 - 16));
      v30 += 2;
      v27 = (uint8x8_t *)v29;
      v34 = vmovl_u8(*(uint8x8_t *)(v29 - 8));
      __pld((void *)(v29 + 192));
      v29 += 32;
      v28 = vaddq_s32(
              vmovl_u16(*(uint16x4_t *)&v34.n128_i8[8]),
              vaddq_s32(
                vmovl_u16((uint16x4_t)v34.n128_u64[0]),
                vaddq_s32(
                  vmovl_u16(*(uint16x4_t *)&v33.n128_i8[8]),
                  vaddq_s32(
                    vaddq_s32(
                      vmovl_u16(*(uint16x4_t *)&v32.n128_i8[8]),
                      vaddq_s32(
                        vmovl_u16((uint16x4_t)v32.n128_u64[0]),
                        vaddq_s32(
                          vmovl_u16(*(uint16x4_t *)&v31.n128_i8[8]),
                          vaddq_s32(vmovl_u16((uint16x4_t)v31.n128_u64[0]), v28)))),
                    vmovl_u16((uint16x4_t)v33.n128_u64[0])))));
    }
    while ( v30 != ((v26 - 3) & 0xFFFFFFFE) + 2 );
  }
  do
  {
    v35.n64_u64[0] = v27->n64_u64[0];
    v36.n64_u64[0] = v27[1].n64_u64[0];
    v27 += 2;
    v37 = vmovl_u8(v35);
    v38 = vmovl_u8(v36);
    ++v30;
    v28 = vaddq_s32(
            vmovl_u16(*(uint16x4_t *)&v38.n128_i8[8]),
            vaddq_s32(
              vmovl_u16((uint16x4_t)v38.n128_u64[0]),
              vaddq_s32(
                vmovl_u16(*(uint16x4_t *)&v37.n128_i8[8]),
                vaddq_s32(vmovl_u16((uint16x4_t)v37.n128_u64[0]), v28))));
  }
  while ( v30 < v26 );
  v39.n64_u64[0] = vadd_s32((int32x2_t)v28.n128_u64[0], *(int32x2_t *)&v28.n128_i8[8]).n64_u64[0];
  v25 += v160;
  v23 += vpadd_s32(v39, v39).n64_u64[0];
  if ( v160 != v158 )
  {
LABEL_348:
    LOBYTE(v23) = v23 + v11[v25];
    if ( v22 > v25 + 1 )
    {
      LOBYTE(v23) = v23 + v11[v25 + 1];
      if ( v22 > v25 + 2 )
      {
        LOBYTE(v23) = v23 + v11[v25 + 2];
        if ( v22 > v25 + 3 )
        {
          LOBYTE(v23) = v23 + v11[v25 + 3];
          if ( v22 > v25 + 4 )
          {
            LOBYTE(v23) = v23 + v11[v25 + 4];
            if ( v22 > v25 + 5 )
            {
              LOBYTE(v23) = v23 + v11[v25 + 5];
              if ( v22 > v25 + 6 )
              {
                LOBYTE(v23) = v23 + v11[v25 + 6];
                if ( v22 > v25 + 7 )
                {
                  LOBYTE(v23) = v23 + v11[v25 + 7];
                  if ( v22 > v25 + 8 )
                  {
                    LOBYTE(v23) = v23 + v11[v25 + 8];
                    if ( v22 > v25 + 9 )
                    {
                      LOBYTE(v23) = v23 + v11[v25 + 9];
                      if ( v22 > v25 + 10 )
                      {
                        LOBYTE(v23) = v23 + v11[v25 + 10];
                        if ( v22 > v25 + 11 )
                        {
                          LOBYTE(v23) = v23 + v11[v25 + 11];
                          if ( v22 > v25 + 12 )
                          {
                            v40 = v25 + 13;
                            LOBYTE(v23) = v23 + v11[v25 + 12];
                            if ( v22 > v25 + 13 )
                            {
                              v41 = v25 + 14;
                              v42 = v11[v40];
                              v43 = v22 >= v41;
                              v44 = v22 == v41;
                              if ( v22 > v41 )
                                LOBYTE(v41) = v11[v41];
                              LOBYTE(v23) = v23 + v42;
                              if ( !v44 && v43 )
                                LOBYTE(v23) = v23 + v41;
                            }
                          }
                        }
                      }
                    }
                  }
                }
              }
            }
          }
        }
      }
    }
  }
LABEL_81:
  if ( v11[v22] != (unsigned __int8)~(_BYTE)v23 )
  {
LABEL_134:
    LOBYTE(v49) = 0;
    v159 = 0;
    *(_DWORD *)(v3 + 4) = *(_DWORD *)(v3 + 4) & 0xFFFDFFFF | 0x800000;
    goto LABEL_135;
  }
  LOWORD(v45) = -16384;
  *(_DWORD *)(v3 + 4) &= 0xFFFDFFFF;
  *(_BYTE *)(v1 + 0xBD) = v7;
  v46 = *(unsigned __int8 *)(v1 + 0xBC);
  *(_BYTE *)(v1 + 0xBC) = v7;
  v47 = *(unsigned __int8 *)(v3 + 16);
  v48 = *(unsigned __int8 *)(v3 + 17) << 8;
  if ( v7 & 4 )
  {
    HIWORD(v45) = (unsigned int)dword_8100C000 >> 16;
    if ( *(_DWORD *)(v45 + 0x258) )
    {
      LOBYTE(v49) = 0;
    }
    else
    {
      v49 = (v48 | v47) - 4;
      if ( (v48 | v47) != 4 )
        LOBYTE(v49) = 1;
    }
  }
  else
  {
    LOBYTE(v49) = v7 & 4;
  }
  v50 = v7 & 0x20;
  if ( v7 & 0x20 )
  {
    if ( !*(_DWORD *)(v1 + 0x258) )
    {
      v159 = byte_8100C0C1 == 0;
      if ( !((v7 ^ v46) & 8) )
        goto LABEL_115;
      goto LABEL_87;
    }
    v50 = 0;
  }
  v159 = v50;
  if ( !((v7 ^ v46) & 8) )
  {
LABEL_115:
    v54 = v48 | v47;
    if ( v54 < 258 )
      goto LABEL_93;
    goto LABEL_116;
  }
LABEL_87:
  v51 = *(void (__fastcall **)(int, _DWORD))(v1 + 0x204);
  LOWORD(v52) = -16384;
  v53 = (v7 >> 3) & 1;
  if ( !v51 )
    goto LABEL_115;
  if ( *v157 )
  {
    HIWORD(v52) = (unsigned int)dword_8100C000 >> 16;
    ((void (__fastcall *)(signed int, signed int, signed int, int, _DWORD, _DWORD))*v157)(
      5,
      4,
      36,
      v53,
      *(_DWORD *)(v1 + 0x204),
      *(_DWORD *)(v52 + 0x208));
  }
  v51(v53, *(_DWORD *)(v1 + 0x208));
  if ( *v157 )
    (*v157)(6, 1, 36);
  v54 = (*(unsigned __int8 *)(v3 + 17) << 8) | *(unsigned __int8 *)(v3 + 16);
  if ( v54 < 258 )
  {
LABEL_93:
    if ( v54 < 256 )
    {
      if ( (unsigned int)(v54 - 3) <= 1 )
      {
        v55 = (unsigned __int8 *)(v3 + 48);
        if ( *(_DWORD *)(v3 + 4) & 0x400 )
        {
          if ( *(_DWORD *)(v3 + 92) )
            goto LABEL_122;
          v55 = *(unsigned __int8 **)(v3 + 84);
        }
        v56 = 0;
        LOWORD(v57) = -27260;
        HIWORD(v57) = (unsigned int)byte_81009584 >> 16;
        v58 = 0;
        v162 = v1;
        *(_DWORD *)(v1 + 0xA8) = (v55[6] << 16) | (v55[7] << 24) | v55[4] | (v55[5] << 8);
        do
        {
          v59 = *(unsigned __int8 *)(v57 + v56);
          v60 = (_BYTE *)(v57 + v56);
          v61 = *(_BYTE **)(v57 + v56 + 4);
          v56 += 8;
          v62 = (unsigned __int8)v60[2];
          v63 = v55[v59 + 4];
          v64 = (char)*v61;
          if ( v63 & v60[1] )
            v62 ^= 1u;
          *v61 = v62;
          if ( v64 == v62 )
            v65 = (unsigned int)(v64 + 1) <= 0;
          else
            v65 = 1;
          if ( v65 )
            v58 |= 1 << v60[3];
        }
        while ( v56 != 200 );
        v66 = v162;
        v163 = v3;
        v67 = &dword_8100958C[5];
        v68 = v66;
        do
        {
          v69 = *((unsigned __int8 *)v67 - 25);
          if ( (1 << v69) & v58 )
          {
            v70 = v68 + 8 * (v69 + 28);
            __pld(v67);
            v71 = *(char *)*(v67 - 6);
            v164 = *(void (__fastcall **)(int, _DWORD))(v70 + 4);
            if ( v164 )
            {
              if ( *v157 )
              {
                v156 = *(char *)*(v67 - 6);
                ((void (__fastcall *)(signed int, signed int, signed int, int, void (__fastcall *)(int, _DWORD), _DWORD))*v157)(
                  5,
                  4,
                  v69,
                  v71,
                  v164,
                  *(_DWORD *)(v70 + 8));
                v71 = v156;
              }
              v164(v71, *(_DWORD *)(v68 + 8 * (v69 + 28) + 8));
              if ( *v157 )
                (*v157)(6, 1, v69);
            }
          }
          v67 += 2;
        }
        while ( v67 != &dword_8100958C[55] );
        v1 = v68;
        v3 = v163;
        if ( v58 & 0x20 )
        {
          if ( *(_BYTE *)(v1 + 199) )
          {
            if ( !byte_8100C0E0 )
              byte_8100C0E0 = 1;
          }
          else
          {
            v146 = *(void (__fastcall **)(signed int, _DWORD))(v1 + 524);
            if ( byte_8100C0E0 == 1 )
              byte_8100C0E0 = 0;
            *(_DWORD *)(v1 + 172) = 327424;
            if ( v146 )
            {
              if ( *v157 )
                (*v157)(5, 4, 37);
              v146(327424, *(_DWORD *)(v1 + 528));
              if ( *v157 )
                (*v157)(6, 1, 37);
            }
          }
        }
        if ( !(v58 & 0x4000) )
          goto LABEL_283;
        if ( !*(_BYTE *)(v1 + 204) )
        {
          v147 = *(void (__fastcall **)(unsigned int, _DWORD))(v1 + 532);
          v148 = *(_DWORD *)(v1 + 176) & 0xFF0000FF;
          if ( byte_8100C0DF == 1 )
            byte_8100C0DF = *(_BYTE *)(v1 + 204);
          *(_DWORD *)(v1 + 176) = v148;
          if ( v147 )
          {
            if ( *v157 )
              ((void (__fastcall *)(signed int, signed int, signed int, unsigned int, void (__fastcall *)(unsigned int, _DWORD), int))*v157)(
                5,
                4,
                38,
                v148,
                v147,
                dword_8100C218);
            v147(v148, *(_DWORD *)(v1 + 536));
            v102 = *v157;
            if ( *v157 )
            {
              LOBYTE(v49) = 0;
LABEL_281:
              v102(6, 1, 38);
              goto LABEL_122;
            }
          }
LABEL_283:
          LOBYTE(v49) = 0;
          goto LABEL_122;
        }
        LOBYTE(v49) = byte_8100C0DF;
        if ( byte_8100C0DF )
          goto LABEL_283;
        goto LABEL_121;
      }
      goto LABEL_122;
    }
LABEL_240:
    if ( *(_DWORD *)(v3 + 4) & 0x400 )
    {
      if ( *(_DWORD *)(v3 + 92) )
        goto LABEL_122;
      v127 = *(_DWORD *)(v3 + 84);
    }
    else
    {
      v127 = v3 + 48;
    }
    v128 = *(char *)(v1 + 0xDB);
    LOWORD(v129) = -16384;
    HIWORD(v129) = (unsigned int)dword_8100C000 >> 16;
    v130 = *(char *)(v1 + 0xDC);
    v131 = ((unsigned int)*(unsigned __int8 *)(v127 + 5) >> 6) & 1 ^ 1;
    v132 = (unsigned int)(v128 + 1) <= 0;
    if ( v128 != v131 )
      v132 = 1;
    *(_BYTE *)(v1 + 0xDB) = v131;
    v133 = *(char *)(v1 + 0xDD);
    v134 = ((unsigned int)*(unsigned __int8 *)(v127 + 5) >> 4) & 1 ^ 1;
    *(_BYTE *)(v1 + 0xDC) = v134;
    if ( v130 == v134 )
      v135 = (unsigned int)(v130 + 1) <= 0;
    else
      v135 = 1;
    v136 = v132 | 2 * v135;
    v137 = *(char *)(v1 + 0xDE);
    v138 = ((unsigned int)*(unsigned __int8 *)(v127 + 7) >> 3) & 1;
    *(_BYTE *)(v1 + 0xDD) = v138;
    if ( v133 == v138 )
      v139 = (unsigned int)(v133 + 1) <= 0;
    else
      v139 = 1;
    v140 = v136 | 4 * v139;
    v141 = ((unsigned int)*(unsigned __int8 *)(v127 + 7) >> 1) & 1 ^ 1;
    if ( v137 == v141 )
      v142 = (unsigned int)(v137 + 1) <= 0;
    else
      v142 = 1;
    *(_BYTE *)(v1 + 0xDE) = v141;
    v143 = v140 | 8 * v142;
    if ( v143 & 1 )
    {
      v152 = *(void (__fastcall **)(int, _DWORD))(v129 + 0x1E4);
      v153 = *(char *)(v129 + 0xDB);
      if ( v152 )
      {
        if ( *v157 )
          ((void (__fastcall *)(signed int, signed int, signed int, int, void (__fastcall *)(int, _DWORD), _DWORD))*v157)(
            5,
            4,
            32,
            v153,
            v152,
            *(_DWORD *)(v129 + 0x1E8));
        v152(v153, *(_DWORD *)(v1 + 0x1E8));
        if ( *v157 )
          (*v157)(6, 1, 32);
      }
    }
    if ( v143 & 2 )
    {
      v150 = *(void (__fastcall **)(int, _DWORD))(v1 + 0x1EC);
      v151 = *(char *)(v1 + 0xDC);
      if ( v150 )
      {
        if ( *v157 )
          ((void (__fastcall *)(signed int, signed int, signed int, int, _DWORD, int))*v157)(
            5,
            4,
            33,
            v151,
            *(_DWORD *)(v1 + 0x1EC),
            dword_8100C1F0);
        v150(v151, *(_DWORD *)(v1 + 0x1F0));
        if ( *v157 )
          (*v157)(6, 1, 33);
      }
    }
    if ( v143 & 4 )
    {
      v154 = *(void (__fastcall **)(int, _DWORD))(v1 + 0x1F4);
      v155 = *(char *)(v1 + 0xDD);
      if ( v154 )
      {
        if ( *v157 )
          ((void (__fastcall *)(signed int, signed int, signed int, int, _DWORD, int))*v157)(
            5,
            4,
            34,
            v155,
            *(_DWORD *)(v1 + 0x1F4),
            dword_8100C1F8);
        v154(v155, *(_DWORD *)(v1 + 0x1F8));
        if ( *v157 )
          (*v157)(6, 1, 34);
      }
    }
    if ( v143 & 8 )
    {
      v144 = *(void (__fastcall **)(int, _DWORD))(v1 + 0x1FC);
      v145 = *(char *)(v1 + 0xDE);
      if ( v144 )
      {
        if ( *v157 )
          ((void (__fastcall *)(signed int, signed int, signed int, int, _DWORD, int))*v157)(
            5,
            4,
            35,
            v145,
            *(_DWORD *)(v1 + 0x1FC),
            dword_8100C200);
        v144(v145, *(_DWORD *)(v1 + 0x200));
        if ( *v157 )
          (*v157)(6, 1, 35);
      }
    }
    goto LABEL_122;
  }
LABEL_116:
  if ( v54 == 260 )
    goto LABEL_240;
  if ( v54 <= 260 )
  {
    if ( v54 == 259 )
    {
      if ( *(_DWORD *)(v3 + 4) & 0x400 )
      {
        if ( *(_DWORD *)(v3 + 92) )
          goto LABEL_122;
        v112 = *(_DWORD *)(v3 + 84);
      }
      else
      {
        v112 = v3 + 48;
      }
      v113 = *(_DWORD *)(v112 + 4);
      v114 = *(void (__fastcall **)(int, _DWORD))(v1 + 0x20C);
      LOWORD(v115) = -16212;
      *(_BYTE *)(v1 + 0xE0) = 0;
      HIWORD(v115) = (unsigned int)&dword_8100C0AC >> 16;
      *v115 = v113;
      v116 = *(_DWORD *)(v1 + 0xAC);
      if ( v114 )
      {
        if ( *v157 )
          ((void (__fastcall *)(signed int, signed int, signed int, _DWORD, void (__fastcall *)(int, _DWORD), _DWORD))*v157)(
            5,
            4,
            37,
            *(_DWORD *)(v1 + 0xAC),
            v114,
            *(_DWORD *)((char *)v115 + 0xFFFFFF54 + 528));
        v114(v116, *(_DWORD *)(v1 + 0x210));
        if ( *v157 )
          (*v157)(6, 1, 37);
      }
      goto LABEL_122;
    }
  }
  else
  {
    if ( v54 == 304 )
    {
      if ( *(_DWORD *)(v3 + 4) & 0x400 )
      {
        if ( *(_DWORD *)(v3 + 92) )
          goto LABEL_122;
        v97 = *(_DWORD *)(v3 + 84);
      }
      else
      {
        v97 = v3 + 48;
      }
      v98 = *(_DWORD *)(v97 + 4);
      v99 = *(void (__fastcall **)(int, _DWORD))(v1 + 0x214);
      LOWORD(v100) = -16208;
      *(_BYTE *)(v1 + 0xDF) = 0;
      HIWORD(v100) = (unsigned int)&dword_8100C0B0 >> 16;
      *v100 = v98;
      v101 = *(_DWORD *)(v1 + 0xB0);
      if ( !v99 )
        goto LABEL_122;
      if ( *v157 )
        ((void (__fastcall *)(signed int, signed int, signed int, _DWORD, void (__fastcall *)(int, _DWORD), _DWORD))*v157)(
          5,
          4,
          38,
          *(_DWORD *)(v1 + 0xB0),
          v99,
          *(_DWORD *)((char *)v100 + 0xFFFFFF50 + 536));
      v99(v101, *(_DWORD *)(v1 + 0x218));
      v102 = *v157;
      if ( !*v157 )
        goto LABEL_122;
      goto LABEL_281;
    }
    if ( v54 == 432 && !*(_BYTE *)(v1 + 0xDF) )
    {
LABEL_121:
      byte_8100C0DF = 1;
      goto LABEL_122;
    }
  }
LABEL_122:
  if ( !(*(unsigned __int8 *)(v3 + 51) << 26) )
  {
    v72 = *(_DWORD *)(v3 + 4);
    if ( (v72 & 0x2000400) == 1024 )
    {
      v73 = *(unsigned __int8 *)(v3 + 50);
      if ( v73 > 1 )
      {
        v74 = *(_DWORD *)(v3 + 92);
        v75 = v73 - 2;
        v76 = v73 - 2 + v74;
        if ( v76 <= *(_DWORD *)(v3 + 88) )
        {
          if ( v73 != 2 )
          {
            imp_SceSysclibForDriver_memmove(*(_DWORD *)(v3 + 84) + v74, v74 + 4 + *(_DWORD *)(v3 + 84), v75);
            v76 = *(_DWORD *)(v3 + 92) + v75;
          }
          *(_DWORD *)(v3 + 92) = v76;
        }
        else
        {
          *(_DWORD *)(v3 + 4) = v72 | 0x2000000;
        }
      }
    }
  }
LABEL_135:
  imp_SceCpuForDriver_ksceKernelCpuLockStoreLR(&unk_8100C260);
  v77 = *(_DWORD *)(v3 + 4);
  if ( !(v77 & 0x800010)
    && ((v78 = *(unsigned __int8 *)(v3 + 51), v78 & 0x40) || *(_DWORD *)(v3 + 12) != 17 && (v78 ^ 0x80u) <= 1) )
  {
    v84 = -1;
    v83 = 2;
  }
  else
  {
    if ( v77 & 0x40000 )
    {
      *(_DWORD *)(v3 + 4) = v77 & 0xFFFBFFFF;
      --*(_BYTE *)(v1 + 193);
    }
    if ( (*(unsigned __int8 *)(v3 + 16) | (*(unsigned __int8 *)(v3 + 17) << 8)) == 4 )
    {
      v117 = *(unsigned __int8 *)(v3 + 50);
      if ( v117 > 7 )
      {
        LOWORD(v118) = -16384;
        if ( v117 > 0x1C )
          v119 = 22;
        else
          v119 = v117 - 6;
        v120 = v3;
        do
        {
          v123 = *(unsigned __int8 *)(v120 + 57);
          v124 = *(unsigned __int8 *)(v120 + 56);
          v125 = v124 | (v123 << 8);
          if ( !(v125 & 0x8000) )
          {
            v44 = (v124 & 0x80) == 0;
            v126 = 17;
            if ( !v44 )
            {
              if ( v123 > 0xF )
                v126 = 16;
              else
                v126 = *(unsigned __int8 *)(v120 + 57);
            }
            v121 = *(_DWORD *)(v1 + 4 * v126 + 4);
            if ( v121 )
            {
              v44 = v125 == (*(unsigned __int8 *)(v121 + 16) | (*(unsigned __int8 *)(v121 + 17) << 8));
              v122 = *(_DWORD *)(v121 + 4);
              if ( v44 )
              {
                if ( !(v122 & 0x40000) )
                {
                  *(_DWORD *)(v121 + 4) = v122 | 0x40000;
                  HIWORD(v118) = (unsigned int)dword_8100C000 >> 16;
                  ++*(_BYTE *)(v118 + 0xC1);
                }
              }
              else
              {
                *(_DWORD *)(v121 + 4) = v122 | 0x1000000;
              }
            }
          }
          v120 += 2;
        }
        while ( v119 > 1 - (signed int)v3 + v120 );
      }
    }
    if ( *v157 )
      ((void (__fastcall *)(signed int, signed int, int, _DWORD, _DWORD, _DWORD, _DWORD, _DWORD, _DWORD, _DWORD, _DWORD, _DWORD, _DWORD, _DWORD, _DWORD, _DWORD, _DWORD, _DWORD, _DWORD, _DWORD, _DWORD, _DWORD, _DWORD, _DWORD, _DWORD, _DWORD, _DWORD, _DWORD, _DWORD, _DWORD, _DWORD, _DWORD, _DWORD, _DWORD, _DWORD, _DWORD))*v157)(
        2,
        34,
        *(unsigned __int8 *)(v3 + 16) | (*(unsigned __int8 *)(v3 + 17) << 8),
        *(_DWORD *)(v3 + 4),
        *(unsigned __int8 *)(v3 + 48),
        *(unsigned __int8 *)(v3 + 49),
        *(unsigned __int8 *)(v3 + 50),
        *(unsigned __int8 *)(v3 + 51),
        *(unsigned __int8 *)(v3 + 52),
        *(unsigned __int8 *)(v3 + 53),
        *(unsigned __int8 *)(v3 + 54),
        *(unsigned __int8 *)(v3 + 55),
        *(unsigned __int8 *)(v3 + 56),
        *(unsigned __int8 *)(v3 + 57),
        *(unsigned __int8 *)(v3 + 58),
        *(unsigned __int8 *)(v3 + 59),
        *(unsigned __int8 *)(v3 + 60),
        *(unsigned __int8 *)(v3 + 61),
        *(unsigned __int8 *)(v3 + 62),
        *(unsigned __int8 *)(v3 + 63),
        *(unsigned __int8 *)(v3 + 64),
        *(unsigned __int8 *)(v3 + 65),
        *(unsigned __int8 *)(v3 + 66),
        *(unsigned __int8 *)(v3 + 67),
        *(unsigned __int8 *)(v3 + 68),
        *(unsigned __int8 *)(v3 + 69),
        *(unsigned __int8 *)(v3 + 70),
        *(unsigned __int8 *)(v3 + 71),
        *(unsigned __int8 *)(v3 + 72),
        *(unsigned __int8 *)(v3 + 73),
        *(unsigned __int8 *)(v3 + 74),
        *(unsigned __int8 *)(v3 + 75),
        *(unsigned __int8 *)(v3 + 76),
        *(unsigned __int8 *)(v3 + 77),
        *(unsigned __int8 *)(v3 + 78),
        *(unsigned __int8 *)(v3 + 79));
    v79 = *(_DWORD *)(v3 + 12);
    LOWORD(v80) = -16384;
    v81 = *(_DWORD *)v3;
    *(_DWORD *)(v1 + 4 * v79 + 4) = *(_DWORD *)v3;
    if ( !v81 )
    {
      HIWORD(v80) = (unsigned int)dword_8100C000 >> 16;
      *(_DWORD *)(v80 + 4 * v79 + 76) = 0;
    }
    v82 = *(_DWORD *)(v3 + 4);
    v83 = *(_DWORD *)(v3 + 96);
    *(_DWORD *)v3 = 0;
    v84 = *(_DWORD *)(v3 + 8);
    *(_DWORD *)(v3 + 4) = v82 & 0xBFF6FFFF | 0x80000;
    if ( v83 )
    {
      imp_SceCpuForDriver_ksceKernelCpuUnlockStoreLR(&unk_8100C260);
      if ( *v157 )
        ((void (__fastcall *)(signed int, signed int, int, _DWORD, _DWORD, _DWORD, _DWORD, _DWORD, _DWORD, _DWORD, _DWORD, _DWORD, _DWORD, _DWORD, _DWORD, _DWORD, _DWORD, _DWORD, _DWORD, _DWORD, _DWORD, _DWORD, _DWORD, _DWORD, _DWORD, _DWORD, _DWORD, _DWORD, _DWORD, _DWORD, _DWORD, _DWORD, _DWORD, _DWORD, _DWORD))*v157)(
          3,
          33,
          *(unsigned __int8 *)(v3 + 16) | (*(unsigned __int8 *)(v3 + 17) << 8),
          *(unsigned __int8 *)(v3 + 48),
          *(unsigned __int8 *)(v3 + 49),
          *(unsigned __int8 *)(v3 + 50),
          *(unsigned __int8 *)(v3 + 51),
          *(unsigned __int8 *)(v3 + 52),
          *(unsigned __int8 *)(v3 + 53),
          *(unsigned __int8 *)(v3 + 54),
          *(unsigned __int8 *)(v3 + 55),
          *(unsigned __int8 *)(v3 + 56),
          *(unsigned __int8 *)(v3 + 57),
          *(unsigned __int8 *)(v3 + 58),
          *(unsigned __int8 *)(v3 + 59),
          *(unsigned __int8 *)(v3 + 60),
          *(unsigned __int8 *)(v3 + 61),
          *(unsigned __int8 *)(v3 + 62),
          *(unsigned __int8 *)(v3 + 63),
          *(unsigned __int8 *)(v3 + 64),
          *(unsigned __int8 *)(v3 + 65),
          *(unsigned __int8 *)(v3 + 66),
          *(unsigned __int8 *)(v3 + 67),
          *(unsigned __int8 *)(v3 + 68),
          *(unsigned __int8 *)(v3 + 69),
          *(unsigned __int8 *)(v3 + 70),
          *(unsigned __int8 *)(v3 + 71),
          *(unsigned __int8 *)(v3 + 72),
          *(unsigned __int8 *)(v3 + 73),
          *(unsigned __int8 *)(v3 + 74),
          *(unsigned __int8 *)(v3 + 75),
          *(unsigned __int8 *)(v3 + 76),
          *(unsigned __int8 *)(v3 + 77),
          *(unsigned __int8 *)(v3 + 78),
          *(unsigned __int8 *)(v3 + 79));
      v85 = (*(int (__fastcall **)(_DWORD, _DWORD))(v3 + 96))(v3, *(_DWORD *)(v3 + 100));
      v83 = v85;
      if ( *v157 )
        ((void (__fastcall *)(signed int, signed int, int, int))*v157)(
          4,
          2,
          *(unsigned __int8 *)(v3 + 16) | (*(unsigned __int8 *)(v3 + 17) << 8),
          v85);
      imp_SceCpuForDriver_ksceKernelCpuLockStoreLR(&unk_8100C260);
      if ( v83 )
      {
        if ( v83 == 1 )
        {
          v103 = *(_DWORD *)(v3 + 12);
          v104 = v1 + 4 * v103;
          v105 = *(_DWORD *)(v104 + 4);
          if ( v105 )
          {
            *(_DWORD *)(v104 + 4) = v3;
            *(_DWORD *)v3 = v105;
          }
          else
          {
            *(_DWORD *)(v104 + 4) = v3;
            dword_8100C000[v103 + 19] = v3;
            *(_DWORD *)v3 = 0;
          }
          v84 = -1;
          v106 = *(_DWORD *)(v1 + 72);
          *(_DWORD *)(v3 + 4) = *(_DWORD *)(v3 + 4) & 0xFFF6FFFF | 0x10000;
          goto LABEL_192;
        }
        if ( v83 == 2 )
        {
          v86 = *(_DWORD *)(v3 + 12) + 18;
          v87 = *(_DWORD **)(v1 + 4 * v86 + 4);
          if ( v87 )
            *v87 = v3;
          v88 = *(_DWORD *)(v3 + 4);
          *(_DWORD *)(v1 + 4 * v86 + 4) = v3;
          v84 = -1;
          *(_DWORD *)v3 = 0;
          *(_DWORD *)(v3 + 4) = v88 & 0xFFF6FFFF | 0x10000;
        }
      }
    }
  }
  if ( *(_BYTE *)(v1 + 224) == 1 )
  {
    if ( !dword_8100C048 )
      dword_8100C090 = (int)&dword_8100C3FC;
    v91 = *(char *)(v1 + 223);
    dword_8100C3FC = dword_8100C048;
    *(_DWORD *)(v1 + 72) = &dword_8100C3FC;
    LOWORD(v89) = -15364;
    *(_BYTE *)(v1 + 224) = 2;
    if ( v91 == 1 )
    {
      LOWORD(v90) = -15748;
      HIWORD(v89) = -32512;
      HIWORD(v90) = (unsigned int)&dword_8100C27C >> 16;
LABEL_206:
      *v90 = (int)v89;
      *(_DWORD *)(v1 + 72) = v90;
      *(_BYTE *)(v1 + 223) = 2;
      if ( v159 )
      {
        LOWORD(v89) = -15748;
        LOWORD(v92) = -15620;
        HIWORD(v89) = (unsigned int)&dword_8100C27C >> 16;
        HIWORD(v92) = (unsigned int)&dword_8100C2FC >> 16;
LABEL_171:
        LOWORD(v93) = -15620;
        *v92 = (int)v89;
        HIWORD(v93) = (unsigned int)&dword_8100C2FC >> 16;
        *(_DWORD *)(v1 + 72) = v92;
LABEL_172:
        v94 = 4;
        if ( *(_DWORD *)(v1 + 600) )
          v94 = dword_8100C0A4;
        while ( v94 >= imp_SceThreadmgrForDriver_ksceKernelGetSystemTimeLow() - HIDWORD(v3) )
          ;
        *(_DWORD *)v1 = v93;
        sub_8100003C((int)v93);
        goto LABEL_176;
      }
      if ( !(v49 & 1) )
      {
        v106 = -2130656644;
        goto LABEL_333;
      }
      v89 = &dword_8100C27C;
      goto LABEL_210;
    }
    HIWORD(v89) = (unsigned int)&dword_8100C3FC >> 16;
  }
  else
  {
    v89 = (int *)dword_8100C048;
    if ( byte_8100C0DF == 1 )
    {
      v90 = &dword_8100C27C;
      if ( !dword_8100C048 )
        dword_8100C090 = (int)&dword_8100C27C;
      goto LABEL_206;
    }
  }
  if ( v159 )
  {
    v92 = &dword_8100C2FC;
    if ( !v89 )
      *(_DWORD *)(v1 + 144) = &dword_8100C2FC;
    goto LABEL_171;
  }
  if ( v49 & 1 )
  {
    if ( !v89 )
    {
      *(_DWORD *)(v1 + 72) = &dword_8100C37C;
      *(_DWORD *)(v1 + 144) = &dword_8100C37C;
      dword_8100C37C = 0;
      goto LABEL_211;
    }
LABEL_210:
    *(_DWORD *)(v1 + 72) = &dword_8100C37C;
    dword_8100C37C = (int)v89;
LABEL_211:
    v93 = &dword_8100C37C;
    goto LABEL_172;
  }
  v106 = (signed int)v89;
LABEL_192:
  if ( v106 )
  {
LABEL_333:
    v93 = (int *)v106;
    goto LABEL_172;
  }
  v107 = *(unsigned __int8 *)(v1 + 192);
  v93 = 0;
  v108 = 17;
  v109 = *(unsigned __int8 *)(v1 + 192);
  do
  {
    v110 = *(_DWORD *)(v1 + 4 * v109 + 4);
    if ( v110 )
    {
      v111 = *(_DWORD *)(v110 + 4);
      if ( v111 & 0x40000 )
      {
        v93 = *(int **)(v1 + 4 * v109 + 4);
        goto LABEL_285;
      }
      if ( v111 & 0x40000000 )
      {
        if ( !v106 )
          v106 = *(_DWORD *)(v1 + 4 * v109 + 4);
      }
      else if ( !v93 )
      {
        v93 = *(int **)(v1 + 4 * v109 + 4);
      }
    }
    if ( ++v109 == 17 )
      v109 = 0;
    --v108;
  }
  while ( v108 );
  if ( !v93 )
  {
    if ( v106 )
    {
      v93 = (int *)v106;
      if ( !*(_DWORD *)(v1 + 604) )
      {
        if ( dword_8100C258 )
          v93 = (int *)v106;
        else
          v93 = 0;
      }
    }
  }
LABEL_285:
  if ( v107 > 0xF )
    v149 = 0;
  else
    v149 = v107 + 1;
  *(_BYTE *)(v1 + 192) = v149;
  if ( v93 )
    goto LABEL_172;
LABEL_176:
  if ( !v83 && v84 >= 0 )
    imp_SceThreadmgrForDriver_ksceKernelSignalSema(v84, 1);
  LOWORD(v95) = -15776;
  HIWORD(v95) = (unsigned int)&unk_8100C260 >> 16;
  *(_BYTE *)(v1 + 190) = 0;
  imp_SceCpuForDriver_ksceKernelCpuResumeIntr(v95, v161);
  return -1;
}

unsigned int __fastcall sub_81001934(unsigned int result, int a2, int a3)
{
  unsigned int v3; // r4
  int v4; // r3
  int v5; // r3
  int v6; // r3
  int v7; // r6
  int v8; // r4
  int v9; // r0
  int v10; // r0
  int v11; // lr
  int v12; // r1
  int v13; // r3
  unsigned int v14; // r0
  char v15; // r7
  int v16; // r2
  int v17; // r5
  char v18; // r4
  _BYTE *v19; // r2
  char v20; // r0
  int v21; // r3
  bool v22; // nf
  int v23; // r2
  int v24; // r1
  int v25; // r3

  v3 = result;
  if ( result )
  {
    if ( result != 1 )
      return 0;
    if ( a2 != 65542 )
    {
      if ( a2 == 65551 )
      {
        imp_SceIntrmgrForDriver_ksceKernelSetIntrTarget(248, 15);
        LOWORD(v4) = -16384;
        HIWORD(v4) = (unsigned int)dword_8100C000 >> 16;
        result = 0;
        *(_DWORD *)(v4 + 0x258) = 0;
        return result;
      }
      return 0;
    }
    LOWORD(v6) = -16384;
    HIWORD(v6) = (unsigned int)dword_8100C000 >> 16;
    v7 = *(_DWORD *)(a3 + 4);
    v8 = *(_DWORD *)(v6 + 0x98);
    imp_ScePervasiveForDriver_kscePervasiveSpiClockEnable(0);
    imp_ScePervasiveForDriver_kscePervasiveSpiResetDisable(0);
    *(_DWORD *)(v8 + 32) = 0;
    __dmb(0xFu);
    ((void (*)(void))imp_SceGpioForDriver_ksceGpioSetPortMode)();
    imp_SceGpioForDriver_ksceGpioSetPortMode(0, 4, 1);
    imp_SceGpioForDriver_ksceGpioSetIntrMode(0, 4, 3);
    v9 = imp_SceIntrmgrForDriver_ksceKernelEnableSubIntr(248, 4);
    v10 = imp_SceThreadmgrForDriver_ksceKernelGetSystemTimeLow(v9);
    LOWORD(v11) = -16384;
    LOWORD(v12) = -27260;
    HIWORD(v11) = (unsigned int)dword_8100C000 >> 16;
    HIWORD(v12) = (unsigned int)byte_81009584 >> 16;
    *(_DWORD *)(v11 + 0x9C) = v10;
    v13 = 0;
    v14 = *(_DWORD *)(v7 + 12);
    for ( *(_DWORD *)(v11 + 0xA8) = v14; ; v14 = *(_DWORD *)(v11 + 0xA8) )
    {
      v15 = *(_BYTE *)(v12 + v13);
      v16 = v12 + v13;
      v17 = *(unsigned __int8 *)(v12 + v13 + 1);
      v13 += 8;
      v18 = *(_BYTE *)(v16 + 2);
      v19 = *(_BYTE **)(v16 + 4);
      v20 = (v14 >> 8 * v15) & v17 ? v18 ^ 1 : v18;
      *v19 = v20;
      if ( v13 == 200 )
        break;
    }
    LOWORD(v21) = -16384;
    result = 0;
    v23 = *(_DWORD *)(v11 + 0x228) << 28;
    v22 = v23 < 0;
    *(_BYTE *)(v11 + 0xDB) = ((*(_DWORD *)(v7 + 20) ^ 0x4000u) >> 14) & 1;
    if ( v23 < 0 )
      LOBYTE(v23) = 1;
    *(_BYTE *)(v11 + 0xDC) = ((*(_DWORD *)(v7 + 20) ^ 0x1000u) >> 12) & 1;
    *(_BYTE *)(v11 + 0xDD) = (*(_DWORD *)(v7 + 20) >> 27) & 1;
    v24 = *(_DWORD *)(v7 + 20);
    if ( v22 )
    {
      HIWORD(v21) = (unsigned int)dword_8100C000 >> 16;
      *(_BYTE *)(v21 + 0xDF) = v23;
    }
    *(_BYTE *)(v11 + 0xC7) = -1;
    *(_BYTE *)(v11 + 0xE0) = 1;
    *(_BYTE *)(v11 + 0xC8) = -1;
    *(_BYTE *)(v11 + 0xDE) = ((v24 ^ 0x2000000u) >> 25) & 1;
  }
  else
  {
    switch ( a2 )
    {
      case 16390:
        imp_SceIntrmgrForDriver_ksceKernelDisableSubIntr(248, 4);
        return v3;
      case 16399:
        imp_SceIntrmgrForDriver_ksceKernelSetIntrTarget(248, 8);
        LOWORD(v5) = -16384;
        HIWORD(v5) = (unsigned int)dword_8100C000 >> 16;
        result = v3;
        *(_DWORD *)(v5 + 0x258) = 1;
        return result;
      case 1026:
        v25 = dword_8100C004;
        if ( !dword_8100C004 )
        {
          if ( !dword_8100C008
            && !dword_8100C00C
            && !dword_8100C010
            && !dword_8100C014
            && !dword_8100C018
            && !dword_8100C01C
            && !dword_8100C020
            && !dword_8100C024
            && !dword_8100C028
            && !dword_8100C02C
            && !dword_8100C030
            && !dword_8100C034
            && !dword_8100C038
            && !dword_8100C03C
            && !dword_8100C040 )
          {
            result = dword_8100C044;
            if ( !dword_8100C044 )
              return result;
          }
          v25 = dword_8100C008;
          if ( !dword_8100C008 )
          {
            v25 = dword_8100C00C;
            if ( !dword_8100C00C )
            {
              v25 = dword_8100C010;
              if ( !dword_8100C010 )
              {
                v25 = dword_8100C014;
                if ( !dword_8100C014 )
                {
                  v25 = dword_8100C018;
                  if ( !dword_8100C018 )
                  {
                    v25 = dword_8100C01C;
                    if ( !dword_8100C01C )
                    {
                      v25 = dword_8100C020;
                      if ( !dword_8100C020 )
                      {
                        v25 = dword_8100C024;
                        if ( !dword_8100C024 )
                        {
                          v25 = dword_8100C028;
                          if ( !dword_8100C028 )
                          {
                            v25 = dword_8100C02C;
                            if ( !dword_8100C02C )
                            {
                              v25 = dword_8100C030;
                              if ( !dword_8100C030 )
                              {
                                v25 = dword_8100C034;
                                if ( !dword_8100C034 )
                                {
                                  v25 = dword_8100C038;
                                  if ( !dword_8100C038 )
                                  {
                                    v25 = dword_8100C03C;
                                    if ( !dword_8100C03C )
                                    {
                                      v25 = dword_8100C040;
                                      if ( !dword_8100C040 )
                                      {
                                        v25 = dword_8100C044;
                                        if ( !dword_8100C044 )
                                          return -1;
                                      }
                                    }
                                  }
                                }
                              }
                            }
                          }
                        }
                      }
                    }
                  }
                }
              }
            }
          }
        }
        return *(unsigned __int8 *)(v25 + 16) | (*(unsigned __int8 *)(v25 + 17) << 8) | 0x80258000;
    }
  }
  return result;
}

int sub_81001B80()
{
  int v0; // r12
  int v1; // r11
  int v2; // ST0C_4
  int v3; // r6
  int v4; // r5
  int v5; // r0
  int v6; // r0
  int v7; // r0
  int v8; // r3
  int v9; // r2
  int v10; // r0
  int v11; // r0
  int v12; // r1
  int v13; // r0
  int v14; // r4
  int v15; // r1
  int v16; // r2
  int v17; // r0
  int v18; // r0
  int *v19; // r2
  int result; // r0
  int v21; // [sp+14h] [bp-84h]
  int v22; // [sp+1Ch] [bp-7Ch]
  int v23; // [sp+24h] [bp-74h]
  int v24; // [sp+6Ch] [bp-2Ch]

  LOWORD(v0) = -15492;
  HIWORD(v0) = (unsigned int)&dword_8100C37C >> 16;
  v24 = MEMORY[0];
  LOWORD(v1) = -15620;
  v2 = v0;
  HIWORD(v1) = (unsigned int)&dword_8100C2FC >> 16;
  imp_SceSysclibForDriver_memset(dword_8100C000, 0, 632);
  LOWORD(v3) = -15364;
  HIWORD(v3) = (unsigned int)&dword_8100C3FC >> 16;
  imp_SceSysclibForDriver_memset(v2, 0, 128);
  *(_DWORD *)(v2 + 12) = 17;
  *(_BYTE *)(v2 + 19) = -5;
  *(_DWORD *)(v2 + 8) = -1;
  LOWORD(v4) = -15748;
  *(_BYTE *)(v2 + 16) = 3;
  HIWORD(v4) = (unsigned int)&dword_8100C27C >> 16;
  *(_BYTE *)(v2 + 18) = 1;
  imp_SceSysclibForDriver_memset(v1, 0, 128);
  *(_DWORD *)(v1 + 0xC) = 17;
  *(_DWORD *)(v1 + 8) = -1;
  *(_BYTE *)(v1 + 0x13) = -6;
  *(_BYTE *)(v1 + 0x12) = 1;
  *(_BYTE *)(v1 + 0x10) = 4;
  imp_SceSysclibForDriver_memset(v3, 0, 128);
  *(_BYTE *)(v3 + 0x13) = -6;
  *(_DWORD *)(v3 + 0xC) = 17;
  *(_DWORD *)(v3 + 8) = -1;
  *(_BYTE *)(v3 + 0x10) = 3;
  *(_BYTE *)(v3 + 0x11) = 1;
  *(_BYTE *)(v3 + 0x12) = 1;
  imp_SceSysclibForDriver_memset(v4, 0, 128);
  *(_DWORD *)(v4 + 0xC) = 17;
  *(_DWORD *)(v4 + 8) = -1;
  dword_8100C0A0 = 4000;
  dword_8100C0A4 = 20000;
  dword_8100C0B8 = 7;
  *(_BYTE *)(v4 + 0x13) = -51;
  *(_BYTE *)(v4 + 0x10) = 48;
  *(_BYTE *)(v4 + 0x11) = 1;
  *(_BYTE *)(v4 + 0x12) = 1;
  dword_8100C25C = 1;
  dword_8100C0B4 = 3;
  v5 = imp_SceGpioForDriver_ksceGpioPortClear(0, 3);
  dword_8100C09C = imp_SceThreadmgrForDriver_ksceKernelGetSystemTimeLow(v5);
  imp_SceGpioForDriver_ksceGpioSetPortMode(0, 3, 0);
  imp_SceGpioForDriver_ksceGpioSetPortMode(0, 4, 1);
  imp_SceGpioForDriver_ksceGpioSetIntrMode(0, 4, 3);
  imp_SceSysclibForDriver_memset(&v21, 0, 88);
  LOWORD(v6) = -27060;
  HIWORD(v6) = (unsigned int)&dword_8100958C[48] >> 16;
  v21 = 88;
  v23 = -526385152;
  v22 = 2;
  v7 = imp_SceSysmemForDriver_ksceKernelAllocMemBlock(v6, 537921542, 4096, &v21);
  v8 = v7;
  if ( v7 >= 0 )
  {
    dword_8100C264 = v7;
    imp_SceSysmemForDriver_ksceKernelGetMemBlockBase();
    LOWORD(v9) = -27048;
    HIWORD(v9) = (unsigned int)&dword_8100958C[51] >> 16;
    imp_SceIntrmgrForDriver_ksceKernelRegisterSubIntrHandler(248, 4, v9, sub_8100064C, 0);
    v10 = imp_SceIntrmgrForDriver_ksceKernelEnableSubIntr(248, 4);
    dword_8100C228 = imp_SceSysrootForDriver_ksceSysrootGetHardwareInfo(v10);
    imp_SceSysrootForDriver_ksceSysrootIsAuCodecIcConexant(&unk_8100C22C);
    LOWORD(v11) = -27032;
    LOWORD(v12) = 6453;
    HIWORD(v12) = (unsigned int)sub_81001934 >> 16;
    HIWORD(v11) = (unsigned int)&dword_8100958C[55] >> 16;
    imp_SceKernelSuspendForDriver_ksceKernelRegisterSysEventHandler(v11, v12, 0);
    LOWORD(v13) = -27032;
    HIWORD(v13) = (unsigned int)&dword_8100958C[55] >> 16;
    v8 = imp_SceThreadmgrForDriver_ksceKernelCreateMutex(v13, 0, 0, 0);
    dword_8100C268 = v8;
    if ( v8 >= 0 )
    {
      v14 = dword_8100C098;
      imp_ScePervasiveForDriver_kscePervasiveSpiClockEnable(0);
      imp_ScePervasiveForDriver_kscePervasiveSpiResetDisable(0);
      *(_DWORD *)(v14 + 32) = 0;
      __dmb(0xFu);
      LOWORD(v15) = -27020;
      LOWORD(v16) = 765;
      HIWORD(v16) = (unsigned int)sub_810002FC >> 16;
      HIWORD(v15) = (unsigned int)&dword_8100958C[58] >> 16;
      v17 = imp_SceThreadmgrForDriver_ksceKernelEnqueueWorkQueue(65571, v15, v16, 0);
      v18 = imp_SceSysrootForDriver_0x3AE319DA(v17);
      LOWORD(v19) = -15752;
      v8 = 0;
      HIWORD(v19) = (unsigned int)&dword_8100C278 >> 16;
      *v19 = v18;
    }
  }
  result = v8;
  if ( v24 != MEMORY[0] )
    imp_SceSysclibForDriver___stack_chk_fail(v8);
  return result;
}

// [Export libnid: 0x00000000 (noname), NID: 0x935CD196]
signed int exp_noname_0x935CD196()
{
  signed int result; // r0

  if ( sub_81001B80() >= 0 )
    result = 0;
  else
    result = 2;
  return result;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0xF245CD6F]
int __fastcall exp_SceSysconForDriver_ksceSysconSetDebugHandlers(int a1)
{
  dword_8100C094 = a1;
  return 0;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x55DF1C9B]
int exp_SceSysconForDriver_ksceSysconWaitInitialized()
{
  while ( !dword_8100C270 )
    imp_SceThreadmgrForDriver_ksceKernelDelayThread(1000);
  return 0;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x0D58E297]
int __fastcall exp_SceSysconForDriver_0x0D58E297(int a1)
{
  int v1; // r4
  int v2; // r3
  int result; // r0

  v1 = a1;
  if ( a1 )
    imp_SceIntrmgrForDriver_ksceKernelSetIntrTarget(248, 8);
  else
    imp_SceIntrmgrForDriver_ksceKernelSetIntrTarget(248, 15);
  LOWORD(v2) = -16384;
  result = 0;
  HIWORD(v2) = (unsigned int)dword_8100C000 >> 16;
  *(_DWORD *)(v2 + 0x258) = v1;
  return result;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x901D6CD4]
int __fastcall exp_SceSysconForDriver_0x901D6CD4(int a1)
{
    // Sets a flag used internally by SceSyscon.
    // Used by SceCtrl, ScePower.

  dword_8100C25C = a1;
  return 0;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0xFF86F4C5]
int exp_SceSysconForDriver_ksceSysconGetBaryonVersion()
{
  return dword_8100C224;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0xCBD6D8BC]
int exp_SceSysconForDriver_ksceSysconGetHardwareInfo()
{
  return dword_8100C228;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x965C68C3]
int __fastcall exp_SceSysconForDriver_ksceSysconGetHardwareInfo2(_DWORD *a1, int a2, int a3)
{
  int v3; // r6
  int *v4; // r4
  _DWORD *v5; // r5
  bool v6; // nf
  int v7; // r3
  int result; // r0

  v4 = dword_8100C000;
  v5 = a1;
  v7 = dword_8100C228 << 26;
  v6 = dword_8100C228 << 26 < 0;
  if ( v6 )
    v4 = (int *)&unk_8100C22C;
  else
    LOWORD(v3) = 3;
  if ( dword_8100C228 << 26 >= 0 )
  {
    HIWORD(v3) = -32731;
  }
  else
  {
    a1 = (_DWORD *)*v4;
    a2 = v4[1];
    a3 = v4[2];
    v7 = v4[3];
    v3 = 0;
  }
  if ( dword_8100C228 << 26 < 0 )
    *v5 = a1;
  result = v3;
  if ( v6 )
  {
    v5[1] = a2;
    v5[2] = a3;
    v5[3] = v7;
  }
  return result;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x4D588A0A]
__int64 exp_SceSysconForDriver_ksceSysconGetBaryonTimestamp()
{
  return unk_8100C250;
}


#pragma region "ksceSysconSetXxxCallback"

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x18A6F4D9]
int __fastcall exp_SceSysconForDriver_0x18A6F4D9(int a1, int a2)
{
  return SysconSetCallback(a1, a2, 0);
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0xDE613081]
int __fastcall exp_SceSysconForDriver_0xDE613081(int a1, int a2)
{
  return SysconSetCallback(a1, a2, 1);
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x229A07C2]
int __fastcall exp_SceSysconForDriver_0x229A07C2(int a1, int a2)
{
  return SysconSetCallback(a1, a2, 2);
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x8351526D]
int __fastcall exp_SceSysconForDriver_0x8351526D(int a1, int a2)
{
  return SysconSetCallback(a1, a2, 3);
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x9F8340FF]
int __fastcall exp_SceSysconForDriver_0x9F8340FF(int a1, int a2)
{
  return SysconSetCallback(a1, a2, 4);
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x35E1689F]
int __fastcall exp_SceSysconForDriver_0x35E1689F(int a1, int a2)
{
  return SysconSetCallback(a1, a2, 5);
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x474A9EA7]
int __fastcall exp_SceSysconForDriver_0x474A9EA7(int a1, int a2)
{
  return SysconSetCallback(a1, a2, 6);
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x4E88B4D9]
int __fastcall exp_SceSysconForDriver_0x4E88B4D9(int a1, int a2)
{
  return SysconSetCallback(a1, a2, 8);
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x376CCCB8]
int __fastcall exp_SceSysconForDriver_0x376CCCB8(int a1, int a2)
{
  return SysconSetCallback(a1, a2, 9);
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x3BAAC8A9]
int __fastcall exp_SceSysconForDriver_0x3BAAC8A9(int a1, int a2)
{
  return SysconSetCallback(a1, a2, 10);
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x4A42712F]
int __fastcall exp_SceSysconForDriver_0x4A42712F(int a1, int a2)
{
  return SysconSetCallback(a1, a2, 15);
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x3F0DB7C0]
int __fastcall exp_SceSysconForDriver_ksceSysconSetLowBatteryCallback(int a1, int a2)
{
  return SysconSetCallback(a1, a2, 16);
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x80D6E061]
int __fastcall exp_SceSysconForDriver_0x80D6E061(int a1, int a2)
{
  return SysconSetCallback(a1, a2, 17);
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x7682FE69]
int __fastcall exp_SceSysconForDriver_0x7682FE69(int a1, int a2)
{
  return SysconSetCallback(a1, a2, 18);
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0xE0D52DF0]
int __fastcall exp_SceSysconForDriver_0xE0D52DF0(int a1, int a2)
{
  return SysconSetCallback(a1, a2, 19);
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x2D471528]
int __fastcall exp_SceSysconForDriver_0x2D471528(int a1, int a2)
{
  return SysconSetCallback(a1, a2, 20);
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x129EA022]
int __fastcall exp_SceSysconForDriver_0x129EA022(int a1, int a2)
{
  return SysconSetCallback(a1, a2, 21);
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x85E5DEBF]
int __fastcall exp_SceSysconForDriver_0x85E5DEBF(int a1, int a2)
{
  return SysconSetCallback(a1, a2, 22);
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x773B8126]
int __fastcall exp_SceSysconForDriver_ksceSysconSetThermalAlertCallback(int a1, int a2)
{
  return SysconSetCallback(a1, a2, 23);
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x2E4BA4B8]
int __fastcall exp_SceSysconForDriver_0x2E4BA4B8(int a1, int a2)
{
  return SysconSetCallback(a1, a2, 25);
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0xC442D0BE]
int __fastcall exp_SceSysconForDriver_0xC442D0BE(int a1, int a2)
{
  return SysconSetCallback(a1, a2, 26);
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x61AE3970]
int __fastcall exp_SceSysconForDriver_0x61AE3970(int a1, int a2)
{
  return SysconSetCallback(a1, a2, 27);
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x94678881]
int __fastcall exp_SceSysconForDriver_0x94678881(int a1, int a2)
{
  return SysconSetCallback(a1, a2, 28);
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x7AA00C01]
int __fastcall exp_SceSysconForDriver_0x7AA00C01(int a1, int a2)
{
  return SysconSetCallback(a1, a2, 29);
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x423D0C58]
int __fastcall exp_SceSysconForDriver_0x423D0C58(int a1, int a2)
{
  return SysconSetCallback(a1, a2, 32);
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x154676F1]
int __fastcall exp_SceSysconForDriver_0x154676F1(int a1, int a2)
{
  return SysconSetCallback(a1, a2, 33);
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x63352A39]
int __fastcall exp_SceSysconForDriver_0x63352A39(int a1, int a2)
{
  return SysconSetCallback(a1, a2, 34);
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x14730196]
int __fastcall exp_SceSysconForDriver_0x14730196(int a1, int a2)
{
  return SysconSetCallback(a1, a2, 35);
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x32418370]
int __fastcall exp_SceSysconForDriver_ksceSysconSetAlarmCallback(int a1, int a2)
{
  return SysconSetCallback(a1, a2, 36);
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0xA26586B2]
int __fastcall exp_SceSysconForDriver_0xA26586B2(int a1, int a2)
{
  return SysconSetCallback(a1, a2, 37);
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x67A4CB9F]
int __fastcall exp_SceSysconForDriver_0x67A4CB9F(int a1, int a2)
{
  return SysconSetCallback(a1, a2, 38);
}

#pragma endregion "ksceSysconSetXxxCallback"


#pragma region "Command execution"

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0xC2224E82]
signed int __fastcall exp_SceSysconForDriver_ksceSysconCmdExecAsync(int a1, unsigned __int16 a2, int a3, int a4)
{
  unsigned __int16 v4; // r7
  _DWORD *v5; // r5
  int v6; // r9
  int v7; // r10
  int v8; // r2
  int v9; // r6
  signed int v10; // r2
  unsigned int v11; // lr
  int v12; // r3
  unsigned int v13; // r0
  unsigned int v14; // r4
  signed int v15; // r1
  unsigned int v16; // lr
  unsigned int v17; // r12
  int v18; // r0
  int32x4_t v19; // q8
  int v20; // r0
  unsigned int v21; // lr
  uint16x8_t v22; // q12
  uint16x8_t v23; // q11
  uint16x8_t v24; // q9
  uint8x8_t *v25; // r11
  int32x4_t v26; // q11
  uint16x8_t v27; // q8
  uint8x8_t v28; // d18
  uint8x8_t v29; // d19
  uint16x8_t v30; // q10
  uint16x8_t v31; // q9
  int32x2_t v32; // d16
  int v33; // r0
  int v34; // r1
  char v35; // r0
  bool v36; // nf
  unsigned __int8 v37; // vf
  unsigned int v38; // r4
  unsigned int v39; // r0
  unsigned int v40; // r9
  int *v41; // lr
  int v42; // r3
  int *v43; // r2
  int v44; // r2
  int *v45; // r3
  int v46; // r6
  unsigned int v47; // r4
  int v49; // r3
  int v50; // r0
  int v51; // r3
  int v52; // r6
  unsigned int v53; // r4
  unsigned int v54; // [sp+4h] [bp-34h]
  int v55; // [sp+8h] [bp-30h]

  v4 = a2;
  v5 = (_DWORD *)a1;
  v6 = a3;
  v7 = a4;
  if ( byte_8100C0BF )
    return -2145058558;
  if ( !(a2 & 0x800) )
  {
    v8 = *(unsigned __int8 *)(a1 + 18);
    v9 = a1 + 16;
    *(_DWORD *)(a1 + 80) = 0;
    if ( (unsigned int)(v8 - 1) <= 0x1C )
      goto LABEL_4;
    return -2145058810;
  }
  v9 = *(_DWORD *)(a1 + 80);
  if ( !v9 )
    return -2145058814;
  *(_BYTE *)(a1 + 16) = *(_BYTE *)v9;
  *(_BYTE *)(a1 + 17) = *(_BYTE *)(v9 + 1);
  *(_BYTE *)(a1 + 18) = *(_BYTE *)(v9 + 2);
  if ( (unsigned __int8)(*(_BYTE *)(v9 + 2) - 1) > 0xFCu )
    return -2145058810;
LABEL_4:
  if ( !(a2 & 0x400) )
  {
    *(_DWORD *)(a1 + 84) = 0;
    *(_DWORD *)(a1 + 88) = 0;
LABEL_7:
    if ( a2 & 0x100 )
      goto LABEL_58;
    v10 = *(unsigned __int8 *)(v9 + 2);
    v11 = v10 + 2;
    v12 = v10 + 1;
    if ( (-(v9 & 7) & 0xFu) >= v10 + 2 )
      v13 = v10 + 2;
    else
      v13 = -(v9 & 7) & 0xF;
    if ( v11 > 0x11 )
    {
      v14 = v13;
      v15 = v13;
      if ( !v13 )
        goto LABEL_32;
    }
    else
    {
      v13 = v10 + 2;
    }
    v14 = *(unsigned __int8 *)v9;
    if ( v13 <= 1 )
    {
      v15 = 1;
    }
    else
    {
      v14 += *(unsigned __int8 *)(v9 + 1);
      if ( v13 <= 2 )
      {
        v15 = 2;
      }
      else
      {
        v14 += v10;
        if ( v13 <= 3 )
        {
          v15 = 3;
        }
        else
        {
          v14 += *(unsigned __int8 *)(v9 + 3);
          if ( v13 <= 4 )
          {
            v15 = 4;
          }
          else
          {
            v14 += *(unsigned __int8 *)(v9 + 4);
            if ( v13 <= 5 )
            {
              v15 = 5;
            }
            else
            {
              v14 += *(unsigned __int8 *)(v9 + 5);
              if ( v13 <= 6 )
              {
                v15 = 6;
              }
              else
              {
                v14 += *(unsigned __int8 *)(v9 + 6);
                if ( v13 <= 7 )
                {
                  v15 = 7;
                }
                else
                {
                  v14 += *(unsigned __int8 *)(v9 + 7);
                  if ( v13 <= 8 )
                  {
                    v15 = 8;
                  }
                  else
                  {
                    v14 += *(unsigned __int8 *)(v9 + 8);
                    if ( v13 <= 9 )
                    {
                      v15 = 9;
                    }
                    else
                    {
                      v14 += *(unsigned __int8 *)(v9 + 9);
                      if ( v13 <= 0xA )
                      {
                        v15 = 10;
                      }
                      else
                      {
                        v14 += *(unsigned __int8 *)(v9 + 10);
                        if ( v13 <= 0xB )
                        {
                          v15 = 11;
                        }
                        else
                        {
                          v14 += *(unsigned __int8 *)(v9 + 11);
                          if ( v13 <= 0xC )
                          {
                            v15 = 12;
                          }
                          else
                          {
                            v14 += *(unsigned __int8 *)(v9 + 12);
                            if ( v13 <= 0xD )
                            {
                              v15 = 13;
                            }
                            else
                            {
                              v14 += *(unsigned __int8 *)(v9 + 13);
                              if ( v13 <= 0xE )
                              {
                                v15 = 14;
                              }
                              else
                              {
                                v14 += *(unsigned __int8 *)(v9 + 14);
                                if ( v13 <= 0xF )
                                {
                                  v15 = 15;
                                }
                                else
                                {
                                  v14 += *(unsigned __int8 *)(v9 + 15);
                                  if ( v13 <= 0x10 )
                                  {
                                    v15 = 16;
                                  }
                                  else
                                  {
                                    v15 = 17;
                                    v14 += *(unsigned __int8 *)(v9 + 16);
                                  }
                                }
                              }
                            }
                          }
                        }
                      }
                    }
                  }
                }
              }
            }
          }
        }
      }
      if ( v11 == v13 )
      {
LABEL_56:
        *(_BYTE *)(v9 + v10 + 2) = ~(_BYTE)v14;
        if ( v10 <= 28 )
          imp_SceSysclibForDriver_memset(v10 + 3 + v9, -1, 29 - v10);
LABEL_58:
        imp_SceSysclibForDriver_memset(v5 + 12, -1, 32);
        v5[24] = v6;
        v5[25] = v7;
        v5[1] = v4 | 0x10000;
        v5[23] = 0;
        *v5 = 0;
        v5[2] = -1;
        v38 = *(unsigned __int8 *)(v9 + 1);
        if ( *(char *)v9 < 0 )
        {
          v51 = dword_8100C0A4;
          if ( v38 > 0xF )
            v38 = 16;
          v5[3] = v38;
          v5[27] = v51;
        }
        else
        {
          v38 = 17;
          v5[27] = 0;
          v5[3] = 17;
        }
        v39 = imp_SceCpuForDriver_ksceKernelCpuSuspendIntr(&unk_8100C260);
        LOWORD(v42) = -16384;
        v40 = v39;
        v41 = &dword_8100C000[v38];
        HIWORD(v42) = (unsigned int)dword_8100C000 >> 16;
        v43 = (int *)v41[1];
        if ( v43 )
        {
          if ( v4 & 1 )
          {
            if ( !(v43[1] & 0x40060000) )
            {
              *v5 = v43;
              LOWORD(v45) = -16384;
              v44 = dword_8100C000[0];
              HIWORD(v45) = (unsigned int)dword_8100C000 >> 16;
              v41[1] = (int)v5;
              if ( v44 )
                goto LABEL_83;
              goto LABEL_65;
            }
            v52 = *v43;
            *v5 = *v43;
            *v43 = (int)v5;
            if ( !v52 )
              *(_DWORD *)(v42 + 4 * v38 + 76) = v5;
          }
          else
          {
            v49 = v42 + 4 * (v38 + 18);
            **(_DWORD **)(v49 + 4) = v5;
            *(_DWORD *)(v49 + 4) = v5;
          }
        }
        else
        {
          v41[1] = (int)v5;
          *(_DWORD *)(v42 + 4 * v38 + 76) = v5;
        }
        v45 = dword_8100C000;
        if ( dword_8100C000[0] )
          goto LABEL_83;
LABEL_65:
        if ( !*((_BYTE *)v45 + 190) )
        {
          if ( v45[150] )
          {
            if ( v5[3] == 17 )
              v53 = 4;
            else
              v53 = v45[41];
            do
              v39 = imp_SceThreadmgrForDriver_ksceKernelGetSystemTimeLow(v39) - dword_8100C09C;
            while ( v53 >= v39 );
            dword_8100C000[0] = (int)v5;
            sub_8100003C((int)v5);
            goto LABEL_83;
          }
          v46 = v45[18];
          if ( v46 && ((_DWORD *)v46 != v5 || !(v4 & 2)) )
          {
            if ( v38 == 17 || !(v4 & 1) )
              goto LABEL_73;
          }
          else if ( v38 == 17 )
          {
            goto LABEL_83;
          }
          v46 = dword_8100C000[v38 + 1];
          if ( (_DWORD *)v46 == v5 )
          {
            if ( !(v4 & 2) )
              goto LABEL_73;
          }
          else if ( v46 )
          {
LABEL_73:
            v47 = 4;
            if ( dword_8100C25C )
              v47 = dword_8100C0A0;
            do
              v39 = imp_SceThreadmgrForDriver_ksceKernelGetSystemTimeLow(v39) - dword_8100C09C;
            while ( v47 >= v39 );
            dword_8100C000[0] = v46;
            sub_8100003C(v46);
            goto LABEL_83;
          }
        }
LABEL_83:
        LOWORD(v50) = -15776;
        HIWORD(v50) = (unsigned int)&unk_8100C260 >> 16;
        imp_SceCpuForDriver_ksceKernelCpuResumeIntr(v50, v40);
        return 0;
      }
    }
LABEL_32:
    v16 = v11 - v13;
    v17 = v16 >> 4;
    v54 = v16;
    v55 = 16 * (v16 >> 4);
    if ( !(v16 >> 4) )
      goto LABEL_125;
    v18 = v13 + v9;
    if ( v17 <= 2 )
    {
      v19.n128_u64[0] = 0LL;
      v19.n128_u64[1] = 0LL;
      v25 = (uint8x8_t *)v18;
      v21 = 0;
    }
    else
    {
      v19.n128_u64[0] = 0LL;
      v19.n128_u64[1] = 0LL;
      v20 = v18 + 32;
      v21 = 0;
      do
      {
        v22 = vmovl_u8(*(uint8x8_t *)(v20 - 32));
        v23 = vmovl_u8(*(uint8x8_t *)(v20 - 24));
        v24 = vmovl_u8(*(uint8x8_t *)(v20 - 16));
        v21 += 2;
        v25 = (uint8x8_t *)v20;
        v26 = vaddq_s32(
                vmovl_u16(*(uint16x4_t *)&v23.n128_i8[8]),
                vaddq_s32(
                  vmovl_u16((uint16x4_t)v23.n128_u64[0]),
                  vaddq_s32(
                    vmovl_u16(*(uint16x4_t *)&v22.n128_i8[8]),
                    vaddq_s32(vmovl_u16((uint16x4_t)v22.n128_u64[0]), v19))));
        v27 = vmovl_u8(*(uint8x8_t *)(v20 - 8));
        __pld((void *)(v20 + 192));
        v20 += 32;
        v19 = vaddq_s32(
                vmovl_u16(*(uint16x4_t *)&v27.n128_i8[8]),
                vaddq_s32(
                  vmovl_u16((uint16x4_t)v27.n128_u64[0]),
                  vaddq_s32(
                    vmovl_u16(*(uint16x4_t *)&v24.n128_i8[8]),
                    vaddq_s32(v26, vmovl_u16((uint16x4_t)v24.n128_u64[0])))));
      }
      while ( v21 != ((v17 - 3) & 0xFFFFFFFE) + 2 );
      v12 = v10 + 1;
    }
    do
    {
      v28.n64_u64[0] = v25->n64_u64[0];
      v29.n64_u64[0] = v25[1].n64_u64[0];
      v25 += 2;
      v30 = vmovl_u8(v28);
      v31 = vmovl_u8(v29);
      ++v21;
      v19 = vaddq_s32(
              vmovl_u16(*(uint16x4_t *)&v31.n128_i8[8]),
              vaddq_s32(
                vmovl_u16((uint16x4_t)v31.n128_u64[0]),
                vaddq_s32(
                  vmovl_u16(*(uint16x4_t *)&v30.n128_i8[8]),
                  vaddq_s32(vmovl_u16((uint16x4_t)v30.n128_u64[0]), v19))));
    }
    while ( v17 > v21 );
    v32.n64_u64[0] = vadd_s32((int32x2_t)v19.n128_u64[0], *(int32x2_t *)&v19.n128_i8[8]).n64_u64[0];
    v15 += v55;
    v14 += vpadd_s32(v32, v32).n64_u64[0];
    if ( v54 != v55 )
    {
LABEL_125:
      LOBYTE(v14) = v14 + *(_BYTE *)(v9 + v15);
      if ( v15 + 1 <= v12 )
      {
        LOBYTE(v14) = v14 + *(_BYTE *)(v9 + v15 + 1);
        if ( v12 >= v15 + 2 )
        {
          LOBYTE(v14) = v14 + *(_BYTE *)(v9 + v15 + 2);
          if ( v12 >= v15 + 3 )
          {
            LOBYTE(v14) = v14 + *(_BYTE *)(v9 + v15 + 3);
            if ( v12 >= v15 + 4 )
            {
              LOBYTE(v14) = v14 + *(_BYTE *)(v9 + v15 + 4);
              if ( v12 >= v15 + 5 )
              {
                LOBYTE(v14) = v14 + *(_BYTE *)(v9 + v15 + 5);
                if ( v12 >= v15 + 6 )
                {
                  LOBYTE(v14) = v14 + *(_BYTE *)(v9 + v15 + 6);
                  if ( v12 >= v15 + 7 )
                  {
                    LOBYTE(v14) = v14 + *(_BYTE *)(v9 + v15 + 7);
                    if ( v12 >= v15 + 8 )
                    {
                      LOBYTE(v14) = v14 + *(_BYTE *)(v9 + v15 + 8);
                      if ( v12 >= v15 + 9 )
                      {
                        LOBYTE(v14) = v14 + *(_BYTE *)(v9 + v15 + 9);
                        if ( v12 >= v15 + 10 )
                        {
                          LOBYTE(v14) = v14 + *(_BYTE *)(v9 + v15 + 10);
                          if ( v12 >= v15 + 11 )
                          {
                            LOBYTE(v14) = v14 + *(_BYTE *)(v9 + v15 + 11);
                            if ( v12 >= v15 + 12 )
                            {
                              v33 = v15 + 13;
                              LOBYTE(v14) = v14 + *(_BYTE *)(v9 + v15 + 12);
                              if ( v12 >= v15 + 13 )
                              {
                                v34 = v15 + 14;
                                v35 = *(_BYTE *)(v9 + v33);
                                v37 = __OFSUB__(v12, v34);
                                v36 = v12 - v34 < 0;
                                if ( v12 >= v34 )
                                  LOBYTE(v12) = *(_BYTE *)(v9 + v34);
                                LOBYTE(v14) = v14 + v35;
                                if ( !(v36 ^ v37) )
                                  LOBYTE(v14) = v14 + v12;
                              }
                            }
                          }
                        }
                      }
                    }
                  }
                }
              }
            }
          }
        }
      }
    }
    goto LABEL_56;
  }
  if ( !*(_DWORD *)(a1 + 84) )
    return -2145058814;
  if ( *(_DWORD *)(a1 + 88) > 0x20u )
    goto LABEL_7;
  return -2145058815;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0xC9DB5BFF]
signed int __fastcall exp_SceSysconForDriver_0xC9DB5BFF(signed int *a1)
{
  signed int *v1; // r4
  int v2; // r0
  int v3; // r5
  int v4; // r0
  signed int v5; // r6
  int v6; // r7
  signed int *v7; // r2
  signed int *v8; // r3
  signed int v9; // r4

  v1 = a1;
  LOWORD(v2) = -15776;
  LOWORD(v3) = -16384;
  HIWORD(v2) = (unsigned int)&unk_8100C260 >> 16;
  HIWORD(v3) = (unsigned int)dword_8100C000 >> 16;
  v4 = imp_SceCpuForDriver_ksceKernelCpuSuspendIntr(v2);
  v5 = v1[3];
  v6 = v3 + 4 * v5;
  v7 = *(signed int **)(v6 + 4);
  if ( !v7 )
  {
LABEL_7:
    v9 = -2145058813;
    goto LABEL_8;
  }
  if ( v1 != v7 )
  {
    while ( 1 )
    {
      v8 = (signed int *)*v7;
      if ( !*v7 )
        goto LABEL_7;
      if ( v1 == v8 )
      {
        if ( v8[1] & 0x40060000 )
          goto LABEL_14;
        *v7 = *v8;
        goto LABEL_11;
      }
      v7 = (signed int *)*v7;
    }
  }
  if ( v7[1] & 0x40060000 )
  {
LABEL_14:
    v9 = -2145058812;
  }
  else
  {
    *(_DWORD *)(v6 + 4) = *v7;
    v8 = v7;
    v7 = 0;
LABEL_11:
    v9 = *v8;
    if ( *v8 )
      v9 = 0;
    else
      *(_DWORD *)(v3 + 4 * v5 + 76) = v7;
  }
LABEL_8:
  imp_SceCpuForDriver_ksceKernelCpuResumeIntr(&unk_8100C260, v4);
  return v9;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x6E517D22]
signed int __fastcall exp_SceSysconForDriver_ksceSysconCmdSync(unsigned int a1, int a2)
{
  unsigned int v2; // r6
  int i; // r3
  unsigned int v4; // r5
  int v5; // r3
  signed int result; // r0
  int v7; // r8
  int v8; // r0
  int v9; // r7

  v2 = a1;
  if ( !a2 )
  {
    if ( dword_8100C258 )
    {
      for ( i = *(_DWORD *)(a1 + 4); !(i & 0x80000); i = *(_DWORD *)(v2 + 4) )
      {
        if ( *(_DWORD *)(v2 + 12) == 17 )
          v4 = 4;
        else
          v4 = dword_8100C0A0;
        do
          a1 = imp_SceThreadmgrForDriver_ksceKernelGetSystemTimeLow(a1) - dword_8100C09C;
        while ( a1 <= v4 );
        while ( !imp_SceGpioForDriver_ksceGpioQueryIntr(0, 4) )
          ;
        a1 = sub_8100064C();
      }
    }
    else
    {
      result = imp_SceThreadmgrForDriver_ksceKernelCheckWaitableStatus();
      if ( result < 0 )
        return result;
      v7 = imp_SceCpuForDriver_ksceKernelCpuSuspendIntr(&unk_8100C260);
      if ( *(_DWORD *)(v2 + 4) & 0x80000 )
      {
        imp_SceCpuForDriver_ksceKernelCpuResumeIntr(&unk_8100C260, v7);
        i = *(_DWORD *)(v2 + 4);
      }
      else
      {
        LOWORD(v8) = -27004;
        HIWORD(v8) = (unsigned int)&dword_8100958C[62] >> 16;
        v9 = imp_SceThreadmgrForDriver_ksceKernelCreateSema(v8, 0, 0, 1, 0);
        if ( v9 < 0 )
        {
          imp_SceCpuForDriver_ksceKernelCpuResumeIntr(&unk_8100C260, v7);
          return v9;
        }
        *(_DWORD *)(v2 + 8) = v9;
        imp_SceCpuForDriver_ksceKernelCpuResumeIntr(&unk_8100C260, v7);
        imp_SceThreadmgrForDriver_ksceKernelWaitSema(v9, 1, 0);
        imp_SceThreadmgrForDriver_ksceKernelDeleteSema(v9);
        i = *(_DWORD *)(v2 + 4);
      }
    }
    goto LABEL_10;
  }
  if ( a2 != 1 )
    return -2145058816;
  if ( !a1 )
  {
    if ( !dword_8100C004 && !dword_8100C008 && !dword_8100C00C && !dword_8100C010 && !dword_8100C014 && !dword_8100C018 )
    {
      if ( !dword_8100C01C
        && !dword_8100C020
        && !dword_8100C024
        && !dword_8100C028
        && !dword_8100C02C
        && !dword_8100C030
        && !dword_8100C034
        && !dword_8100C038
        && !dword_8100C03C
        && !dword_8100C040
        && !dword_8100C044 )
      {
        return 0;
      }
      return 1;
    }
    return 1;
  }
  i = *(_DWORD *)(a1 + 4);
  if ( !(i & 0x80000) )
    return 1;
LABEL_10:
  if ( i & 0xB00000 )
    return -2145058559;
  if ( !(i & 0x2000000) )
  {
    v5 = *(unsigned __int8 *)(v2 + 51);
    if ( v5 << 26 )
      return v5 | 0x80250200;
    return 0;
  }
  return -2145058555;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x9ADDCA4A]
signed int __fastcall exp_SceSysconForDriver_ksceSysconCmdExec(int a1, unsigned __int16 a2)
{
  int v2; // r4
  unsigned __int16 v3; // r7
  int v4; // r2
  int v5; // r5
  signed int v6; // r2
  unsigned int v7; // r3
  unsigned int v8; // r9
  int v9; // r0
  unsigned int v10; // lr
  signed int v11; // r1
  unsigned int v12; // r9
  unsigned int v13; // r8
  int v14; // r10
  uint8x8_t *v15; // lr
  int32x4_t v16; // q8
  int v17; // lr
  unsigned int v18; // r12
  uint16x8_t v19; // q12
  uint16x8_t v20; // q11
  uint16x8_t v21; // q9
  int32x4_t v22; // q11
  uint16x8_t v23; // q8
  uint8x8_t v24; // d18
  uint8x8_t v25; // d19
  uint16x8_t v26; // q10
  uint16x8_t v27; // q9
  int32x2_t v28; // d16
  int v29; // lr
  int v30; // r1
  char v31; // lr
  bool v32; // nf
  unsigned __int8 v33; // vf
  int v34; // r2
  unsigned int v35; // r5
  int v36; // r0
  unsigned int v37; // r0
  int *v38; // lr
  int *v39; // r2
  unsigned int v40; // r8
  int v41; // r3
  int v42; // r2
  int *v43; // r3
  int v44; // r9
  unsigned int v45; // r5
  signed int result; // r0
  int v47; // r3
  int v48; // r0
  int v49; // r3
  int v50; // lr
  unsigned int v51; // r5
  uint8x8_t *v52; // [sp+4h] [bp-2Ch]

  v2 = a1;
  v3 = a2;
  if ( dword_8100C258 )
  {
    if ( imp_SceIntrmgrForDriver_ksceKernelIsIntrAllowedInCurrentContext() )
      return -2145058811;
  }
  else
  {
    result = imp_SceThreadmgrForDriver_ksceKernelCheckWaitableStatus();
    if ( result < 0 )
      return result;
  }
  if ( byte_8100C0BF )
    return -2145058558;
  if ( !(v3 & 0x800) )
  {
    v4 = *(unsigned __int8 *)(v2 + 18);
    v5 = v2 + 16;
    *(_DWORD *)(v2 + 80) = 0;
    if ( (unsigned int)(v4 - 1) <= 0x1C )
      goto LABEL_6;
    return -2145058810;
  }
  v5 = *(_DWORD *)(v2 + 80);
  if ( !v5 )
    return -2145058814;
  *(_BYTE *)(v2 + 16) = *(_BYTE *)v5;
  *(_BYTE *)(v2 + 17) = *(_BYTE *)(v5 + 1);
  *(_BYTE *)(v2 + 18) = *(_BYTE *)(v5 + 2);
  if ( (unsigned __int8)(*(_BYTE *)(v5 + 2) - 1) > 0xFCu )
    return -2145058810;
LABEL_6:
  if ( !(v3 & 0x400) )
  {
    *(_DWORD *)(v2 + 84) = 0;
    *(_DWORD *)(v2 + 88) = 0;
LABEL_9:
    if ( v3 & 0x100 )
      goto LABEL_59;
    v6 = *(unsigned __int8 *)(v5 + 2);
    v7 = -(v5 & 7) & 0xF;
    v8 = v6 + 2;
    v9 = v6 + 1;
    if ( v7 >= v6 + 2 )
      v7 = v6 + 2;
    if ( v8 > 0x11 )
    {
      v10 = v7;
      if ( !v7 )
      {
        v12 = v8 - v7;
        v11 = v7;
        v13 = v12 >> 4;
        v14 = 16 * (v12 >> 4);
        if ( !(v12 >> 4) )
          goto LABEL_40;
        goto LABEL_34;
      }
    }
    else
    {
      v10 = v6 + 2;
    }
    v7 = *(unsigned __int8 *)v5;
    if ( v10 <= 1 )
    {
      v11 = 1;
    }
    else
    {
      v7 += *(unsigned __int8 *)(v5 + 1);
      if ( v10 <= 2 )
      {
        v11 = 2;
      }
      else
      {
        v7 += v6;
        if ( v10 <= 3 )
        {
          v11 = 3;
        }
        else
        {
          v7 += *(unsigned __int8 *)(v5 + 3);
          if ( v10 <= 4 )
          {
            v11 = 4;
          }
          else
          {
            v7 += *(unsigned __int8 *)(v5 + 4);
            if ( v10 <= 5 )
            {
              v11 = 5;
            }
            else
            {
              v7 += *(unsigned __int8 *)(v5 + 5);
              if ( v10 <= 6 )
              {
                v11 = 6;
              }
              else
              {
                v7 += *(unsigned __int8 *)(v5 + 6);
                if ( v10 <= 7 )
                {
                  v11 = 7;
                }
                else
                {
                  v7 += *(unsigned __int8 *)(v5 + 7);
                  if ( v10 <= 8 )
                  {
                    v11 = 8;
                  }
                  else
                  {
                    v7 += *(unsigned __int8 *)(v5 + 8);
                    if ( v10 <= 9 )
                    {
                      v11 = 9;
                    }
                    else
                    {
                      v7 += *(unsigned __int8 *)(v5 + 9);
                      if ( v10 <= 0xA )
                      {
                        v11 = 10;
                      }
                      else
                      {
                        v7 += *(unsigned __int8 *)(v5 + 10);
                        if ( v10 <= 0xB )
                        {
                          v11 = 11;
                        }
                        else
                        {
                          v7 += *(unsigned __int8 *)(v5 + 11);
                          if ( v10 <= 0xC )
                          {
                            v11 = 12;
                          }
                          else
                          {
                            v7 += *(unsigned __int8 *)(v5 + 12);
                            if ( v10 <= 0xD )
                            {
                              v11 = 13;
                            }
                            else
                            {
                              v7 += *(unsigned __int8 *)(v5 + 13);
                              if ( v10 <= 0xE )
                              {
                                v11 = 14;
                              }
                              else
                              {
                                v7 += *(unsigned __int8 *)(v5 + 14);
                                if ( v10 <= 0xF )
                                {
                                  v11 = 15;
                                }
                                else
                                {
                                  v7 += *(unsigned __int8 *)(v5 + 15);
                                  if ( v10 <= 0x10 )
                                  {
                                    v11 = 16;
                                  }
                                  else
                                  {
                                    v11 = 17;
                                    v7 += *(unsigned __int8 *)(v5 + 16);
                                  }
                                }
                              }
                            }
                          }
                        }
                      }
                    }
                  }
                }
              }
            }
          }
        }
      }
      if ( v8 == v10 )
        goto LABEL_57;
    }
    v12 = v8 - v10;
    v13 = v12 >> 4;
    v14 = 16 * (v12 >> 4);
    if ( !(v12 >> 4) )
      goto LABEL_40;
LABEL_34:
    v15 = (uint8x8_t *)(v10 + v5);
    if ( v13 <= 2 )
    {
      v16.n128_u64[0] = 0LL;
      v16.n128_u64[1] = 0LL;
      v18 = 0;
    }
    else
    {
      v16.n128_u64[0] = 0LL;
      v16.n128_u64[1] = 0LL;
      v17 = (int)&v15[4];
      v18 = 0;
      do
      {
        v19 = vmovl_u8(*(uint8x8_t *)(v17 - 32));
        v20 = vmovl_u8(*(uint8x8_t *)(v17 - 24));
        v21 = vmovl_u8(*(uint8x8_t *)(v17 - 16));
        v18 += 2;
        v22 = vaddq_s32(
                vmovl_u16(*(uint16x4_t *)&v20.n128_i8[8]),
                vaddq_s32(
                  vmovl_u16((uint16x4_t)v20.n128_u64[0]),
                  vaddq_s32(
                    vmovl_u16(*(uint16x4_t *)&v19.n128_i8[8]),
                    vaddq_s32(vmovl_u16((uint16x4_t)v19.n128_u64[0]), v16))));
        v23 = vmovl_u8(*(uint8x8_t *)(v17 - 8));
        v52 = (uint8x8_t *)v17;
        __pld((void *)(v17 + 192));
        v17 += 32;
        v16 = vaddq_s32(
                vmovl_u16(*(uint16x4_t *)&v23.n128_i8[8]),
                vaddq_s32(
                  vmovl_u16((uint16x4_t)v23.n128_u64[0]),
                  vaddq_s32(
                    vmovl_u16(*(uint16x4_t *)&v21.n128_i8[8]),
                    vaddq_s32(v22, vmovl_u16((uint16x4_t)v21.n128_u64[0])))));
      }
      while ( v18 != ((v13 - 3) & 0xFFFFFFFE) + 2 );
      v15 = v52;
    }
    do
    {
      v24.n64_u64[0] = v15->n64_u64[0];
      v25.n64_u64[0] = v15[1].n64_u64[0];
      v15 += 2;
      v26 = vmovl_u8(v24);
      v27 = vmovl_u8(v25);
      ++v18;
      v16 = vaddq_s32(
              vmovl_u16(*(uint16x4_t *)&v27.n128_i8[8]),
              vaddq_s32(
                vmovl_u16((uint16x4_t)v27.n128_u64[0]),
                vaddq_s32(
                  vmovl_u16(*(uint16x4_t *)&v26.n128_i8[8]),
                  vaddq_s32(vmovl_u16((uint16x4_t)v26.n128_u64[0]), v16))));
    }
    while ( v13 > v18 );
    v28.n64_u64[0] = vadd_s32((int32x2_t)v16.n128_u64[0], *(int32x2_t *)&v16.n128_i8[8]).n64_u64[0];
    v11 += v14;
    v7 += vpadd_s32(v28, v28).n64_u64[0];
    if ( v14 != v12 )
    {
LABEL_40:
      LOBYTE(v7) = v7 + *(_BYTE *)(v5 + v11);
      if ( v11 + 1 <= v9 )
      {
        LOBYTE(v7) = v7 + *(_BYTE *)(v5 + v11 + 1);
        if ( v9 >= v11 + 2 )
        {
          LOBYTE(v7) = v7 + *(_BYTE *)(v5 + v11 + 2);
          if ( v9 >= v11 + 3 )
          {
            LOBYTE(v7) = v7 + *(_BYTE *)(v5 + v11 + 3);
            if ( v9 >= v11 + 4 )
            {
              LOBYTE(v7) = v7 + *(_BYTE *)(v5 + v11 + 4);
              if ( v9 >= v11 + 5 )
              {
                LOBYTE(v7) = v7 + *(_BYTE *)(v5 + v11 + 5);
                if ( v9 >= v11 + 6 )
                {
                  LOBYTE(v7) = v7 + *(_BYTE *)(v5 + v11 + 6);
                  if ( v9 >= v11 + 7 )
                  {
                    LOBYTE(v7) = v7 + *(_BYTE *)(v5 + v11 + 7);
                    if ( v9 >= v11 + 8 )
                    {
                      LOBYTE(v7) = v7 + *(_BYTE *)(v5 + v11 + 8);
                      if ( v9 >= v11 + 9 )
                      {
                        LOBYTE(v7) = v7 + *(_BYTE *)(v5 + v11 + 9);
                        if ( v9 >= v11 + 10 )
                        {
                          LOBYTE(v7) = v7 + *(_BYTE *)(v5 + v11 + 10);
                          if ( v9 >= v11 + 11 )
                          {
                            LOBYTE(v7) = v7 + *(_BYTE *)(v5 + v11 + 11);
                            if ( v9 >= v11 + 12 )
                            {
                              v29 = v11 + 13;
                              LOBYTE(v7) = v7 + *(_BYTE *)(v5 + v11 + 12);
                              if ( v9 >= v11 + 13 )
                              {
                                v30 = v11 + 14;
                                v31 = *(_BYTE *)(v5 + v29);
                                v33 = __OFSUB__(v9, v30);
                                v32 = v9 - v30 < 0;
                                if ( v9 >= v30 )
                                  LOBYTE(v30) = *(_BYTE *)(v5 + v30);
                                LOBYTE(v7) = v7 + v31;
                                if ( !(v32 ^ v33) )
                                  LOBYTE(v7) = v7 + v30;
                              }
                            }
                          }
                        }
                      }
                    }
                  }
                }
              }
            }
          }
        }
      }
    }
LABEL_57:
    *(_BYTE *)(v5 + v6 + 2) = ~(_BYTE)v7;
    if ( v6 <= 28 )
      imp_SceSysclibForDriver_memset(v6 + 3 + v5, -1, 29 - v6);
LABEL_59:
    imp_SceSysclibForDriver_memset(v2 + 48, -1, 32);
    *(_DWORD *)(v2 + 92) = 0;
    *(_DWORD *)(v2 + 96) = 0;
    *(_DWORD *)(v2 + 100) = 0;
    *(_DWORD *)v2 = 0;
    *(_DWORD *)(v2 + 4) = v3 | 0x10000;
    *(_DWORD *)(v2 + 8) = -1;
    v34 = *(char *)v5;
    v35 = *(unsigned __int8 *)(v5 + 1);
    if ( v34 < 0 )
    {
      v49 = dword_8100C0A4;
      if ( v35 > 0xF )
        v35 = 16;
      *(_DWORD *)(v2 + 12) = v35;
      *(_DWORD *)(v2 + 108) = v49;
    }
    else
    {
      v35 = 17;
      *(_DWORD *)(v2 + 108) = 0;
      *(_DWORD *)(v2 + 12) = 17;
    }
    LOWORD(v36) = -15776;
    HIWORD(v36) = (unsigned int)&unk_8100C260 >> 16;
    v37 = imp_SceCpuForDriver_ksceKernelCpuSuspendIntr(v36);
    v38 = &dword_8100C000[v35];
    LOWORD(v41) = -16384;
    v39 = (int *)dword_8100C000[v35 + 1];
    v40 = v37;
    HIWORD(v41) = (unsigned int)dword_8100C000 >> 16;
    if ( v39 )
    {
      if ( v3 & 1 )
      {
        if ( !(v39[1] & 0x40060000) )
        {
          *(_DWORD *)v2 = v39;
          LOWORD(v43) = -16384;
          v42 = dword_8100C000[0];
          HIWORD(v43) = (unsigned int)dword_8100C000 >> 16;
          v38[1] = v2;
          if ( v42 )
            goto LABEL_86;
LABEL_66:
          if ( *((_BYTE *)v43 + 190) )
            goto LABEL_86;
          if ( v43[150] )
          {
            if ( *(_DWORD *)(v2 + 12) == 17 )
              v51 = 4;
            else
              v51 = v43[41];
            do
              v37 = imp_SceThreadmgrForDriver_ksceKernelGetSystemTimeLow(v37) - dword_8100C09C;
            while ( v37 <= v51 );
            dword_8100C000[0] = v2;
            sub_8100003C(v2);
            goto LABEL_86;
          }
          v44 = v43[18];
          if ( v44 )
          {
            if ( v2 != v44 )
            {
LABEL_70:
              if ( v35 == 17 || !(v3 & 1) )
                goto LABEL_74;
LABEL_72:
              v44 = dword_8100C000[v35 + 1];
              if ( v2 == v44 )
              {
                if ( !(v3 & 2) )
                {
                  v44 = v2;
                  goto LABEL_74;
                }
              }
              else if ( v44 )
              {
LABEL_74:
                v45 = 4;
                if ( dword_8100C25C )
                  v45 = dword_8100C0A0;
                do
                  v37 = imp_SceThreadmgrForDriver_ksceKernelGetSystemTimeLow(v37) - dword_8100C09C;
                while ( v37 <= v45 );
                dword_8100C000[0] = v44;
                sub_8100003C(v44);
                goto LABEL_86;
              }
LABEL_86:
              LOWORD(v48) = -15776;
              HIWORD(v48) = (unsigned int)&unk_8100C260 >> 16;
              imp_SceCpuForDriver_ksceKernelCpuResumeIntr(v48, v40);
              return exp_SceSysconForDriver_ksceSysconCmdSync(v2, 0);
            }
            if ( !(v3 & 2) )
            {
              v44 = v2;
              goto LABEL_70;
            }
          }
          if ( v35 == 17 )
            goto LABEL_86;
          goto LABEL_72;
        }
        v50 = *v39;
        *(_DWORD *)v2 = *v39;
        *v39 = v2;
        if ( !v50 )
          *(_DWORD *)(v41 + 4 * v35 + 76) = v2;
      }
      else
      {
        v47 = v41 + 4 * (v35 + 18);
        **(_DWORD **)(v47 + 4) = v2;
        *(_DWORD *)(v47 + 4) = v2;
      }
    }
    else
    {
      v38[1] = v2;
      *(_DWORD *)(v41 + 4 * v35 + 76) = v2;
    }
    v43 = dword_8100C000;
    if ( dword_8100C000[0] )
      goto LABEL_86;
    goto LABEL_66;
  }
  if ( !*(_DWORD *)(v2 + 84) )
    return -2145058814;
  if ( *(_DWORD *)(v2 + 88) > 0x20u )
    goto LABEL_9;
  return -2145058815;
}

signed int __fastcall sub_81002D20(_DWORD *a1, unsigned __int16 a2)
{
  _DWORD *v2; // r7
  int v3; // r5
  int v4; // r3
  signed int result; // r0
  int v6; // r3
  int v7; // r2
  unsigned int v8; // r0
  int v9; // r1
  BOOL v10; // r3
  int v11; // r4
  uint16x8_t v12; // q9
  uint16x8_t v13; // q10
  int32x4_t v14; // q9
  unsigned __int8 v15; // d16.b[0]
  unsigned __int8 v16; // r3
  char *v17; // r0
  int v18; // r1
  int v19; // r0
  int v20; // r1
  signed int v21; // r8
  unsigned int v22; // r0
  unsigned int v23; // r9
  int v24; // r1
  int v25; // r3
  int v26; // r3
  int v27; // r0
  int v28; // r3
  int v29; // r10
  unsigned int v30; // r8
  unsigned int v31; // r8
  int v32; // [sp+0h] [bp-B0h]
  int v33; // [sp+8h] [bp-A8h]
  int v34; // [sp+Ch] [bp-A4h]
  int v35; // [sp+10h] [bp-A0h]
  int v36; // [sp+14h] [bp-9Ch]
  uint8x8_t v37; // [sp+18h] [bp-98h]
  uint8x8_t v38; // [sp+20h] [bp-90h]
  char v39; // [sp+38h] [bp-78h]
  unsigned __int8 v40; // [sp+3Ah] [bp-76h]
  char v41; // [sp+3Ch] [bp-74h]
  int v42; // [sp+58h] [bp-58h]
  int v43; // [sp+5Ch] [bp-54h]
  int v44; // [sp+60h] [bp-50h]
  int v45; // [sp+64h] [bp-4Ch]
  int v46; // [sp+68h] [bp-48h]
  int v47; // [sp+6Ch] [bp-44h]
  int v48; // [sp+74h] [bp-3Ch]
  int v49; // [sp+8Ch] [bp-24h]
  char v50; // [sp+90h] [bp-20h]

  v2 = a1;
  v49 = MEMORY[0];
  if ( !a1 )
  {
    result = -2145058814;
    goto LABEL_35;
  }
  LOWORD(v3) = -16384;
  HIWORD(v3) = (unsigned int)dword_8100C000 >> 16;
  v37.n64_u16[0] = a2;
  v4 = *(_DWORD *)(v3 + 0x258);
  v37.n64_u8[2] = 1;
  if ( v4 )
  {
    if ( imp_SceIntrmgrForDriver_ksceKernelIsIntrAllowedInCurrentContext() )
    {
      result = -2145058811;
      goto LABEL_35;
    }
  }
  else
  {
    result = imp_SceThreadmgrForDriver_ksceKernelCheckWaitableStatus();
    if ( result < 0 )
      goto LABEL_35;
  }
  v6 = *(unsigned __int8 *)(v3 + 0xBF);
  if ( *(_BYTE *)(v3 + 0xBF) )
  {
    result = -2145058558;
    goto LABEL_35;
  }
  v7 = v37.n64_u8[2];
  v42 = *(unsigned __int8 *)(v3 + 0xBF);
  if ( (unsigned int)v37.n64_u8[2] - 1 > 0x1C )
  {
    result = -2145058810;
    goto LABEL_35;
  }
  v8 = v37.n64_u8[2] + 2;
  v43 = v6;
  v9 = v8 & 0x1F0;
  v44 = v6;
  v10 = v9 == 0;
  v11 = v37.n64_u8[2] + 1;
  if ( v8 <= 0xF )
    v10 = 1;
  if ( v10 )
  {
    v16 = v37.n64_u8[0];
    v18 = 1;
  }
  else
  {
    v12 = vmovl_u8(v37);
    v13 = vmovl_u8(v38);
    v14 = vaddq_s32(
            vmovl_u16(*(uint16x4_t *)&v13.n128_i8[8]),
            vaddq_s32(
              vmovl_u16((uint16x4_t)v13.n128_u64[0]),
              vaddq_s32(vmovl_u16((uint16x4_t)v12.n128_u64[0]), vmovl_u16(*(uint16x4_t *)&v12.n128_i8[8]))));
    v14.n128_u64[0] = vadd_s32((int32x2_t)v14.n128_u64[0], *(int32x2_t *)&v14.n128_i8[8]).n64_u64[0];
    v15 = vpadd_s32((int32x2_t)v14.n128_u64[0], (int32x2_t)v14.n128_u64[0]).n64_u64[0];
    v16 = v15;
    if ( v9 == v8 )
      goto LABEL_25;
    v17 = &v50 + v9;
    v18 = v9 + 1;
    v16 = v15 + *(v17 - 120);
    if ( v18 > v11 )
      goto LABEL_25;
  }
  v16 += *(&v50 + v18 - 120);
  if ( v11 >= v18 + 1 )
  {
    v16 += *(&v50 + v18 - 119);
    if ( v11 >= v18 + 2 )
    {
      v16 += *(&v50 + v18 - 118);
      if ( v11 >= v18 + 3 )
      {
        v16 += *(&v50 + v18 - 117);
        if ( v11 >= v18 + 4 )
        {
          v16 += *(&v50 + v18 - 116);
          if ( v11 >= v18 + 5 )
          {
            v16 += *(&v50 + v18 - 115);
            if ( v11 >= v18 + 6 )
            {
              v16 += *(&v50 + v18 - 114);
              if ( v11 >= v18 + 7 )
              {
                v16 += *(&v50 + v18 - 113);
                if ( v11 >= v18 + 8 )
                {
                  v16 += *(&v50 + v18 - 112);
                  if ( v11 >= v18 + 9 )
                  {
                    v16 += *(&v50 + v18 - 111);
                    if ( v11 >= v18 + 10 )
                    {
                      v16 += *(&v50 + v18 - 110);
                      if ( v11 >= v18 + 11 )
                      {
                        v19 = v18 + 12;
                        v16 += *(&v50 + v18 - 109);
                        if ( v11 >= v18 + 12 )
                        {
                          v20 = v18 + 13;
                          v16 += *(&v50 + v19 - 120);
                          if ( v11 >= v20 )
                            v16 += *(&v50 + v20 - 120);
                        }
                      }
                    }
                  }
                }
              }
            }
          }
        }
      }
    }
  }
LABEL_25:
  *(&v50 + v37.n64_u8[2] - 118) = ~v16;
  if ( v7 != 29 )
    imp_SceSysclibForDriver_memset((char *)&v33 + v7 + 19, -1, 29 - v7);
  imp_SceSysclibForDriver_memset(&v39, -1, 32);
  v45 = 0;
  v33 = 0;
  v46 = 0;
  v47 = 0;
  v34 = 0x10000;
  v35 = -1;
  v21 = v37.n64_u8[1];
  if ( v37.n64_i8[0] < 0 )
  {
    v28 = *(_DWORD *)(v3 + 0xA4);
    if ( v37.n64_u8[1] > 0xFu )
      v21 = 16;
    v36 = v21;
    v48 = v28;
  }
  else
  {
    v48 = 0;
    v21 = 17;
    v36 = 17;
  }
  v22 = imp_SceCpuForDriver_ksceKernelCpuSuspendIntr(&unk_8100C260);
  LOWORD(v25) = -16384;
  v23 = v22;
  v24 = v3 + 4 * v21;
  HIWORD(v25) = (unsigned int)dword_8100C000 >> 16;
  if ( *(_DWORD *)(v24 + 4) )
  {
    v26 = v25 + 4 * (v21 + 18);
    **(_DWORD **)(v26 + 4) = &v33;
    *(_DWORD *)(v26 + 4) = &v33;
  }
  else
  {
    *(_DWORD *)(v24 + 4) = &v33;
    *(_DWORD *)(v25 + 4 * v21 + 76) = &v33;
  }
  if ( *(_DWORD *)v3 || byte_8100C0BE )
    goto LABEL_32;
  if ( dword_8100C258 )
  {
    if ( v36 == 17 )
      v31 = 4;
    else
      v31 = dword_8100C0A4;
    do
      v22 = imp_SceThreadmgrForDriver_ksceKernelGetSystemTimeLow(v22) - *(_DWORD *)(v3 + 0x9C);
    while ( v22 <= v31 );
    dword_8100C000[0] = (int)&v33;
    sub_8100003C((int)&v33);
  }
  else
  {
    v29 = dword_8100C048;
    if ( v21 != 17 )
    {
      if ( dword_8100C048 )
      {
LABEL_47:
        v30 = 4;
        if ( *(_DWORD *)(v3 + 0x25C) )
          v30 = dword_8100C0A0;
        do
          v22 = imp_SceThreadmgrForDriver_ksceKernelGetSystemTimeLow(v22) - *(_DWORD *)(v3 + 0x9C);
        while ( v22 <= v30 );
        dword_8100C000[0] = v29;
        sub_8100003C(v29);
        goto LABEL_32;
      }
      v29 = dword_8100C000[v21 + 1];
    }
    if ( v29 )
      goto LABEL_47;
  }
LABEL_32:
  LOWORD(v27) = -15776;
  HIWORD(v27) = (unsigned int)&unk_8100C260 >> 16;
  imp_SceCpuForDriver_ksceKernelCpuResumeIntr(v27, v23);
  result = exp_SceSysconForDriver_ksceSysconCmdSync((unsigned int)&v33, 0);
  if ( result >= 0 )
  {
    if ( (unsigned int)v40 - 3 > 3 )
    {
      result = -2145058560;
    }
    else
    {
      v32 = 0;
      imp_SceSysclibForDriver_memcpy(&v32, &v41, v40 - 2);
      result = 0;
      *v2 = v32;
    }
  }
LABEL_35:
  if ( v49 != MEMORY[0] )
    imp_SceSysclibForDriver___stack_chk_fail(result);
  return result;
}

int __fastcall sub_810030C8(unsigned int a1, unsigned __int16 a2, unsigned __int8 a3)
{
  int v3; // r4
  unsigned int v4; // lr
  unsigned int v5; // r7
  unsigned int v6; // r6
  int v7; // r0
  int v8; // r3
  int v9; // r2
  unsigned int v10; // r6
  int v11; // r1
  BOOL v12; // r3
  int v13; // r0
  uint16x8_t v14; // q9
  uint16x8_t v15; // q10
  int32x4_t v16; // q9
  unsigned __int8 v17; // d16.b[0]
  unsigned __int8 v18; // r3
  char *v19; // r6
  int v20; // r1
  int v21; // r6
  int v22; // r1
  unsigned int v23; // r7
  int v24; // r3
  unsigned int v25; // r0
  unsigned int v26; // r8
  int v27; // r2
  int v28; // r3
  int v29; // r3
  int v30; // r0
  signed int v31; // r0
  int result; // r0
  int v33; // r9
  unsigned int v34; // r7
  unsigned int v35; // r7
  int v36; // [sp+0h] [bp-A8h]
  int v37; // [sp+4h] [bp-A4h]
  int v38; // [sp+8h] [bp-A0h]
  unsigned int v39; // [sp+Ch] [bp-9Ch]
  uint8x8_t v40; // [sp+10h] [bp-98h]
  uint8x8_t v41; // [sp+18h] [bp-90h]
  char v42; // [sp+30h] [bp-78h]
  int v43; // [sp+50h] [bp-58h]
  int v44; // [sp+54h] [bp-54h]
  int v45; // [sp+58h] [bp-50h]
  int v46; // [sp+5Ch] [bp-4Ch]
  int v47; // [sp+60h] [bp-48h]
  int v48; // [sp+64h] [bp-44h]
  int v49; // [sp+6Ch] [bp-3Ch]
  int v50; // [sp+84h] [bp-24h]
  char v51; // [sp+88h] [bp-20h]

  LOWORD(v3) = -16384;
  HIWORD(v3) = (unsigned int)dword_8100C000 >> 16;
  v4 = a1 >> 8;
  v5 = a1 >> 16;
  v6 = a1 >> 24;
  v40.n64_u8[3] = a1;
  v7 = *(_DWORD *)(v3 + 0x258);
  v40.n64_u16[0] = a2;
  v40.n64_u8[2] = a3;
  v50 = MEMORY[0];
  v40.n64_u8[4] = v4;
  v40.n64_u8[5] = v5;
  v40.n64_u8[6] = v6;
  if ( v7 )
  {
    if ( imp_SceIntrmgrForDriver_ksceKernelIsIntrAllowedInCurrentContext() )
    {
      result = -2145058811;
      goto LABEL_37;
    }
  }
  else
  {
    result = imp_SceThreadmgrForDriver_ksceKernelCheckWaitableStatus();
    if ( result < 0 )
      goto LABEL_37;
  }
  v8 = *(unsigned __int8 *)(v3 + 0xBF);
  if ( *(_BYTE *)(v3 + 0xBF) )
  {
    result = -2145058558;
  }
  else
  {
    v9 = v40.n64_u8[2];
    v43 = *(unsigned __int8 *)(v3 + 0xBF);
    if ( (unsigned int)v40.n64_u8[2] - 1 <= 0x1C )
    {
      v10 = v40.n64_u8[2] + 2;
      v44 = v8;
      v11 = v10 & 0x1F0;
      v45 = v8;
      v12 = v11 == 0;
      v13 = v40.n64_u8[2] + 1;
      if ( v10 <= 0xF )
        v12 = 1;
      if ( v12 )
      {
        v18 = v40.n64_u8[0];
        v20 = 1;
      }
      else
      {
        v14 = vmovl_u8(v40);
        v15 = vmovl_u8(v41);
        v16 = vaddq_s32(
                vmovl_u16(*(uint16x4_t *)&v15.n128_i8[8]),
                vaddq_s32(
                  vmovl_u16((uint16x4_t)v15.n128_u64[0]),
                  vaddq_s32(vmovl_u16((uint16x4_t)v14.n128_u64[0]), vmovl_u16(*(uint16x4_t *)&v14.n128_i8[8]))));
        v16.n128_u64[0] = vadd_s32((int32x2_t)v16.n128_u64[0], *(int32x2_t *)&v16.n128_i8[8]).n64_u64[0];
        v17 = vpadd_s32((int32x2_t)v16.n128_u64[0], (int32x2_t)v16.n128_u64[0]).n64_u64[0];
        v18 = v17;
        if ( v11 == v10 )
          goto LABEL_24;
        v19 = &v51 + v11;
        v20 = v11 + 1;
        v18 = v17 + *(v19 - 120);
        if ( v20 > v13 )
          goto LABEL_24;
      }
      v18 += *(&v51 + v20 - 120);
      if ( v13 >= v20 + 1 )
      {
        v18 += *(&v51 + v20 - 119);
        if ( v13 >= v20 + 2 )
        {
          v18 += *(&v51 + v20 - 118);
          if ( v13 >= v20 + 3 )
          {
            v18 += *(&v51 + v20 - 117);
            if ( v13 >= v20 + 4 )
            {
              v18 += *(&v51 + v20 - 116);
              if ( v13 >= v20 + 5 )
              {
                v18 += *(&v51 + v20 - 115);
                if ( v13 >= v20 + 6 )
                {
                  v18 += *(&v51 + v20 - 114);
                  if ( v13 >= v20 + 7 )
                  {
                    v18 += *(&v51 + v20 - 113);
                    if ( v13 >= v20 + 8 )
                    {
                      v18 += *(&v51 + v20 - 112);
                      if ( v13 >= v20 + 9 )
                      {
                        v18 += *(&v51 + v20 - 111);
                        if ( v13 >= v20 + 10 )
                        {
                          v18 += *(&v51 + v20 - 110);
                          if ( v13 >= v20 + 11 )
                          {
                            v21 = v20 + 12;
                            v18 += *(&v51 + v20 - 109);
                            if ( v13 >= v20 + 12 )
                            {
                              v22 = v20 + 13;
                              v18 += *(&v51 + v21 - 120);
                              if ( v13 >= v22 )
                                v18 += *(&v51 + v22 - 120);
                            }
                          }
                        }
                      }
                    }
                  }
                }
              }
            }
          }
        }
      }
LABEL_24:
      *(&v51 + v40.n64_u8[2] - 118) = ~v18;
      if ( v9 != 29 )
        imp_SceSysclibForDriver_memset((char *)&v36 + v9 + 19, -1, 29 - v9);
      imp_SceSysclibForDriver_memset(&v42, -1, 32);
      v37 = 0x10000;
      v38 = -1;
      v23 = v40.n64_u8[1];
      v46 = 0;
      v36 = 0;
      if ( v40.n64_i8[0] >= 0 )
        v23 = 17;
      v47 = 0;
      v48 = 0;
      if ( v40.n64_i8[0] < 0 )
      {
        v24 = *(_DWORD *)(v3 + 0xA4);
        if ( v23 > 0xF )
          v23 = 16;
        v39 = v23;
        v49 = v24;
      }
      else
      {
        v49 = 0;
        v39 = v23;
      }
      v25 = imp_SceCpuForDriver_ksceKernelCpuSuspendIntr(&unk_8100C260);
      LOWORD(v28) = -16384;
      v26 = v25;
      v27 = v3 + 4 * v23;
      HIWORD(v28) = (unsigned int)dword_8100C000 >> 16;
      if ( *(_DWORD *)(v27 + 4) )
      {
        v29 = v28 + 4 * (v23 + 18);
        **(_DWORD **)(v29 + 4) = &v36;
        *(_DWORD *)(v29 + 4) = &v36;
      }
      else
      {
        *(_DWORD *)(v27 + 4) = &v36;
        *(_DWORD *)(v28 + 4 * v23 + 76) = &v36;
      }
      if ( *(_DWORD *)v3 || byte_8100C0BE )
        goto LABEL_36;
      if ( dword_8100C258 )
      {
        if ( v39 == 17 )
          v35 = 4;
        else
          v35 = dword_8100C0A4;
        do
          v25 = imp_SceThreadmgrForDriver_ksceKernelGetSystemTimeLow(v25) - *(_DWORD *)(v3 + 0x9C);
        while ( v25 <= v35 );
        dword_8100C000[0] = (int)&v36;
        sub_8100003C((int)&v36);
      }
      else
      {
        v33 = dword_8100C048;
        if ( v23 != 17 )
        {
          if ( dword_8100C048 )
          {
LABEL_46:
            v34 = 4;
            if ( *(_DWORD *)(v3 + 0x25C) )
              v34 = dword_8100C0A0;
            do
              v25 = imp_SceThreadmgrForDriver_ksceKernelGetSystemTimeLow(v25) - *(_DWORD *)(v3 + 0x9C);
            while ( v25 <= v34 );
            dword_8100C000[0] = v33;
            sub_8100003C(v33);
            goto LABEL_36;
          }
          v33 = dword_8100C000[v23 + 1];
        }
        if ( v33 )
          goto LABEL_46;
      }
LABEL_36:
      LOWORD(v30) = -15776;
      HIWORD(v30) = (unsigned int)&unk_8100C260 >> 16;
      imp_SceCpuForDriver_ksceKernelCpuResumeIntr(v30, v26);
      v31 = exp_SceSysconForDriver_ksceSysconCmdSync((unsigned int)&v36, 0);
      result = v31 & (v31 >> 31);
      goto LABEL_37;
    }
    result = -2145058810;
  }
LABEL_37:
  if ( v50 != MEMORY[0] )
    imp_SceSysclibForDriver___stack_chk_fail(result);
  return result;
}

#pragma endregion "Command execution"




#pragma region "Get internal state variables"

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0xEBE3262C]
int exp_SceSysconForDriver_0xEBE3262C()
{
  return (unsigned __int8)byte_8100C0BC;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0xB7BCC638]
int exp_SceSysconForDriver_0xB7BCC638()
{
  return dword_8100C0A8;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x9DA2A5AB]
unsigned int exp_SceSysconForDriver_0x9DA2A5AB()
{
  return ((unsigned int)(unsigned __int8)byte_8100C0BC >> 3) & 1;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x9ADD60D2]
unsigned int exp_SceSysconForDriver_ksceSysconIsDownLoaderMode()
{
  return ((unsigned int)(unsigned __int8)byte_8100C0BC >> 6) & 1;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x2E4A2198]
int exp_SceSysconForDriver_0x2E4A2198()
{
  return dword_8100C0B4;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x1E08A40F]
int exp_SceSysconForDriver_0x1E08A40F()
{
  return dword_8100C0B8;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0xC0F215B7]
int exp_SceSysconForDriver_0xC0F215B7()
{
  return byte_8100C0C2;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0xC3504ADE]
int exp_SceSysconForDriver_0xC3504ADE()
{
  return byte_8100C0C3;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0xB832B72C]
int exp_SceSysconForDriver_0xB832B72C()
{
  return byte_8100C0C4;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x86BAAF7D]
int exp_SceSysconForDriver_0x86BAAF7D()
{
  return byte_8100C0C5;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0xA57B5433]
int exp_SceSysconForDriver_0xA57B5433()
{
  return byte_8100C0C6;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0xACC7F71E]
int exp_SceSysconForDriver_0xACC7F71E()
{
  return byte_8100C0C7 & ~(byte_8100C0C7 >> 31);
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x769F9AC4]
int exp_SceSysconForDriver_0x769F9AC4()
{
  int result; // r0

  if ( imp_SceSblAIMgrForDriver_ksceSblAimgrIsDolce() )
    result = 1;
  else
    result = byte_8100C0C8 & ~(byte_8100C0C8 >> 31);
  return result;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x4BC63A40]
int exp_SceSysconForDriver_0x4BC63A40()
{
  return byte_8100C0C9;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x99A254A9]
int exp_SceSysconForDriver_0x99A254A9()
{
  return byte_8100C0CA;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x9A4F4B7C]
int exp_SceSysconForDriver_0x9A4F4B7C()
{
  return byte_8100C0CB;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x92D2C6A4]
int exp_SceSysconForDriver_0x92D2C6A4()
{
  return byte_8100C0CC;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0xB9EA2FA8]
int exp_SceSysconForDriver_0xB9EA2FA8()
{
  return byte_8100C0CD;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0xD7F5A797]
int exp_SceSysconForDriver_0xD7F5A797()
{
  return byte_8100C0CE;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x27758A64]
int exp_SceSysconForDriver_0x27758A64()
{
  return byte_8100C0CF;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x4A184B7C]
int exp_SceSysconForDriver_0x4A184B7C()
{
  return byte_8100C0D0;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0xACEE1C70]
int exp_SceSysconForDriver_0xACEE1C70()
{
  return byte_8100C0D1;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x03C50DC3]
int exp_SceSysconForDriver_0x03C50DC3()
{
  return byte_8100C0D2;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0xBFDA5590]
int exp_SceSysconForDriver_0xBFDA5590()
{
  return byte_8100C0D3;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x63B14156]
int exp_SceSysconForDriver_0x63B14156()
{
  return byte_8100C0D4;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x50CAE242]
int exp_SceSysconForDriver_0x50CAE242()
{
  return byte_8100C0D5;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x29CF4335]
int exp_SceSysconForDriver_0x29CF4335()
{
  return byte_8100C0D6;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x9F4042F8]
int exp_SceSysconForDriver_0x9F4042F8()
{
  return byte_8100C0D7;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0xC50568E9]
int exp_SceSysconForDriver_0xC50568E9()
{
  return byte_8100C0D8;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x1A0C140F]
int exp_SceSysconForDriver_ksceSysconIsLowBatteryInhibitUpdateReboot()
{
  return byte_8100C0D9;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x1E3130EE]
int exp_SceSysconForDriver_ksceSysconIsLowBatteryInhibitUpdateDownload()
{
  return byte_8100C0DA;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x012B57B3]
int exp_SceSysconForDriver_0x012B57B3()
{
  return byte_8100C0DB;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x32B2DB3D]
int exp_SceSysconForDriver_0x32B2DB3D()
{
  return byte_8100C0DC;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x142D5E82]
int exp_SceSysconForDriver_0x142D5E82()
{
  return byte_8100C0DD;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x490C5548]
int exp_SceSysconForDriver_0x490C5548()
{
  return byte_8100C0DE;
}

#pragma endregion "Get internal state variables"



#pragma region "Command Wrappers"

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x0D0B6D25]
signed int exp_SceSysconForDriver_0x0D0B6D25()
{
  signed int result; // r0
  int v1; // [sp+0h] [bp-98h]
  char v2; // [sp+10h] [bp-88h]
  char v3; // [sp+11h] [bp-87h]
  char v4; // [sp+12h] [bp-86h]
  int v5; // [sp+84h] [bp-14h]

  v2 = 0;
  v3 = 0;
  v4 = 1;
  v5 = MEMORY[0];
  result = exp_SceSysconForDriver_ksceSysconCmdExec((int)&v1, 0);
  if ( v5 != MEMORY[0] )
    imp_SceSysclibForDriver___stack_chk_fail(result);
  return result;
}

signed int __fastcall sub_810036A8(_DWORD *a1)
{
  return sub_81002D20(a1, 3u);
}

signed int __fastcall sub_810036B4(_DWORD *a1)
{
  return sub_81002D20(a1, 1u);
}

signed int __fastcall sub_810036C0(int a1)
{
  int v1; // r5
  signed int v2; // r6
  signed int v3; // r0
  int v4; // r1
  int v5; // r2
  signed int result; // r0
  int v7; // [sp+0h] [bp-98h]
  char v8; // [sp+10h] [bp-88h]
  char v9; // [sp+11h] [bp-87h]
  char v10; // [sp+12h] [bp-86h]
  int v11; // [sp+34h] [bp-64h]
  int v12; // [sp+38h] [bp-60h]
  int v13; // [sp+3Ch] [bp-5Ch]
  int v14; // [sp+84h] [bp-14h]

  v1 = a1;
  v14 = MEMORY[0];
  if ( a1 )
  {
    v2 = 0;
    v9 = 0;
    v8 = 2;
    v10 = 1;
    v3 = exp_SceSysconForDriver_ksceSysconCmdExec((int)&v7, 0);
    if ( v3 >= 0 )
    {
      *(_BYTE *)(v1 + 12) = 0;
      v4 = v12;
      v5 = v13;
      *(_DWORD *)v1 = v11;
      *(_DWORD *)(v1 + 4) = v4;
      *(_DWORD *)(v1 + 8) = v5;
    }
    else
    {
      v2 = v3;
    }
  }
  else
  {
    v2 = -2145058814;
  }
  result = v2;
  if ( v14 != MEMORY[0] )
    imp_SceSysclibForDriver___stack_chk_fail(v2);
  return result;
}

int __fastcall sub_8100371C(unsigned int a1)
{
  return sub_810030C8(a1, 0x80u, 3u);
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0xCF5B2F2F]
signed int __fastcall exp_SceSysconForDriver_0xCF5B2F2F(_DWORD *a1)
{
  return sub_81002D20(a1, 0x10u);
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x93075DD1]
signed int __fastcall exp_SceSysconForDriver_0x93075DD1(_DWORD *a1)
{
  return sub_81002D20(a1, 0x14u);
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x3E09A1F4]
signed int __fastcall exp_SceSysconForDriver_ksceSysconGetManufacturesStatus(_DWORD *a1)
{
  return sub_81002D20(a1, 0x15u);
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0xD7BEFF8B]
signed int __fastcall exp_SceSysconForDriver_0xD7BEFF8B(_DWORD *a1)
{
  return sub_81002D20(a1, 0x11u);
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x9B6A6F64]
int __fastcall exp_SceSysconForDriver_0x9B6A6F64(unsigned int a1)
{
  return sub_810030C8(a1, 0x81u, 5u);
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x3168F3AF]
signed int __fastcall exp_SceSysconForDriver_0x3168F3AF(_DWORD *a1, _DWORD *a2)
{
  _DWORD *v2; // r6
  _DWORD *v3; // r7
  signed int result; // r0
  int v5; // r3
  bool v6; // cf
  bool v7; // zf
  int v8; // [sp+0h] [bp-A0h]
  char v9; // [sp+10h] [bp-90h]
  char v10; // [sp+11h] [bp-8Fh]
  char v11; // [sp+12h] [bp-8Eh]
  unsigned __int8 v12; // [sp+32h] [bp-6Eh]
  int v13; // [sp+34h] [bp-6Ch]
  unsigned __int8 v14; // [sp+38h] [bp-68h]
  int v15; // [sp+84h] [bp-1Ch]

  v2 = a1;
  v3 = a2;
  v15 = MEMORY[0];
  if ( a1 )
  {
    v10 = 0;
    v9 = 18;
    v11 = 1;
    result = exp_SceSysconForDriver_ksceSysconCmdExec((int)&v8, 0);
    if ( result >= 0 )
    {
      *v2 = v13;
      if ( v3 )
      {
        v5 = v12;
        v6 = v12 >= 6u;
        v7 = v12 == 6;
        if ( v12 <= 6u )
        {
          result = 0;
          *v3 = 0;
        }
        else
        {
          v5 = v14;
          result = 0;
        }
        if ( !v7 && v6 )
          *v3 = v5;
      }
      else
      {
        result = 0;
      }
    }
  }
  else
  {
    result = -2145058814;
  }
  if ( v15 != MEMORY[0] )
    imp_SceSysclibForDriver___stack_chk_fail(result);
  return result;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x51164951]
int __fastcall exp_SceSysconForDriver_0x51164951(unsigned int a1, char a2)
{
  unsigned int v2; // r5
  char v3; // r6
  signed int v4; // r0
  int result; // r0
  int v6; // [sp+0h] [bp-A0h]
  char v7; // [sp+10h] [bp-90h]
  char v8; // [sp+11h] [bp-8Fh]
  char v9; // [sp+12h] [bp-8Eh]
  __int16 v10; // [sp+13h] [bp-8Dh]
  char v11; // [sp+15h] [bp-8Bh]
  char v12; // [sp+16h] [bp-8Ah]
  char v13; // [sp+17h] [bp-89h]
  int v14; // [sp+84h] [bp-1Ch]

  v2 = a1;
  v3 = a2;
  v14 = MEMORY[0];
  if ( (unsigned int)exp_SceSysconForDriver_ksceSysconGetBaryonVersion() <= 0x90906 )
  {
    result = sub_810030C8(v2, 0x82u, 5u);
  }
  else
  {
    v10 = v2;
    v13 = v3;
    v8 = 0;
    v11 = BYTE2(v2);
    v12 = HIBYTE(v2);
    v7 = -126;
    v9 = 6;
    v4 = exp_SceSysconForDriver_ksceSysconCmdExec((int)&v6, 0);
    result = v4 & (v4 >> 31);
  }
  if ( v14 != MEMORY[0] )
    imp_SceSysclibForDriver___stack_chk_fail(result);
  return result;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0xBA09F171]
signed int __fastcall exp_SceSysconForDriver_0xBA09F171(_DWORD *a1, _DWORD *a2)
{
  _DWORD *v2; // r6
  _DWORD *v3; // r5
  signed int result; // r0
  unsigned __int8 v5; // [sp+0h] [bp-18h]
  unsigned __int8 v6; // [sp+1h] [bp-17h]
  int v7; // [sp+4h] [bp-14h]

  v2 = a1;
  v3 = a2;
  v7 = MEMORY[0];
  result = sub_81002D20(&v5, 0x13u);
  if ( result >= 0 )
  {
    if ( v2 )
      *v2 = v5;
    if ( v3 )
    {
      result = 0;
      *v3 = v6;
    }
    else
    {
      result = 0;
    }
  }
  if ( v7 != MEMORY[0] )
    imp_SceSysclibForDriver___stack_chk_fail(result);
  return result;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x373ECF8A]
int __fastcall exp_SceSysconForDriver_0x373ECF8A(unsigned int a1)
{
  return sub_810030C8(a1, 0x83u, 3u);
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x2659535C]
int exp_SceSysconForDriver_0x2659535C()
{
  return sub_810030C8(0, 0x84u, 1u);
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x4295D497]
int exp_SceSysconForDriver_0x4295D497()
{
  return sub_810030C8(0, 0x85u, 1u);
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x701535FC]
signed int __fastcall exp_SceSysconForDriver_ksceSysconGetLogInfo(_DWORD *a1)
{
  signed int v1; // r5
  _DWORD *v2; // r6
  signed int v3; // r0
  int v4; // r1
  char *v5; // r3
  bool v6; // nf
  int v7; // r2
  signed int result; // r0
  int v9; // r3
  int v10; // [sp+0h] [bp-A0h]
  char v11; // [sp+10h] [bp-90h]
  char v12; // [sp+11h] [bp-8Fh]
  char v13; // [sp+12h] [bp-8Eh]
  char v14; // [sp+34h] [bp-6Ch]
  int v15; // [sp+84h] [bp-1Ch]

  v1 = 0;
  v2 = a1;
  v13 = 1;
  v12 = 0;
  v11 = -122;
  v15 = MEMORY[0];
  v3 = exp_SceSysconForDriver_ksceSysconCmdExec((int)&v10, 0);
  v6 = v3 < 0;
  v7 = v15;
  if ( v3 < 0 )
    v1 = v3;
  else
    v5 = &v14;
  if ( v3 >= 0 )
  {
    v4 = *((_DWORD *)v5 + 1);
    *v2 = *(_DWORD *)v5;
  }
  result = v1;
  v9 = MEMORY[0];
  if ( !v6 )
    v2[1] = v4;
  if ( v7 != v9 )
    imp_SceSysclibForDriver___stack_chk_fail(v1);
  return result;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x4E55CF5E]
int exp_SceSysconForDriver_ksceSysconLogStart()
{
  return sub_810030C8(0, 0x87u, 1u);
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x487D97F3]
signed int __fastcall exp_SceSysconForDriver_ksceSysconLogReadData(__int16 a1, int a2, signed int a3)
{
  signed int v3; // r6
  int v4; // r7
  signed int result; // r0
  signed int v6; // r5
  int v7; // [sp+0h] [bp-A0h]
  char v8; // [sp+10h] [bp-90h]
  char v9; // [sp+11h] [bp-8Fh]
  char v10; // [sp+12h] [bp-8Eh]
  __int16 v11; // [sp+13h] [bp-8Dh]
  unsigned __int8 v12; // [sp+36h] [bp-6Ah]
  char v13; // [sp+37h] [bp-69h]
  int v14; // [sp+84h] [bp-1Ch]

  v3 = a3;
  v4 = a2;
  v14 = MEMORY[0];
  if ( (unsigned int)(a3 - 1) > 0x17 )
  {
    result = -2145058815;
  }
  else
  {
    v11 = a1;
    v9 = 0;
    v8 = -120;
    v10 = 3;
    result = exp_SceSysconForDriver_ksceSysconCmdExec((int)&v7, 0);
    if ( result >= 0 )
    {
      v6 = v12;
      if ( v12 > v3 )
      {
        result = -2145058560;
      }
      else
      {
        imp_SceSysclibForDriver_memcpy(v4, &v13, v12);
        result = v6;
      }
    }
  }
  if ( v14 != MEMORY[0] )
    imp_SceSysclibForDriver___stack_chk_fail(result);
  return result;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x9C0B1E61]
int exp_SceSysconForDriver_ksceSysconLogStartWaiting()
{
  return sub_810030C8(0, 0x89u, 1u);
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x299B1CE7]
signed int __fastcall exp_SceSysconForDriver_ksceSysconReadCommand(int a1, int a2, int a3)
{
  int v3; // r5
  int v4; // r7
  signed int result; // r0
  int v6; // [sp+0h] [bp-A0h]
  char v7; // [sp+10h] [bp-90h]
  char v8; // [sp+11h] [bp-8Fh]
  char v9; // [sp+12h] [bp-8Eh]
  __int16 v10; // [sp+13h] [bp-8Dh]
  char v11; // [sp+15h] [bp-8Bh]
  char v12; // [sp+34h] [bp-6Ch]
  int v13; // [sp+84h] [bp-1Ch]

  v3 = a3;
  v4 = a2;
  v13 = MEMORY[0];
  if ( (unsigned int)(a3 - 1) > 0x17 || (unsigned int)(a3 + a1) > 0x100 )
  {
    result = -2145058815;
  }
  else
  {
    v10 = a1;
    v11 = a3;
    v8 = 0;
    v7 = -112;
    v9 = 4;
    result = exp_SceSysconForDriver_ksceSysconCmdExec((int)&v6, 0);
    if ( result >= 0 )
    {
      imp_SceSysclibForDriver_memcpy(v4, &v12, v3);
      result = 0;
    }
  }
  if ( v13 != MEMORY[0] )
    imp_SceSysclibForDriver___stack_chk_fail(result);
  return result;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0xE26488B9]
int __fastcall exp_SceSysconForDriver_ksceSysconSendCommand(int a1, int a2, int a3)
{
  signed int v3; // r0
  int result; // r0
  int v5; // [sp+0h] [bp-A0h]
  char v6; // [sp+10h] [bp-90h]
  char v7; // [sp+11h] [bp-8Fh]
  char v8; // [sp+12h] [bp-8Eh]
  __int16 v9; // [sp+13h] [bp-8Dh]
  char v10; // [sp+15h] [bp-8Bh]
  char v11; // [sp+16h] [bp-8Ah]
  int v12; // [sp+84h] [bp-1Ch]

  v12 = MEMORY[0];
  if ( (unsigned int)(a3 - 1) > 0x17 || (unsigned int)(a3 + a1) > 0x100 )
  {
    result = -2145058815;
  }
  else
  {
    v9 = a1;
    v10 = a3;
    v8 = a3 + 4;
    v6 = -111;
    v7 = 0;
    imp_SceSysclibForDriver_memcpy(&v11, a2, a3);
    v3 = exp_SceSysconForDriver_ksceSysconCmdExec((int)&v5, 0);
    result = v3 & (v3 >> 31);
  }
  if ( v12 != MEMORY[0] )
    imp_SceSysclibForDriver___stack_chk_fail(result);
  return result;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0xBF5CDB61]
signed int __fastcall exp_SceSysconForDriver_0xBF5CDB61(int a1, int a2, int a3)
{
  int v3; // r5
  int v4; // r7
  signed int result; // r0
  int v6; // [sp+0h] [bp-A0h]
  char v7; // [sp+10h] [bp-90h]
  char v8; // [sp+11h] [bp-8Fh]
  char v9; // [sp+12h] [bp-8Eh]
  __int16 v10; // [sp+13h] [bp-8Dh]
  char v11; // [sp+15h] [bp-8Bh]
  char v12; // [sp+34h] [bp-6Ch]
  int v13; // [sp+84h] [bp-1Ch]

  v3 = a3;
  v4 = a2;
  v13 = MEMORY[0];
  if ( (unsigned int)(a3 - 1) > 0x17 || (unsigned int)(a3 + a1) > 0x80 )
  {
    result = -2145058815;
  }
  else
  {
    v10 = a1;
    v11 = a3;
    v8 = 0;
    v7 = -110;
    v9 = 4;
    result = exp_SceSysconForDriver_ksceSysconCmdExec((int)&v6, 0);
    if ( result >= 0 )
    {
      imp_SceSysclibForDriver_memcpy(v4, &v12, v3);
      result = 0;
    }
  }
  if ( v13 != MEMORY[0] )
    imp_SceSysclibForDriver___stack_chk_fail(result);
  return result;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x4E0EB57B]
int __fastcall exp_SceSysconForDriver_0x4E0EB57B(int a1, int a2, int a3)
{
  signed int v3; // r0
  int result; // r0
  int v5; // [sp+0h] [bp-A0h]
  char v6; // [sp+10h] [bp-90h]
  char v7; // [sp+11h] [bp-8Fh]
  char v8; // [sp+12h] [bp-8Eh]
  __int16 v9; // [sp+13h] [bp-8Dh]
  char v10; // [sp+15h] [bp-8Bh]
  char v11; // [sp+16h] [bp-8Ah]
  int v12; // [sp+84h] [bp-1Ch]

  v12 = MEMORY[0];
  if ( (unsigned int)(a3 - 1) > 0x17 || (unsigned int)(a3 + a1) > 0x80 )
  {
    result = -2145058815;
  }
  else
  {
    v9 = a1;
    v10 = a3;
    v8 = a3 + 4;
    v6 = -109;
    v7 = 0;
    imp_SceSysclibForDriver_memcpy(&v11, a2, a3);
    v3 = exp_SceSysconForDriver_ksceSysconCmdExec((int)&v5, 0);
    result = v3 & (v3 >> 31);
  }
  if ( v12 != MEMORY[0] )
    imp_SceSysclibForDriver___stack_chk_fail(result);
  return result;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x058941D7]
int __fastcall exp_SceSysconForDriver_0x058941D7(unsigned int a1)
{
  return sub_810030C8(a1, 0xB0u, 2u);
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0xDECCB2B4]
int __fastcall exp_SceSysconForDriver_ksceSysconCtrlHostOutputViaDongle(unsigned int a1)
{
  return sub_810030C8(a1, 0xB2u, 2u);
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x33B5CDB3]
signed int __fastcall exp_SceSysconForDriver_0x33B5CDB3(_DWORD *a1)
{
  return sub_81002D20(a1, 0xB3u);
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0xF6D4DDC4]
signed int __fastcall exp_SceSysconForDriver_0xF6D4DDC4(_DWORD *a1)
{
  return sub_81002D20(a1, 0xB4u);
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x00AE3AEB]
int __fastcall exp_SceSysconForDriver_0x00AE3AEB(unsigned __int8 a1)
{
  return sub_810030C8(a1, 0xB5u, 2u);
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x0D300158]
int __fastcall exp_SceSysconForDriver_0x0D300158(unsigned __int8 a1)
{
  return sub_810030C8(a1, 0xB6u, 2u);
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x91EF4EC3]
signed int __fastcall exp_SceSysconForDriver_0x91EF4EC3(_DWORD *a1)
{
  return sub_81002D20(a1, 0xB7u);
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x8A95D35C]
void exp_SceSysconForDriver_ksceSysconSetPowerMode()
{
    SecureMonitorCall();
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x94AB13CC]
#define ERNIE_SHUTDOWN_SHUTDOWN 0
#define ERNIE_SHUTDOWN_REBOOT 1
int __fastcall exp_SceSysconForDriver_ksceSysconErnieShutdown(unsigned int a1)
{
  return sub_810030C8(a1, 0xC1u, 2u);
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x4278E614]
void exp_SceSysconForDriver_0x4278E614()
{
    // MOVS R1, #0
    // MOVS R0, #5
    SecureMonitorCall();
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x145F59A4]
signed int __fastcall exp_SceSysconForDriver_0x145F59A4(_DWORD *a1)
{
  _DWORD *v1; // r5
  signed int v2; // r3
  int v3; // r2
  signed int result; // r0
  int v5; // [sp+0h] [bp-18h]
  int v6; // [sp+4h] [bp-14h]

  v1 = a1;
  v6 = MEMORY[0];
  v2 = sub_81002D20(&v5, 0x100u);
  v3 = v6;
  if ( v2 >= 0 )
    *v1 = v5;
  result = v2;
  if ( v3 != MEMORY[0] )
    imp_SceSysclibForDriver___stack_chk_fail(v2);
  return result;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x76272CB9]
signed int __fastcall exp_SceSysconForDriver_0x76272CB9(_DWORD *a1)
{
  _DWORD *v1; // r5
  signed int v2; // r3
  int v3; // r2
  signed int result; // r0
  int v5; // [sp+0h] [bp-18h]
  int v6; // [sp+4h] [bp-14h]

  v1 = a1;
  v6 = MEMORY[0];
  v2 = sub_81002D20(&v5, 0x120u);
  v3 = v6;
  if ( v2 >= 0 )
    *v1 = v5;
  result = v2;
  if ( v3 != MEMORY[0] )
    imp_SceSysclibForDriver___stack_chk_fail(v2);
  return result;
}

signed int __fastcall sub_81003C5C(_DWORD *a1)
{
  return sub_81002D20(a1, 0x130u);
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0xD6F6D472]
int __fastcall exp_SceSysconForDriver_0xD6F6D472(_DWORD *a1)
{
  if ( a1 )
    *a1 = dword_8100C0B0;
  return 0;
}

signed int __fastcall sub_81003C7C(_DWORD *a1)
{
  _DWORD *v1; // r5
  signed int result; // r0
  char v3; // r3
  bool v4; // zf
  int v5; // [sp+0h] [bp-18h]
  int v6; // [sp+4h] [bp-14h]

  v1 = a1;
  v6 = MEMORY[0];
  if ( (exp_SceSysconForDriver_ksceSysconGetHardwareInfo() & 0xFF0000u) >= 0x400000 )
  {
    result = sub_81002D20(v1, 0x103u);
  }
  else
  {
    result = sub_81002D20(&v5, 0x103u);
    if ( result >= 0 )
    {
      v3 = BYTE1(v5);
      switch ( BYTE1(v5) )
      {
        case 0x65u:
          BYTE1(v5) = 4;
          break;
        case 0x55u:
          BYTE1(v5) = 5;
          break;
        case 0x51u:
          BYTE1(v5) = 2;
          break;
        default:
          v4 = BYTE1(v5) == 96;
          if ( BYTE1(v5) == 96 )
            v3 = 0;
          else
            BYTE1(v5) = -1;
          if ( v4 )
            BYTE1(v5) = v3;
          break;
      }
      if ( v1 )
        *v1 = v5;
    }
  }
  if ( v6 != MEMORY[0] )
    imp_SceSysclibForDriver___stack_chk_fail(result);
  return result;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x1503D6A0]
int __fastcall exp_SceSysconForDriver_ksceSysconGetMultiCnInfo(unsigned int *a1)
{
  int v1; // r3
  unsigned int *v2; // r6
  unsigned int v3; // r5
  int v4; // r3
  int result; // r0
  int v6; // [sp+4h] [bp-14h]

  LOWORD(v1) = -16384;
  HIWORD(v1) = (unsigned int)dword_8100C000 >> 16;
  v2 = a1;
  v3 = *(_DWORD *)(v1 + 0xAC);
  v6 = MEMORY[0];
  if ( (exp_SceSysconForDriver_ksceSysconGetHardwareInfo() & 0xFF0000u) < 0x400000 )
  {
    v4 = (unsigned __int16)v3 >> 8;
    switch ( v4 )
    {
      case 101:
        v3 = v3 & 0xFFFF00FF | 0x400;
        break;
      case 85:
        v3 = v3 & 0xFFFF00FF | 0x500;
        break;
      case 81:
        v3 = v3 & 0xFFFF00FF | 0x200;
        break;
      case 96:
        v3 &= 0xFFFF00FF;
        break;
      default:
        v3 |= 0xFF00u;
        break;
    }
  }
  if ( v2 )
  {
    if ( imp_SceSblAIMgrForDriver_ksceSblAimgrIsGenuineDolce() )
      v3 &= 0xFFFF00FF;
    *v2 = v3;
  }
  result = 0;
  if ( v6 != MEMORY[0] )
    imp_SceSysclibForDriver___stack_chk_fail(0);
  return result;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x8AAB6308]
int __fastcall exp_SceSysconForDriver_ksceSysconSetMultiCnPort(unsigned int a1)
{
  return sub_810030C8(a1, 0x190u, 3u);
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0xE7893732]
int __fastcall exp_SceSysconForDriver_0xE7893732(unsigned int a1)
{
  return sub_810030C8(a1, 0x1B0u, 3u);
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0xCFCEE733]
int __fastcall exp_SceSysconForDriver_0xCFCEE733(unsigned int a1)
{
  return sub_810030C8(a1, 0x181u, 2u);
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0xB7E294AF]
signed int __fastcall exp_SceSysconForDriver_0xB7E294AF(signed int *a1)
{
  signed int *v1; // r6
  signed int v2; // r0
  int v3; // r1
  int v4; // r2
  int v5; // r3
  bool v6; // nf
  signed int v7; // r7
  signed int result; // r0
  int v9; // r2
  int v10; // r3
  int v11; // [sp+0h] [bp-A0h]
  char v12; // [sp+10h] [bp-90h]
  char v13; // [sp+11h] [bp-8Fh]
  char v14; // [sp+12h] [bp-8Eh]
  int v15; // [sp+34h] [bp-6Ch]
  int v16; // [sp+38h] [bp-68h]
  int v17; // [sp+3Ch] [bp-64h]
  int v18; // [sp+40h] [bp-60h]
  int v19; // [sp+84h] [bp-1Ch]

  v1 = a1;
  v12 = -126;
  v19 = MEMORY[0];
  v13 = 1;
  v14 = 1;
  v2 = exp_SceSysconForDriver_ksceSysconCmdExec((int)&v11, 0);
  v6 = v2 < 0;
  if ( v2 < 0 )
  {
    v7 = v2;
  }
  else
  {
    v7 = 0;
    v2 = v15;
    v3 = v16;
    v4 = v17;
    v5 = v18;
  }
  if ( !v6 )
    *v1 = v2;
  result = v7;
  if ( !v6 )
  {
    v1[2] = v4;
    v1[3] = v5;
  }
  v9 = v19;
  v10 = MEMORY[0];
  if ( !v6 )
    v1[1] = v3;
  if ( v9 != v10 )
    imp_SceSysclibForDriver___stack_chk_fail(v7);
  return result;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0xFDB3AE9D]
signed int __fastcall exp_SceSysconForDriver_0xFDB3AE9D(signed int *a1)
{
  signed int *v1; // r6
  signed int v2; // r0
  int v3; // r1
  int v4; // r2
  int v5; // r3
  bool v6; // nf
  signed int v7; // r7
  signed int result; // r0
  int v9; // r2
  int v10; // r3
  int v11; // [sp+0h] [bp-A0h]
  char v12; // [sp+10h] [bp-90h]
  char v13; // [sp+11h] [bp-8Fh]
  char v14; // [sp+12h] [bp-8Eh]
  int v15; // [sp+34h] [bp-6Ch]
  int v16; // [sp+38h] [bp-68h]
  int v17; // [sp+3Ch] [bp-64h]
  int v18; // [sp+40h] [bp-60h]
  int v19; // [sp+84h] [bp-1Ch]

  v1 = a1;
  v12 = -125;
  v19 = MEMORY[0];
  v13 = 1;
  v14 = 1;
  v2 = exp_SceSysconForDriver_ksceSysconCmdExec((int)&v11, 0);
  v6 = v2 < 0;
  if ( v2 < 0 )
  {
    v7 = v2;
  }
  else
  {
    v7 = 0;
    v2 = v15;
    v3 = v16;
    v4 = v17;
    v5 = v18;
  }
  if ( !v6 )
    *v1 = v2;
  result = v7;
  if ( !v6 )
  {
    v1[2] = v4;
    v1[3] = v5;
  }
  v9 = v19;
  v10 = MEMORY[0];
  if ( !v6 )
    v1[1] = v3;
  if ( v9 != v10 )
    imp_SceSysclibForDriver___stack_chk_fail(v7);
  return result;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x8A97E690]
int __fastcall exp_SceSysconForDriver_0x8A97E690(int *a1)
{
  int v1; // r12
  int v2; // lr
  int v3; // r7
  int v4; // r6
  signed int v5; // r0
  int result; // r0
  int v7; // [sp+0h] [bp-A0h]
  char v8; // [sp+10h] [bp-90h]
  char v9; // [sp+11h] [bp-8Fh]
  char v10; // [sp+12h] [bp-8Eh]
  int v11; // [sp+13h] [bp-8Dh]
  int v12; // [sp+17h] [bp-89h]
  int v13; // [sp+1Bh] [bp-85h]
  int v14; // [sp+1Fh] [bp-81h]
  int v15; // [sp+84h] [bp-1Ch]

  v1 = *a1;
  v2 = a1[1];
  v3 = a1[2];
  v4 = a1[3];
  v9 = 1;
  v10 = 17;
  v15 = MEMORY[0];
  v11 = v1;
  v12 = v2;
  v13 = v3;
  v14 = v4;
  v8 = -124;
  v5 = exp_SceSysconForDriver_ksceSysconCmdExec((int)&v7, 0);
  result = v5 & (v5 >> 31);
  if ( v15 != MEMORY[0] )
    imp_SceSysclibForDriver___stack_chk_fail(result);
  return result;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0xC314B64B]
signed int __fastcall exp_SceSysconForDriver_0xC314B64B(signed int *a1)
{
  signed int *v1; // r5
  signed int v2; // r0
  int v3; // r1
  int v4; // r2
  bool v5; // nf
  signed int v6; // r6
  signed int result; // r0
  int v8; // r2
  int v9; // r3
  int v10; // [sp+0h] [bp-98h]
  char v11; // [sp+10h] [bp-88h]
  char v12; // [sp+11h] [bp-87h]
  char v13; // [sp+12h] [bp-86h]
  int v14; // [sp+34h] [bp-64h]
  int v15; // [sp+38h] [bp-60h]
  int v16; // [sp+3Ch] [bp-5Ch]
  int v17; // [sp+84h] [bp-14h]

  v1 = a1;
  v12 = 1;
  v13 = 1;
  v11 = -123;
  v17 = MEMORY[0];
  v2 = exp_SceSysconForDriver_ksceSysconCmdExec((int)&v10, 0);
  v5 = v2 < 0;
  if ( v2 < 0 )
  {
    v6 = v2;
  }
  else
  {
    v6 = 0;
    v2 = v14;
    v3 = v15;
    v4 = v16;
  }
  if ( !v5 )
    *v1 = v2;
  result = v6;
  if ( !v5 )
    v1[2] = v4;
  v8 = v17;
  v9 = MEMORY[0];
  if ( !v5 )
    v1[1] = v3;
  if ( v8 != v9 )
    imp_SceSysclibForDriver___stack_chk_fail(v6);
  return result;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0xF99BC858]
signed int __fastcall exp_SceSysconForDriver_0xF99BC858(_DWORD *a1)
{
  return sub_81002D20(a1, 0x131u);
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0xF492E69E]
signed int __fastcall exp_SceSysconForDriver_ksceSysconGetTouchpanelDeviceInfo(_WORD *a1)
{
  _WORD *v1; // r4
  signed int result; // r0
  __int16 v3; // r7
  __int16 v4; // r2
  __int16 v5; // r1
  __int16 v6; // r3
  int v7; // r6
  int v8; // [sp+0h] [bp-A0h]
  char v9; // [sp+10h] [bp-90h]
  char v10; // [sp+11h] [bp-8Fh]
  char v11; // [sp+12h] [bp-8Eh]
  unsigned __int16 v12; // [sp+34h] [bp-6Ch]
  unsigned __int8 v13; // [sp+36h] [bp-6Ah]
  unsigned __int8 v14; // [sp+37h] [bp-69h]
  unsigned __int8 v15; // [sp+38h] [bp-68h]
  unsigned __int8 v16; // [sp+39h] [bp-67h]
  unsigned __int8 v17; // [sp+3Ah] [bp-66h]
  unsigned __int8 v18; // [sp+3Bh] [bp-65h]
  int v19; // [sp+84h] [bp-1Ch]

  v1 = a1;
  v19 = MEMORY[0];
  if ( a1 )
  {
    v9 = -128;
    v10 = 3;
    v11 = 1;
    result = exp_SceSysconForDriver_ksceSysconCmdExec((int)&v8, 0);
    if ( result >= 0 )
    {
      result = 0;
      v3 = v15;
      v4 = v16;
      v5 = v17;
      v6 = v18;
      v7 = v14 | (v13 << 8);
      *v1 = _byteswap_ushort(v12);
      v1[1] = v7;
      v1[2] = v4 | (v3 << 8);
      v1[3] = v6 | (v5 << 8);
    }
  }
  else
  {
    result = -2145058814;
  }
  if ( v19 != MEMORY[0] )
    imp_SceSysclibForDriver___stack_chk_fail(result);
  return result;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x856DFC0E]
int __fastcall exp_SceSysconForDriver_0x856DFC0E(__int16 a1, int a2, int a3)
{
  signed int v3; // r0
  int result; // r0
  int v5; // [sp+0h] [bp-A0h]
  char v6; // [sp+10h] [bp-90h]
  char v7; // [sp+11h] [bp-8Fh]
  char v8; // [sp+12h] [bp-8Eh]
  __int16 v9; // [sp+13h] [bp-8Dh]
  char v10; // [sp+15h] [bp-8Bh]
  __int16 v11; // [sp+16h] [bp-8Ah]
  __int16 v12; // [sp+18h] [bp-88h]
  __int16 v13; // [sp+1Ah] [bp-86h]
  __int16 v14; // [sp+1Ch] [bp-84h]
  __int16 v15; // [sp+1Eh] [bp-82h]
  int v16; // [sp+84h] [bp-1Ch]

  v16 = MEMORY[0];
  if ( (unsigned int)(a3 - 1) > 9 )
  {
    result = -2145058815;
  }
  else
  {
    v9 = a1;
    v10 = a3;
    v6 = -125;
    v7 = 3;
    v8 = 14;
    v11 = 0;
    v12 = 0;
    v13 = 0;
    v14 = 0;
    v15 = 0;
    imp_SceSysclibForDriver_memcpy(&v11, a2, a3);
    v3 = exp_SceSysconForDriver_ksceSysconCmdExec((int)&v5, 0);
    result = v3 & (v3 >> 31);
  }
  if ( v16 != MEMORY[0] )
    imp_SceSysclibForDriver___stack_chk_fail(result);
  return result;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0xE38045F5]
int __fastcall exp_SceSysconForDriver_0xE38045F5(__int16 a1, int a2, int a3)
{
  signed int v3; // r0
  int result; // r0
  int v5; // [sp+0h] [bp-A0h]
  char v6; // [sp+10h] [bp-90h]
  char v7; // [sp+11h] [bp-8Fh]
  char v8; // [sp+12h] [bp-8Eh]
  __int16 v9; // [sp+13h] [bp-8Dh]
  char v10; // [sp+15h] [bp-8Bh]
  __int16 v11; // [sp+16h] [bp-8Ah]
  __int16 v12; // [sp+18h] [bp-88h]
  __int16 v13; // [sp+1Ah] [bp-86h]
  __int16 v14; // [sp+1Ch] [bp-84h]
  __int16 v15; // [sp+1Eh] [bp-82h]
  int v16; // [sp+84h] [bp-1Ch]

  v16 = MEMORY[0];
  if ( (unsigned int)(a3 - 1) > 9 )
  {
    result = -2145058815;
  }
  else
  {
    v9 = a1;
    v10 = a3;
    v6 = -123;
    v7 = 3;
    v8 = 14;
    v11 = 0;
    v12 = 0;
    v13 = 0;
    v14 = 0;
    v15 = 0;
    imp_SceSysclibForDriver_memcpy(&v11, a2, a3);
    v3 = exp_SceSysconForDriver_ksceSysconCmdExec((int)&v5, 0);
    result = v3 & (v3 >> 31);
  }
  if ( v16 != MEMORY[0] )
    imp_SceSysclibForDriver___stack_chk_fail(result);
  return result;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x1C5D466C]
signed int __fastcall exp_SceSysconForDriver_0x1C5D466C(__int16 a1, int a2, int a3)
{
  int v3; // r5
  int v4; // r7
  signed int result; // r0
  signed int v6; // r6
  int v7; // [sp+0h] [bp-A0h]
  char v8; // [sp+10h] [bp-90h]
  char v9; // [sp+11h] [bp-8Fh]
  char v10; // [sp+12h] [bp-8Eh]
  __int16 v11; // [sp+13h] [bp-8Dh]
  char v12; // [sp+15h] [bp-8Bh]
  unsigned __int8 v13; // [sp+34h] [bp-6Ch]
  char v14; // [sp+35h] [bp-6Bh]
  int v15; // [sp+84h] [bp-1Ch]

  v3 = a3;
  v4 = a2;
  v15 = MEMORY[0];
  if ( (unsigned int)(a3 - 1) > 9 )
    goto LABEL_11;
  v11 = a1;
  v12 = a3;
  v8 = -124;
  v9 = 3;
  v10 = 4;
  result = exp_SceSysconForDriver_ksceSysconCmdExec((int)&v7, 0);
  if ( result < 0 )
    goto LABEL_5;
  v6 = v13;
  if ( v13 )
  {
LABEL_11:
    result = -2145058815;
  }
  else
  {
    imp_SceSysclibForDriver_memcpy(v4, &v14, v3);
    result = v6;
  }
LABEL_5:
  if ( v15 != MEMORY[0] )
    imp_SceSysclibForDriver___stack_chk_fail(result);
  return result;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x6D408E30]
signed int __fastcall exp_SceSysconForDriver_0x6D408E30(__int16 a1, int a2, int a3)
{
  int v3; // r5
  int v4; // r7
  signed int result; // r0
  signed int v6; // r6
  int v7; // [sp+0h] [bp-A0h]
  char v8; // [sp+10h] [bp-90h]
  char v9; // [sp+11h] [bp-8Fh]
  char v10; // [sp+12h] [bp-8Eh]
  __int16 v11; // [sp+13h] [bp-8Dh]
  char v12; // [sp+15h] [bp-8Bh]
  unsigned __int8 v13; // [sp+34h] [bp-6Ch]
  char v14; // [sp+35h] [bp-6Bh]
  int v15; // [sp+84h] [bp-1Ch]

  v3 = a3;
  v4 = a2;
  v15 = MEMORY[0];
  if ( (unsigned int)(a3 - 1) > 9 )
    goto LABEL_11;
  v11 = a1;
  v12 = a3;
  v8 = -122;
  v9 = 3;
  v10 = 4;
  result = exp_SceSysconForDriver_ksceSysconCmdExec((int)&v7, 0);
  if ( result < 0 )
    goto LABEL_5;
  v6 = v13;
  if ( v13 )
  {
LABEL_11:
    result = -2145058815;
  }
  else
  {
    imp_SceSysclibForDriver_memcpy(v4, &v14, v3);
    result = v6;
  }
LABEL_5:
  if ( v15 != MEMORY[0] )
    imp_SceSysclibForDriver___stack_chk_fail(result);
  return result;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x1546A141]
int __fastcall exp_SceSysconForDriver_0x1546A141(int a1, int a2)
{
  bool v2; // nf
  char v3; // r2
  char v4; // r2
  char v5; // r5
  signed int v6; // r0
  int result; // r0
  int v8; // [sp+0h] [bp-98h]
  char v9; // [sp+10h] [bp-88h]
  char v10; // [sp+11h] [bp-87h]
  char v11; // [sp+12h] [bp-86h]
  char v12; // [sp+13h] [bp-85h]
  char v13; // [sp+14h] [bp-84h]
  char v14; // [sp+15h] [bp-83h]
  char v15; // [sp+16h] [bp-82h]
  char v16; // [sp+17h] [bp-81h]
  char v17; // [sp+18h] [bp-80h]
  char v18; // [sp+19h] [bp-7Fh]
  char v19; // [sp+1Ah] [bp-7Eh]
  int v20; // [sp+84h] [bp-14h]

  v2 = a1 < 0;
  if ( a1 < 0 )
    LOBYTE(a1) = 0;
  v11 = 9;
  v20 = MEMORY[0];
  if ( v2 )
    v3 = a1;
  else
    v3 = 1;
  v12 = v3;
  if ( a2 >= 0 )
    v4 = a2;
  else
    v4 = 0;
  v14 = 0;
  v15 = 0;
  v10 = 3;
  if ( a2 >= 0 )
    v5 = 1;
  else
    v5 = v4;
  v13 = a1;
  v17 = v4;
  v18 = 0;
  v19 = 0;
  v9 = -121;
  v16 = v5;
  v6 = exp_SceSysconForDriver_ksceSysconCmdExec((int)&v8, 0);
  result = v6 & (v6 >> 31);
  if ( v20 != MEMORY[0] )
    imp_SceSysclibForDriver___stack_chk_fail(result);
  return result;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0xA1F1B973]
signed int exp_SceSysconForDriver_0xA1F1B973()
{
  signed int result; // r0
  int v1; // [sp+0h] [bp-10h]
  int v2; // [sp+4h] [bp-Ch]

  v1 = 0;
  v2 = MEMORY[0];
  result = sub_81002D20(&v1, 0x38Au);
  if ( result >= 0 )
    result = v1;
  if ( v2 != MEMORY[0] )
    imp_SceSysclibForDriver___stack_chk_fail(result);
  return result;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x030D447F]
signed int __fastcall exp_SceSysconForDriver_0x030D447F(int a1)
{
  int v1; // r4
  signed int result; // r0
  int v3; // r10
  int v4; // r11
  unsigned __int16 v5; // r3
  int v6; // r12
  char v7; // lr
  int v8; // r8
  char v9; // r7
  char v10; // ST00_1
  int v11; // r9
  char v12; // r2
  char v13; // r6
  char v14; // ST04_1
  char v15; // r2
  char v16; // [sp+8h] [bp-B0h]
  char v17; // [sp+18h] [bp-A0h]
  char v18; // [sp+19h] [bp-9Fh]
  char v19; // [sp+1Ah] [bp-9Eh]
  unsigned __int16 v20; // [sp+3Ch] [bp-7Ch]
  unsigned __int8 v21; // [sp+3Eh] [bp-7Ah]
  unsigned __int8 v22; // [sp+3Fh] [bp-79h]
  unsigned __int8 v23; // [sp+40h] [bp-78h]
  unsigned __int8 v24; // [sp+41h] [bp-77h]
  char v25; // [sp+42h] [bp-76h]
  char v26; // [sp+43h] [bp-75h]
  char v27; // [sp+44h] [bp-74h]
  char v28; // [sp+45h] [bp-73h]
  unsigned __int8 v29; // [sp+46h] [bp-72h]
  unsigned __int8 v30; // [sp+47h] [bp-71h]
  unsigned __int8 v31; // [sp+48h] [bp-70h]
  unsigned __int8 v32; // [sp+49h] [bp-6Fh]
  unsigned __int8 v33; // [sp+4Ah] [bp-6Eh]
  unsigned __int8 v34; // [sp+4Bh] [bp-6Dh]
  char v35; // [sp+4Ch] [bp-6Ch]
  char v36; // [sp+4Dh] [bp-6Bh]
  char v37; // [sp+4Eh] [bp-6Ah]
  char v38; // [sp+4Fh] [bp-69h]
  int v39; // [sp+8Ch] [bp-2Ch]

  v1 = a1;
  v17 = -112;
  v39 = MEMORY[0];
  v18 = 3;
  v19 = 1;
  result = exp_SceSysconForDriver_ksceSysconCmdExec((int)&v16, 0);
  if ( result >= 0 )
  {
    result = 0;
    v3 = v24 | (v23 << 8);
    v4 = v22 | (v21 << 8);
    v5 = _byteswap_ushort(v20);
    v6 = v34 | (v33 << 8);
    v7 = v25;
    v8 = v32 | (v31 << 8);
    v9 = v26;
    v10 = v35;
    v11 = v30 | (v29 << 8);
    v12 = v36;
    v13 = v27;
    *(_BYTE *)(v1 + 9) = v28;
    v14 = v12;
    *(_WORD *)v1 = v5;
    v15 = v37;
    LOBYTE(v5) = v38;
    *(_WORD *)(v1 + 2) = v4;
    *(_WORD *)(v1 + 4) = v3;
    *(_WORD *)(v1 + 10) = v11;
    *(_WORD *)(v1 + 12) = v8;
    *(_WORD *)(v1 + 14) = v6;
    *(_BYTE *)(v1 + 6) = v7;
    *(_BYTE *)(v1 + 7) = v9;
    *(_BYTE *)(v1 + 8) = v13;
    *(_BYTE *)(v1 + 16) = v10;
    *(_BYTE *)(v1 + 18) = v15;
    *(_BYTE *)(v1 + 19) = v5;
    *(_BYTE *)(v1 + 17) = v14;
  }
  if ( v39 != MEMORY[0] )
    imp_SceSysclibForDriver___stack_chk_fail(result);
  return result;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0xBC1D93D7]
signed int __fastcall exp_SceSysconForDriver_0xBC1D93D7(_DWORD *a1)
{
  _DWORD *v1; // r5
  signed int result; // r0
  int v3; // [sp+0h] [bp-A0h]
  char v4; // [sp+10h] [bp-90h]
  char v5; // [sp+11h] [bp-8Fh]
  char v6; // [sp+12h] [bp-8Eh]
  unsigned __int8 v7; // [sp+34h] [bp-6Ch]
  int v8; // [sp+84h] [bp-1Ch]

  v1 = a1;
  v4 = -96;
  v8 = MEMORY[0];
  v5 = 3;
  v6 = 1;
  result = exp_SceSysconForDriver_ksceSysconCmdExec((int)&v3, 0);
  if ( result >= 0 && v1 )
    *v1 = v7;
  if ( v8 != MEMORY[0] )
    imp_SceSysclibForDriver___stack_chk_fail(result);
  return result;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x8B0B4A97]
signed int __fastcall exp_SceSysconForDriver_0x8B0B4A97(_DWORD *a1)
{
  _DWORD *v1; // r5
  signed int result; // r0
  int v3; // [sp+0h] [bp-A0h]
  char v4; // [sp+10h] [bp-90h]
  char v5; // [sp+11h] [bp-8Fh]
  char v6; // [sp+12h] [bp-8Eh]
  unsigned __int8 v7; // [sp+34h] [bp-6Ch]
  int v8; // [sp+84h] [bp-1Ch]

  v1 = a1;
  v4 = -80;
  v8 = MEMORY[0];
  v5 = 3;
  v6 = 1;
  result = exp_SceSysconForDriver_ksceSysconCmdExec((int)&v3, 0);
  if ( result >= 0 && v1 )
    *v1 = v7;
  if ( v8 != MEMORY[0] )
    imp_SceSysclibForDriver___stack_chk_fail(result);
  return result;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x567865A3]
signed int __fastcall exp_SceSysconForDriver_0x567865A3(_DWORD *a1)
{
  _DWORD *v1; // r5
  signed int result; // r0
  int v3; // [sp+0h] [bp-A0h]
  char v4; // [sp+10h] [bp-90h]
  char v5; // [sp+11h] [bp-8Fh]
  char v6; // [sp+12h] [bp-8Eh]
  unsigned __int8 v7; // [sp+34h] [bp-6Ch]
  int v8; // [sp+84h] [bp-1Ch]

  v1 = a1;
  v4 = -95;
  v8 = MEMORY[0];
  v5 = 3;
  v6 = 1;
  result = exp_SceSysconForDriver_ksceSysconCmdExec((int)&v3, 0);
  if ( result >= 0 && v1 )
    *v1 = v7;
  if ( v8 != MEMORY[0] )
    imp_SceSysclibForDriver___stack_chk_fail(result);
  return result;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0xAE127A96]
signed int __fastcall exp_SceSysconForDriver_0xAE127A96(_DWORD *a1)
{
  _DWORD *v1; // r5
  signed int result; // r0
  int v3; // [sp+0h] [bp-A0h]
  char v4; // [sp+10h] [bp-90h]
  char v5; // [sp+11h] [bp-8Fh]
  char v6; // [sp+12h] [bp-8Eh]
  unsigned __int8 v7; // [sp+34h] [bp-6Ch]
  int v8; // [sp+84h] [bp-1Ch]

  v1 = a1;
  v4 = -79;
  v8 = MEMORY[0];
  v5 = 3;
  v6 = 1;
  result = exp_SceSysconForDriver_ksceSysconCmdExec((int)&v3, 0);
  if ( result >= 0 && v1 )
    *v1 = v7;
  if ( v8 != MEMORY[0] )
    imp_SceSysclibForDriver___stack_chk_fail(result);
  return result;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x41942B50]
signed int __fastcall exp_SceSysconForDriver_0x41942B50(_DWORD *a1)
{
  _DWORD *v1; // r5
  signed int result; // r0
  int v3; // [sp+0h] [bp-A0h]
  char v4; // [sp+10h] [bp-90h]
  char v5; // [sp+11h] [bp-8Fh]
  char v6; // [sp+12h] [bp-8Eh]
  unsigned __int8 v7; // [sp+34h] [bp-6Ch]
  int v8; // [sp+84h] [bp-1Ch]

  v1 = a1;
  v4 = -92;
  v8 = MEMORY[0];
  v5 = 3;
  v6 = 1;
  result = exp_SceSysconForDriver_ksceSysconCmdExec((int)&v3, 0);
  if ( result >= 0 && v1 )
    *v1 = v7;
  if ( v8 != MEMORY[0] )
    imp_SceSysclibForDriver___stack_chk_fail(result);
  return result;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x2A61ACE1]
signed int __fastcall exp_SceSysconForDriver_0x2A61ACE1(_DWORD *a1)
{
  _DWORD *v1; // r5
  signed int result; // r0
  int v3; // [sp+0h] [bp-A0h]
  char v4; // [sp+10h] [bp-90h]
  char v5; // [sp+11h] [bp-8Fh]
  char v6; // [sp+12h] [bp-8Eh]
  unsigned __int8 v7; // [sp+34h] [bp-6Ch]
  int v8; // [sp+84h] [bp-1Ch]

  v1 = a1;
  v4 = -76;
  v8 = MEMORY[0];
  v5 = 3;
  v6 = 1;
  result = exp_SceSysconForDriver_ksceSysconCmdExec((int)&v3, 0);
  if ( result >= 0 && v1 )
    *v1 = v7;
  if ( v8 != MEMORY[0] )
    imp_SceSysclibForDriver___stack_chk_fail(result);
  return result;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x998ECD6D]
signed int __fastcall exp_SceSysconForDriver_0x998ECD6D(_DWORD *a1)
{
  _DWORD *v1; // r5
  signed int result; // r0
  int v3; // [sp+0h] [bp-A0h]
  char v4; // [sp+10h] [bp-90h]
  char v5; // [sp+11h] [bp-8Fh]
  char v6; // [sp+12h] [bp-8Eh]
  unsigned __int8 v7; // [sp+34h] [bp-6Ch]
  int v8; // [sp+84h] [bp-1Ch]

  v1 = a1;
  v4 = -91;
  v8 = MEMORY[0];
  v5 = 3;
  v6 = 1;
  result = exp_SceSysconForDriver_ksceSysconCmdExec((int)&v3, 0);
  if ( result >= 0 && v1 )
    *v1 = v7;
  if ( v8 != MEMORY[0] )
    imp_SceSysclibForDriver___stack_chk_fail(result);
  return result;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0xCEE2B8F7]
signed int __fastcall exp_SceSysconForDriver_0xCEE2B8F7(_DWORD *a1)
{
  _DWORD *v1; // r5
  signed int result; // r0
  int v3; // [sp+0h] [bp-A0h]
  char v4; // [sp+10h] [bp-90h]
  char v5; // [sp+11h] [bp-8Fh]
  char v6; // [sp+12h] [bp-8Eh]
  unsigned __int8 v7; // [sp+34h] [bp-6Ch]
  int v8; // [sp+84h] [bp-1Ch]

  v1 = a1;
  v4 = -75;
  v8 = MEMORY[0];
  v5 = 3;
  v6 = 1;
  result = exp_SceSysconForDriver_ksceSysconCmdExec((int)&v3, 0);
  if ( result >= 0 && v1 )
    *v1 = v7;
  if ( v8 != MEMORY[0] )
    imp_SceSysclibForDriver___stack_chk_fail(result);
  return result;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x0396BA09]
signed int exp_SceSysconForDriver_0x0396BA09()
{
  signed int result; // r0
  int v1; // [sp+0h] [bp-98h]
  char v2; // [sp+10h] [bp-88h]
  char v3; // [sp+11h] [bp-87h]
  char v4; // [sp+12h] [bp-86h]
  int v5; // [sp+84h] [bp-14h]

  v3 = 3;
  v4 = 1;
  v2 = -90;
  v5 = MEMORY[0];
  result = exp_SceSysconForDriver_ksceSysconCmdExec((int)&v1, 0);
  if ( v5 != MEMORY[0] )
    imp_SceSysclibForDriver___stack_chk_fail(result);
  return result;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x23558805]
signed int exp_SceSysconForDriver_0x23558805()
{
  signed int result; // r0
  int v1; // [sp+0h] [bp-98h]
  char v2; // [sp+10h] [bp-88h]
  char v3; // [sp+11h] [bp-87h]
  char v4; // [sp+12h] [bp-86h]
  int v5; // [sp+84h] [bp-14h]

  v3 = 3;
  v4 = 1;
  v2 = -74;
  v5 = MEMORY[0];
  result = exp_SceSysconForDriver_ksceSysconCmdExec((int)&v1, 0);
  if ( v5 != MEMORY[0] )
    imp_SceSysclibForDriver___stack_chk_fail(result);
  return result;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x9B6B8BB9]
signed int __fastcall exp_SceSysconForDriver_0x9B6B8BB9(int *a1)
{
  int *v1; // r5
  signed int result; // r0
  int v3; // [sp+0h] [bp-A0h]
  char v4; // [sp+10h] [bp-90h]
  char v5; // [sp+11h] [bp-8Fh]
  char v6; // [sp+12h] [bp-8Eh]
  unsigned __int8 v7; // [sp+34h] [bp-6Ch]
  unsigned __int8 v8; // [sp+35h] [bp-6Bh]
  int v9; // [sp+84h] [bp-1Ch]

  v1 = a1;
  v4 = -89;
  v9 = MEMORY[0];
  v5 = 3;
  v6 = 1;
  result = exp_SceSysconForDriver_ksceSysconCmdExec((int)&v3, 0);
  if ( result >= 0 && v1 )
    *v1 = v7 | (v8 << 8);
  if ( v9 != MEMORY[0] )
    imp_SceSysclibForDriver___stack_chk_fail(result);
  return result;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0xC4A61241]
signed int __fastcall exp_SceSysconForDriver_0xC4A61241(int *a1)
{
  int *v1; // r5
  signed int result; // r0
  int v3; // [sp+0h] [bp-A0h]
  char v4; // [sp+10h] [bp-90h]
  char v5; // [sp+11h] [bp-8Fh]
  char v6; // [sp+12h] [bp-8Eh]
  unsigned __int8 v7; // [sp+34h] [bp-6Ch]
  unsigned __int8 v8; // [sp+35h] [bp-6Bh]
  int v9; // [sp+84h] [bp-1Ch]

  v1 = a1;
  v4 = -73;
  v9 = MEMORY[0];
  v5 = 3;
  v6 = 1;
  result = exp_SceSysconForDriver_ksceSysconCmdExec((int)&v3, 0);
  if ( result >= 0 && v1 )
    *v1 = v7 | (v8 << 8);
  if ( v9 != MEMORY[0] )
    imp_SceSysclibForDriver___stack_chk_fail(result);
  return result;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x3B57B777]
signed int __fastcall exp_SceSysconForDriver_0x3B57B777(signed int *a1)
{
  signed int *v1; // r5
  signed int v2; // r0
  int v3; // r1
  bool v4; // nf
  int v5; // r2
  signed int v6; // r6
  signed int result; // r0
  int v8; // r3
  int v9; // [sp+0h] [bp-A0h]
  char v10; // [sp+10h] [bp-90h]
  char v11; // [sp+11h] [bp-8Fh]
  char v12; // [sp+12h] [bp-8Eh]
  int v13; // [sp+34h] [bp-6Ch]
  int v14; // [sp+38h] [bp-68h]
  int v15; // [sp+84h] [bp-1Ch]

  v1 = a1;
  v12 = 1;
  v15 = MEMORY[0];
  v11 = 3;
  v10 = -88;
  v2 = exp_SceSysconForDriver_ksceSysconCmdExec((int)&v9, 0);
  v4 = v2 < 0;
  v5 = v15;
  if ( v2 < 0 )
  {
    v6 = v2;
  }
  else
  {
    v6 = 0;
    v2 = v13;
    v3 = v14;
  }
  if ( !v4 )
    *v1 = v2;
  result = v6;
  v8 = MEMORY[0];
  if ( !v4 )
    v1[1] = v3;
  if ( v5 != v8 )
    imp_SceSysclibForDriver___stack_chk_fail(v6);
  return result;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x08A561C7]
signed int __fastcall exp_SceSysconForDriver_0x08A561C7(signed int *a1)
{
  signed int *v1; // r5
  signed int v2; // r0
  int v3; // r1
  bool v4; // nf
  int v5; // r2
  signed int v6; // r6
  signed int result; // r0
  int v8; // r3
  int v9; // [sp+0h] [bp-A0h]
  char v10; // [sp+10h] [bp-90h]
  char v11; // [sp+11h] [bp-8Fh]
  char v12; // [sp+12h] [bp-8Eh]
  int v13; // [sp+34h] [bp-6Ch]
  int v14; // [sp+38h] [bp-68h]
  int v15; // [sp+84h] [bp-1Ch]

  v1 = a1;
  v12 = 1;
  v15 = MEMORY[0];
  v11 = 3;
  v10 = -72;
  v2 = exp_SceSysconForDriver_ksceSysconCmdExec((int)&v9, 0);
  v4 = v2 < 0;
  v5 = v15;
  if ( v2 < 0 )
  {
    v6 = v2;
  }
  else
  {
    v6 = 0;
    v2 = v13;
    v3 = v14;
  }
  if ( !v4 )
    *v1 = v2;
  result = v6;
  v8 = MEMORY[0];
  if ( !v4 )
    v1[1] = v3;
  if ( v5 != v8 )
    imp_SceSysclibForDriver___stack_chk_fail(v6);
  return result;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x010F95D9]
int __fastcall exp_SceSysconForDriver_0x010F95D9(unsigned int a1)
{
  return sub_810030C8(a1, 0x3A9u, 1u);
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x42E599AC]
int __fastcall exp_SceSysconForDriver_0x42E599AC(unsigned int a1)
{
  return sub_810030C8(a1, 0x3B9u, 1u);
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x9F9156BE]
signed int __fastcall exp_SceSysconForDriver_0x9F9156BE(int a1, signed int a2)
{
  int v2; // r7
  signed int v3; // r5
  signed int v4; // r0
  signed int v5; // r1
  __int16 v6; // r2
  char v7; // r3
  signed int result; // r0
  int v9; // [sp+0h] [bp-A0h]
  char v10; // [sp+10h] [bp-90h]
  char v11; // [sp+11h] [bp-8Fh]
  char v12; // [sp+12h] [bp-8Eh]
  int v13; // [sp+13h] [bp-8Dh]
  __int16 v14; // [sp+17h] [bp-89h]
  int v15; // [sp+34h] [bp-6Ch]
  __int16 v16; // [sp+38h] [bp-68h]
  char v17; // [sp+3Ah] [bp-66h]
  int v18; // [sp+84h] [bp-1Ch]

  v2 = *(_DWORD *)a1;
  v3 = a2;
  v14 = *(_WORD *)(a1 + 4);
  v18 = MEMORY[0];
  v13 = v2;
  v10 = -86;
  v11 = 3;
  v12 = 7;
  v4 = exp_SceSysconForDriver_ksceSysconCmdExec((int)&v9, 0);
  if ( v4 >= 0 )
  {
    v5 = v3;
    if ( v3 )
    {
      v5 = 0;
      v6 = v16;
      v7 = v17;
      *(_DWORD *)v3 = v15;
      *(_WORD *)(v3 + 4) = v6;
      *(_BYTE *)(v3 + 6) = v7;
    }
  }
  else
  {
    v5 = v4;
  }
  result = v5;
  if ( v18 != MEMORY[0] )
    imp_SceSysclibForDriver___stack_chk_fail(v5);
  return result;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0xF648BE61]
signed int __fastcall exp_SceSysconForDriver_0xF648BE61(int a1, signed int a2)
{
  int v2; // r7
  signed int v3; // r5
  signed int v4; // r0
  signed int v5; // r1
  __int16 v6; // r2
  char v7; // r3
  signed int result; // r0
  int v9; // [sp+0h] [bp-A0h]
  char v10; // [sp+10h] [bp-90h]
  char v11; // [sp+11h] [bp-8Fh]
  char v12; // [sp+12h] [bp-8Eh]
  int v13; // [sp+13h] [bp-8Dh]
  __int16 v14; // [sp+17h] [bp-89h]
  int v15; // [sp+34h] [bp-6Ch]
  __int16 v16; // [sp+38h] [bp-68h]
  char v17; // [sp+3Ah] [bp-66h]
  int v18; // [sp+84h] [bp-1Ch]

  v2 = *(_DWORD *)a1;
  v3 = a2;
  v14 = *(_WORD *)(a1 + 4);
  v18 = MEMORY[0];
  v13 = v2;
  v10 = -70;
  v11 = 3;
  v12 = 7;
  v4 = exp_SceSysconForDriver_ksceSysconCmdExec((int)&v9, 0);
  if ( v4 >= 0 )
  {
    v5 = v3;
    if ( v3 )
    {
      v5 = 0;
      v6 = v16;
      v7 = v17;
      *(_DWORD *)v3 = v15;
      *(_WORD *)(v3 + 4) = v6;
      *(_BYTE *)(v3 + 6) = v7;
    }
  }
  else
  {
    v5 = v4;
  }
  result = v5;
  if ( v18 != MEMORY[0] )
    imp_SceSysclibForDriver___stack_chk_fail(v5);
  return result;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x357CC9D9]
int __fastcall exp_SceSysconForDriver_0x357CC9D9(unsigned __int8 a1, __int16 a2)
{
  return sub_810030C8(a1 | (unsigned __int16)(a2 << 8), 0x3ABu, 3u);
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0xCCA56A16]
int __fastcall exp_SceSysconForDriver_0xCCA56A16(unsigned __int8 a1, __int16 a2)
{
  return sub_810030C8(a1 | (unsigned __int16)(a2 << 8), 0x3BBu, 3u);
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x3664E2C0]
signed int __fastcall exp_SceSysconForDriver_0x3664E2C0(_DWORD *a1)
{
  *a1 = 0;
  return sub_81002D20(a1, 0x3ACu);
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x2E6D97CD]
signed int __fastcall exp_SceSysconForDriver_0x2E6D97CD(_DWORD *a1)
{
  *a1 = 0;
  return sub_81002D20(a1, 0x3BCu);
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0xB9835A63]
signed int __fastcall exp_SceSysconForDriver_0xB9835A63(__int16 a1, __int16 a2, int a3, unsigned int a4, _DWORD *a5)
{
  signed int result; // r0
  int v6; // [sp+0h] [bp-A0h]
  char v7; // [sp+10h] [bp-90h]
  char v8; // [sp+11h] [bp-8Fh]
  char v9; // [sp+12h] [bp-8Eh]
  __int16 v10; // [sp+13h] [bp-8Dh]
  __int16 v11; // [sp+15h] [bp-8Bh]
  char v12; // [sp+17h] [bp-89h]
  char v13; // [sp+18h] [bp-88h]
  unsigned __int8 v14; // [sp+36h] [bp-6Ah]
  int v15; // [sp+84h] [bp-1Ch]

  v15 = MEMORY[0];
  if ( a4 > 0x18 )
  {
    result = -2145058815;
  }
  else
  {
    v10 = a1;
    v9 = a4 + 6;
    v7 = -94;
    v8 = 3;
    v11 = a2;
    v12 = a4;
    imp_SceSysclibForDriver_memcpy(&v13, a3, a4);
    result = exp_SceSysconForDriver_ksceSysconCmdExec((int)&v6, 0);
    if ( result >= 0 )
      *a5 = v14;
  }
  if ( v15 != MEMORY[0] )
    imp_SceSysclibForDriver___stack_chk_fail(result);
  return result;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0xCD6A6BC7]
signed int __fastcall exp_SceSysconForDriver_0xCD6A6BC7(__int16 a1, __int16 a2, int a3, unsigned int a4, _DWORD *a5)
{
  signed int result; // r0
  int v6; // [sp+0h] [bp-A0h]
  char v7; // [sp+10h] [bp-90h]
  char v8; // [sp+11h] [bp-8Fh]
  char v9; // [sp+12h] [bp-8Eh]
  __int16 v10; // [sp+13h] [bp-8Dh]
  __int16 v11; // [sp+15h] [bp-8Bh]
  char v12; // [sp+17h] [bp-89h]
  char v13; // [sp+18h] [bp-88h]
  unsigned __int8 v14; // [sp+36h] [bp-6Ah]
  int v15; // [sp+84h] [bp-1Ch]

  v15 = MEMORY[0];
  if ( a4 > 0x18 )
  {
    result = -2145058815;
  }
  else
  {
    v10 = a1;
    v9 = a4 + 6;
    v7 = -78;
    v8 = 3;
    v11 = a2;
    v12 = a4;
    imp_SceSysclibForDriver_memcpy(&v13, a3, a4);
    result = exp_SceSysconForDriver_ksceSysconCmdExec((int)&v6, 0);
    if ( result >= 0 )
      *a5 = v14;
  }
  if ( v15 != MEMORY[0] )
    imp_SceSysclibForDriver___stack_chk_fail(result);
  return result;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0xB63AF451]
signed int __fastcall exp_SceSysconForDriver_0xB63AF451(__int16 a1, _DWORD *a2)
{
  _DWORD *v2; // r8
  signed int result; // r0
  int v4; // r2
  int v5; // [sp+0h] [bp-A0h]
  char v6; // [sp+10h] [bp-90h]
  char v7; // [sp+11h] [bp-8Fh]
  char v8; // [sp+12h] [bp-8Eh]
  __int16 v9; // [sp+13h] [bp-8Dh]
  unsigned __int8 v10; // [sp+36h] [bp-6Ah]
  int v11; // [sp+84h] [bp-1Ch]

  v2 = a2;
  v7 = 3;
  v8 = 3;
  v9 = a1;
  v11 = MEMORY[0];
  v6 = -93;
  result = exp_SceSysconForDriver_ksceSysconCmdExec((int)&v5, 0);
  v4 = v11;
  if ( result >= 0 )
    *v2 = v10;
  if ( v4 != MEMORY[0] )
    imp_SceSysclibForDriver___stack_chk_fail(result);
  return result;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x8EBA8A26]
signed int __fastcall exp_SceSysconForDriver_0x8EBA8A26(__int16 a1, _DWORD *a2)
{
  _DWORD *v2; // r8
  signed int result; // r0
  int v4; // r2
  int v5; // [sp+0h] [bp-A0h]
  char v6; // [sp+10h] [bp-90h]
  char v7; // [sp+11h] [bp-8Fh]
  char v8; // [sp+12h] [bp-8Eh]
  __int16 v9; // [sp+13h] [bp-8Dh]
  unsigned __int8 v10; // [sp+36h] [bp-6Ah]
  int v11; // [sp+84h] [bp-1Ch]

  v2 = a2;
  v7 = 3;
  v8 = 3;
  v9 = a1;
  v11 = MEMORY[0];
  v6 = -77;
  result = exp_SceSysconForDriver_ksceSysconCmdExec((int)&v5, 0);
  v4 = v11;
  if ( result >= 0 )
    *v2 = v10;
  if ( v4 != MEMORY[0] )
    imp_SceSysclibForDriver___stack_chk_fail(result);
  return result;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0xB3960346]
signed int __fastcall exp_SceSysconForDriver_0xB3960346(_BYTE *a1)
{
  _BYTE *v1; // r5
  signed int result; // r0
  int v3; // [sp+0h] [bp-98h]
  char v4; // [sp+10h] [bp-88h]
  char v5; // [sp+11h] [bp-87h]
  char v6; // [sp+12h] [bp-86h]
  char v7; // [sp+34h] [bp-64h]
  int v8; // [sp+84h] [bp-14h]

  v1 = a1;
  v8 = MEMORY[0];
  if ( a1 )
  {
    v4 = -83;
    v5 = 3;
    v6 = 1;
    result = exp_SceSysconForDriver_ksceSysconCmdExec((int)&v3, 0);
    if ( result >= 0 )
    {
      result = 0;
      *v1 = v7;
    }
  }
  else
  {
    result = -2145058814;
  }
  if ( v8 != MEMORY[0] )
    imp_SceSysclibForDriver___stack_chk_fail(result);
  return result;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x830A9347]
signed int __fastcall exp_SceSysconForDriver_0x830A9347(_BYTE *a1)
{
  _BYTE *v1; // r5
  signed int result; // r0
  int v3; // [sp+0h] [bp-98h]
  char v4; // [sp+10h] [bp-88h]
  char v5; // [sp+11h] [bp-87h]
  char v6; // [sp+12h] [bp-86h]
  char v7; // [sp+34h] [bp-64h]
  int v8; // [sp+84h] [bp-14h]

  v1 = a1;
  v8 = MEMORY[0];
  if ( a1 )
  {
    v4 = -67;
    v5 = 3;
    v6 = 1;
    result = exp_SceSysconForDriver_ksceSysconCmdExec((int)&v3, 0);
    if ( result >= 0 )
    {
      result = 0;
      *v1 = v7;
    }
  }
  else
  {
    result = -2145058814;
  }
  if ( v8 != MEMORY[0] )
    imp_SceSysclibForDriver___stack_chk_fail(result);
  return result;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x93FE51F5]
signed int __fastcall exp_SceSysconForDriver_0x93FE51F5(int *a1)
{
  int *v1; // r5
  signed int result; // r0
  int v3; // [sp+0h] [bp-98h]
  char v4; // [sp+10h] [bp-88h]
  char v5; // [sp+11h] [bp-87h]
  char v6; // [sp+12h] [bp-86h]
  unsigned __int8 v7; // [sp+34h] [bp-64h]
  unsigned __int8 v8; // [sp+35h] [bp-63h]
  unsigned __int8 v9; // [sp+36h] [bp-62h]
  int v10; // [sp+84h] [bp-14h]

  v1 = a1;
  v10 = MEMORY[0];
  if ( a1 )
  {
    v4 = -82;
    v5 = 3;
    v6 = 1;
    result = exp_SceSysconForDriver_ksceSysconCmdExec((int)&v3, 0);
    if ( result >= 0 )
    {
      result = 0;
      *v1 = (v8 << 8) | (v9 << 16) | v7;
    }
  }
  else
  {
    result = -2145058814;
  }
  if ( v10 != MEMORY[0] )
    imp_SceSysclibForDriver___stack_chk_fail(result);
  return result;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x6A093056]
signed int __fastcall exp_SceSysconForDriver_0x6A093056(int *a1)
{
  int *v1; // r5
  signed int result; // r0
  int v3; // [sp+0h] [bp-98h]
  char v4; // [sp+10h] [bp-88h]
  char v5; // [sp+11h] [bp-87h]
  char v6; // [sp+12h] [bp-86h]
  unsigned __int8 v7; // [sp+34h] [bp-64h]
  unsigned __int8 v8; // [sp+35h] [bp-63h]
  unsigned __int8 v9; // [sp+36h] [bp-62h]
  int v10; // [sp+84h] [bp-14h]

  v1 = a1;
  v10 = MEMORY[0];
  if ( a1 )
  {
    v4 = -66;
    v5 = 3;
    v6 = 1;
    result = exp_SceSysconForDriver_ksceSysconCmdExec((int)&v3, 0);
    if ( result >= 0 )
    {
      result = 0;
      *v1 = (v8 << 8) | (v9 << 16) | v7;
    }
  }
  else
  {
    result = -2145058814;
  }
  if ( v10 != MEMORY[0] )
    imp_SceSysclibForDriver___stack_chk_fail(result);
  return result;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0xB8F4F4E3]
int __fastcall exp_SceSysconForDriver_0xB8F4F4E3(unsigned __int8 a1)
{
  return sub_810030C8(a1, 0x3AFu, 2u);
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x240A604E]
int __fastcall exp_SceSysconForDriver_0x240A604E(unsigned __int8 a1)
{
  return sub_810030C8(a1, 0x3BFu, 2u);
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x48ED8981]
int exp_SceSysconForDriver_0x48ED8981()
{
  return sub_810030C8(0, 0x392u, 2u);
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x060E55C1]
int exp_SceSysconForDriver_0x060E55C1()
{
  return sub_810030C8(1u, 0x392u, 2u);
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x9A28BEEF]
int exp_SceSysconForDriver_0x9A28BEEF()
{
  return sub_810030C8(0, 0x393u, 2u);
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x8874EF45]
int exp_SceSysconForDriver_0x8874EF45()
{
  return sub_810030C8(1u, 0x393u, 2u);
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0xD9B11C0A]
signed int __fastcall exp_SceSysconForDriver_0xD9B11C0A(_DWORD *a1)
{
  _DWORD *v1; // r6
  signed int result; // r0
  int v3; // [sp+0h] [bp-A0h]
  char v4; // [sp+10h] [bp-90h]
  char v5; // [sp+11h] [bp-8Fh]
  char v6; // [sp+12h] [bp-8Eh]
  char v7; // [sp+13h] [bp-8Dh]
  unsigned __int8 v8; // [sp+34h] [bp-6Ch]
  int v9; // [sp+84h] [bp-1Ch]

  v1 = a1;
  v9 = MEMORY[0];
  v7 = 0;
  v4 = -99;
  v5 = 3;
  v6 = 2;
  result = exp_SceSysconForDriver_ksceSysconCmdExec((int)&v3, 0);
  if ( result >= 0 )
  {
    if ( v1 )
    {
      result = 0;
      *v1 = v8;
    }
    else
    {
      result = 0;
    }
  }
  if ( v9 != MEMORY[0] )
    imp_SceSysclibForDriver___stack_chk_fail(result);
  return result;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0xFB37D7AD]
signed int __fastcall exp_SceSysconForDriver_0xFB37D7AD(_DWORD *a1)
{
  _DWORD *v1; // r5
  signed int result; // r0
  int v3; // [sp+0h] [bp-A0h]
  char v4; // [sp+10h] [bp-90h]
  char v5; // [sp+11h] [bp-8Fh]
  char v6; // [sp+12h] [bp-8Eh]
  char v7; // [sp+13h] [bp-8Dh]
  unsigned __int8 v8; // [sp+34h] [bp-6Ch]
  int v9; // [sp+84h] [bp-1Ch]

  v1 = a1;
  v9 = MEMORY[0];
  v4 = -99;
  v5 = 3;
  v6 = 2;
  v7 = 1;
  result = exp_SceSysconForDriver_ksceSysconCmdExec((int)&v3, 0);
  if ( result >= 0 )
  {
    if ( v1 )
    {
      result = 0;
      *v1 = v8;
    }
    else
    {
      result = 0;
    }
  }
  if ( v9 != MEMORY[0] )
    imp_SceSysclibForDriver___stack_chk_fail(result);
  return result;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x5946B29B]
int exp_SceSysconForDriver_0x5946B29B()
{
  return sub_810030C8(0, 0x3C2u, 2u);
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x10327C64]
int exp_SceSysconForDriver_0x10327C64()
{
  return sub_810030C8(1u, 0x3C2u, 2u);
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x4DFA12FB]
signed int __fastcall exp_SceSysconForDriver_0x4DFA12FB(__int16 a1, __int16 a2, int a3, unsigned int a4, _DWORD *a5)
{
  unsigned int v5; // r5
  __int16 v6; // r7
  __int16 v7; // r6
  int v8; // r10
  signed int result; // r0
  int v10; // [sp+0h] [bp-130h]
  int v11; // [sp+4h] [bp-12Ch]
  char v12; // [sp+10h] [bp-120h]
  char v13; // [sp+11h] [bp-11Fh]
  char v14; // [sp+12h] [bp-11Eh]
  char v15; // [sp+13h] [bp-11Dh]
  __int16 v16; // [sp+14h] [bp-11Ch]
  __int16 v17; // [sp+16h] [bp-11Ah]
  char v18; // [sp+18h] [bp-118h]
  unsigned __int8 v19; // [sp+36h] [bp-FAh]
  char *v20; // [sp+50h] [bp-E0h]
  char v21; // [sp+84h] [bp-ACh]
  char v22; // [sp+85h] [bp-ABh]
  char v23; // [sp+86h] [bp-AAh]
  __int16 v24; // [sp+88h] [bp-A8h]
  __int16 v25; // [sp+8Ah] [bp-A6h]
  char v26; // [sp+8Ch] [bp-A4h]
  char v27; // [sp+8Dh] [bp-A3h]
  int v28; // [sp+104h] [bp-2Ch]

  v5 = a4;
  v6 = a1;
  v7 = a2;
  v8 = a3;
  v28 = MEMORY[0];
  if ( a4 > 0x20 )
  {
    result = -2145058815;
  }
  else
  {
    imp_SceSysclibForDriver_memset(&v21, 0, 128);
    v20 = &v21;
    v26 = v5;
    v15 = 0;
    v24 = v6;
    v18 = v5;
    v16 = v6;
    v17 = v7;
    v25 = v7;
    v14 = v5 + 7;
    v23 = v5 + 7;
    v11 = 2048;
    v12 = -61;
    v21 = -61;
    v13 = 3;
    v22 = 3;
    imp_SceSysclibForDriver_memcpy(&v27, v8, v5);
    result = exp_SceSysconForDriver_ksceSysconCmdExec((int)&v10, 0x800u);
    if ( result >= 0 && a5 )
      *a5 = v19;
  }
  if ( v28 != MEMORY[0] )
    imp_SceSysclibForDriver___stack_chk_fail(result);
  return result;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x3B06F619]
signed int __fastcall exp_SceSysconForDriver_0x3B06F619(__int16 a1, __int16 a2, int a3, unsigned int a4, _DWORD *a5)
{
  unsigned int v5; // r5
  __int16 v6; // r7
  __int16 v7; // r6
  int v8; // r10
  signed int result; // r0
  int v10; // [sp+0h] [bp-128h]
  int v11; // [sp+4h] [bp-124h]
  char v12; // [sp+10h] [bp-118h]
  char v13; // [sp+11h] [bp-117h]
  char v14; // [sp+12h] [bp-116h]
  char v15; // [sp+13h] [bp-115h]
  __int16 v16; // [sp+14h] [bp-114h]
  __int16 v17; // [sp+16h] [bp-112h]
  char v18; // [sp+18h] [bp-110h]
  unsigned __int8 v19; // [sp+36h] [bp-F2h]
  char *v20; // [sp+50h] [bp-D8h]
  char v21; // [sp+84h] [bp-A4h]
  char v22; // [sp+85h] [bp-A3h]
  char v23; // [sp+86h] [bp-A2h]
  char v24; // [sp+87h] [bp-A1h]
  __int16 v25; // [sp+88h] [bp-A0h]
  __int16 v26; // [sp+8Ah] [bp-9Eh]
  char v27; // [sp+8Ch] [bp-9Ch]
  char v28; // [sp+8Dh] [bp-9Bh]
  int v29; // [sp+104h] [bp-24h]

  v5 = a4;
  v6 = a1;
  v7 = a2;
  v8 = a3;
  v29 = MEMORY[0];
  if ( a4 > 0x20 )
  {
    result = -2145058815;
  }
  else
  {
    imp_SceSysclibForDriver_memset(&v21, 0, 128);
    v20 = &v21;
    v27 = v5;
    v18 = v5;
    v14 = v5 + 7;
    v23 = v5 + 7;
    v25 = v6;
    v15 = 1;
    v24 = 1;
    v16 = v6;
    v17 = v7;
    v26 = v7;
    v11 = 2048;
    v12 = -61;
    v21 = -61;
    v13 = 3;
    v22 = 3;
    imp_SceSysclibForDriver_memcpy(&v28, v8, v5);
    result = exp_SceSysconForDriver_ksceSysconCmdExec((int)&v10, 0x800u);
    if ( result >= 0 && a5 )
      *a5 = v19;
  }
  if ( v29 != MEMORY[0] )
    imp_SceSysclibForDriver___stack_chk_fail(result);
  return result;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x68366D29]
signed int __fastcall exp_SceSysconForDriver_0x68366D29(__int16 a1, _DWORD *a2)
{
  _DWORD *v2; // r6
  signed int result; // r0
  int v4; // [sp+0h] [bp-A0h]
  char v5; // [sp+10h] [bp-90h]
  char v6; // [sp+11h] [bp-8Fh]
  char v7; // [sp+12h] [bp-8Eh]
  char v8; // [sp+13h] [bp-8Dh]
  __int16 v9; // [sp+14h] [bp-8Ch]
  unsigned __int8 v10; // [sp+36h] [bp-6Ah]
  int v11; // [sp+84h] [bp-1Ch]

  v2 = a2;
  v9 = a1;
  v11 = MEMORY[0];
  v8 = 0;
  v5 = -60;
  v6 = 3;
  v7 = 4;
  result = exp_SceSysconForDriver_ksceSysconCmdExec((int)&v4, 0);
  if ( result >= 0 )
  {
    if ( v2 )
    {
      result = 0;
      *v2 = v10;
    }
    else
    {
      result = 0;
    }
  }
  if ( v11 != MEMORY[0] )
    imp_SceSysclibForDriver___stack_chk_fail(result);
  return result;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x2BCE7F5A]
signed int __fastcall exp_SceSysconForDriver_0x2BCE7F5A(__int16 a1, _DWORD *a2)
{
  _DWORD *v2; // r5
  signed int result; // r0
  int v4; // [sp+0h] [bp-A0h]
  char v5; // [sp+10h] [bp-90h]
  char v6; // [sp+11h] [bp-8Fh]
  char v7; // [sp+12h] [bp-8Eh]
  char v8; // [sp+13h] [bp-8Dh]
  __int16 v9; // [sp+14h] [bp-8Ch]
  unsigned __int8 v10; // [sp+36h] [bp-6Ah]
  int v11; // [sp+84h] [bp-1Ch]

  v2 = a2;
  v9 = a1;
  v11 = MEMORY[0];
  v5 = -60;
  v6 = 3;
  v7 = 4;
  v8 = 1;
  result = exp_SceSysconForDriver_ksceSysconCmdExec((int)&v4, 0);
  if ( result >= 0 )
  {
    if ( v2 )
    {
      result = 0;
      *v2 = v10;
    }
    else
    {
      result = 0;
    }
  }
  if ( v11 != MEMORY[0] )
    imp_SceSysclibForDriver___stack_chk_fail(result);
  return result;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x9A4304F2]
signed int __fastcall exp_SceSysconForDriver_0x9A4304F2(int a1)
{
  int v1; // r4
  signed int result; // r0
  char v3; // lr
  char v4; // r7
  char v5; // r6
  char v6; // r1
  char v7; // r2
  char v8; // r3
  int v9; // [sp+0h] [bp-A0h]
  char v10; // [sp+10h] [bp-90h]
  char v11; // [sp+11h] [bp-8Fh]
  char v12; // [sp+12h] [bp-8Eh]
  char v13; // [sp+13h] [bp-8Dh]
  char v14; // [sp+34h] [bp-6Ch]
  char v15; // [sp+35h] [bp-6Bh]
  unsigned __int8 v16; // [sp+37h] [bp-69h]
  unsigned __int8 v17; // [sp+38h] [bp-68h]
  char v18; // [sp+39h] [bp-67h]
  char v19; // [sp+3Ah] [bp-66h]
  char v20; // [sp+3Bh] [bp-65h]
  char v21; // [sp+3Ch] [bp-64h]
  int v22; // [sp+84h] [bp-1Ch]

  v1 = a1;
  v22 = MEMORY[0];
  v13 = 0;
  v10 = -59;
  v11 = 3;
  v12 = 2;
  result = exp_SceSysconForDriver_ksceSysconCmdExec((int)&v9, 0);
  if ( result >= 0 )
  {
    if ( v1 )
    {
      result = 0;
      v3 = v14;
      v4 = v15;
      v5 = v18;
      v6 = v19;
      v7 = v20;
      v8 = v21;
      *(_WORD *)(v1 + 2) = v16 | (unsigned __int16)(v17 << 8);
      *(_BYTE *)v1 = v3;
      *(_BYTE *)(v1 + 1) = v4;
      *(_BYTE *)(v1 + 4) = v5;
      *(_BYTE *)(v1 + 5) = v6;
      *(_BYTE *)(v1 + 6) = v7;
      *(_BYTE *)(v1 + 7) = v8;
    }
    else
    {
      result = 0;
    }
  }
  if ( v22 != MEMORY[0] )
    imp_SceSysclibForDriver___stack_chk_fail(result);
  return result;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0xC2955814]
signed int __fastcall exp_SceSysconForDriver_0xC2955814(int a1)
{
  int v1; // r4
  signed int result; // r0
  char v3; // lr
  char v4; // r7
  char v5; // r6
  char v6; // r1
  char v7; // r2
  char v8; // r3
  int v9; // [sp+0h] [bp-A0h]
  char v10; // [sp+10h] [bp-90h]
  char v11; // [sp+11h] [bp-8Fh]
  char v12; // [sp+12h] [bp-8Eh]
  char v13; // [sp+13h] [bp-8Dh]
  char v14; // [sp+34h] [bp-6Ch]
  char v15; // [sp+35h] [bp-6Bh]
  unsigned __int8 v16; // [sp+37h] [bp-69h]
  unsigned __int8 v17; // [sp+38h] [bp-68h]
  char v18; // [sp+39h] [bp-67h]
  char v19; // [sp+3Ah] [bp-66h]
  char v20; // [sp+3Bh] [bp-65h]
  char v21; // [sp+3Ch] [bp-64h]
  int v22; // [sp+84h] [bp-1Ch]

  v1 = a1;
  v22 = MEMORY[0];
  v10 = -59;
  v11 = 3;
  v12 = 2;
  v13 = 1;
  result = exp_SceSysconForDriver_ksceSysconCmdExec((int)&v9, 0);
  if ( result >= 0 )
  {
    if ( v1 )
    {
      result = 0;
      v3 = v14;
      v4 = v15;
      v5 = v18;
      v6 = v19;
      v7 = v20;
      v8 = v21;
      *(_WORD *)(v1 + 2) = v16 | (unsigned __int16)(v17 << 8);
      *(_BYTE *)v1 = v3;
      *(_BYTE *)(v1 + 1) = v4;
      *(_BYTE *)(v1 + 4) = v5;
      *(_BYTE *)(v1 + 5) = v6;
      *(_BYTE *)(v1 + 6) = v7;
      *(_BYTE *)(v1 + 7) = v8;
    }
    else
    {
      result = 0;
    }
  }
  if ( v22 != MEMORY[0] )
    imp_SceSysclibForDriver___stack_chk_fail(result);
  return result;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0xF162D3D6]
signed int __fastcall exp_SceSysconForDriver_0xF162D3D6(_DWORD *a1)
{
  _DWORD *v1; // r6
  signed int result; // r0
  int v3; // [sp+0h] [bp-A0h]
  char v4; // [sp+10h] [bp-90h]
  char v5; // [sp+11h] [bp-8Fh]
  char v6; // [sp+12h] [bp-8Eh]
  char v7; // [sp+13h] [bp-8Dh]
  unsigned __int8 v8; // [sp+34h] [bp-6Ch]
  int v9; // [sp+84h] [bp-1Ch]

  v1 = a1;
  v9 = MEMORY[0];
  v7 = 0;
  v4 = -58;
  v5 = 3;
  v6 = 2;
  result = exp_SceSysconForDriver_ksceSysconCmdExec((int)&v3, 0);
  if ( result >= 0 )
  {
    if ( v1 )
    {
      result = 0;
      *v1 = v8;
    }
    else
    {
      result = 0;
    }
  }
  if ( v9 != MEMORY[0] )
    imp_SceSysclibForDriver___stack_chk_fail(result);
  return result;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x25144105]
signed int __fastcall exp_SceSysconForDriver_0x25144105(_DWORD *a1)
{
  _DWORD *v1; // r5
  signed int result; // r0
  int v3; // [sp+0h] [bp-A0h]
  char v4; // [sp+10h] [bp-90h]
  char v5; // [sp+11h] [bp-8Fh]
  char v6; // [sp+12h] [bp-8Eh]
  char v7; // [sp+13h] [bp-8Dh]
  unsigned __int8 v8; // [sp+34h] [bp-6Ch]
  int v9; // [sp+84h] [bp-1Ch]

  v1 = a1;
  v9 = MEMORY[0];
  v4 = -58;
  v5 = 3;
  v6 = 2;
  v7 = 1;
  result = exp_SceSysconForDriver_ksceSysconCmdExec((int)&v3, 0);
  if ( result >= 0 )
  {
    if ( v1 )
    {
      result = 0;
      *v1 = v8;
    }
    else
    {
      result = 0;
    }
  }
  if ( v9 != MEMORY[0] )
    imp_SceSysclibForDriver___stack_chk_fail(result);
  return result;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x9C840439]
signed int __fastcall exp_SceSysconForDriver_0x9C840439(_DWORD *a1)
{
  _DWORD *v1; // r6
  signed int result; // r0
  int v3; // [sp+0h] [bp-A0h]
  char v4; // [sp+10h] [bp-90h]
  char v5; // [sp+11h] [bp-8Fh]
  char v6; // [sp+12h] [bp-8Eh]
  char v7; // [sp+13h] [bp-8Dh]
  unsigned __int8 v8; // [sp+34h] [bp-6Ch]
  int v9; // [sp+84h] [bp-1Ch]

  v1 = a1;
  v9 = MEMORY[0];
  v7 = 0;
  v4 = -57;
  v5 = 3;
  v6 = 2;
  result = exp_SceSysconForDriver_ksceSysconCmdExec((int)&v3, 0);
  if ( result >= 0 )
  {
    if ( v1 )
    {
      result = 0;
      *v1 = v8;
    }
    else
    {
      result = 0;
    }
  }
  if ( v9 != MEMORY[0] )
    imp_SceSysclibForDriver___stack_chk_fail(result);
  return result;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0xD632D197]
signed int __fastcall exp_SceSysconForDriver_0xD632D197(_DWORD *a1)
{
  _DWORD *v1; // r5
  signed int result; // r0
  int v3; // [sp+0h] [bp-A0h]
  char v4; // [sp+10h] [bp-90h]
  char v5; // [sp+11h] [bp-8Fh]
  char v6; // [sp+12h] [bp-8Eh]
  char v7; // [sp+13h] [bp-8Dh]
  unsigned __int8 v8; // [sp+34h] [bp-6Ch]
  int v9; // [sp+84h] [bp-1Ch]

  v1 = a1;
  v9 = MEMORY[0];
  v4 = -57;
  v5 = 3;
  v6 = 2;
  v7 = 1;
  result = exp_SceSysconForDriver_ksceSysconCmdExec((int)&v3, 0);
  if ( result >= 0 )
  {
    if ( v1 )
    {
      result = 0;
      *v1 = v8;
    }
    else
    {
      result = 0;
    }
  }
  if ( v9 != MEMORY[0] )
    imp_SceSysclibForDriver___stack_chk_fail(result);
  return result;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x6450D6BE]
signed int __fastcall exp_SceSysconForDriver_0x6450D6BE(__int16 a1, _DWORD *a2)
{
  _DWORD *v2; // r6
  signed int result; // r0
  int v4; // [sp+0h] [bp-A0h]
  char v5; // [sp+10h] [bp-90h]
  char v6; // [sp+11h] [bp-8Fh]
  char v7; // [sp+12h] [bp-8Eh]
  char v8; // [sp+13h] [bp-8Dh]
  __int16 v9; // [sp+14h] [bp-8Ch]
  unsigned __int8 v10; // [sp+36h] [bp-6Ah]
  int v11; // [sp+84h] [bp-1Ch]

  v2 = a2;
  v9 = a1;
  v11 = MEMORY[0];
  v8 = 0;
  v5 = -56;
  v6 = 3;
  v7 = 4;
  result = exp_SceSysconForDriver_ksceSysconCmdExec((int)&v4, 0);
  if ( result >= 0 )
  {
    if ( v2 )
    {
      result = 0;
      *v2 = v10;
    }
    else
    {
      result = 0;
    }
  }
  if ( v11 != MEMORY[0] )
    imp_SceSysclibForDriver___stack_chk_fail(result);
  return result;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x16A466E7]
signed int __fastcall exp_SceSysconForDriver_0x16A466E7(__int16 a1, _DWORD *a2)
{
  _DWORD *v2; // r5
  signed int result; // r0
  int v4; // [sp+0h] [bp-A0h]
  char v5; // [sp+10h] [bp-90h]
  char v6; // [sp+11h] [bp-8Fh]
  char v7; // [sp+12h] [bp-8Eh]
  char v8; // [sp+13h] [bp-8Dh]
  __int16 v9; // [sp+14h] [bp-8Ch]
  unsigned __int8 v10; // [sp+36h] [bp-6Ah]
  int v11; // [sp+84h] [bp-1Ch]

  v2 = a2;
  v9 = a1;
  v11 = MEMORY[0];
  v5 = -56;
  v6 = 3;
  v7 = 4;
  v8 = 1;
  result = exp_SceSysconForDriver_ksceSysconCmdExec((int)&v4, 0);
  if ( result >= 0 )
  {
    if ( v2 )
    {
      result = 0;
      *v2 = v10;
    }
    else
    {
      result = 0;
    }
  }
  if ( v11 != MEMORY[0] )
    imp_SceSysclibForDriver___stack_chk_fail(result);
  return result;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x9962CE6F]
signed int __fastcall exp_SceSysconForDriver_0x9962CE6F(char a1, _DWORD *a2, _WORD *a3, _WORD *a4)
{
  _DWORD *v4; // r8
  _WORD *v5; // r9
  _WORD *v6; // r7
  signed int result; // r0
  int v8; // [sp+0h] [bp-A8h]
  char v9; // [sp+10h] [bp-98h]
  char v10; // [sp+11h] [bp-97h]
  char v11; // [sp+12h] [bp-96h]
  char v12; // [sp+13h] [bp-95h]
  char v13; // [sp+14h] [bp-94h]
  unsigned __int8 v14; // [sp+34h] [bp-74h]
  unsigned __int8 v15; // [sp+35h] [bp-73h]
  unsigned __int8 v16; // [sp+36h] [bp-72h]
  unsigned __int8 v17; // [sp+37h] [bp-71h]
  unsigned __int8 v18; // [sp+38h] [bp-70h]
  int v19; // [sp+84h] [bp-24h]

  v13 = a1;
  v4 = a2;
  v5 = a3;
  v6 = a4;
  v19 = MEMORY[0];
  v12 = 0;
  v9 = -55;
  v10 = 3;
  v11 = 3;
  result = exp_SceSysconForDriver_ksceSysconCmdExec((int)&v8, 0);
  if ( result >= 0 )
  {
    if ( v4 )
      *v4 = v14;
    if ( v5 )
      *v5 = v15 | (unsigned __int16)(v16 << 8);
    if ( v6 )
      *v6 = v17 | (unsigned __int16)(v18 << 8);
  }
  if ( v19 != MEMORY[0] )
    imp_SceSysclibForDriver___stack_chk_fail(result);
  return result;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x31B8F7BE]
signed int __fastcall exp_SceSysconForDriver_0x31B8F7BE(char a1, _DWORD *a2, _WORD *a3, _WORD *a4)
{
  _DWORD *v4; // r7
  _WORD *v5; // r8
  _WORD *v6; // r6
  signed int result; // r0
  int v8; // [sp+0h] [bp-A8h]
  char v9; // [sp+10h] [bp-98h]
  char v10; // [sp+11h] [bp-97h]
  char v11; // [sp+12h] [bp-96h]
  char v12; // [sp+13h] [bp-95h]
  char v13; // [sp+14h] [bp-94h]
  unsigned __int8 v14; // [sp+34h] [bp-74h]
  unsigned __int8 v15; // [sp+35h] [bp-73h]
  unsigned __int8 v16; // [sp+36h] [bp-72h]
  unsigned __int8 v17; // [sp+37h] [bp-71h]
  unsigned __int8 v18; // [sp+38h] [bp-70h]
  int v19; // [sp+84h] [bp-24h]

  v13 = a1;
  v4 = a2;
  v5 = a3;
  v6 = a4;
  v19 = MEMORY[0];
  v9 = -55;
  v10 = 3;
  v11 = 3;
  v12 = 1;
  result = exp_SceSysconForDriver_ksceSysconCmdExec((int)&v8, 0);
  if ( result >= 0 )
  {
    if ( v4 )
      *v4 = v14;
    if ( v5 )
      *v5 = v15 | (unsigned __int16)(v16 << 8);
    if ( v6 )
      *v6 = v17 | (unsigned __int16)(v18 << 8);
  }
  if ( v19 != MEMORY[0] )
    imp_SceSysclibForDriver___stack_chk_fail(result);
  return result;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0xCA203A5F]
signed int __fastcall exp_SceSysconForDriver_0xCA203A5F(char a1, __int16 a2, unsigned int a3, int a4, _DWORD *a5)
{
  char v5; // r6
  char v6; // r11
  __int16 v7; // r7
  int v8; // r10
  signed int result; // r0
  signed int v10; // r6
  int v11; // [sp+0h] [bp-130h]
  int v12; // [sp+4h] [bp-12Ch]
  char v13; // [sp+10h] [bp-120h]
  char v14; // [sp+11h] [bp-11Fh]
  char v15; // [sp+12h] [bp-11Eh]
  char v16; // [sp+13h] [bp-11Dh]
  char v17; // [sp+14h] [bp-11Ch]
  __int16 v18; // [sp+15h] [bp-11Bh]
  char v19; // [sp+17h] [bp-119h]
  char v20; // [sp+18h] [bp-118h]
  unsigned __int8 v21; // [sp+34h] [bp-FCh]
  char *v22; // [sp+54h] [bp-DCh]
  int v23; // [sp+58h] [bp-D8h]
  char v24; // [sp+84h] [bp-ACh]
  int v25; // [sp+104h] [bp-2Ch]

  v5 = a3;
  v6 = a1;
  v7 = a2;
  v8 = a4;
  v25 = MEMORY[0];
  if ( a3 > 0x20 )
  {
    result = -2145058815;
  }
  else
  {
    imp_SceSysclibForDriver_memset(&v24, 0, 128);
    v23 = 128;
    v18 = v7;
    v19 = v5;
    v22 = &v24;
    v17 = v6;
    v13 = -54;
    v14 = 3;
    v15 = 7;
    v12 = 1024;
    v16 = 0;
    v20 = 0;
    result = exp_SceSysconForDriver_ksceSysconCmdExec((int)&v11, 0x400u);
    v10 = result;
    if ( result >= 0 )
    {
      if ( a5 )
        *a5 = v21;
      if ( v8 )
      {
        imp_SceSysclibForDriver_memcpy(v8, &v24, v21);
        result = v10;
      }
    }
  }
  if ( v25 != MEMORY[0] )
    imp_SceSysclibForDriver___stack_chk_fail(result);
  return result;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x55B6FEF5]
signed int __fastcall exp_SceSysconForDriver_0x55B6FEF5(char a1, __int16 a2, unsigned int a3, int a4, _DWORD *a5)
{
  char v5; // r6
  char v6; // r11
  __int16 v7; // r7
  int v8; // r10
  signed int result; // r0
  signed int v10; // r6
  int v11; // [sp+0h] [bp-130h]
  int v12; // [sp+4h] [bp-12Ch]
  char v13; // [sp+10h] [bp-120h]
  char v14; // [sp+11h] [bp-11Fh]
  char v15; // [sp+12h] [bp-11Eh]
  char v16; // [sp+13h] [bp-11Dh]
  char v17; // [sp+14h] [bp-11Ch]
  __int16 v18; // [sp+15h] [bp-11Bh]
  char v19; // [sp+17h] [bp-119h]
  char v20; // [sp+18h] [bp-118h]
  unsigned __int8 v21; // [sp+34h] [bp-FCh]
  char *v22; // [sp+54h] [bp-DCh]
  int v23; // [sp+58h] [bp-D8h]
  char v24; // [sp+84h] [bp-ACh]
  int v25; // [sp+104h] [bp-2Ch]

  v5 = a3;
  v6 = a1;
  v7 = a2;
  v8 = a4;
  v25 = MEMORY[0];
  if ( a3 > 0x20 )
  {
    result = -2145058815;
  }
  else
  {
    imp_SceSysclibForDriver_memset(&v24, 0, 128);
    v23 = 128;
    v17 = v6;
    v15 = 7;
    v19 = v5;
    v22 = &v24;
    v18 = v7;
    v13 = -54;
    v14 = 3;
    v16 = 1;
    v20 = 0;
    v12 = 1024;
    result = exp_SceSysconForDriver_ksceSysconCmdExec((int)&v11, 0x400u);
    v10 = result;
    if ( result >= 0 )
    {
      if ( a5 )
        *a5 = v21;
      if ( v8 )
      {
        imp_SceSysclibForDriver_memcpy(v8, &v24, v21);
        result = v10;
      }
    }
  }
  if ( v25 != MEMORY[0] )
    imp_SceSysclibForDriver___stack_chk_fail(result);
  return result;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x94418CB4]
signed int __fastcall exp_SceSysconForDriver_0x94418CB4(char a1, __int16 a2, unsigned int a3, int a4, _DWORD *a5)
{
  unsigned int v5; // r5
  char v6; // r11
  __int16 v7; // r7
  int v8; // r10
  signed int result; // r0
  int v10; // [sp+0h] [bp-130h]
  int v11; // [sp+4h] [bp-12Ch]
  char v12; // [sp+10h] [bp-120h]
  char v13; // [sp+11h] [bp-11Fh]
  char v14; // [sp+12h] [bp-11Eh]
  char v15; // [sp+13h] [bp-11Dh]
  char v16; // [sp+14h] [bp-11Ch]
  __int16 v17; // [sp+15h] [bp-11Bh]
  char v18; // [sp+17h] [bp-119h]
  unsigned __int8 v19; // [sp+34h] [bp-FCh]
  char *v20; // [sp+50h] [bp-E0h]
  char v21; // [sp+84h] [bp-ACh]
  char v22; // [sp+85h] [bp-ABh]
  char v23; // [sp+86h] [bp-AAh]
  char v24; // [sp+88h] [bp-A8h]
  __int16 v25; // [sp+89h] [bp-A7h]
  char v26; // [sp+8Bh] [bp-A5h]
  char v27; // [sp+8Ch] [bp-A4h]
  int v28; // [sp+104h] [bp-2Ch]

  v5 = a3;
  v6 = a1;
  v7 = a2;
  v8 = a4;
  v28 = MEMORY[0];
  if ( a3 > 0x20 )
  {
    result = -2145058815;
  }
  else
  {
    imp_SceSysclibForDriver_memset(&v21, 0, 128);
    v24 = v6;
    v20 = &v21;
    v16 = v6;
    v15 = 0;
    v14 = v5 + 6;
    v23 = v5 + 6;
    v17 = v7;
    v25 = v7;
    v26 = v5;
    v18 = v5;
    v11 = 2048;
    v12 = -53;
    v21 = -53;
    v13 = 3;
    v22 = 3;
    imp_SceSysclibForDriver_memcpy(&v27, v8, v5);
    result = exp_SceSysconForDriver_ksceSysconCmdExec((int)&v10, 0x800u);
    if ( result >= 0 && a5 )
      *a5 = v19;
  }
  if ( v28 != MEMORY[0] )
    imp_SceSysclibForDriver___stack_chk_fail(result);
  return result;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x7EC67149]
signed int __fastcall exp_SceSysconForDriver_0x7EC67149(char a1, __int16 a2, unsigned int a3, int a4, _DWORD *a5)
{
  unsigned int v5; // r5
  char v6; // r11
  __int16 v7; // r7
  int v8; // r10
  signed int result; // r0
  int v10; // [sp+0h] [bp-130h]
  int v11; // [sp+4h] [bp-12Ch]
  char v12; // [sp+10h] [bp-120h]
  char v13; // [sp+11h] [bp-11Fh]
  char v14; // [sp+12h] [bp-11Eh]
  char v15; // [sp+13h] [bp-11Dh]
  char v16; // [sp+14h] [bp-11Ch]
  __int16 v17; // [sp+15h] [bp-11Bh]
  char v18; // [sp+17h] [bp-119h]
  unsigned __int8 v19; // [sp+34h] [bp-FCh]
  char *v20; // [sp+50h] [bp-E0h]
  char v21; // [sp+84h] [bp-ACh]
  char v22; // [sp+85h] [bp-ABh]
  char v23; // [sp+86h] [bp-AAh]
  char v24; // [sp+87h] [bp-A9h]
  char v25; // [sp+88h] [bp-A8h]
  __int16 v26; // [sp+89h] [bp-A7h]
  char v27; // [sp+8Bh] [bp-A5h]
  char v28; // [sp+8Ch] [bp-A4h]
  int v29; // [sp+104h] [bp-2Ch]

  v5 = a3;
  v6 = a1;
  v7 = a2;
  v8 = a4;
  v29 = MEMORY[0];
  if ( a3 > 0x20 )
  {
    result = -2145058815;
  }
  else
  {
    imp_SceSysclibForDriver_memset(&v21, 0, 128);
    v25 = v6;
    v16 = v6;
    v14 = v5 + 6;
    v23 = v5 + 6;
    v15 = 1;
    v24 = 1;
    v17 = v7;
    v26 = v7;
    v27 = v5;
    v20 = &v21;
    v18 = v5;
    v11 = 2048;
    v12 = -53;
    v21 = -53;
    v13 = 3;
    v22 = 3;
    imp_SceSysclibForDriver_memcpy(&v28, v8, v5);
    result = exp_SceSysconForDriver_ksceSysconCmdExec((int)&v10, 0x800u);
    if ( result >= 0 && a5 )
      *a5 = v19;
  }
  if ( v29 != MEMORY[0] )
    imp_SceSysclibForDriver___stack_chk_fail(result);
  return result;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x9A7858B6]
int __fastcall exp_SceSysconForDriver_0x9A7858B6(char a1, int a2)
{
  return sub_810030C8(a1 & 1 | (a2 << 8), 0x481u, 3u);
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0xD01E64FC]
signed int __fastcall exp_SceSysconForDriver_0xD01E64FC(_DWORD *a1)
{
  *a1 = 0;
  return sub_81002D20(a1, 0x480u);
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x270B7B0B]
signed int __fastcall exp_SceSysconForDriver_0x270B7B0B(int a1, int a2)
{
  signed int result; // r0
  int v3; // [sp+0h] [bp-98h]
  char v4; // [sp+10h] [bp-88h]
  char v5; // [sp+11h] [bp-87h]
  char v6; // [sp+12h] [bp-86h]
  int v7; // [sp+54h] [bp-44h]
  int v8; // [sp+58h] [bp-40h]
  int v9; // [sp+5Ch] [bp-3Ch]
  int v10; // [sp+84h] [bp-14h]

  v7 = a1;
  v8 = a2;
  v5 = 4;
  v6 = 1;
  v10 = MEMORY[0];
  v4 = 0;
  result = exp_SceSysconForDriver_ksceSysconCmdExec((int)&v3, 0x400u);
  if ( result >= 0 )
    result = v9;
  if ( v10 != MEMORY[0] )
    imp_SceSysclibForDriver___stack_chk_fail(result);
  return result;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0xA2FE9BF9]
signed int __fastcall exp_SceSysconForDriver_0xA2FE9BF9(_DWORD *a1)
{
  *a1 = 0;
  return sub_81002D20(a1, 0x800u);
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x14B99945]
void exp_SceSysconForDriver_sceSysconPowerCtrlKermitResetForDriver()
{
    // MOVS R1, #0
    // MOVS R0, #2
    SecureMonitorCall();
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0xEF810687]
signed int __fastcall exp_SceSysconForDriver_ksceSysconGetUsbDetStatus(_DWORD *a1)
{
  *a1 = 0;
  return sub_81002D20(a1, 0x805u);
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0xE7F5D3DC]
signed int __fastcall exp_SceSysconForDriver_0xE7F5D3DC(_DWORD *a1)
{
  *a1 = 0;
  return sub_81002D20(a1, 0x806u);
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x253CC522]
signed int __fastcall exp_SceSysconForDriver_0x253CC522(_BYTE *a1)
{
  _BYTE *v1; // r6
  signed int result; // r0
  int v3; // r2
  char v4; // r3^1
  int v5; // [sp+0h] [bp-18h]
  int v6; // [sp+4h] [bp-14h]

  v1 = a1;
  v5 = 0;
  v6 = MEMORY[0];
  result = sub_81002D20(&v5, 0x807u);
  v3 = v6;
  if ( result >= 0 )
  {
    v4 = BYTE1(v5);
    result = 0;
    *v1 = v5;
    v1[1] = v4;
  }
  if ( v3 != MEMORY[0] )
    imp_SceSysclibForDriver___stack_chk_fail(result);
  return result;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x727F985A]
int __fastcall exp_SceSysconForDriver_0x727F985A(unsigned int a1)
{
  int result; // r0

  if ( a1 > 2 )
    result = -2145058815;
  else
    result = sub_810030C8(0, a1 + 2080, 1u);
  return result;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x175CE5A1]
signed int __fastcall exp_SceSysconForDriver_0x175CE5A1(signed int *a1)
{
  signed int *v1; // r5
  signed int v2; // r0
  int v3; // r1
  bool v4; // nf
  int v5; // r2
  signed int v6; // r6
  signed int result; // r0
  int v8; // r3
  int v9; // [sp+0h] [bp-A0h]
  char v10; // [sp+10h] [bp-90h]
  char v11; // [sp+11h] [bp-8Fh]
  char v12; // [sp+12h] [bp-8Eh]
  int v13; // [sp+34h] [bp-6Ch]
  int v14; // [sp+38h] [bp-68h]
  int v15; // [sp+84h] [bp-1Ch]

  v1 = a1;
  v12 = 1;
  v15 = MEMORY[0];
  v11 = 8;
  v10 = -125;
  v2 = exp_SceSysconForDriver_ksceSysconCmdExec((int)&v9, 0);
  v4 = v2 < 0;
  v5 = v15;
  if ( v2 < 0 )
  {
    v6 = v2;
  }
  else
  {
    v6 = 0;
    v2 = v13;
    v3 = v14;
  }
  if ( !v4 )
    *v1 = v2;
  result = v6;
  v8 = MEMORY[0];
  if ( !v4 )
    v1[1] = v3;
  if ( v5 != v8 )
    imp_SceSysclibForDriver___stack_chk_fail(v6);
  return result;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x4FEC564C]
signed int __fastcall exp_SceSysconForDriver_ksceSysconGetManualChargeMode(_DWORD *a1)
{
  *a1 = 0;
  return sub_81002D20(a1, 0x840u);
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x3C3B949C]
signed int __fastcall exp_SceSysconForDriver_0x3C3B949C(_DWORD *a1)
{
  *a1 = 0;
  return sub_81002D20(a1, 0x841u);
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x1C29C00E]
signed int __fastcall exp_SceSysconForDriver_0x1C29C00E(_DWORD *a1)
{
  *a1 = 0;
  return sub_81002D20(a1, 0x842u);
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x730E4725]
signed int __fastcall exp_SceSysconForDriver_0x730E4725(_DWORD *a1)
{
  *a1 = 0;
  return sub_81002D20(a1, 0x843u);
}

signed int __fastcall sub_810058AC(_DWORD *a1)
{
  *a1 = 0;
  return sub_81002D20(a1, 0x802u);
}

signed int __fastcall sub_810058BC(_DWORD *a1)
{
  *a1 = 0;
  return sub_81002D20(a1, 0x803u);
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0xA039B563]
signed int __fastcall exp_SceSysconForDriver_0xA039B563(_DWORD *a1)
{
  *a1 = 0;
  return sub_81002D20(a1, 0x880u);
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x9BF78047]
signed int __fastcall exp_SceSysconForDriver_0x9BF78047(_DWORD *a1)
{
  *a1 = 0;
  return sub_81002D20(a1, 0x881u);
}

signed int __fastcall sub_810058EC(int *a1, int *a2, int *a3)
{
  int *v3; // r7
  int *v4; // r6
  int *v5; // r5
  signed int result; // r0
  int v7; // [sp+0h] [bp-A0h]
  char v8; // [sp+10h] [bp-90h]
  char v9; // [sp+11h] [bp-8Fh]
  char v10; // [sp+12h] [bp-8Eh]
  unsigned __int8 v11; // [sp+34h] [bp-6Ch]
  unsigned __int8 v12; // [sp+35h] [bp-6Bh]
  unsigned __int8 v13; // [sp+36h] [bp-6Ah]
  unsigned __int8 v14; // [sp+37h] [bp-69h]
  unsigned __int8 v15; // [sp+38h] [bp-68h]
  unsigned __int8 v16; // [sp+39h] [bp-67h]
  int v17; // [sp+84h] [bp-1Ch]

  v3 = a1;
  v4 = a2;
  v5 = a3;
  v17 = MEMORY[0];
  v8 = -126;
  v9 = 8;
  v10 = 1;
  result = exp_SceSysconForDriver_ksceSysconCmdExec((int)&v7, 0);
  if ( result >= 0 )
  {
    if ( v3 )
      *v3 = v11 | (v12 << 8);
    if ( v4 )
      *v4 = v13 | (v14 << 8);
    if ( v5 )
    {
      result = 0;
      *v5 = v15 | (v16 << 8);
    }
    else
    {
      result = 0;
    }
  }
  if ( v17 != MEMORY[0] )
    imp_SceSysclibForDriver___stack_chk_fail(result);
  return result;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x3FDD29D6]
int __fastcall exp_SceSysconForDriver_0x3FDD29D6(unsigned int a1, int a2)
{
  unsigned int v2; // r4
  int result; // r0

  v2 = a1;
  if ( a2 )
  {
    result = sub_810030C8(a1 | 0x80, 0x884u, 2u);
    if ( result >= 0 )
      dword_8100C0B4 |= v2;
  }
  else
  {
    result = sub_810030C8(a1, 0x884u, 2u);
    if ( result >= 0 )
      dword_8100C0B4 &= ~v2;
  }
  return result;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x79E6DD8B]
int __fastcall exp_SceSysconForDriver_0x79E6DD8B(unsigned int a1, int a2)
{
  unsigned int v2; // r4
  int result; // r0

  v2 = a1;
  if ( a2 )
  {
    result = sub_810030C8(a1 | 0x80, 0x885u, 2u);
    if ( result >= 0 )
      dword_8100C0B8 |= v2;
  }
  else
  {
    result = sub_810030C8(a1, 0x885u, 2u);
    if ( result >= 0 )
      dword_8100C0B8 &= ~v2;
  }
  return result;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x62155962]
//int ksceSysconCtrlHdmiCecPower(int power)
//Set the ADV7533 HDMI CEC power.
int __fastcall exp_SceSysconForDriver_ksceSysconCtrlHdmiCecPower(unsigned int a1)
{
  return sub_810030C8(a1, 0x886u, 2u);
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x063425AE]
int __fastcall exp_SceSysconForDriver_0x063425AE(unsigned int a1)
{
  return sub_810030C8(a1, 0x887u, 2u);
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0xBE1ADE4F]
int __fastcall exp_SceSysconForDriver_ksceSysconCtrlSdPower(unsigned int a1)
{
  unsigned int v1; // r4
  int result; // r0

  v1 = a1;
  exp_SceSysconForDriver_0x0D0B6D25();
  if ( exp_SceSysconForDriver_ksceSysconIsDownLoaderMode() )
    result = 0;
  else
    result = sub_810030C8(v1, 0x888u, 2u);
  return result;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x8D1D97E8]
int __fastcall exp_SceSysconForDriver_ksceSysconCtrlAccPower(unsigned int a1)
{
  return sub_810030C8(a1, 0x889u, 2u);
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0xA2E85DB9]
int __fastcall exp_SceSysconForDriver_0xA2E85DB9(unsigned int a1)
{
  return sub_810030C8(a1, 0x88Au, 2u);
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x5A614349]
int __fastcall exp_SceSysconForDriver_0x5A614349(unsigned int a1)
{
  return sub_810030C8(a1, 0x88Bu, 2u);
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0xB872E904]
int __fastcall exp_SceSysconForDriver_0xB872E904(unsigned int a1)
{
  return sub_810030C8(a1, 0x88Cu, 2u);
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0xDD16ABD9]
int __fastcall exp_SceSysconForDriver_0xDD16ABD9(unsigned int a1)
{
  return sub_810030C8(a1, 0x88Du, 2u);
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x7F198FA2]
int __fastcall exp_SceSysconForDriver_0x7F198FA2(char a1, unsigned int a2)
{
  unsigned int v2; // r5
  char v3; // r6
  unsigned int v4; // r0
  signed int v5; // r0
  int result; // r0
  unsigned int v7; // r0
  int v8; // [sp+0h] [bp-A0h]
  char v9; // [sp+10h] [bp-90h]
  char v10; // [sp+11h] [bp-8Fh]
  char v11; // [sp+12h] [bp-8Eh]
  char v12; // [sp+13h] [bp-8Dh]
  char v13; // [sp+14h] [bp-8Ch]
  int v14; // [sp+84h] [bp-1Ch]

  v2 = a2;
  v3 = a1;
  v14 = MEMORY[0];
  if ( a2 <= 0x3F )
  {
    v4 = exp_SceSysconForDriver_ksceSysconGetHardwareInfo() & 0xFF0000;
    if ( v4 >= 0x600000 && v4 != 7340032 && (v4 < 0x800000 && v4 != 7471104 || v2 <= 0x3B) )
      goto LABEL_13;
LABEL_3:
    v12 = v3;
    v13 = v2;
    v9 = -114;
    v10 = 8;
    v11 = 3;
    v5 = exp_SceSysconForDriver_ksceSysconCmdExec((int)&v8, 0);
    result = v5 & (v5 >> 31);
    goto LABEL_4;
  }
  v7 = exp_SceSysconForDriver_ksceSysconGetHardwareInfo() & 0xFF0000;
  if ( v7 >= 0x600000 && v7 != 7340032 )
    goto LABEL_3;
  if ( v2 - 77 <= 0x3F )
  {
    LOBYTE(v2) = -116 - v2;
    goto LABEL_3;
  }
LABEL_13:
  result = -2145058815;
LABEL_4:
  if ( v14 != MEMORY[0] )
    imp_SceSysclibForDriver___stack_chk_fail(result);
  return result;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x40FF3898]
int __fastcall exp_SceSysconForDriver_ksceSysconCtrlDeviceReset(int a1, int a2)
{
  return sub_810030C8(a2 | (a1 << 8), 0x88Fu, 3u);
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x285594F8]
int __fastcall exp_SceSysconForDriver_0x285594F8(unsigned int a1)
{
  return sub_810030C8(a1, 0x890u, 2u);
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x04EC7579]
int __fastcall exp_SceSysconForDriver_ksceSysconCtrlLED(int a1, int a2)
{
  signed int v2; // r1

  if ( a2 )
    v2 = 128;
  else
    v2 = 0;
  return sub_810030C8(a1 | v2, 0x891u, 2u);
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x596B17B7]
int __fastcall exp_SceSysconForDriver_0x596B17B7(unsigned int a1)
{
  return sub_810030C8(a1, 0x892u, 2u);
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x2A4B0437]
int __fastcall exp_SceSysconForDriver_0x2A4B0437(int a1, int a2)
{
  return sub_810030C8(a2 | (a1 << 8), 0x893u, 3u);
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x5BF765BB]
signed int __fastcall exp_SceSysconForDriver_0x5BF765BB(unsigned int a1, _WORD *a2)
{
  _WORD *v2; // r5
  signed int result; // r0
  __int16 v4; // r6
  __int16 v5; // r7
  int v6; // [sp+0h] [bp-A0h]
  char v7; // [sp+10h] [bp-90h]
  char v8; // [sp+11h] [bp-8Fh]
  char v9; // [sp+12h] [bp-8Eh]
  char v10; // [sp+13h] [bp-8Dh]
  unsigned __int8 v11; // [sp+34h] [bp-6Ch]
  unsigned __int8 v12; // [sp+35h] [bp-6Bh]
  int v13; // [sp+84h] [bp-1Ch]

  v2 = a2;
  v13 = MEMORY[0];
  if ( a1 > 0xFF )
  {
    result = -2145058815;
  }
  else
  {
    v10 = a1;
    v7 = -107;
    v8 = 8;
    v9 = 2;
    result = exp_SceSysconForDriver_ksceSysconCmdExec((int)&v6, 0);
    if ( result >= 0 )
    {
      v4 = v12;
      v5 = v11;
      *v2 = v11 | (unsigned __int16)(v12 << 8);
      if ( (unsigned int)exp_SceSysconForDriver_ksceSysconGetBaryonVersion() <= 0x70503 )
      {
        result = 0;
        *v2 = v4 | (v5 << 8);
      }
      else
      {
        result = 0;
      }
    }
  }
  if ( v13 != MEMORY[0] )
    imp_SceSysclibForDriver___stack_chk_fail(result);
  return result;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0xCCC71C28]
int __fastcall exp_SceSysconForDriver_0xCCC71C28(unsigned int a1, unsigned int a2)
{
  char v2; // r5
  unsigned int v3; // r6
  signed int v4; // r0
  int result; // r0
  int v6; // [sp+0h] [bp-98h]
  char v7; // [sp+10h] [bp-88h]
  char v8; // [sp+11h] [bp-87h]
  char v9; // [sp+12h] [bp-86h]
  char v10; // [sp+13h] [bp-85h]
  __int16 v11; // [sp+14h] [bp-84h]
  int v12; // [sp+84h] [bp-14h]

  v12 = MEMORY[0];
  if ( a1 > 0xFF )
  {
    result = -2145058815;
  }
  else
  {
    v2 = a2;
    v3 = a2 >> 8;
    v10 = a1;
    v7 = -106;
    v8 = 8;
    v9 = 4;
    v11 = a2;
    if ( (unsigned int)exp_SceSysconForDriver_ksceSysconGetBaryonVersion() <= 0x70503 )
    {
      LOBYTE(v11) = v3;
      HIBYTE(v11) = v2;
    }
    v4 = exp_SceSysconForDriver_ksceSysconCmdExec((int)&v6, 0);
    result = v4 & (v4 >> 31);
  }
  if ( v12 != MEMORY[0] )
    imp_SceSysclibForDriver___stack_chk_fail(result);
  return result;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x95975DD1]
signed int __fastcall exp_SceSysconForDriver_0x95975DD1(unsigned int a1, _WORD *a2)
{
  _WORD *v2; // r5
  signed int result; // r0
  __int16 v4; // r6
  __int16 v5; // r7
  int v6; // [sp+0h] [bp-A0h]
  char v7; // [sp+10h] [bp-90h]
  char v8; // [sp+11h] [bp-8Fh]
  char v9; // [sp+12h] [bp-8Eh]
  char v10; // [sp+13h] [bp-8Dh]
  unsigned __int8 v11; // [sp+34h] [bp-6Ch]
  unsigned __int8 v12; // [sp+35h] [bp-6Bh]
  int v13; // [sp+84h] [bp-1Ch]

  v2 = a2;
  v13 = MEMORY[0];
  if ( a1 > 0xFF )
  {
    result = -2145058815;
  }
  else
  {
    v10 = a1;
    v7 = -105;
    v8 = 8;
    v9 = 2;
    result = exp_SceSysconForDriver_ksceSysconCmdExec((int)&v6, 0);
    if ( result >= 0 )
    {
      v4 = v12;
      v5 = v11;
      *v2 = v11 | (unsigned __int16)(v12 << 8);
      if ( (unsigned int)exp_SceSysconForDriver_ksceSysconGetBaryonVersion() <= 0x70503 )
      {
        result = 0;
        *v2 = v4 | (v5 << 8);
      }
      else
      {
        result = 0;
      }
    }
  }
  if ( v13 != MEMORY[0] )
    imp_SceSysclibForDriver___stack_chk_fail(result);
  return result;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0xF39300D3]
int __fastcall exp_SceSysconForDriver_0xF39300D3(unsigned int a1, unsigned int a2)
{
  char v2; // r5
  unsigned int v3; // r6
  signed int v4; // r0
  int result; // r0
  int v6; // [sp+0h] [bp-98h]
  char v7; // [sp+10h] [bp-88h]
  char v8; // [sp+11h] [bp-87h]
  char v9; // [sp+12h] [bp-86h]
  char v10; // [sp+13h] [bp-85h]
  __int16 v11; // [sp+14h] [bp-84h]
  int v12; // [sp+84h] [bp-14h]

  v12 = MEMORY[0];
  if ( a1 > 0xFF )
  {
    result = -2145058815;
  }
  else
  {
    v2 = a2;
    v3 = a2 >> 8;
    v10 = a1;
    v7 = -104;
    v8 = 8;
    v9 = 4;
    v11 = a2;
    if ( (unsigned int)exp_SceSysconForDriver_ksceSysconGetBaryonVersion() <= 0x70503 )
    {
      LOBYTE(v11) = v3;
      HIBYTE(v11) = v2;
    }
    v4 = exp_SceSysconForDriver_ksceSysconCmdExec((int)&v6, 0);
    result = v4 & (v4 >> 31);
  }
  if ( v12 != MEMORY[0] )
    imp_SceSysclibForDriver___stack_chk_fail(result);
  return result;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x5CDDA14D]
int __fastcall exp_SceSysconForDriver_0x5CDDA14D(unsigned int a1)
{
  unsigned int v1; // r4
  int result; // r0

  v1 = a1;
  if ( (unsigned int)exp_SceSysconForDriver_ksceSysconGetBaryonVersion() > 0x80000 )
    result = sub_810030C8(v1, 0x899u, 2u);
  else
    result = 0;
  return result;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x59DC5938]
int __fastcall exp_SceSysconForDriver_0x59DC5938(unsigned int a1)
{
  return sub_810030C8(a1, 0x89Au, 3u);
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x710A7CF0]
int __fastcall exp_SceSysconForDriver_ksceSysconCtrlRMRPower(unsigned int a1)
{
  unsigned int v1; // r4
  int result; // r0

  v1 = a1;
  exp_SceSysconForDriver_0x0D0B6D25();
  if ( exp_SceSysconForDriver_ksceSysconIsDownLoaderMode() )
    result = 0;
  else
    result = sub_810030C8(v1, 0x89Bu, 2u);
  return result;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0xB1F88B11]
int __fastcall exp_SceSysconForDriver_0xB1F88B11(unsigned int a1)
{
  return sub_810030C8(a1, 0x89Cu, 2u);
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x6F586D1A]
int __fastcall exp_SceSysconForDriver_0x6F586D1A(int a1, int a2)
{
  int result; // r0

  if ( a1 == 64 )
    result = sub_810030C8((a2 << 8) | 1, 0x89Du, 3u);
  else
    result = -2145058815;
  return result;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x9CA6EB70]
int __fastcall exp_SceSysconForDriver_0x9CA6EB70(unsigned int a1)
{
  unsigned int v1; // r4
  int result; // r0

  v1 = a1;
  if ( (exp_SceSysconForDriver_ksceSysconGetHardwareInfo() & 0xFF0000u) >= 0x400000 )
    result = sub_810030C8(v1, 0x89Eu, 2u);
  else
    result = 0;
  return result;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0xCB41B531]
int __fastcall exp_SceSysconForDriver_0xCB41B531(char a1, char a2, __int16 a3, __int16 a4)
{
  signed int v4; // r0
  int result; // r0
  int v6; // [sp+0h] [bp-A0h]
  char v7; // [sp+10h] [bp-90h]
  char v8; // [sp+11h] [bp-8Fh]
  char v9; // [sp+12h] [bp-8Eh]
  char v10; // [sp+13h] [bp-8Dh]
  char v11; // [sp+14h] [bp-8Ch]
  __int16 v12; // [sp+15h] [bp-8Bh]
  __int16 v13; // [sp+17h] [bp-89h]
  int v14; // [sp+84h] [bp-1Ch]

  v10 = a1;
  v11 = a2;
  v12 = a3;
  v13 = a4;
  v14 = MEMORY[0];
  v7 = -97;
  v8 = 8;
  v9 = 7;
  v4 = exp_SceSysconForDriver_ksceSysconCmdExec((int)&v6, 0);
  result = v4 & (v4 >> 31);
  if ( v14 != MEMORY[0] )
    imp_SceSysclibForDriver___stack_chk_fail(result);
  return result;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x4946538A]
int __fastcall exp_SceSysconForDriver_ksceSysconEnableHibernateIO(unsigned int a1)
{
  return sub_810030C8(a1, 0x8B0u, 1u);
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0xC6A2C9EF]
int __fastcall exp_SceSysconForDriver_ksceSysconCtrlManualChargeMode(unsigned int a1)
{
  return sub_810030C8(a1, 0x8C0u, 2u);
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x3274A925]
int __fastcall exp_SceSysconForDriver_0x3274A925(unsigned int a1)
{
  return sub_810030C8(a1, 0x8C5u, 2u);
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0xDFB024C4]
signed int __fastcall exp_SceSysconForDriver_0xDFB024C4(_DWORD *a1)
{
  *a1 = 0;
  return sub_81002D20(a1, 0x8C6u);
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x87FF8041]
int __fastcall exp_SceSysconForDriver_0x87FF8041(unsigned int a1)
{
  return sub_810030C8(a1, 0x8C7u, 2u);
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x7BFA95DA]
int __fastcall exp_SceSysconForDriver_0x7BFA95DA(unsigned int a1)
{
  return sub_810030C8(a1, 0x8C8u, 2u);
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x451C1662]
int __fastcall exp_SceSysconForDriver_0x451C1662(unsigned int a1)
{
  return sub_810030C8(a1, 0x8C9u, 2u);
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x79074DE4]
signed int __fastcall exp_SceSysconForDriver_0x79074DE4(_DWORD *a1)
{
  _DWORD *v1; // r6
  signed int result; // r0
  int v3; // r2
  int v4; // [sp+0h] [bp-18h]
  int v5; // [sp+4h] [bp-14h]

  v1 = a1;
  v4 = 0;
  v5 = MEMORY[0];
  result = sub_81002D20(&v4, 0x8CAu);
  v3 = v5;
  if ( result >= 0 )
  {
    result = 0;
    *v1 = (char)v4;
  }
  if ( v3 != MEMORY[0] )
    imp_SceSysclibForDriver___stack_chk_fail(result);
  return result;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x7D25F6D2]
signed int __fastcall exp_SceSysconForDriver_0x7D25F6D2(unsigned int a1, _BYTE *a2)
{
  _BYTE *v2; // r7
  signed int result; // r0
  int v4; // [sp+0h] [bp-A0h]
  char v5; // [sp+10h] [bp-90h]
  char v6; // [sp+11h] [bp-8Fh]
  char v7; // [sp+12h] [bp-8Eh]
  char v8; // [sp+13h] [bp-8Dh]
  char v9; // [sp+34h] [bp-6Ch]
  int v10; // [sp+84h] [bp-1Ch]

  v2 = a2;
  v10 = MEMORY[0];
  if ( a1 > 0xFF )
  {
    result = -2145058815;
  }
  else
  {
    v8 = a1;
    v5 = -53;
    v6 = 8;
    v7 = 2;
    result = exp_SceSysconForDriver_ksceSysconCmdExec((int)&v4, 0);
    if ( result >= 0 )
    {
      result = 0;
      *v2 = v9;
    }
  }
  if ( v10 != MEMORY[0] )
    imp_SceSysclibForDriver___stack_chk_fail(result);
  return result;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0xD2ADABCA]
int __fastcall exp_SceSysconForDriver_0xD2ADABCA(unsigned int a1, char a2)
{
  signed int v2; // r0
  int result; // r0
  int v4; // [sp+0h] [bp-A0h]
  char v5; // [sp+10h] [bp-90h]
  char v6; // [sp+11h] [bp-8Fh]
  char v7; // [sp+12h] [bp-8Eh]
  char v8; // [sp+13h] [bp-8Dh]
  char v9; // [sp+14h] [bp-8Ch]
  int v10; // [sp+84h] [bp-1Ch]

  v10 = MEMORY[0];
  if ( a1 > 0xFF )
  {
    result = -2145058815;
  }
  else
  {
    v8 = a1;
    v9 = a2;
    v5 = -52;
    v6 = 8;
    v7 = 3;
    v2 = exp_SceSysconForDriver_ksceSysconCmdExec((int)&v4, 0);
    result = v2 & (v2 >> 31);
  }
  if ( v10 != MEMORY[0] )
    imp_SceSysclibForDriver___stack_chk_fail(result);
  return result;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x3B354824]
signed int __fastcall exp_SceSysconForDriver_ksceSysconGetTemperatureLog(signed int *a1)
{
  signed int *v1; // r6
  signed int v2; // r0
  int v3; // r1
  int v4; // r2
  int v5; // r3
  bool v6; // nf
  signed int v7; // r7
  signed int result; // r0
  int v9; // r2
  int v10; // r3
  int v11; // [sp+0h] [bp-A0h]
  char v12; // [sp+10h] [bp-90h]
  char v13; // [sp+11h] [bp-8Fh]
  char v14; // [sp+12h] [bp-8Eh]
  int v15; // [sp+34h] [bp-6Ch]
  int v16; // [sp+38h] [bp-68h]
  int v17; // [sp+3Ch] [bp-64h]
  int v18; // [sp+40h] [bp-60h]
  int v19; // [sp+84h] [bp-1Ch]

  v1 = a1;
  v12 = -50;
  v19 = MEMORY[0];
  v13 = 8;
  v14 = 1;
  v2 = exp_SceSysconForDriver_ksceSysconCmdExec((int)&v11, 0);
  v6 = v2 < 0;
  if ( v2 < 0 )
  {
    v7 = v2;
  }
  else
  {
    v7 = 0;
    v2 = v15;
    v3 = v16;
    v4 = v17;
    v5 = v18;
  }
  if ( !v6 )
    *v1 = v2;
  result = v7;
  if ( !v6 )
  {
    v1[2] = v4;
    v1[3] = v5;
  }
  v9 = v19;
  v10 = MEMORY[0];
  if ( !v6 )
    v1[1] = v3;
  if ( v9 != v10 )
    imp_SceSysclibForDriver___stack_chk_fail(v7);
  return result;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x3843D657]
int __fastcall exp_SceSysconForDriver_ksceSysconClearTemperatureLog(unsigned int a1)
{
  return sub_810030C8(a1, 0x8CFu, 2u);
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0xF87679EE]
int __fastcall exp_SceSysconForDriver_0xF87679EE(unsigned int a1)
{
  return sub_810030C8(a1, 0x8D0u, 2u);
}

signed int __fastcall sub_81006074(_DWORD *a1)
{
  *a1 = 0;
  return sub_81002D20(a1, 0x8A0u);
}

signed int __fastcall sub_81006084(_DWORD *a1)
{
  *a1 = 0;
  return sub_81002D20(a1, 0x8A1u);
}

signed int __fastcall sub_81006094(_DWORD *a1)
{
  _DWORD *v1; // r5
  signed int result; // r0
  int v3; // r2
  __int16 v4; // [sp+0h] [bp-18h]
  int v5; // [sp+4h] [bp-14h]

  v1 = a1;
  v5 = MEMORY[0];
  result = sub_81002D20(&v4, 0x8A2u);
  v3 = v5;
  if ( result >= 0 )
  {
    result = 0;
    *v1 = v4;
  }
  if ( v3 != MEMORY[0] )
    imp_SceSysclibForDriver___stack_chk_fail(result);
  return result;
}

signed int __fastcall sub_810060CC(_DWORD *a1)
{
  *a1 = 0;
  return sub_81002D20(a1, 0x8A3u);
}

signed int __fastcall sub_810060DC(_DWORD *a1)
{
  *a1 = 0;
  return sub_81002D20(a1, 0x8A4u);
}

signed int __fastcall sub_810060EC(unsigned int a1, _WORD *a2)
{
  _WORD *v2; // r6
  signed int result; // r0
  int v4; // [sp+0h] [bp-A0h]
  char v5; // [sp+10h] [bp-90h]
  char v6; // [sp+11h] [bp-8Fh]
  char v7; // [sp+12h] [bp-8Eh]
  __int16 v8; // [sp+13h] [bp-8Dh]
  unsigned __int8 v9; // [sp+34h] [bp-6Ch]
  unsigned __int8 v10; // [sp+35h] [bp-6Bh]
  int v11; // [sp+84h] [bp-1Ch]

  v2 = a2;
  v11 = MEMORY[0];
  if ( a1 >= 0x10000 )
  {
    result = -2145058815;
  }
  else
  {
    v8 = a1;
    v5 = -88;
    v6 = 8;
    v7 = 3;
    result = exp_SceSysconForDriver_ksceSysconCmdExec((int)&v4, 0);
    if ( result >= 0 )
    {
      result = 0;
      *v2 = v9 | (unsigned __int16)(v10 << 8);
    }
  }
  if ( v11 != MEMORY[0] )
    imp_SceSysclibForDriver___stack_chk_fail(result);
  return result;
}

int __fastcall sub_81006154(unsigned int a1, __int16 a2)
{
  signed int v2; // r0
  int result; // r0
  int v4; // [sp+0h] [bp-A0h]
  char v5; // [sp+10h] [bp-90h]
  char v6; // [sp+11h] [bp-8Fh]
  char v7; // [sp+12h] [bp-8Eh]
  __int16 v8; // [sp+13h] [bp-8Dh]
  char v9; // [sp+15h] [bp-8Bh]
  char v10; // [sp+16h] [bp-8Ah]
  int v11; // [sp+84h] [bp-1Ch]

  v11 = MEMORY[0];
  if ( a1 >= 0x10000 )
  {
    result = -2145058815;
  }
  else
  {
    v8 = a1;
    v9 = HIBYTE(a2);
    v10 = a2;
    v5 = -87;
    v6 = 8;
    v7 = 5;
    v2 = exp_SceSysconForDriver_ksceSysconCmdExec((int)&v4, 0);
    result = v2 & (v2 >> 31);
  }
  if ( v11 != MEMORY[0] )
    imp_SceSysclibForDriver___stack_chk_fail(result);
  return result;
}

signed int __fastcall sub_810061B8(unsigned int a1, _WORD *a2)
{
  _WORD *v2; // r6
  signed int result; // r0
  int v4; // [sp+0h] [bp-A0h]
  char v5; // [sp+10h] [bp-90h]
  char v6; // [sp+11h] [bp-8Fh]
  char v7; // [sp+12h] [bp-8Eh]
  __int16 v8; // [sp+13h] [bp-8Dh]
  unsigned __int8 v9; // [sp+34h] [bp-6Ch]
  unsigned __int8 v10; // [sp+35h] [bp-6Bh]
  int v11; // [sp+84h] [bp-1Ch]

  v2 = a2;
  v11 = MEMORY[0];
  if ( a1 >= 0x10000 )
  {
    result = -2145058815;
  }
  else
  {
    v8 = a1;
    v5 = -86;
    v6 = 8;
    v7 = 3;
    result = exp_SceSysconForDriver_ksceSysconCmdExec((int)&v4, 0);
    if ( result >= 0 )
    {
      result = 0;
      *v2 = v9 | (unsigned __int16)(v10 << 8);
    }
  }
  if ( v11 != MEMORY[0] )
    imp_SceSysclibForDriver___stack_chk_fail(result);
  return result;
}

int sub_81006220()
{
  signed int v0; // r0
  int result; // r0
  int v2; // [sp+0h] [bp-A0h]
  char v3; // [sp+10h] [bp-90h]
  char v4; // [sp+11h] [bp-8Fh]
  char v5; // [sp+12h] [bp-8Eh]
  char v6; // [sp+13h] [bp-8Dh]
  int v7; // [sp+84h] [bp-1Ch]

  v6 = 0;
  v5 = 1;
  v7 = MEMORY[0];
  v3 = -80;
  v4 = 8;
  v0 = exp_SceSysconForDriver_ksceSysconCmdExec((int)&v2, 0);
  result = v0 & (v0 >> 31);
  if ( v7 != MEMORY[0] )
    imp_SceSysclibForDriver___stack_chk_fail(result);
  return result;
}

int sub_81006264()
{
  signed int v0; // r0
  int result; // r0
  int v2; // [sp+0h] [bp-A0h]
  char v3; // [sp+10h] [bp-90h]
  char v4; // [sp+11h] [bp-8Fh]
  char v5; // [sp+12h] [bp-8Eh]
  char v6; // [sp+13h] [bp-8Dh]
  int v7; // [sp+84h] [bp-1Ch]

  v6 = 0;
  v5 = 1;
  v7 = MEMORY[0];
  v3 = -79;
  v4 = 8;
  v0 = exp_SceSysconForDriver_ksceSysconCmdExec((int)&v2, 0);
  result = v0 & (v0 >> 31);
  if ( v7 != MEMORY[0] )
    imp_SceSysclibForDriver___stack_chk_fail(result);
  return result;
}

int __fastcall sub_810062A8(__int16 a1, char a2, int a3, int a4)
{
  signed int v4; // r0
  int result; // r0
  int v6; // [sp+0h] [bp-A0h]
  char v7; // [sp+10h] [bp-90h]
  char v8; // [sp+11h] [bp-8Fh]
  char v9; // [sp+12h] [bp-8Eh]
  __int16 v10; // [sp+13h] [bp-8Dh]
  char v11; // [sp+15h] [bp-8Bh]
  char v12; // [sp+16h] [bp-8Ah]
  char v13; // [sp+17h] [bp-89h]
  int v14; // [sp+84h] [bp-1Ch]

  v14 = MEMORY[0];
  if ( (unsigned int)(a4 - 1) > 0xF )
  {
    result = -2145058815;
  }
  else
  {
    v10 = a1;
    v9 = a4 + 5;
    v8 = 8;
    v12 = a2;
    v11 = a4;
    v7 = -78;
    imp_SceSysclibForDriver_memcpy(&v13, a3, a4);
    v4 = exp_SceSysconForDriver_ksceSysconCmdExec((int)&v6, 0);
    result = v4 & (v4 >> 31);
  }
  if ( v14 != MEMORY[0] )
    imp_SceSysclibForDriver___stack_chk_fail(result);
  return result;
}

int __fastcall sub_8100631C(__int16 a1)
{
  signed int v1; // r0
  int result; // r0
  int v3; // [sp+0h] [bp-A0h]
  char v4; // [sp+10h] [bp-90h]
  char v5; // [sp+11h] [bp-8Fh]
  char v6; // [sp+12h] [bp-8Eh]
  __int16 v7; // [sp+13h] [bp-8Dh]
  char v8; // [sp+15h] [bp-8Bh]
  int v9; // [sp+84h] [bp-1Ch]

  v7 = a1;
  v8 = 0;
  v9 = MEMORY[0];
  v4 = -77;
  v5 = 8;
  v6 = 3;
  v1 = exp_SceSysconForDriver_ksceSysconCmdExec((int)&v3, 0);
  result = v1 & (v1 >> 31);
  if ( v9 != MEMORY[0] )
    imp_SceSysclibForDriver___stack_chk_fail(result);
  return result;
}

signed int __fastcall sub_81006374(__int16 a1, char a2, char a3, int a4, int a5)
{
  int v5; // r8
  signed int result; // r0
  int v7; // [sp+0h] [bp-A0h]
  char v8; // [sp+10h] [bp-90h]
  char v9; // [sp+11h] [bp-8Fh]
  char v10; // [sp+12h] [bp-8Eh]
  __int16 v11; // [sp+13h] [bp-8Dh]
  char v12; // [sp+15h] [bp-8Bh]
  char v13; // [sp+16h] [bp-8Ah]
  char v14; // [sp+17h] [bp-89h]
  char v15; // [sp+36h] [bp-6Ah]
  int v16; // [sp+84h] [bp-1Ch]

  v5 = a4;
  v16 = MEMORY[0];
  if ( (unsigned int)(a5 - 1) > 0xF )
  {
    result = -2145058815;
  }
  else
  {
    v11 = a1;
    v12 = a5;
    v13 = a2;
    v14 = a3;
    v8 = -76;
    v9 = 8;
    v10 = 6;
    result = exp_SceSysconForDriver_ksceSysconCmdExec((int)&v7, 0);
    if ( result >= 0 )
    {
      imp_SceSysclibForDriver_memcpy(v5, &v15, a5);
      result = 0;
    }
  }
  if ( v16 != MEMORY[0] )
    imp_SceSysclibForDriver___stack_chk_fail(result);
  return result;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0xC562AF3A]
signed int __fastcall exp_SceSysconForDriver_0xC562AF3A(_DWORD *a1)
{
  _DWORD *v1; // r4

  v1 = a1;
  if ( (unsigned int)exp_SceSysconForDriver_ksceSysconGetBaryonVersion() <= 0x70503 )
    return sub_810058AC(v1);
  *v1 = 0;
  return sub_81002D20(v1, 0x900u);
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x03F11220]
signed int __fastcall exp_SceSysconForDriver_0x03F11220(_DWORD *a1)
{
  _DWORD *v1; // r4

  v1 = a1;
  if ( (unsigned int)exp_SceSysconForDriver_ksceSysconGetBaryonVersion() <= 0x70503 )
    return sub_810058BC(v1);
  *v1 = 0;
  return sub_81002D20(v1, 0x901u);
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0xAD0A8275]
signed int __fastcall exp_SceSysconForDriver_0xAD0A8275(_DWORD *a1)
{
  *a1 = 0;
  return sub_81002D20(a1, 0x902u);
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x26F9D729]
signed int __fastcall exp_SceSysconForDriver_0x26F9D729(_DWORD *a1, _DWORD *a2)
{
  _DWORD *v2; // r6
  _DWORD *v3; // r5
  signed int result; // r0
  int v5; // [sp+0h] [bp-20h]
  int v6; // [sp+4h] [bp-1Ch]

  v2 = a1;
  v3 = a2;
  v5 = 0;
  v6 = MEMORY[0];
  result = sub_81002D20(&v5, 0x903u);
  if ( result >= 0 )
  {
    if ( v2 )
      *v2 = (unsigned __int8)v5;
    if ( v3 )
      *v3 = BYTE1(v5);
  }
  if ( v6 != MEMORY[0] )
    imp_SceSysclibForDriver___stack_chk_fail(result);
  return result;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x9ADC9936]
signed int __fastcall exp_SceSysconForDriver_ksceSysconGetBatteryCalibData(int *a1, int *a2, int *a3, int *a4)
{
  int *v4; // r7
  int *v5; // r6
  int *v6; // r8
  int *v7; // r5
  signed int result; // r0
  int v9; // [sp+0h] [bp-A8h]
  char v10; // [sp+10h] [bp-98h]
  char v11; // [sp+11h] [bp-97h]
  char v12; // [sp+12h] [bp-96h]
  unsigned __int8 v13; // [sp+34h] [bp-74h]
  unsigned __int8 v14; // [sp+35h] [bp-73h]
  unsigned __int8 v15; // [sp+36h] [bp-72h]
  unsigned __int8 v16; // [sp+37h] [bp-71h]
  unsigned __int8 v17; // [sp+38h] [bp-70h]
  unsigned __int8 v18; // [sp+39h] [bp-6Fh]
  unsigned __int8 v19; // [sp+3Ah] [bp-6Eh]
  unsigned __int8 v20; // [sp+3Bh] [bp-6Dh]
  int v21; // [sp+84h] [bp-24h]

  v4 = a1;
  v5 = a2;
  v6 = a3;
  v7 = a4;
  v21 = MEMORY[0];
  v10 = 4;
  v11 = 9;
  v12 = 1;
  result = exp_SceSysconForDriver_ksceSysconCmdExec((int)&v9, 0);
  if ( result >= 0 )
  {
    if ( v4 )
      *v4 = v13 | (v14 << 8);
    if ( v5 )
      *v5 = v15 | (v16 << 8);
    if ( v6 )
      *v6 = v17 | (v18 << 8);
    if ( v7 )
    {
      result = 0;
      *v7 = v19 | (v20 << 8);
    }
    else
    {
      result = 0;
    }
  }
  if ( v21 != MEMORY[0] )
    imp_SceSysclibForDriver___stack_chk_fail(result);
  return result;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x68E0031E]
signed int __fastcall exp_SceSysconForDriver_ksceSysconGetBatteryVersion(int *a1, int *a2, int *a3)
{
  int *v3; // r7
  int *v4; // r6
  int *v5; // r5
  signed int result; // r0
  int v7; // [sp+0h] [bp-A0h]
  char v8; // [sp+10h] [bp-90h]
  char v9; // [sp+11h] [bp-8Fh]
  char v10; // [sp+12h] [bp-8Eh]
  unsigned __int8 v11; // [sp+34h] [bp-6Ch]
  unsigned __int8 v12; // [sp+35h] [bp-6Bh]
  unsigned __int8 v13; // [sp+36h] [bp-6Ah]
  unsigned __int8 v14; // [sp+37h] [bp-69h]
  unsigned __int8 v15; // [sp+38h] [bp-68h]
  unsigned __int8 v16; // [sp+39h] [bp-67h]
  int v17; // [sp+84h] [bp-1Ch]

  v3 = a1;
  v4 = a2;
  v5 = a3;
  v17 = MEMORY[0];
  if ( (unsigned int)exp_SceSysconForDriver_ksceSysconGetBaryonVersion() <= 0x70503 )
  {
    result = sub_810058EC(v3, v4, v5);
  }
  else
  {
    v8 = -128;
    v9 = 9;
    v10 = 1;
    result = exp_SceSysconForDriver_ksceSysconCmdExec((int)&v7, 0);
    if ( result >= 0 )
    {
      if ( v3 )
        *v3 = v11 | (v12 << 8);
      if ( v4 )
        *v4 = v13 | (v14 << 8);
      if ( v5 )
      {
        result = 0;
        *v5 = v15 | (v16 << 8);
      }
      else
      {
        result = 0;
      }
    }
  }
  if ( v17 != MEMORY[0] )
    imp_SceSysclibForDriver___stack_chk_fail(result);
  return result;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x9070F139]
signed int __fastcall exp_SceSysconForDriver_0x9070F139(_DWORD *a1)
{
  _DWORD *v1; // r4

  v1 = a1;
  if ( (unsigned int)exp_SceSysconForDriver_ksceSysconGetBaryonVersion() <= 0x70503 )
    return sub_81006074(v1);
  *v1 = 0;
  return sub_81002D20(v1, 0x981u);
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x00A65FC1]
signed int __fastcall exp_SceSysconForDriver_0x00A65FC1(_DWORD *a1)
{
  _DWORD *v1; // r4

  v1 = a1;
  if ( (unsigned int)exp_SceSysconForDriver_ksceSysconGetBaryonVersion() <= 0x70503 )
    return sub_81006084(v1);
  *v1 = 0;
  return sub_81002D20(v1, 0x982u);
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x0826BA07]
signed int __fastcall exp_SceSysconForDriver_0x0826BA07(_DWORD *a1)
{
  _DWORD *v1; // r5
  signed int result; // r0
  __int16 v3; // [sp+0h] [bp-18h]
  int v4; // [sp+4h] [bp-14h]

  v1 = a1;
  v4 = MEMORY[0];
  if ( (unsigned int)exp_SceSysconForDriver_ksceSysconGetBaryonVersion() <= 0x70503 )
  {
    result = sub_81006094(v1);
  }
  else
  {
    result = sub_81002D20(&v3, 0x983u);
    if ( result >= 0 )
    {
      result = 0;
      *v1 = v3;
    }
  }
  if ( v4 != MEMORY[0] )
    imp_SceSysclibForDriver___stack_chk_fail(result);
  return result;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0xB841C141]
signed int __fastcall exp_SceSysconForDriver_0xB841C141(_DWORD *a1)
{
  _DWORD *v1; // r4

  v1 = a1;
  if ( (unsigned int)exp_SceSysconForDriver_ksceSysconGetBaryonVersion() <= 0x70503 )
    return sub_810060CC(v1);
  *v1 = 0;
  return sub_81002D20(v1, 0x984u);
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x91D3B7A3]
signed int __fastcall exp_SceSysconForDriver_0x91D3B7A3(_DWORD *a1, _DWORD *a2)
{
  _DWORD *v2; // r6
  _DWORD *v3; // r5
  signed int result; // r0
  unsigned __int8 v5; // [sp+0h] [bp-18h]
  unsigned __int8 v6; // [sp+1h] [bp-17h]
  int v7; // [sp+4h] [bp-14h]

  v2 = a1;
  v3 = a2;
  v7 = MEMORY[0];
  if ( (unsigned int)exp_SceSysconForDriver_ksceSysconGetBaryonVersion() > 0x70503 )
  {
    result = sub_81002D20(&v5, 0x985u);
    if ( result >= 0 )
    {
      if ( v2 )
        *v2 = v5;
      if ( v3 )
        *v3 = v6;
    }
  }
  else
  {
    if ( v3 )
      *v3 = 0;
    result = sub_810060DC(v2);
  }
  if ( v7 != MEMORY[0] )
    imp_SceSysclibForDriver___stack_chk_fail(result);
  return result;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0xC2FB5565]
signed int __fastcall exp_SceSysconForDriver_0xC2FB5565(unsigned int a1, _WORD *a2)
{
  unsigned int v2; // r5
  _WORD *v3; // r6
  signed int result; // r0
  int v5; // [sp+0h] [bp-A0h]
  char v6; // [sp+10h] [bp-90h]
  char v7; // [sp+11h] [bp-8Fh]
  char v8; // [sp+12h] [bp-8Eh]
  __int16 v9; // [sp+13h] [bp-8Dh]
  unsigned __int8 v10; // [sp+34h] [bp-6Ch]
  unsigned __int8 v11; // [sp+35h] [bp-6Bh]
  int v12; // [sp+84h] [bp-1Ch]

  v2 = a1;
  v3 = a2;
  v12 = MEMORY[0];
  if ( (unsigned int)exp_SceSysconForDriver_ksceSysconGetBaryonVersion() <= 0x70503 )
  {
    result = sub_810060EC(v2, v3);
  }
  else if ( v2 >= 0x10000 )
  {
    result = -2145058815;
  }
  else
  {
    v9 = v2;
    v6 = -122;
    v7 = 9;
    v8 = 3;
    result = exp_SceSysconForDriver_ksceSysconCmdExec((int)&v5, 0);
    if ( result >= 0 )
    {
      result = 0;
      *v3 = v10 | (unsigned __int16)(v11 << 8);
    }
  }
  if ( v12 != MEMORY[0] )
    imp_SceSysclibForDriver___stack_chk_fail(result);
  return result;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x9B779DB0]
int __fastcall exp_SceSysconForDriver_0x9B779DB0(unsigned int a1, __int16 a2)
{
  unsigned int v2; // r5
  __int16 v3; // r6
  signed int v4; // r0
  int result; // r0
  int v6; // [sp+0h] [bp-A0h]
  char v7; // [sp+10h] [bp-90h]
  char v8; // [sp+11h] [bp-8Fh]
  char v9; // [sp+12h] [bp-8Eh]
  __int16 v10; // [sp+13h] [bp-8Dh]
  char v11; // [sp+15h] [bp-8Bh]
  char v12; // [sp+16h] [bp-8Ah]
  int v13; // [sp+84h] [bp-1Ch]

  v2 = a1;
  v3 = a2;
  v13 = MEMORY[0];
  if ( (unsigned int)exp_SceSysconForDriver_ksceSysconGetBaryonVersion() <= 0x70503 )
  {
    result = sub_81006154(v2, v3);
  }
  else if ( v2 >= 0x10000 )
  {
    result = -2145058815;
  }
  else
  {
    v10 = v2;
    v11 = HIBYTE(v3);
    v12 = v3;
    v7 = -121;
    v8 = 9;
    v9 = 5;
    v4 = exp_SceSysconForDriver_ksceSysconCmdExec((int)&v6, 0);
    result = v4 & (v4 >> 31);
  }
  if ( v13 != MEMORY[0] )
    imp_SceSysclibForDriver___stack_chk_fail(result);
  return result;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0xA5AB19B1]
signed int __fastcall exp_SceSysconForDriver_0xA5AB19B1(unsigned int a1, _WORD *a2)
{
  unsigned int v2; // r5
  _WORD *v3; // r6
  signed int result; // r0
  int v5; // [sp+0h] [bp-A0h]
  char v6; // [sp+10h] [bp-90h]
  char v7; // [sp+11h] [bp-8Fh]
  char v8; // [sp+12h] [bp-8Eh]
  __int16 v9; // [sp+13h] [bp-8Dh]
  unsigned __int8 v10; // [sp+34h] [bp-6Ch]
  unsigned __int8 v11; // [sp+35h] [bp-6Bh]
  int v12; // [sp+84h] [bp-1Ch]

  v2 = a1;
  v3 = a2;
  v12 = MEMORY[0];
  if ( (unsigned int)exp_SceSysconForDriver_ksceSysconGetBaryonVersion() <= 0x70503 )
  {
    result = sub_810061B8(v2, v3);
  }
  else if ( v2 >= 0x10000 )
  {
    result = -2145058815;
  }
  else
  {
    v9 = v2;
    v6 = -120;
    v7 = 9;
    v8 = 3;
    result = exp_SceSysconForDriver_ksceSysconCmdExec((int)&v5, 0);
    if ( result >= 0 )
    {
      result = 0;
      *v3 = v10 | (unsigned __int16)(v11 << 8);
    }
  }
  if ( v12 != MEMORY[0] )
    imp_SceSysclibForDriver___stack_chk_fail(result);
  return result;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x87DA378D]
int exp_SceSysconForDriver_ksceSysconBatterySWReset()
{
  signed int v0; // r0
  int result; // r0
  int v2; // [sp+0h] [bp-98h]
  char v3; // [sp+10h] [bp-88h]
  char v4; // [sp+11h] [bp-87h]
  char v5; // [sp+12h] [bp-86h]
  char v6; // [sp+13h] [bp-85h]
  int v7; // [sp+84h] [bp-14h]

  v7 = MEMORY[0];
  if ( (unsigned int)exp_SceSysconForDriver_ksceSysconGetBaryonVersion() <= 0x70503 )
  {
    result = -2145058241;
  }
  else
  {
    v6 = 0;
    v3 = -119;
    v4 = 9;
    v5 = 1;
    v0 = exp_SceSysconForDriver_ksceSysconCmdExec((int)&v2, 0);
    result = v0 & (v0 >> 31);
  }
  if ( v7 != MEMORY[0] )
    imp_SceSysclibForDriver___stack_chk_fail(result);
  return result;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0xF93CF833]
signed int __fastcall exp_SceSysconForDriver_0xF93CF833(_DWORD *a1)
{
  *a1 = 0;
  return sub_81002D20(a1, 0x98Au);
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0xE1885F68]
signed int __fastcall exp_SceSysconForDriver_0xE1885F68(_DWORD *a1)
{
  *a1 = 0;
  return sub_81002D20(a1, 0x98Bu);
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0xCD73079D]
signed int __fastcall exp_SceSysconForDriver_0xCD73079D(_DWORD *a1)
{
  *a1 = 0;
  return sub_81002D20(a1, 0x98Cu);
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x2CEF078E]
int exp_SceSysconForDriver_ksceSysconBatteryStartBLMode()
{
  signed int v0; // r0
  int result; // r0
  int v2; // [sp+0h] [bp-98h]
  char v3; // [sp+10h] [bp-88h]
  char v4; // [sp+11h] [bp-87h]
  char v5; // [sp+12h] [bp-86h]
  char v6; // [sp+13h] [bp-85h]
  int v7; // [sp+84h] [bp-14h]

  v7 = MEMORY[0];
  if ( (unsigned int)exp_SceSysconForDriver_ksceSysconGetBaryonVersion() <= 0x70503 )
  {
    result = sub_81006220();
  }
  else
  {
    v6 = 0;
    v3 = -80;
    v4 = 9;
    v5 = 1;
    v0 = exp_SceSysconForDriver_ksceSysconCmdExec((int)&v2, 0);
    result = v0 & (v0 >> 31);
  }
  if ( v7 != MEMORY[0] )
    imp_SceSysclibForDriver___stack_chk_fail(result);
  return result;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0xE4AE7852]
int exp_SceSysconForDriver_ksceSysconBatteryStopBLMode()
{
  signed int v0; // r0
  int result; // r0
  int v2; // [sp+0h] [bp-98h]
  char v3; // [sp+10h] [bp-88h]
  char v4; // [sp+11h] [bp-87h]
  char v5; // [sp+12h] [bp-86h]
  char v6; // [sp+13h] [bp-85h]
  int v7; // [sp+84h] [bp-14h]

  v7 = MEMORY[0];
  if ( (unsigned int)exp_SceSysconForDriver_ksceSysconGetBaryonVersion() <= 0x70503 )
  {
    result = sub_81006264();
  }
  else
  {
    v6 = 0;
    v3 = -79;
    v4 = 9;
    v5 = 1;
    v0 = exp_SceSysconForDriver_ksceSysconCmdExec((int)&v2, 0);
    result = v0 & (v0 >> 31);
  }
  if ( v7 != MEMORY[0] )
    imp_SceSysclibForDriver___stack_chk_fail(result);
  return result;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0xE4F29744]
int __fastcall exp_SceSysconForDriver_ksceSysconBatterySetBLCommand(__int16 a1, char a2, int a3, int a4)
{
  __int16 v4; // r6
  int v5; // r5
  char v6; // r7
  int v7; // r8
  signed int v8; // r0
  int result; // r0
  int v10; // [sp+0h] [bp-A0h]
  char v11; // [sp+10h] [bp-90h]
  char v12; // [sp+11h] [bp-8Fh]
  char v13; // [sp+12h] [bp-8Eh]
  __int16 v14; // [sp+13h] [bp-8Dh]
  char v15; // [sp+15h] [bp-8Bh]
  char v16; // [sp+16h] [bp-8Ah]
  char v17; // [sp+17h] [bp-89h]
  int v18; // [sp+84h] [bp-1Ch]

  v4 = a1;
  v5 = a4;
  v6 = a2;
  v7 = a3;
  v18 = MEMORY[0];
  if ( (unsigned int)exp_SceSysconForDriver_ksceSysconGetBaryonVersion() <= 0x70503 )
  {
    result = sub_810062A8(v4, v6, v7, v5);
  }
  else if ( (unsigned int)(v5 - 1) > 0xF )
  {
    result = -2145058815;
  }
  else
  {
    v14 = v4;
    v15 = v5;
    v13 = v5 + 5;
    v16 = v6;
    v11 = -78;
    v12 = 9;
    imp_SceSysclibForDriver_memcpy(&v17, v7, v5);
    v8 = exp_SceSysconForDriver_ksceSysconCmdExec((int)&v10, 0);
    result = v8 & (v8 >> 31);
  }
  if ( v18 != MEMORY[0] )
    imp_SceSysclibForDriver___stack_chk_fail(result);
  return result;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x74B2AB55]
int __fastcall exp_SceSysconForDriver_ksceSysconBatteryExecBLCommand(__int16 a1)
{
  __int16 v1; // r5
  signed int v2; // r0
  int result; // r0
  int v4; // [sp+0h] [bp-A0h]
  char v5; // [sp+10h] [bp-90h]
  char v6; // [sp+11h] [bp-8Fh]
  char v7; // [sp+12h] [bp-8Eh]
  __int16 v8; // [sp+13h] [bp-8Dh]
  char v9; // [sp+15h] [bp-8Bh]
  int v10; // [sp+84h] [bp-1Ch]

  v1 = a1;
  v10 = MEMORY[0];
  if ( (unsigned int)exp_SceSysconForDriver_ksceSysconGetBaryonVersion() <= 0x70503 )
  {
    result = sub_8100631C(v1);
  }
  else
  {
    v8 = v1;
    v9 = 0;
    v5 = -77;
    v6 = 9;
    v7 = 3;
    v2 = exp_SceSysconForDriver_ksceSysconCmdExec((int)&v4, 0);
    result = v2 & (v2 >> 31);
  }
  if ( v10 != MEMORY[0] )
    imp_SceSysclibForDriver___stack_chk_fail(result);
  return result;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x448DAFF1]
signed int __fastcall exp_SceSysconForDriver_ksceSysconBatteryReadBLCommand(__int16 a1, char a2, char a3, int a4, int a5)
{
  __int16 v5; // r7
  char v6; // r8
  char v7; // r9
  int v8; // r10
  signed int result; // r0
  char v10; // [sp+8h] [bp-A8h]
  char v11; // [sp+18h] [bp-98h]
  char v12; // [sp+19h] [bp-97h]
  char v13; // [sp+1Ah] [bp-96h]
  __int16 v14; // [sp+1Bh] [bp-95h]
  char v15; // [sp+1Dh] [bp-93h]
  char v16; // [sp+1Eh] [bp-92h]
  char v17; // [sp+1Fh] [bp-91h]
  char v18; // [sp+3Eh] [bp-72h]
  int v19; // [sp+8Ch] [bp-24h]

  v5 = a1;
  v6 = a2;
  v7 = a3;
  v8 = a4;
  v19 = MEMORY[0];
  if ( (unsigned int)exp_SceSysconForDriver_ksceSysconGetBaryonVersion() <= 0x70503 )
  {
    result = sub_81006374(v5, v6, v7, v8, a5);
  }
  else if ( (unsigned int)(a5 - 1) > 0xF )
  {
    result = -2145058815;
  }
  else
  {
    v14 = v5;
    v15 = a5;
    v16 = v6;
    v17 = v7;
    v11 = -76;
    v12 = 9;
    v13 = 6;
    result = exp_SceSysconForDriver_ksceSysconCmdExec((int)&v10, 0);
    if ( result >= 0 )
    {
      imp_SceSysclibForDriver_memcpy(v8, &v18, a5);
      result = 0;
    }
  }
  if ( v19 != MEMORY[0] )
    imp_SceSysclibForDriver___stack_chk_fail(result);
  return result;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0xC51B4E6F]
signed int __fastcall exp_SceSysconForDriver_0xC51B4E6F(_DWORD *a1)
{
  signed int v1; // r7
  _DWORD *v2; // r6
  char *v3; // r4
  signed int v4; // r0
  signed int v5; // r1
  bool v6; // nf
  int v7; // r0
  signed int *v8; // r4
  int v9; // r1
  int v10; // r2
  int v11; // r3
  int v12; // r2
  int v13; // r3
  signed int result; // r0
  int v15; // [sp+0h] [bp-A0h]
  char v16; // [sp+10h] [bp-90h]
  char v17; // [sp+11h] [bp-8Fh]
  char v18; // [sp+12h] [bp-8Eh]
  char v19; // [sp+34h] [bp-6Ch]
  int v20; // [sp+84h] [bp-1Ch]

  v1 = 0;
  v2 = a1;
  v3 = (char *)MEMORY[0];
  v17 = 10;
  v20 = MEMORY[0];
  v18 = 1;
  v16 = 0;
  v4 = exp_SceSysconForDriver_ksceSysconCmdExec((int)&v15, 0);
  v6 = v4 < 0;
  if ( v4 < 0 )
    v1 = v4;
  else
    v3 = &v19;
  if ( v4 >= 0 )
  {
    v7 = *(_DWORD *)v3;
    v9 = *((_DWORD *)v3 + 1);
    v10 = *((_DWORD *)v3 + 2);
    v11 = *((_DWORD *)v3 + 3);
    v8 = (signed int *)(v3 + 16);
    *v2 = v7;
    v2[1] = v9;
    v4 = *v8;
    v5 = v8[1];
    v2[2] = v10;
    v2[3] = v11;
  }
  v12 = v20;
  v13 = MEMORY[0];
  if ( !v6 )
    v2[4] = v4;
  result = v1;
  if ( !v6 )
    v2[5] = v5;
  if ( v12 != v13 )
    imp_SceSysclibForDriver___stack_chk_fail(v1);
  return result;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x36AC812B]
signed int __fastcall exp_SceSysconForDriver_0x36AC812B(int a1, int a2, int a3)
{
  int v3; // r5
  int v4; // r8
  signed int result; // r0
  int v6; // [sp+0h] [bp-A0h]
  char v7; // [sp+10h] [bp-90h]
  char v8; // [sp+11h] [bp-8Fh]
  char v9; // [sp+12h] [bp-8Eh]
  char v10; // [sp+13h] [bp-8Dh]
  char v11; // [sp+14h] [bp-8Ch]
  char v12; // [sp+34h] [bp-6Ch]
  int v13; // [sp+84h] [bp-1Ch]

  v3 = a2;
  v4 = a3;
  v13 = MEMORY[0];
  if ( (unsigned int)(a2 - 1) > 0x1A || (unsigned int)(a2 + a1) > 0x100 )
  {
    result = -2145058815;
  }
  else
  {
    v10 = a1;
    v11 = a2;
    v7 = -128;
    v8 = 10;
    v9 = 3;
    result = exp_SceSysconForDriver_ksceSysconCmdExec((int)&v6, 0);
    if ( result >= 0 )
    {
      imp_SceSysclibForDriver_memcpy(v4, &v12, v3);
      result = 0;
    }
  }
  if ( v13 != MEMORY[0] )
    imp_SceSysclibForDriver___stack_chk_fail(result);
  return result;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x26E36625]
int __fastcall exp_SceSysconForDriver_0x26E36625(int a1, int a2, int a3)
{
  signed int v3; // r0
  int result; // r0
  int v5; // [sp+0h] [bp-A0h]
  char v6; // [sp+10h] [bp-90h]
  char v7; // [sp+11h] [bp-8Fh]
  char v8; // [sp+12h] [bp-8Eh]
  char v9; // [sp+13h] [bp-8Dh]
  char v10; // [sp+14h] [bp-8Ch]
  char v11; // [sp+15h] [bp-8Bh]
  int v12; // [sp+84h] [bp-1Ch]

  v12 = MEMORY[0];
  if ( (unsigned int)(a2 - 1) > 0x1A || (unsigned int)(a2 + a1) > 0x100 )
  {
    result = -2145058815;
  }
  else
  {
    v9 = a1;
    v7 = 10;
    v10 = a2;
    v8 = a2 + 3;
    v6 = -127;
    imp_SceSysclibForDriver_memcpy(&v11, a3, a2);
    v3 = exp_SceSysconForDriver_ksceSysconCmdExec((int)&v5, 0);
    result = v3 & (v3 >> 31);
  }
  if ( v12 != MEMORY[0] )
    imp_SceSysclibForDriver___stack_chk_fail(result);
  return result;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x7BAFE083]
int __fastcall exp_SceSysconForDriver_0x7BAFE083(unsigned int a1)
{
  return sub_810030C8(a1, 0xA82u, 2u);
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x81A6060D]
int __fastcall exp_SceSysconForDriver_ksceSysconNvsSetRunMode(unsigned int a1)
{
  return sub_810030C8(a1, 0x1080u, 2u);
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x2EC6D55D]
int __fastcall exp_SceSysconForDriver_0x2EC6D55D(unsigned int a1)
{
  return sub_810030C8(a1, 0x1081u, 2u);
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0xACAFA2B8]
signed int __fastcall exp_SceSysconForDriver_ksceSysconNvsReadData(int a1, int a2, unsigned int a3)
{
  unsigned int v3; // r4
  unsigned int v4; // r3
  bool v5; // cf
  bool v6; // zf
  int v7; // r7
  bool v8; // zf
  signed int result; // r0
  int v10; // [sp+0h] [bp-A0h]
  char v11; // [sp+10h] [bp-90h]
  char v12; // [sp+11h] [bp-8Fh]
  char v13; // [sp+12h] [bp-8Eh]
  __int16 v14; // [sp+13h] [bp-8Dh]
  char v15; // [sp+15h] [bp-8Bh]
  char v16; // [sp+34h] [bp-6Ch]
  int v17; // [sp+84h] [bp-1Ch]

  v3 = a3;
  v4 = a3 - 1;
  v5 = a3 >= 4;
  v6 = a3 == 4;
  if ( a3 != 4 )
  {
    v5 = v4 >= 1;
    v6 = v4 == 1;
  }
  v7 = a2;
  v17 = MEMORY[0];
  if ( !v6 && v5 )
  {
    v8 = a3 == 8;
    if ( a3 != 8 )
      v8 = a3 == 16;
    if ( !v8 )
      goto LABEL_19;
  }
  if ( v4 & a1 || a3 + a1 > 0xB60 )
  {
LABEL_19:
    result = -2145058815;
  }
  else
  {
    v14 = a1;
    v15 = a3;
    v11 = -126;
    v12 = 16;
    v13 = 4;
    result = exp_SceSysconForDriver_ksceSysconCmdExec((int)&v10, 0);
    if ( result >= 0 )
    {
      imp_SceSysclibForDriver_memcpy(v7, &v16, v3);
      result = 0;
    }
  }
  if ( v17 != MEMORY[0] )
    imp_SceSysclibForDriver___stack_chk_fail(result);
  return result;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x10C9657A]
int __fastcall exp_SceSysconForDriver_ksceSysconNvsWriteData(int a1, int a2, unsigned int a3)
{
  unsigned int v3; // r3
  bool v4; // cf
  bool v5; // zf
  bool v6; // zf
  signed int v7; // r0
  int result; // r0
  int v9; // [sp+0h] [bp-A0h]
  char v10; // [sp+10h] [bp-90h]
  char v11; // [sp+11h] [bp-8Fh]
  char v12; // [sp+12h] [bp-8Eh]
  __int16 v13; // [sp+13h] [bp-8Dh]
  char v14; // [sp+15h] [bp-8Bh]
  char v15; // [sp+16h] [bp-8Ah]
  int v16; // [sp+84h] [bp-1Ch]

  v3 = a3 - 1;
  v4 = a3 >= 4;
  v5 = a3 == 4;
  if ( a3 != 4 )
  {
    v4 = v3 >= 1;
    v5 = v3 == 1;
  }
  v16 = MEMORY[0];
  if ( !v5 && v4 )
  {
    v6 = a3 == 8;
    if ( a3 != 8 )
      v6 = a3 == 16;
    if ( !v6 )
      goto LABEL_18;
  }
  if ( v3 & a1 || a3 + a1 > 0xB60 )
  {
LABEL_18:
    result = -2145058815;
  }
  else
  {
    v13 = a1;
    v12 = a3 + 4;
    v11 = 16;
    v14 = a3;
    v10 = -125;
    imp_SceSysclibForDriver_memcpy(&v15, a2, a3);
    v7 = exp_SceSysconForDriver_ksceSysconCmdExec((int)&v9, 0);
    result = v7 & (v7 >> 31);
  }
  if ( v16 != MEMORY[0] )
    imp_SceSysclibForDriver___stack_chk_fail(result);
  return result;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x44A173F5]
int exp_SceSysconForDriver_ksceSysconJigOpenPort()
{
  int result; // r0
  signed int v1; // r6
  int v2; // [sp+0h] [bp-A0h]
  char v3; // [sp+10h] [bp-90h]
  char v4; // [sp+11h] [bp-8Fh]
  char v5; // [sp+12h] [bp-8Eh]
  char v6; // [sp+13h] [bp-8Dh]
  int v7; // [sp+84h] [bp-1Ch]

  v7 = MEMORY[0];
  result = imp_SceThreadmgrForDriver_ksceKernelLockMutex(dword_8100C268, 1, 0);
  if ( result >= 0 )
  {
    if ( dword_8100C26C > 0 )
    {
      imp_SceThreadmgrForDriver_ksceKernelUnlockMutex(dword_8100C268, 1);
      result = -2145058815;
    }
    else
    {
      v4 = 32;
      v6 = 0;
      v3 = -128;
      v5 = 2;
      v1 = exp_SceSysconForDriver_ksceSysconCmdExec((int)&v2, 0);
      if ( v1 >= 0 && dword_8100C26C < 0 )
        dword_8100C26C = 1;
      imp_SceThreadmgrForDriver_ksceKernelUnlockMutex(dword_8100C268, 1);
      result = v1;
    }
  }
  if ( v7 != MEMORY[0] )
    imp_SceSysclibForDriver___stack_chk_fail(result);
  return result;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x483FAE05]
int exp_SceSysconForDriver_ksceSysconJigClosePort()
{
  int result; // r0
  signed int v1; // r7
  int v2; // [sp+0h] [bp-A0h]
  char v3; // [sp+10h] [bp-90h]
  char v4; // [sp+11h] [bp-8Fh]
  char v5; // [sp+12h] [bp-8Eh]
  char v6; // [sp+13h] [bp-8Dh]
  int v7; // [sp+84h] [bp-1Ch]

  v7 = MEMORY[0];
  result = imp_SceThreadmgrForDriver_ksceKernelLockMutex(dword_8100C268, 1, 0);
  if ( result >= 0 )
  {
    if ( dword_8100C26C < 0 )
    {
      imp_SceThreadmgrForDriver_ksceKernelUnlockMutex(dword_8100C268, 1);
      result = -2145058815;
    }
    else
    {
      v3 = -127;
      v6 = 0;
      v4 = 32;
      v5 = 1;
      v1 = exp_SceSysconForDriver_ksceSysconCmdExec((int)&v2, 0);
      if ( v1 >= 0 )
        dword_8100C26C = -1;
      imp_SceThreadmgrForDriver_ksceKernelUnlockMutex(dword_8100C268, 1);
      result = v1;
    }
  }
  if ( v7 != MEMORY[0] )
    imp_SceSysclibForDriver___stack_chk_fail(result);
  return result;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0xD24BF916]
int __fastcall exp_SceSysconForDriver_ksceSysconJigSetConfig(char a1, char a2)
{
  char v2; // r7
  char v3; // r6
  int result; // r0
  signed int v5; // r6
  int v6; // [sp+0h] [bp-A0h]
  char v7; // [sp+10h] [bp-90h]
  char v8; // [sp+11h] [bp-8Fh]
  char v9; // [sp+12h] [bp-8Eh]
  char v10; // [sp+13h] [bp-8Dh]
  char v11; // [sp+14h] [bp-8Ch]
  char v12; // [sp+15h] [bp-8Bh]
  char v13; // [sp+16h] [bp-8Ah]
  int v14; // [sp+84h] [bp-1Ch]

  v2 = a1;
  v3 = a2;
  v14 = MEMORY[0];
  result = imp_SceThreadmgrForDriver_ksceKernelLockMutex(dword_8100C268, 1, 0);
  if ( result >= 0 )
  {
    if ( dword_8100C26C < 0 )
    {
      imp_SceThreadmgrForDriver_ksceKernelUnlockMutex(dword_8100C268, 1);
      result = -2145058815;
    }
    else
    {
      v11 = v3;
      v12 = 0;
      v13 = 0;
      v7 = -126;
      v8 = 32;
      v9 = 5;
      v10 = v2;
      v5 = exp_SceSysconForDriver_ksceSysconCmdExec((int)&v6, 0);
      imp_SceThreadmgrForDriver_ksceKernelUnlockMutex(dword_8100C268, 1);
      result = v5;
    }
  }
  if ( v14 != MEMORY[0] )
    imp_SceSysclibForDriver___stack_chk_fail(result);
  return result;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x3C80B529]
int __fastcall exp_SceSysconForDriver_0x3C80B529(unsigned int a1, unsigned int a2, int a3)
{
  int v3; // r5
  char v4; // r6
  unsigned int v5; // r4
  int v6; // r3
  int result; // r0
  signed int v8; // r11
  signed int v9; // r10
  int v10; // [sp+4h] [bp-B4h]
  char v11; // [sp+8h] [bp-B0h]
  char v12; // [sp+18h] [bp-A0h]
  char v13; // [sp+19h] [bp-9Fh]
  char v14; // [sp+1Ah] [bp-9Eh]
  char v15; // [sp+1Bh] [bp-9Dh]
  char v16; // [sp+1Ch] [bp-9Ch]
  char v17; // [sp+3Ch] [bp-7Ch]
  int v18; // [sp+8Ch] [bp-2Ch]

  v3 = a3;
  v4 = a1;
  v5 = a2;
  v18 = MEMORY[0];
  if ( a3 )
  {
    if ( a1 <= 0x3F && a2 )
    {
      LOWORD(v6) = -16384;
      HIWORD(v6) = (unsigned int)dword_8100C000 >> 16;
      v10 = v6;
      result = imp_SceThreadmgrForDriver_ksceKernelLockMutex(*(_DWORD *)(v6 + 0x268), 1, 0);
      if ( result >= 0 )
      {
        if ( *(_DWORD *)(v10 + 620) < 0 )
        {
          imp_SceThreadmgrForDriver_ksceKernelUnlockMutex(*(_DWORD *)(v10 + 616), 1);
          result = -2145058815;
        }
        else
        {
          do
          {
            if ( v5 >= 0x18 )
              v8 = 24;
            else
              v8 = v5;
            v12 = -125;
            v13 = 32;
            v14 = 3;
            v15 = v4;
            v16 = v8;
            v9 = exp_SceSysconForDriver_ksceSysconCmdExec((int)&v11, 0);
            if ( v9 < 0 )
              break;
            v4 += v8;
            imp_SceSysclibForDriver_memcpy(v3, &v17, v8);
            v5 -= v8;
            v3 += v8;
          }
          while ( v5 );
          imp_SceThreadmgrForDriver_ksceKernelUnlockMutex(*(_DWORD *)(v10 + 616), 1);
          result = v9;
        }
      }
    }
    else
    {
      result = -2145058815;
    }
  }
  else
  {
    result = -2145058814;
  }
  if ( v18 != MEMORY[0] )
    imp_SceSysclibForDriver___stack_chk_fail(result);
  return result;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0xCE346793]
int __fastcall exp_SceSysconForDriver_0xCE346793(int a1, int a2)
{
  int v2; // r7
  int v3; // r6
  int v4; // r4
  int result; // r0
  signed int v6; // r6
  int v7; // [sp+0h] [bp-A0h]
  char v8; // [sp+10h] [bp-90h]
  char v9; // [sp+11h] [bp-8Fh]
  char v10; // [sp+12h] [bp-8Eh]
  char v11; // [sp+13h] [bp-8Dh]
  char v12; // [sp+14h] [bp-8Ch]
  char v13; // [sp+15h] [bp-8Bh]
  int v14; // [sp+84h] [bp-1Ch]

  v2 = a2;
  v3 = a1;
  v14 = MEMORY[0];
  if ( a2 )
  {
    if ( (unsigned int)(a1 - 1) > 0x17 )
    {
      result = -2145058815;
    }
    else
    {
      LOWORD(v4) = -16384;
      HIWORD(v4) = (unsigned int)dword_8100C000 >> 16;
      result = imp_SceThreadmgrForDriver_ksceKernelLockMutex(*(_DWORD *)(v4 + 0x268), 1, 0);
      if ( result >= 0 )
      {
        if ( *(_DWORD *)(v4 + 0x26C) < 0 )
        {
          imp_SceThreadmgrForDriver_ksceKernelUnlockMutex(*(_DWORD *)(v4 + 0x268), 1);
          result = -2145058815;
        }
        else
        {
          v8 = -124;
          v9 = 32;
          v10 = v3 + 3;
          v12 = v3;
          v11 = 0;
          imp_SceSysclibForDriver_memcpy(&v13, v2, v3);
          v6 = exp_SceSysconForDriver_ksceSysconCmdExec((int)&v7, 0);
          imp_SceThreadmgrForDriver_ksceKernelUnlockMutex(*(_DWORD *)(v4 + 0x268), 1);
          result = v6;
        }
      }
    }
  }
  else
  {
    result = -2145058814;
  }
  if ( v14 != MEMORY[0] )
    imp_SceSysclibForDriver___stack_chk_fail(result);
  return result;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x7BFBA09E]
int __fastcall exp_SceSysconForDriver_0x7BFBA09E(char a1, unsigned int a2, unsigned int a3, int a4)
{
  int v4; // r6
  char v5; // r7
  unsigned int v6; // r5
  char v7; // r4
  int v8; // r11
  int result; // r0
  char v10; // r10
  signed int v11; // r4
  signed int v12; // r2
  int v13; // ST00_4
  char v14; // [sp+8h] [bp-B0h]
  char v15; // [sp+18h] [bp-A0h]
  char v16; // [sp+19h] [bp-9Fh]
  char v17; // [sp+1Ah] [bp-9Eh]
  char v18; // [sp+1Bh] [bp-9Dh]
  char v19; // [sp+1Ch] [bp-9Ch]
  char v20; // [sp+1Dh] [bp-9Bh]
  char v21; // [sp+1Eh] [bp-9Ah]
  int v22; // [sp+8Ch] [bp-2Ch]

  v4 = a4;
  v5 = a1;
  v6 = a3;
  v7 = a2;
  v22 = MEMORY[0];
  if ( a4 )
  {
    if ( a2 <= 0x3F && a3 && (signed int)(a2 + a3) <= 63 )
    {
      LOWORD(v8) = -16384;
      HIWORD(v8) = (unsigned int)dword_8100C000 >> 16;
      result = imp_SceThreadmgrForDriver_ksceKernelLockMutex(*(_DWORD *)(v8 + 0x268), 1, 0);
      if ( result >= 0 )
      {
        if ( *(_DWORD *)(v8 + 0x26C) < 0 )
        {
          imp_SceThreadmgrForDriver_ksceKernelUnlockMutex(*(_DWORD *)(v8 + 0x268), 1);
          result = -2145058815;
        }
        else
        {
          v10 = v7;
          do
          {
            if ( v6 >= 0x18 )
              v11 = 24;
            else
              v11 = v6;
            v15 = -123;
            v20 = v11;
            v17 = v11 + 4;
            v16 = 32;
            v18 = v5;
            v19 = v10;
            imp_SceSysclibForDriver_memcpy(&v21, v4, v11);
            v12 = exp_SceSysconForDriver_ksceSysconCmdExec((int)&v14, 0);
            if ( v12 < 0 )
              break;
            v6 -= v11;
            v10 += v11;
            v4 += v11;
          }
          while ( v6 );
          v13 = v12;
          imp_SceThreadmgrForDriver_ksceKernelUnlockMutex(*(_DWORD *)(v8 + 0x268), 1);
          result = v13;
        }
      }
    }
    else
    {
      result = -2145058815;
    }
  }
  else
  {
    result = -2145058814;
  }
  if ( v22 != MEMORY[0] )
    imp_SceSysclibForDriver___stack_chk_fail(result);
  return result;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x933D813F]
int __fastcall exp_SceSysconForDriver_0x933D813F(char a1, unsigned int a2, int a3)
{
  char v3; // r6
  char v4; // r8
  char v5; // r7
  int v6; // r5
  int result; // r0
  signed int v8; // r6
  int v9; // [sp+0h] [bp-A0h]
  char v10; // [sp+10h] [bp-90h]
  char v11; // [sp+11h] [bp-8Fh]
  char v12; // [sp+12h] [bp-8Eh]
  char v13; // [sp+13h] [bp-8Dh]
  char v14; // [sp+14h] [bp-8Ch]
  char v15; // [sp+15h] [bp-8Bh]
  int v16; // [sp+84h] [bp-1Ch]

  v3 = a2;
  v4 = a1;
  v5 = a3;
  v16 = MEMORY[0];
  if ( a2 <= 0x3F && a3 && (signed int)(a2 + a3) <= 63 )
  {
    LOWORD(v6) = -16384;
    HIWORD(v6) = (unsigned int)dword_8100C000 >> 16;
    result = imp_SceThreadmgrForDriver_ksceKernelLockMutex(*(_DWORD *)(v6 + 0x268), 1, 0);
    if ( result >= 0 )
    {
      if ( *(_DWORD *)(v6 + 0x26C) < 0 )
      {
        imp_SceThreadmgrForDriver_ksceKernelUnlockMutex(*(_DWORD *)(v6 + 0x268), 1);
        result = -2145058815;
      }
      else
      {
        v14 = v3;
        v10 = -123;
        v11 = 32;
        v12 = 4;
        v13 = v4;
        v15 = v5;
        v8 = exp_SceSysconForDriver_ksceSysconCmdExec((int)&v9, 0);
        imp_SceThreadmgrForDriver_ksceKernelUnlockMutex(*(_DWORD *)(v6 + 0x268), 1);
        result = v8;
      }
    }
  }
  else
  {
    result = -2145058815;
  }
  if ( v16 != MEMORY[0] )
    imp_SceSysclibForDriver___stack_chk_fail(result);
  return result;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0xD2F456DC]
signed int __fastcall exp_SceSysconForDriver_0xD2F456DC(char *a1)
{
  char *v1; // r4
  signed int result; // r0
  char v3; // r1
  char v4; // r2
  char v5; // r3
  int v6; // [sp+0h] [bp-98h]
  char v7; // [sp+10h] [bp-88h]
  char v8; // [sp+11h] [bp-87h]
  char v9; // [sp+12h] [bp-86h]
  char v10; // [sp+13h] [bp-85h]
  char v11; // [sp+34h] [bp-64h]
  char v12; // [sp+35h] [bp-63h]
  char v13; // [sp+36h] [bp-62h]
  char v14; // [sp+37h] [bp-61h]
  int v15; // [sp+84h] [bp-14h]

  v1 = a1;
  v15 = MEMORY[0];
  if ( a1 )
  {
    v7 = 0;
    v10 = 0;
    v8 = 17;
    v9 = 1;
    result = exp_SceSysconForDriver_ksceSysconCmdExec((int)&v6, 0);
    if ( result >= 0 )
    {
      v3 = v12;
      v4 = v13;
      v5 = v14;
      v1[3] = v11;
      v1[2] = v3;
      v1[1] = v4;
      *v1 = v5;
    }
  }
  else
  {
    result = -2145058815;
  }
  if ( v15 != MEMORY[0] )
    imp_SceSysclibForDriver___stack_chk_fail(result);
  return result;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x9B00BC7F]
signed int __fastcall exp_SceSysconForDriver_ksceSysconUpdaterSetSegment(char a1)
{
  signed int result; // r0
  int v2; // [sp+0h] [bp-A0h]
  char v3; // [sp+10h] [bp-90h]
  char v4; // [sp+11h] [bp-8Fh]
  char v5; // [sp+12h] [bp-8Eh]
  char v6; // [sp+13h] [bp-8Dh]
  char v7; // [sp+14h] [bp-8Ch]
  int v8; // [sp+84h] [bp-1Ch]

  v6 = a1;
  v7 = 0;
  v5 = 2;
  v8 = MEMORY[0];
  v3 = -128;
  v4 = 17;
  result = exp_SceSysconForDriver_ksceSysconCmdExec((int)&v2, 0);
  if ( v8 != MEMORY[0] )
    imp_SceSysclibForDriver___stack_chk_fail(result);
  return result;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x356B9696]
signed int __fastcall exp_SceSysconForDriver_0x356B9696(int a1, __int16 a2, int a3)
{
  signed int result; // r0
  int v4; // [sp+0h] [bp-A0h]
  char v5; // [sp+10h] [bp-90h]
  char v6; // [sp+11h] [bp-8Fh]
  char v7; // [sp+12h] [bp-8Eh]
  __int16 v8; // [sp+13h] [bp-8Dh]
  char v9; // [sp+15h] [bp-8Bh]
  char v10; // [sp+16h] [bp-8Ah]
  int v11; // [sp+84h] [bp-1Ch]

  v11 = MEMORY[0];
  if ( a1 )
  {
    v8 = a2;
    v9 = a3;
    v7 = a3 + 4;
    v5 = -127;
    v6 = 17;
    imp_SceSysclibForDriver_memcpy(&v10, a1, a3);
    result = exp_SceSysconForDriver_ksceSysconCmdExec((int)&v4, 0);
  }
  else
  {
    result = -2145058815;
  }
  if ( v11 != MEMORY[0] )
    imp_SceSysclibForDriver___stack_chk_fail(result);
  return result;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x734544E4]
signed int __fastcall exp_SceSysconForDriver_0x734544E4(int a1, __int16 a2, int a3)
{
  int v3; // r6
  __int16 v4; // r7
  int v5; // r5
  signed int result; // r0
  char v7; // [sp+4h] [bp-124h]
  int v8; // [sp+8h] [bp-120h]
  char v9; // [sp+14h] [bp-114h]
  char v10; // [sp+15h] [bp-113h]
  char v11; // [sp+16h] [bp-112h]
  __int16 v12; // [sp+17h] [bp-111h]
  char v13; // [sp+19h] [bp-10Fh]
  char *v14; // [sp+54h] [bp-D4h]
  char v15; // [sp+84h] [bp-A4h]
  char v16; // [sp+85h] [bp-A3h]
  char v17; // [sp+86h] [bp-A2h]
  __int16 v18; // [sp+87h] [bp-A1h]
  char v19; // [sp+89h] [bp-9Fh]
  char v20; // [sp+8Ah] [bp-9Eh]
  int v21; // [sp+104h] [bp-24h]

  v3 = a1;
  v4 = a2;
  v5 = a3;
  v21 = MEMORY[0];
  if ( a1 )
  {
    imp_SceSysclibForDriver_memset(&v15, 0, 128);
    v19 = v5;
    v14 = &v15;
    v13 = v5;
    v11 = v5 + 4;
    v17 = v5 + 4;
    v8 = 2048;
    v9 = -127;
    v15 = -127;
    v10 = 17;
    v16 = 17;
    v12 = v4;
    v18 = v4;
    imp_SceSysclibForDriver_memcpy(&v20, v3, v5);
    result = exp_SceSysconForDriver_ksceSysconCmdExec((int)&v7, 0x800u);
  }
  else
  {
    result = -2145058815;
  }
  if ( v21 != MEMORY[0] )
    imp_SceSysclibForDriver___stack_chk_fail(result);
  return result;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x69AD76E4]
signed int __fastcall exp_SceSysconForDriver_ksceSysconUpdaterExecProgramming(int a1)
{
  signed int result; // r0
  int v2; // [sp+0h] [bp-A0h]
  char v3; // [sp+10h] [bp-90h]
  char v4; // [sp+11h] [bp-8Fh]
  char v5; // [sp+12h] [bp-8Eh]
  __int16 v6; // [sp+13h] [bp-8Dh]
  char v7; // [sp+15h] [bp-8Bh]
  char v8; // [sp+16h] [bp-8Ah]
  char v9; // [sp+17h] [bp-89h]
  int v10; // [sp+84h] [bp-1Ch]

  v6 = a1;
  v9 = 0;
  v5 = 5;
  v10 = MEMORY[0];
  v7 = BYTE2(a1);
  v8 = HIBYTE(a1);
  v3 = -126;
  v4 = 17;
  result = exp_SceSysconForDriver_ksceSysconCmdExec((int)&v2, 0);
  if ( v10 != MEMORY[0] )
    imp_SceSysclibForDriver___stack_chk_fail(result);
  return result;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0xB487C2FB]
signed int __fastcall exp_SceSysconForDriver_ksceSysconUpdaterSetRunMode(unsigned int a1)
{
  signed int result; // r0
  int v2; // [sp+0h] [bp-A0h]
  char v3; // [sp+10h] [bp-90h]
  char v4; // [sp+11h] [bp-8Fh]
  char v5; // [sp+12h] [bp-8Eh]
  __int16 v6; // [sp+13h] [bp-8Dh]
  char v7; // [sp+15h] [bp-8Bh]
  int v8; // [sp+84h] [bp-1Ch]

  v8 = MEMORY[0];
  if ( a1 == 29370 )
    goto LABEL_5;
  if ( a1 <= 0x72BA )
  {
    if ( a1 == 5422 || a1 == 13925 )
      goto LABEL_5;
  }
  else if ( a1 == 39508 || a1 == 50663 )
  {
LABEL_5:
    v6 = a1;
    v7 = 0;
    v3 = -125;
    v4 = 17;
    v5 = 3;
    result = exp_SceSysconForDriver_ksceSysconCmdExec((int)&v2, 0);
    goto LABEL_6;
  }
  result = -2145058815;
LABEL_6:
  if ( v8 != MEMORY[0] )
    imp_SceSysclibForDriver___stack_chk_fail(result);
  return result;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0xC7747A63]
signed int __fastcall exp_SceSysconForDriver_ksceSysconUpdaterExecFinalize(int *a1, int a2)
{
  int v2; // r1
  int v3; // r12
  int v4; // lr
  int v5; // r7
  int v6; // r3
  signed int result; // r0
  int v8; // [sp+0h] [bp-A0h]
  char v9; // [sp+10h] [bp-90h]
  char v10; // [sp+11h] [bp-8Fh]
  char v11; // [sp+12h] [bp-8Eh]
  int v12; // [sp+13h] [bp-8Dh]
  int v13; // [sp+17h] [bp-89h]
  int v14; // [sp+1Bh] [bp-85h]
  int v15; // [sp+1Fh] [bp-81h]
  int v16; // [sp+23h] [bp-7Dh]
  int v17; // [sp+84h] [bp-1Ch]

  v2 = a2 != 20;
  if ( !a1 )
    v2 |= 1u;
  v17 = MEMORY[0];
  if ( v2 )
  {
    result = -2145058815;
  }
  else
  {
    v3 = a1[1];
    v4 = a1[2];
    v5 = a1[3];
    v12 = *a1;
    v6 = a1[4];
    v13 = v3;
    v14 = v4;
    v15 = v5;
    v16 = v6;
    v9 = -124;
    v10 = 17;
    v11 = 21;
    result = exp_SceSysconForDriver_ksceSysconCmdExec((int)&v8, 0);
  }
  if ( v17 != MEMORY[0] )
    imp_SceSysclibForDriver___stack_chk_fail(result);
  return result;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0xCBA836FF]
signed int __fastcall exp_SceSysconForDriver_0xCBA836FF(int *a1, int a2)
{
  int v2; // r1
  int v3; // r2
  int v4; // r8
  int v5; // r12
  int v6; // r5
  int v7; // r2
  signed int result; // r0
  int v9; // [sp+0h] [bp-A0h]
  char v10; // [sp+10h] [bp-90h]
  char v11; // [sp+11h] [bp-8Fh]
  char v12; // [sp+12h] [bp-8Eh]
  int v13; // [sp+13h] [bp-8Dh]
  int v14; // [sp+17h] [bp-89h]
  int v15; // [sp+1Bh] [bp-85h]
  int v16; // [sp+1Fh] [bp-81h]
  int v17; // [sp+23h] [bp-7Dh]
  int v18; // [sp+27h] [bp-79h]
  int v19; // [sp+84h] [bp-1Ch]

  v2 = a2 != 24;
  if ( !a1 )
    v2 |= 1u;
  v19 = MEMORY[0];
  if ( v2 )
  {
    result = -2145058815;
  }
  else
  {
    v3 = a1[1];
    v4 = a1[2];
    v5 = a1[3];
    v13 = *a1;
    v14 = v3;
    v6 = a1[4];
    v7 = a1[5];
    v15 = v4;
    v16 = v5;
    v17 = v6;
    v18 = v7;
    v10 = -123;
    v11 = 17;
    v12 = 25;
    result = exp_SceSysconForDriver_ksceSysconCmdExec((int)&v9, 0);
  }
  if ( v19 != MEMORY[0] )
    imp_SceSysclibForDriver___stack_chk_fail(result);
  return result;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0xD27C3D80]
signed int __fastcall exp_SceSysconForDriver_ksceSysconUpdaterCalcChecksum(unsigned __int8 *a1, unsigned int a2, int *a3)
{
  bool v3; // zf
  unsigned int v4; // r4
  unsigned int v5; // r3
  signed int v6; // r5
  int v7; // r12
  unsigned int v8; // lr
  int v9; // r8
  uint8x8_t *v10; // r4
  int32x4_t v11; // q8
  int v12; // r6
  unsigned int v13; // r7
  uint16x8_t v14; // q12
  uint16x8_t v15; // q11
  uint16x8_t v16; // q10
  uint16x8_t v17; // q9
  uint8x8_t v18; // d18
  uint8x8_t v19; // d19
  uint16x8_t v20; // q10
  uint16x8_t v21; // q9
  int32x2_t v22; // d16
  int v23; // r4
  unsigned int v24; // r5
  int v25; // r4
  bool v26; // cf
  bool v27; // zf
  signed int result; // r0

  v3 = a2 == 0;
  if ( a2 )
    v3 = a1 == 0;
  if ( !v3 && a3 )
  {
    v4 = -((unsigned __int8)a1 & 7) & 0xF;
    if ( v4 >= a2 )
      v4 = a2;
    if ( a2 > 0x11 )
    {
      v5 = v4;
      v6 = v4;
      if ( !v4 )
      {
        v7 = a2;
        v8 = a2 >> 4;
        v9 = 16 * (a2 >> 4);
        if ( !(a2 >> 4) )
          goto LABEL_34;
        goto LABEL_29;
      }
    }
    else
    {
      v4 = a2;
    }
    v5 = *a1;
    if ( v4 <= 1 )
    {
      v6 = 1;
    }
    else
    {
      v5 += a1[1];
      if ( v4 <= 2 )
      {
        v6 = 2;
      }
      else
      {
        v5 += a1[2];
        if ( v4 <= 3 )
        {
          v6 = 3;
        }
        else
        {
          v5 += a1[3];
          if ( v4 <= 4 )
          {
            v6 = 4;
          }
          else
          {
            v5 += a1[4];
            if ( v4 <= 5 )
            {
              v6 = 5;
            }
            else
            {
              v5 += a1[5];
              if ( v4 <= 6 )
              {
                v6 = 6;
              }
              else
              {
                v5 += a1[6];
                if ( v4 <= 7 )
                {
                  v6 = 7;
                }
                else
                {
                  v5 += a1[7];
                  if ( v4 <= 8 )
                  {
                    v6 = 8;
                  }
                  else
                  {
                    v5 += a1[8];
                    if ( v4 <= 9 )
                    {
                      v6 = 9;
                    }
                    else
                    {
                      v5 += a1[9];
                      if ( v4 <= 0xA )
                      {
                        v6 = 10;
                      }
                      else
                      {
                        v5 += a1[10];
                        if ( v4 <= 0xB )
                        {
                          v6 = 11;
                        }
                        else
                        {
                          v5 += a1[11];
                          if ( v4 <= 0xC )
                          {
                            v6 = 12;
                          }
                          else
                          {
                            v5 += a1[12];
                            if ( v4 <= 0xD )
                            {
                              v6 = 13;
                            }
                            else
                            {
                              v5 += a1[13];
                              if ( v4 <= 0xE )
                              {
                                v6 = 14;
                              }
                              else
                              {
                                v5 += a1[14];
                                if ( v4 <= 0xF )
                                {
                                  v6 = 15;
                                }
                                else
                                {
                                  v5 += a1[15];
                                  if ( v4 <= 0x10 )
                                  {
                                    v6 = 16;
                                  }
                                  else
                                  {
                                    v6 = 17;
                                    v5 += a1[16];
                                  }
                                }
                              }
                            }
                          }
                        }
                      }
                    }
                  }
                }
              }
            }
          }
        }
      }
    }
    if ( a2 == v4 )
      goto LABEL_51;
    v7 = a2 - v4;
    v8 = (a2 - v4) >> 4;
    v9 = 16 * v8;
    if ( !v8 )
      goto LABEL_34;
LABEL_29:
    v10 = (uint8x8_t *)&a1[v4];
    if ( v8 <= 2 )
    {
      v11.n128_u64[0] = 0LL;
      v11.n128_u64[1] = 0LL;
      v13 = 0;
    }
    else
    {
      v11.n128_u64[0] = 0LL;
      v11.n128_u64[1] = 0LL;
      v12 = (int)&v10[4];
      v13 = 0;
      do
      {
        v14 = vmovl_u8(*(uint8x8_t *)(v12 - 32));
        v15 = vmovl_u8(*(uint8x8_t *)(v12 - 24));
        v16 = vmovl_u8(*(uint8x8_t *)(v12 - 16));
        v13 += 2;
        v10 = (uint8x8_t *)v12;
        v17 = vmovl_u8(*(uint8x8_t *)(v12 - 8));
        __pld((void *)(v12 + 192));
        v12 += 32;
        v11 = vaddq_s32(
                vmovl_u16(*(uint16x4_t *)&v17.n128_i8[8]),
                vaddq_s32(
                  vmovl_u16((uint16x4_t)v17.n128_u64[0]),
                  vaddq_s32(
                    vmovl_u16(*(uint16x4_t *)&v16.n128_i8[8]),
                    vaddq_s32(
                      vaddq_s32(
                        vmovl_u16(*(uint16x4_t *)&v15.n128_i8[8]),
                        vaddq_s32(
                          vmovl_u16((uint16x4_t)v15.n128_u64[0]),
                          vaddq_s32(
                            vmovl_u16(*(uint16x4_t *)&v14.n128_i8[8]),
                            vaddq_s32(vmovl_u16((uint16x4_t)v14.n128_u64[0]), v11)))),
                      vmovl_u16((uint16x4_t)v16.n128_u64[0])))));
      }
      while ( v13 != ((v8 - 3) & 0xFFFFFFFE) + 2 );
    }
    do
    {
      v18.n64_u64[0] = v10->n64_u64[0];
      v19.n64_u64[0] = v10[1].n64_u64[0];
      v10 += 2;
      v20 = vmovl_u8(v18);
      v21 = vmovl_u8(v19);
      ++v13;
      v11 = vaddq_s32(
              vmovl_u16(*(uint16x4_t *)&v21.n128_i8[8]),
              vaddq_s32(
                vmovl_u16((uint16x4_t)v21.n128_u64[0]),
                vaddq_s32(
                  vmovl_u16(*(uint16x4_t *)&v20.n128_i8[8]),
                  vaddq_s32(vmovl_u16((uint16x4_t)v20.n128_u64[0]), v11))));
    }
    while ( v8 > v13 );
    v22.n64_u64[0] = vadd_s32((int32x2_t)v11.n128_u64[0], *(int32x2_t *)&v11.n128_i8[8]).n64_u64[0];
    v6 += v9;
    v5 += vpadd_s32(v22, v22).n64_u64[0];
    if ( v7 != v9 )
    {
LABEL_34:
      v5 += a1[v6];
      if ( a2 > v6 + 1 )
      {
        v5 += a1[v6 + 1];
        if ( a2 > v6 + 2 )
        {
          v5 += a1[v6 + 2];
          if ( a2 > v6 + 3 )
          {
            v5 += a1[v6 + 3];
            if ( a2 > v6 + 4 )
            {
              v5 += a1[v6 + 4];
              if ( a2 > v6 + 5 )
              {
                v5 += a1[v6 + 5];
                if ( a2 > v6 + 6 )
                {
                  v5 += a1[v6 + 6];
                  if ( a2 > v6 + 7 )
                  {
                    v5 += a1[v6 + 7];
                    if ( a2 > v6 + 8 )
                    {
                      v5 += a1[v6 + 8];
                      if ( a2 > v6 + 9 )
                      {
                        v5 += a1[v6 + 9];
                        if ( a2 > v6 + 10 )
                        {
                          v5 += a1[v6 + 10];
                          if ( a2 > v6 + 11 )
                          {
                            v5 += a1[v6 + 11];
                            if ( a2 > v6 + 12 )
                            {
                              v23 = v6 + 13;
                              v5 += a1[v6 + 12];
                              if ( a2 > v6 + 13 )
                              {
                                v24 = v6 + 14;
                                v25 = a1[v23];
                                v26 = a2 >= v24;
                                v27 = a2 == v24;
                                if ( a2 > v24 )
                                  a2 = a1[v24];
                                v5 += v25;
                                if ( !v27 && v26 )
                                  v5 += a2;
                              }
                            }
                          }
                        }
                      }
                    }
                  }
                }
              }
            }
          }
        }
      }
    }
LABEL_51:
    result = 0;
    *a3 = ~v5;
    return result;
  }
  return -2145058815;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0xCE48E8EB]
signed int __fastcall exp_SceSysconForDriver_0xCE48E8EB(_WORD *a1)
{
  _WORD *v1; // r4
  signed int result; // r0
  __int16 v3; // r7
  __int16 v4; // r2
  __int16 v5; // r1
  __int16 v6; // r3
  int v7; // r6
  int v8; // [sp+0h] [bp-A0h]
  char v9; // [sp+10h] [bp-90h]
  char v10; // [sp+11h] [bp-8Fh]
  char v11; // [sp+12h] [bp-8Eh]
  unsigned __int16 v12; // [sp+34h] [bp-6Ch]
  unsigned __int8 v13; // [sp+36h] [bp-6Ah]
  unsigned __int8 v14; // [sp+37h] [bp-69h]
  unsigned __int8 v15; // [sp+38h] [bp-68h]
  unsigned __int8 v16; // [sp+39h] [bp-67h]
  unsigned __int8 v17; // [sp+3Ah] [bp-66h]
  unsigned __int8 v18; // [sp+3Bh] [bp-65h]
  int v19; // [sp+84h] [bp-1Ch]

  v1 = a1;
  v19 = MEMORY[0];
  if ( a1 )
  {
    v9 = 0;
    v10 = 19;
    v11 = 1;
    result = exp_SceSysconForDriver_ksceSysconCmdExec((int)&v8, 0);
    if ( result >= 0 )
    {
      result = 0;
      v3 = v15;
      v4 = v16;
      v5 = v17;
      v6 = v18;
      v7 = v14 | (v13 << 8);
      *v1 = _byteswap_ushort(v12);
      v1[1] = v7;
      v1[2] = v4 | (v3 << 8);
      v1[3] = v6 | (v5 << 8);
    }
  }
  else
  {
    result = -2145058814;
  }
  if ( v19 != MEMORY[0] )
    imp_SceSysclibForDriver___stack_chk_fail(result);
  return result;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0xA6F05153]
signed int __fastcall exp_SceSysconForDriver_0xA6F05153(__int16 a1, int a2, int a3)
{
  int v3; // r5
  int v4; // r6
  signed int result; // r0
  int v6; // [sp+0h] [bp-A0h]
  char v7; // [sp+10h] [bp-90h]
  char v8; // [sp+11h] [bp-8Fh]
  char v9; // [sp+12h] [bp-8Eh]
  __int16 v10; // [sp+13h] [bp-8Dh]
  char v11; // [sp+15h] [bp-8Bh]
  char v12; // [sp+34h] [bp-6Ch]
  int v13; // [sp+84h] [bp-1Ch]

  v3 = a3;
  v4 = a2;
  v13 = MEMORY[0];
  if ( (unsigned int)(a3 - 1) > 0x17 )
  {
    result = -2145058815;
  }
  else
  {
    v10 = a1;
    v11 = a3;
    v7 = -128;
    v8 = 19;
    v9 = 4;
    result = exp_SceSysconForDriver_ksceSysconCmdExec((int)&v6, 0);
    if ( result >= 0 )
    {
      imp_SceSysclibForDriver_memcpy(v4, &v12, v3);
      result = 0;
    }
  }
  if ( v13 != MEMORY[0] )
    imp_SceSysclibForDriver___stack_chk_fail(result);
  return result;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x95C16911]
int __fastcall exp_SceSysconForDriver_0x95C16911(__int16 a1, int a2, int a3)
{
  signed int v3; // r0
  int result; // r0
  int v5; // [sp+0h] [bp-A0h]
  char v6; // [sp+10h] [bp-90h]
  char v7; // [sp+11h] [bp-8Fh]
  char v8; // [sp+12h] [bp-8Eh]
  __int16 v9; // [sp+13h] [bp-8Dh]
  char v10; // [sp+15h] [bp-8Bh]
  char v11; // [sp+16h] [bp-8Ah]
  int v12; // [sp+84h] [bp-1Ch]

  v12 = MEMORY[0];
  if ( (unsigned int)(a3 - 1) > 0x17 )
  {
    result = -2145058815;
  }
  else
  {
    v9 = a1;
    v10 = a3;
    v8 = a3 + 4;
    v6 = -127;
    v7 = 19;
    imp_SceSysclibForDriver_memcpy(&v11, a2, a3);
    v3 = exp_SceSysconForDriver_ksceSysconCmdExec((int)&v5, 0);
    result = v3 & (v3 >> 31);
  }
  if ( v12 != MEMORY[0] )
    imp_SceSysclibForDriver___stack_chk_fail(result);
  return result;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0xA4968B8C]
int exp_SceSysconForDriver_ksceSysconBeginConfigstorageTransaction()
{
  return sub_810030C8(0, 0x1382u, 1u);
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0xFCC3E8EE]
int exp_SceSysconForDriver_ksceSysconEndConfigstorageTransaction()
{
  return sub_810030C8(0, 0x1383u, 1u);
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x7B9B3617]
int exp_SceSysconForDriver_ksceSysconCommitConfigstorageTransaction()
{
  return sub_810030C8(0, 0x1384u, 1u);
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x89C5CFD6]
int __fastcall exp_SceSysconForDriver_ksceSysconLoadConfigstorageScript(__int16 a1, int a2, int a3)
{
  int v3; // r5
  __int16 v4; // r6
  int v5; // r8
  signed int v6; // r0
  int result; // r0
  char v8; // [sp+4h] [bp-124h]
  int v9; // [sp+8h] [bp-120h]
  char *v10; // [sp+54h] [bp-D4h]
  char v11; // [sp+84h] [bp-A4h]
  char v12; // [sp+85h] [bp-A3h]
  char v13; // [sp+86h] [bp-A2h]
  __int16 v14; // [sp+87h] [bp-A1h]
  char v15; // [sp+89h] [bp-9Fh]
  char v16; // [sp+8Ah] [bp-9Eh]
  int v17; // [sp+104h] [bp-24h]

  v3 = a3;
  v4 = a1;
  v5 = a2;
  v17 = MEMORY[0];
  if ( (unsigned int)(a3 - 1) > 0x3F )
  {
    result = -2145058815;
  }
  else
  {
    imp_SceSysclibForDriver_memset(&v11, 0, 128);
    v14 = v4;
    v15 = v3;
    v13 = v3 + 4;
    v11 = -123;
    v10 = &v11;
    v9 = 2048;
    v12 = 19;
    imp_SceSysclibForDriver_memcpy(&v16, v5, v3);
    v6 = exp_SceSysconForDriver_ksceSysconCmdExec((int)&v8, 0x800u);
    result = v6 & (v6 >> 31);
  }
  if ( v17 != MEMORY[0] )
    imp_SceSysclibForDriver___stack_chk_fail(result);
  return result;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0xCC6F90A8]
int __fastcall exp_SceSysconForDriver_ksceSysconVerifyConfigstorageScript(__int16 a1, int a2, int a3)
{
  int v3; // r5
  __int16 v4; // r6
  int v5; // r8
  signed int v6; // r0
  int result; // r0
  char v8; // [sp+4h] [bp-124h]
  int v9; // [sp+8h] [bp-120h]
  char *v10; // [sp+54h] [bp-D4h]
  char v11; // [sp+84h] [bp-A4h]
  char v12; // [sp+85h] [bp-A3h]
  char v13; // [sp+86h] [bp-A2h]
  __int16 v14; // [sp+87h] [bp-A1h]
  char v15; // [sp+89h] [bp-9Fh]
  char v16; // [sp+8Ah] [bp-9Eh]
  int v17; // [sp+104h] [bp-24h]

  v3 = a3;
  v4 = a1;
  v5 = a2;
  v17 = MEMORY[0];
  if ( (unsigned int)(a3 - 1) > 0x3F )
  {
    result = -2145058815;
  }
  else
  {
    imp_SceSysclibForDriver_memset(&v11, 0, 128);
    v14 = v4;
    v15 = v3;
    v13 = v3 + 4;
    v11 = -122;
    v10 = &v11;
    v9 = 2048;
    v12 = 19;
    imp_SceSysclibForDriver_memcpy(&v16, v5, v3);
    v6 = exp_SceSysconForDriver_ksceSysconCmdExec((int)&v8, 0x800u);
    result = v6 & (v6 >> 31);
  }
  if ( v17 != MEMORY[0] )
    imp_SceSysclibForDriver___stack_chk_fail(result);
  return result;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x351946B0]
signed int __fastcall exp_SceSysconForDriver_0x351946B0(_DWORD *a1)
{
  return sub_81002D20(a1, 0x1310u);
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x76506A57]
signed int __fastcall exp_SceSysconForDriver_0x76506A57(char a1, unsigned int a2, int a3, int a4)
{
  __int16 v4; // r5
  char v5; // r10
  int v6; // r9
  int v7; // r6
  __int64 v8; // r7
  signed int result; // r0
  char v10; // [sp+4h] [bp-12Ch]
  int v11; // [sp+8h] [bp-128h]
  char v12; // [sp+14h] [bp-11Ch]
  char v13; // [sp+15h] [bp-11Bh]
  char v14; // [sp+16h] [bp-11Ah]
  char v15; // [sp+17h] [bp-119h]
  __int16 v16; // [sp+18h] [bp-118h]
  char v17; // [sp+1Ah] [bp-116h]
  __int64 v18; // [sp+58h] [bp-D8h]
  char v19; // [sp+84h] [bp-ACh]
  int v20; // [sp+104h] [bp-2Ch]

  v4 = a2;
  v5 = a1;
  v6 = a3;
  v7 = a4;
  v20 = MEMORY[0];
  if ( a2 >= 0x3F0 || (unsigned int)(a4 - 1) > 0x3F )
  {
    result = -2145058815;
  }
  else
  {
    LODWORD(v8) = &v19;
    HIDWORD(v8) = 128;
    imp_SceSysclibForDriver_memset(&v19, 0, 128);
    v18 = v8;
    v15 = v5;
    v16 = v4;
    v17 = v7;
    v12 = -112;
    v13 = 19;
    v14 = 5;
    v11 = 1024;
    result = exp_SceSysconForDriver_ksceSysconCmdExec((int)&v10, 0x400u);
    if ( result >= 0 )
    {
      imp_SceSysclibForDriver_memcpy(v6, &v19, v7);
      result = 0;
    }
  }
  if ( v20 != MEMORY[0] )
    imp_SceSysclibForDriver___stack_chk_fail(result);
  return result;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0xF9584DC9]
int __fastcall exp_SceSysconForDriver_0xF9584DC9(char a1, __int16 a2, int a3, int a4)
{
  int v4; // r5
  char v5; // r9
  __int16 v6; // r6
  int v7; // r8
  signed int v8; // r0
  int result; // r0
  char v10; // [sp+4h] [bp-124h]
  int v11; // [sp+8h] [bp-120h]
  char *v12; // [sp+54h] [bp-D4h]
  char v13; // [sp+84h] [bp-A4h]
  char v14; // [sp+85h] [bp-A3h]
  char v15; // [sp+86h] [bp-A2h]
  char v16; // [sp+87h] [bp-A1h]
  __int16 v17; // [sp+88h] [bp-A0h]
  char v18; // [sp+8Ah] [bp-9Eh]
  char v19; // [sp+8Bh] [bp-9Dh]
  int v20; // [sp+104h] [bp-24h]

  v4 = a4;
  v5 = a1;
  v6 = a2;
  v7 = a3;
  v20 = MEMORY[0];
  if ( (unsigned int)(a4 - 1) > 0x3F )
  {
    result = -2145058815;
  }
  else
  {
    imp_SceSysclibForDriver_memset(&v13, 0, 128);
    v16 = v5;
    v18 = v4;
    v13 = -111;
    v14 = 19;
    v17 = v6;
    v12 = &v13;
    v15 = v4 + 5;
    v11 = 2048;
    imp_SceSysclibForDriver_memcpy(&v19, v7, v4);
    v8 = exp_SceSysconForDriver_ksceSysconCmdExec((int)&v10, 0x800u);
    result = v8 & (v8 >> 31);
  }
  if ( v20 != MEMORY[0] )
    imp_SceSysclibForDriver___stack_chk_fail(result);
  return result;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0xFD65FFCB]
int __fastcall exp_SceSysconForDriver_0xFD65FFCB(unsigned int a1, unsigned int a2)
{
  int result; // r0

  if ( a1 > 0xFF || a2 > 2 )
    result = -2145058815;
  else
    result = sub_810030C8(a1 | (a2 << 8), 0x1392u, 3u);
  return result;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x02350352]
int __fastcall exp_SceSysconForDriver_0x02350352(unsigned int a1)
{
  int result; // r0

  if ( a1 > 0xFF )
    result = -2145058815;
  else
    result = sub_810030C8(a1, 0x1393u, 2u);
  return result;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x7DE84CE3]
int __fastcall exp_SceSysconForDriver_0x7DE84CE3(unsigned int a1)
{
  int result; // r0

  if ( a1 > 0xFF )
    result = -2145058815;
  else
    result = sub_810030C8(a1, 0x1394u, 2u);
  return result;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x956D07CB]
int exp_SceSysconForDriver_ksceSysconIduModeSet()
{
  int result; // r0
  int v1; // r1
  int v2; // r4
  int v3; // r1
  int v4; // r0

  result = exp_SceSysconForDriver_ksceSysconBeginConfigstorageTransaction();
  if ( result >= 0 )
  {
    LOWORD(v1) = -27456;
    HIWORD(v1) = (unsigned int)&dword_81009360[88] >> 16;
    v2 = exp_SceSysconForDriver_ksceSysconLoadConfigstorageScript(0, v1, 32);
    if ( v2 < 0
      || (LOWORD(v3) = -27424,
          HIWORD(v3) = (unsigned int)&dword_81009360[96] >> 16,
          v2 = exp_SceSysconForDriver_ksceSysconLoadConfigstorageScript(32, v3, 32),
          v2 < 0)
      || (v2 = exp_SceSysconForDriver_ksceSysconCommitConfigstorageTransaction(), v2 < 0) )
    {
      exp_SceSysconForDriver_ksceSysconEndConfigstorageTransaction();
      result = v2;
    }
    else
    {
      v4 = exp_SceSysconForDriver_ksceSysconEndConfigstorageTransaction();
      result = v4 & (v4 >> 31);
    }
  }
  return result;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x34574496]
int exp_SceSysconForDriver_ksceSysconIduModeClear()
{
  int result; // r0
  int v1; // r1
  int v2; // r4
  int v3; // r1
  int v4; // r0

  result = exp_SceSysconForDriver_ksceSysconBeginConfigstorageTransaction();
  if ( result >= 0 )
  {
    LOWORD(v1) = -27328;
    HIWORD(v1) = (unsigned int)&dword_81009360[120] >> 16;
    v2 = exp_SceSysconForDriver_ksceSysconLoadConfigstorageScript(0, v1, 32);
    if ( v2 < 0
      || (LOWORD(v3) = -27296,
          HIWORD(v3) = (unsigned int)&dword_81009360[128] >> 16,
          v2 = exp_SceSysconForDriver_ksceSysconLoadConfigstorageScript(32, v3, 32),
          v2 < 0)
      || (v2 = exp_SceSysconForDriver_ksceSysconCommitConfigstorageTransaction(), v2 < 0) )
    {
      exp_SceSysconForDriver_ksceSysconEndConfigstorageTransaction();
      result = v2;
    }
    else
    {
      v4 = exp_SceSysconForDriver_ksceSysconEndConfigstorageTransaction();
      result = v4 & (v4 >> 31);
    }
  }
  return result;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x6D65B70F]
int exp_SceSysconForDriver_ksceSysconShowModeSet()
{
  int result; // r0
  int v1; // r1
  int v2; // r4
  int v3; // r1
  int v4; // r0

  result = exp_SceSysconForDriver_ksceSysconBeginConfigstorageTransaction();
  if ( result >= 0 )
  {
    LOWORD(v1) = -27392;
    HIWORD(v1) = (unsigned int)&dword_81009360[104] >> 16;
    v2 = exp_SceSysconForDriver_ksceSysconLoadConfigstorageScript(0, v1, 32);
    if ( v2 < 0
      || (LOWORD(v3) = -27360,
          HIWORD(v3) = (unsigned int)&dword_81009360[112] >> 16,
          v2 = exp_SceSysconForDriver_ksceSysconLoadConfigstorageScript(32, v3, 32),
          v2 < 0)
      || (v2 = exp_SceSysconForDriver_ksceSysconCommitConfigstorageTransaction(), v2 < 0) )
    {
      exp_SceSysconForDriver_ksceSysconEndConfigstorageTransaction();
      result = v2;
    }
    else
    {
      v4 = exp_SceSysconForDriver_ksceSysconEndConfigstorageTransaction();
      result = v4 & (v4 >> 31);
    }
  }
  return result;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x8D7724C0]
int exp_SceSysconForDriver_ksceSysconShowModeClear()
{
  int result; // r0
  int v1; // r1
  int v2; // r4
  int v3; // r1
  int v4; // r0

  result = exp_SceSysconForDriver_ksceSysconBeginConfigstorageTransaction();
  if ( result >= 0 )
  {
    LOWORD(v1) = -27328;
    HIWORD(v1) = (unsigned int)&dword_81009360[120] >> 16;
    v2 = exp_SceSysconForDriver_ksceSysconLoadConfigstorageScript(0, v1, 32);
    if ( v2 < 0
      || (LOWORD(v3) = -27296,
          HIWORD(v3) = (unsigned int)&dword_81009360[128] >> 16,
          v2 = exp_SceSysconForDriver_ksceSysconLoadConfigstorageScript(32, v3, 32),
          v2 < 0)
      || (v2 = exp_SceSysconForDriver_ksceSysconCommitConfigstorageTransaction(), v2 < 0) )
    {
      exp_SceSysconForDriver_ksceSysconEndConfigstorageTransaction();
      result = v2;
    }
    else
    {
      v4 = exp_SceSysconForDriver_ksceSysconEndConfigstorageTransaction();
      result = v4 & (v4 >> 31);
    }
  }
  return result;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x4D03754A]
signed int __fastcall exp_SceSysconForDriver_syscon_update_command_0xD00002(int *a1, int a2, _DWORD *a3, int a4)
{
  int *v4; // r4
  int v5; // r1
  _DWORD *v6; // r10
  int v7; // r7
  char *v8; // r9
  char *v9; // r5
  int v10; // lr
  int v11; // r0
  int v12; // r1
  int v13; // r2
  int v14; // r3
  int v15; // r2
  signed int v16; // r0
  signed int v17; // lr
  signed int v18; // r3
  _DWORD *v19; // r5
  int v20; // r0
  int v21; // r1
  int v22; // r2
  int v23; // r3
  int v24; // r1
  signed int result; // r0
  char v26; // [sp+4h] [bp-1A4h]
  int v27; // [sp+8h] [bp-1A0h]
  char v28; // [sp+14h] [bp-194h]
  char v29; // [sp+15h] [bp-193h]
  char v30; // [sp+16h] [bp-192h]
  char *v31; // [sp+54h] [bp-154h]
  char *v32; // [sp+58h] [bp-150h]
  int v33; // [sp+5Ch] [bp-14Ch]
  char v34; // [sp+84h] [bp-124h]
  char v35; // [sp+86h] [bp-122h]
  char v36; // [sp+87h] [bp-121h]
  char v37; // [sp+104h] [bp-A4h]
  char v38; // [sp+124h] [bp-84h]
  int v39; // [sp+184h] [bp-24h]

  v4 = a1;
  v5 = a2 != 40;
  if ( !a1 )
    v5 |= 1u;
  v6 = a3;
  v39 = MEMORY[0];
  if ( v5 )
    goto LABEL_22;
  v7 = a4 - 40;
  if ( a4 != 40 )
    v7 = 1;
  if ( !a3 )
    v7 |= 1u;
  if ( v7 )
  {
LABEL_22:
    v18 = 0x80250001;
  }
  else
  {
    v8 = &v37;
    v9 = &v36;
    imp_SceSysclibForDriver_memset(&v34, 0, 128);
    imp_SceSysclibForDriver_memset(&v37, 0, 128);
    v10 = (int)(v4 + 8);
    v31 = &v34;
    v29 = 0;
    v32 = &v37;
    v27 = 3072;
    v33 = 46;
    v28 = -48;
    v34 = -48;
    v35 = 41;
    v30 = 41;
    do
    {
      v11 = *v4;
      v4 += 4;
      v12 = *(v4 - 3);
      v9 += 16;
      v13 = *(v4 - 2);
      v14 = *(v4 - 1);
      *((_DWORD *)v9 - 4) = v11;
      *((_DWORD *)v9 - 3) = v12;
      *((_DWORD *)v9 - 2) = v13;
      *((_DWORD *)v9 - 1) = v14;
    }
    while ( v4 != (int *)v10 );
    v15 = *v4;
    *((_DWORD *)v9 + 1) = v4[1];
    *(_DWORD *)v9 = v15;
    v16 = exp_SceSysconForDriver_ksceSysconCmdExec((int)&v26, 0xC00u);
    v17 = v16;
    if ( v16 >= 0 )
    {
      v19 = v6;
      do
      {
        v19 += 4;
        v20 = *(_DWORD *)v8;
        v21 = *((_DWORD *)v8 + 1);
        v22 = *((_DWORD *)v8 + 2);
        v23 = *((_DWORD *)v8 + 3);
        v8 += 16;
        *(v19 - 4) = v20;
        *(v19 - 3) = v21;
        *(v19 - 2) = v22;
        *(v19 - 1) = v23;
      }
      while ( v8 != &v38 );
      v24 = *((_DWORD *)v8 + 1);
      v18 = v17;
      *v19 = *(_DWORD *)v8;
      v19[1] = v24;
    }
    else
    {
      v18 = v16;
    }
  }
  result = v18;
  if ( v39 != MEMORY[0] )
    imp_SceSysclibForDriver___stack_chk_fail(v18);
  return result;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0xC14BD637]
signed int exp_SceSysconForDriver_0xC14BD637()
{
  signed int result; // r0
  int v1; // [sp+0h] [bp-98h]
  char v2; // [sp+10h] [bp-88h]
  char v3; // [sp+11h] [bp-87h]
  char v4; // [sp+12h] [bp-86h]
  int v5; // [sp+84h] [bp-14h]

  v3 = 0;
  v4 = 1;
  v5 = MEMORY[0];
  v2 = -47;
  result = exp_SceSysconForDriver_ksceSysconCmdExec((int)&v1, 0);
  if ( v5 != MEMORY[0] )
    imp_SceSysclibForDriver___stack_chk_fail(result);
  return result;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0xEBDF88B9]
// local variable allocation has failed, the output may be wrong!
signed int __fastcall exp_SceSysconForDriver_snvs_read(int *a1, int a2, int a3, int a4)
{
  int *v4; // r4
  int v5; // r1
  int v6; // r9
  int v7; // r6
  char *v8; // r7
  char *v9; // r8 OVERLAPPED
  int v10; // r10
  int v11; // r12
  int v12; // r2
  int v13; // r4
  signed int v14; // r7
  int v15; // r6
  int v16; // r0
  int v17; // r1
  int v18; // r2
  int v19; // r3
  signed int result; // r0
  char v21; // [sp+4h] [bp-1ACh]
  int v22; // [sp+8h] [bp-1A8h]
  char v23; // [sp+14h] [bp-19Ch]
  char v24; // [sp+15h] [bp-19Bh]
  char v25; // [sp+16h] [bp-19Ah]
  __int64 v26; // [sp+54h] [bp-15Ch]
  int v27; // [sp+5Ch] [bp-154h]
  char v28; // [sp+84h] [bp-12Ch]
  char v29; // [sp+86h] [bp-12Ah]
  int v30; // [sp+87h] [bp-129h]
  int v31; // [sp+8Bh] [bp-125h]
  int v32; // [sp+8Fh] [bp-121h]
  int v33; // [sp+93h] [bp-11Dh]
  char v34; // [sp+104h] [bp-ACh]
  char v35; // [sp+134h] [bp-7Ch]
  int v36; // [sp+184h] [bp-2Ch]

  v4 = a1;
  v5 = a2 != 16;
  if ( !a1 )
    v5 |= 1u;
  v6 = a3;
  v36 = MEMORY[0];
  if ( v5 )
    goto LABEL_19;
  v7 = a4 - 48;
  if ( a4 != 48 )
    v7 = 1;
  if ( !a3 )
    v7 |= 1u;
  if ( v7 )
  {
LABEL_19:
    result = 0x80250001;
  }
  else
  {
    v8 = &v28;
    v9 = &v34;
    imp_SceSysclibForDriver_memset(&v28, 0, 128);
    imp_SceSysclibForDriver_memset(&v34, 0, 128);
    v10 = *v4;
    v11 = v4[1];
    v12 = v4[2];
    v13 = v4[3];
    v24 = 0;
    v28 = -46;
    v29 = 17;
    v26 = *(_QWORD *)(&v9 - 1);
    v27 = 54;
    v22 = 3072;
    v23 = -46;
    v25 = 17;
    v30 = v10;
    v31 = v11;
    v32 = v12;
    v33 = v13;
    v14 = exp_SceSysconForDriver_ksceSysconCmdExec((int)&v21, 0xC00u);
    if ( v14 >= 0 )
    {
      v15 = v6;
      do
      {
        v15 += 16;
        v16 = *(_DWORD *)v9;
        v17 = *((_DWORD *)v9 + 1);
        v18 = *((_DWORD *)v9 + 2);
        v19 = *((_DWORD *)v9 + 3);
        v9 += 16;
        *(_DWORD *)(v15 - 16) = v16;
        *(_DWORD *)(v15 - 12) = v17;
        *(_DWORD *)(v15 - 8) = v18;
        *(_DWORD *)(v15 - 4) = v19;
      }
      while ( v9 != &v35 );
    }
    result = v14;
  }
  if ( v36 != MEMORY[0] )
    imp_SceSysclibForDriver___stack_chk_fail(result);
  return result;
}

// [Export libnid: 0x60A35F64 (SceSysconForDriver), NID: 0x63683B9B]
signed int __fastcall exp_SceSysconForDriver_snvs_write(int *a1, int a2, _DWORD *a3, int a4)
{
  int *v4; // r4
  int v5; // r1
  _DWORD *v6; // r7
  int v7; // r8
  char *v8; // r5
  int v9; // r0
  int v10; // lr
  int v11; // r1
  int v12; // r2
  int v13; // r3
  signed int v14; // r0
  signed int v15; // r5
  int v16; // r1
  int v17; // r2
  int v18; // r3
  signed int result; // r0
  char v20; // [sp+4h] [bp-124h]
  int v21; // [sp+8h] [bp-120h]
  char v22; // [sp+14h] [bp-114h]
  char v23; // [sp+15h] [bp-113h]
  char v24; // [sp+16h] [bp-112h]
  int v25; // [sp+38h] [bp-F0h]
  int v26; // [sp+3Ch] [bp-ECh]
  int v27; // [sp+40h] [bp-E8h]
  int v28; // [sp+44h] [bp-E4h]
  char *v29; // [sp+54h] [bp-D4h]
  char v30; // [sp+84h] [bp-A4h]
  char v31; // [sp+86h] [bp-A2h]
  char v32; // [sp+87h] [bp-A1h]
  int v33; // [sp+104h] [bp-24h]

  v4 = a1;
  v5 = a2 != 48;
  if ( !a1 )
    v5 |= 1u;
  v6 = a3;
  v33 = MEMORY[0];
  if ( v5 )
    goto LABEL_20;
  v7 = a4 - 16;
  if ( a4 != 16 )
    v7 = 1;
  if ( !a3 )
    v7 |= 1u;
  if ( v7 )
  {
LABEL_20:
    v15 = 0x80250001;
  }
  else
  {
    imp_SceSysclibForDriver_memset(&v30, 0, 128);
    v8 = &v32;
    v9 = (int)(v4 + 12);
    v29 = &v30;
    v23 = 0;
    v21 = 2048;
    v22 = 0xD3u;
    v30 = 0xD3u;
    v24 = 49;
    v31 = 49;
    do
    {
      v10 = *v4;
      v4 += 4;
      v11 = *(v4 - 3);
      v8 += 16;
      v12 = *(v4 - 2);
      v13 = *(v4 - 1);
      *((_DWORD *)v8 - 4) = v10;
      *((_DWORD *)v8 - 3) = v11;
      *((_DWORD *)v8 - 2) = v12;
      *((_DWORD *)v8 - 1) = v13;
    }
    while ( v4 != (int *)v9 );
    v14 = exp_SceSysconForDriver_ksceSysconCmdExec((int)&v20, 0x800u);
    if ( v14 >= 0 )
    {
      v15 = v14;
      v16 = v26;
      v17 = v27;
      v18 = v28;
      *v6 = v25;
      v6[1] = v16;
      v6[2] = v17;
      v6[3] = v18;
    }
    else
    {
      v15 = v14;
    }
  }
  result = v15;
  if ( v33 != MEMORY[0] )
    imp_SceSysclibForDriver___stack_chk_fail(v15);
  return result;
}

#pragma endregion "Command Wrappers"





#pragma region ".data"

//.data:8100C000                 AREA .data, DATA, ALIGN=0
//.data:8100C000                 ; ORG 0x8100C000
//.data:8100C000 ; int dword_8100C000[]
//.data:8100C000 dword_8100C000  % 4                     ; DATA XREF: sub_8100003C+4↑o
//.data:8100C000                                         ; sub_810002FC+36↑o ...
//.data:8100C004 dword_8100C004  % 4                     ; DATA XREF: sub_81001934+176↑r
//.data:8100C004                                         ; exp.SceSysconForDriver.ksceSysconCmdSync+D2↑r
//.data:8100C008 dword_8100C008  % 4                     ; DATA XREF: sub_81001934+17E↑r
//.data:8100C008                                         ; sub_81001934:loc_81001B14↑r ...
//.data:8100C00C dword_8100C00C  % 4                     ; DATA XREF: sub_81001934+184↑r
//.data:8100C00C                                         ; sub_81001934+1EE↑r ...
//.data:8100C010 dword_8100C010  % 4                     ; DATA XREF: sub_81001934+18A↑r
//.data:8100C010                                         ; sub_81001934+1F2↑r ...
//.data:8100C014 dword_8100C014  % 4                     ; DATA XREF: sub_81001934+190↑r
//.data:8100C014                                         ; sub_81001934+1F6↑r ...
//.data:8100C018 dword_8100C018  % 4                     ; DATA XREF: sub_81001934+196↑r
//.data:8100C018                                         ; sub_81001934+1FA↑r ...
//.data:8100C01C dword_8100C01C  % 4                     ; DATA XREF: sub_81001934+19C↑r
//.data:8100C01C                                         ; sub_81001934+1FE↑r ...
//.data:8100C020 dword_8100C020  % 4                     ; DATA XREF: sub_81001934+1A2↑r
//.data:8100C020                                         ; sub_81001934+202↑r ...
//.data:8100C024 dword_8100C024  % 4                     ; DATA XREF: sub_81001934+1A8↑r
//.data:8100C024                                         ; sub_81001934+206↑r ...
//.data:8100C028 dword_8100C028  % 4                     ; DATA XREF: sub_81001934+1AE↑r
//.data:8100C028                                         ; sub_81001934+20A↑r ...
//.data:8100C02C dword_8100C02C  % 4                     ; DATA XREF: sub_81001934+1B4↑r
//.data:8100C02C                                         ; sub_81001934+20E↑r ...
//.data:8100C030 dword_8100C030  % 4                     ; DATA XREF: sub_81001934+1BA↑r
//.data:8100C030                                         ; sub_81001934+212↑r ...
//.data:8100C034 dword_8100C034  % 4                     ; DATA XREF: sub_81001934+1C0↑r
//.data:8100C034                                         ; sub_81001934+220↑r ...
//.data:8100C038 dword_8100C038  % 4                     ; DATA XREF: sub_81001934+1C6↑r
//.data:8100C038                                         ; sub_81001934+224↑r ...
//.data:8100C03C dword_8100C03C  % 4                     ; DATA XREF: sub_81001934+1CC↑r
//.data:8100C03C                                         ; sub_81001934+228↑r ...
//.data:8100C040 dword_8100C040  % 4                     ; DATA XREF: sub_81001934+1D2↑r
//.data:8100C040                                         ; sub_81001934+22C↑r ...
//.data:8100C044 dword_8100C044  % 4                     ; DATA XREF: sub_81001934+1D8↑r
//.data:8100C044                                         ; sub_81001934+230↑r ...
//.data:8100C048 dword_8100C048  % 0x48                  ; 0
//.data:8100C048                                         ; DATA XREF: sub_8100064C+7B0↑r
//.data:8100C048                                         ; sub_8100064C:loc_81000E2A↑r ...
//.data:8100C090 dword_8100C090  % 4                     ; DATA XREF: sub_8100064C+7C4↑w
//.data:8100C090                                         ; sub_8100064C+CCC↑w ...
//.data:8100C094 dword_8100C094  % 4                     ; DATA XREF: sub_8100003C+18↑r
//.data:8100C094                                         ; sub_8100064C:loc_81000772↑r ...
//.data:8100C098 dword_8100C098  % 4                     ; DATA XREF: sub_8100003C+10↑r
//.data:8100C098                                         ; sub_8100064C+5C↑r ...
//.data:8100C09C dword_8100C09C  % 4                     ; DATA XREF: sub_8100064C+40↑w
//.data:8100C09C                                         ; sub_81001934+CE↑w ...
//.data:8100C0A0 dword_8100C0A0  % 4                     ; DATA XREF: exp.SceSysconForDriver.ksceSysconCmdExecAsync+3D8↑r
//.data:8100C0A0                                         ; exp.SceSysconForDriver.ksceSysconCmdSync+28↑r ...
//.data:8100C0A4 dword_8100C0A4  % 4                     ; DATA XREF: sub_8100064C+83E↑r
//.data:8100C0A4                                         ; exp.SceSysconForDriver.ksceSysconCmdExecAsync:loc_81002476↑r ...
//.data:8100C0A8 dword_8100C0A8  % 4                     ; DATA XREF: sub_810002FC+28E↑w
//.data:8100C0A8                                         ; sub_810002FC:loc_81000590↑r ...
//.data:8100C0AC dword_8100C0AC  % 4                     ; DATA XREF: sub_8100064C+9E2↑o
//.data:8100C0AC                                         ; sub_8100064C+9E6↑w ...
//.data:8100C0B0 dword_8100C0B0  % 4                     ; DATA XREF: sub_8100064C+8AA↑o
//.data:8100C0B0                                         ; sub_8100064C+8AE↑w ...
//.data:8100C0B4 dword_8100C0B4  % 4                     ; DATA XREF: sub_81001B80+11C↑w
//.data:8100C0B4                                         ; exp.SceSysconForDriver.0x2E4A2198+8↑r ...
//.data:8100C0B8 dword_8100C0B8  % 4                     ; DATA XREF: sub_81001B80+100↑w
//.data:8100C0B8                                         ; exp.SceSysconForDriver.0x1E08A40F+8↑r ...
//.data:8100C0BC byte_8100C0BC   % 1                     ; DATA XREF: sub_810002FC:loc_81000544↑r
//.data:8100C0BC                                         ; sub_8100064C+3FC↑r ...
//.data:8100C0BD byte_8100C0BD   % 1                     ; DATA XREF: sub_8100064C+3F8↑w
//.data:8100C0BE byte_8100C0BE   % 1                     ; DATA XREF: sub_8100064C+20↑w
//.data:8100C0BE                                         ; sub_8100064C+872↑w ...
//.data:8100C0BF byte_8100C0BF   % 1                     ; DATA XREF: exp.SceSysconForDriver.ksceSysconCmdExecAsync+12↑r
//.data:8100C0BF                                         ; exp.SceSysconForDriver.ksceSysconCmdExec:loc_8100278E↑r ...
//.data:8100C0C0 byte_8100C0C0   % 1                     ; DATA XREF: sub_8100064C+920↑r
//.data:8100C0C0                                         ; sub_8100064C+CAC↑w
//.data:8100C0C1 byte_8100C0C1   % 1                     ; DATA XREF: sub_8100064C+436↑r
//.data:8100C0C1                                         ; sub_8100064C+6AE↑r ...
//.data:8100C0C2 byte_8100C0C2   % 1                     ; DATA XREF: sub_8100064C+572↑o
//.data:8100C0C2                                         ; exp.SceSysconForDriver.0xC0F215B7+8↑r ...
//.data:8100C0C3 byte_8100C0C3   % 1                     ; DATA XREF: exp.SceSysconForDriver.0xC3504ADE+8↑r
//.data:8100C0C4 byte_8100C0C4   % 1                     ; DATA XREF: exp.SceSysconForDriver.0xB832B72C+8↑r
//.data:8100C0C5 byte_8100C0C5   % 1                     ; DATA XREF: exp.SceSysconForDriver.0x86BAAF7D+8↑r
//.data:8100C0C6 byte_8100C0C6   % 1                     ; DATA XREF: exp.SceSysconForDriver.0xA57B5433+8↑r
//.data:8100C0C7 byte_8100C0C7   % 1                     ; DATA XREF: sub_810002FC+26E↑w
//.data:8100C0C7                                         ; sub_810002FC+2D4↑w ...
//.data:8100C0C8 byte_8100C0C8   % 1                     ; DATA XREF: sub_810002FC+272↑w
//.data:8100C0C8                                         ; sub_810002FC+2D8↑w ...
//.data:8100C0C9 byte_8100C0C9   % 1                     ; DATA XREF: exp.SceSysconForDriver.0x4BC63A40+8↑r
//.data:8100C0CA byte_8100C0CA   % 1                     ; DATA XREF: exp.SceSysconForDriver.0x99A254A9+8↑r
//.data:8100C0CB byte_8100C0CB   % 1                     ; DATA XREF: exp.SceSysconForDriver.0x9A4F4B7C+8↑r
//.data:8100C0CC byte_8100C0CC   % 1                     ; DATA XREF: exp.SceSysconForDriver.0x92D2C6A4+8↑r
//.data:8100C0CD byte_8100C0CD   % 1                     ; DATA XREF: exp.SceSysconForDriver.0xB9EA2FA8+8↑r
//.data:8100C0CE byte_8100C0CE   % 1                     ; DATA XREF: exp.SceSysconForDriver.0xD7F5A797+8↑r
//.data:8100C0CF byte_8100C0CF   % 1                     ; DATA XREF: exp.SceSysconForDriver.0x27758A64+8↑r
//.data:8100C0D0 byte_8100C0D0   % 1                     ; DATA XREF: exp.SceSysconForDriver.0x4A184B7C+8↑r
//.data:8100C0D1 byte_8100C0D1   % 1                     ; DATA XREF: exp.SceSysconForDriver.0xACEE1C70+8↑r
//.data:8100C0D2 byte_8100C0D2   % 1                     ; DATA XREF: exp.SceSysconForDriver.0x03C50DC3+8↑r
//.data:8100C0D3 byte_8100C0D3   % 1                     ; DATA XREF: exp.SceSysconForDriver.0xBFDA5590+8↑r
//.data:8100C0D4 byte_8100C0D4   % 1                     ; DATA XREF: exp.SceSysconForDriver.0x63B14156+8↑r
//.data:8100C0D5 byte_8100C0D5   % 1                     ; DATA XREF: exp.SceSysconForDriver.0x50CAE242+8↑r
//.data:8100C0D6 byte_8100C0D6   % 1                     ; DATA XREF: exp.SceSysconForDriver.0x29CF4335+8↑r
//.data:8100C0D7 byte_8100C0D7   % 1                     ; DATA XREF: exp.SceSysconForDriver.0x9F4042F8+8↑r
//.data:8100C0D8 byte_8100C0D8   % 1                     ; DATA XREF: exp.SceSysconForDriver.0xC50568E9+8↑r
//.data:8100C0D9 byte_8100C0D9   % 1                     ; DATA XREF: exp.SceSysconForDriver.ksceSysconIsLowBatteryInhibitUpdateReboot+8↑r
//.data:8100C0DA byte_8100C0DA   % 1                     ; DATA XREF: exp.SceSysconForDriver.ksceSysconIsLowBatteryInhibitUpdateDownload+8↑r
//.data:8100C0DB byte_8100C0DB   % 1                     ; DATA XREF: sub_8100064C:loc_8100110A↑r
//.data:8100C0DB                                         ; sub_8100064C+AEC↑w ...
//.data:8100C0DC byte_8100C0DC   % 1                     ; DATA XREF: sub_8100064C+ACC↑r
//.data:8100C0DC                                         ; sub_8100064C+B0A↑w ...
//.data:8100C0DD byte_8100C0DD   % 1                     ; DATA XREF: sub_8100064C+AF6↑r
//.data:8100C0DD                                         ; sub_8100064C+B32↑w ...
//.data:8100C0DE byte_8100C0DE   % 1                     ; DATA XREF: sub_8100064C+B2A↑r
//.data:8100C0DE                                         ; sub_8100064C+B60↑w ...
//.data:8100C0DF byte_8100C0DF   % 1                     ; DATA XREF: sub_8100064C+5E8↑r
//.data:8100C0DF                                         ; sub_8100064C+5F8↑w ...
//.data:8100C0E0 byte_8100C0E0   % 0x104                 ; 0
//.data:8100C0E0                                         ; DATA XREF: sub_810002FC+2C8↑w
//.data:8100C0E0                                         ; sub_8100064C:loc_81000DE8↑r ...
//.data:8100C1E4 dword_8100C1E4  % 4                     ; DATA XREF: sub_8100064C:loc_810013EC↑r
//.data:8100C1E8 dword_8100C1E8  % 4                     ; DATA XREF: sub_8100064C+DBE↑r
//.data:8100C1E8                                         ; sub_8100064C:loc_810014F2↑r
//.data:8100C1EC dword_8100C1EC  % 4                     ; DATA XREF: sub_8100064C:loc_810013AA↑r
//.data:8100C1F0 dword_8100C1F0  % 4                     ; DATA XREF: sub_8100064C+D86↑r
//.data:8100C1F0                                         ; sub_8100064C:loc_810014DC↑r
//.data:8100C1F4 dword_8100C1F4  % 4                     ; DATA XREF: sub_8100064C:loc_81001424↑r
//.data:8100C1F8 dword_8100C1F8  % 4                     ; DATA XREF: sub_8100064C+DFE↑r
//.data:8100C1F8                                         ; sub_8100064C:loc_81001508↑r
//.data:8100C1FC dword_8100C1FC  % 4                     ; DATA XREF: sub_8100064C+B80↑r
//.data:8100C200 dword_8100C200  % 4                     ; DATA XREF: sub_8100064C+BA6↑r
//.data:8100C200                                         ; sub_8100064C:loc_81001920↑r
//.data:8100C204 dword_8100C204  % 4                     ; DATA XREF: sub_8100064C:loc_81000A9A↑r
//.data:8100C208 dword_8100C208  % 4                     ; DATA XREF: sub_8100064C+474↑r
//.data:8100C208                                         ; sub_8100064C:loc_81001904↑r
//.data:8100C20C dword_8100C20C  % 4                     ; DATA XREF: sub_8100064C+9D6↑r
//.data:8100C210 dword_8100C210  % 4                     ; DATA XREF: sub_8100064C+A06↑r
//.data:8100C210                                         ; sub_8100064C:loc_81001532↑r
//.data:8100C214 dword_8100C214  % 4                     ; DATA XREF: sub_8100064C+89E↑r
//.data:8100C218 dword_8100C218  % 4                     ; DATA XREF: sub_8100064C+8CE↑r
//.data:8100C218                                         ; sub_8100064C:loc_8100151E↑r
//.data:8100C21C                 % 1
//.data:8100C21D                 % 1
//.data:8100C21E                 % 1
//.data:8100C21F                 % 1
//.data:8100C220                 % 1
//.data:8100C221                 % 1
//.data:8100C222                 % 1
//.data:8100C223                 % 1
//.data:8100C224 dword_8100C224  % 4                     ; DATA XREF: sub_810002FC:loc_8100030C↑o
//.data:8100C224                                         ; sub_810002FC:loc_81000526↑r ...
//.data:8100C228 dword_8100C228  % 4                     ; DATA XREF: sub_810002FC+25A↑r
//.data:8100C228                                         ; sub_81001934+108↑r ...
//.data:8100C22C unk_8100C22C    % 1                     ; DATA XREF: exp.SceSysconForDriver.ksceSysconGetHardwareInfo2+1A↑o
//.data:8100C22D                 % 1
//.data:8100C22E                 % 1
//.data:8100C22F                 % 1
//.data:8100C230                 % 1
//.data:8100C231                 % 1
//.data:8100C232                 % 1
//.data:8100C233                 % 1
//.data:8100C234                 % 1
//.data:8100C235                 % 1
//.data:8100C236                 % 1
//.data:8100C237                 % 1
//.data:8100C238                 % 1
//.data:8100C239                 % 1
//.data:8100C23A                 % 1
//.data:8100C23B                 % 1
//.data:8100C23C byte_8100C23C   % 1                     ; DATA XREF: sub_810002FC:loc_8100031C↑o
//.data:8100C23C                                         ; sub_810002FC+3C↑r
//.data:8100C23D byte_8100C23D   % 1                     ; DATA XREF: sub_810002FC+46↑r
//.data:8100C23E byte_8100C23E   % 1                     ; DATA XREF: sub_810002FC+5C↑r
//.data:8100C23F byte_8100C23F   % 1                     ; DATA XREF: sub_810002FC+72↑r
//.data:8100C240 byte_8100C240   % 1                     ; DATA XREF: sub_810002FC+96↑r
//.data:8100C241 byte_8100C241   % 1                     ; DATA XREF: sub_810002FC+B6↑r
//.data:8100C242 byte_8100C242   % 1                     ; DATA XREF: sub_810002FC+D6↑r
//.data:8100C243 byte_8100C243   % 1                     ; DATA XREF: sub_810002FC+F6↑r
//.data:8100C244 byte_8100C244   % 1                     ; DATA XREF: sub_810002FC+116↑r
//.data:8100C245 byte_8100C245   % 1                     ; DATA XREF: sub_810002FC+138↑r
//.data:8100C246 byte_8100C246   % 1                     ; DATA XREF: sub_810002FC+164↑r
//.data:8100C247 byte_8100C247   % 1                     ; DATA XREF: sub_810002FC+188↑r
//.data:8100C248 byte_8100C248   % 1                     ; DATA XREF: sub_810002FC+1AC↑r
//.data:8100C249 byte_8100C249   % 1                     ; DATA XREF: sub_810002FC+1D0↑r
//.data:8100C24A byte_8100C24A   % 1                     ; DATA XREF: sub_810002FC+1F4↑r
//.data:8100C24B                 % 1
//.data:8100C24C                 % 1
//.data:8100C24D                 % 1
//.data:8100C24E                 % 1
//.data:8100C24F                 % 1
//.data:8100C250 unk_8100C250    % 1
//.data:8100C251                 % 1
//.data:8100C252                 % 1
//.data:8100C253                 % 1
//.data:8100C254                 % 1
//.data:8100C255                 % 1
//.data:8100C256                 % 1
//.data:8100C257                 % 1
//.data:8100C258 dword_8100C258  % 4                     ; DATA XREF: sub_8100064C+424↑r
//.data:8100C258                                         ; sub_8100064C:loc_81000C90↑r ...
//.data:8100C25C dword_8100C25C  % 4                     ; DATA XREF: sub_8100064C+E52↑r
//.data:8100C25C                                         ; sub_81001B80+114↑w ...
//.data:8100C260 unk_8100C260    % 1                     ; DATA XREF: sub_8100000C+2↑o
//.data:8100C260                                         ; sub_8100064C+12↑o ...
//.data:8100C261                 % 1
//.data:8100C262                 % 1
//.data:8100C263                 % 1
//.data:8100C264 dword_8100C264  % 4                     ; DATA XREF: sub_81001B80+180↑w
//.data:8100C268 dword_8100C268  % 4                     ; DATA XREF: sub_81001B80+1E6↑w
//.data:8100C268                                         ; exp.SceSysconForDriver.ksceSysconJigOpenPort+18↑r ...
//.data:8100C26C dword_8100C26C  % 4                     ; DATA XREF: exp.SceSysconForDriver.ksceSysconJigOpenPort+28↑r
//.data:8100C26C                                         ; exp.SceSysconForDriver.ksceSysconJigOpenPort+54↑r ...
//.data:8100C270 dword_8100C270  % 4                     ; DATA XREF: sub_810002FC+2CE↑w
//.data:8100C270                                         ; exp.SceSysconForDriver.ksceSysconWaitInitialized+A↑r ...
//.data:8100C274                 ALIGN 8
//.data:8100C278 dword_8100C278  % 4                     ; DATA XREF: sub_8100003C+8C↑o
//.data:8100C278                                         ; sub_8100003C+94↑r ...
//.data:8100C27C dword_8100C27C  % 4                     ; DATA XREF: sub_8100064C+7B6↑o
//.data:8100C27C                                         ; sub_8100064C+96C↑o ...
//.data:8100C280                 % 1
//.data:8100C281                 % 1
//.data:8100C282                 % 1
//.data:8100C283                 % 1
//.data:8100C284 dword_8100C284  % 4                     ; DATA XREF: sub_81001B80+F8↑w
//.data:8100C288 dword_8100C288  % 4                     ; DATA XREF: sub_81001B80+E8↑w
//.data:8100C28C byte_8100C28C   % 1                     ; DATA XREF: sub_81001B80+106↑w
//.data:8100C28D byte_8100C28D   % 1                     ; DATA XREF: sub_81001B80+10A↑w
//.data:8100C28E byte_8100C28E   % 1                     ; DATA XREF: sub_81001B80+10E↑w
//.data:8100C28F byte_8100C28F   % 0x6D                  ; 0
//.data:8100C28F                                         ; DATA XREF: sub_81001B80+104↑w
//.data:8100C2FC dword_8100C2FC  % 4                     ; DATA XREF: sub_8100064C+814↑o
//.data:8100C2FC                                         ; sub_8100064C+826↑w ...
//.data:8100C300                 % 1
//.data:8100C301                 % 1
//.data:8100C302                 % 1
//.data:8100C303                 % 1
//.data:8100C304 dword_8100C304  % 4                     ; DATA XREF: sub_81001B80+9C↑w
//.data:8100C308 dword_8100C308  % 4                     ; DATA XREF: sub_81001B80+94↑w
//.data:8100C30C byte_8100C30C   % 1                     ; DATA XREF: sub_81001B80+B0↑w
//.data:8100C30D                 ALIGN 2
//.data:8100C30E byte_8100C30E   % 1                     ; DATA XREF: sub_81001B80+AC↑w
//.data:8100C30F byte_8100C30F   % 0x6D                  ; 0
//.data:8100C30F                                         ; DATA XREF: sub_81001B80+A8↑w
//.data:8100C37C dword_8100C37C  % 0x80                  ; 0
//.data:8100C37C                                         ; DATA XREF: sub_8100064C:loc_81000FEC↑o
//.data:8100C37C                                         ; sub_8100064C+9AA↑w ...
//.data:8100C3FC dword_8100C3FC  % 4                     ; DATA XREF: sub_8100064C+7E0↑o
//.data:8100C3FC                                         ; sub_8100064C+7F6↑w ...
//.data:8100C400                 % 1
//.data:8100C401                 % 1
//.data:8100C402                 % 1
//.data:8100C403                 % 1
//.data:8100C404 dword_8100C404  % 4                     ; DATA XREF: sub_81001B80+D0↑w
//.data:8100C408 dword_8100C408  % 4                     ; DATA XREF: sub_81001B80+C6↑w
//.data:8100C40C byte_8100C40C   % 1                     ; DATA XREF: sub_81001B80+D2↑w
//.data:8100C40D byte_8100C40D   % 1                     ; DATA XREF: sub_81001B80+D4↑w
//.data:8100C40E byte_8100C40E   % 1                     ; DATA XREF: sub_81001B80+D8↑w
//.data:8100C40F byte_8100C40F   % 0x6D                  ; 0
//.data:8100C40F                                         ; DATA XREF: sub_81001B80+C2↑w
//.data:8100C40F ; .data         ends

#pragma endregion ".data"
