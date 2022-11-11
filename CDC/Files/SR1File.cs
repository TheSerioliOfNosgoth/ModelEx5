using System;
using System.IO;
using System.Collections.Generic;
using CDC.Objects.Models;
using TPages = BenLincoln.TheLostWorlds.CDTextures.PSXTextureDictionary;

namespace CDC.Objects
{
	public class SR1File : DataFile
	{
		public const UInt32 PROTO_19981025_VERSION = 0x00000000;
		public const UInt32 ALPHA_19990123_VERSION_1_X = 0x3c204127;
		public const UInt32 ALPHA_19990123_VERSION_1 = 0x3c204128;
		public const UInt32 ALPHA_19990204_VERSION_2 = 0x3c204129;
		public const UInt32 ALPHA_19990216_VERSION_3 = 0x3c204131;
		public const UInt32 BETA_19990512_VERSION = 0x3c204139;
		public const UInt32 RETAIL_VERSION = 0x3C20413B;

		// ushort tPageMask = 0x001F (x, 0-31) | 0x0010 (y1, 0-1) | 0x0800 (y2, 0-2) | 0x0180 (tp, 0-3)| 0x0060 (abr, 0-3)
		// ushort clutMask = 0x003F (x, 0-63) | 0xFFC0 (y, 0-1023)
		protected TPages _tPages = new TPages(0x001F | 0x0010 | 0x0800, 0x003F | 0xFFC0);
		public TPages TPages { get { return _tPages; } }

		public MonsterAttributes _monsterAttributes;

		public SR1File(String dataFileName, Platform platform, ExportOptions options)
			: base(dataFileName, Game.SR1, platform, options)
		{
		}

		protected override void ReadObjectData(BinaryReader reader, ExportOptions options)
		{
			// Object name
			reader.BaseStream.Position = _dataStart + 0x00000024;
			reader.BaseStream.Position = _dataStart + reader.ReadUInt32();
			String strModelName = new String(reader.ReadChars(8));
			_name = Utility.CleanObjectName(strModelName);

			// Hack to check for lighthouse demo.
			// The only way the name can be at 0x0000003C is if the Level structure is smaller. Hence it's the demo.
			reader.BaseStream.Position = _dataStart + 0x00000024;
			if (reader.ReadUInt32() == 0x0000003C)
			{
				_version = PROTO_19981025_VERSION;
			}
			else
			{
				// Assume retail for now. There might be other checks needed.
				_version = RETAIL_VERSION;
			}

			// Texture type
			if (_platform == Platform.None)
			{
				reader.BaseStream.Position = _dataStart + 0x44;
				if (_version == PROTO_19981025_VERSION || reader.ReadUInt64() != 0xFFFFFFFFFFFFFFFF)
				{
					_platform = Platform.PSX;
				}
				else
				{
					_platform = Platform.PC;
				}
			}

			// Model data
			reader.BaseStream.Position = _dataStart + 0x00000008;
			_modelCount = reader.ReadUInt16();
			_animCount = reader.ReadUInt16();
			_modelStart = _dataStart + reader.ReadUInt32();
			_animStart = _dataStart + reader.ReadUInt32();

			_models = new SR1Model[_modelCount];
			Platform ePlatform = _platform;

			for (UInt16 m = 0; m < _modelCount; m++)
			{
				Console.WriteLine(string.Format("Debug: reading object model {0} / {1}", m, (_modelCount - 1)));

				long modelPointer = _modelStart + (m * 4);
				if (modelPointer < 0 || modelPointer > reader.BaseStream.Length)
				{
					Console.WriteLine(string.Format("Error: attempt to read a model with index {0} from a stream with length {1}", m, reader.BaseStream.Length));
					continue;
				}

				reader.BaseStream.Position = modelPointer;
				uint modelData = _dataStart + reader.ReadUInt32();
				reader.BaseStream.Position = _modelStart;

				SR1ObjectModel model = new SR1ObjectModel(reader, _dataStart, modelData, _name, _platform, _version, _tPages);
				model.ReadData(reader, options);
				_models[m] = model;

				if ((_platform == Platform.None) && (_models[m].Platform == Platform.Dreamcast))
				{
					ePlatform = _models[m].Platform;
				}
			}

			if (_platform == Platform.None)
			{
				_platform = ePlatform;
			}

			/*reader.BaseStream.Position = _dataStart + 0x1C;
			UInt32 objectDataStart = reader.ReadUInt32();
			reader.BaseStream.Position = _dataStart + 0x2C;
			Int32 oflags2 = reader.ReadInt32();

			// MonsterAttributes
			if ((oflags2 & 0x00080000) != 0 && objectDataStart != 0)
			{
				_monsterAttributes = new MonsterAttributes();
				
				reader.BaseStream.Position = objectDataStart + 0x24;
				if (Version <= BETA_19990512_VERSION)
				{
					reader.BaseStream.Position += 0x03;
				}
				if (Version == BETA_19990512_VERSION)
				{
					reader.BaseStream.Position += 0x01;
				}

				_monsterAttributes.numSubAttributes = reader.ReadSByte();
				_monsterAttributes.subAttributes = new MonsterSubAttributes[_monsterAttributes.numSubAttributes];
				reader.BaseStream.Position += 0x07;

				for (int s = 0; s < _monsterAttributes.numSubAttributes; s++)
				{
					_monsterAttributes.subAttributes[s].dataStart = reader.ReadUInt32();
				}

				for (int s = 0; s < _monsterAttributes.numSubAttributes; s++)
				{
					reader.BaseStream.Position = _monsterAttributes.subAttributes[s].dataStart + 0x26;
					_monsterAttributes.subAttributes[s].modelNum = reader.ReadByte();
				}
			}*/
		}

		protected override void ReadUnitData(BinaryReader reader, ExportOptions options)
		{
			bool validVersion = false;

			if (!validVersion)
			{
				reader.BaseStream.Position = _dataStart + 0xF0;
				_version = reader.ReadUInt32();
				if (_version == RETAIL_VERSION)
				{
					validVersion = true;
				}
				else if (_version == BETA_19990512_VERSION)
				{
					validVersion = true;
				}
			}

			if (!validVersion)
			{
				reader.BaseStream.Position = _dataStart + 0xE4;
				_version = reader.ReadUInt32();
				if (_version == ALPHA_19990216_VERSION_3)
				{
					validVersion = true;
				}
			}

			if (!validVersion)
			{
				reader.BaseStream.Position = _dataStart + 0xE0;
				_version = reader.ReadUInt32();
				if (_version == ALPHA_19990204_VERSION_2)
				{
					validVersion = true;
				}
				if (_version == ALPHA_19990123_VERSION_1_X ||
					_version == ALPHA_19990123_VERSION_1)
				{
					validVersion = true;
				}
			}

			if (!validVersion)
			{
				// Lighthouse
				_version = PROTO_19981025_VERSION;
				validVersion = true;
			}

			if (!validVersion)
			{
				throw new Exception("Wrong version number for level x");
			}

			// Adjacent units are seperate from portals.
			// There can be multiple portals to the same unit.
			// Portals
			reader.BaseStream.Position = _dataStart;
			UInt32 m_uConnectionData = _dataStart + reader.ReadUInt32(); // Same as m_uModelData?

			if (_version == PROTO_19981025_VERSION)
			{
				reader.BaseStream.Position = m_uConnectionData + 0x3C;
			}
			else
			{
				reader.BaseStream.Position = m_uConnectionData + 0x30;
			}

			reader.BaseStream.Position = _dataStart + reader.ReadUInt32();
			portalCount = reader.ReadUInt32();
			_portalNames = new String[portalCount];
			for (int i = 0; i < portalCount; i++)
			{
				String strUnitName = new String(reader.ReadChars(12));
				_portalNames[i] = Utility.CleanName(strUnitName);

				if (_version == PROTO_19981025_VERSION)
				{
					reader.BaseStream.Position += 0x4C;
				}
				else
				{
					reader.BaseStream.Position += 0x50;
				}
			}

			// Intros
			if (_version == PROTO_19981025_VERSION)
			{
				reader.BaseStream.Position = _dataStart + 0x84;
			}
			else if (_version == ALPHA_19990123_VERSION_1_X ||
				_version == ALPHA_19990123_VERSION_1 ||
				_version == ALPHA_19990204_VERSION_2)
			{
				reader.BaseStream.Position = _dataStart + 0x70;
			}
			else if (_version == ALPHA_19990216_VERSION_3)
			{
				reader.BaseStream.Position = _dataStart + 0x74;
			}
			else
			{
				reader.BaseStream.Position = _dataStart + 0x78;
			}

			// Intros
			bool hasRaziel = false;
			_introCount = reader.ReadUInt32();
			_introStart = _dataStart + reader.ReadUInt32();
			_intros = new Intro[_introCount];
			for (int i = 0; i < _introCount; i++)
			{
				if (_version == PROTO_19981025_VERSION)
				{
					reader.BaseStream.Position = _introStart + 0x48 * i;
				}
				else if (_version == ALPHA_19990123_VERSION_1_X ||
					_version == ALPHA_19990123_VERSION_1 ||
					_version == ALPHA_19990204_VERSION_2)
				{
					reader.BaseStream.Position = _introStart + 0x50 * i;
				}
				else
				{
					reader.BaseStream.Position = _introStart + 0x4C * i;
				}

				String introName = new String(reader.ReadChars(8));
				reader.BaseStream.Position += 0x08;
				_intros[i].introNum = reader.ReadInt32();
				_intros[i].uniqueID = reader.ReadInt32();
				_intros[i].rotation.x = (float)(Math.PI * 2 / 4096) * reader.ReadInt16();
				_intros[i].rotation.y = (float)(Math.PI * 2 / 4096) * reader.ReadInt16();
				_intros[i].rotation.z = (float)(Math.PI * 2 / 4096) * reader.ReadInt16();
				reader.BaseStream.Position += 0x02;
				_intros[i].position.x = (float)reader.ReadInt16();
				_intros[i].position.y = (float)reader.ReadInt16();
				_intros[i].position.z = (float)reader.ReadInt16();
				_intros[i].name = Utility.CleanObjectName(introName) + "-" + _intros[i].uniqueID;
				_intros[i].fileName = Utility.CleanObjectName(introName);

				if (_intros[i].fileName == "raziel")
				{
					hasRaziel = true;
				}

				reader.BaseStream.Position += 0x0A;
				UInt32 iniCommand = reader.ReadUInt32();
				if (iniCommand != 0)
				{
					reader.BaseStream.Position = iniCommand;

					while (true)
					{
						UInt16 command = reader.ReadUInt16();
						if (command == 0)
						{
							break;
						}

						UInt16 numParameters = reader.ReadUInt16();

						if (command == 6)
						{
							_intros[i].monsterAge = reader.ReadInt32();
						}
						else if (command == 18)
						{
							_intros[i].modelIndex = reader.ReadInt32();
						}

						iniCommand += 4u + (4u * numParameters);
					}
				}
			}

			// Object Names
			if (_version == PROTO_19981025_VERSION)
			{
				reader.BaseStream.Position = _dataStart + 0x98;
			}
			else if (_version == ALPHA_19990123_VERSION_1_X ||
				_version == ALPHA_19990123_VERSION_1 ||
				_version == ALPHA_19990204_VERSION_2)
			{
				reader.BaseStream.Position = _dataStart + 0x84;
			}
			else if (_version == ALPHA_19990216_VERSION_3)
			{
				reader.BaseStream.Position = _dataStart + 0x88;
			}
			else
			{
				reader.BaseStream.Position = _dataStart + 0x8C;
			}
			_objectNameStart = _dataStart + reader.ReadUInt32();
			reader.BaseStream.Position = _objectNameStart;
			List<String> objectNames = new List<String>();
			while (reader.ReadByte() != 0xFF)
			{
				reader.BaseStream.Position--;
				String strObjectName = new String(reader.ReadChars(8));
				objectNames.Add(Utility.CleanObjectName(strObjectName));
				reader.BaseStream.Position += 0x08;
			}

			if (hasRaziel && !objectNames.Contains("raziel"))
			{
				objectNames.Add("raziel");
			}

			_objectNames = objectNames.ToArray();

			// Unit name
			if (_version == PROTO_19981025_VERSION)
			{
				reader.BaseStream.Position = _dataStart + 0xA4;
			}
			else if (_version == ALPHA_19990123_VERSION_1_X ||
				_version == ALPHA_19990123_VERSION_1 ||
				_version == ALPHA_19990204_VERSION_2)
			{
				reader.BaseStream.Position = _dataStart + 0x90;
			}
			else if (_version == ALPHA_19990216_VERSION_3)
			{
				reader.BaseStream.Position = _dataStart + 0x94;
			}
			else
			{
				reader.BaseStream.Position = _dataStart + 0x98;
			}
			reader.BaseStream.Position = _dataStart + reader.ReadUInt32();
			String strModelName = new String(reader.ReadChars(8));
			_name = Utility.CleanName(strModelName);

			// Texture type
			bool handledSpecificVersion = false;
			if (_version == PROTO_19981025_VERSION ||
				_version == ALPHA_19990123_VERSION_1_X ||
				_version == ALPHA_19990123_VERSION_1 ||
				_version == ALPHA_19990204_VERSION_2 ||
				_version == ALPHA_19990216_VERSION_3)
			{
				if (_platform == Platform.None)
				{
					_platform = Platform.PSX;
				}
				handledSpecificVersion = true;
			}

			if (!handledSpecificVersion)
			{
				reader.BaseStream.Position = _dataStart + 0x9C;
				UInt64 checkVal = reader.ReadUInt64();
				if (_platform == Platform.None)
				{
					if (checkVal != 0xFFFFFFFFFFFFFFFF)
					{
						_platform = Platform.PSX;
					}
					else
					{
						_platform = Platform.PC;
					}
				}
			}

			// Connected unit list. (unreferenced?)
			//reader.BaseStream.Position = _dataStart + 0xC0;
			//m_uConnectedUnitsStart = _dataStart + reader.ReadUInt32() + 0x08;
			//reader.BaseStream.Position = m_uConnectedUnitsStart;
			//reader.BaseStream.Position += 0x18;
			//String strUnitName0 = new String(reader.ReadChars(16));
			//strUnitName0 = strUnitName0.Substring(0, strUnitName0.IndexOf(','));
			//reader.BaseStream.Position += 0x18;
			//String strUnitName1 = new String(reader.ReadChars(16));
			//strUnitName1 = strUnitName1.Substring(0, strUnitName1.IndexOf(','));
			//reader.BaseStream.Position += 0x18;
			//String strUnitName2 = new String(reader.ReadChars(16));
			//strUnitName2 = strUnitName2.Substring(0, strUnitName2.IndexOf(','));

			reader.BaseStream.Position = _dataStart;
			_modelCount = 1;
			_modelStart = _dataStart;
			_models = new SR1Model[_modelCount];
			reader.BaseStream.Position = _modelStart;
			uint modelData = _dataStart + reader.ReadUInt32();

			Console.WriteLine("Debug: reading area model 0");

			SR1UnitModel model = new SR1UnitModel(reader, _dataStart, modelData, _name, _platform, _version, _tPages);
			model.ReadData(reader, options);
			_models[0] = model;

			//if (m_axModels[0].Platform == Platform.Dreamcast ||
			//    m_axModels[1].Platform == Platform.Dreamcast)
			//{
			//    _platform = Platform.Dreamcast;
			//}
		}

		protected override void ResolvePointers(BinaryReader reader, BinaryWriter writer)
		{
			UInt32 dataStart = ((reader.ReadUInt32() >> 9) << 11) + 0x00000800;
			if (reader.ReadUInt32() == 0x00000000)
			{
				_asset = Asset.Unit;
			}
			else
			{
				_asset = Asset.Object;
			}

			reader.BaseStream.Position = dataStart;
			writer.BaseStream.Position = 0;

			reader.BaseStream.CopyTo(writer.BaseStream);
		}
	}
}