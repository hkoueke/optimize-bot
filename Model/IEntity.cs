namespace OptimizeBot.Model
{
    public interface IEntity<TKey>
    {
        TKey Id { get; set; }
    }
}
