using FMT.FileTools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace FMT.Sound.SampleBank
{
    public record SampleBankDataSet
    {
        public int Id { get; init; }

        public int SampleGroupId { get; init; }

        public uint DataOffset { get; init; }

        public List<SampleBankField> Fields { get; init; }

        public List<SampleBankField> IndexColumns { get; init; }

        public List<SoundBankDataSetIndex> Indexes { get; init; }

        public int NumElems
        {
            get
            {
                return numElems;
            }
            set
            {
                numElems = value;
            }
        }

        private int numElems;

        public SampleBankDataSet(int Id, int SampleGroupId, uint DataOffset, int NumElems, List<SampleBankField> Fields, List<SampleBankField> IndexColumns, List<SoundBankDataSetIndex> Indexes)
        {
            this.Id = Id;
            this.SampleGroupId = SampleGroupId;
            this.DataOffset = DataOffset;
            this.Fields = Fields;
            this.IndexColumns = IndexColumns;
            this.Indexes = Indexes;
            numElems = NumElems;
        }

        public SampleBankField Get(int key)
        {
            return Fields.FirstOrDefault((SampleBankField f) => f.Id == key) ?? IndexColumns.FirstOrDefault((SampleBankField f) => f.Id == key);
        }

        public SampleBankField Get(string name)
        {
            int nameHash = Fnv1a.HashString(name);
            return Get(nameHash);
        }

        [CompilerGenerated]
        protected virtual bool PrintMembers(StringBuilder builder)
        {
            RuntimeHelpers.EnsureSufficientExecutionStack();
            builder.Append("Id = ");
            builder.Append(Id.ToString());
            builder.Append(", SampleGroupId = ");
            builder.Append(SampleGroupId.ToString());
            builder.Append(", DataOffset = ");
            builder.Append(DataOffset.ToString());
            builder.Append(", Fields = ");
            builder.Append(Fields);
            builder.Append(", IndexColumns = ");
            builder.Append(IndexColumns);
            builder.Append(", Indexes = ");
            builder.Append(Indexes);
            builder.Append(", NumElems = ");
            builder.Append(NumElems.ToString());
            return true;
        }

        [CompilerGenerated]
        public void Deconstruct(out int Id, out int SampleGroupId, out uint DataOffset, out int NumElems, out List<SampleBankField> Fields, out List<SampleBankField> IndexColumns, out List<SoundBankDataSetIndex> Indexes)
        {
            Id = this.Id;
            SampleGroupId = this.SampleGroupId;
            DataOffset = this.DataOffset;
            NumElems = this.NumElems;
            Fields = this.Fields;
            IndexColumns = this.IndexColumns;
            Indexes = this.Indexes;
        }
    }

    public record SoundBankDataSetIndex(List<int> ColumnKeys);
}
