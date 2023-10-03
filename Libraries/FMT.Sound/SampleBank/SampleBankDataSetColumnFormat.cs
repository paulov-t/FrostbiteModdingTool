using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FMT.Sound.SampleBank
{
    public enum SampleBankDataSetColumnFormat : byte
    {
        Constant,
        IndexFormula,
        ShiftedBase,
        LookupTable,
        Raw
    }
}
