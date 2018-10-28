/*<FILE_LICENSE>
 * Azos (A to Z Application Operating System) Framework
 * The A to Z Foundation (a.k.a. Azist) licenses this file to you under the MIT license.
 * See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using MySql.Data.MySqlClient;
using MySqlConnector.Protocol;
using MySqlConnector.Protocol.Payloads;
using MySqlConnector.Protocol.Serialization;
using MySqlConnector.Utilities;

namespace MySqlConnector.Core
{
	internal sealed class TypeMapper
	{
		public static TypeMapper Instance = new TypeMapper();

		private TypeMapper()
		{
			m_columnTypeMetadata = new List<ColumnTypeMetadata>();
			m_dbTypeMappingsByClrType = new Dictionary<Type, DbTypeMapping>();
			m_dbTypeMappingsByDbType = new Dictionary<DbType, DbTypeMapping>();
			m_columnTypeMetadataLookup = new Dictionary<string, ColumnTypeMetadata>(StringComparer.OrdinalIgnoreCase);
			m_mySqlDbTypeToColumnTypeMetadata = new Dictionary<MySqlDbType, ColumnTypeMetadata>();

			// boolean
			var typeBoolean = AddDbTypeMapping(new DbTypeMapping(typeof(bool), new[] { DbType.Boolean }, convert: o => Convert.ToBoolean(o)));
			AddColumnTypeMetadata(new ColumnTypeMetadata("TINYINT", typeBoolean, MySqlDbType.Bool, isUnsigned: false, length: 1, columnSize: 1, simpleDataTypeName: "BOOL", createFormat: "BOOL"));
			AddColumnTypeMetadata(new ColumnTypeMetadata("TINYINT", typeBoolean, MySqlDbType.Bool, isUnsigned: true, length: 1));

			// integers
			var typeSbyte = AddDbTypeMapping(new DbTypeMapping(typeof(sbyte), new[] { DbType.SByte }, convert: o => Convert.ToSByte(o)));
			var typeByte = AddDbTypeMapping(new DbTypeMapping(typeof(byte), new[] { DbType.Byte }, convert: o => Convert.ToByte(o)));
			var typeShort = AddDbTypeMapping(new DbTypeMapping(typeof(short), new[] { DbType.Int16 }, convert: o => Convert.ToInt16(o)));
			var typeUshort = AddDbTypeMapping(new DbTypeMapping(typeof(ushort), new[] { DbType.UInt16 }, convert: o => Convert.ToUInt16(o)));
			var typeInt = AddDbTypeMapping(new DbTypeMapping(typeof(int), new[] { DbType.Int32 }, convert: o => Convert.ToInt32(o)));
			var typeUint = AddDbTypeMapping(new DbTypeMapping(typeof(uint), new[] { DbType.UInt32 }, convert: o => Convert.ToUInt32(o)));
			var typeLong = AddDbTypeMapping(new DbTypeMapping(typeof(long), new[] { DbType.Int64 }, convert: o => Convert.ToInt64(o)));
			var typeUlong = AddDbTypeMapping(new DbTypeMapping(typeof(ulong), new[] { DbType.UInt64 }, convert: o => Convert.ToUInt64(o)));
			AddColumnTypeMetadata(new ColumnTypeMetadata("TINYINT", typeSbyte, MySqlDbType.Byte, isUnsigned: false));
			AddColumnTypeMetadata(new ColumnTypeMetadata("TINYINT", typeByte, MySqlDbType.UByte, isUnsigned: true));
			AddColumnTypeMetadata(new ColumnTypeMetadata("SMALLINT", typeShort, MySqlDbType.Int16, isUnsigned: false));
			AddColumnTypeMetadata(new ColumnTypeMetadata("SMALLINT", typeUshort, MySqlDbType.UInt16, isUnsigned: true));
			AddColumnTypeMetadata(new ColumnTypeMetadata("INT", typeInt, MySqlDbType.Int32, isUnsigned: false));
			AddColumnTypeMetadata(new ColumnTypeMetadata("INT", typeUint, MySqlDbType.UInt32, isUnsigned: true));
			AddColumnTypeMetadata(new ColumnTypeMetadata("MEDIUMINT", typeInt, MySqlDbType.Int24, isUnsigned: false));
			AddColumnTypeMetadata(new ColumnTypeMetadata("MEDIUMINT", typeUint, MySqlDbType.UInt24, isUnsigned: true));
			AddColumnTypeMetadata(new ColumnTypeMetadata("BIGINT", typeLong, MySqlDbType.Int64, isUnsigned: false));
			AddColumnTypeMetadata(new ColumnTypeMetadata("BIGINT", typeUlong, MySqlDbType.UInt64, isUnsigned: true));
			AddColumnTypeMetadata(new ColumnTypeMetadata("BIT", typeUlong, MySqlDbType.Bit));

			// decimals
			var typeDecimal = AddDbTypeMapping(new DbTypeMapping(typeof(decimal), new[] { DbType.Decimal, DbType.Currency, DbType.VarNumeric }, convert: o => Convert.ToDecimal(o)));
			var typeDouble = AddDbTypeMapping(new DbTypeMapping(typeof(double), new[] { DbType.Double }, convert: o => Convert.ToDouble(o)));
			var typeFloat = AddDbTypeMapping(new DbTypeMapping(typeof(float), new[] { DbType.Single }, convert: o => Convert.ToSingle(o)));
			AddColumnTypeMetadata(new ColumnTypeMetadata("DECIMAL", typeDecimal, MySqlDbType.NewDecimal, createFormat: "DECIMAL({0},{1});precision,scale"));
			AddColumnTypeMetadata(new ColumnTypeMetadata("DECIMAL", typeDecimal, MySqlDbType.Decimal));
			AddColumnTypeMetadata(new ColumnTypeMetadata("DOUBLE", typeDouble, MySqlDbType.Double));
			AddColumnTypeMetadata(new ColumnTypeMetadata("FLOAT", typeFloat, MySqlDbType.Float));

			// string
			var typeFixedString = AddDbTypeMapping(new DbTypeMapping(typeof(string), new[] { DbType.StringFixedLength, DbType.AnsiStringFixedLength }, convert: Convert.ToString));
			var typeString = AddDbTypeMapping(new DbTypeMapping(typeof(string), new[] { DbType.String, DbType.AnsiString, DbType.Xml }, convert: Convert.ToString));
			AddColumnTypeMetadata(new ColumnTypeMetadata("VARCHAR", typeString, MySqlDbType.VarChar, createFormat: "VARCHAR({0});size"));
			AddColumnTypeMetadata(new ColumnTypeMetadata("VARCHAR", typeString, MySqlDbType.VarString));
			AddColumnTypeMetadata(new ColumnTypeMetadata("CHAR", typeFixedString, MySqlDbType.String, createFormat: "CHAR({0});size"));
			AddColumnTypeMetadata(new ColumnTypeMetadata("TINYTEXT", typeString, MySqlDbType.TinyText, columnSize: byte.MaxValue, simpleDataTypeName: "VARCHAR"));
			AddColumnTypeMetadata(new ColumnTypeMetadata("TEXT", typeString, MySqlDbType.Text, columnSize: ushort.MaxValue, simpleDataTypeName: "VARCHAR"));
			AddColumnTypeMetadata(new ColumnTypeMetadata("MEDIUMTEXT", typeString, MySqlDbType.MediumText, columnSize: 16777215, simpleDataTypeName: "VARCHAR"));
			AddColumnTypeMetadata(new ColumnTypeMetadata("LONGTEXT", typeString, MySqlDbType.LongText, columnSize: uint.MaxValue, simpleDataTypeName: "VARCHAR"));
			AddColumnTypeMetadata(new ColumnTypeMetadata("ENUM", typeString, MySqlDbType.Enum));
			AddColumnTypeMetadata(new ColumnTypeMetadata("SET", typeString, MySqlDbType.Set));
			AddColumnTypeMetadata(new ColumnTypeMetadata("JSON", typeString, MySqlDbType.JSON));

			// binary
			var typeBinary = AddDbTypeMapping(new DbTypeMapping(typeof(byte[]), new[] { DbType.Binary }));
			AddColumnTypeMetadata(new ColumnTypeMetadata("BLOB", typeBinary, MySqlDbType.Blob, binary: true, columnSize: ushort.MaxValue, simpleDataTypeName: "BLOB"));
			AddColumnTypeMetadata(new ColumnTypeMetadata("BINARY", typeBinary, MySqlDbType.Binary, binary: true, simpleDataTypeName: "BLOB", createFormat: "BINARY({0});length"));
			AddColumnTypeMetadata(new ColumnTypeMetadata("VARBINARY", typeBinary, MySqlDbType.VarBinary, binary: true, simpleDataTypeName: "BLOB", createFormat: "VARBINARY({0});length"));
			AddColumnTypeMetadata(new ColumnTypeMetadata("TINYBLOB", typeBinary, MySqlDbType.TinyBlob, binary: true, columnSize: byte.MaxValue, simpleDataTypeName: "BLOB"));
			AddColumnTypeMetadata(new ColumnTypeMetadata("MEDIUMBLOB", typeBinary, MySqlDbType.MediumBlob, binary: true, columnSize: 16777215, simpleDataTypeName: "BLOB"));
			AddColumnTypeMetadata(new ColumnTypeMetadata("LONGBLOB", typeBinary, MySqlDbType.LongBlob, binary: true, columnSize: uint.MaxValue, simpleDataTypeName: "BLOB"));

			// spatial
			AddColumnTypeMetadata(new ColumnTypeMetadata("GEOMETRY", typeBinary, MySqlDbType.Geometry, binary: true));
			AddColumnTypeMetadata(new ColumnTypeMetadata("POINT", typeBinary, MySqlDbType.Geometry, binary: true));
			AddColumnTypeMetadata(new ColumnTypeMetadata("LINESTRING", typeBinary, MySqlDbType.Geometry, binary: true));
			AddColumnTypeMetadata(new ColumnTypeMetadata("POLYGON", typeBinary, MySqlDbType.Geometry, binary: true));
			AddColumnTypeMetadata(new ColumnTypeMetadata("MULTIPOINT", typeBinary, MySqlDbType.Geometry, binary: true));
			AddColumnTypeMetadata(new ColumnTypeMetadata("MULTILINESTRING", typeBinary, MySqlDbType.Geometry, binary: true));
			AddColumnTypeMetadata(new ColumnTypeMetadata("MULTIPOLYGON", typeBinary, MySqlDbType.Geometry, binary: true));
			AddColumnTypeMetadata(new ColumnTypeMetadata("GEOMETRYCOLLECTION", typeBinary, MySqlDbType.Geometry, binary: true));

			// date/time
			var typeDate = AddDbTypeMapping(new DbTypeMapping(typeof(DateTime), new[] { DbType.Date }));
			var typeDateTime = AddDbTypeMapping(new DbTypeMapping(typeof(DateTime), new[] { DbType.DateTime, DbType.DateTime2, DbType.DateTimeOffset }));
			var typeTime = AddDbTypeMapping(new DbTypeMapping(typeof(TimeSpan), new[] { DbType.Time }));
			AddColumnTypeMetadata(new ColumnTypeMetadata("DATETIME", typeDateTime, MySqlDbType.DateTime));
			AddColumnTypeMetadata(new ColumnTypeMetadata("DATE", typeDate, MySqlDbType.Date));
			AddColumnTypeMetadata(new ColumnTypeMetadata("DATE", typeDate, MySqlDbType.Newdate));
			AddColumnTypeMetadata(new ColumnTypeMetadata("TIME", typeTime, MySqlDbType.Time));
			AddColumnTypeMetadata(new ColumnTypeMetadata("TIMESTAMP", typeDateTime, MySqlDbType.Timestamp));
			AddColumnTypeMetadata(new ColumnTypeMetadata("YEAR", typeInt, MySqlDbType.Year));

			// guid
			var typeGuid = AddDbTypeMapping(new DbTypeMapping(typeof(Guid), new[] { DbType.Guid }, convert: o => Guid.Parse(Convert.ToString(o))));
			AddColumnTypeMetadata(new ColumnTypeMetadata("CHAR", typeGuid, MySqlDbType.Guid, length: 36, simpleDataTypeName: "CHAR(36)", createFormat: "CHAR(36)"));

			// null
			var typeNull = AddDbTypeMapping(new DbTypeMapping(typeof(object), new[] { DbType.Object }, convert: o => null));
			AddColumnTypeMetadata(new ColumnTypeMetadata("NULL", typeNull, MySqlDbType.Null));
		}

		public IReadOnlyList<ColumnTypeMetadata> GetColumnTypeMetadata() => m_columnTypeMetadata.AsReadOnly();

		public ColumnTypeMetadata GetColumnTypeMetadata(MySqlDbType mySqlDbType) => m_mySqlDbTypeToColumnTypeMetadata[mySqlDbType];

		public DbType GetDbTypeForMySqlDbType(MySqlDbType mySqlDbType) => m_mySqlDbTypeToColumnTypeMetadata[mySqlDbType].DbTypeMapping.DbTypes[0];

		public MySqlDbType GetMySqlDbTypeForDbType(DbType dbType)
		{
			foreach (var pair in m_mySqlDbTypeToColumnTypeMetadata)
			{
				if (pair.Value.DbTypeMapping.DbTypes.Contains(dbType))
					return pair.Key;
			}
			return MySqlDbType.VarChar;
		}

		private DbTypeMapping AddDbTypeMapping(DbTypeMapping dbTypeMapping)
		{
			m_dbTypeMappingsByClrType[dbTypeMapping.ClrType] = dbTypeMapping;

			if (dbTypeMapping.DbTypes != null)
				foreach (var dbType in dbTypeMapping.DbTypes)
					m_dbTypeMappingsByDbType[dbType] = dbTypeMapping;

			return dbTypeMapping;
		}

		private void AddColumnTypeMetadata(ColumnTypeMetadata columnTypeMetadata)
		{
			m_columnTypeMetadata.Add(columnTypeMetadata);
			var lookupKey = columnTypeMetadata.CreateLookupKey();
			if (!m_columnTypeMetadataLookup.ContainsKey(lookupKey))
				m_columnTypeMetadataLookup.Add(lookupKey, columnTypeMetadata);
			if (!m_mySqlDbTypeToColumnTypeMetadata.ContainsKey(columnTypeMetadata.MySqlDbType))
				m_mySqlDbTypeToColumnTypeMetadata.Add(columnTypeMetadata.MySqlDbType, columnTypeMetadata);
		}

		internal DbTypeMapping GetDbTypeMapping(Type clrType)
		{
			m_dbTypeMappingsByClrType.TryGetValue(clrType, out var dbTypeMapping);
			return dbTypeMapping;
		}

		internal DbTypeMapping GetDbTypeMapping(DbType dbType)
		{
			m_dbTypeMappingsByDbType.TryGetValue(dbType, out var dbTypeMapping);
			return dbTypeMapping;
		}

		public DbTypeMapping GetDbTypeMapping(string columnTypeName, bool unsigned = false, int length = 0)
		{
			return GetColumnTypeMetadata(columnTypeName, unsigned, length)?.DbTypeMapping;
		}

		private ColumnTypeMetadata GetColumnTypeMetadata(string columnTypeName, bool unsigned, int length)
		{
			if (!m_columnTypeMetadataLookup.TryGetValue(ColumnTypeMetadata.CreateLookupKey(columnTypeName, unsigned, length), out var columnTypeMetadata) && length != 0)
				m_columnTypeMetadataLookup.TryGetValue(ColumnTypeMetadata.CreateLookupKey(columnTypeName, unsigned, 0), out columnTypeMetadata);
			return columnTypeMetadata;
		}

		public static MySqlDbType ConvertToMySqlDbType(ColumnDefinitionPayload columnDefinition, bool treatTinyAsBoolean, bool oldGuids)
		{
			var isUnsigned = (columnDefinition.ColumnFlags & ColumnFlags.Unsigned) != 0;
			switch (columnDefinition.ColumnType)
			{
			case ColumnType.Tiny:
				return treatTinyAsBoolean && columnDefinition.ColumnLength == 1 ? MySqlDbType.Bool :
					isUnsigned ? MySqlDbType.UByte : MySqlDbType.Byte;

			case ColumnType.Int24:
				return isUnsigned ? MySqlDbType.UInt24 : MySqlDbType.Int24;

			case ColumnType.Long:
				return isUnsigned ? MySqlDbType.UInt32 : MySqlDbType.Int32;

			case ColumnType.Longlong:
				return isUnsigned ? MySqlDbType.UInt64 : MySqlDbType.Int64;

			case ColumnType.Bit:
				return MySqlDbType.Bit;

			case ColumnType.String:
				if (!oldGuids && columnDefinition.ColumnLength / ProtocolUtility.GetBytesPerCharacter(columnDefinition.CharacterSet) == 36)
					return MySqlDbType.Guid;
				if ((columnDefinition.ColumnFlags & ColumnFlags.Enum) != 0)
					return MySqlDbType.Enum;
				if ((columnDefinition.ColumnFlags & ColumnFlags.Set) != 0)
					return MySqlDbType.Set;
				goto case ColumnType.VarString;

			case ColumnType.VarString:
			case ColumnType.TinyBlob:
			case ColumnType.Blob:
			case ColumnType.MediumBlob:
			case ColumnType.LongBlob:
				var type = columnDefinition.ColumnType;
				if (columnDefinition.CharacterSet == CharacterSet.Binary)
				{
					if (oldGuids && columnDefinition.ColumnLength == 16)
						return MySqlDbType.Guid;

					return type == ColumnType.String ? MySqlDbType.Binary :
						type == ColumnType.VarString ? MySqlDbType.VarBinary :
						type == ColumnType.TinyBlob ? MySqlDbType.TinyBlob :
						type == ColumnType.Blob ? MySqlDbType.Blob :
						type == ColumnType.MediumBlob ? MySqlDbType.MediumBlob :
						MySqlDbType.LongBlob;
				}
				return type == ColumnType.String ? MySqlDbType.String :
					type == ColumnType.VarString ? MySqlDbType.VarChar :
					type == ColumnType.TinyBlob ? MySqlDbType.TinyText :
					type == ColumnType.Blob ? MySqlDbType.Text:
					type == ColumnType.MediumBlob ? MySqlDbType.MediumText :
					MySqlDbType.LongText;

			case ColumnType.Json:
				return MySqlDbType.JSON;

			case ColumnType.Short:
				return isUnsigned ? MySqlDbType.UInt16 : MySqlDbType.Int16;

			case ColumnType.Date:
				return MySqlDbType.Date;

			case ColumnType.DateTime:
				return MySqlDbType.DateTime;

			case ColumnType.Timestamp:
				return MySqlDbType.Timestamp;

			case ColumnType.Time:
				return MySqlDbType.Time;

			case ColumnType.Year:
				return MySqlDbType.Year;

			case ColumnType.Float:
				return MySqlDbType.Float;

			case ColumnType.Double:
				return MySqlDbType.Double;

			case ColumnType.Decimal:
				return MySqlDbType.Decimal;

			case ColumnType.NewDecimal:
				return MySqlDbType.NewDecimal;

			case ColumnType.Null:
				return MySqlDbType.Null;

			default:
				throw new NotImplementedException("ConvertToMySqlDbType for {0} is not implemented".FormatInvariant(columnDefinition.ColumnType));
			}
		}

		internal IEnumerable<ColumnTypeMetadata> GetColumnMappings()
		{
			return m_columnTypeMetadataLookup.Values.AsEnumerable();
		}

		readonly List<ColumnTypeMetadata> m_columnTypeMetadata;
		readonly Dictionary<Type, DbTypeMapping> m_dbTypeMappingsByClrType;
		readonly Dictionary<DbType, DbTypeMapping> m_dbTypeMappingsByDbType;
		readonly Dictionary<string, ColumnTypeMetadata> m_columnTypeMetadataLookup;
		readonly Dictionary<MySqlDbType, ColumnTypeMetadata> m_mySqlDbTypeToColumnTypeMetadata;
	}
}
