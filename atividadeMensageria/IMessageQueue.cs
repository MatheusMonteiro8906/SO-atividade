public interface IMessageQueue<T>
{
    void Enqueue(T message);
    T Dequeue();
    bool IsEmpty();
    int MaxRetries { get; set; }
    void EnqueueToDLQ(T message);
    T DequeueFromDLQ();
    void LogDLQMessages(string logFilePath);
}