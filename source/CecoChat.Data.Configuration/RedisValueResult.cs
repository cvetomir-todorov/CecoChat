namespace CecoChat.Data.Configuration
{
    public readonly struct RedisValueResult<TValue>
    {
        public RedisValueResult(bool isSuccess, TValue value)
        {
            IsSuccess = isSuccess;
            Value = value;
        }

        public bool IsSuccess { get; }
        public TValue Value { get; }

        public override string ToString()
        {
            return IsSuccess ? Value.ToString() : string.Empty;
        }

        public static RedisValueResult<TValue> Success(TValue value)
        {
            return new RedisValueResult<TValue>(isSuccess: true, value);
        }

        public static RedisValueResult<TValue> Failure(TValue value = default)
        {
            return new RedisValueResult<TValue>(isSuccess: false, value);
        }
    }
}
