using System.Collections;
using System.Data.Common;

namespace DataAccessTracing.Tests.Mocks
{
    internal sealed class TestParameterCollection : DbParameterCollection
    {
        private readonly List<DbParameter> _parameters = new();
        IList InnerList => (IList)_parameters;

        public override int Count => _parameters.Count;

        public override object SyncRoot => InnerList.SyncRoot;

        public override int Add(object value)
        {
            {
                if (!(value is TestParameter param))
                    throw new ArgumentException();

                //if (Contains(param.ParameterName))
                //    throw new ArgumentException("duplicate", param.ParameterName);

                return InnerList.Add(value);
            }
        }

        public override void AddRange(Array values)
        {
            foreach (object val in values)
                Add(val);
        }

        public override void Clear() => _parameters.Clear();

        public override bool Contains(object value) => InnerList.Contains(value);

        public override bool Contains(string value) => _parameters.Any(p => p.ParameterName == value);

        public override void CopyTo(Array array, int index) => InnerList.CopyTo(array, index);

        public override IEnumerator GetEnumerator() => InnerList.GetEnumerator();

        public override int IndexOf(object value) => InnerList.IndexOf(value);

        public override int IndexOf(string parameterName) => _parameters.FindIndex(p => p.ParameterName == parameterName);

        public override void Insert(int index, object value) => InnerList.Insert(index, value);

        public override void Remove(object value) => InnerList.Remove(value);

        public override void RemoveAt(int index) => InnerList.RemoveAt(index);

        public override void RemoveAt(string parameterName) => _parameters.RemoveAll(p => p.ParameterName == parameterName);

        protected override DbParameter GetParameter(int index) => _parameters[index];

        protected override DbParameter GetParameter(string parameterName) => _parameters.First(p => p.ParameterName == parameterName);

        protected override void SetParameter(int index, DbParameter value) => _parameters[index] = value;

        protected override void SetParameter(string parameterName, DbParameter value)
        {
            var index = IndexOf(parameterName);
            if (index < 0)
                throw new ArgumentException("Parameter not found", nameof(parameterName));
            _parameters[index] = value;
        }
    }
}
