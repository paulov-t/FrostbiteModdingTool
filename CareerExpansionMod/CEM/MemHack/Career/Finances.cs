﻿using CareerExpansionMod.CEM;
using Memory;
using System;
using System.Collections.Generic;
using System.Text;
using v2k4FIFAModdingCL.MemHack.Core;

namespace v2k4FIFAModdingCL.MemHack.Career
{
    

    public class Finances : IDisposable
    {

        private static class POINTER_ADDRESSES
        {
            /// Will need to redo this for every EA Update
            public static string TRANSFER_BUDGET = "FIFA20.exe+06E30D80,0x10,0x48,0x30,0x58,0x5F8";
            /// Will need to redo this for every EA Update
            public static string STARTING_BUDGET = "FIFA20.exe+06E30098,0x10,0x48,0x30,0x58,0x5E0";
        }

        private static class AOB_ADDRESSES
        {
            // DOESN'T WORK!
            public static string TRANSFER_BUDGET = "?? D9 39 01 70 A3 23 00 AC FC 28 01 78 29 20 69 E9 1A 02 00 8D 16 F4 00";
        }



        public int TransferBudget
        {
            get
            {
                if (CoreHack.GetProcess().HasValue) { 
                    var transferbudget = CoreHack.MemLib.readInt(POINTER_ADDRESSES.TRANSFER_BUDGET);
                    CEMCore.CEMCoreInstance.Finances = this;
                    return transferbudget;
                }
                return -1;
            }
            set
            {

                if (CoreHack.GetProcess().HasValue)
                {
                    CoreHack.MemLib.writeMemory(POINTER_ADDRESSES.TRANSFER_BUDGET, "int", value.ToString());
                    CEMCore.CEMCoreInstance.Finances = this;
                }

            }
        }

        public int StartingBudget
        {
            get
            {
                if (CoreHack.GetProcess().HasValue)
                {
                    var budget = CoreHack.MemLib.readInt(POINTER_ADDRESSES.STARTING_BUDGET);
                    CEMCore.CEMCoreInstance.Finances = this;
                    return budget;
                }
                return -1;
            }
        }

        public bool RequestAdditionalFunds(out string message)
        {
            bool success = false;
            var ratio = ((TransferBudget > 0 ? (double)TransferBudget : 1) / (StartingBudget > 0 ? (double)StartingBudget : 1));
            ratio = Math.Round(ratio, 2) * 100;
            if (ratio < 85)
            {
                message = "Board: OK. We have provided you with some extra funds.";
                SetTransferBudget(TransferBudget + (StartingBudget - TransferBudget));
                success = true;
            }
            else
            {
                message = "<p>Board: Sorry we are unable to provide you with additional funds at this time.<br /> You currently have a ratio of " + ratio.ToString() + "%.</p>";
                success = false;
            }

            CEMCore.CEMCoreInstance.Finances = this;
            return success;
        }

        public static Finances FinanceInstance
        {
            get
            {
                if (CEMCore.CEMCoreInstance.Finances == null)
                    CEMCore.CEMCoreInstance.Finances = new Finances();

                return CEMCore.CEMCoreInstance.Finances;
            }
        }

        public static int GetTransferBudget()
        {
            return FinanceInstance.TransferBudget;
        }

        public static void SetTransferBudget(int newBudget)
        {
            FinanceInstance.TransferBudget = newBudget;
        }

        public void Dispose()
        {
            //~Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~Finances()
        {

        }
    }
}
