﻿using System;
using System.Threading;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace OSCSGO
{
    public class CSharpMemoryManager
    {
        public string processName;
        public string moduleName;
        public Process process;
        public IntPtr handle;

        public CSharpMemoryManager(string processName, string moduleName)
        {
            this.processName = processName;
            this.moduleName = moduleName;
        }

        /*
        * ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
        * ------------------------------------------------------------- WinAPI --------------------------------------------------------------------
        *  ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
        */

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, [Out] byte[] lpbuffer, int dwSize, out IntPtr lpNumberOfBytesReas);

        [DllImport("kernel32.dll")]
        static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int dwSize, out IntPtr lpNumberOfBytesWritten);

        /*
        *  ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
        * ---------------------------------------------------------- Capture Process ---------------------------------------------------------------
        *  ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
        */
        //returns the list of all processes with the given name

        public IntPtr GetBaseModule()
        {
            //attempt to get the process
            try
            {
                Process process = GetProcessByName(this.processName)[0];
                this.process = process;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            
            //get the desired module
            IntPtr baseAddress = IntPtr.Zero;
            foreach (ProcessModule module in process.Modules)
            {
                if (module.ModuleName.Contains(this.moduleName))
                {
                    baseAddress = module.BaseAddress;
                    break;
                }
            }

            if (baseAddress == IntPtr.Zero)
            {
                Console.WriteLine("module name not found");
                return IntPtr.Zero;
            }

            //open the handle to the process module
            this.handle = GetHandle(this.process);

            return baseAddress;
        }

        private Process[] GetProcessByName(string processName)
        {
            Process[] myProcess = null;

            //attempt to capture process by name
            try
            {
                myProcess = Process.GetProcessesByName(processName);
                Console.WriteLine("Process successfully captured for: " + processName);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.StackTrace);
                Console.WriteLine("Unable to capture process");
            }

            return myProcess;
        }

        public IntPtr GetHandle(Process capturedProcess)
        {
            IntPtr processHandle = IntPtr.Zero;
            int PROCESS_ALL_ACCESS = (0x1F0FFF);

            if (capturedProcess == null)
            {
                Console.WriteLine("Cannot get handle for null process");
                return processHandle;
            }

            //attempt to aquire the handle of the captured process
            try
            {
                processHandle = OpenProcess(PROCESS_ALL_ACCESS, true, capturedProcess.Id);
                Console.WriteLine("Handle for process acquired");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.StackTrace);
                Console.WriteLine("Failed to acquire handle for process");
            }

            return processHandle;
        }


        /*
         * ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
         * ------------------------------------------------------------- Read Memory --------------------------------------------------------------------
         *  ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
         */

        byte[] ReadMemory(IntPtr address, int size)
        {
            //Prepare buffer and pointer
            byte[] dataBuffer = new byte[size];
            IntPtr bytesRead = IntPtr.Zero;

            //Read
            ReadProcessMemory(this.handle, address, dataBuffer, dataBuffer.Length, out bytesRead);

            if (bytesRead == IntPtr.Zero)
            {
                Console.WriteLine("Failed to read Bytes");
                return null;
            }

            return dataBuffer;
        }

        int ReadInt32(IntPtr address)
        {
            byte[] data;

            data = ReadMemory(address, 4);

            if (data == null)
            {
                Console.WriteLine("Failed to read int, returning default value 0");
                return 0;
            }

            return BitConverter.ToInt32(data, 0);
        }

        /*
         * ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
         * -------------------------------------------------------- Write Memory --------------------------------------------------------------------
         *  ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
         */

        bool Write(int address, int value)
        {
            byte[] dataBuffer = BitConverter.GetBytes(value);
            IntPtr bytesWritten = IntPtr.Zero;

            WriteProcessMemory(handle, (IntPtr)address, dataBuffer, dataBuffer.Length, out bytesWritten);

            if (bytesWritten == IntPtr.Zero)
            {
                Console.WriteLine("Nothing was written...");
                return false;
            }
            if (bytesWritten.ToInt32() < dataBuffer.Length)
            {
                Console.WriteLine("We wrote {0} out of {1} bytes...", bytesWritten.ToInt32(), dataBuffer.Length.ToString());
                return false;
            }
            return true;
        }

        static void Main(string[] args)
        {

            string processName = "csgo";
            string moduleName = "client_panorama.dll";

            //**********************Change on updates*******************************

            //Client
            int dwLocalPlayer =  0xCF5A4C;
            int oHealth = 0x100;

            //**********************************************************************

            //get the memory manager
            CSharpMemoryManager manager = new CSharpMemoryManager(processName, moduleName);

            //get the base address
            IntPtr baseAddress = manager.GetBaseModule();

            Console.WriteLine(moduleName + " base address: " + baseAddress);

            //offset to local player memory location
            int localPlayerAddress = manager.ReadInt32(IntPtr.Add(baseAddress, dwLocalPlayer));

            //print the new memory location
            Console.WriteLine("Local player memory address: " + localPlayerAddress);

            IntPtr healthPointer = IntPtr.Add((IntPtr)localPlayerAddress, oHealth);

            //read the player health
            int health = manager.ReadInt32((IntPtr) healthPointer);
            Console.WriteLine("Health: " + health);


            Thread.Sleep(5000);
            
        }
    }
}
