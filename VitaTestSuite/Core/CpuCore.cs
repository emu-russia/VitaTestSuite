// Common CPU core abstraction for VitaTestSuite.
//
// Every architecture interpreter (ARM Cortex-A9 MPCore, Toshiba MeP-c5 / CMeP,
// Renesas RL78) derives from CpuCore and implements Step()/Reset().
//
// This file (and everything in Core/) must stay free of WinForms dependencies so
// that the cores can be compiled and unit-tested standalone.

using System;
using System.Collections.Generic;
using System.Globalization;

namespace VitaTestSuite.Core
{
    public enum CpuArch
    {
        Unknown = 0,
        Arm,        // ARM Cortex-A9 MPCore (Kermit main CPU, ARMv7-A + TrustZone)
        MeP,        // Toshiba MeP-c5 : CMeP ("F00D") and Venezia MPE
        Rl78        // Renesas RL78 : ErnIE syscon
    }

    /// <summary>
    /// Log sink used by cores, memory and devices. Implemented by the GUI Report
    /// and by the headless console log.
    /// </summary>
    public interface ILogSink
    {
        void Log(string category, string text);
        bool IsEnabled(string category);
    }

    /// <summary>One named register value, for the GUI register view.</summary>
    public class RegValue
    {
        public string Group;
        public string Name;
        public uint Value;
        public string Note;

        public RegValue(string group, string name, uint value, string note = null)
        {
            Group = group; Name = name; Value = value; Note = note;
        }

        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "{0,-6} = {1:X8}", Name, Value);
        }
    }

    /// <summary>Result of one Step(): what happened, for tracing in the GUI.</summary>
    public class StepResult
    {
        public uint Address;
        public int Length;
        public string Text = "";
        public bool Faulted;
        public string Fault = "";
    }

    public abstract class CpuCore
    {
        /// <summary>Human readable core name, e.g. "Cortex-A9 MPCore #0".</summary>
        public string Name = "cpu";

        public abstract CpuArch Arch { get; }

        /// <summary>Memory (RAM + MMIO) seen by this core.</summary>
        public MemoryHub Mem;

        /// <summary>Optional log sink.</summary>
        public ILogSink Log;

        public ulong InstructionCount;
        public ulong CycleCount;

        public bool Halted;
        public string HaltReason = "";

        /// <summary>Current program counter (also stored in Mem.CurrentPC while running).</summary>
        public uint Pc;

        /// <summary>Set to true when the core stops because it executed an undefined instruction.</summary>
        public bool UndefinedInstruction;

        protected CpuCore(MemoryHub mem, ILogSink log)
        {
            Mem = mem;
            Log = log;
        }

        public void Emit(string category, string text)
        {
            if (Log != null) Log.Log(category, text);
        }

        protected void Fault(string reason)
        {
            Halted = true;
            HaltReason = reason;
            Emit("cpu", "[" + Name + "] HALT: " + reason);
        }

        /// <summary>Reset the core: sets PC to the reset vector, clears internal state.</summary>
        public abstract void Reset();

        /// <summary>Reset to an explicit entry point (overrides the reset vector).</summary>
        public virtual void Reset(uint entryPoint)
        {
            Reset();
            Pc = entryPoint;
        }

        /// <summary>Execute exactly one instruction. Must never throw for a well-formed image.</summary>
        public abstract StepResult Step();

        /// <summary>Disassemble one instruction at <paramref name="address"/>.</summary>
        public abstract string Disassemble(uint address, out int length);

        /// <summary>Fill the register list shown in the GUI.</summary>
        public abstract void GetRegisters(List<RegValue> regs);

        /// <summary>Named register write used by the command processor ("setreg pc 0x..." )</summary>
        public virtual bool SetRegister(string name, uint value) { return false; }

        /// <summary>Named register read used by the command processor ("getreg pc")</summary>
        public virtual bool GetRegister(string name, out uint value) { value = 0; return false; }

        /// <summary>Extra state shown in the GUI status line (mode, banking, flags...).</summary>
        public virtual string StatusLine { get { return ""; } }

        /// <summary>
        /// Run up to <paramref name="maxSteps"/> instructions. Stops early when
        /// <paramref name="abort"/> returns true, when the core halts or on a fault.
        /// Returns the number of instructions executed.
        /// </summary>
        public int Run(int maxSteps, Func<bool> abort)
        {
            int done = 0;

            while (done < maxSteps)
            {
                if (Halted) break;
                if (abort != null && abort()) break;

                Step();
                done++;

                if (UndefinedInstruction) break;
            }

            return done;
        }

        /// <summary>Breakpoint addresses (the GUI/command processor own the policy).</summary>
        public HashSet<uint> Breakpoints = new HashSet<uint>();

        protected bool BreakpointHit(uint address)
        {
            return Breakpoints.Count != 0 && Breakpoints.Contains(address);
        }
    }
}
