using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Ceras.Test.GitHub
{
	public class PersistTypeCacheTest
	{
		SerializerConfig Config()
		{
			var config1 = new SerializerConfig();
			config1.Advanced.PersistTypeCache = true;
			config1.Advanced.EmbedChecksum = true;
			return config1;
		}
		
		[Fact]
		public void Test()
		{
			var serializer = new CerasSerializer(Config()); // one serializer like on service


			TestType(serializer);
			TestType(serializer);

			//TestClone(serializer);
			//TestClone(serializer);
			//TestClone(serializer);
		}

		void TestType(CerasSerializer serializer)
		{
			var deserializer = new CerasSerializer(Config());
			var b = serializer.Serialize(typeof(BpDto));

			var clone = deserializer.Deserialize<Type>(b);

			Assert.Equal(typeof(BpDto), clone);
		}

		void TestClone(CerasSerializer serializer)
		{
			var deserializer = new CerasSerializer(Config());

			var dto = new BpDto() { Title = "test", ID = 12, Inner = new BpDto() { Title = "inner", ID = 9, } };

			var b = serializer.Serialize(dto);

			var clone = deserializer.Deserialize<BpDto>(b);

			Assert.Equal(dto.Title, clone.Title);
		}

		public class BpDto
		{
			public int ID { get; set; }
			public string Title { get; set; }

			public BpDto Inner { get; set; }
		}
	}
}
