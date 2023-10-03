using FMT.FileTools;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FMT.Sound.SampleBank
{
    public record SampleBankField(Endian Endian, int Id, SampleBankFieldType DataType, SampleBankDataSetColumnFormat OriginalFormat, uint TableOffset, uint NextReferenceOffset, ObservableCollection<object> Values);
}
