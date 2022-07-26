﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;

namespace GeekBurger.ProductChanged
{
    public class Program
    {
        public static IEnumerable<Task> PendingCompleteTasks { get; }
        private const string QueueConnectionString = "Endpoint=sb://geekburguer.servicebus.windows.net/;SharedAccessKeyName=ProductPolicy;SharedAccessKey=JBg/mWxJ8W9MXbfDrTYr0UJGRfv65YQDUTfwaLgDUeU=";
        private const string QueuePath = "ProductChanged";
        private static IQueueClient _queueClient;

        #region PUBLICO
        public static void CheckCommunicationExceptions(Task task)
        {
            if (task.Exception == null || task.Exception.InnerExceptions.Count == 0) return;

            task.Exception.InnerExceptions.ToList().ForEach(innerException =>
            {
                Console.WriteLine($"Error in SendAsync task: {innerException.Message}" +
                    $".Details: {innerException.StackTrace}");

                if (innerException is ServiceBusCommunicationException) Console.WriteLine("Connection Problem with Host");
            });
        }
        #endregion

        #region PRIVATES

        private static async Task ReceiveMessagesAsync()
        {
            // Recebendo mensagem – tratamento de cancelamento prematuro
            _queueClient = new QueueClient(QueueConnectionString, QueuePath);
            _queueClient.RegisterMessageHandler(MessageHandler, new MessageHandlerOptions(ExceptionHandler) { AutoComplete = false });
            Console.ReadLine();
            Console.WriteLine($" Request to close async. Pending tasks: {PendingCompleteTasks.Count()}");

            await Task.WhenAll(PendingCompleteTasks);
            Console.WriteLine("All pending tasks were completed");

            var closeTask = _queueClient.CloseAsync();
            await closeTask;
            CheckCommunicationExceptions(closeTask);
        }

        private static async Task SendMessagesAsync()
        {

            _queueClient = new QueueClient(QueueConnectionString, QueuePath);
            _queueClient.RegisterMessageHandler(MessageHandler,
                new MessageHandlerOptions(ExceptionHandler) { AutoComplete = false });
            Console.ReadLine();
            await _queueClient.CloseAsync();

            var queueClient = new QueueClient(QueueConnectionString,
                QueuePath);
            queueClient.OperationTimeout = TimeSpan.FromSeconds(10);
            var messages = " Hi,Hello,Hey,How are you,Be Welcome".Split(',')
                .Select(msg =>
                {
                    Console.WriteLine($"Will send message: {msg}");
                    return new Message(Encoding.UTF8.GetBytes(msg));
                })
                .ToList();

            var sendTask = queueClient.SendAsync(messages);
            await sendTask;
            CheckCommunicationExceptions(sendTask);

            var closeTask = _queueClient.CloseAsync();
            await closeTask;
            CheckCommunicationExceptions(closeTask);
        }

        private static async Task MessageHandler(Message message, CancellationToken cancellationToken)
        {

            Console.WriteLine($"Received message: {Encoding.UTF8.GetString(message.Body)}");

            if (cancellationToken.IsCancellationRequested || _queueClient.IsClosedOrClosing) return;

            int count = 0;
            Console.WriteLine($"task {count++}");

            Task PendingTask;
            lock (PendingCompleteTasks)
            {
                PendingCompleteTasks.Append(_queueClient.CompleteAsync(message.SystemProperties.LockToken));
                PendingTask = PendingCompleteTasks.LastOrDefault();
            }
            Console.WriteLine($"calling complete for task {count}");

            await PendingTask;
            Console.WriteLine($"remove task {count} from task queue");
            PendingCompleteTasks.Append(PendingTask);
        }

        private static void Main()
        {
            SendMessagesAsync().GetAwaiter().GetResult();
            Console.WriteLine("messages were sent");
            Console.ReadLine();
        }

        private static Task ExceptionHandler(ExceptionReceivedEventArgs exceptionArgs)
        {
            Console.WriteLine($"Message handler encountered an exception {exceptionArgs.Exception}.");
            var context = exceptionArgs.ExceptionReceivedContext;
            Console.WriteLine($"Endpoint:{context.Endpoint}, Path:{context.EntityPath}, Action:{context.Action}");
            return Task.CompletedTask;
        }


        #endregion


    }
}
