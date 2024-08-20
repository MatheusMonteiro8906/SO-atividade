public class MessageQueue<T> : IMessageQueue<T>
{
    private readonly Queue<T> queue;
    private readonly Queue<T> deadLetterQueue;
    private readonly Dictionary<T, int> retryCount;
    private readonly object lockObject = new object();
    private bool queueFullNotified = false;

    public int MaxRetries { get; set; } = 3;
    public int MaxQueueSize { get; set; } = 5; // Limite padrão de tamanho da fila

    public MessageQueue()
    {
        queue = new Queue<T>();
        deadLetterQueue = new Queue<T>();
        retryCount = new Dictionary<T, int>();
    }

    public void Enqueue(T message)
    {
        lock (lockObject)
        {
            if (queue.Count >= MaxQueueSize)
            {
                if (!queueFullNotified)
                {
                    Console.WriteLine("A fila está cheia. Mensagens adicionais serão rejeitadas até que haja espaço.");
                    queueFullNotified = true; 
                }
                return; 
            }

            queue.Enqueue(message);
            queueFullNotified = false; // Reset após adicionar uma mensagem
            Monitor.PulseAll(lockObject); // Notifica que a fila tem espaço para novas mensagens
        }
    }

    public T Dequeue()
    {
        lock (lockObject)
        {
            while (queue.Count == 0)
            {
                Monitor.Wait(lockObject);
            }

            T message = queue.Dequeue();

            if (!retryCount.ContainsKey(message))
            {
                retryCount[message] = 0;
            }

            retryCount[message]++;

            if (retryCount[message] > MaxRetries)
            {
                EnqueueToDLQ(message);
                retryCount.Remove(message);
                return default;
            }

            Monitor.PulseAll(lockObject); // Notifica que a fila tem espaço para novas mensagens
            return message;
        }
    }

    public void EnqueueToDLQ(T message)
    {
        lock (lockObject)
        {
            deadLetterQueue.Enqueue(message);
        }
    }

    public T DequeueFromDLQ()
    {
        lock (lockObject)
        {
            return deadLetterQueue.Count > 0 ? deadLetterQueue.Dequeue() : default;
        }
    }

    public void LogDLQMessages(string logFilePath)
    {
        lock (lockObject)
        {
            using (StreamWriter writer = new StreamWriter(logFilePath, append: true))
            {
                if (deadLetterQueue.Count == 0)
                {
                    writer.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] - MQ consumiu todas as mensagens recebidas.");
                }
                else
                {
                    foreach (var message in deadLetterQueue)
                    {
                        writer.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] - Mensagem capturada: {message}");
                    }
                }
            }
        }
    }

    public bool IsEmpty()
    {
        lock (lockObject)
        {
            return queue.Count == 0;
        }
    }
}
