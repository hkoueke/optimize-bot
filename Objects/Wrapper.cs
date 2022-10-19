namespace OptimizeBot.Objects
{
    public class Wrapper<T> where T : struct
    {
        private T _t;

        public T Value
        {
            get => _t;
            set => _t = value;
        }

        public Wrapper(T value) => Value = value;

        public static implicit operator T(Wrapper<T> wrapper) => wrapper.Value;
        public static implicit operator Wrapper<T>(T value) => new(value);
        public override string ToString() => $"{_t}";
    }
}
