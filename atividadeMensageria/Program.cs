class Program
{
    static void Main(string[] args)
    {
        string logFilePath = "DLQMessages.log";

        IMessageQueue<string> messageQueue = new MessageQueue<string>();

        Thread producer = new Thread(() =>
        {
            for (int i = 0; i < 5; i++)
            {
                string message = $"Mensagem {i + 1}";
                Console.WriteLine($"Produzindo: {message}");
                messageQueue.Enqueue(message);
                Thread.Sleep(1000);
            }
        });

        Thread consumer = new Thread(() =>
        {
            for (int i = 0; i < 5; i++)
            {
                string message = messageQueue.Dequeue();

                if (message != null && i % 2 == 0) // Simulando falha em mensagens pares
                {
                    Console.WriteLine($"Erro ao processar: {message}");
                    messageQueue.EnqueueToDLQ(message); // Enfileirando na DLQ
                    messageQueue.Enqueue(message); // Reenfileirando para retry
                }
                else if (message != null)
                {
                    Console.WriteLine($"Consumindo: {message}");
                }

                Thread.Sleep(1500);
            }

            messageQueue.LogDLQMessages(logFilePath);

        });

        producer.Start();
        consumer.Start();

        producer.Join();
        consumer.Join();

        Console.WriteLine("Processo de mensagem concluído.");
    }
}