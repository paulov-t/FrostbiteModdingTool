using FrostyEditor.IO;
using FrostySdk;

namespace v2k4FIFASDKGenerator.FIFA21
{
	public class TypeInfo : BaseInfo.TypeInfo
	{
		private uint nameHash;

		public override void Read(MemoryReader reader)
		{
			name = reader.ReadNullTerminatedString();
            if (name.ToLower().Contains("gp_goalkeeper_cpu_difficulty".ToLower()))
            {

            }
			nameHash = reader.ReadUInt();
			flags = reader.ReadUShort();
			flags >>= 1;
            size = reader.ReadUInt();
            reader.Position -= 4L;
            size = reader.ReadUShort();

			guid = reader.ReadGuid();

			long position = reader.ReadLong();
			var unk1 = reader.ReadLong();

			alignment = reader.ReadUShort();
			fieldCount = reader.ReadUShort();

			padding3 = reader.ReadUInt();
			if(padding3 != 0)
            {

            }
			else
            {

            }
			long[] array = new long[7];
			for (int i = 0; i < 7; i++)
			{
				array[i] = reader.ReadLong();
			}
			reader.Position = position;
			nameSpace = reader.ReadNullTerminatedString();
			bool flag = false;
			parentClass = array[0];
			if (base.Type == 2)
			{
				reader.Position = array[6];
				flag = true;
			}
			else if (base.Type == 3)
			{
				reader.Position = array[1];
				//reader.Position = array[3];
				//reader.Position = array[4];
				//if(reader.Position == 0)                
				//reader.Position = array[3];

				flag = true;
			}
			else if (base.Type == 8)
			{
				parentClass = 0L;
				reader.Position = array[0];
				flag = true;
			}
			if (flag)
			{
				for (int j = 0; j < fieldCount; j++)
				{
					FieldInfo fieldInfo = new FieldInfo();
					fieldInfo.Read(reader);
					fieldInfo.index = j;
					fields.Add(fieldInfo);
				}
			}
		}

		public override void Modify(DbObject classObj)
		{
			classObj.SetValue("nameHash", nameHash);
		}
	}
}
