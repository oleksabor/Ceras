using Ceras;
using Ceras.Formatters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Ceras.Test.TestsNested
{
	public class NestedFieldMapping
	{
		public NestedFieldMapping()
		{
			DynamicFormatter.SerializerNestedInjector = DynamicFormatterNested.InjectToSerializer;
			DynamicFormatter.DeserializerNestedInjector = DynamicFormatterNested.InjectToDeserializer;
		}

		[Fact]
		void map_nested_inner()
		{
			var d = new Data() { Id = 12, Ignored = "some value", Reason = "maped",
				Inner = new Data() { Id = 3 }
			};

			var dc = Clone(d, null);

			Assert.Equal(3, dc.Inner.Id);
			Assert.Throws<NullReferenceException>(() => dc.Inner.Inner);
			Assert.Null(dc.Ignored);
			Assert.Equal("maped", dc.Reason);
		}

		[Fact]
		void map_nested_inner_list()
		{
			var d = new Data()
			{
				Id = 12,
				InnerList = new[] { new Data { Id = 10, Created = DateTime.Today } }.ToList(),
			};

			var dc = Clone(d, null);

			Assert.Equal(10, dc.InnerList[0].Id);
			Assert.Throws<NullReferenceException>(() => d.InnerArray);
		}

		[Fact]
		void map_nested_inner_array()
		{
			var d = new Data()
			{
				Id = 12,
				InnerArray = new[] { new Data { Id = 10, Created = DateTime.Today } },
			};

			var dc = Clone(d, null);

			Assert.Equal(10, dc.InnerArray[0].Id);
			Assert.Throws<NullReferenceException>(() => d.InnerList);
		}

		[Fact]
		void isdirty()
		{
			var d = new Data { Id = 1, Title = "asdf" };

			var dc = Clone(d, null); // client has serialized dirty object
			Assert.True(dc.IsDirty); // service should get dirty object

			d.IsDirtyReset(); // like server does after object was read 
			dc = Clone(d, null);
			Assert.False(dc.IsDirty); // client should get non dirty object after deserialization
		}

		public T Clone<T>(T source, SerializerConfig config = null)
		{
			var ceras = new CerasSerializer(config);

			byte[] data = new byte[0x1000];
			int len = ceras.Serialize(source, ref data);

			T clone = default(T);
			int read = 0;
			ceras.Deserialize(ref clone, data, ref read, len);

			return clone;
		}

		delegate bool Dele(System.Reflection.PropertyInfo pi, Data d);

		[Fact]
		void ArrayTest()
		{
			var al = new[] { "one", "another one" };

			SerializerConfig config = new SerializerConfig();
			var ceras = new CerasSerializer(config);

			var buffer = ceras.Serialize(al);

			string[] al2 = null;

			ceras.Deserialize(ref al2, buffer);

			Assert.Equal(al.Count(), al2.Count());
		}

		[Fact]
		void GenericListValues()
		{
			var al = new List<int>();
			al.Add(1);
			al.Add(2);
			al.Add(3);

			SerializerConfig config = new SerializerConfig();
			var ceras = new CerasSerializer(config);

			var buffer = ceras.Serialize<object>(al);

			var o = ceras.Deserialize<object>(buffer);
			var al2 = o as List<int>;

			Assert.Equal(al.Count, al2.Count);
		}


		static bool HasData<T>(System.Reflection.PropertyInfo pi, T value)
		{
			return value != null && pi == null;
		}

	
	}

	public interface IData
	{
		DateTime Created { get; set; }
		int Id { get; set; }
		List<Data> InnerList { get; set; }
		string Title { get; set; }
		Data Inner { get; set; }

		string Reason { get; set; }
	}

	// no Serializable attribute
	public class Data : IData
	{
		public Data()
		{
			Id = 11;
			Created = DateTime.Now;
		}

		[OnBeforeDeserialize]
		void IgnoreSetIsDirty() //required to deserialize IsDirty properly
		{
			_ignoreSetDirty = true;
		}
		[OnAfterDeserialize]
		void RestoreSetIsDirty() //required to set IsDirty properly
		{
			_ignoreSetDirty = false;
		}

		Func<Data> factory;

		public string Reason { get; set; }

		[Exclude]
		public string Ignored { get; set; }

		int _id;
		public int Id { get => _id; set { Set(ref _id, value); } }

		public DateTime Created { get; set; }

		bool _ignoreSetDirty;

		public bool IsDirty { get; protected set; }

		public void IsDirtyReset()
		{
			IsDirty = false;
		}

		bool Set<T>(ref T oldValue, T newValue)
		{
			if (!EqualityComparer<T>.Default.Equals(oldValue, newValue))
			{
				if (!_ignoreSetDirty)
					IsDirty = true;
				oldValue = newValue;
				return true;
			}
			return false;
		}

		int _innerListId;
		public int InnerListId { get => _innerListId; set { Set(ref _innerListId, value); } }

		string _title;
		public string Title { get => _title; set { Set(ref _title, value); } }

		List<Data> _innerList;
		public List<Data> InnerList { get { return _innerList ?? new[] { factory() }.ToList(); } set { _innerList = value; } }
	
		Data[] _innerArray;
		public Data[] InnerArray { get { return _innerArray ?? new[] { factory() }; } set { _innerArray = value; } }

		Data _inner;
		public Data Inner { get { return _inner ?? factory(); } set { _inner = value; } }
	}
}
